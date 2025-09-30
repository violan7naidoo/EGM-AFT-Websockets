using SlotMathCore.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace EGMENGINE.GLOBALTYPES
{

    public enum UserRole
    {
        Attendant,
        Technician,
        Operator,
        Manufacturer,
        UNAUTHORIZED
    }

    public enum UserPwdResponse
    {
        Success,
        PinAlreadyUsed,
        UNAUTHORIZED
    }


    public enum AnimationEvent
    {
        WheelSpinning,
        WheelStoped,
        ActionGameWinAnimationFinished,
        ReelsSpinning,
        ReelsStoped,
        ExpandedSymbolDrawing,
        ExpandedSymbolDrawFinished,
        MisteryWinAnimationFinished,
        SymbolsExpanded,
        WinAnimationFinished,
        ScatterWinAnimationFinished,
        EnterBillValidationTest,
        ExitBillValidationTest
    }


    public struct LastPlay
    {
        public DateTime Date { get; }
        public decimal CreditsBefore { get; }
        public decimal CreditsAfter { get; }
        public decimal CreditsWagered { get; }
        public decimal CreditsWon { get; }
        public decimal BaseCreditsWon { get; }
        public decimal ScatterCreditsWon { get; }
        public decimal ExpandedCreditsWon { get; }
        public decimal MisteryCreditsWon { get; }
        public decimal CreditValue { get; }
        public decimal Prize { get; }
        public decimal Bonus { get; }
        public decimal Total { get; }
        public int Reel1Stop { get; }
        public int Reel2Stop { get; }
        public int Reel3Stop { get; }
        public int Reel4Stop { get; }
        public int Reel5Stop { get; }

        public Winningline[] WinningLines { get; }
        public bool PennyGamesTrigger { get; }
        public decimal PennyGamesPrize { get; }
        public bool ActionGamesTrigger { get; }
        public decimal ActionGamesPrize { get; }

        public LastPlay(DateTime dt, decimal creditsbefore, decimal creditsafter, decimal creditswagered, decimal creditswon, decimal basecreditswon, decimal scattercreditswon, decimal expandedcreditswon, decimal misterycreditswon, decimal creditsvalue, decimal prize, decimal bonus, decimal total, int reel1stop, int reel2stop, int reel3stop, int reel4stop, int reel5stop, Winningline[] winninglines, bool pennygamestrigger, decimal pennygamesprize, bool actiongamestrigger, decimal actiongamesprize)
        {
            Date = dt;
            CreditsBefore = creditsbefore;
            CreditsAfter = creditsafter;
            CreditsWagered = creditswagered;
            CreditsWon = creditswon;
            BaseCreditsWon = basecreditswon;
            ScatterCreditsWon = scattercreditswon;
            ExpandedCreditsWon = expandedcreditswon;
            MisteryCreditsWon = misterycreditswon;
            CreditValue = creditsvalue;
            Prize = prize;
            Bonus = bonus;
            Total = total;
            Reel1Stop = reel1stop;
            Reel2Stop = reel2stop;
            Reel3Stop = reel3stop;
            Reel4Stop = reel4stop;
            Reel5Stop = reel5stop;
            WinningLines = winninglines;
            PennyGamesTrigger = pennygamestrigger;
            PennyGamesPrize = pennygamesprize;
            ActionGamesTrigger = actiongamestrigger;
            ActionGamesPrize = actiongamesprize;

        }
    }
    public struct AccountingAFTTransfer
    {        
        public string Type { get; }
        public DateTime Timestamp { get; }
        public string DebitCredit { get; }
        public decimal CurrencyAmount { get; }
        public int CashableAmount { get; }
        public int RestrictedAmount { get; }
        public int NonRestrictedAmount { get; }

        public AccountingAFTTransfer(string type_, DateTime timestamp_, string debitcredit_, decimal currencyamount_, int cashableamount_, int restrictedamount_, int nonrestrictedamount_)
        {
            Type = type_;
            Timestamp = timestamp_;
            DebitCredit = debitcredit_;
            CurrencyAmount = currencyamount_;
            CashableAmount = cashableamount_;
            RestrictedAmount = restrictedamount_;
            NonRestrictedAmount = nonrestrictedamount_;
        }
    }

    public enum MenuSceneNameAction
    {
        ReadOnly,
        FullAccess,
        NotAuthorized
    }
    public enum MenuSceneName
    {
        Statistics,
        Statistics_GameStatistics,
        Statistics_LastBills,
        Statistics_AFTTransaction,
        Statistics_LastPlays,
        Diagnostics,
        Diagnostics_InputOutputTester,
        Diagnostics_BillValidatorTest,
        GameIdentification_SystemID,
        GameIdentification_ROMIdInformation,
        Diagnostics_TouchScreenCalibrationUtility,
        Configuration,
        Configuration_GameConfiguration,
        Configuration_GameConfiguration_BetAndMoneyLimits,
        Configuration_CashInCashOutConfiguration,
        Configuration_SASConfiguration,
        Configuration_SASConfiguration_SASMainConfiguration,
        Configuration_SASConfiguration_SASVLTConfiguration,
        Configuration_SASConfiguration_SASHostConfiguration,
        Configuration_BillsConfiguration,
        Configuration_SetDateAndTime,
        Configuration_SetPassword,
        Configuration_RAMClear

    }

    public enum BillValidatorLogDetail
    {
        SlaveReset,
        ReadNote,
        CreditNote,
        NoteRejecting,
        NoteRejected,
        NoteStacking,
        NoteStacked,
        NotePathOpen,
        ChannelDisable,
        SafeNoteJam,
        UnsafeNoteJam,
        ValidatorDisabled,
        FraudAttempt,
        StackerFull,
        NoteClearedFromFront,
        NoteClearedToCashbox,
        CashboxRemoved,
        CashboxReplaced,
        StackerRemoved,
        StackerReplaced
    }

    public struct BillValidatorLog
    {
        public BillValidatorLogDetail detail { get; }
        public DateTime timestamp { get; }

        public BillValidatorLog(DateTime ts, BillValidatorLogDetail d)
        {
            detail = d;
            timestamp = ts;
        }

    }


    public struct LastBill
    {
        public DateTime date { get; }
        public int Denomination { get; }

        public LastBill(DateTime ts, int denom)
        {
            date = ts;
            Denomination = denom;
        }
    }

    public struct RamClearLog
    {
        public string User { get; }
        public DateTime TimeStamp { get; }
        public decimal CoinIn { get; }
        public decimal CoinOut { get; }
        public int GamePlays { get; }
        public RamClearLog(string user, DateTime ts, decimal coinin, decimal coinout, int gameplays)
        {
            User = user;
            TimeStamp = ts;
            CoinIn = coinin;
            CoinOut = coinout;
            GamePlays = gameplays;
        }
    }

    /// <summary>
    /// SystemLog. Estructura  utilizada para dejar bitácora de eventos del juego
    /// </summary>
    public struct SystemLog
    {
        public SystemLog(DateTime dt, string detail, string user)
        {
            Date = dt;
            EventDetail = detail;
            User = user;
        }

        public DateTime Date { get; }
        public string EventDetail { get; }
        public string User { get; }
    }


    /// <summary>
    /// HandpayTransaction
    /// </summary>
    public struct HandpayTransaction
    {
        public string Type { get; }
        public DateTime Date { get; }
        public decimal Amount { get; }
        public HandpayTransaction(string type, DateTime date, decimal amount)
        {
            Type = type;
            Date = date;
            Amount = amount;
        }
    }

    /// <summary>
    ///  Estructura de SystemTime. Utilizada en Menu_SetDateTime para la actualización de la fecha y hora del sistema
    /// </summary>
    internal struct SystemTime
    {
        public ushort Year;
        public ushort Month;
        public ushort DayOfWeek;
        public ushort Day;
        public ushort Hour;
        public ushort Minute;
        public ushort Second;
        public ushort Millisecond;
    };

    /// <summary>
    /// EventType. Utilizado para realizar determinadas acciones de acuerdo al evento sucedido
    /// </summary>
    public enum EventType
    {
        AutoSpinToggled,
        BetPlusPressed,
        BetMinusPressed,
        LinesPlusPressed,
        LinesMinusPressed,
        SpinPressed,
        MaxBetPressed,
        InfoPressed
    }
    public delegate void UIPriorityEventHandler(object sender, EventType eventtype, EventArgs e);
    internal class Denomination
    {
        public byte Code { get; set; }
        public decimal monetaryValue { get; set; }
        public bool enabled { get; set; }
    }
}
