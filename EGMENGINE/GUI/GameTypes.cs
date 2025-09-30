using EGMENGINE.EGMPlayModule.Impl.Slot;
using EGMENGINE.GLOBALTYPES;
using System;
using System.Collections.Generic;
using System.Text;

namespace EGMENGINE.GUI.GAMETYPES
{
    public class SpinMarshall
    {
        public SlotPlay slotplay;
    }



    public class GameUIState {
        public bool playable;
        public int betPerLines;
        public int lastWon;
        public int lastBaseWon;
        public int lastScatterWon;
        public int lastExpandedWon;
        public int lastMisteryWon;
        public int lastActionGameWon;
        public int linesBet;
        public int totalBet;
        public decimal totalWon;
        public bool disabledByHost;
        public bool soundEnabled;
        public bool menuActive;
        public decimal selectedCreditValue;
        public decimal totalBetAmount;
        public UserRole? currentLoggedUser;
        public string[] tiltMessages;
        public string info;
        public string[] extraInfo;
        public bool collectInProgress;
        public bool handpayPending;
        public decimal handpayPendingAmount;
        public bool jackpotPending;
        public decimal jackpotPendingAmount;
        public bool autoSpin;
        public GameUIState()
        {
            tiltMessages = new string[2];
        }

    
    }
}
