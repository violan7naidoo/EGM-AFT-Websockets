# EGMENGINE WebSocket Architecture

This document describes **only** the WebSocket layer in the EGMENGINE project: how the EGM acts as a **WebSocket client**, which events it sends and receives, and how it correlates with the WebSocket adapter server.

---

## 1. Overview

- **Role of the EGM:** The EGM is a **WebSocket client**. It connects outbound to a single server and uses the connection to:
  - **Send** cash/credit events (bills, AFT, spin completed) so a remote game or adapter can stay in sync.
  - **Receive** game-round results (`GAME_UPDATE`) so the EGM can drive a spin and update credits without the player pressing the spin button on the machine.
- **Endpoint:** Hard-coded in code as `ws://localhost:5000/ws`.
- **Library:** `WebSocketSharp` (NuGet: `websocketsharp.core`). The EGM uses `WebSocketSharp.WebSocket` as a **client** (no server socket in the EGM).
- **Lifecycle:** The WebSocket is created and connected once during EGM construction (`InitializeWebSocket`), right after persistence and hardware init. There is no automatic reconnect; if the connection drops, it stays down until the EGM process is restarted (unless you add reconnect logic elsewhere).

---

## 2. Where the code lives

All WebSocket logic is in **one place**: the EGM singleton.

| File        | What it contains |
|------------|-------------------|
| `EGMENGINE/EGM.cs` | `InitializeWebSocket`, `SendToWebSocket`, `ProcessWebSocketMessageIn`, `SendBillAcceptedToWebSocket`, `SendAFTWebSocket`, `SendSpinCompletionMessage`, `TriggerSpinFromWebSocket`, `TestWebSocketConnection`, `CheckWebSocketStatus`, `TestWebSocket`, plus AFT retry timer and `HandleTransferConfirmation` (used with WebSocket AFT flow). |

There is **no** separate “WebSocket service” or “adapter” class; everything is inline in the EGM.

---

## 3. Connection and lifecycle

### 3.1 Initialization

- **When:** In the EGM protected constructor, after GPIO and SAS init, before starting the persistence timer.
- **Call:** `InitializeWebSocket("ws://localhost:5000/ws");` then `TestWebSocketConnection();`.
- **What it does:**
  - Creates a single `WebSocketSharp.WebSocket` instance and stores it in a private field `webSocket`.
  - Subscribes:
    - `OnOpen` → sets `isConnected = true`, logs.
    - `OnMessage` → logs the raw message and calls `ProcessWebSocketMessageIn(e.Data)`.
    - `OnError` → sets `isConnected = false`, logs.
  - Calls `webSocket.Connect()` (no async/await; connection is fire-and-forget from the constructor).

### 3.2 Sending

- **Single entry point:** All sends go through `SendToWebSocket(object update)`.
  - Serializes `update` to JSON with `Newtonsoft.Json.JsonConvert.SerializeObject(update)`.
  - Sends only if `webSocket?.IsAlive == true`; otherwise logs “WebSocket not connected, message not sent”.
  - Logs connection status and the JSON (for debugging).

### 3.3 Receiving

- **Single handler:** `ProcessWebSocketMessageIn(string message)`.
  - Parses JSON with `JObject.Parse(message)`.
  - Reads event type from `data["EventType"]` (and in one place `eventType` for the test message).
  - Dispatches by string (e.g. `"GAME_UPDATE"`, `"CREDIT_UPDATE"`, `"AFT_CONFIRMED"`, etc.). Unknown types are logged only.

### 3.4 No reconnect

- If the socket closes or errors, `isConnected` is set to false and no automatic reconnection is performed in the code shown. The EGM continues running; further sends are no-ops until the process is restarted and the constructor runs again.

---

## 4. Events: EGM → Server (outbound)

The EGM sends the following event types. All go to the same WebSocket server.

| Event               | When sent | Payload (from code) |
|---------------------|-----------|----------------------|
| **CONNECTION_TEST** | Right after connect (`TestWebSocketConnection`) | `eventType`, `message`, `timestamp`, `client: "EGM_Application"` |
| **BILL_INSERTED**   | When a bill is accepted (bill acceptor handler) | `EventType`, `amount`, `CurrentCredits`, `timestamp`, `currency: "ZAR"` |
| **AFT_DEPOSIT**     | When an AFT transfer completes as a deposit (SAS AFT completed handler) | `EventType`, `Amount`, `CurrentCredits`, `Timestamp`, `AFTReference`, `Currency: "ZAR"` |
| **AFT_CASHOUT**     | When an AFT transfer completes as a cashout (same handler, `isCashout == true`) | Same shape as AFT_DEPOSIT |
| **SPIN_COMPLETED**  | After the EGM has finished processing a spin triggered by `GAME_UPDATE` | `EventType`, `BetAmount`, `WinAmount`, `CurrentCredits`, `Timestamp`, `Status: "SUCCESS"` |
| **TEST** (optional) | When `TestWebSocket()` is called manually | `eventType: "TEST"`, `message`, `timestamp`, `client: "EGM_Application"` |

- **BILL_INSERTED:** Sent from the bill-acceptor subscription in `InitBillAcceptorController`; implemented in `SendBillAcceptedToWebSocket(decimal amount)`.
- **AFT_***: Sent from the `AFTTransferCompleted` handler (SAS); implemented in `SendAFTWebSocket(...)`. A retry timer can resend the same AFT message until confirmation (see AFT confirmation below).
- **SPIN_COMPLETED:** Sent at the end of `TriggerSpinFromWebSocket` via `SendSpinCompletionMessage`.

---

## 5. Events: Server → EGM (inbound)

The EGM handles these event types when received on the WebSocket:

| Event             | EGM behavior |
|-------------------|---------------|
| **GAME_UPDATE**   | Requires `BetAmount` and `WinAmount` (integers). Starts a background task `TriggerSpinFromWebSocket(betAmount, winAmount)` so the WebSocket thread is not blocked. That task: checks play state (must be `Playable` or recovers from `WinningState`), calls `GameGUIController.GUI_SpinButtonPressed(winAmount, betAmount)`, runs animation steps, then sends **SPIN_COMPLETED** back. Guarded by `_spinLock` and `_isProcessingSpin` to avoid overlapping spins. |
| **CREDIT_UPDATE**  | If `CurrentCredits` is present, logs it. No change to EGM credits. |
| **AFT_CONFIRMED**  | Logs “Processing transfer confirmation”. `HandleTransferConfirmation(true)` is commented out, so AFT confirmation is not applied in code today. |
| **TEST_RESPONSE**  | Logged. |
| **CONNECTION_TEST_RESPONSE** | Logged. |
| **ERROR**          | Logs `data["errorMessage"]`. |
| (unknown)         | Logged as “unknown message type”. |

---

## 6. Concurrency and thread safety

- **Send:** `SendToWebSocket` is not locked; it is called from multiple places (bill acceptor callback, SAS AFT callback, spin completion, test methods). The WebSocketSharp client may or may not serialize sends internally; the EGM does not add another lock.
- **Receive:** `ProcessWebSocketMessageIn` runs on the WebSocket library’s callback thread. Only **GAME_UPDATE** does real work, and that is offloaded with `Task.Run(() => TriggerSpinFromWebSocket(...))` so the callback returns quickly. Spin execution is serialized by `_spinLock` and `_isProcessingSpin`.
- **AFT retry:** A timer (`_websocketRetryTimer`) can resend AFT messages while `_waitingForConfirmation` and `_pendingTransferData` are set. When the server sends **AFT_CONFIRMED**, the intended flow is to call `HandleTransferConfirmation(true)` (currently commented out).

---

## 7. Logging

- A static `Logger` class in `EGM.cs` writes to a file on the **Desktop**: `egm_websocket_log.txt`.
- WebSocket-related log lines include: connection status, “WebSocket sent”, “Received WebSocket message”, “Processing WebSocket message”, and spin/error messages. This is the main place to debug WebSocket behavior from the EGM side.

---

## 8. Correlation with the WebSocket adapter server

The document **`C:\Users\Violan Work\source\repos\WebSocketServer-incoming-master\WebSocketServer\WEBSOCKET_SERVER.md`** describes a **WebSocket adapter server** that:

- Listens on **the same endpoint**: `ws://localhost:5000/ws`.
- Treats one client as **EGM** and another as **Roulette** (game UI), and routes/translates messages between them.

**Conclusion: the EGMENGINE WebSocket client is built to work with that server.** It is the “EGM” client in the server’s flow.

### 8.1 How they match

| Server doc (WEBSOCKET_SERVER.md) | EGMENGINE (this project) |
|----------------------------------|----------------------------|
| **Endpoint** `ws://localhost:5000/ws` | Same URL hard-coded in `InitializeWebSocket`. |
| **EGM → Server** sends `BILL_INSERTED` (amount, current credits) | EGM sends `BILL_INSERTED` with `amount`, `CurrentCredits`, `timestamp`, `currency: "ZAR"`. |
| **EGM → Server** sends `AFT_DEPOSIT` / `AFT_CASHOUT` | EGM sends `AFT_DEPOSIT` or `AFT_CASHOUT` with `Amount`, `CurrentCredits`, `Timestamp`, `AFTReference`, `Currency`. |
| **EGM → Server** sends `SPIN_COMPLETED` | EGM sends `SPIN_COMPLETED` with `BetAmount`, `WinAmount`, `CurrentCredits`, `Timestamp`, `Status`. |
| **EGM → Server** sends `CONNECTION_TEST` or `session_initialized` with `client: "EGM_Application"` to identify EGM | EGM sends `CONNECTION_TEST` with `client: "EGM_Application"` on startup; server can use this to tag the socket as EGM. |
| **Server → EGM** sends **only** `GAME_UPDATE` (BetAmount, WinAmount, etc.) after Roulette sends `round_result` | EGM only reacts to `GAME_UPDATE` by running a spin with `BetAmount` and `WinAmount`. |

So the **event names and direction (EGM → server cash/credit events, server → EGM game result)** align. The EGM is the machine/backend that the server document calls “EGM”; the Roulette is a separate client that talks to the same server.

### 8.2 Minor differences / notes

- **Property names:** The server doc mentions the server may read `EventType`, `eventType`, or `event`. The EGM consistently uses `EventType` for the main events and `eventType` for the connection test. So the server must accept at least `EventType` and/or `eventType` for EGM-originated messages.
- **Optional fields:** The server doc sometimes refers to `egmId` in EGM messages. The EGM code does not set `egmId` in the payloads we saw; if the server expects it, it may need to be added or the server may use a default.
- **AFT_CONFIRMED:** The server doc says Roulette can send `AFT_CONFIRMED` (stored only on server). The EGM is prepared to receive `AFT_CONFIRMED` and would call `HandleTransferConfirmation(true)` if that line is uncommented. So the protocol is in place; the EGM just does not currently apply the confirmation.
- **ui_ping / UI_PING:** The server doc lists `ui_ping` / `UI_PING` as EGM → server keep-alive. The EGM code does not send `ui_ping` in the snippets we have; it only sends `CONNECTION_TEST` at startup. So either keep-alive is not implemented in the EGM yet, or it is sent from somewhere else.

---

## 9. Summary diagram (EGM side only)

```
  ┌─────────────────────────────────────────────────────────────────┐
  │                        EGMENGINE (this repo)                      │
  │                                                                   │
  │  Bill acceptor ──► SendBillAcceptedToWebSocket ──┐                │
  │  SAS AFT completed ──► SendAFTWebSocket ─────────┼──► SendToWebSocket ──► ws://localhost:5000/ws
  │  Spin done (from GAME_UPDATE) ──► SendSpinCompletionMessage ──────┤                │
  │  Startup / Test ──► CONNECTION_TEST, TEST ──────────────────────┘                │
  │                                                                   │                 │
  │  ProcessWebSocketMessageIn ◄── OnMessage ◄─────────────────────────┘                │
  │       │                                                                             │
  │       ├─ GAME_UPDATE ──► TriggerSpinFromWebSocket(bet, win) ──► GameGUIController   │
  │       ├─ CREDIT_UPDATE, AFT_CONFIRMED, ERROR, etc. ──► log only (or future use)     │
  └─────────────────────────────────────────────────────────────────┘
```

When the adapter server is running, it sits at `ws://localhost:5000/ws` and connects the EGM (this client) to the Roulette client as described in **WEBSOCKET_SERVER.md**.
