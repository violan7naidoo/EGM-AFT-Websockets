using System;
using System.Collections.Generic;
using System.Text;

namespace EGMENGINE.EGMSettingsModule.EGMSASConfig
{
    internal class SAS_MainConfiguration
    {
        public bool SASEnabled;
        public int SASId;
        public int AssetNumber;
        public int SerialNumber;
        public bool TiltOnASASDisconnection;
        public decimal AccountingDenomination;
        public decimal SASReportedDenomination;
        public decimal InHouseInLimit;
        public decimal InHouseOutLimit;
        public SAS_MainConfiguration()
        {
            SASReportedDenomination = (decimal)0.01;
            AccountingDenomination = (decimal)0.01;
            TiltOnASASDisconnection = false;
            SerialNumber = DateTime.Now.Millisecond;
            AssetNumber = 12345;
            SASId = 0;
            SASEnabled = false;
            InHouseInLimit = (decimal)0;
            InHouseOutLimit = (decimal)0;
        }
    }
}
