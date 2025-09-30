using EGMENGINE.GLOBALTYPES;
using System;
using System.Collections.Generic;
using System.Text;

namespace EGMENGINE.GUI.MENUTYPES
{
    #region Game Configuration

    public class GameConfiguration_SystemId
    {
        public string systemId { get; set; }
        public GameConfiguration_SystemId()
        {
        }

    }

    public class GameConfiguration_BillValidatorFirmware
    {
        public string BVFirmware { get; set; }
        public GameConfiguration_BillValidatorFirmware()
        {
        }
    }

    public class GameConfiguration_ROMId
    {
        public string ROMId { get; set; }
        public string ROMId_SHA1 { get; set; }
        public string ROMId_MD5 { get; set; }

        public GameConfiguration_ROMId()
        {
        }
    }

    #endregion
    #region Configuration

    #region BetAndMoneyLimitConfiguration
    public class Configuration_BetAndMoneyLimitConfiguration
    {
        public decimal jackpotLimit;
        public bool jackpotEnabled;
        public decimal maxLoad;
        public Configuration_BetAndMoneyLimitConfiguration()
        {
            jackpotLimit = 0;
            jackpotEnabled = false;
            maxLoad = 0;
        }
    }

    #endregion

    #region DateTimeConfiguration

    public class Configuration_DateTimeConfiguration
    {
        public int Year;

        public int Month;
        public int Day;
        public int Hour;
        public int Minute;

        public Configuration_DateTimeConfiguration()
        {
            Year = 0;
            Month = 0;
            Day = 0;
            Hour = 0;
            Minute = 0;
        }
    }

    #endregion
    #region SASConfiguration

    public struct SASHostConfig
    {
        public SASHostConfig(bool _BillAcceptorEnabeld, bool _SoundEnabled, bool _RealTimeModeEnabled, bool _ValidateHandpaysReceipts, bool _TicketsForForeignRestrictedAmounts, bool _TicketsRedemptionEnabled)
        {
            BillAcceptorEnabled = _BillAcceptorEnabeld;
            SoundEnabled = _SoundEnabled;
            RealTimeModeEnabled = _RealTimeModeEnabled;
            ValidateHandpaysReceipts = _ValidateHandpaysReceipts;
            TicketsForForeignRestrictedAmounts = _TicketsForForeignRestrictedAmounts;
            TicketsRedemptionEnabled = _TicketsRedemptionEnabled;

        }
        public bool BillAcceptorEnabled { get; set; }
        public bool SoundEnabled { get; set; }
        public bool RealTimeModeEnabled { get; set; }
        public bool ValidateHandpaysReceipts { get; set; }
        public bool TicketsForForeignRestrictedAmounts { get; set; }
        public bool TicketsRedemptionEnabled { get; set; }

    }

    public struct SASMainConfig
    {
        public SASMainConfig(bool _SASEnabled, int _SASId, int _AssetNumber,int _SerialNumber, bool _TiltOnASASDisconnection, decimal _AccountingDenomination, decimal _SASReportedDenomination, decimal _InHouseInLimit, decimal _InHouseOutLimit)
        {
            SASEnabled = _SASEnabled;
            SASId = _SASId;
            AssetNumber = _AssetNumber;
            SerialNumber = _SerialNumber;
            TiltOnASASDisconnection = _TiltOnASASDisconnection;
            AccountingDenomination = _AccountingDenomination;
            SASReportedDenomination = _SASReportedDenomination;
            InHouseInLimit = _InHouseInLimit;
            InHouseOutLimit = _InHouseOutLimit;
        }

        public bool SASEnabled { get; set; }
        public int SASId { get; set; }
        public int AssetNumber { get; set; }
        public int SerialNumber { get; set; }
        public bool TiltOnASASDisconnection { get; set; }
        public decimal AccountingDenomination { get; set; }
        public decimal SASReportedDenomination { get; set; }
        public decimal InHouseInLimit { get; set; }
        public decimal InHouseOutLimit { get; set; }


    }

    public struct SASVLTConfig
    {
        public SASVLTConfig(string _SASVersion, string _GameID, string _AdditionalID, decimal _AccountingDenomination, bool _MultidenominationEnabled, bool _AuthenticationEnabled, bool _ExtendedMetersEnabled, bool _TicketsCounterEnabled)
        {
            SASVersion = _SASVersion;
            GameID = _GameID;
            AdditionalID = _AdditionalID;
            AccountingDenomination = _AccountingDenomination;
            MultidenominationEnabled = _MultidenominationEnabled;
            AuthenticationEnabled = _AuthenticationEnabled;
            ExtendedMetersEnabled = _ExtendedMetersEnabled;
            TicketsCounterEnabled = _TicketsCounterEnabled;

        }

        public string SASVersion { get; set; }
        public string GameID { get; set; }
        public string AdditionalID { get; set; }
        public decimal AccountingDenomination { get; set; }
        public bool MultidenominationEnabled { get; set; }
        public bool AuthenticationEnabled { get; set; }
        public bool ExtendedMetersEnabled { get; set; }
        public bool TicketsCounterEnabled { get; set; }

    }


    public class Configuration_SASConfiguration
    {
        public SASHostConfig HostConfiguration;
        public SASMainConfig MainConfiguration;
        public SASVLTConfig VLTConfig;
    }


    #endregion
    #region BillsConfiguration
    /*
     * 
     * Bills Configuration
     * 
     */


    public struct Channel
    {
        public Channel(int channelNum_, bool hardwareEnabled_, int credit_, bool hostEnabled_, int denomination_)
        {
            channelNum = channelNum_; ;
            HardwareEnabled = hardwareEnabled_;
            credit = credit_;
            HostEnabled = hostEnabled_;
            denomination = denomination_;
        }


        public int channelNum { get; }
        public bool HardwareEnabled { get; set; }
        public int credit { get; set; }
        public bool HostEnabled { get; set; }
        public int denomination { get; set; }

    }

    public class Configuration_BillsConfiguration
    {
        public Dictionary<int, Channel> channels;

        public Configuration_BillsConfiguration()
        {
            channels = new Dictionary<int, Channel>();
        }
    }

    #endregion

    #region CashIn CashOut Configuration

    public class Configuration_CashInCashOutConfiguration
    {
        public bool PartialPay;
        public bool BillAcceptor;
        public bool AFTEnabled;
        public bool HandpayEnabled;
        public string CashOutOrder;

        public Configuration_CashInCashOutConfiguration()
        {
            PartialPay = true;
            BillAcceptor = false;
            AFTEnabled = false;
            HandpayEnabled = false;
            CashOutOrder = "Aft/Handpay";
        }
    }

    #endregion

    #endregion

    #region Statistics

    #region Last Plays


    public class Statistics_LastPlays
    {
        public List<LastPlay> lastPlays;

        public Statistics_LastPlays()
        {
            lastPlays = new List<LastPlay>();
        }
    }

    #endregion

    #region General Game Statistics

    public struct GameGeneralStatisticDetail
    {
        public GameGeneralStatisticDetail(decimal amount_, int quantities_)
        {
            amount = amount_;
            quantities = quantities_;
        }


        public decimal amount { get; }
        public int quantities { get; }

    }
    public class Statistics_GeneralGameStatistics
    {
        public GameGeneralStatisticDetail BillsIn;
        public GameGeneralStatisticDetail TotalCreditsFromBillAccepted;
        public GameGeneralStatisticDetail BillsInStacker;
        public GameGeneralStatisticDetail AFTIn;
        public GameGeneralStatisticDetail AFTOut;
        public GameGeneralStatisticDetail TotalCancelledCredits;
        public GameGeneralStatisticDetail TotalHandpayCancelledCredits;
        public GameGeneralStatisticDetail TotalDrop;
        public GameGeneralStatisticDetail Handpay;
        public GameGeneralStatisticDetail Credits;
        public GameGeneralStatisticDetail CoinIn;
        public GameGeneralStatisticDetail CoinOut;
        public GameGeneralStatisticDetail TotalGames;
        public GameGeneralStatisticDetail TotalGamesWon;
        public GameGeneralStatisticDetail TotalGamesLost;
        public GameGeneralStatisticDetail SincePowerCycle;
        public GameGeneralStatisticDetail SinceDoorClosed;
        public GameGeneralStatisticDetail TheoreticalPayback;
        public GameGeneralStatisticDetail ActualPayback;
    }
    #endregion

    #region System Logs


    public class Statistics_SystemLogs
    {
        private List<SystemLog> logList { get; }
        private int indexer;
        public Statistics_SystemLogs()
        {
            logList = new List<SystemLog>();
        }

        internal void AddLog(DateTime dt, string detail, string user)
        {
            SystemLog log = new SystemLog(dt, detail, user);
            logList.Add(log);

        }
        public List<SystemLog> logs { get => logList; }

    }
    #endregion

    #region Last Bills

    public class Statistics_LastBills
    {
        public List<LastBill> lastbills;

        public Statistics_LastBills()
        {
            lastbills = new List<LastBill>();
        }
    }

    #endregion

    #region Last Bills

    public class Statistics_BillsByDenomination
    {
        public int Bill10;
        public int Bill20;
        public int Bill50;
        public int Bill100;
        public int Bill200;
        public int Bill500;
        public int Bill1000;


        public Statistics_BillsByDenomination()
        {
            Bill10 = 0;
            Bill20 = 0;
            Bill50 = 0;
            Bill100 = 0;
            Bill200 = 0;
            Bill500 = 0;
            Bill50 = 0;

        }
    }

    #endregion

    #region WarningAndErrors

    public struct WarningAndErrorStatistic
    {

        public int MainDoorOpen { get; }
        public int ReinforcementOpen { get; }
        public int IOBoardDoorOpen { get; }
        public int LogicDoorOpen { get; }
        public int CashboxDoorOpen { get; }
        public int DropBoxOpen { get; }
        public int StackerOpen { get; }
        public int BillJam { get; }
        public int SASInterfaceError { get; }
        public int ClearRamPartialCount { get; }
        public int GeneralTilt { get; }

        public WarningAndErrorStatistic(int _MainDoorOpen, int _ReinforcementOpen, int _IOBoardDoorOpen, int _LogicDoorOpen, int _CashboxDoorOpen, int _DropBoxOpen, int _StackerOpen, int _BillJam, int _SASInterfaceError, int _ClearRamPartialCount, int _GeneralTilt)
        {
            MainDoorOpen = _MainDoorOpen;
            ReinforcementOpen = _ReinforcementOpen;
            IOBoardDoorOpen = _IOBoardDoorOpen;
            LogicDoorOpen = _LogicDoorOpen;
            CashboxDoorOpen = _CashboxDoorOpen;
            DropBoxOpen = _DropBoxOpen;
            StackerOpen = _StackerOpen;
            BillJam = _BillJam;
            SASInterfaceError = _SASInterfaceError;
            ClearRamPartialCount = _ClearRamPartialCount;
            GeneralTilt = _GeneralTilt;
        }

    }

    public class Statistics_WarningAndErrors
    {
        public WarningAndErrorStatistic statistic;

        public Statistics_WarningAndErrors()
        {
           
        }
    }

    #endregion
    #region Game

    public struct GameStatistic
    {

        public decimal Denom { get; }
        public decimal In_Cash { get; }
        public int In_Credits { get; }
        public decimal Out_Cash { get; }
        public int Out_Credits { get; }
        public int Played { get; }
        public int Won { get; }
        public decimal TheoreticalPayback { get; }
        public decimal ActualPayback { get; }

        public GameStatistic(decimal denom, decimal In, int InC, decimal Out, int OutC, int played, int won, decimal theoreticalpayback, decimal actualpayback)
        {
            Denom = denom;
            In_Cash = In;
            In_Credits = InC;
            Out_Cash = Out;
            Out_Credits = OutC;
            Played = played;
            Won = won;
            TheoreticalPayback = theoreticalpayback;
            ActualPayback = actualpayback;
        }

    }

    public class Statistics_Game
    {
        public List<GameStatistic> statistics;

        public Statistics_Game()
        {
            statistics = new List<GameStatistic>();
        }
    }

    #endregion

    #endregion

    #region Diagnostics

    #region InputOutputTester

    public struct Outputs
    {
        public Outputs(bool _Spin, bool _AutoSpin, bool _Collect, bool _Help, bool _Lines, bool _CallAttendant, bool _Bet, bool _MaxBet, bool _TowerLight2, bool _TowerLight1, bool _CounterLights)
        {
            Spin = _Spin; AutoSpin = _AutoSpin;
            Collect = _Collect;
            Help = _Help; Lines = _Lines;
            CallAttendant = _CallAttendant;
            Bet = _Bet;
            MaxBet = _MaxBet;
            TowerLight1 = _TowerLight1;
            TowerLight2 = _TowerLight2;
            CounterLights = _CounterLights;

        }
        public bool Spin { get; }
        public bool AutoSpin { get; }
        public bool Collect { get; }
        public bool Help { get; }
        public bool Lines { get; }
        public bool CallAttendant { get; }
        public bool Bet { get; }
        public bool MaxBet { get; }
        public bool TowerLight2 { get; }
        public bool TowerLight1 { get; }
        public bool CounterLights { get; }

    }

    public struct Inputs
    {
        public Inputs(bool _Spin, bool _AutoSpin, bool _Collect, bool _Help, bool _Lines, bool _CallAttendant, bool _Bet, bool _MaxBet, bool _Key, bool _MainDoorOpen, bool _BellyDoorOpen, bool _DropDoorOpen, bool _LogicDoorOpen, bool _IOBoardDoorOpen, bool _ReinforcementOpen, bool _CashboxDoorOpen)
        {
            Spin = _Spin; AutoSpin = _AutoSpin; Collect = _Collect;
            Help = _Help; Lines = _Lines;
            CallAttendant = _CallAttendant;
            Bet = _Bet;
            MaxBet = _MaxBet;
            Key = _Key;
            MainDoorOpen = _MainDoorOpen;
            BellyDoorOpen = _BellyDoorOpen;
            DropDoorOpen = _DropDoorOpen;
            LogicDoorOpen = _LogicDoorOpen;
            IOBoardDoorOpen = _IOBoardDoorOpen;
            ReinforcementOpen = _ReinforcementOpen;
            CashboxDoorOpen = _CashboxDoorOpen;

        }
        public bool Spin { get; }
        public bool AutoSpin { get; }
        public bool Collect { get; }
        public bool Help { get; }
        public bool Lines { get; }
        public bool CallAttendant { get; }
        public bool Bet { get; }
        public bool MaxBet { get; }
        public bool Key { get; }
        public bool MainDoorOpen { get; }
        public bool BellyDoorOpen { get; }
        public bool DropDoorOpen { get; }
        public bool LogicDoorOpen { get; }
        public bool IOBoardDoorOpen { get; }
        public bool ReinforcementOpen { get; }
        public bool CashboxDoorOpen { get; }

    }

    public class Diagnostics_InputOutputTester
    {
        public Outputs outputs;
        public Inputs inputs;

    }

    #endregion

    #endregion

}
