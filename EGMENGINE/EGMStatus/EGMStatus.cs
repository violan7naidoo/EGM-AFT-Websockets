
using EGMENGINE.EGMDataPersisterModule;
using EGMENGINE.EGMPlayModule.Impl.Slot;
using EGMENGINE.EGMSettingsModule;
using EGMENGINE.EGMStatusModule.CollectModule;
using EGMENGINE.EGMStatusModule.HandPayModule;
using EGMENGINE.EGMStatusModule.JackPotModule;
using EGMENGINE.GLOBALTYPES;
using EGMENGINE.GUI.GAMETYPES;
using SlotMathCore.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EGMENGINE.EGMStatusModule
{

    internal class Tilt
    {
        public DateTime TSTilt;
        public string Description;

        public Tilt(DateTime ts, string descr)
        {
            this.Description = descr;
            this.TSTilt = ts;
        }
    }

    /// <summary>
    /// Class EGMSettings definition. Singleton
    /// </summary>
    internal class EGMStatus
    {


        private static EGMStatus egmstatus_;

        private static EGMStatus old_egmstatus_;

        /// <summary>
        /// Calc from restricted + non restricted + cashable
        /// </summary>
        public decimal currentAmount;
        public decimal currentRestrictedAmount;
        public decimal currentNonRestrictedAmount;
        public decimal currentCashableAmount;
        public decimal currentBet;
        public decimal selectedCreditValue;

        public int betLines;
        public int betxline;
        public bool menuActive;
        public bool menuActive_billValidationTestActive;
        public bool maintenanceMode;
        public bool disabledByHost;
        public bool soundEnabled;
        public bool autoSpin;
        public bool userautoSpin;
        public bool setDateTime;
        public bool setSAS;
        public bool showInfo;
        public bool fullramclearperformed;
        public bool criticaltilt;
        public bool lastplayonview;

        //public enum SensorName
        //{
        //    D_MAINDOOR,
        //    D_BELLYDOOR,
        //    D_CASHBOXDOOR,
        //    D_DROPBOXDOOR,
        //    D_CARDCAGEDOOR,
        //    D_LOGICDOOR
        //}
        public AnimationEvent? current_animation_event;

        /* OUTPUT STATUS */

        public bool o_tower1lightStatus;
        public bool o_tower2lightStatus;
        public bool o_spinbuttonlightstatus;
        public bool o_betbuttonlightstatus;
        public bool o_helpbuttonlightstatus;
        public bool o_linesbuttonlightstatus;
        public bool o_cashoutbuttonlightstatus;
        public bool o_maxbetbuttonlightstatus;
        public bool o_servicebuttonlightstatus;
        public bool o_autospinbuttonlightstatus;

        /* SENSOR STATUS */

        public bool s_mainDoorStatus;
        public bool s_bellyDoorStatus;
        public bool s_cashBoxDoorStatus;
        public bool s_dropBoxDoorStatus;
        public bool s_cardCageDoorStatus;
        public bool s_logicDoorStatus;

        public List<string> extra_info_list;
        /* SPIN STATUS */
        public SpinRepresentation spinstatus;

        /* CURRENT TITLS */
        public List<Tilt> currentTilts;
        /* CURRENT COLLECT */
        public Collect current_collect;
        /* CURRENT HANDPAY */
        public Handpay current_handpay;
        /* CURRENT JACKPOT */
        public JackPot current_jackpot;
        /* FRONTEND PLAY */
        public FrontEndPlay frontend_play;
        public FrontEndPlayPenny frontend_play_penny;

        /* Selected Denomination */
        public Denomination selected_denomination;
        /// <summary>
        /// Current logged in user. It is updated when a user enter menu (pass the pin screen)
        /// </summary>
        public UserRole? currentLoggedInUser;
        /* Current Play */
        public EGMStatus_CurrentPlay current_play;

        /*******************/
        /* PERSIST PENDING */
        /*******************/
        public bool cashoutwithrestricted;

        public string MeterCHK;
        /// <summary>
        /// EGMStatus Constructor
        /// </summary>
        protected EGMStatus()
        {
            selectedCreditValue = 1;
            currentCashableAmount = 0;
            currentNonRestrictedAmount = 0;
            currentRestrictedAmount = 0;
            currentAmount = currentCashableAmount + currentNonRestrictedAmount + currentRestrictedAmount;
            menuActive = false;
            disabledByHost = false;
            soundEnabled = true;
            currentTilts = new List<Tilt>();
            betLines = EGMSettings.GetInstance().minBetLines;
            betxline = EGMSettings.GetInstance().minBetxline;
            current_collect = new Collect();
            current_handpay = new Handpay();
            current_jackpot = new JackPot();
            selected_denomination = new Denomination();
            maintenanceMode = false;
            spinstatus = new SpinRepresentation();
            autoSpin = false;
            extra_info_list = new List<string>();
            setDateTime = false;
            setSAS = false;
            showInfo = false;
            fullramclearperformed = true;
            criticaltilt = false;
            current_animation_event = null;
            current_play = new EGMStatus_CurrentPlay();
            current_play.spin = new SpinMarshall();
            current_play.spin.slotplay = new SlotPlay();
            current_play.spin.slotplay.Finished = true;
            current_play.spin.slotplay.physicalReelStops = new int[] { 22, 16, 26, 20, 26 };
            cashoutwithrestricted = false;
            userautoSpin = false;
            //lastplay = "";
        }

        public void resetStatus()
        {
            selectedCreditValue = 1;
            currentCashableAmount = 0;
            currentNonRestrictedAmount = 0;
            currentRestrictedAmount = 0;
            currentAmount = currentCashableAmount + currentNonRestrictedAmount + currentRestrictedAmount;
            menuActive = false;
            disabledByHost = false;
            soundEnabled = true;
            currentTilts = new List<Tilt>();
            betLines = EGMSettings.GetInstance().minBetLines;
            betxline = EGMSettings.GetInstance().minBetxline;
            current_collect = new Collect();
            current_handpay = new Handpay();
            current_jackpot = new JackPot();
            selected_denomination = new Denomination();
            maintenanceMode = false;
            spinstatus = new SpinRepresentation();
            menuActive_billValidationTestActive = false;
            setDateTime = false;
            setSAS = false;
            showInfo = false;
            criticaltilt = false;
            current_animation_event = null;
            current_play = new EGMStatus_CurrentPlay();
            current_play.spin = new SpinMarshall();
            current_play.spin.slotplay = new SlotPlay();
            current_play.spin.slotplay.Finished = true;
            current_play.spin.slotplay.physicalReelStops = new int[] { 22, 16, 26, 20, 26 };
            cashoutwithrestricted = false;
            userautoSpin = false;

            //lastplay = "";

        }

        /// <summary>
        /// Method for add credits, cashable, restricted or nonrestricted. Automatically updates the current credits
        /// It is used mainly by InitBillAcceptorController on EGM class, capturing each bill insertion event
        /// </summary>
        /// <param name="cashable"></param>
        /// <param name="restricted"></param>
        /// <param name="nonRestricted"></param>
        public void AddAmount(decimal cashable, decimal restricted, decimal nonRestricted)
        {
            currentCashableAmount += cashable;
            currentNonRestrictedAmount += nonRestricted;
            currentRestrictedAmount += restricted;

            currentAmount = currentCashableAmount + currentNonRestrictedAmount + currentRestrictedAmount;
         
        }

        public static EGMStatus GetOldInstance()
        {
            if (old_egmstatus_ == null)
            {
                if (old_egmstatus_ == null)
                {
                    old_egmstatus_ = new EGMStatus();
                }
                old_egmstatus_.currentLoggedInUser = null;


            }
            return old_egmstatus_;
        }

        public static EGMStatus GetInstance()
        {
            if (egmstatus_ == null)
            {
                if (egmstatus_ == null)
                {
                    egmstatus_ = new EGMStatus();
                }
                egmstatus_.currentLoggedInUser = null;


            }
            return egmstatus_;
        }


    }
}
