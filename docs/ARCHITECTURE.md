## EGMENGINE architecture & design guide

This document is a **code-driven** explanation of the architecture of the `EGMENGINE` program as it exists in this repository. It focuses on:

- **What the major modules are**
- **How data flows** (UI/menu ↔ engine ↔ SAS ↔ storage ↔ WebSockets)
- **Where state lives** (settings/status/accounting) and **how it is persisted**
- **What design patterns are used**, and where
- **How access control works** (roles, menu permissions, access levels)

It is written for someone who wants to be able to open the codebase and quickly answer “where does X happen?”

---

## Repository & project layout

At the solution level:

- `EGMENGINE.sln`: solution
- `EGMENGINE/EGMENGINE.csproj`: single `.NET` project (`netstandard2.1`)
- `EGMENGINE/EGM.cs`: the central “engine/orchestrator” class (singleton, partial)

Inside `EGMENGINE/` the architecture is organized primarily by subsystem:

- **Core orchestration**
  - `EGM.cs` (main engine + init + event wiring + WebSockets)
  - `EGMAux.cs` (shared helpers: credits, meters, tilts, conversions, etc)
  - `EGMPlaySpin.cs` (spin/play pipeline; uses `SlotMathCore`)
- **UI façade layer (engine-facing “API” for front-end)**
  - `GUI/GameGUIController.cs` (game UI calls)
  - `GUI/MenuGUIController.cs` (menu UI calls + menu security matrix)
  - `GUI/MenuTypes.cs`, `GUI/GameTypes.cs`, `GUI/GlobalTypes.cs` (DTOs/enums)
- **State and domain model**
  - `EGMStatus/` (runtime state machines + live status)
  - `EGMSettings/` (configuration/settings, including SAS config)
  - `EGMAccounting/` (meters/history/logs)
- **Persistence**
  - `EGMDataPersister/EGMDataPersisterCTL.cs` (SQLite persistence controller)
  - `EGMDataPersister/IntegrityController.cs` (integrity flags like RAM/DATABASE mismatch)
  - `DATABASE/EGMEngineDB.db` (a DB file included in repo; runtime path is elsewhere—see persistence section)
- **External/hardware integration**
  - `SASCTL/` (SAS controller + SAS client implementations)
  - `BillAccCTL/` (bill acceptor controller + state machine)
  - `GPIOCTL/` (I/O controller; real vs virtual)
- **Native/3rd-party libs**
  - `GXGSASAPI.dll`, `GXGCoreAPI.dll`, etc + `LibDependencies/` bridges
  - NuGet packages: `Mono.Data.Sqlite`, `Newtonsoft.Json`, `System.IO.Hashing`, `websocketsharp.core`

### Build-time dependencies (from `EGMENGINE.csproj`)

- **Target framework**: `netstandard2.1`
- **NuGet packages**
  - `Mono.Data.Sqlite` (SQLite persistence)
  - `Newtonsoft.Json` (JSON serialization + parsing)
  - `System.IO.Hashing` (CRC32 used for DB schema integrity check)
  - `websocketsharp.core` (WebSocket client/server library used as a client here)
- **Native DLL references copied to output**
  - `GXGCoreAPIBridge.dll`, `GXGGFMAPIBridge.dll` (bridges)
  - `GXGSASAPI.dll` + headers/libs
  - hardware/support DLLs like `libusb-1.0.dll`, `wdapi1250.dll`

---

## The big picture (mental model)

This program is built around **one central singleton** that acts as the “kernel”:

- `EGMENGINE.EGM` (in `EGMENGINE/EGM.cs`) is the orchestrator.

Everything else is effectively a subsystem or façade around the EGM:

- **UI layer** calls into EGM via `GameGUIController` and `MenuGUIController`.
- **Hardware/event sources** (SAS, bill acceptor, GPIO) push events into EGM.
- **EGM holds/updates state** in singleton models: `EGMStatus`, `EGMSettings`, `EGMAccounting`.
- **Persistence** is handled by `EGMDataPersisterCTL` (SQLite), and is triggered frequently from EGM.
- **Networking/WebSockets** is also hosted inside EGM (using `WebSocketSharp`), used for telemetry and remote-triggered actions (like forced spins and AFT confirmations).

### Layering (what calls what)

Practical call directions in this codebase:

- UI → (`GameGUIController`, `MenuGUIController`) → `EGM` → (`EGMStatus`, `EGMSettings`, `EGMAccounting`) → (`SASCTL`, `IBillAccCTL`, `IGPIOCTL`) → (native libs/services)
- “Hardware interrupts / events” → (`SASCTL`, `IBillAccCTL`, `IGPIOCTL`) → `EGM`
- “Persistence timer” (inside `EGM`) → `EGMDataPersisterCTL`

There is no strict clean-architecture boundary enforced (this is not a pure DDD hexagonal system), but the controllers (`GUI/*Controller.cs`) act as “ports” and the subsystem controllers (`SASCTL`, bill acceptor, GPIO) act as “adapters”.

### “EGM is a partial class” (how the code is physically organized)

`EGM` is declared as `internal partial class EGM` and is split across:

- `EGMENGINE/EGM.cs`: constructor/init, event wiring, WebSockets, menu integration points
- `EGMENGINE/EGMAux.cs`: cross-cutting helpers (credits/meter sync, tilts, conversions)
- `EGMENGINE/EGMPlaySpin.cs`: spin/play creation and finishing logic (slot math, meters, persistence)

When navigating “where is the method implemented?”, keep this in mind.

### Startup sequence (what happens when `EGM.GetInstance()` is called)

`EGM` uses lazy initialization: the first call to `EGM.GetInstance()` constructs the engine.

High-level startup flow inside `EGM` constructor:

- **Environment detection**
  - Sets `unitydevelopment = true` if it finds `Assets/EGMEngine.dll`
  - Sets `localganlot = true` if it finds `%USERPROFILE%\Desktop\LocalGanlot186200034154.txt`
- **Persistence boot**
  - Initializes SQLite persistence controller (`InitEGMDataPersister(...)`)
  - Reads persisted `EGMSettings` and `EGMAccounting` into their singletons
- **Subsystem init / wiring**
  - Initializes SAS controller + client selection (`InitSASController()` + later `SASCTL.Init(...)`)
  - Initializes bill acceptor controller and subscribes to its events (`InitBillAcceptorController()`)
  - Initializes `EGMStatus` and conditionally reads persisted status (`ReadEGMStatus()` only if DB OK)
  - Initializes GPIO controller and subscribes to door/button events (`InitGPIOController()`)
  - Loads slot math model (`math_ctrller.LoadModel()`)
- **SAS sync + machine identity**
  - Sets current player denomination, SAS address (from `SASId`), asset number, serial number
  - Pushes current credits and all meters to SAS
- **WebSockets**
  - Initializes a WebSocket connection to `ws://localhost:5000/ws`
  - Sends a connection test message and begins processing inbound messages
- **Starts the engine loop**
  - Starts a high-frequency timer that repeatedly:
    - recomputes front-end state (`ReprocessEGMStatus_Frontend()`)
    - persists status/settings/accounting (`PersistAllData(false, false)`)

---

## Where state lives (and why)

This project centralizes most mutable state into 3 “global singleton models”:

### 1) `EGMStatus` (runtime, frequently changing)

File: `EGMENGINE/EGMStatus/EGMStatus.cs`

Examples of what lives here:

- Credits split into **cashable / restricted / nonrestricted**, plus `currentAmount` total
- Front-end state machines: `frontend_play`, `frontend_play_penny`, `spinstatus`
- Menu state: `menuActive`, `maintenanceMode`, `currentLoggedInUser`
- Tilt list: `currentTilts`
- Flags like `setDateTime`, `setSAS`, `fullramclearperformed`

This is the “truth” for current machine state.

### 2) `EGMSettings` (configuration)

File: `EGMENGINE/EGMSettings/EGMSettings.cs`

Examples:

- Role PINs (attendant/operator/technician/manufacturer)
- Credit limits, jackpot enable/limit
- Cashin/cashout toggles: `AFTEnabled`, `HandpayEnabled`, `BillAcceptor`, `PartialPay`, `CashOutOrder`
- Bill acceptor config: COM port + channel bitmasks
- SAS configuration: `sasSettings` (see `EGMENGINE/EGMSettings/EGMSASConfig/*`)

This is “what the machine should be configured to do.”

### 3) `EGMAccounting` (meters + history)

File: `EGMENGINE/EGMAccounting/EGMAccounting.cs` plus the submodules under `EGMAccounting/`

Examples:

- Meter model (many SAS-style meters)
- Histories: last plays, last bills, AFT transfers, system logs, ram clears, handpays

This is “auditing/statistics”.

---

## Persistence (how the program stores info)

Controller: `EGMENGINE/EGMDataPersister/EGMDataPersisterCTL.cs`

### Storage technology

- Uses **SQLite** via `Mono.Data.Sqlite`.
- Stores data in `EGMEngineDB.db`.

### Where the DB is expected at runtime

The runtime DB path is selected by environment flags:

- If `unitydevelopment == false`:
  - if `localganlot == true`: `C:/Gamedata\EGMEngineDB.db`
  - else: `D:/Gamedata\EGMEngineDB.db`
- Else (unity/dev): `H:/Gamedata\EGMEngineDB.db`

If the DB file is missing, it sets `IntegrityController.GetInstance().RAMERROR = true` and returns `"RAM ERROR - Missing"`.

### Integrity checking / schema checksum

On startup, the persister reads SQLite schema from `sqlite_master` and computes a **CRC32 checksum**; it compares against a hard-coded `dbchecksum` string.

If the checksum differs, it flags integrity error and returns `"DATABASE MISMATCH"`.

### What gets persisted (high level)

EGM triggers persistence via a high-frequency timer (see below). The persister persists:

- **EGMStatus** (current credits, state machine states, tilt flags, etc)
- **EGMSettings** (configuration)
- **EGMAccounting** (meters + history tables)
- **Slot play** snapshot/history (from `EGMPlay`)

Histories are **truncated** on startup (e.g., last bills capped by `lastbillMax`, AFT transfers capped by `lastAFTTransferMax`).

### What tables exist (based on persister queries)

The persister reads/writes at least these tables:

- **Core tables**
  - `EGMStatus`
  - `EGMSettings`
  - `EGMCurrentSlotPlay`
- **Accounting/history tables**
  - `EGMAccountingMeters`
  - `EGMAccountingLastBills`
  - `EGMAccountingLastPlays`
  - `EGMAccountingAFTTransfers`
  - `EGMAccountingSystemLogs`
  - `EGMAccountingHandpay`
  - `EGMAccountingRamClears`

### When persistence happens

EGM uses a timer loop and also persists explicitly after some state transitions:

- Regular loop: in `EGM`, a `Timer` calls `PersistAllData(false, false)` frequently (the code shows `50ms` intervals).
- Explicit persistence: after events like RAM clear, transfers, etc, it calls `PersistEGMStatus(true)`, `PersistEGMAccounting(true)`, `PersistEGMSettings(true)`.

---

## Menu/settings flow (how info passes into the menu system)

This program does **not** have the UI directly mutate models. Instead, it uses a façade:

- UI calls `MenuGUIController` methods (public).
- `MenuGUIController` delegates to `EGM` internal “menu” methods: `Menu_*`.
- `EGM` changes `EGMSettings` and/or `EGMStatus`, triggers SAS updates, and persists.

### Menu controller as façade

File: `EGMENGINE/GUI/MenuGUIController.cs`

Patterns:

- **Facade**: exposes many `GUI_*` methods that become the “public API” for the front-end menu.
- **Access control matrix**: it precomputes a 7-dimensional array mapping “conditions → access”.

### UI eventing model (how the engine “pushes” info to the menu)

Although the engine uses C# events internally, the menu controller exposes a **queue-based pull model** for the front-end:

- `EGM` raises `UIPriorityEvent` (a C# event) when it wants to notify the UI.
- `MenuGUIController` subscribes to `EGM.GetInstance().UIPriorityEvent` and appends those events into an in-memory `List<EventType> current_events`.
- The front-end can call `MenuGUIController.ConsumeEvent()` to pull one event at a time.

This pattern is used to avoid the UI having to subscribe to engine events directly, and it gives the UI a simple “poll and consume” interface.

### Menu access rules (roles + conditions)

`MenuGUIController` builds a matrix:

- Dimensions include:
  - `UserRole` (5 values)
  - `MenuSceneName` (52 screens)
  - Main door open? (2)
  - Logic door open? (2)
  - Has credits? (2)
  - Ram clear performed? (2)
  - Bill acceptor enabled? (2)

Then `GUI_Menu_Validate_MenuAccess(MenuSceneName name)` returns `ReadOnly`, `FullAccess`, or `NotAuthorized` based on the current machine state and logged-in role.

### Concrete examples: how settings updates flow

#### Example A: Update SAS configuration from menu

UI call:

- `MenuGUIController.GUI_Set_Configuration_SASConfiguration(Configuration_SASConfiguration input)`

Flow:

- `MenuGUIController` → `EGM.Menu_UpdateSASConfiguration(input)`
- `EGM` updates `EGMSettings.GetInstance().sasSettings.*`
- `EGM` updates SAS runtime via `SASCTL` (e.g., `SetSerialNumber`, `SetAssetNumber`, `UpdateSASInfo`, linking SAS id to SAS address)
- `EGM` persists settings via `EGMDataPersisterCTL.PersistEGMSettings(true)` (and sometimes persists status too)

#### Example B: Update cashin/cashout toggles (BillAcceptor, AFT, Handpay)

UI call:

- `MenuGUIController.GUI_Set_Configuration_CashInCashOutConfiguration(Configuration_CashInCashOutConfiguration input)`

Flow:

- `MenuGUIController` → `EGM.Menu_UpdateCashinCashoutConfiguration(input)`
- `EGM` updates `EGMSettings` (toggles/features)
- `EGM` may enable/disable subcontrollers (bill acceptor, SAS AFT) accordingly
- `EGM` persists settings

#### Example C: Update bill denomination channel enables

UI call:

- `MenuGUIController.GUI_Set_Configuration_BillsConfiguration(Configuration_BillsConfiguration configuration)`

Flow:

- `MenuGUIController` → `EGM.Menu_SetBillDenomination(List<Channel>)`
- `EGM` converts the list into three bitmask bytes (`lsb`, `snd_byte`, `thrd_byte`)
- Updates settings: `EGMSettings.BillAcceptorChannelSet1/2/3`
- Pushes to bill acceptor controller: `billAcc.ConfigBillDenominations(...)`
- (Persistence typically occurs on the next persistence loop, or via explicit persist depending on the menu action)

---

## SAS communication (how it talks to SAS and sends info)

### Architectural approach

SAS integration is wrapped in two layers:

1) `SASCTL` (controller, business logic, conversion, event re-emit)
2) `ISASClient` implementations (device-specific transport + protocol)

This gives the code two major modes:

- Real hardware mode: `GanlotSASClient`
- Dev mode: `DevSASClient`

### SAS controller (`SASCTL`)

File: `EGMENGINE/SASCTL/SASCTL.cs`

Responsibilities:

- Instantiate the correct `ISASClient` implementation depending on `unitydevelopment`
- Subscribe to long-poll events from the SAS client (VirtualEGM LP handlers)
- Convert values between:
  - **SAS “integer format”** (credits in units of `SASReportedDenomination`)
  - **internal decimal currency** (e.g., ZAR cents/rands)
- Provide a higher-level event API consumed by `EGM`:
  - Game enable/disable
  - Sound enable/disable
  - Bill acceptor enable/disable + host denomination config
  - AFT incoming/completed/rejected
  - Maintenance mode enter/exit
  - Tilt/link-down events
- Provide a higher-level command API used by `EGM`:
  - Send exceptions (door open/close, bill inserted, etc)
  - Set credits per pool
  - Update meters
  - Handle RAM clear requests
  - Handle AFT lock state/busy state

### How SAS events come in (host → EGM)

`ISASClient` raises events like:

- `VirtualEGM01`, `VirtualEGM02`, … (host commands)
- `VirtualEGMLP72` (AFT transfer data)
- `VirtualEGM74` (AFT lock / lock-related events)
- `SASLinkDown`, `ClientCriticalError`, `ClientNoError`

`SASCTL` subscribes to these and re-emits them using its own delegates such as:

- `AFTTransferIncoming`, `AFTTransferCompleted`, `AFTTransferRejected`
- `SASTiltDetected`, `SASTiltLinkDown`, `SASNoTiltDetected`
- `EnterMaintenanceMode`, `ExitMaintenanceMode`

Then `EGM` subscribes to `SASCTL` events and updates internal state, logs, meters, persistence, etc.

### How the program sends info to SAS (EGM → host)

Key patterns:

- **Exceptions**: `SASCTL.LaunchExceptionByEvent(SASEvent)` → `client.SendException(...)`
  - Example: when a bill is accepted, EGM calls `SASCTL.SASBillException(bill)` which maps bill value → a SAS exception code (e.g., Bill10Inserted).
  - Example: door sensors call `SASCTL.SASDoorOpenException(...)`
- **Credits**: `SASCTL.SetCredits(type, decimal)` → `client.SetCredits(type, ulongValue)`
  - EGM uses this especially during AFT completion (`ProcessTransferCompletion(...)`) to sync pools back to host.
- **Meters**: `SASCTL.SetMeter(...)` / `GetMeter(...)` / `ResetMeters()`
- **AFT transfers**:
  - Host sends LP72 (incoming transfer)
  - `SASCTL` evaluates limits/lock state and calls `client.FinishAFTTransfer(...)`
  - `SASCTL` then raises `AFTTransferCompleted(...)` to EGM
  - EGM updates credits + persists + logs
- **AFT locks / busy state**:
  - `SASCTL.HandleLP74(...)` reacts to host lock request/cancel
  - Calls into client helpers like `CheckAndHandleAFTLocks()` / `CancelAFTLock()`, and may call `SetSASBusy(false)`

### Transport layer (`GanlotSASClient`)

Folder: `EGMENGINE/SASCTL/SASClient/GanlotSASClient/*`

This implementation is a bridge to native libs:

- Uses `GXGSASAPIBridge.*` calls (P/Invoke / DLL bridge)
- Uses `GXGSAS` driver/API versioning, firmware queries, interrupt callback registration, etc.

In other words: **SASCTL is “our logic”**; the `GanlotSASClient` is “the protocol driver/adapter”.

---

## WebSocket integration (how it sends/receives info over WebSockets)

File: `EGMENGINE/EGM.cs`

### Library + endpoint

- Uses `websocketsharp.core` (`WebSocketSharp.WebSocket`)
- Connects to a hard-coded URL: `ws://localhost:5000/ws`

### Outbound messages (EGM → WebSocket server/client)

EGM serializes anonymous objects via `Newtonsoft.Json.JsonConvert.SerializeObject(update)` and calls `webSocket.Send(json)`.

Observed event types include:

- `BILL_INSERTED` (via `SendBillAcceptedToWebSocket`)
  - Includes amount, current credits, timestamp, currency
- `AFT_DEPOSIT` / `AFT_CASHOUT` (via `SendAFTWebSocket`)
  - Includes amount, current credits, reference
- `SPIN_COMPLETED` (via `SendSpinCompletionMessage`)
- Various test messages (`CONNECTION_TEST`, etc)

### Inbound messages (WebSocket server/client → EGM)

EGM receives JSON strings, parses them with `JObject.Parse(...)`, and reacts to:

- `GAME_UPDATE`:
  - Triggers `TriggerSpinFromWebSocket(betAmount, winAmount)` in a background task
  - Uses a lock (`_spinLock`) + boolean guard (`_isProcessingSpin`) to avoid overlapping spins
- `CREDIT_UPDATE`: logs only (currently)
- `AFT_CONFIRMED`: placeholder / intended to confirm AFT completion (see AFT confirmation section)

### AFT confirmation via WebSockets (important nuance)

The code includes an architecture for “**send AFT event to WebSocket client, then wait for confirmation**”:

- EGM stores a pending transfer (`_pendingTransferData`)
- Sets a waiting flag (`_waitingForConfirmation`)
- Sends `AFT_DEPOSIT` or `AFT_CASHOUT` over WebSocket
- Starts a retry timer that will resend the message until confirmed
- Exposes `HandleTransferConfirmation(bool confirmed)` for when a confirmation message arrives

However, in the current implementation, the AFT completion handler calls `HandleTransferConfirmation(true)` immediately after sending the WebSocket message (so it **auto-confirms** right now). That means the “wait for WebSocket confirmation” is present structurally, but not enforced yet.

### Important: WebSocket logging

`EGM.cs` includes a `Logger` that writes to:

- Desktop file: `egm_websocket_log.txt`

This is used heavily by the WebSocket-related code paths.

---

## Bill acceptor (how it communicates and sends info)

There are two bill acceptor implementations:

- Real: `SSPBillAccServiceCTL` (talks to a Windows service via named pipes)
- Dev: `VirtualBillAccCTL` (simulated)

### Real bill acceptor via service + named pipes

File: `EGMENGINE/BillAccCTL/Impl/SSPBillAccServiceCTL/SSPBillAccServiceCTL.cs`

Key points:

- Uses `NamedPipeServerStream` / `NamedPipeClientStream` to send commands and listen for events.
- Keeps a keep-alive poll timer and error counters to detect:
  - service unreachable
  - port comm failure
  - comm restored
- Deserializes JSON event messages into a generic `Event` object and switches on `EventName`:
  - `CommEnabled`, `ChannelConfigUpdated`, `BillInserted`, `BillAccepted`, etc

### How bill acceptance affects the system (end-to-end flow)

When the bill acceptor emits `BillAccepted`:

1) `SSPBillAccServiceCTL` raises its `BillAccepted` C# event
2) `EGM` subscribed handler executes:
   - Adds credits (`AddAmount(bill, ...)`)
   - Adds a system log (“R X.00 bill accepted”)
   - Sends a WebSocket message (`BILL_INSERTED`)
   - Sends SAS exception (bill inserted) via `SASCTL.SASBillException(bill)`
   - Appends to last-bills history (`EGMAccounting.bills.AddNewBill(...)`)
   - Updates meters
3) Persistence loop eventually persists status/accounting/settings.

### Bill acceptor state machine

File: `EGMENGINE/BillAccCTL/BillAccStateMachine.cs`

The validator controller keeps an explicit state machine:

- `Idle → BillInserted → Validating → (Stacking|Rejecting) → Idle` (and jam paths)

This is used to ensure consistent event sequencing and avoid invalid transitions.

---

## GPIO / sensors / outputs (I/O architecture)

Folder: `EGMENGINE/GPIOCTL/`

There are two implementations:

- `GanlotGPIOCTL` (real hardware)
- `VirtualGPIOCTL` (dev mode)

EGM subscribes to sensor events (like doors) and:

- Adds tilts (`AddTilt(...)`)
- Adds system logs (`AddSystemLog(...)`)
- Notifies SAS (`SASCTL.SASDoorOpenException(...)`)
- Updates meters
- Adjusts output status (tower lights, button lamps) via “reprocess output status” helpers

This subsystem is event-driven and tightly integrated with the main state model (`EGMStatus`).

---

## Concurrency / timing model (how work is scheduled)

The code uses:

- `System.Timers.Timer` for periodic work (e.g., persistence loop, keep-alives)
- `Task.Run(...)` for background processing (e.g., bill acceptor listening loop, WebSocket-triggered spins)
- `lock(...)` to enforce critical sections:
  - `PersistAllData(...)` is guarded by a lock and temporarily stops the persister timer
  - WebSocket spin processing is guarded by `_spinLock` and `_isProcessingSpin`

Implications:

- The engine relies on shared mutable singleton state, so **thread safety matters**.
- A lot of event handlers run on thread-pool threads (timers/tasks), not a UI thread.

---

## Access levels (public/internal) & API surface

The project intentionally limits its public API:

- Many core classes are `internal` (e.g., `EGM`, `EGMStatus`, `EGMSettings`, `EGMAccounting`, `SASCTL`, `EGMDataPersisterCTL`).
- The primary “public API” is through:
  - `GUI/GameGUIController.cs` (`public class GameGUIController`)
  - `GUI/MenuGUIController.cs` (`public class MenuGUIController`)

This is consistent with the idea that:

- External consumers (front-end) should only interact via controllers (façades).
- Everything else is “engine internals”.

---

## Design patterns used (and where)

This codebase is dominated by a few patterns repeatedly.

### Singleton

Used widely for “global subsystems/models”:

- `EGM.GetInstance()` (engine orchestrator; `EGM` is `internal partial class`)
- `EGMStatus.GetInstance()` (runtime state)
- `EGMSettings.GetInstance()` (configuration)
- `EGMAccounting.GetInstance()` (meters/history)
- `EGMDataPersisterCTL.GetInstance()` (persistence controller)
- `SASCTL.GetInstance()` (SAS controller)
- `GameGUIController.GetInstance()`, `MenuGUIController.GetInstance()` (UI façades)

This is the core architectural decision: a “global kernel + global state”.

### Observer / publish–subscribe (events)

Used for:

- SAS long-poll events: `ISASClient` → `SASCTL` → `EGM`
- Bill acceptor events: `IBillAccCTL` → `EGM`
- UI notifications: EGM “priority UI events” are queued in `MenuGUIController` (`UIPriorityEvent`)

### State machine

Used for consistent transitions in multiple subsystems:

- Bill acceptor: `BillAccStateMachine`
- Front-end play: `FrontendPlayStatusMachine`, `FrontendPlayPennyStatusMachine`
- Collect/Handpay/Jackpot status machines (in `EGMStatus/*`)
- AFT transfer status (`AFTTransfer` under `EGMStatus/AFTTransfer/`)

### Strategy (environment-dependent implementations)

Selected at runtime based on flags like `unitydevelopment`:

- SAS client: `GanlotSASClient` vs `DevSASClient`
- GPIO controller: `GanlotGPIOCTL` vs `VirtualGPIOCTL`
- Bill acceptor: `SSPBillAccServiceCTL` vs `VirtualBillAccCTL`

### Adapter / Bridge (to native code and external services)

- SAS driver uses bridge DLLs (`GXGSASAPIBridge`, etc) and wraps them in `GanlotSASClient`.
- Bill acceptor uses an out-of-process Windows service controlled by named pipes.

### Facade

- `MenuGUIController` and `GameGUIController` are facades over the internal engine API.

### Snapshot / “dirty checking”

Both `EGMSettings` and `EGMStatus` keep “old” instances (`GetOldInstance`) and the persister compares current state vs snapshot to decide whether to write (and/or can be forced).

### Command (message-based commands)

- Bill acceptor service protocol uses string commands like `"SrvGetVersion"`, `"SrvSync ..."` over named pipes.

### “State synchronization” pattern (credits/meters)

There is an explicit helper strategy in `EGMAux.cs`:

- When credits change (via `AddAmount(...)`), the engine updates:
  - `EGMStatus` (internal totals)
  - `SASCTL` internal cached totals (`UpdateSASInfo(...)`)
  - SAS credits via `SASCTL.SetCredits(...)`
  - Accounting meters via `UpdateMetersByEvent(...)`

This is effectively a “synchronization routine” to keep multiple representations of the same domain concept aligned (status, SAS, accounting).

---

## “Where do I start reading?” (recommended entry points)

If you want to understand the system end-to-end, open files in this order:

1) `EGMENGINE/EGM.cs`
   - Look for constructor/init: it wires controllers, loads persisted state, starts timers, subscribes to SAS/bill acceptor/GPIO events, and initializes WebSocket.
   - Also note the environment detection flags (`unitydevelopment`, `localganlot`) that change which implementations are used.
2) `EGMENGINE/GUI/MenuGUIController.cs`
   - Shows how menu calls are mapped into `EGM.Menu_*` methods and how access control works.
3) `EGMENGINE/GUI/GameGUIController.cs`
   - Shows how game UI events become engine actions (`Game_SlotCreatePlay`, animations, etc).
   - Then follow into `EGMENGINE/EGMPlaySpin.cs` to see how a spin is generated and persisted.
4) `EGMENGINE/SASCTL/SASCTL.cs` + `EGMENGINE/SASCTL/SASClient/*`
   - Shows how host events (LPs) drive the engine and how the engine sends responses/exceptions/credits back.
5) `EGMENGINE/EGMDataPersister/EGMDataPersisterCTL.cs`
   - Shows exactly what is persisted, where, and under what conditions.

---

## Glossary

- **EGM**: Electronic Gaming Machine (the whole runtime engine)
- **SAS**: Slot Accounting System / Slot Accounting protocol (host link)
- **AFT**: Advanced Funds Transfer (cashless transfer flows between host and EGM)
- **Tilt**: A condition that makes the machine unplayable (e.g., door open, comm loss)

