using EGMENGINE.EGMDataPersisterModule;
using EGMENGINE.EGMStatusModule;
using Newtonsoft.Json;
using SlotMathCore.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EGMENGINE.GUI.GAMETYPES
{
    internal class ExtraPlayData
    {
        public Symbol? ExpandedSymbol;
        public List<BaseSpin> PennyGames = new List<BaseSpin>();
        public List<ActionGame> ActionGames = new List<ActionGame>();

    }


    internal class EGMStatus_CurrentPlay
    {
        /* MeterCHK */
        internal string MeterCHK;

        public decimal payline1amount;
        public decimal payline2amount;
        public decimal payline3amount;
        public decimal payline4amount;
        public decimal payline5amount;

        public int CreditsWon;
        public int BaseCreditsWon;
        public int ScatterCreditsWon;
        public int ExpandedCreditsWon;
        public int MisteryCreditsWon;
        public int ActionGameCreditsWon;

        public SpinMarshall? spin;
        /* PENNY GAMES */
        public bool scatter_win;
        public bool mistery_win;
        public bool expanded_win;
        public bool baseWinning;
        /* PLAY PENNY */
        public ExtraPlayData? ExtraData;
        public string ExtraDataStr;

        public int pennyGamesIndex;
        public decimal awardedPennyGames;
        public int totalCurrentPennyGames;
        /* ACTION GAMES */
        public int totalCurrentActionGames;
        public int actionGameIndex;
        public bool actiongamewinning;

        public void ResetPlay()
        {

            spin.slotplay.Finished = true;
           
        }

        public void TransformToExtraDataString()
        {
            try
            {

                ExtraDataStr = JsonConvert.SerializeObject(EGMStatus.GetInstance().current_play.ExtraData);
            }
            catch
            {

            }
        }



        public List<ActionGame> getActionGames()
        {
            if (spin == null)
            {
                return new List<ActionGame>();
            }
            else
            {
                return ExtraData.ActionGames ?? new List<ActionGame>();
            }
        }
        public List<BaseSpin> getPennyGames()
        {
            if (spin == null)
            {
                return new List<BaseSpin>();
            }
            else
            {
                return ExtraData.PennyGames ?? new List<BaseSpin>();
            }
        }
        public EGMStatus_CurrentPlay()
        {
            pennyGamesIndex = -1;
            actionGameIndex = -1;
            totalCurrentPennyGames = 0;
            payline1amount = 0;
            payline2amount = 0;
            payline3amount = 0;
            payline4amount = 0;
            payline5amount = 0;
            mistery_win = false;
            actiongamewinning = false;
            scatter_win = false;
            baseWinning = false;
            spin = null;
            ExtraData = new ExtraPlayData();
        }
    }
}
