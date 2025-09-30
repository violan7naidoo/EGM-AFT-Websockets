using SlotMathCore.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace EGMENGINE.EGMPlayModule.Impl.Slot
{
    public class SlotPlay : IEGMPlay
    {

        /* PARTIAL WINS */
        public decimal scatterWin;
        public decimal expandedWin;
        public decimal baseWin;
        public decimal misteryWin;
        public decimal actionGameWin;

        /* REEL DISPOSITION */
        public String[] reel1Disposition;
        public String[] reel2Disposition;
        public String[] reel3Disposition;
        public String[] reel4Disposition;
        public String[] reel5Disposition;

        /* SCATTER REEL POSITIONS */
        public int[] scatterReelPositions;

        /* REEL STOPS FOR SLOT PLAY */
        public int[] physicalReelStops;

        /* PAY LINEs */
        public string[] payLines;

        /* EXPANDED PAY LINES */
        public string[] expandedPayLines;

        /* EXPANDED SYMBOL */
        public Symbol? expandedSymbol;

        /* WINNING LINES */
        public Winningline[] winningLines;

        /* EXPANDED WINNING LINES */
        public Winningline[] expandedWinningLines;

        /* REMAINING PENNY GAMES */
        public int remainingPennyGames;

        /* AWARDED PENNY GAMES */
        public decimal awardedPennyGames;

        /* ORIGINAL SPIN DATA */
        //public BaseSpin originalSpinData;

        /* ACTION GAME */
        public int actiongame_segment;
        public int remainingActionGames;

        decimal creditsBefore1;
        public decimal creditsBefore
        {
            get
            {
                return creditsBefore1; // CurrentCredits
            }
            set
            {
                creditsBefore1 = value; // CurrentCredits
            }
        }

        decimal creditsAfter1;
        public decimal creditsAfter
        {
            get
            {
                return creditsAfter1; // CurrentCredits
            }
            set
            {
                creditsAfter1 = value; // CurrentCredits
            }
        }




        decimal creditsAfterBaseWin1;
        public decimal creditsAfterBaseWin
        {
            get
            {
                return creditsAfterBaseWin1; // CurrentCredits
            }
            set
            {
                creditsAfterBaseWin1 = value; // CurrentCredits
            }
        }



        decimal creditsAfterScatterWin1;
        public decimal creditsAfterScatterWin
        {
            get
            {
                return creditsAfterScatterWin1; // CurrentCredits
            }
            set
            {
                creditsAfterScatterWin1 = value; // CurrentCredits
            }
        }


        decimal creditsAfterExpandedWin1;
        public decimal creditsAfterExpandedWin
        {
            get
            {
                return creditsAfterExpandedWin1; // CurrentCredits
            }
            set
            {
                creditsAfterExpandedWin1 = value; // CurrentCredits
            }
        }

        decimal creditsAfterMisteryWin1;
        public decimal creditsAfterMisteryWin
        {
            get
            {
                return creditsAfterMisteryWin1; // CurrentCredits
            }
            set
            {
                creditsAfterMisteryWin1 = value; // CurrentCredits
            }
        }

        decimal creditsAfterActionGameWin1;
        public decimal creditsAfterActionGameWin
        {
            get
            {
                return creditsAfterActionGameWin1; // CurrentCredits
            }
            set
            {
                creditsAfterActionGameWin1 = value; // CurrentCredits
            }
        }



        decimal betCredits1;
        public decimal totalBetAmount
        {
            get
            {
                return betCredits1;
            }
            set
            {
                betCredits1 = value;
            }
        }
        decimal creditsOnPlay1;
        public decimal creditsOnPlay
        {
            get
            {
                return creditsOnPlay1;
            }
            set
            {
                creditsOnPlay1 = value;
            }
        }

        decimal winCredits1;
        public decimal winCredits
        {
            get
            {
                return winCredits1;
            }
            set
            {
                winCredits1 = value;
            }
        }

        decimal exceededCredits1;
        public decimal exceededCredits
        {
            get
            {
                return exceededCredits1; // CurrentCredits
            }
            set
            {
                exceededCredits1 = value; // CurrentCredits
            }
        }

        decimal bonus1;
        public decimal bonus
        {
            get
            {
                return bonus1;
            }
            set
            {
                bonus1 = value;
            }
        }

        decimal creditValue1;
        public decimal creditValue
        {
            get
            {
                return creditValue1;
            }
            set
            {
                creditValue1 = value;
            }
        }

        bool Finished1;
        public bool Finished
        {
            get
            {
                return Finished1;
            }
            set
            {
                Finished1 = value;
            }
        }

        DateTime TSPlayStart1;
        public DateTime TSPlayStart
        {
            get
            {
                return TSPlayStart1;
            }
            set
            {
                TSPlayStart1 = value;
            }
        }
        DateTime TSPlayEnd1;
        public DateTime TSPlayEnd
        {
            get
            {
                return TSPlayEnd1;
            }
            set
            {
                TSPlayEnd1 = value;
            }
        }


        PlayRepresentationStatus status1;
        public PlayRepresentationStatus status
        {
            get
            {
                return status1; // CurrentCredits
            }
            set
            {
                status1 = value; // CurrentCredits
            }
        }


        public void executePlay()
        {

        }
    }
}
