using EGMENGINE.BillAccCTLModule;
using EGMENGINE.BillAccCTLModule.BillAccTypes;
using EGMENGINE.BillAccCTLModule.Impl.SSPBillAccCTL;
using EGMENGINE.BillAccCTLModule.Impl.SSPBillAccServiceCTL;
using EGMENGINE.BillAccCTLModule.Impl.SSPBillAccServiceCTL.MessageTypes;
using EGMENGINE.BillAccCTLModule.Impl.VirtualBillAccCTL;
using EGMENGINE.EGMAccountingModule;
using EGMENGINE.EGMDataPersisterModule;
using EGMENGINE.EGMPlayModule.Impl.Slot;
using EGMENGINE.EGMSettingsModule;
using EGMENGINE.EGMSettingsModule.EGMSASConfig;
using EGMENGINE.EGMStatusModule;
using EGMENGINE.EGMStatusModule.CollectModule;
using EGMENGINE.EGMStatusModule.HandPayModule;
using EGMENGINE.EGMStatusModule.JackPotModule;
using EGMENGINE.GLOBALTYPES;
using EGMENGINE.GPIOCTL;
using EGMENGINE.GPIOCTL.Impl.GanlotGPIOCTL;
using EGMENGINE.GPIOCTL.Impl.VirtualGPIOCTL;
using EGMENGINE.GUI;
using EGMENGINE.GUI.GAMETYPES;
using EGMENGINE.GUI.MENUTYPES;
using EGMENGINE.IntegrityControlModule;
using EGMENGINE.SASCTLModule;
using Newtonsoft.Json.Linq;
using SlotMathCore;
using SlotMathCore.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using WebSocketSharp;
using static EGMENGINE.GUI.MenuGUIController;
using Timer = System.Timers.Timer;
using System.Diagnostics;

namespace EGMENGINE
{
    public static class Logger
    {
        private static readonly string logFilePath;
        private static readonly object lockObject = new object();

        static Logger()
        {
            // location: Desktop 
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            logFilePath = Path.Combine(desktopPath, "egm_websocket_log.txt");

            // initial message to confirm file creation
            EnsureFileExists();
        }

        private static void EnsureFileExists()
        {
            try
            {
                if (!File.Exists(logFilePath))
                {
                    File.WriteAllText(logFilePath, $"Log file created at: {DateTime.Now}{Environment.NewLine}");
                    Console.WriteLine($"Log file created at: {logFilePath}"); // Also show in console
                }
                else
                {
                    Console.WriteLine($"Log file already exists at: {logFilePath}"); // Also show in console
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create log file: {ex.Message}"); // Show in console
                                                                               // Try fallback to temp directory
                TryFallbackLogging($"Failed to create log file: {ex.Message}");
            }
        }

        private static void TryFallbackLogging(string errorMessage)
        {
            // Try to create a file in temp directory as fallback
            try
            {
                string tempFile = Path.Combine(Path.GetTempPath(), "egm_websocket_error.txt");
                File.AppendAllText(tempFile, $"{DateTime.Now}: {errorMessage}{Environment.NewLine}");
                Console.WriteLine($"Fallback log created at: {tempFile}"); // Show in console
            }
            catch (Exception fallbackEx)
            {
                Console.WriteLine($"Fallback logging also failed: {fallbackEx.Message}"); // Show in console
            }
        }

        public static void Log(string message)
        {
            lock (lockObject)
            {
                try
                {
                    string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}{Environment.NewLine}";
                    File.AppendAllText(logFilePath, logMessage);
                    Console.WriteLine($"LOG: {message}"); // Also output to console for debugging
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Log failed: {ex.Message}. Original message: {message}"); // Show in console
                    TryFallbackLogging($"Log failed: {ex.Message}. Original message: {message}");
                }
            }
        }

        public static string GetLogFilePath()
        {
            return logFilePath;
        }
    }




    /// <summary>
    /// Class EGM definition. Singleton
    /// </summary>
    internal partial class EGM
    {

        public void ForceLogCreation()
        {
            string logPath = Logger.GetLogFilePath();

            // Try to create a test message
            Logger.Log("=== EGM APPLICATION STARTED ===");
            Logger.Log($"Log file path: {logPath}");
            Logger.Log($"Current directory: {AppDomain.CurrentDomain.BaseDirectory}");
            Logger.Log($"Desktop path: {Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}");

            // Check if file exists and is writable
            try
            {
                if (File.Exists(logPath))
                {
                    Logger.Log("Log file exists and is writable");
                }
                else
                {
                    Logger.Log("Log file does not exist - attempting to create");
                    File.WriteAllText(logPath, "Test creation message");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"File access error: {ex.Message}");
            }
        }

        public string GetLogStatus()
        {
            string logPath = Logger.GetLogFilePath();
            string status = $"Log path: {logPath}\n";

            try
            {
                if (File.Exists(logPath))
                {
                    status += $"File exists: Yes\n";
                    status += $"File size: {new FileInfo(logPath).Length} bytes\n";
                    status += $"Last write: {File.GetLastWriteTime(logPath)}\n";

                    // Test writing
                    File.AppendAllText(logPath, $"Test write at: {DateTime.Now}{Environment.NewLine}");
                    status += "File is writable: Yes\n";
                }
                else
                {
                    status += $"File exists: No\n";

                    // Try to create it
                    try
                    {
                        File.WriteAllText(logPath, $"Created at: {DateTime.Now}{Environment.NewLine}");
                        status += "File created successfully\n";
                    }
                    catch (Exception ex)
                    {
                        status += $"Create failed: {ex.Message}\n";
                    }
                }
            }
            catch (Exception ex)
            {
                status += $"Error checking file: {ex.Message}\n";
            }

            return status;
        }
        /// <summary>
        /// SET TO TRUE IF IT IS USED FOR UNITY DEVELOPMENT
        /// SET TO FALSE IF IT WILL BE USED ON A GANLOT ENVIRONMENT
        /// </summary>
        internal bool unitydevelopment = false;

        /// <summary>
        /// SET TO TRUE IF IT IS USED ON LOCAL GANLOT
        /// SET TO FALSE IF IT WILL BE USED ON ANOTHER GANLOT ENVIRONMENT
        /// </summary>
        internal bool localganlot = false;

        private static EGM egm_;
        // Math Controller
        Controller math_ctrller = new Controller();
        // BIll Acceptor property
        internal IBillAccCTL billAcc;
        // GPIO Controller Property
        internal IGPIOCTL gpioCTL;
        //Websocket connection status
        private bool isConnected = false;
        private WebSocketSharp.WebSocket webSocket;
        private readonly object spinLock = new object();
        private int currentBet = 0;  // Default values
        private int currentWin = 0;  // Default values

        /// jackpot amount
        private decimal jackpot_amount = 0;

        private string lastinfo = "";
        // Persist status
        internal bool persiststatus = false;

        internal bool criticaltilt = false;

        private static readonly object locker = new object(); // Para sincronizar el acceso a la variable

        // 
        // UI Priority Event
        public event UIPriorityEventHandler UIPriorityEvent;
        // Collect process
        Timer collect_wait_72 = new Timer(5000);
        Timer persister = new System.Timers.Timer(100);
        Timer billacceptormaxlaod = new System.Timers.Timer(1000);

        [DllImport("kernel32.dll", EntryPoint = "SetSystemTime", SetLastError = true)]
        internal extern static bool Win32SetSystemTime(ref SystemTime sysTime);
        /// <summary>
        /// Singleton Method. Get the unique instance of EGM
        /// </summary>
        /// <returns></returns>
        public static EGM GetInstance()
        {
            if (egm_ == null)
            {
                egm_ = new EGM(); // Instantiate a new EGM if the singleton is used for the first time
                //
            }
            return egm_;
        }

        #region EGM INITIALIZING
        /// <summary>
        /// EGM Constructor
        /// </summary>
        protected EGM()
        {
            // Check if it is Unity development
            if (File.Exists("Assets/EGMEngine.dll"))
            {
                unitydevelopment = true;
            }

            if (File.Exists(Environment.ExpandEnvironmentVariables($"%USERPROFILE%\\Desktop\\LocalGanlot186200034154.txt")))
            {
                localganlot = true;
            }

            // Init EGM Data Persister
            string databaseMSG = EGMDataPersisterCTL.GetInstance().InitEGMDataPersister(unitydevelopment, localganlot);
            // Read EGM Settings into EGMSettings singleton
            EGMDataPersisterCTL.GetInstance().ReadEGMSettings();
            // Read EGM Accounting into EGMAccounting singleton
            EGMDataPersisterCTL.GetInstance().ReadEGMAccounting();
            // Init SASController
            InitSASController();
            // Init SSPBillAcceptor
            InitBillAcceptorController();
            // Init EGMStatus
            InitEGMStatus();
            // Read EGM Status into EGMStatus singleton
            if (databaseMSG == "OK")
                EGMDataPersisterCTL.GetInstance().ReadEGMStatus();
            else
            {
                EGMStatus.GetInstance().fullramclearperformed = false;
                AddTilt(databaseMSG, true);
            }

            // Init the SASCTL with several info from EGMStatus and EGMSettings
            SASCTL.GetInstance().Init(EGMSettings.GetInstance().sasSettings.mainConfiguration.SASReportedDenomination,
                                             EGMSettings.GetInstance().creditLimit,
                                             EGMStatus.GetInstance().currentCashableAmount,
                                             EGMStatus.GetInstance().currentRestrictedAmount,
                                             EGMStatus.GetInstance().currentNonRestrictedAmount, EGMSettings.GetInstance().sasSettings.mainConfiguration.InHouseInLimit,
                                             EGMSettings.GetInstance().sasSettings.mainConfiguration.InHouseOutLimit, EGMSettings.GetInstance().sasSettings.mainConfiguration.SASEnabled);


            // Send Power Lost
            AddSystemLog("Power Off", "-");
            SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.ACPowerLostFromGamingMachine);

            // Init GPIOController
            InitGPIOController();

            // Update meters by event
            UpdateMetersByEvent(Meters_EventName.EGMInitialization);
            // Init Math Controller
            math_ctrller.LoadModel();
            /********************** REPROCESS ****************************/
            ReprocessEGMStatus_Output(() => { });
            /*************************************************************/
            /********************** SAS CTL ******************************/
            /*************************************************************/


            // Set the selected denomination code
            SASCTL.GetInstance().SetCurrentPlayerDenomination(0x06);

            ForceLogCreation();

            /*************************************************************/
            /********************** WebSockets ******************************/
            /*************************************************************/
            InitializeWebSocket("ws://localhost:5000/ws");
            SendSessionInitialized();

            Logger.Log("EGM initialization completed");


            /* Enable / Disable denominations */
            foreach (Denomination denomination in EGMSettings.GetInstance().Denominations.GetDisabledDenominations())
            {
                // Disable
                SASCTL.GetInstance().DisableDenomination(0x00, denomination.Code);
            }

            foreach (Denomination denomination in EGMSettings.GetInstance().Denominations.GetEnabledDenominations())
            {
                // Enable
                SASCTL.GetInstance().EnableDenomination(0x00, denomination.Code, 9999);
            }

            // Send Power On
            AddSystemLog("Power On", "-");
            SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.ACPowerAppliedToGamingMachine);
            /* TO BE DELETED UNTIL WE PERSIST TILTS */
            if (EGMStatus.GetInstance().maintenanceMode)
            {
                EnterMaintenanceMode();
                SASCTL.GetInstance().SetMaintenanceMode(true);
            }
            else
            {
                ExitMaintenanceMode();
                SASCTL.GetInstance().SetMaintenanceMode(false);

            }

            // Update SAS Credits
            SASCTL.GetInstance().SetCredits((byte)SAS_CREDIT_TYPE.SAS_CREDIT_CASHABLE, EGMStatus.GetInstance().currentCashableAmount);
            SASCTL.GetInstance().SetCredits((byte)SAS_CREDIT_TYPE.SAS_CREDIT_RESTRICTED, EGMStatus.GetInstance().currentRestrictedAmount);
            SASCTL.GetInstance().SetCredits((byte)SAS_CREDIT_TYPE.SAS_CREDIT_NONRESTRICTED, EGMStatus.GetInstance().currentNonRestrictedAmount);

            // Set SAS Address
            LinkEGMSettingsSASIdToSASAddress();

            // Init the asset number to SASCTL
            SASCTL.GetInstance().SetAssetNumber(EGMSettings.GetInstance().sasSettings.mainConfiguration.AssetNumber);
            // Init the serial number to SASCTL
            SASCTL.GetInstance().SetSerialNumber(EGMSettings.GetInstance().sasSettings.mainConfiguration.SerialNumber);
            // If there is no tilts, set as busy = false
            if (EGMStatus.GetInstance().currentTilts.Count() == 0)
            {
                SASCTL.GetInstance().SetSASBusy(false);
            }

            if (EGMStatus.GetInstance().disabledByHost)
            {
                SASCTL.GetInstance().RejectTransfer(true, true);
            }
            else
            {
                SASCTL.GetInstance().AcceptTransfer(true, true);

            }
            // Set SAS Game Details
            UpdateSASGameDetails();

            // Start Integrity checker
            if (databaseMSG == "OK" && IntegrityController.GetInstance().RAMERROR)
                AddTilt("RAM INTEGRITY ERROR");

            if (databaseMSG == "OK" && !EGMSettings.GetInstance().sasSettings.SASConfigured)
            {
                AddTilt("Post ram clear configuration pending");
            }
            foreach (byte code in EGMAccounting.GetInstance().MeterCodes)
            {
                int value = EGMAccounting.GetInstance().GetMeter(code);
                SASCTL.GetInstance().SetMeter("", code, value, false);
            }
            //EGMUpdateMeter((byte)SASMeter.TotalGames, EGMAccounting.GetInstance().plays.GetLastPlays().ToList().Count()); // Total Games
            //EGMUpdateMeter((byte)SASMeter.GamesWon, EGMAccounting.GetInstance().plays.GetLastPlays().ToList().Where(l => l.CreditsWon > 0).Count()); // Total Won Games
            //EGMUpdateMeter((byte)SASMeter.GamesLost, EGMAccounting.GetInstance().plays.GetLastPlays().ToList().Where(l => l.CreditsWon == 0).Count()); // Total Lost Games


            persister = new Timer(50);
            persister.Elapsed += new ElapsedEventHandler((o, t) =>
            {
                ReprocessEGMStatus_Frontend();
                PersistAllData(false, false);

            });
            persister.Start();

            if (EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.Playable || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.WaitingForBet || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.WaitingForCredits)
            {
                billAcc.EnableBillAcceptor();
                SASCTL.GetInstance().AcceptTransfer(true, true);
            }
            else
            {
                billAcc.DisableBillAcceptor();
                SASCTL.GetInstance().RejectTransfer(true, true);
            }

            //if (EGMStatus.GetInstance().current_play.baseWinning)
            //{
            //    Game_AnimationUpdate(AnimationEvent.WinAnimationFinished);
            //}
            //if (EGMStatus.GetInstance().current_play.scatter_win)
            //{
            //    Game_AnimationUpdate(AnimationEvent.ScatterWinAnimationFinished);
            //}
        }

        /// <summary>
        /// Sends session_initialized to the WebSocket server once per engine run to initialize the session with current credits.
        /// </summary>
        public void SendSessionInitialized()
        {
            Logger.Log("=== Sending session_initialized to WebSocket ===");
            CheckWebSocketStatus();

            if (_sessionInitializedSent)
            {
                Logger.Log("Session already initialized this run - skipping (sends only once until engine restart).");
                return;
            }

            if (webSocket?.IsAlive == true)
            {
                decimal credits = EGM_GetCurrentCredits();
                var message = new
                {
                    eventType = "session_initialized",
                    client = "EGM_Application",
                    timestamp = DateTime.UtcNow,
                    payload = new { availableCredits = credits }
                };

                SendToWebSocket(message);
                _sessionInitializedSent = true;
                Logger.Log($"session_initialized sent once (availableCredits: {credits}). Will not send again until engine restarts.");
            }
            else
            {
                Logger.Log("WebSocket not connected - cannot send session_initialized");
            }
        }

        private void InitializeWebSocket(string url)
        {
            try
            {
                Logger.Log($"Initializing WebSocket to: {url}");
                webSocket = new WebSocketSharp.WebSocket(url);

                webSocket.OnOpen += (s, e) =>
                {
                    isConnected = true;
                    Logger.Log("WebSocket connected");
                };

                webSocket.OnMessage += (s, e) =>
                {
                    Logger.Log($"Received WebSocket message: {e.Data}");
                    ProcessWebSocketMessageIn(e.Data);

                };

                webSocket.OnError += (s, e) =>
                {
                    isConnected = false;
                    Logger.Log($"WebSocket error: {e.Message}");
                };

                webSocket.Connect();
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to initialize WebSocket: {ex.Message}");
            }
        }



        private void ProcessWebSocketMessageIn(string message)
        {
            try
            {
                Logger.Log($"Processing WebSocket message: {message}");
                var data = JObject.Parse(message);

                // Handle GAME_UPDATE - Trigger spin directly in engine
                if (data["EventType"]?.ToString() == "GAME_UPDATE" &&
                    data["BetAmount"] != null &&
                    data["WinAmount"] != null)
                {
                    int betAmount = data["BetAmount"].Value<int>();
                    int winAmount = data["WinAmount"].Value<int>();

                    Logger.Log($"Received GAME_UPDATE - Bet: {betAmount}, Win: {winAmount}");

                    // Process the spin on a separate thread to avoid blocking WebSocket
                    Task.Run(() => TriggerSpinFromWebSocket(betAmount, winAmount));
                }
                else if (data["EventType"]?.ToString() == "CREDIT_UPDATE")
                {
                    // Handle credit updates if needed
                    if (data["CurrentCredits"] != null)
                    {
                        decimal credits = data["CurrentCredits"].Value<decimal>();
                        Logger.Log($"Credit update received: {credits}");
                    }
                }
                else if (data["EventType"]?.ToString() == "AFT_CONFIRMED")
                {
                    Logger.Log("Received AFT_CONFIRMED - Processing transfer confirmation");
                    //HandleTransferConfirmation(true);
                }


                else if (data["EventType"]?.ToString() == "TEST_RESPONSE")
                {
                    Logger.Log($"Received TEST_RESPONSE: {data["message"]}");
                }
                else if (data["EventType"]?.ToString() == "CONNECTION_TEST_RESPONSE")
                {
                    Logger.Log($"Received CONNECTION_TEST_RESPONSE: {data["message"]}");
                }
                else if (data["EventType"]?.ToString() == "ERROR")
                {
                    Logger.Log($"Received ERROR: {data["errorMessage"]}");
                }
                else
                {
                    Logger.Log($"Received unknown message type: {message}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error processing message: {ex.Message}");
            }
        }
        private readonly object _spinLock = new object();

        private bool _isProcessingSpin = false;
        public event Action<decimal> CreditsUpdated;
        //public event Action<decimal> CreditsUpdated;
        private void TriggerSpinFromWebSocket(int betAmount, int winAmount)
        {
            lock (_spinLock)
            {
                if (_isProcessingSpin)
                {
                    Logger.Log("Spin already in progress, ignoring duplicate request");
                    return;
                }

                _isProcessingSpin = true;

                try
                {
                    Logger.Log($"Starting WebSocket spin process - Bet: {betAmount}, Win: {winAmount}");


                    if (EGMStatus.GetInstance().frontend_play.thisstatus != FrontEndPlayStatus.Playable)
                    {
                        Logger.Log($"Cannot spin - wrong state: {EGMStatus.GetInstance().frontend_play.thisstatus}");
                        if (EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.WinningState)
                        {
                            Logger.Log("Attempting to recover from stuck WinningState...");
                            GameGUIController.GetInstance().AnimationStatusUpdate(AnimationEvent.WinAnimationFinished);
                            Thread.Sleep(100);
                            if (EGMStatus.GetInstance().frontend_play.thisstatus != FrontEndPlayStatus.Playable)
                            {
                                Logger.Log("Recovery failed, state is still not Playable.");
                                _isProcessingSpin = false;
                                return;
                            }
                            Logger.Log("Recovery successful, state is now Playable. Proceeding with spin.");
                        }
                        else
                        {
                            _isProcessingSpin = false;
                            return;
                        }
                    }
                    // =========================================================================

                    decimal creditsBefore = EGM_GetCurrentCredits();
                    Logger.Log($"Credits before spin: {creditsBefore}");

                    Logger.Log("Calling GUI_SpinButtonPressed...");
                    var spinConfig = GameGUIController.GetInstance().GUI_SpinButtonPressed(winAmount, betAmount);
                    var currentState = GameGUIController.GetInstance().GUI_get_FrontEndPlayStatus();
                    Logger.Log($"State after GUI_SpinButtonPressed: {currentState}");

                    Logger.Log("Triggering ReelsSpinning animation...");
                    GameGUIController.GetInstance().AnimationStatusUpdate(AnimationEvent.ReelsSpinning);
                    Thread.Sleep(500);

                    Logger.Log("Triggering ReelsStoped animation...");
                    GameGUIController.GetInstance().AnimationStatusUpdate(AnimationEvent.ReelsStoped);


                    //prevents the state from getting stuck after a win.
                    if (winAmount > 0)
                    {
                        Thread.Sleep(200);
                        Logger.Log($"Win detected ({winAmount}). Triggering WinAnimationFinished to return to Playable state.");
                        GameGUIController.GetInstance().AnimationStatusUpdate(AnimationEvent.WinAnimationFinished);
                    }
                    // ===============================================================================

                    spinConfig = GameGUIController.GetInstance().GUI_GetLastPlay();
                    Logger.Log($"Last play retrieved - CreditsBefore: {spinConfig.slotplay.creditsBefore}, CreditsOnPlay: {spinConfig.slotplay.creditsOnPlay}");

                    decimal currentCredits = EGM_GetCurrentCredits();
                    Logger.Log($"Credits after spin: {currentCredits}");
                    CreditsUpdated?.Invoke(currentCredits);

                    currentState = GameGUIController.GetInstance().GUI_get_FrontEndPlayStatus();
                    Logger.Log($"Final state after processing: {currentState}");

                    Logger.Log("Waiting for spin completion...");
                    Thread.Sleep(10000);

                    SendSpinCompletionMessage(betAmount, winAmount, currentCredits);
                    Logger.Log("WebSocket spin completed successfully.");
                }
                catch (Exception ex)
                {
                    Logger.Log($"WebSocket spin failed: {ex.Message}");
                    EGMStatus.GetInstance().frontend_play.Transition(FrontEndPlayStatus.Playable);
                    EGMStatus.GetInstance().spinstatus.Transition(SpinRepresentationStatus.Idle);
                }
                finally
                {
                    _isProcessingSpin = false;
                }
            }
        }
        private void SendSpinCompletionMessage(int betAmount, int winAmount, decimal currentCredits)
        {
            var completionMessage = new
            {
                EventType = "SPIN_COMPLETED",
                BetAmount = betAmount,
                WinAmount = winAmount,
                CurrentCredits = currentCredits,
                Timestamp = DateTime.UtcNow,
                Status = "SUCCESS"
            };

            SendToWebSocket(completionMessage);
        }



        private void SendToWebSocket(object update)
        {
            try
            {
                Logger.Log($"WebSocket connection status: IsAlive={webSocket?.IsAlive}, ReadyState={webSocket?.ReadyState}");
                if (webSocket?.IsAlive == true)
                {
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(update);
                    Logger.Log($"Attempting to send: {json}");
                    webSocket.Send(json);
                    Logger.Log($"WebSocket sent: {json}");
                }
                else
                {
                    Logger.Log("WebSocket not connected, message not sent");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error sending message: {ex.Message}");
            }
        }
        void PersistAllData(bool slotplay, bool forcepersist)
        {
            lock (locker)
            {
                persister.Stop();

                if (slotplay || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.Playing || EGMStatus.GetInstance().frontend_play_penny.thisstatus == FrontEndPlayPennyStatus.Playing)
                {
                    EGMDataPersisterCTL.GetInstance().PersistEGMSlotPlay();
                    EGMStatus.GetInstance().current_play.TransformToExtraDataString();
                    //     EGMDataPersisterCTL.GetInstance().PersistAllData();

                }
                // Persist EGM Status
                EGMDataPersisterCTL.GetInstance().PersistEGMStatus(forcepersist);
                EGMDataPersisterCTL.GetInstance().PersistEGMAccounting(forcepersist);
                EGMDataPersisterCTL.GetInstance().PersistEGMSettings(forcepersist);

                persister.Start();
            }
        }
        /// <summary>
        /// Init EGM Status. Used to suscribe different events, from handpay and collect for example
        /// </summary>
        private void InitEGMStatus()
        {
            // Init current collect
            EGMStatus.GetInstance().current_collect = new Collect();
            // Init current handpay
            EGMStatus.GetInstance().current_handpay = new Handpay();
            // Init current jackpot
            EGMStatus.GetInstance().current_jackpot = new JackPot();
            EGMStatus.GetInstance().current_collect.TransitionExecuted += new Collect.TransactionExecutedEvent((s, not_onload, e) =>
            {
                if (s == CollectStatus.Waiting72)
                {
                    // Send exception CashButtonPressed
                    SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.CashoutButtonPressed);
                    // Start timer to wait the long poll 72
                    collect_wait_72.Start();
                    // Update Meters by event
                    UpdateMetersByEvent(Meters_EventName.CashoutPressed, new int[] { ToSASFormat(EGMStatus.GetInstance().currentCashableAmount + EGMStatus.GetInstance().currentNonRestrictedAmount) });
                    // Persist state
                    if (not_onload)
                        EGMDataPersisterCTL.GetInstance().PersistEGMStatus(true);
                }
                else if (s == CollectStatus.HandpayInProgress)
                {
                    // Cashable and non restricted
                    collect_wait_72.Stop();
                    handpay_cashable_amount = EGMStatus.GetInstance().currentCashableAmount;
                    handpay_nonrestricted_amount = EGMStatus.GetInstance().currentNonRestrictedAmount;
                    // Initiate Handpay
                    InitiateHandpay();
                    //// Persist state
                    if (not_onload)
                        EGMDataPersisterCTL.GetInstance().PersistEGMStatus(true);
                }
                else if (s == CollectStatus.AFTCollectInProgress)
                {
                    // TODO
                }
                else if (s == CollectStatus.Completed)
                {

                    // TRANSITION: Handpay from Completed -> Idle
                    EGMStatus.GetInstance().current_handpay.Transition(HandpayStatus.Idle);
                    // TRANSITION: Collect from Completed -> Idle
                    EGMStatus.GetInstance().current_collect.Transition(CollectStatus.Idle);
                    // Substract the amount
                    AddAmount(-handpay_cashable_amount, 0, -handpay_nonrestricted_amount, true);
                    // Persist state
                    if (not_onload)
                    {
                        // Persist
                        EGMDataPersisterCTL.GetInstance().PersistEGMAccounting(true);
                        // Persist state
                        EGMDataPersisterCTL.GetInstance().PersistEGMStatus(true);
                    }
                }
            });
            collect_wait_72.Elapsed += new ElapsedEventHandler((b, a) =>
            {
                // TRANSITION: Collect from Waiting72 -> HandpayInProgress
                EGMStatus.GetInstance().current_collect.Transition(CollectStatus.HandpayInProgress);
            });

            EGMStatus.GetInstance().current_handpay.TransitionExecuted += new Handpay.TransactionExecutedEvent((s, not_onload, e) =>
            {
                if (s == HandpayStatus.HandpayPending)
                {
                    // amount equals to currentcashablecredits and current non restricted credits
                    //decimal handpay_cashable_amount = EGMStatus.GetInstance().currentCashableCredits;
                    //decimal handpay_nonrestricted_amount = EGMStatus.GetInstance().currentNonRestrictedCredits;
                    // Amount is the current cashable credits
                    if (not_onload)
                        EGMStatus.GetInstance().current_handpay.Amount = handpay_cashable_amount + handpay_nonrestricted_amount;
                    // Handpay Ocurred
                    if (not_onload)
                        SASCTL.GetInstance().StartHandpay(handpay_cashable_amount + handpay_nonrestricted_amount, 0x00, 0, 0x00, 0x00, 0x00);
                    // Persist state
                    if (not_onload)
                        EGMDataPersisterCTL.GetInstance().PersistEGMStatus(true);
                }
                else if (s == HandpayStatus.HandpayResetByKey)
                {
                    // Set the reset mode
                    if (not_onload)
                        EGMStatus.GetInstance().current_handpay.ResetMode = "Key";
                    // Launch Handpay Reset exception
                    if (not_onload)
                        SASCTL.GetInstance().ResetHandpay(0x00, 0x00);
                    // Add current handpay to history
                    EGMAccounting.GetInstance().handpays.AddNewHandpay(new HandpayTransaction(EGMStatus.GetInstance().current_handpay.ResetMode,
                                                                       DateTime.Now,
                                                                       EGMStatus.GetInstance().current_handpay.Amount), true);
                    // Update Meters by event
                    UpdateMetersByEvent(Meters_EventName.HandpayReset, new int[] { ToSASFormat(EGMStatus.GetInstance().current_handpay.Amount) });
                    // TRANSITION: Handpay from HandpayResetByKey -> Idle
                    EGMStatus.GetInstance().current_handpay.Transition(HandpayStatus.Idle);
                    // TRANSITION: Collect from HandpayInProgress -> Completed
                    EGMStatus.GetInstance().current_collect.Transition(CollectStatus.Completed);
                    // Persist
                    if (not_onload)
                        EGMDataPersisterCTL.GetInstance().PersistEGMAccounting(true);
                    // Persist state
                    if (not_onload)
                        EGMDataPersisterCTL.GetInstance().PersistEGMStatus(true);
                }
            });

            EGMStatus.GetInstance().current_jackpot.TransitionExecuted += new JackPot.TransactionExecutedEvent((s, not_onload, e) =>
            {
                if (s == JackPotStatus.JackpotPending)
                {

                    // Amount is the jackpot amount
                    if (jackpot_amount != 0)
                        EGMStatus.GetInstance().current_jackpot.Amount = jackpot_amount;
                    // JackPot Ocurred
                    if (not_onload)
                        SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.JackpotOcurred);
                    // Persist state
                    if (not_onload)
                        EGMDataPersisterCTL.GetInstance().PersistEGMStatus(true);
                }
                else if (s == JackPotStatus.JackpotResetByKey)
                {
                    // Set the reset mode
                    EGMStatus.GetInstance().current_jackpot.ResetMode = "Key";
                    // Launch Jackpot Reset exception
                    if (not_onload)
                        SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.HandpayReset_DEPRECATED);
                    //// Add current jackpot to history
                    //EGMAccounting.GetInstance().jackpots.AddNewJackpot(new HandpayTransaction(EGMStatus.GetInstance().current_jackpot.ResetMode,
                    //                                                   DateTime.Now,
                    //                                                   EGMStatus.GetInstance().current_jackpot.Amount), true);
                    // Update Meters by event
                    UpdateMetersByEvent(Meters_EventName.JackpotOcurred, new int[] { ToSASFormat(EGMStatus.GetInstance().current_jackpot.Amount) });
                    // TRANSITION: Jackpot from JackpotResetByKey -> Idle
                    EGMStatus.GetInstance().current_jackpot.Transition(JackPotStatus.Idle);
                    // Persist
                    if (not_onload)
                        EGMDataPersisterCTL.GetInstance().PersistEGMAccounting(true);
                    // Persist state
                    if (not_onload)
                        EGMDataPersisterCTL.GetInstance().PersistEGMStatus(true);
                }
            });
            // Init frontend play
            EGMStatus.GetInstance().frontend_play = new FrontEndPlay();
            EGMStatus.GetInstance().frontend_play_penny = new FrontEndPlayPenny();
            EGMStatus.GetInstance().frontend_play.TransitionExecuted += new FrontEndPlay.TransactionExecutedEvent((s, not_onload, e) =>
            {
                try
                {
                    if (s == FrontEndPlayStatus.Playing)
                    {
                        Game_AnimationUpdate(AnimationEvent.ReelsSpinning);

                    }
                    else if (s == FrontEndPlayStatus.WinningState)
                    {

                    }

                    if (s == FrontEndPlayStatus.Playable || s == FrontEndPlayStatus.WaitingForBet || s == FrontEndPlayStatus.WaitingForCredits)
                    {
                        billAcc.EnableBillAcceptor();
                        SASCTL.GetInstance().AcceptTransfer(true, true);
                    }

                }
                catch
                {

                }

            });
            EGMStatus.GetInstance().frontend_play_penny.TransitionExecuted += new FrontEndPlayPenny.TransactionExecutedEvent((s, not_onload, e) =>
            {
                try
                {
                    if (s == FrontEndPlayPennyStatus.Playing)
                    {
                        Game_AnimationUpdate(AnimationEvent.ReelsSpinning);

                    }
                    else if (s == FrontEndPlayPennyStatus.WinningState)
                    {
                    }

                }
                catch
                {

                }

            });
            EGMStatus.GetInstance().selected_denomination = EGMSettings.GetInstance().Denominations.GetDenomination(0x01);



        }

        /// <summary>
        /// Init SAS Controller. It is used by EGM Constructor
        /// </summary>
        /// 


        private bool _waitingForConfirmation = false;
        private AFTTransferData _pendingTransferData;
        private System.Timers.Timer _websocketRetryTimer;
        private bool _cardInserted = false;
        private bool _sessionInitializedSent = false;

        //hold transfer data (for websocket to confirm)
        private class AFTTransferData
        {
            public string Type { get; set; }
            public decimal Cashable { get; set; }
            public decimal Restricted { get; set; }
            public decimal NonRestricted { get; set; }
            public object DataCode { get; set; }
            public int RetryCount { get; set; }
        }
        private void InitSASController()
        {
            // Initialize retry timer
            _websocketRetryTimer = new System.Timers.Timer(2000); // 2 seconds
            _websocketRetryTimer.Elapsed += WebSocketRetryTimer_Elapsed;
            _websocketRetryTimer.AutoReset = true;


            // Instantiate SAS CTL
            SASCTL.GetInstance().InstantiateSASCTL(unitydevelopment);

            // If SASCTL singleton is used for the first time, instantiate by using GetInstance. This creates and starts the SASClient 
            // Suscribes the event SASGameDisabled
            SASCTLModule.SASCTL.GetInstance().SASGameDisabled += new SASGameDisabledHandler((e) =>
            {
                // Update the disabledByHost
                EGMStatus.GetInstance().disabledByHost = true;

                // Cash In GM Lock Cnt
                SASCTL.GetInstance().RejectTransfer(true, true);


            });
            // Suscribes the event SASGameEnabled
            SASCTL.GetInstance().SASGameEnabled += new SASGameEnabledHandler((e) =>
            {
                // Update the disabledByHost
                EGMStatus.GetInstance().disabledByHost = false;

                // Cash In GM Lock Cnt

                SASCTL.GetInstance().AcceptTransfer(true, true);


            });
            // Suscribes the event SASSoundDisabled
            SASCTL.GetInstance().SASSoundDisabled += new SASSoundDisabledHandler((e) =>
            {
                // Update the soundEnabled
                EGMStatus.GetInstance().soundEnabled = false;
            });
            // Suscribes the event SASSoundEnabled
            SASCTL.GetInstance().SASSoundEnabled += new SASSoundEnabledHandler((e) =>
            {
                // Update the soundEnabled
                EGMStatus.GetInstance().soundEnabled = true;
            });
            // Suscribes the event SASEnterMaintenanceMode
            SASCTL.GetInstance().EnterMaintenanceMode += new EnterMaintenanceModeHandler((e) =>
            {
                EnterMaintenanceMode();
            });
            // Suscribes the event ExitMaintenanceMode
            SASCTL.GetInstance().ExitMaintenanceMode += new ExitMaintenanceModeHandler((e) =>
            {
                ExitMaintenanceMode();
            });
            // Suscribes the event SASReelSpinOrGamePlaySoundDisabled
            SASCTL.GetInstance().SASReelSpinOrGamePlaySoundDisabled += new SASReelSpinOrGamePlaySoundDisabledHandler((e) =>
            {
                // TODO SASReelSpinOrGamePlaySoundDisabled
            });
            // Suscribes the event SASBillAcceptorDisabled
            SASCTL.GetInstance().SASBillAcceptorDisabled += new SASBillAcceptorDisabledHandler((e) =>
            {
                billAcc.DisableBillAcceptor();
            });
            // Suscribes the event SASEnableDisableGame
            SASCTL.GetInstance().SASEnableDisableGameN += new SASEnableDisableGameNHandler((gamenumber, enabled, e) =>
            {
                // TODO SASEnableDisableGameN
            });
            // Suscribes the event SASMultipleJackpot
            SASCTL.GetInstance().SASMultipleJackpot += new SASMultipleJackpotHandler((minimumwin, maximunwin, multiplier_taxstatus, enable_disable, wagerType, e) =>
            {
                // UNSUPPORTED.
            });
            // Suscribes the event SASBillAcceptorEnabled
            SASCTL.GetInstance().SASBillAcceptorEnabled += new SASBillAcceptorEnabledHandler((e) =>
            {
                billAcc.EnableBillAcceptor();
            });
            // Suscribes the event SasHostCfgBillDenomination
            SASCTL.GetInstance().SasHostCfgBillDenomination += new SASHostCfgBillDenominationHandler((cfg, flag, e) =>
            {
                if (flag == BILL_ACCEPTOR_ACTION_FLAG.DISABLE_BILL_ACCEPTOR_AFTER_EACH_ACCEPTED_BILL)
                {
                    EGMSettings.GetInstance().DisableBillAcceptorAfterEachAcceptedBill = true;
                }
                else
                {
                    EGMSettings.GetInstance().DisableBillAcceptorAfterEachAcceptedBill = false;
                }

                byte lsb = 0x00;
                if (cfg.Enable_1)
                    lsb |= (byte)Math.Pow(2, 0);
                if (cfg.Enable_2)
                    lsb |= (byte)Math.Pow(2, 1);
                if (cfg.Enable_5)
                    lsb |= (byte)Math.Pow(2, 2);
                if (cfg.Enable_10)
                    lsb |= (byte)Math.Pow(2, 3);
                if (cfg.Enable_20)
                    lsb |= (byte)Math.Pow(2, 4);
                if (cfg.Enable_25)
                    lsb |= (byte)Math.Pow(2, 5);
                if (cfg.Enable_50)
                    lsb |= (byte)Math.Pow(2, 6);
                if (cfg.Enable_100)
                    lsb |= (byte)Math.Pow(2, 7);


                byte snd_byte = 0x00;
                if (cfg.Enable_200)
                    snd_byte |= (byte)Math.Pow(2, 0);
                if (cfg.Enable_250)
                    snd_byte |= (byte)Math.Pow(2, 1);
                if (cfg.Enable_500)
                    snd_byte |= (byte)Math.Pow(2, 2);
                if (cfg.Enable_1000)
                    snd_byte |= (byte)Math.Pow(2, 3);
                if (cfg.Enable_2000)
                    snd_byte |= (byte)Math.Pow(2, 4);
                if (cfg.Enable_2500)
                    snd_byte |= (byte)Math.Pow(2, 5);
                if (cfg.Enable_5000)
                    snd_byte |= (byte)Math.Pow(2, 6);
                if (cfg.Enable_10000)
                    snd_byte |= (byte)Math.Pow(2, 7);


                byte thrd_byte = 0x00;
                if (cfg.Enable_20000)
                    thrd_byte |= (byte)Math.Pow(2, 0);
                if (cfg.Enable_25000)
                    thrd_byte |= (byte)Math.Pow(2, 1);
                if (cfg.Enable_50000)
                    thrd_byte |= (byte)Math.Pow(2, 2);
                if (cfg.Enable_100000)
                    thrd_byte |= (byte)Math.Pow(2, 3);
                if (cfg.Enable_200000)
                    thrd_byte |= (byte)Math.Pow(2, 4);
                if (cfg.Enable_250000)
                    thrd_byte |= (byte)Math.Pow(2, 5);
                if (cfg.Enable_500000)
                    thrd_byte |= (byte)Math.Pow(2, 6);
                if (cfg.Enable_1000000)
                    thrd_byte |= (byte)Math.Pow(2, 7);

                billAcc.ConfigBillDenominations(lsb, snd_byte, thrd_byte);
            });
            // Suscribes the event SASRemoteHandpayReset
            SASCTL.GetInstance().SASRemoteHandpayReset += new SASRemoteHandpayResetHandler((e) =>
            {
                // UNSUPPORTED.
            });
            // Suscribes the event SASEnableJackpotHandpayReset
            SASCTL.GetInstance().SASEnableJackpotHandpayReset += new SASEnableJackpotHandpayResetHandler((method, e) =>
            {
                // UNSUPPORTED. 
            });
            // Suscribes the event SASEnableDisableAutoRebet
            SASCTL.GetInstance().SASEnableDisableAutoRebet += new SASEnableDisableAutoRebetHandler((enabled, e) =>
            {
                // TODO SASEnableDisableAutoRebet
            });
            // Suscribes the event AFTTransferCompleted
            SASCTL.GetInstance().AFTTransferCompleted += new AFTTransferCompletedHandler((t, dc, c, r, nr, e) =>
            {

                //// Store original values for WebSocket
                //decimal originalC = c;
                //decimal originalR = r;
                //decimal originalNr = nr;

                // Convert for internal processing if cashout
                if (t == "Cash Out")
                {
                    c = -1 * c;
                    r = -1 * r;
                    nr = -1 * nr;
                }
                // For both cashout AND cashin, store the data and wait for confirmation
                _pendingTransferData = new AFTTransferData
                {
                    Type = t,
                    Cashable = c,
                    Restricted = r,
                    NonRestricted = nr,
                    DataCode = dc,
                    RetryCount = 0
                };

                _waitingForConfirmation = true;

                // Send websocket and wait for confirmation
                decimal totalAmount = c + r + nr;
                // Calculate future credits by adding the transfer amount to current credits
                decimal currentCredits = EGM_GetCurrentCredits() + totalAmount;
                bool isCashout = t == "Cash Out";
               
                SendAFTWebSocket(Math.Abs(totalAmount), isCashout, currentCredits, dc.ToString());
                Logger.Log($"WebSocket sent: Total amount : {totalAmount}, isCashout: {isCashout}, currentCredits {currentCredits}. Waiting for confirmation...");
                HandleTransferConfirmation(true);
                // Start retry timer
                _websocketRetryTimer.Start();

                // Don't process further until confirmation is received
                //when you get aft_confirmed from websocket, it will automatically call HandleTransferConfirmation(true)
                return;
            });

            // Suscribes the event AFTTransferRejected
            SASCTL.GetInstance().AFTTransferRejected += new AFTTransferRejectedHandler((e) =>
            {
                // If we were waiting for confirmation and it got rejected, reset the state
                if (_waitingForConfirmation)
                {
                    _waitingForConfirmation = false;
                    _pendingTransferData = null;
                    Logger.Log("Transfer was rejected while waiting for confirmation.");
                }

                // TRANSITION: Collect from AFTCollectInProgress -> HandpayInProgress
                if (EGMStatus.GetInstance().current_collect.status == CollectStatus.AFTCollectInProgress)
                    EGMStatus.GetInstance().current_collect.Transition(CollectStatus.HandpayInProgress);
            });

            // Suscribes the event AFTTransferIncoming
            SASCTL.GetInstance().AFTTransferIncoming += new AFTTransferIncomingHandler((e) =>
            {
                Logger.Log("Transfer incoming.");
                // Stop collect_wait_72 timer
                collect_wait_72.Stop();
                // TRANSITION: Collect from Waiting72 -> AFTCollectInProgress
                EGMStatus.GetInstance().current_collect.Transition(CollectStatus.AFTCollectInProgress);
            });

            // Suscribes the event SASTiltDetected
            SASCTL.GetInstance().SASTiltDetected += new SASTiltDetectedHandler((msg, e) =>
            {
                UpdateMetersByEvent(Meters_EventName.SASInterfaceError);
                AddTilt(msg);
            });

            // Suscribes the event SASTiltLinkDown
            SASCTL.GetInstance().SASTiltLinkDown += new SASTiltLinkDownHandler((b, e) =>
            {
                // Log SAS Disconnection 
                if (b)
                {
                    AddSystemLog("SAS Comm Loss", "-");
                }
                else
                {
                    AddSystemLog("SAS Comms restored", "-");
                }
                // Tilt on SAS Disconnection

                if (b && (EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.WaitingForCredits
                || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.WaitingForBet
                || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.Playable
                || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.Tilt) && EGMSettings.GetInstance().sasSettings.mainConfiguration.TiltOnASASDisconnection)
                {
                    AddTilt("SAS Comm Loss");
                }
                else
                {
                    RemoveTilt("SAS Comm Loss");
                }
            });

            // Suscribes the event SASNoTiltDetected
            SASCTL.GetInstance().SASNoTiltDetected += new SASTiltNoDetectedHandler((msg, e) =>
            {
                RemoveTilt(msg);
            });
        }

        private void WebSocketRetryTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_waitingForConfirmation && _pendingTransferData != null)
            {
                _pendingTransferData.RetryCount++;

                // Resend WebSocket
                decimal totalAmount = _pendingTransferData.Cashable + _pendingTransferData.Restricted + _pendingTransferData.NonRestricted;
                decimal currentCredits = EGM_GetCurrentCredits() + totalAmount; // Add totalAmount to get future credits
                bool isCashout = _pendingTransferData.Type == "Cash Out";

                SendAFTWebSocket(Math.Abs(totalAmount), isCashout, currentCredits, _pendingTransferData.DataCode.ToString());
                Logger.Log($"WebSocket retry #{_pendingTransferData.RetryCount} sent: Total amount: {totalAmount}, isCashout: {isCashout}");
            }
            else
            {
                _websocketRetryTimer.Stop();
            }
        }

        // Method to process transfer completion
        private void ProcessTransferCompletion(string type, object dataCode, decimal cashable, decimal restricted, decimal nonRestricted)
        {
            // Stop retry timer
            _websocketRetryTimer.Stop();

            // Update credits at EGM Status
            EGMStatus.GetInstance().AddAmount(cashable, restricted, nonRestricted);

            if (cashable + restricted + nonRestricted > 0)
            {
                UpdateMetersByEvent(Meters_EventName.AFTTransferCompleted, new int[] { ToSASFormat(cashable + restricted + nonRestricted) });
            }

            // The transfer amount is to EGM, so we have to update the current credit in SASCTL
            if (Math.Abs(cashable) > 0)
            {
                SASCTL.GetInstance().SetCredits((byte)SAS_CREDIT_TYPE.SAS_CREDIT_CASHABLE, EGMStatus.GetInstance().currentCashableAmount);
            }
            if (Math.Abs(restricted) > 0)
            {
                SASCTL.GetInstance().SetCredits((byte)SAS_CREDIT_TYPE.SAS_CREDIT_RESTRICTED, EGMStatus.GetInstance().currentRestrictedAmount);
            }
            if (Math.Abs(nonRestricted) > 0)
            {
                SASCTL.GetInstance().SetCredits((byte)SAS_CREDIT_TYPE.SAS_CREDIT_NONRESTRICTED, EGMStatus.GetInstance().currentNonRestrictedAmount);
            }

            // Add transfer to history
            if (!(cashable == 0 && restricted == 0 && nonRestricted == 0))
            {
                EGMAccounting.GetInstance().transfers.AddNewTransfer(new GLOBALTYPES.AccountingAFTTransfer(
                    type,
                    DateTime.Now,
                    dataCode.ToString(),  // Convert to string
                    cashable + restricted + nonRestricted,
                    ToSASFormat(cashable),
                    ToSASFormat(restricted),
                    ToSASFormat(nonRestricted)), true);
            }

            // Persist EGM Accounting
            EGMDataPersisterCTL.GetInstance().PersistEGMAccounting(true);

            // TRANSITION: Collect from AFTCollectInProgress -> Completed
            EGMStatus.GetInstance().current_collect.Transition(CollectStatus.Completed);
        }

        // Method to handle transfer confirmation from client (call this when you receive WebSocket response)
        public void HandleTransferConfirmation(bool confirmed)
        {
            if (_waitingForConfirmation && _pendingTransferData != null)
            {
                // Stop retry timer
                _websocketRetryTimer.Stop();

                if (confirmed)
                {
                    // Client confirmed, process the transfer
                    Logger.Log("Transfer confirmed by client. Processing...");
                    ProcessTransferCompletion(
                        _pendingTransferData.Type,
                        _pendingTransferData.DataCode,
                        _pendingTransferData.Cashable,
                        _pendingTransferData.Restricted,
                        _pendingTransferData.NonRestricted
                    );
                }
                else
                {
                    // Client rejected transfer - no reversal, just log and reset state
                    Logger.Log("Transfer rejected by client. Resetting state.");


                    EGMStatus.GetInstance().current_collect.Transition(CollectStatus.Waiting72);

                }

                // Reset flags
                _waitingForConfirmation = false;
                _pendingTransferData = null;
            }
            else
            {
                Logger.Log("HandleTransferConfirmation called but no pending transfer to confirm.");
            }
        }












        private decimal GetCurrentCredits()
        {
            return MenuGUIController.GetInstance().GUI_GetCurrentCredits();
        }

        /// <summary>
        /// Init Bill Acceptor Controller. It is used by EGM Constructor
        /// </summary>
        private void InitBillAcceptorController()
        {
            if (!unitydevelopment)
                billAcc = new SSPBillAccServiceCTL(EGMSettings.GetInstance().BillAcceptorComPort); // Instantiate the Bill Acceptor Controller
            else
                billAcc = new VirtualBillAccCTL();

            billAcc.Enabled = EGMSettings.GetInstance().BillAcceptor;
            billAcc.ConfigBillDenominations((byte)EGMSettings.GetInstance().BillAcceptorChannelSet1, (byte)EGMSettings.GetInstance().BillAcceptorChannelSet2, (byte)EGMSettings.GetInstance().BillAcceptorChannelSet3);
            billAcc.StartBillAccCTL(); // Start the bill acceptor
            // Suscribe the bill accepted event with the following method
            billAcc.BillAccepted += new CTLBillAcceptedHandler((bill, obj) =>
            {
                // Add to credits the bill amount
                AddAmount(bill, 0, 0, true);
                // Add Bill Insertion
                AddSystemLog($"R {bill}.00 bill accepted", "-");

                // Send websocket with correct current credits (after bill is added)
                SendBillAcceptedToWebSocket(bill);

                Logger.Log($"Bill accepted: {bill}");


                // Throw the exception to SAS Client
                SASCTL.GetInstance().SASBillException(bill);
                // Add bill to the last bill history
                EGMAccounting.GetInstance().bills.AddNewBill(new LastBill(DateTime.Now, bill), true);
                // Increment count to the different bill meters
                UpdateMetersByEvent(Meters_EventName.BillInserted, new int[] { bill });

                //if (EGMSettings.GetInstance().DisableBillAcceptorAfterEachAcceptedBill)
                //{
                //    billAcc.DisableBillAcceptor();
                //    SASCTL.GetInstance().SetBillValidatorEnabledInSAS(false);
                //}
                //else
                //{
                //    billAcc.EnableBillAcceptor();
                //    SASCTL.GetInstance().SetBillValidatorEnabledInSAS(true);
                //}
                // decimal newCredits = MenuGUIController.GetInstance().GUI_GetCurrentCredits();
                // CreditsUpdated?.Invoke(newCredits);

            });
            // Suscribe the cashbox removing with the following method
            billAcc.CashboxRemoved += new CTLBillAcceptorSingleEventHandler((obj) =>
            {
                AddTilt("Cashbox Removed");
                AddSystemLog("Stacker Removed", "-");
                UpdateMetersByEvent(Meters_EventName.StackerOpen);
                // Throw the exception to SAS Client
                SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.CashboxRemoved);
            });
            // Suscribe the cashbox replacing with the following method
            billAcc.CashboxReplaced += new CTLBillAcceptorSingleEventHandler((obj) =>
            {
                RemoveTilt("Cashbox Removed");

                AddSystemLog("Stacker Installed", "-");

                UpdateMetersByEvent(Meters_EventName.CashboxDoorClosed);

                // Throw the exception to SAS Client
                SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.CashboxInstalled);
                EGMStatus.GetInstance().extra_info_list.Add("Cashbox Replaced");
            });
            // Suscribe the note rejected with the following method
            billAcc.NoteRejected += new CTLBillAcceptorSingleEventHandler((obj) =>
            {
                // Throw the exception to SAS Client
                SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.BillRejected);

                AddSystemLog("Bill Rejected", "-");
            });
            // Suscribe the cashbox full event with the following method
            billAcc.CashboxFull += new CTLBillAcceptorSingleEventHandler((obj) =>
            {
                // Throw the exception to SAS Client
                SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.CashboxFull);

            });
            // Suscribe the bill jamming with the following method
            billAcc.BillJam += new CTLBillAcceptorSingleEventHandler((obj) =>
            {
                UpdateMetersByEvent(Meters_EventName.BillJam);
                // Throw the exception to SAS Client
                SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.BillJam);

            });
            // Suscribe the event with the following method
            billAcc.BillAcceptorUnreachable += new CTLBillAcceptorAlertEventHandler((msg, obj) =>
            {
                AddTilt(msg);

            });
            // Suscribe the event with the following method
            billAcc.BillAcceptorCommRestored += new CTLBillAcceptorAlertEventHandler((msg, obj) =>
            {
                billAcc.ConfigBillDenominations((byte)EGMSettings.GetInstance().BillAcceptorChannelSet1, (byte)EGMSettings.GetInstance().BillAcceptorChannelSet2, (byte)EGMSettings.GetInstance().BillAcceptorChannelSet3);
                RemoveTilt(msg);


            });


            billacceptormaxlaod.Elapsed += new ElapsedEventHandler((o, t) =>
            {
                var denoms = Menu_GetBillDenominations();

                bool cambio = false;
                byte lsb = (byte)EGMSettings.GetInstance().BillAcceptorChannelSet1;

                byte snd_byte = (byte)EGMSettings.GetInstance().BillAcceptorChannelSet2;

                byte thrd_byte = (byte)EGMSettings.GetInstance().BillAcceptorChannelSet3;

                foreach (Channel c in denoms)
                {
                    if ((new int[] { 5, 10, 20, 50, 100, 200 }).Contains(c.denomination))
                    {
                        if (EGMStatus.GetInstance().currentAmount + c.denomination > EGMSettings.GetInstance().creditLimit)
                        {
                            if (c.denomination == 5)
                            {
                                byte mask = (byte)~(byte)Math.Pow(2, 2);
                                if ((lsb & mask) != lsb)
                                {
                                    cambio = true;
                                    lsb &= mask;
                                }
                            }
                            if (c.denomination == 10)
                            {
                                byte mask = (byte)~(byte)Math.Pow(2, 3);
                                if ((lsb & mask) != lsb)
                                {
                                    cambio = true;
                                    lsb &= mask;
                                }
                            }
                            if (c.denomination == 20)
                            {
                                byte mask = (byte)~(byte)Math.Pow(2, 4);
                                if ((lsb & mask) != lsb)
                                {
                                    cambio = true;
                                    lsb &= mask;
                                }
                            }
                            if (c.denomination == 50)
                            {
                                byte mask = (byte)~(byte)Math.Pow(2, 6);
                                if ((lsb & mask) != lsb)
                                {
                                    cambio = true;
                                    lsb &= mask;
                                }
                            }
                            if (c.denomination == 100)
                            {
                                byte mask = (byte)~(byte)Math.Pow(2, 7);
                                if ((lsb & mask) != lsb)
                                {
                                    cambio = true;
                                    lsb &= mask;
                                }
                            }

                            if (c.denomination == 200)
                            {
                                byte mask = (byte)~(byte)Math.Pow(2, 0);
                                if ((snd_byte & mask) != snd_byte)
                                {
                                    cambio = true;
                                    snd_byte &= mask;
                                }
                            }
                        }
                        else if (EGMSettings.GetInstance().EnabledChannels(c.denomination) && EGMStatus.GetInstance().currentAmount + c.denomination <= EGMSettings.GetInstance().creditLimit)
                        {

                            if (c.denomination == 5)
                            {
                                byte mask = (byte)Math.Pow(2, 2);
                                if ((lsb | mask) != lsb)
                                {
                                    cambio = true;
                                    lsb |= mask;
                                }
                            }
                            if (c.denomination == 10)
                            {
                                byte mask = (byte)Math.Pow(2, 3);
                                if ((lsb | mask) != lsb)
                                {
                                    cambio = true;
                                    lsb |= mask;
                                }
                            }
                            if (c.denomination == 20)
                            {
                                byte mask = (byte)Math.Pow(2, 4);
                                if ((lsb | mask) != lsb)
                                {
                                    cambio = true;
                                    lsb |= mask;
                                }
                            }
                            if (c.denomination == 50)
                            {
                                byte mask = (byte)Math.Pow(2, 6);
                                if ((lsb | mask) != lsb)
                                {
                                    cambio = true;
                                    lsb |= mask;
                                }
                            }
                            if (c.denomination == 100)
                            {
                                byte mask = (byte)Math.Pow(2, 7);
                                if ((lsb | mask) != lsb)
                                {
                                    cambio = true;
                                    lsb |= mask;
                                }
                            }

                            if (c.denomination == 200)
                            {
                                byte mask = (byte)Math.Pow(2, 0);
                                if ((snd_byte | mask) != snd_byte)
                                {
                                    cambio = true;
                                    snd_byte |= mask;
                                }
                            }

                        }
                    }

                }
                if (cambio)
                {
                    billAcc.ConfigBillDenominations(lsb, snd_byte, thrd_byte);
                }

            });
            billacceptormaxlaod.Start();
        }

        public void CheckWebSocketStatus()
        {
            Logger.Log($"WebSocket Status - Connected: {isConnected}, IsAlive: {webSocket?.IsAlive}, ReadyState: {webSocket?.ReadyState}");
        }

        public void TestWebSocket()
        {
            Logger.Log("Testing WebSocket connection manually...");
            CheckWebSocketStatus();

            var testMessage = new
            {
                eventType = "TEST",
                message = "Test connection from EGM",
                timestamp = DateTime.UtcNow,
                client = "EGM_Application"
            };

            SendToWebSocket(testMessage);
            Logger.Log("Test message sent - waiting for response...");

            // Optional: Add a small delay to see the response
            Thread.Sleep(1000);
            Logger.Log("Test completed");
        }

        public void SendBillAcceptedToWebSocket(decimal amount)
        {
            // Credits already include the bill amount since AddAmount was called before this method
            // So we just get the current credits without adding the amount again
            decimal credits = EGM_GetCurrentCredits();
            var message = new
            {
                EventType = "BILL_INSERTED",
                amount = amount,
                CurrentCredits = credits,
                timestamp = DateTime.UtcNow,
                currency = "ZAR"
            };

            SendToWebSocket(message);
            Logger.Log($"Sent BILL_INSERTED: {amount} ZAR");
        }
        private void SendAFTWebSocket(decimal amount, bool isCashout, decimal currentCredits, string reference = "")
        {
            var message = new
            {
                EventType = isCashout ? "AFT_CASHOUT" : "AFT_DEPOSIT",
                Amount = amount,
                CurrentCredits = currentCredits,
                Timestamp = DateTime.UtcNow,
                AFTReference = reference,
                Currency = "ZAR"
            };

            SendToWebSocket(message);
        }

        /// <summary>
        /// Init GPIO Controller. It is used by EGM Constructor
        /// </summary>
        private void InitGPIOController()
        {
            if (!unitydevelopment)
                gpioCTL = new GanlotGPIOCTL(); // Instantiate the GPIO Controller
            else
            {
                gpioCTL = new VirtualGPIOCTL();
                ((VirtualGPIOCTL)gpioCTL).Bill10Inserted += new VirtualGPIOCTL.VIRTUALGPIOCTL_Bill10Inserted((o) =>
                {
                    // Add to credits the bill amount
                    AddAmount(10, 0, 0, true);
                    // Add Bill Insertion
                    AddSystemLog($"R {10}.00 bill accepted", "-");
                    // Throw the exception to SAS Client
                    SASCTL.GetInstance().SASBillException(10);
                    // Add bill to the last bill history
                    EGMAccounting.GetInstance().bills.AddNewBill(new LastBill(DateTime.Now, 10), true);
                    // Increment count to the different bill meters
                    UpdateMetersByEvent(Meters_EventName.BillInserted, new int[] { 10 });
                }); ;
            }

            // Suscribe the Sensor Opened event with the following method
            gpioCTL.SensorOpened += new GPIOCTL_SensorOpenedEvent((type, obj) =>
            {


                if (type == SensorName.D_MAINDOOR)
                {
                    // Add the tilt 'MainDoorOpen'
                    AddTilt("MainDoorOpen");

                    if (!EGMStatus.GetInstance().s_mainDoorStatus)
                    {

                        //Add system log
                        AddSystemLog("Main Door Open", "-");
                        // Throw the exception to SAS Client
                        SASCTL.GetInstance().SASDoorOpenException(type);
                        // Update meters by event SlotDoorOpen
                        UpdateMetersByEvent(Meters_EventName.SlotDoorOpen);
                        // Set Main Door Status to true
                        EGMStatus.GetInstance().s_mainDoorStatus = true;
                        // Reprocess Output Status
                        ReprocessEGMStatus_Output(() =>
                        {  // Set the towerlight1 on
                            EGMStatus.GetInstance().o_tower1lightStatus = true;
                        });
                    }
                }
                else if (type == SensorName.D_LOGICDOOR)
                {
                    // Add the tilt 'Logic Door Open'
                    AddTilt("Logic Door Open");
                    if (!EGMStatus.GetInstance().s_logicDoorStatus)
                    {
                        //Add system log
                        AddSystemLog("Logic Door Open", "-");
                        // Update meters by event LogicDoorOpen
                        UpdateMetersByEvent(Meters_EventName.LogicDoorOpen);
                        // Throw the exception to SAS Client
                        SASCTL.GetInstance().SASDoorOpenException(SensorName.D_CARDCAGEDOOR);
                        // Set Logic Door Status to true
                        EGMStatus.GetInstance().s_logicDoorStatus = true;

                    }

                }
                else if (type == SensorName.D_BELLYDOOR)
                {
                    if (!localganlot)
                    {
                        AddTilt("BellyDoorOpen");
                    }
                    if (!EGMStatus.GetInstance().s_bellyDoorStatus)
                    {
                        //Add system log
                        AddSystemLog("Belly Door Open", "-");
                        // Throw the exception to SAS Client
                        SASCTL.GetInstance().SASDoorOpenException(type);
                        // Set Belly Door Status to true
                        EGMStatus.GetInstance().s_bellyDoorStatus = true;
                        // Update meters by event BellyDoorOpen
                        UpdateMetersByEvent(Meters_EventName.BellyDoorOpen);
                    }

                }
                else if (type == SensorName.D_CARDCAGEDOOR)
                {
                    if (!localganlot)
                    {
                        AddTilt("CardCageOpen");
                    }

                    if (!EGMStatus.GetInstance().s_cardCageDoorStatus)
                    {
                        //Add system log
                        AddSystemLog("Card Cage Open", "-");

                        // Throw the exception to SAS Client
                        SASCTL.GetInstance().SASDoorOpenException(type);
                        // Set CardCage Door Status to true
                        EGMStatus.GetInstance().s_cardCageDoorStatus = true;
                        // Update meters by event CardCageDoorOpen
                        UpdateMetersByEvent(Meters_EventName.CardCageDoorOpen);
                    }
                }
                else if (type == SensorName.D_CASHBOXDOOR)
                {
                    if (!localganlot)
                    {
                        AddTilt("CashboxDoorOpen");
                    }

                    if (!EGMStatus.GetInstance().s_cashBoxDoorStatus)
                    {
                        //Add system log
                        AddSystemLog("Cashbox Door Open", "-");
                        // Throw the exception to SAS Client
                        SASCTL.GetInstance().SASDoorOpenException(type);
                        // Set CashBox Door Status to true
                        EGMStatus.GetInstance().s_cashBoxDoorStatus = true;
                        // Update meters by event CashboxDoorOpen
                        UpdateMetersByEvent(Meters_EventName.CashboxDoorOpen);
                    }
                }
                else if (type == SensorName.D_DROPBOXDOOR)
                {
                    if (!localganlot)
                    {
                        AddTilt("DropboxDoorOpen");
                    }

                    if (!EGMStatus.GetInstance().s_dropBoxDoorStatus)
                    {
                        //Add system log
                        AddSystemLog("Drop Door Open", "-");
                        // Throw the exception to SAS Client
                        SASCTL.GetInstance().SASDoorOpenException(type);
                        // Set Drop Door Status to true
                        EGMStatus.GetInstance().s_dropBoxDoorStatus = true;
                        // Update meters by event DropDoorOpen
                        UpdateMetersByEvent(Meters_EventName.DropDoorOpen);
                    }
                }

                ReprocessEGMStatus_Frontend();

            });
            // Suscribe the Sensor Closed event with the following method
            gpioCTL.SensorClosed += new GPIOCTL_SensorClosedEvent((type, obj) =>
            {
                // Throw the exception to SAS Client

                if (type == SensorName.D_MAINDOOR)
                {
                    // Remove the tilt 'MainDoorOpen'
                    RemoveTilt("MainDoorOpen");

                    if (EGMStatus.GetInstance().s_mainDoorStatus)
                    {
                        //Add system log
                        AddSystemLog("Main Door Closed", "-");

                        SASCTL.GetInstance().SASDoorClosedException(type);

                        // Update meters by event
                        UpdateMetersByEvent(Meters_EventName.SlotDoorClosed);
                        // Set Main Door Status to false
                        EGMStatus.GetInstance().s_mainDoorStatus = false;

                        EGMStatus.GetInstance().extra_info_list.Add("Door Closed");
                        // Reprocess Output Status
                        ReprocessEGMStatus_Output(() =>
                        {  // Set the towerlight1 off
                            EGMStatus.GetInstance().o_tower1lightStatus = false;
                        });
                    }
                }
                else if (type == SensorName.D_LOGICDOOR)
                {
                    // Remove the tilt 'Logic Door Open'
                    RemoveTilt("Logic Door Open");

                    if (EGMStatus.GetInstance().s_logicDoorStatus)
                    {
                        //Add system log
                        AddSystemLog("Logic Door Closed", "-");

                        SASCTL.GetInstance().SASDoorClosedException(SensorName.D_CARDCAGEDOOR);
                        // Set Logic Door Status to false
                        EGMStatus.GetInstance().s_logicDoorStatus = false;

                    }
                }
                else if (type == SensorName.D_BELLYDOOR)
                {
                    RemoveTilt("BellyDoorOpen");

                    if (EGMStatus.GetInstance().s_bellyDoorStatus)
                    {
                        //Add system log
                        AddSystemLog("Belly Door Closed", "-");

                        SASCTL.GetInstance().SASDoorClosedException(type);
                        // Set Belly Door Status to false
                        EGMStatus.GetInstance().s_bellyDoorStatus = false;
                        // Update meters by event BellyDoorClosed
                        UpdateMetersByEvent(Meters_EventName.BellyDoorClosed);
                    }

                }
                else if (type == SensorName.D_CARDCAGEDOOR)
                {

                    RemoveTilt("CardCageOpen");

                    if (EGMStatus.GetInstance().s_cardCageDoorStatus)
                    {
                        //Add system log
                        AddSystemLog("Card Cage Closed", "-");

                        SASCTL.GetInstance().SASDoorClosedException(type);
                        // Set Card Door Status to false
                        EGMStatus.GetInstance().s_cardCageDoorStatus = false;
                        // Update meters by event CardCageDoorClosed
                        UpdateMetersByEvent(Meters_EventName.CardCageDoorClosed);
                    }
                }
                else if (type == SensorName.D_CASHBOXDOOR)
                {
                    RemoveTilt("CashboxDoorOpen");

                    if (EGMStatus.GetInstance().s_cashBoxDoorStatus)
                    {
                        //Add system log
                        AddSystemLog("Cashbox Door Closed", "-");

                        SASCTL.GetInstance().SASDoorClosedException(type);
                        // Set Cash Door Status to false
                        EGMStatus.GetInstance().s_cashBoxDoorStatus = false;
                        // Update meters by event CashboxDoorClosed
                        UpdateMetersByEvent(Meters_EventName.CashboxDoorClosed);
                    }
                }
                else if (type == SensorName.D_DROPBOXDOOR)
                {
                    RemoveTilt("DropboxDoorOpen");

                    if (EGMStatus.GetInstance().s_dropBoxDoorStatus)
                    {
                        //Add system log
                        AddSystemLog("Drop Door Closed", "-");

                        SASCTL.GetInstance().SASDoorClosedException(type);
                        // Set Cash Door Status to false
                        EGMStatus.GetInstance().s_dropBoxDoorStatus = false;
                        // Update meters by event DropDoorClosed
                        UpdateMetersByEvent(Meters_EventName.DropDoorClosed);
                    }
                }

                ReprocessEGMStatus_Frontend();

            });
            // Suscribe the Button Pressed event with the following method
            gpioCTL.ButtonPressed += new GPIOCTL_ButtonPressedEvent((button, obj) =>
            {
                if (button == ButtonName.B_KEY)
                {
                    if (!criticaltilt)
                    {
                        // If there is a handpay that could be reset, add the current handpay to history and persist
                        // TRANSITION: Handpay from HandpayPending -> HandpayResetByKey
                        if (EGMStatus.GetInstance().current_handpay.Transition(HandpayStatus.HandpayResetByKey))
                        {
                            AddSystemLog("Handpay Reset", "-");
                        }
                        // If there is a jackpot that could be reset, add the current jackpot to history and persist
                        // TRANSITION: Jackpot from JackpotPending -> JackpotResetByKey
                        else if (EGMStatus.GetInstance().current_jackpot.Transition(JackPotStatus.JackpotResetByKey))
                        { }
                        // Else, enter to menu
                        else
                        {
                            EGMStatus.GetInstance().menuActive = true;
                        }
                    }

                }
                else if (button == ButtonName.B_SERVICE)
                {
                    bool actionable = (EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.Playable
                                   || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.WaitingForCredits
                                   || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.WaitingForBet) && !(EGMStatus.GetInstance().autoSpin);

                    if (actionable)
                    {
                        // Reprocess Output Status
                        ReprocessEGMStatus_Output(() =>
                        {
                            EGMStatus.GetInstance().o_tower2lightStatus = !EGMStatus.GetInstance().o_tower2lightStatus;
                            if (EGMStatus.GetInstance().o_tower2lightStatus)
                            {
                                SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.ChangeLampOn);
                            }
                            else
                            {
                                SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.ChangeLampOff);
                            }
                        });
                    }
                }
                else if (button == ButtonName.B_AUTOSPIN)
                {
                    bool actionable = EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.Playable
                                   || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.Playing;


                    UIPriorityEvent(this, EventType.AutoSpinToggled, new EventArgs());

                    if (actionable)
                    {
                        Game_AutoSpinToggle();
                    }


                }
                else if (button == ButtonName.B_SPIN)
                {
                    bool actionable = (EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.Playable
                                   || EGMStatus.GetInstance().frontend_play_penny.thisstatus == FrontEndPlayPennyStatus.Playable
                                    || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.ActionGamePlayable
                                    || EGMStatus.GetInstance().frontend_play_penny.thisstatus == FrontEndPlayPennyStatus.ActionGamePlayable) && !EGMStatus.GetInstance().showInfo;

                    UIPriorityEvent(this, EventType.SpinPressed, new EventArgs());

                    if (actionable)
                    {
                        EGMStatus.GetInstance().spinstatus.Transition(SpinRepresentationStatus.ReelSpinning);
                        if (EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.Playable
                                   || EGMStatus.GetInstance().frontend_play_penny.thisstatus == FrontEndPlayPennyStatus.Playable)
                            Game_AnimationUpdate(AnimationEvent.ReelsSpinning);
                        else if (EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.ActionGamePlayable
                              || EGMStatus.GetInstance().frontend_play_penny.thisstatus == FrontEndPlayPennyStatus.ActionGamePlayable)
                            Game_AnimationUpdate(AnimationEvent.WheelSpinning);

                    }
                }
                else if (button == ButtonName.B_LINES)
                {
                    UIPriorityEvent(this, EventType.LinesPlusPressed, new EventArgs());

                    bool actionable = (EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.Playable
                                   || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.WaitingForCredits
                                   || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.WaitingForBet)
                                   && !(EGMStatus.GetInstance().autoSpin);

                    if (actionable)
                        Game_IncreaseLinesBet();
                }
                else if (button == ButtonName.B_BET)
                {
                    UIPriorityEvent(this, EventType.BetPlusPressed, new EventArgs());

                    bool actionable = (EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.Playable
                                   || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.WaitingForCredits
                                   || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.WaitingForBet)
                                   && !(EGMStatus.GetInstance().autoSpin)
                                   && EGMStatus.GetInstance().currentAmount > 0;

                    if (actionable)
                        Game_IncreaseSelectedCreditValue();
                }
                else if (button == ButtonName.B_MAXBET)
                {
                    UIPriorityEvent(this, EventType.MaxBetPressed, new EventArgs());

                    bool actionable = (EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.Playable
                                   || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.WaitingForCredits
                                   || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.WaitingForBet)
                                   && !(EGMStatus.GetInstance().autoSpin)
                                   && EGMStatus.GetInstance().currentCashableAmount > 0;

                    if (actionable)
                        Game_MaxBet();
                }
                else if (button == ButtonName.B_HELP)
                {
                    bool actionable = (EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.Playable
                                   || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.WaitingForCredits
                                   || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.WaitingForBet
                                   || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.ShowingHelp) && EGMStatus.GetInstance().current_animation_event != AnimationEvent.ReelsSpinning && !(EGMStatus.GetInstance().autoSpin);

                    if (actionable)
                    {
                        if (EGMStatus.GetInstance().showInfo)
                            Game_ExitShowingHelp();
                        else
                            Game_EnterShowingHelp();
                    }
                }

                else if (button == ButtonName.B_CASHOUT)
                {
                    bool actionable = (EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.Playable
                                    || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.WaitingForBet
                                    || (EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.WaitingForCredits && (EGMStatus.GetInstance().currentCashableAmount + EGMStatus.GetInstance().currentNonRestrictedAmount) > 0)) && !(EGMStatus.GetInstance().autoSpin);
                    if (EGMStatus.GetInstance().currentCashableAmount == 0 &&
                        EGMStatus.GetInstance().currentNonRestrictedAmount == 0)
                        EGMStatus.GetInstance().cashoutwithrestricted = true;
                    else
                    {
                        if (actionable)
                        {
                            // Cashout & AFTEnabled
                            if (EGMSettings.GetInstance().AFTEnabled)
                            {
                                // TRANSITION: Collect from Idle -> Waiting72
                                EGMStatus.GetInstance().current_collect.Transition(CollectStatus.Waiting72);
                            }
                            // Cashout & Not AFT Enabled
                            else
                            {
                                if (EGMStatus.GetInstance().current_handpay.status == HandpayStatus.Idle)
                                {
                                    // TRANSITION: Collect from Idle -> HandpayInProgress
                                    EGMStatus.GetInstance().current_collect.Transition(CollectStatus.HandpayInProgress);
                                }
                            }
                        }
                    }
                }


            });
            // Suscribe the Button Released event with the following method
            gpioCTL.ButtonReleased += new GPIOCTL_ButtonReleasedEvent((button, obj) =>
            {
                // TODO
            });
            // Suscribe the Battery Low event with the following method
            gpioCTL.BatteryLow += new GPIOCTL_BatteryLowEvent((obj) =>
            {
                SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.BatteryLow);
                ReprocessEGMStatus_Frontend();

            });
            // Start the GPIOCTL
            gpioCTL.StartGPIOCTL();
        }


        #endregion

        #region EXPOSED TO GUICONTROLLER

        #region GameGUI

        /// <summary>
        /// {{ SET }}
        /// Used by GameGUIController. Enter Showing help
        /// </summary>
        /// <returns></returns>
        internal void Game_EnterShowingHelp()
        {
            bool actionable = EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.Playable
                                  || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.WaitingForCredits
                                  || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.WaitingForBet;

            if (actionable)
            {
                EGMStatus.GetInstance().showInfo = true;

            }
        }

        /// <summary>
        /// {{ SET }}
        /// Used by GameGUIController. Exit Showing help
        /// </summary>
        /// <returns></returns>
        internal void Game_ExitShowingHelp()
        {
            bool actionable = EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.ShowingHelp;

            if (actionable)
            {
                EGMStatus.GetInstance().showInfo = false;
            }

        }



        internal FrontEndPlayStatus getFrontEndPlayStatus()
        {
            return EGMStatus.GetInstance().frontend_play.thisstatus;
        }

        internal FrontEndPlayPennyStatus getFrontEndPlayPennyStatus()
        {
            return EGMStatus.GetInstance().frontend_play_penny.thisstatus;
        }


        internal void Game_AutoSpinToggle()
        {
            EGMStatus.GetInstance().autoSpin = !EGMStatus.GetInstance().autoSpin;
            EGMStatus.GetInstance().userautoSpin = EGMStatus.GetInstance().autoSpin;
        }

        /// <summary>
        /// {{ SET }}
        /// Used by GameGUIController. Set the current game UI State (finish the play).
        /// </summary>
        /// <returns></returns>
        internal void Game_AnimationUpdate(AnimationEvent anevent)
        {

            if (!(EGMStatus.GetInstance().frontend_play_penny.thisstatus == FrontEndPlayPennyStatus.ExpandedSymbolDraw
             && anevent == AnimationEvent.ReelsStoped))
            {

                EGMStatus.GetInstance().current_animation_event = anevent;

                if (anevent == AnimationEvent.WinAnimationFinished)
                {
                    EGMStatus.GetInstance().current_play.baseWinning = false;
                    if (EGMStatus.GetInstance().current_play.actionGameIndex == -1)
                    {
                        if (EGMStatus.GetInstance().current_play.totalCurrentActionGames > 0)
                            EGMStatus.GetInstance().current_play.actionGameIndex = 0;
                    }
                }
                if (anevent == AnimationEvent.WheelStoped)
                {
                    EGMStatus.GetInstance().current_play.actionGameIndex++;
                }
                if (anevent == AnimationEvent.ActionGameWinAnimationFinished)
                {
                    EGMStatus.GetInstance().current_play.actiongamewinning = false;
                }
                if (anevent == AnimationEvent.MisteryWinAnimationFinished)
                {
                    EGMStatus.GetInstance().current_play.mistery_win = false;
                }
                if (anevent == AnimationEvent.ScatterWinAnimationFinished)
                {
                    EGMStatus.GetInstance().current_play.scatter_win = false;
                    if (EGMStatus.GetInstance().current_play.pennyGamesIndex == -1)
                    {
                        if (EGMStatus.GetInstance().current_play.getPennyGames() != null)
                        {
                            if (EGMStatus.GetInstance().current_play.getPennyGames().Count() > 0)
                            {
                                EGMStatus.GetInstance().current_play.pennyGamesIndex = 0;
                            }
                        }
                    }
                }
                if (anevent == AnimationEvent.SymbolsExpanded)
                {
                    onexpandedwin = true;
                    EGMStatus.GetInstance().current_play.expanded_win = false;
                    if (EGMStatus.GetInstance().current_play.actionGameIndex == -1)
                    {
                        if (EGMStatus.GetInstance().current_play.totalCurrentActionGames > 0)
                            EGMStatus.GetInstance().current_play.actionGameIndex = 0;
                    }
                }
                if (anevent == AnimationEvent.ReelsStoped)
                {
                    onexpandedwin = false;
                    EGMDataPersisterCTL.GetInstance().PersistEGMSlotPlay();
                    SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.EndPlay);
                    EGMStatus.GetInstance().extra_info_list.Clear();
                }
                if (anevent == AnimationEvent.ReelsSpinning)
                {
                    if (EGMStatus.GetInstance().frontend_play.thisstatus != FrontEndPlayStatus.PennyGamesBonus)
                    {
                    }
                }
                if (anevent == AnimationEvent.EnterBillValidationTest)
                {
                    if (EGMStatus.GetInstance().menuActive)
                        billAcc.EnableBillAcceptor();
                }
                if (anevent == AnimationEvent.ExitBillValidationTest)
                {
                    if (EGMStatus.GetInstance().menuActive)
                        billAcc.DisableBillAcceptor();
                }
            }
        }

        /// <summary>
        /// Get Enabled Denominations
        /// Get Index of current selected denom
        /// Get the previous index and return
        /// </summary>
        internal void Game_DecreaseSelectedCreditValue()
        {
            var denominations = EGMSettings.GetInstance().Denominations.GetEnabledDenominations();

            int index = denominations.IndexOf(denominations.Where(d => d.monetaryValue == EGMStatus.GetInstance().selectedCreditValue).FirstOrDefault());

            if (index == 0)
            {
                index = denominations.Count() - 1;
            }
            else
            {
                index--;
            }

            EGMStatus.GetInstance().selectedCreditValue = denominations[index].monetaryValue;
            EGMDataPersisterCTL.GetInstance().PersistEGMStatus(true);

        }
        internal void Game_IncreaseSelectedCreditValue()
        {
            var denominations = EGMSettings.GetInstance().Denominations.GetEnabledDenominations().Where(
                 denom => (EGMStatus.GetInstance().betxline * EGMStatus.GetInstance().betLines)
                  * denom.monetaryValue <= EGMStatus.GetInstance().currentAmount
                ).ToList();

            if (denominations.Count() > 0)
            {
                int index = denominations.IndexOf(denominations.Where(d => d.monetaryValue == EGMStatus.GetInstance().selectedCreditValue).FirstOrDefault());

                if (index == denominations.Count() - 1)
                {
                    index = 0;
                }
                else
                {
                    index++;
                }
                EGMStatus.GetInstance().selectedCreditValue = denominations[index].monetaryValue;
                EGMDataPersisterCTL.GetInstance().PersistEGMStatus(true);
            }

        }


        /// <summary>
        /// Used by GameGUIController. It is a reaction to B_Lines button. Used to increase the bet lines.
        /// </summary>
        internal void Game_IncreaseLinesBet()
        {
            if (EGMStatus.GetInstance().betLines < EGMSettings.GetInstance().maxBetLines)
            {
                EGMStatus.GetInstance().betLines++;
            }
            else
            {
                EGMStatus.GetInstance().betLines = EGMSettings.GetInstance().minBetLines;
            }

        }

        /// <summary>
        /// Used by GameGUIController. Used to decrease the bet lines.
        /// </summary>
        internal void Game_DecreaseLinesBet()
        {
            if (EGMStatus.GetInstance().betLines > EGMSettings.GetInstance().minBetLines)
            {
                EGMStatus.GetInstance().betLines--;
            }
            else
            {
                EGMStatus.GetInstance().betLines = EGMSettings.GetInstance().maxBetLines;
            }

            UIPriorityEvent(this, EventType.LinesMinusPressed, new EventArgs());
        }

        /// <summary>
        /// Used by GameGUIController. It is a reaction to B_Lines button. Used to increase the bet lines.
        /// </summary>
        internal void Game_IncreaseBet()
        {
            if (EGMStatus.GetInstance().betxline < EGMSettings.GetInstance().maxBetxline)
            {
                EGMStatus.GetInstance().betxline++;
            }
            else
            {
                EGMStatus.GetInstance().betxline = EGMSettings.GetInstance().minBetxline;
            }

        }

        internal void Game_MaxBet()
        {
            EGMStatus.GetInstance().betxline = EGMSettings.GetInstance().maxBetxline;

        }


        /// <summary>
        /// Used by GameGUIController. Used to decrease the bet lines.
        /// </summary>
        internal void Game_DecreaseBet()
        {
            if (EGMStatus.GetInstance().betxline > EGMSettings.GetInstance().minBetxline)
            {
                EGMStatus.GetInstance().betxline--;
            }
            else
            {
                EGMStatus.GetInstance().betxline = EGMSettings.GetInstance().maxBetxline;
            }

            UIPriorityEvent(this, EventType.BetMinusPressed, new EventArgs());
        }

        #endregion
        /// <summary>
        /// {{ GET }}
        /// Used by GameGUIController. Get the current game UI State. Tilts got from EGMStatus
        /// </summary>
        /// <returns></returns>
        internal GameUIState Game_getGameUIState()
        {
            /* Instancio una clase para estructura respuesta */
            GameUIState guis = new GameUIState();  //leak risk
            guis.betPerLines = EGMStatus.GetInstance().betxline;
            guis.linesBet = EGMStatus.GetInstance().betLines;
            guis.selectedCreditValue = EGMStatus.GetInstance().selectedCreditValue;
            guis.totalBet = guis.betPerLines * guis.linesBet;
            guis.totalBetAmount = guis.totalBet * guis.selectedCreditValue;
            try { guis.lastWon = EGMStatus.GetInstance().current_play.CreditsWon; } catch { guis.lastWon = 0; }
            try { guis.lastBaseWon = EGMStatus.GetInstance().current_play.BaseCreditsWon; } catch { guis.lastWon = 0; }
            try { guis.lastScatterWon = EGMStatus.GetInstance().current_play.ScatterCreditsWon; } catch { guis.lastWon = 0; }
            try { guis.lastExpandedWon = EGMStatus.GetInstance().current_play.ExpandedCreditsWon; } catch { guis.lastWon = 0; }
            try { guis.lastMisteryWon = EGMStatus.GetInstance().current_play.MisteryCreditsWon; } catch { guis.lastWon = 0; }
            try { guis.lastActionGameWon = EGMStatus.GetInstance().current_play.ActionGameCreditsWon; } catch { guis.lastWon = 0; }
            guis.extraInfo = EGMStatus.GetInstance().extra_info_list.ToArray();
            if ((EGMStatus.GetInstance().current_play.payline1amount != 0
           || EGMStatus.GetInstance().current_play.payline2amount != 0
           || EGMStatus.GetInstance().current_play.payline3amount != 0
           || EGMStatus.GetInstance().current_play.payline4amount != 0
           || EGMStatus.GetInstance().current_play.payline5amount != 0) &&
           EGMStatus.GetInstance().frontend_play.thisstatus != FrontEndPlayStatus.Playing)
            {
                if (!lastinfo.Contains("Line") || !lastinfo.Contains("pays"))
                {
                    lastinfo = "";
                }
                guis.totalWon = EGMStatus.GetInstance().current_play.payline1amount + EGMStatus.GetInstance().current_play.payline2amount + EGMStatus.GetInstance().current_play.payline3amount + EGMStatus.GetInstance().current_play.payline4amount + EGMStatus.GetInstance().current_play.payline5amount;
                guis.info = (EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.WinningState) ?
                            ((EGMStatus.GetInstance().current_play.payline1amount != 0 ? $";Line 1 pays R{EGMStatus.GetInstance().current_play.payline1amount}" : "") +
                            (EGMStatus.GetInstance().current_play.payline2amount != 0 ? $";Line 2 pays R{EGMStatus.GetInstance().current_play.payline2amount}" : "") +
                            (EGMStatus.GetInstance().current_play.payline3amount != 0 ? $";Line 3 pays R{EGMStatus.GetInstance().current_play.payline3amount}" : "") +
                            (EGMStatus.GetInstance().current_play.payline4amount != 0 ? $";Line 4 pays R{EGMStatus.GetInstance().current_play.payline4amount}" : "") +
                            (EGMStatus.GetInstance().current_play.payline5amount != 0 ? $";Line 5 pays R{EGMStatus.GetInstance().current_play.payline5amount}" : "")) : lastinfo;

            }
            else
            {
                guis.info = "";
            }

            string eventinfo = (SASCTL.GetInstance().AFTInProgress() ? "AFT In Progress (R " + SASCTL.GetInstance().AFTInProgressTotalAmount().ToString() + ")" : "") +
                               (EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.DisabledByHost ? ";Disabled By Host" : "") +
                               (SASCTL.GetInstance().CreditLimitExceeded ? ";Credit Limit Exceeded" : "") +
                               (EGMStatus.GetInstance().cashoutwithrestricted ? ";Remaining credits are non cashable promotional" : "");

            if (eventinfo != "")
            {
                guis.info = eventinfo;
            }
            lastinfo = guis.info;

            /* Current tilts. Get the tilts from EGMStatus and return them in a array*/
            guis.tiltMessages = EGMStatus.GetInstance().currentTilts.Select(t => t.Description).ToArray();
            /* Disabled By Host*/
            guis.disabledByHost = EGMStatus.GetInstance().disabledByHost;
            /* Souns enabled */
            guis.soundEnabled = EGMStatus.GetInstance().soundEnabled;
            /* Menu Active*/
            guis.menuActive = EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.Menu;
            /* Current Logged User */
            guis.currentLoggedUser = EGMStatus.GetInstance().currentLoggedInUser;
            /* Collect In Progress */
            guis.collectInProgress = EGMStatus.GetInstance().current_collect.WorkInProgress();
            /* Handpay Pending */
            guis.handpayPending = EGMStatus.GetInstance().current_handpay.WorkInProgress();
            /* Handpay Pending */
            guis.handpayPendingAmount = 0;
            if (guis.handpayPending)
                guis.handpayPendingAmount = EGMStatus.GetInstance().current_handpay.Amount;
            /* Jackpot Pending */
            guis.jackpotPending = EGMStatus.GetInstance().current_jackpot.WorkInProgress();
            if (guis.jackpotPending)
                guis.jackpotPendingAmount = EGMStatus.GetInstance().current_jackpot.Amount;
            /* PLAYABLE */

            guis.playable = !guis.handpayPending &&
                            !guis.jackpotPending &&
                            !guis.menuActive &&
                            EGMStatus.GetInstance().currentAmount >= guis.totalBetAmount &&
                            !(EGMStatus.GetInstance().betxline * EGMStatus.GetInstance().betLines == 0) &&
                            guis.tiltMessages.Length == 0 &&
                            !EGMStatus.GetInstance().spinstatus.WorkInProgress();

            guis.autoSpin = EGMStatus.GetInstance().autoSpin;

            return guis;
        }



        #region MenuGUI 


        /// <summary>
        /// {{ GET }}
        /// </summary>
        /// Used by MenuGUIController
        /// <returns></returns>
        internal double Menu_GetTheoreticalPayback()
        {
            try
            {
                return math_ctrller.GRHS09LP01_92.GameRTP;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// {{ GET }}
        /// </summary>
        /// Used by MenuGUIController
        /// <returns></returns>
        internal decimal Menu_GetSasReportedDenomination()
        {
            return EGMSettings.GetInstance().sasSettings.mainConfiguration.SASReportedDenomination;
        }
        /// <summary>
        /// {{ GET }}
        /// </summary>
        /// Used by MenuGUIController. It is used to check if can set the date time
        /// <returns></returns>
        internal bool Menu_CanSetDateTime()
        {
            return !EGMStatus.GetInstance().setDateTime;
        }

        /// <summary>
        /// {{ GET }}
        /// Used by MenuGUIController. It is used to check if can set the date time
        /// </summary>
        /// <returns></returns>
        internal bool Menu_CanSetSAS()
        {
            return !EGMStatus.GetInstance().setSAS;
        }
        /// <summary>
        /// {{ GET }}
        /// </summary>
        /// Used by MenuGUIController. It is used to get the mandarotypc status
        /// <returns></returns>
        internal bool Menu_GetMandatoryPC()
        {
            return EGMSettings.GetInstance().MandatoryPC;
        }

        /// <summary>
        /// {{ SET }}
        /// Used by MenuGUIController. Update the DateTime
        /// </summary>
        internal void Menu_SetDateTime(SystemTime systemTime)
        {
            if (!EGMStatus.GetInstance().setDateTime)
            {
                SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.OperatorChangedOptions);
                // Call the unmanaged function that sets the new date and time instantly
                Win32SetSystemTime(ref systemTime);
                // Set flag
                EGMStatus.GetInstance().setDateTime = true;
                EGMDataPersisterCTL.GetInstance().PersistEGMStatus(true);

            }

        }

        /// <summary>
        /// {{ SET }}
        /// Used by MenuGUIController. Make a partial RAM Clear and added a log for it
        /// </summary>
        internal void Menu_RamClearFull()
        {
            UserRole? currentUser = EGMStatus.GetInstance().currentLoggedInUser;
            if (currentUser != null)
            {
                // Reset accounting
                EGMAccounting.GetInstance().resetAccounting();
                // Reset status
                EGMStatus.GetInstance().resetStatus();
                // Reset settings
                EGMSettings.GetInstance().resetSettings();
                // Clear transfer history
                EGMAccounting.GetInstance().transfers.ClearHistory();
                // Clear hanpay history
                EGMAccounting.GetInstance().handpays.ClearHistory();
                // Clear Ram Clear History
                EGMAccounting.GetInstance().ramclears.ClearHistory();
                // Clear last bills history
                EGMAccounting.GetInstance().bills.ClearHistory();
                // Clear system logs
                EGMAccounting.GetInstance().systemlogs.ClearHistory();
                // Clear last plays
                EGMAccounting.GetInstance().plays.ClearHistory();
                // RAM Clear to SASCTL
                SASCTL.GetInstance().RequestFullRAMClear();
                // MandatoryPC
                EGMSettings.GetInstance().MandatoryPC = true;
                // full ram clear performed
                EGMStatus.GetInstance().fullramclearperformed = true;
                // Persist
                EGMDataPersisterCTL.GetInstance().PersistEGMStatus(true);
                EGMDataPersisterCTL.GetInstance().PersistEGMAccounting(true);
                EGMDataPersisterCTL.GetInstance().PersistEGMSettings(true);
                EGMDataPersisterCTL.GetInstance().PersistEGMSlotPlay();
            }
        }
        /// <summary>
        /// {{ SET }}
        /// Used by MenuGUIController. Make a partial RAM Clear and added a log for it
        /// </summary>
        internal void Menu_RamClearPartial()
        {
            UserRole? currentUser = EGMStatus.GetInstance().currentLoggedInUser;
            if (currentUser != null)
            {
                // Add RAMClear to history
                EGMAccounting.GetInstance().ramclears.AddNewRamClear(new RamClearLog(Enum.GetName(typeof(UserRole), currentUser.Value), DateTime.Now, FromSASFormat(EGM_GetMeter(0x00)), FromSASFormat(EGM_GetMeter(0x01)), EGM_GetMeter(0x05)), true);
                // Reset accounting
                EGMAccounting.GetInstance().resetAccounting();
                // Reset status
                EGMStatus.GetInstance().resetStatus();
                // Clear transfer history
                EGMAccounting.GetInstance().transfers.ClearHistory();
                // Clear hanpay history
                EGMAccounting.GetInstance().handpays.ClearHistory();
                // Clear last bills history
                EGMAccounting.GetInstance().bills.ClearHistory();
                // Clear system logs
                EGMAccounting.GetInstance().systemlogs.ClearHistory();
                // Add System Log
                AddSystemLog("Partial Ram Clear", EGMStatus.GetInstance().currentLoggedInUser.ToString());
                // Clear last plays
                EGMAccounting.GetInstance().plays.ClearHistory();
                // RAM Clear to SASCTL
                SASCTL.GetInstance().RequestPartialRAMClear();
                // MandatoryPC
                EGMSettings.GetInstance().MandatoryPC = true;
                // Persist
                EGMDataPersisterCTL.GetInstance().PersistEGMStatus(true);
                EGMDataPersisterCTL.GetInstance().PersistEGMAccounting(true);
                EGMDataPersisterCTL.GetInstance().PersistEGMSlotPlay();
            }
        }

        /// <summary>
        /// {{ GET }}
        /// </summary>
        /// Used by MenuGUIController. Get the RAMClear History
        /// <returns></returns>
        internal List<RamClearLog> Menu_GetRamClearHistory()
        {
            return EGMAccounting.GetInstance().ramclears.GetRamClearHistory();
        }

        /// <summary>
        /// {{ GET }}
        /// </summary>
        /// Used by MenuGUIController. Get the Handpay History
        /// <returns></returns>
        internal List<HandpayTransaction> Menu_GetHandpayHistory()
        {
            return EGMAccounting.GetInstance().handpays.GetHandpayHistory();
        }

        /// <summary>
        ///  {{ GET }}
        /// </summary>
        /// Used by MenuGUIController. Get the warning and errors history
        /// <returns></returns>
        internal WarningAndErrorStatistic Menu_GetWarningAndErrors()
        {
            return new WarningAndErrorStatistic(EGMAccounting.GetInstance().GetMeter("BasicSlotDoorOpen"), // OK
                0,
                0,
                EGMAccounting.GetInstance().GetMeter("BasicLogicDoorOpen"), // OK
                EGMAccounting.GetInstance().GetMeter("BasicCashboxDoorOpen"),
                EGMAccounting.GetInstance().GetMeter("BasicDropDoorOpen"), // OK
                EGMAccounting.GetInstance().GetMeter("BasicStackerOpen"), // OK
                EGMAccounting.GetInstance().GetMeter("BillsJammed"), // OK
                EGMAccounting.GetInstance().GetMeter("SASInterfaceError"), // OK
                EGMAccounting.GetInstance().ramclears.GetRamClearHistory().Count(), // OK
                EGMAccounting.GetInstance().GetMeter("GeneralTilt")); // OK

        }

        /// <summary>
        /// {{ GET }}
        /// </summary>
        /// Used by MenuGUIController. Get the bill denomination configuration
        /// <returns></returns>
        internal List<Channel> Menu_GetBillDenominations()
        {
            List<Channel> billvalidatorChannels = new List<Channel>();
            foreach (int c in billAcc.channelamounts.Keys)
            {
                billvalidatorChannels.Add(new Channel(c,
                                                   true,
                                                   0,
                                                   EGMSettings.GetInstance().EnabledChannels(billAcc.channelamounts[c]),
                                                   billAcc.channelamounts[c]));
            }

            return billvalidatorChannels;
        }

        /// <summary>
        /// {{ SET }}
        /// </summary>
        /// Used by MenuGUIController. Set the bill denomination configuration
        /// <returns></returns>
        internal void Menu_SetBillDenomination(List<Channel> Channels)
        {
            byte lsb = 0x00;

            byte snd_byte = 0x00;

            byte thrd_byte = 0x00;
            foreach (Channel c in Channels)
            {
                if (c.denomination == 1 && c.HostEnabled)
                    lsb |= (byte)Math.Pow(2, 0);
                if (c.denomination == 2 && c.HostEnabled)
                    lsb |= (byte)Math.Pow(2, 1);
                if (c.denomination == 5 && c.HostEnabled)
                    lsb |= (byte)Math.Pow(2, 2);
                if (c.denomination == 10 && c.HostEnabled)
                    lsb |= (byte)Math.Pow(2, 3);
                if (c.denomination == 20 && c.HostEnabled)
                    lsb |= (byte)Math.Pow(2, 4);
                if (c.denomination == 25 && c.HostEnabled)
                    lsb |= (byte)Math.Pow(2, 5);
                if (c.denomination == 50 && c.HostEnabled)
                    lsb |= (byte)Math.Pow(2, 6);
                if (c.denomination == 100 && c.HostEnabled)
                    lsb |= (byte)Math.Pow(2, 7);


                if (c.denomination == 200 && c.HostEnabled)
                    snd_byte |= (byte)Math.Pow(2, 0);
                if (c.denomination == 250 && c.HostEnabled)
                    snd_byte |= (byte)Math.Pow(2, 1);
                if (c.denomination == 500 && c.HostEnabled)
                    snd_byte |= (byte)Math.Pow(2, 2);
                if (c.denomination == 1000 && c.HostEnabled)
                    snd_byte |= (byte)Math.Pow(2, 3);
                if (c.denomination == 2000 && c.HostEnabled)
                    snd_byte |= (byte)Math.Pow(2, 4);
                if (c.denomination == 2500 && c.HostEnabled)
                    snd_byte |= (byte)Math.Pow(2, 5);
                if (c.denomination == 5000 && c.HostEnabled)
                    snd_byte |= (byte)Math.Pow(2, 6);
                if (c.denomination == 10000 && c.HostEnabled)
                    snd_byte |= (byte)Math.Pow(2, 7);

                if (c.denomination == 20000 && c.HostEnabled)
                    thrd_byte |= (byte)Math.Pow(2, 0);
                if (c.denomination == 25000 && c.HostEnabled)
                    thrd_byte |= (byte)Math.Pow(2, 1);
                if (c.denomination == 50000 && c.HostEnabled)
                    thrd_byte |= (byte)Math.Pow(2, 2);
                if (c.denomination == 100000 && c.HostEnabled)
                    thrd_byte |= (byte)Math.Pow(2, 3);
                if (c.denomination == 200000 && c.HostEnabled)
                    thrd_byte |= (byte)Math.Pow(2, 4);
                if (c.denomination == 250000 && c.HostEnabled)
                    thrd_byte |= (byte)Math.Pow(2, 5);
                if (c.denomination == 500000 && c.HostEnabled)
                    thrd_byte |= (byte)Math.Pow(2, 6);
                if (c.denomination == 1000000 && c.HostEnabled)
                    thrd_byte |= (byte)Math.Pow(2, 7);

            }

            EGMSettings.GetInstance().BillAcceptorChannelSet1 = lsb;
            EGMSettings.GetInstance().BillAcceptorChannelSet2 = snd_byte;
            EGMSettings.GetInstance().BillAcceptorChannelSet3 = thrd_byte;


            billAcc.ConfigBillDenominations(lsb, snd_byte, thrd_byte);

        }



        /// <summary>
        /// {{ GET }}
        /// Used by MenuGUIController. Get the transaction history from EGMAccounting
        /// </summary>
        /// <returns></returns>
        internal List<AccountingAFTTransfer> Menu_GetAFTTransactionHistory()
        {
            return EGMAccounting.GetInstance().transfers.GetTranfersHistory();
        }

        /// <summary>
        /// {{ GET }}
        /// Used by MenuGUIController. Get the Bill Validator Logs from bill acceptor
        /// </summary>
        /// <returns></returns>
        internal List<BillValidatorLog> Menu_GetBillValidatorLogs()
        {
            return billAcc.GetValidatorLogs();
        }

        /// <summary>
        /// {{ GET }}
        /// Used by MenuGUIController. Get the Menu Info data
        /// </summary>
        /// <returns></returns>
        internal String Menu_Get_MenuInfoLabel()
        {
            return !EGMSettings.GetInstance().sasSettings.SASConfigured ? "Post ram clear configuration pending;" : "";
        }

        /// <summary>
        /// {{ GET }}
        /// Used by MenuGUIController. Get the System Logs 
        /// </summary>
        /// <returns></returns>
        internal List<SystemLog> Menu_GetSystemLogs()
        {
            return EGMAccounting.GetInstance().systemlogs.GetSystemLogs();
        }


        /// <summary>
        /// {{ GET }}
        /// Used by MenuGUIController. Get the Last Bills from EGMAccounting
        /// </summary>
        /// <returns></returns>
        internal List<LastBill> Menu_GetLastBills()
        {
            return EGMAccounting.GetInstance().bills.GetLastBills();
        }

        /// <summary>
        /// {{ GET }}
        /// Used by MenuGUIController. Get the Last Plays from EGMAccounting
        /// </summary>
        /// <returns></returns>
        internal List<LastPlay> Menu_GetLastPlays()
        {
            return EGMAccounting.GetInstance().plays.GetLastPlays();
        }

        /// <summary>
        /// {{ GET }}
        /// User by MenuGUIController
        /// </summary>
        /// <returns></returns>
        internal bool Menu_GETMaintenanceStatus()
        {
            return EGMStatus.GetInstance().maintenanceMode;
        }

        /// <summary>
        /// User by MenuGUIController
        /// </summary>
        /// <returns></returns>
        internal Configuration_BetAndMoneyLimitConfiguration Menu_GetBetAndMoneyLimits()
        {
            Configuration_BetAndMoneyLimitConfiguration result = new Configuration_BetAndMoneyLimitConfiguration();

            result.jackpotEnabled = EGMSettings.GetInstance().jackpotEnabled;
            result.jackpotLimit = EGMSettings.GetInstance().jackpotLimit;
            result.maxLoad = EGMSettings.GetInstance().creditLimit;




            return result;
        }

        /// <summary>
        /// User by MenuGUIController
        /// </summary>
        /// <returns></returns>
        internal void Menu_SetBetAndMoneyLimits(Configuration_BetAndMoneyLimitConfiguration config)
        {
            EGMSettings.GetInstance().jackpotEnabled = config.jackpotEnabled;
            EGMSettings.GetInstance().jackpotLimit = config.jackpotLimit;
            EGMSettings.GetInstance().creditLimit = config.maxLoad; SASCTL.GetInstance().UpdateSASInfo(null, EGMSettings.GetInstance().creditLimit, null, null, null, null, null);
        }


        /// <summary>
        /// {{ SET }}
        /// Used by MenuGUIController. Updates password for specific role. Based on currentloggedinuser in EGMStatus and update the password in EGMSettings
        /// </summary>
        /// <param name="role"></param>
        /// <param name="newpin"></param>
        internal UserPwdResponse Menu_UpdatePassword(UserRole role, int newpin)
        {

            if (EGMStatus.GetInstance().currentLoggedInUser == UserRole.Manufacturer || EGMStatus.GetInstance().currentLoggedInUser == UserRole.Operator)
            {
                if ((new int[]
                {
                    EGMSettings.GetInstance().AttendantPin,
                    EGMSettings.GetInstance().OperatorPin,
                    EGMSettings.GetInstance().ManufacturerPin,
                    EGMSettings.GetInstance().TechnicianPin
                }).Contains(newpin))
                {
                    ReprocessEGMStatus_Frontend();
                    return UserPwdResponse.PinAlreadyUsed;
                }

                if (role == UserRole.Attendant)
                {
                    // Update Attendant pin
                    EGMSettings.GetInstance().AttendantPin = newpin;
                }
                else if (role == UserRole.Operator)
                {
                    // Update Operator pin
                    EGMSettings.GetInstance().OperatorPin = newpin;
                }
                else if (role == UserRole.Manufacturer)
                {
                    // Update Manufacturer pin
                    EGMSettings.GetInstance().ManufacturerPin = newpin;
                }
                else if (role == UserRole.Technician)
                {
                    // Update Technician pin
                    EGMSettings.GetInstance().TechnicianPin = newpin;
                }

                if (EGMSettings.GetInstance().AttendantPin < 346932164 &&
                    EGMSettings.GetInstance().OperatorPin < 346932164 &&
                    EGMSettings.GetInstance().TechnicianPin < 346932164)
                {
                    EGMStatus.GetInstance().fullramclearperformed = false;
                }

                SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.OperatorChangedOptions);

                AddSystemLog($"Role {role.ToString()} PIN updated", EGMStatus.GetInstance().currentLoggedInUser.ToString());
                ReprocessEGMStatus_Frontend();

                return UserPwdResponse.Success;

            }
            else
            {
                ReprocessEGMStatus_Frontend();
                return UserPwdResponse.UNAUTHORIZED;
            }


        }

        /// <summary>
        /// {{ SET }}
        /// Used by MenuGUIController. Used to generate event for menu exit to main game
        /// </summary>
        internal void Menu_ExitToGame(bool sasexception)
        {

            // Set false the menuActive
            EGMStatus.GetInstance().menuActive = false;
            if (sasexception)
            {
                // Send MenuExited exception
                SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.AttendantMenuExited);
            }
            // Null the current logged in user
            EGMStatus.GetInstance().currentLoggedInUser = null;
        }

        /// <summary>
        /// {{ SET }}
        /// Used by MenuGUIController. Used to generate event for entering the main menu
        /// </summary>
        internal void Menu_EnterToMenu()
        {

            EGMStatus.GetInstance().menuActive = true;

        }

        /// <summary>
        /// {{ SET }}
        /// Used by MenuGUIController. Used to enter the maintenance mode
        /// </summary>
        internal void Menu_EnterMaintenanceMode()
        {

            if (EGMStatus.GetInstance().maintenanceMode == false)
            {
                EnterMaintenanceMode();
                SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.GameIsOutOfServiceByAttendant);
            }
        }

        /// <summary>
        /// {{ SET }}
        /// Used by MenuGUIController. Used to exit the maintenance mode
        /// </summary>
        internal void Menu_ExitMaintenanceMode()
        {

            EGMStatus.GetInstance().maintenanceMode = false;
            SASCTL.GetInstance().SetMaintenanceMode(false);
        }

        /// <summary>
        /// {{ SET }}
        /// Used by MenuGUIController. Update the CashinCashout Configuration in  EGMSettings. Persist the data
        /// </summary>
        /// <param name="input"></param>
        internal void Menu_UpdateCashinCashoutConfiguration(Configuration_CashInCashOutConfiguration input)
        {
            EGMSettings.GetInstance().HandpayEnabled = input.HandpayEnabled;
            if (EGMSettings.GetInstance().BillAcceptor != input.BillAcceptor)
            {
                EGMSettings.GetInstance().BillAcceptor = input.BillAcceptor;
                if (EGMSettings.GetInstance().BillAcceptor)
                {
                    billAcc.Enabled = true;
                }
                else
                {
                    billAcc.Enabled = false;

                }
            }
            if (EGMSettings.GetInstance().AFTEnabled != input.AFTEnabled)
            {
                EGMSettings.GetInstance().AFTEnabled = input.AFTEnabled;
                if (EGMSettings.GetInstance().AFTEnabled)
                {
                    SASCTL.GetInstance().EnableAFT();
                }
                else
                {
                    SASCTL.GetInstance().DisableAFT();

                }
            }
            EGMSettings.GetInstance().PartialPay = input.PartialPay;

            SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.OperatorChangedOptions);
        }

        /// <summary>
        /// {{ SET }}
        /// Used by MenuGUIController. Update the SAS Configuration in EGMSettings. Persist the data
        /// </summary>
        /// <param name="input"></param>
        internal void Menu_UpdateSASConfiguration(Configuration_SASConfiguration input)
        {
            if (!EGMStatus.GetInstance().setSAS)
            {
                EGMStatus.GetInstance().setSAS = true;
                if (EGMSettings.GetInstance().sasSettings.mainConfiguration.SASEnabled != input.MainConfiguration.SASEnabled)
                {
                    EGMSettings.GetInstance().sasSettings.mainConfiguration.SASEnabled = input.MainConfiguration.SASEnabled;
                    if (EGMSettings.GetInstance().sasSettings.mainConfiguration.SASEnabled)
                    {
                        SASCTL.GetInstance().EnableSAS(true);
                        SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.SoftMetersToZero);
                        SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.ACPowerLostFromGamingMachine);
                        SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.ACPowerAppliedToGamingMachine);

                    }
                    else
                    {
                        SASCTL.GetInstance().DisableSAS();
                        EGMSettings.GetInstance().sasSettings.mainConfiguration.SASId = 0;
                    }
                }
            }
            EGMSettings.GetInstance().sasSettings.SASConfigured = true;
            EGMSettings.GetInstance().sasSettings.mainConfiguration.TiltOnASASDisconnection = input.MainConfiguration.TiltOnASASDisconnection;
            EGMSettings.GetInstance().sasSettings.mainConfiguration.SerialNumber = input.MainConfiguration.SerialNumber; SASCTL.GetInstance().SetSerialNumber(EGMSettings.GetInstance().sasSettings.mainConfiguration.SerialNumber);
            EGMSettings.GetInstance().sasSettings.mainConfiguration.AccountingDenomination = input.MainConfiguration.AccountingDenomination;
            EGMSettings.GetInstance().sasSettings.mainConfiguration.InHouseInLimit = input.MainConfiguration.InHouseInLimit;
            EGMSettings.GetInstance().sasSettings.mainConfiguration.InHouseOutLimit = input.MainConfiguration.InHouseOutLimit; SASCTL.GetInstance().UpdateSASInfo(null, null, null, null, null, EGMSettings.GetInstance().sasSettings.mainConfiguration.InHouseInLimit, EGMSettings.GetInstance().sasSettings.mainConfiguration.InHouseOutLimit);
            // Update the SAS Id (SAS Address)
            EGMSettings.GetInstance().sasSettings.mainConfiguration.SASId = input.MainConfiguration.SASId; LinkEGMSettingsSASIdToSASAddress();
            EGMSettings.GetInstance().sasSettings.mainConfiguration.AccountingDenomination = input.MainConfiguration.AccountingDenomination;
            // Update the asset number to SASCTL
            EGMSettings.GetInstance().sasSettings.mainConfiguration.AssetNumber = input.MainConfiguration.AssetNumber; SASCTL.GetInstance().SetAssetNumber(EGMSettings.GetInstance().sasSettings.mainConfiguration.AssetNumber);
            EGMSettings.GetInstance().sasSettings.mainConfiguration.SASReportedDenomination = input.MainConfiguration.SASReportedDenomination;
            EGMSettings.GetInstance().sasSettings.hostConfiguration.BillAcceptorEnabled = input.HostConfiguration.BillAcceptorEnabled;
            EGMSettings.GetInstance().sasSettings.hostConfiguration.RealTimeModeEnabled = input.HostConfiguration.RealTimeModeEnabled;
            EGMSettings.GetInstance().sasSettings.hostConfiguration.SoundEnabled = input.HostConfiguration.SoundEnabled;
            EGMSettings.GetInstance().sasSettings.hostConfiguration.TicketsForForeignRestrictedAmounts = input.HostConfiguration.TicketsForForeignRestrictedAmounts;
            EGMSettings.GetInstance().sasSettings.hostConfiguration.TicketsRedemptionEnabled = input.HostConfiguration.TicketsRedemptionEnabled;
            EGMSettings.GetInstance().sasSettings.hostConfiguration.ValidateHandpaysReceipts = input.HostConfiguration.ValidateHandpaysReceipts;
            EGMSettings.GetInstance().sasSettings.vltConfiguration.AccountingDenomination = input.VLTConfig.AccountingDenomination;
            EGMSettings.GetInstance().sasSettings.vltConfiguration.AdditionalID = input.VLTConfig.AdditionalID;
            EGMSettings.GetInstance().sasSettings.vltConfiguration.AuthenticationEnabled = input.VLTConfig.AuthenticationEnabled;
            EGMSettings.GetInstance().sasSettings.vltConfiguration.ExtendedMetersEnabled = input.VLTConfig.ExtendedMetersEnabled;
            EGMSettings.GetInstance().sasSettings.vltConfiguration.MultidenominationEnabled = input.VLTConfig.MultidenominationEnabled;
            EGMSettings.GetInstance().sasSettings.vltConfiguration.SASVersion = input.VLTConfig.SASVersion;
            EGMSettings.GetInstance().sasSettings.vltConfiguration.TicketsCounterEnabled = input.VLTConfig.TicketsCounterEnabled;

            SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.OperatorChangedOptions);
            RemoveTilt("Post ram clear configuration pending");
            // Set SAS Game Details
            UpdateSASGameDetails();
        }
        internal void Enter_View_Statistic_LastPlay()
        {
            EGMStatus.GetInstance().lastplayonview = true;
        }

        internal void Exit_View_Statistic_LastPlay()
        {
            EGMStatus.GetInstance().lastplayonview = false;
        }

        internal bool On_View_Statistic_LastPlay()
        {
            return EGMStatus.GetInstance().lastplayonview;
        }


        /// <summary>
        /// {{ SET }}
        /// Used by MenuGUIController. Tries to update the currentLoggedInUser (Log in) by a pin passed as argument
        /// </summary>
        /// <param name="pin"></param>
        /// <returns></returns>
        internal UserRole EGM_AttemptLogin(int pin)
        {
            UserRole result = UserRole.UNAUTHORIZED;
            // If pin is the attendant pin
            if (pin == EGMSettings.GetInstance().AttendantPin)
            {
                EGMStatus.GetInstance().currentLoggedInUser = UserRole.Attendant;
                result = UserRole.Attendant;
            }
            // If pin is the operator pin
            else if (pin == EGMSettings.GetInstance().OperatorPin)
            {
                EGMStatus.GetInstance().currentLoggedInUser = UserRole.Operator;
                result = UserRole.Operator;
            }
            // If pin is the manufacturer pin
            else if (pin == EGMSettings.GetInstance().ManufacturerPin)
            {
                EGMStatus.GetInstance().currentLoggedInUser = UserRole.Manufacturer;
                result = UserRole.Manufacturer;

            }
            // If pin is the technician pin
            else if (pin == EGMSettings.GetInstance().TechnicianPin)
            {
                EGMStatus.GetInstance().currentLoggedInUser = UserRole.Technician;
                result = UserRole.Technician;
            }

            if (result != UserRole.UNAUTHORIZED) // Menu should be entered. Send exception
            {
                // Add System Log
                AddSystemLog("Menu Login", EGMStatus.GetInstance().currentLoggedInUser.ToString());
                // Send MenuEntered exception
                SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.AttendantMenuEntered);

            }

            return result;
        }



        /// <summary>
        /// {{ GET }}
        /// Used by MenuGUIController. Current Logged User
        /// </summary>
        /// <returns></returns>
        internal UserRole EGM_GetLoggedUser()
        {
            if (EGMStatus.GetInstance().currentLoggedInUser != null)
                return EGMStatus.GetInstance().currentLoggedInUser.Value;
            return UserRole.UNAUTHORIZED;




        }

        /// <summary>
        /// 
        /// 
        /// Current Credits for websocket
        /// 
        public decimal EGM_GetCurrentCredits()
        {
            return EGMStatus.GetInstance().currentAmount;
        }

        /// <summary>
        /// {{ GET }}
        /// Used by MenuGUIController. Get the meter by code
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public int EGM_GetMeter(byte code)
        {
            return EGMAccounting.GetInstance().GetMeter(code);
        }



        /// <summary>
        /// {{ GET }}
        /// Used by MenuGUIController. Get EGMSettings
        /// </summary>
        /// <returns></returns>
        public EGMSettings EGM_GetEGMSettings()
        {
            return EGMSettings.GetInstance();
        }


        /// <summary>
        /// {{ GET }}
        /// Method for set all outputs off on GPIO Controller;
        /// It is used by MenuGUIController to serve to the GUI Consumer a way to set all outputs off on GPIO Board
        /// </summary>
        internal void GPIO_SetAllOutputsOff()
        {
            SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.OperatorChangedOptions);

            gpioCTL.SetAllOutputOff();
        }


        /// <summary>
        /// {{ GET }}
        /// Method for get the output status;
        /// It is used by MenuGUIController to serve to the GUI Consumer a way to get the output status from GPIO Board (on or off)
        /// </summary>
        /// <param name="output">The output name</param>
        /// <returns>The output status</returns>
        internal bool GPIO_GetOutputStatus(OutputName output)
        {
            return gpioCTL.GetOutputStatus(output);
        }

        /// <summary>
        /// {{ GET }}
        /// Method for get the sensor status;
        /// It is used by MenuGUIController to serve to the GUI Consumer a way to get the sensor status from GPIO Board (open or closed)
        /// </summary>
        /// <param name="sensor">The sensor name</param>
        /// <returns>The output status</returns>
        internal bool GPIO_GetSensorStatus(SensorName sensor)
        {
            return gpioCTL.GetSensorStatus(sensor);
        }

        /// <summary>
        /// {{ GET }}
        /// Method for get the button status;
        /// It is used by MenuGUIController to serve to the GUI Consumer a way to get the button status from GPIO Board (pressed or released)
        /// </summary>
        /// <param name="button">The button name</param>
        /// <returns>The output status</returns>
        internal bool GPIO_GetButtonStatus(ButtonName button)
        {
            return gpioCTL.GetButtonStatus(button);
        }


        /// <summary>
        /// {{ GET }}
        /// Method for set a single output status;
        /// It is used by MenuGUIController to serve to the GUI Consumer a way to set a single output status on GPIO Board
        /// </summary>
        /// <param name="output">Output name</param>
        /// <param name="on">It this value is true, the output should be on. Otherwise, the output should be off</param>
        internal void GPIO_SetOutputStatus(OutputName output, bool on)
        {
            SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.OperatorChangedOptions);

            if (on)
                gpioCTL.SetOutputOn(output);
            else
                gpioCTL.SetOutputOff(output);
        }

        internal Statistics_BillsByDenomination Menu_BillsByDenomination()
        {
            Statistics_BillsByDenomination result = new Statistics_BillsByDenomination();

            result.Bill10 = (int)EGMAccounting.GetInstance().bills.GetLastBills().Where(b => b.Denomination == 10).Count();
            result.Bill20 = (int)EGMAccounting.GetInstance().bills.GetLastBills().Where(b => b.Denomination == 20).Count();
            result.Bill50 = (int)EGMAccounting.GetInstance().bills.GetLastBills().Where(b => b.Denomination == 50).Count();
            result.Bill100 = (int)EGMAccounting.GetInstance().bills.GetLastBills().Where(b => b.Denomination == 100).Count();
            result.Bill200 = (int)EGMAccounting.GetInstance().bills.GetLastBills().Where(b => b.Denomination == 200).Count();
            result.Bill500 = (int)EGMAccounting.GetInstance().bills.GetLastBills().Where(b => b.Denomination == 500).Count();
            result.Bill1000 = (int)EGMAccounting.GetInstance().bills.GetLastBills().Where(b => b.Denomination == 1000).Count();

            return result;
        }

        #endregion

        #endregion



    }


}

