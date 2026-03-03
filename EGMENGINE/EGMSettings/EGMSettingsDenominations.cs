using EGMENGINE.EGMDataPersisterModule;
using EGMENGINE.GLOBALTYPES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EGMENGINE.EGMSettingsModule
{

    /// <summary>
    /// Class EGMSettings definition. Singleton
    /// </summary>
    internal class EGMSettingsDenominations
    {
        private List<Denomination> denominations;

        internal EGMSettingsDenominations()
        {
            denominations = new List<Denomination>
            {
                new Denomination { Code = 0x01, monetaryValue = 0.01M, enabled = true },

                new Denomination { Code = 0x02, monetaryValue = 0.05M, enabled = false },

                new Denomination { Code = 0x03, monetaryValue = 0.10M, enabled = false },
                new Denomination { Code = 0x04, monetaryValue = 0.25M, enabled = false },

                new Denomination { Code = 0x05, monetaryValue = 0.50M, enabled = false },
                new Denomination { Code = 0x06, monetaryValue = 1.00M, enabled = false },
                new Denomination { Code = 0x07, monetaryValue = 5.00M, enabled = false },
                new Denomination { Code = 0x08, monetaryValue = 10.00M, enabled = false },


                new Denomination { Code = 0x09, monetaryValue = 20.00M, enabled = false },

                new Denomination { Code = 0x0A, monetaryValue = 100.00M, enabled = false },
                new Denomination { Code = 0x0B, monetaryValue = 0.20M, enabled = false },

                new Denomination { Code = 0x0C, monetaryValue = 2.00M, enabled = false },

                new Denomination { Code = 0x0D, monetaryValue = 2.50M, enabled = false },

                new Denomination { Code = 0x0E, monetaryValue = 25.00M, enabled = false },
                new Denomination { Code = 0x0F, monetaryValue = 50.00M, enabled = false },

                new Denomination { Code = 0x10, monetaryValue = 200.00M, enabled = false },

                new Denomination { Code = 0x11, monetaryValue = 250.00M, enabled = false },

                new Denomination { Code = 0x12, monetaryValue = 500.00M, enabled = false },

                new Denomination { Code = 0x13, monetaryValue = 1000.00M, enabled = false },

                new Denomination { Code = 0x14, monetaryValue = 2000.00M, enabled = false },
                new Denomination { Code = 0x15, monetaryValue = 2500.00M, enabled = false },

                new Denomination { Code = 0x16, monetaryValue = 5000.00M, enabled = false },

                new Denomination { Code = 0x17, monetaryValue = 0.02M, enabled = false },
                new Denomination { Code = 0x18, monetaryValue = 0.03M, enabled = false },

                new Denomination { Code = 0x19, monetaryValue = 0.15M, enabled = false },

                new Denomination { Code = 0x1A, monetaryValue = 0.40M, enabled = false },

                new Denomination { Code = 0x1B, monetaryValue = 0.005M, enabled = false },

                new Denomination { Code = 0x1C, monetaryValue = 0.0025M, enabled = false },

                new Denomination { Code = 0x1D, monetaryValue = 0.002M, enabled = false },

                new Denomination { Code = 0x1E, monetaryValue = 0.001M, enabled = false },
                new Denomination { Code = 0x1E, monetaryValue = 0.0005M, enabled = false },
                new Denomination { Code = 0x1F, monetaryValue = 0.60M, enabled = false }

            };


        }

        internal void EnableDenomination(byte code)
        {
            Denomination d = denominations.First(d_ => d_.Code == code);
            d.enabled = true;
        }

        internal void DisableDenomination(byte code)
        {
            Denomination d = denominations.First(d_ => d_.Code == code);
            d.enabled = false;
        }

        internal Denomination GetDenomination(byte code)
        {
            Denomination d_ = denominations.First(d_ => d_.Code == code);
            if (d_.enabled)
                return d_;
            else return (new Denomination { enabled = true, Code = 0x00, monetaryValue = 0 });
        }

        internal List<Denomination> GetEnabledDenominations()
        {
            return denominations.Where(d => d.enabled == true).ToList();
        }

        internal List<Denomination> GetDisabledDenominations()
        {
            return denominations.Where(d => d.enabled == false).ToList();
        }

    }
}
