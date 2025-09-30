
using EGMENGINE.EGMDataPersisterModule;
using EGMENGINE.GLOBALTYPES;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EGMENGINE.EGMAccountingModule
{

    /// <summary>
    /// Class EGMSettings definition. Singleton
    /// </summary>
    internal class EGMAccountingLastPlays
    {
        private List<LastPlay> lastplayHistory;
        public List<LastPlay> newlastplays;
        public bool sync = true;
        public EGMAccountingLastPlays()
        {
            lastplayHistory = new List<LastPlay>();
            newlastplays = new List<LastPlay>();
        }

        public void AddNewPlay(LastPlay play, bool persist)
        {
            lastplayHistory.Add(play);
            if (persist)
                newlastplays.Add(play);
        }

        public List<LastPlay> GetLastPlays()
        {
            return lastplayHistory;
        }

        public void ClearHistory()
        {
            lastplayHistory.Clear();
            sync = false;


        }
    }
}
