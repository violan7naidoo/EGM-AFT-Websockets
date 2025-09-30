using System;
using System.Collections.Generic;
using System.Text;

namespace EGMENGINE.EGMSettingsModule.EGMSASConfig
{
    internal class SAS_HostConfiguration
    {
        public bool BillAcceptorEnabled;
        public bool SoundEnabled;
        public bool RealTimeModeEnabled;
        public bool ValidateHandpaysReceipts;
        public bool TicketsForForeignRestrictedAmounts;
        public bool TicketsRedemptionEnabled;
        public SAS_HostConfiguration()
        {
            BillAcceptorEnabled = true;
            SoundEnabled = DateTime.Now.Second % 2 == 0;
            RealTimeModeEnabled = DateTime.Now.Second % 2 == 0;
            ValidateHandpaysReceipts = DateTime.Now.Second % 2 == 0;
            TicketsForForeignRestrictedAmounts = DateTime.Now.Second % 2 == 0;
            TicketsRedemptionEnabled = DateTime.Now.Second % 2 == 0;
        }
    }
}
