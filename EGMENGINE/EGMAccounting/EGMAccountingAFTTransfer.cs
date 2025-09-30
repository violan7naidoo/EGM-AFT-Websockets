
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
    internal class EGMAccountingAFTTransfers
    {
        private List<AccountingAFTTransfer> transferHistory;
        public List<AccountingAFTTransfer> newTransfers;
        public bool sync = true;
        public EGMAccountingAFTTransfers()
        {
            transferHistory = new List<AccountingAFTTransfer>();
            newTransfers = new List<AccountingAFTTransfer>();
        }

        public void AddNewTransfer(AccountingAFTTransfer newTransfer, bool isNew)
        {
            transferHistory.Add(newTransfer);
            if (isNew)
                newTransfers.Add(newTransfer);
        }

        public List<AccountingAFTTransfer> GetTranfersHistory()
        {
            return transferHistory;
        }

        public void ClearHistory()
        {
            transferHistory.Clear();
            sync = false;

        }

    }
}
