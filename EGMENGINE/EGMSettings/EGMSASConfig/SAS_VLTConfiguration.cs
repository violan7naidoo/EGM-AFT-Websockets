using System;
using System.Collections.Generic;
using System.Text;

namespace EGMENGINE.EGMSettingsModule.EGMSASConfig
{
    internal class SAS_VLTConfiguration
    {
        public string SASVersion;
        public string GameID;
        public string AdditionalID;
        public decimal AccountingDenomination;
        public bool MultidenominationEnabled;
        public bool AuthenticationEnabled;
        public bool ExtendedMetersEnabled;
        public bool TicketsCounterEnabled;

        public SAS_VLTConfiguration()
        {
            SASVersion = "SAS version 6.02";
            GameID = "GRHS09LP01_92";
            AdditionalID = "VAR 01: 92";
            AccountingDenomination = (decimal)0.01;
            MultidenominationEnabled = true; 
            AuthenticationEnabled = DateTime.Now.Second % 2 == 0;
            ExtendedMetersEnabled = DateTime.Now.Second % 2 == 0;
            TicketsCounterEnabled = DateTime.Now.Second % 2 == 0;
        }
    }
}
