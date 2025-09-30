
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
    internal class EGMAccountingHandpay
    {
        private List<HandpayTransaction> handpayHistory;
        public List<HandpayTransaction> newHandpays;
        public bool sync = true;
        public EGMAccountingHandpay()
        {
            handpayHistory = new List<HandpayTransaction>();
            newHandpays = new List<HandpayTransaction>();
        }

        public void AddNewHandpay(HandpayTransaction handpay, bool persist)
        {
            handpayHistory.Add(handpay);
            if (persist)
                newHandpays.Add(handpay);
        }

        public List<HandpayTransaction> GetHandpayHistory()
        {
            return handpayHistory;
        }

        public void ClearHistory()
        {
            sync = false;
            handpayHistory.Clear();
        }

    }
}
