using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EGMENGINE.EGMPlayModule
{
    public enum PlayRepresentationStatus
    {
        Generated,
        AnimatingReels,
        AnimatingPrize,
        Finished
    }

    internal interface IEGMPlay
    {
        public decimal creditsBefore { get; set; }
        public decimal creditsOnPlay { get; set; }
        public decimal creditsAfter { get; set;  }
        public decimal creditsAfterBaseWin { get; set; }
        public decimal creditsAfterScatterWin { get; set; }
        public decimal creditsAfterExpandedWin { get; set; }
        public decimal creditsAfterMisteryWin { get; set; }
        public decimal creditsAfterActionGameWin { get; set; }
        public decimal totalBetAmount { get; set;  }
        public decimal winCredits { get; set; }
        public decimal exceededCredits { get; set; }
        public decimal creditValue { get; set; }
        public DateTime TSPlayStart { get; set;  }
        public DateTime TSPlayEnd { get; set; }
        public PlayRepresentationStatus status { get; set; }
        public void executePlay();
    }

}
