using System;
using System.Collections.Generic;
using System.Text;

namespace EGMENGINE.EGMSettingsModule.EGMSASConfig
{
    internal class SASSettings
    {
        public SAS_MainConfiguration mainConfiguration;
        public SAS_VLTConfiguration vltConfiguration;
        public SAS_HostConfiguration hostConfiguration;
        public bool SASConfigured;

        public SASSettings()
        {
            mainConfiguration = new SAS_MainConfiguration();
            vltConfiguration = new SAS_VLTConfiguration();
            hostConfiguration = new SAS_HostConfiguration();
            SASConfigured = false;
        }
    }
}
