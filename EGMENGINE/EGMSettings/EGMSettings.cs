
using EGMENGINE.EGMSettingsModule.EGMSASConfig;
using EGMENGINE.EGMStatusModule.CollectModule;
using EGMENGINE.EGMStatusModule.HandPayModule;
using EGMENGINE.EGMStatusModule.JackPotModule;
using EGMENGINE.EGMStatusModule;
using EGMENGINE.GLOBALTYPES;
using EGMENGINE.GUI.GAMETYPES;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EGMENGINE.GUI.MENUTYPES;

namespace EGMENGINE.EGMSettingsModule
{
    /// <summary>
    /// Class EGMSettings definition. Singleton
    /// </summary>
    internal class EGMSettings
    {
        private static EGMSettings egmsettings_;
        private static EGMSettings old_egmsettings_;

        /* Denominations */
        internal EGMSettingsDenominations Denominations;
        internal bool DisableBillAcceptorAfterEachAcceptedBill;
        // Security pines for each user role
        public int AttendantPin;
        public int TechnicianPin;
        public int OperatorPin;
        public int ManufacturerPin;
        // CashIn CashOut
        public bool PartialPay;
        public bool BillAcceptor;
        public bool AFTEnabled;
        public bool HandpayEnabled;
        public string CashOutOrder;
        // AFT Settings
        // sas Settings
        public SASSettings sasSettings;
        // Game Settings
        public int maxBetLines;
        public int minBetLines;
        public int maxBetxline;
        public int minBetxline;
        /* Accounting settings */
        public bool FirstBootAfterSoftRC;
        public bool FirstBootAfterHardRC;
        /* System */
        public bool MandatoryPC;
        /* Bet and money Limits */
        public decimal jackpotLimit;
        public bool jackpotEnabled;
        public decimal creditLimit;
        /* BillAcceptorComPort */
        public string BillAcceptorComPort;

        public int BillAcceptorChannelSet1;
        public int BillAcceptorChannelSet2;
        public int BillAcceptorChannelSet3;
        /// <summary>
        /// EGMSettings Constructor
        /// </summary>
        protected EGMSettings()
        {
            Denominations = new EGMSettingsDenominations();
            AttendantPin = 346932164;
            TechnicianPin = 346932164;
            OperatorPin = 346932164;
            ManufacturerPin = 8172;
            creditLimit = 200000;
            sasSettings = new SASSettings();
            PartialPay = true;
            BillAcceptor = false;
            AFTEnabled = false;
            HandpayEnabled = false;
            CashOutOrder = "Aft/Handpay";
            maxBetLines = 30;
            minBetLines = 0;
            maxBetxline = 1;
            minBetxline = 1;
            FirstBootAfterSoftRC = false;
            FirstBootAfterHardRC = false;
            MandatoryPC = false;
            DisableBillAcceptorAfterEachAcceptedBill = false;
            jackpotLimit = 10000000;
            jackpotEnabled = false;
            BillAcceptorComPort = "COM4";
            BillAcceptorChannelSet1 = 255;
            BillAcceptorChannelSet2 = 255;
            BillAcceptorChannelSet3 = 255;
        }

        public void resetSettings()
        {
            Denominations = new EGMSettingsDenominations();
            AttendantPin = 346932164;
            TechnicianPin = 346932164;
            OperatorPin = 346932164;
            ManufacturerPin = 8172;
            creditLimit = 200000;
            sasSettings = new SASSettings();
            PartialPay = true;
            BillAcceptor = false;
            AFTEnabled = false;
            HandpayEnabled = false;
            CashOutOrder = "Aft/Handpay";
            maxBetLines = 5;
            minBetLines = 5;
            maxBetxline = 1;
            minBetxline = 1;
            FirstBootAfterSoftRC = false;
            FirstBootAfterHardRC = false;
            MandatoryPC = false;
            DisableBillAcceptorAfterEachAcceptedBill = false;
            jackpotLimit = 10000000;
            jackpotEnabled = false;
            BillAcceptorComPort = "COM4";
            BillAcceptorChannelSet1 = 255;
            BillAcceptorChannelSet2 = 255;
            BillAcceptorChannelSet3 = 255;
        }

        internal bool EnabledChannels(int d)
        {
            if (d == 1)
                return ((BillAcceptorChannelSet1 & (int)Math.Pow(2, 0)) > 0);
            if (d == 2 )
                return ((BillAcceptorChannelSet1 & (int)Math.Pow(2, 1)) > 0);
            if (d == 5 )
                return ((BillAcceptorChannelSet1 & (int)Math.Pow(2, 2)) > 0);
            if (d == 10 )
                return ((BillAcceptorChannelSet1 & (int)Math.Pow(2, 3)) > 0);
            if (d == 20 )
                return ((BillAcceptorChannelSet1 & (int)Math.Pow(2, 4)) > 0);
            if (d == 25 )
                return ((BillAcceptorChannelSet1 & (int)Math.Pow(2, 5)) > 0);
            if (d == 50 )
                return ((BillAcceptorChannelSet1 & (int)Math.Pow(2, 6)) > 0);
            if (d == 100 )
                return ((BillAcceptorChannelSet1 & (int)Math.Pow(2, 7)) > 0);


            if (d == 200 )
                return ((BillAcceptorChannelSet2 & (int)Math.Pow(2, 0)) > 0);
            if (d == 250 )
                return ((BillAcceptorChannelSet2 & (int)Math.Pow(2, 1)) > 0);
            if (d == 500 )
                return ((BillAcceptorChannelSet2 & (int)Math.Pow(2, 2)) > 0);
            if (d == 1000 )
                return ((BillAcceptorChannelSet2 & (int)Math.Pow(2, 3)) > 0);
            if (d == 2000 )
                return ((BillAcceptorChannelSet2 & (int)Math.Pow(2, 4)) > 0);
            if (d == 2500 )
                return ((BillAcceptorChannelSet2 & (int)Math.Pow(2, 5)) > 0);
            if (d == 5000 )
                return ((BillAcceptorChannelSet2 & (int)Math.Pow(2, 6)) > 0);
            if (d == 10000 )
                return ((BillAcceptorChannelSet2 & (int)Math.Pow(2, 7)) > 0);

            if (d == 20000 )
                return ((BillAcceptorChannelSet3 & (int)Math.Pow(2, 0)) > 0);
            if (d == 25000 )
                return ((BillAcceptorChannelSet3 & (int)Math.Pow(2, 1)) > 0);
            if (d == 50000 )
                return ((BillAcceptorChannelSet3 & (int)Math.Pow(2, 2)) > 0);
            if (d == 100000 )
                return ((BillAcceptorChannelSet3 & (int)Math.Pow(2, 3)) > 0);
            if (d == 200000 )
                return ((BillAcceptorChannelSet3 & (int)Math.Pow(2, 4)) > 0);
            if (d == 250000 )
                return ((BillAcceptorChannelSet3 & (int)Math.Pow(2, 5)) > 0);
            if (d == 500000 )
                return ((BillAcceptorChannelSet3 & (int)Math.Pow(2, 6)) > 0);
            if (d == 1000000 )
                return ((BillAcceptorChannelSet3 & (int)Math.Pow(2, 7)) > 0);

            return false;
        }

        public static EGMSettings GetOldInstance()
        {
            if (old_egmsettings_ == null)
            {
                if (old_egmsettings_ == null)
                {
                    old_egmsettings_ = new EGMSettings();
                }


            }
            return old_egmsettings_;
        }

        /// <summary>
        /// Singleton Method. Get the unique instance of EGM
        /// </summary>
        /// <returns></returns>
        public static EGMSettings GetInstance()
        {
            if (egmsettings_ == null)
            {
                if (egmsettings_ == null)
                {
                    egmsettings_ = new EGMSettings();   // Instantiate a new EGMSettings if the singleton is used for the first time
                }
            }
            return egmsettings_;
        }


    }
}
