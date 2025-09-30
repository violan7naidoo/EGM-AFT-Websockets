
using EGMENGINE.EGMDataPersisterModule;
using EGMENGINE.GLOBALTYPES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EGMENGINE.EGMAccountingModule
{

    /// <summary>
    /// Class EGMSettings definition. Singleton
    /// </summary>
    internal class EGMAccountingRamClearHistory
    {
        private List<RamClearLog> ramClearHistory;
        public List<RamClearLog> newRamClears;
        public bool sync = true;

        public EGMAccountingRamClearHistory()
        {
            ramClearHistory = new List<RamClearLog>();
            newRamClears = new List<RamClearLog>();
        }

        public void AddNewRamClear(RamClearLog newRamClear, bool persist)
        {
            ramClearHistory.Add(newRamClear);
            if (persist)
            {
                newRamClears.Add(newRamClear);
            }
        }

        public List<RamClearLog> GetRamClearHistory()
        {
            return ramClearHistory;
        }

        public void ClearHistory()
        {
            sync = false;
            ramClearHistory.Clear();
        }
    }
}
