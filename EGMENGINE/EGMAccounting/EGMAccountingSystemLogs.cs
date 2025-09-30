
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
    internal class EGMAccountingSystemLogs
    {
        private List<SystemLog> logHistory;
        public List<SystemLog> newLogs;
        public bool sync = true;
        public EGMAccountingSystemLogs()
        {
            logHistory = new List<SystemLog>();
            newLogs = new List<SystemLog>();
        }

        public void AddNewSystemLog(SystemLog newLog, bool isNew)
        {
            logHistory.Add(newLog);
            if (isNew)
                newLogs.Add(newLog);
        }

        public List<SystemLog> GetSystemLogs()
        {
            return logHistory;
        }

        public void ClearHistory()
        {
            logHistory.Clear();
            sync = false;

        }

    }
}
