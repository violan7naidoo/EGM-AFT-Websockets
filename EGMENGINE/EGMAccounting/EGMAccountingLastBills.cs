
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
    internal class EGMAccountingLastBills
    {
        private List<LastBill> lastbillsHistory;
        public List<LastBill> newlastbills;
        public bool sync = true;
        public EGMAccountingLastBills()
        {
            lastbillsHistory = new List<LastBill>();
            newlastbills = new List<LastBill>();
        }

        public void AddNewBill(LastBill bill, bool persist)
        {
            lastbillsHistory.Add(bill);
            if (persist)
                newlastbills.Add(bill);
        }

        public List<LastBill> GetLastBills()
        {
            return lastbillsHistory;
        }

        public void ClearHistory()
        {
            lastbillsHistory.Clear();
            sync = false;
        }

    }
}
