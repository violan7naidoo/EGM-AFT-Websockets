using EGMENGINE.EGMPlayModule.Impl.Slot;
using EGMENGINE.GUI;
using EGMENGINE.BillAccCTLModule;
using EGMENGINE.BillAccCTLModule.Impl.SSPBillAccCTL;
using EGMENGINE.BillAccCTLModule.Impl.SSPBillAccServiceCTL;
using EGMENGINE.BillAccCTLModule.Impl.VirtualBillAccCTL;
using EGMENGINE.EGMDataPersisterModule;
using EGMENGINE.EGMStatusModule;
using EGMENGINE.GPIOCTL;
using EGMENGINE.GPIOCTL.Impl.VirtualGPIOCTL;
using EGMENGINE.GUI.GAMETYPES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Security.Cryptography;
using System.Text;
using EGMENGINE.GPIOCTL.Impl.GanlotGPIOCTL;
using System.Timers;
using System.ComponentModel;
using EGMENGINE.EGMSettingsModule;
using EGMENGINE.EGMSettingsModule.EGMSASConfig;
using EGMENGINE.GUI.MENUTYPES;
using EGMENGINE.BillAccCTLModule.BillAccTypes;
using EGMENGINE.EGMAccountingModule;
using EGMENGINE.GLOBALTYPES;
using EGMENGINE.SASCTLModule;
using Newtonsoft.Json.Linq;
using EGMENGINE.EGMStatusModule.CollectModule;
using EGMENGINE.EGMStatusModule.HandPayModule;
using SlotMathCore;
using SlotMathCore.Model;
using System.Collections;
using System.Threading.Tasks;
using static EGMENGINE.GUI.MenuGUIController;
using System.Runtime.InteropServices;
using System.IO;
using EGMENGINE.EGMStatusModule.JackPotModule;
using EGMENGINE.BillAccCTLModule.Impl.SSPBillAccServiceCTL.MessageTypes;
using Timer = System.Timers.Timer;
using EGMENGINE.IntegrityControlModule;

namespace EGMENGINE
{

    /// <summary>
    /// Class EGM definition. Singleton
    /// </summary>
    internal partial class EGM
    {

        internal bool onexpandedwin = false;
        /// <summary>
        /// Method for create a play (simulate a play);
        /// It is used by GameGUIController to serve to the GUI consumer a way to create, start and finish a Action Game Slot Play with the respective information like paylines or reel stops
        /// </summary>
        /// <returns>A SpinMarshall object</returns>
        internal SpinMarshall ActionGame_SlotCreatePlay(ActionGame game)
        {
            /* Exceeded credits before */
            EGMStatus.GetInstance().current_play.spin.slotplay.exceededCredits = 0;
            /* Credits Before */
            EGMStatus.GetInstance().current_play.spin.slotplay.creditsBefore = EGMStatus.GetInstance().current_play?.spin?.slotplay?.creditsAfter == 0 ? EGMStatus.GetInstance().currentAmount : EGMStatus.GetInstance().current_play?.spin?.slotplay?.creditsAfter ?? 0;
            /* Credit Value */
            EGMStatus.GetInstance().current_play.spin.slotplay.creditValue = EGMStatus.GetInstance().selectedCreditValue;
            /* Bet Credits */
            EGMStatus.GetInstance().current_play.spin.slotplay.totalBetAmount =GetRands(game.Bet);
            // CheckLimits
            decimal totalAmount = -EGMStatus.GetInstance().current_play.spin.slotplay.totalBetAmount;
            if (totalAmount < 0)
            {
                if (EGMStatus.GetInstance().currentAmount < ((totalAmount) * -1))
                {
                    EGMStatus.GetInstance().current_play.spin.slotplay.creditsAfter = EGMStatus.GetInstance().currentAmount;
                    EGMStatus.GetInstance().current_play.spin.slotplay.winCredits = 0;

                    return EGMStatus.GetInstance().current_play.spin;
                }
            }

            decimal startcreditsbase = onexpandedwin ? EGMStatus.GetInstance().current_play.spin.slotplay.creditsAfterExpandedWin : EGMStatus.GetInstance().current_play.spin.slotplay.creditsAfterBaseWin;
            
            
            EGMStatus.GetInstance().current_play.spin.slotplay.creditsOnPlay = (EGMStatus.GetInstance().current_play.spin.slotplay.creditsAfterActionGameWin == 0 ?
                                                                                startcreditsbase : EGMStatus.GetInstance().current_play.spin.slotplay.creditsAfterActionGameWin) + totalAmount;

            EGMStatus.GetInstance().current_play.spin.slotplay.actiongame_segment = game.Segment.Segment;
                ;
            /* Declare win = 0 */
            decimal win = 0;

            /*******************/
            /* CHECK ACTION GAME WIN */
            #region CHECK ACTION GAME WIN
            decimal actiongamewin = 0;
            if (game.Segment.PrizeType == "Extra spins")
            {
                EGMStatus.GetInstance().current_play.totalCurrentActionGames += game.Segment.PrizeAmount;
            }
            else if (game.Segment.PrizeType == "Payout")
            {
                win += GetRands(game.Segment.PrizeAmount);
                actiongamewin += GetRands(game.Segment.PrizeAmount);
                EGMStatus.GetInstance().current_play.actiongamewinning = true;

            }

            EGMStatus.GetInstance().current_play.spin.slotplay.actionGameWin = actiongamewin;

            #endregion



            jackpot_amount = 0;
            EGMStatus.GetInstance().current_play.spin.slotplay.creditsAfterActionGameWin = EGMStatus.GetInstance().current_play.spin.slotplay.creditsOnPlay + actiongamewin;
            EGMStatus.GetInstance().current_play.spin.slotplay.creditsAfter = EGMStatus.GetInstance().current_play.spin.slotplay.creditsAfterActionGameWin;
            EGMStatus.GetInstance().current_play.spin.slotplay.creditsAfterBaseWin = EGMStatus.GetInstance().current_play.spin.slotplay.creditsAfterActionGameWin;
            //EGMStatus.GetInstance().current_play.spin.slotplay.creditsAfterMisteryWin = sm.slotplay.creditsOnPlay + _MisteryWin_;
            EGMStatus.GetInstance().current_play.spin.slotplay.creditsAfterScatterWin+=actiongamewin+ totalAmount;
            //EGMStatus.GetInstance().current_play.spin.slotplay.creditsAfterExpandedWin = sm.slotplay.creditsAfterScatterWin + _TotaExpandedWin_;
            return ActionGame_FinishPlay(EGMStatus.GetInstance().current_play.spin, game, win);


        }
        /// <summary>
        /// Finish Play
        /// </summary>
        /// <param name="sm"></param>
        /// <param name="win"></param>
        /// <returns></returns>
        internal SpinMarshall ActionGame_FinishPlay(SpinMarshall sm, ActionGame game, decimal win)
        {
            sm.slotplay.winCredits = win;
            /*******************/
            /* Check remainin action games */
            #region REMAINING ACTION GAMES
            if (EGMStatus.GetInstance().current_play.actionGameIndex > -1)
            {
                sm.slotplay.remainingActionGames = EGMStatus.GetInstance().current_play.totalCurrentActionGames - EGMStatus.GetInstance().current_play.actionGameIndex - 1;
            }
            else
            {
                sm.slotplay.remainingActionGames = EGMStatus.GetInstance().current_play.totalCurrentActionGames;
            }
            #endregion

            EGMStatus.GetInstance().current_play.BaseCreditsWon = (int)(sm.slotplay.baseWin / sm.slotplay.creditValue);
            EGMStatus.GetInstance().current_play.ScatterCreditsWon = (int)(sm.slotplay.scatterWin / sm.slotplay.creditValue);
            EGMStatus.GetInstance().current_play.ExpandedCreditsWon = (int)(sm.slotplay.expandedWin / sm.slotplay.creditValue);
            EGMStatus.GetInstance().current_play.MisteryCreditsWon = (int)(sm.slotplay.misteryWin / sm.slotplay.creditValue);
            EGMStatus.GetInstance().current_play.ActionGameCreditsWon = (int)(sm.slotplay.actionGameWin / sm.slotplay.creditValue);
            EGMStatus.GetInstance().current_play.CreditsWon = EGMStatus.GetInstance().current_play.ActionGameCreditsWon +
                                                    EGMStatus.GetInstance().current_play.MisteryCreditsWon +
                                                    EGMStatus.GetInstance().current_play.ExpandedCreditsWon +
                                                    EGMStatus.GetInstance().current_play.ScatterCreditsWon +
                                                    EGMStatus.GetInstance().current_play.BaseCreditsWon;

            if (sm.slotplay.exceededCredits > 0)
            {

                AddPromotionalCredits(sm.slotplay.winCredits - sm.slotplay.exceededCredits, sm.slotplay.totalBetAmount);


                handpay_cashable_amount = sm.slotplay.exceededCredits;
                handpay_nonrestricted_amount = 0;
                InitiateHandpay();

            }
            else
            {
                AddPromotionalCredits(sm.slotplay.winCredits, sm.slotplay.totalBetAmount);
            }

            PersistAllData(true, true);

            return sm;
        }

        private decimal GetRands(int cents)
        {
            return (decimal)cents / 100m;
        }

        /// <summary>
        /// Create a new spin from math slotmathcore (math_ctrller)
        /// It takes betAmountcents and lines qtty
        /// </summary>
        /// <param name="betAmountcents"></param>
        /// <param name="lines"></param>
        /// <returns></returns>
        private BaseSpin CreateNewPlaySpin(int betAmountcents, int winAmountcents)
        {
            BaseSpin spin = math_ctrller.Spin("GID", betAmountcents, winAmountcents);
            decimal totalbet = default(decimal);
            decimal totalwin = default(decimal);
            totalbet = betAmountcents;
            totalwin = winAmountcents;
            spin.TotalBaseWin = (int)totalwin;
            spin.TotalBaseBet = (int)totalbet;
            spin.TotalExpandedWin = 0;
            spin.MisteryWin = 0;
            spin.TotalScatterWin = 0;
            EGMAccounting.GetInstance().plays.AddNewPlay(new LastPlay(DateTime.Now, EGMStatus.GetInstance().currentAmount, EGMStatus.GetInstance().currentAmount - totalbet + totalwin, totalbet, totalwin, GetRands(spin.TotalBaseWin), GetRands(0), GetRands(0), GetRands(0), EGMStatus.GetInstance().selectedCreditValue, totalwin, GetRands(0), totalwin, spin.StopPositions[0], spin.StopPositions[1], spin.StopPositions[2], spin.StopPositions[3], spin.StopPositions[4], spin.Winninglines.ToArray(), spin.PennyGames != null && spin.PennyGames.Count > 0, GetRands(spin.TotalPennyGamesWin()), spin.ActionGames != null && spin.ActionGames.Count > 0, GetRands(spin.TotalActionGamesWin())), persist: true);
            UpdateMetersByEvent(Meters_EventName.PlayStarted, new int[1] { ToSASFormat(totalbet) });
            if (totalwin > 0m)
            {
                UpdateMetersByEvent(Meters_EventName.PlayWon, new int[1] { ToSASFormat(totalwin) });
            }
            else
            {
                UpdateMetersByEvent(Meters_EventName.PlayLost);
            }
            UpdateMetersByEvent(Meters_EventName.PlayFinished);
            return spin;
        }
        /// <summary>
        /// Method for create a play (simulate a play);
        /// It is used by GameGUIController to serve to the GUI consumer a way to create, start and finish a Slot Play with the respective information like paylines or reel stops
        /// </summary>
        /// <returns>A SpinMarshall object</returns>
        internal SpinMarshall Game_SlotCreatePlay(int winAmountcents, int betAmountcents)
        {
            persiststatus = true;
            if (EGMStatus.GetInstance().current_play.spin != null && !EGMStatus.GetInstance().current_play.spin.slotplay.Finished)
            {
                return EGMStatus.GetInstance().current_play.spin;
            }
            BaseSpin spin = null;
            if (EGMStatus.GetInstance().current_play.actionGameIndex > -1 && EGMStatus.GetInstance().current_play.actionGameIndex < EGMStatus.GetInstance().current_play.getActionGames().Count())
            {
                ActionGame g = EGMStatus.GetInstance().current_play.getActionGames()[EGMStatus.GetInstance().current_play.actionGameIndex];
                return ActionGame_SlotCreatePlay(g);
            }
            if (EGMStatus.GetInstance().current_play.pennyGamesIndex > -1 && EGMStatus.GetInstance().current_play.pennyGamesIndex < EGMStatus.GetInstance().current_play.getPennyGames().Count())
            {
                spin = EGMStatus.GetInstance().current_play.getPennyGames()[EGMStatus.GetInstance().current_play.pennyGamesIndex];
                EGMStatus.GetInstance().current_play.pennyGamesIndex++;
            }
            SpinMarshall sm = new SpinMarshall();
            sm.slotplay = new SlotPlay();
            sm.slotplay.Finished = false;
            sm.slotplay.exceededCredits = 0m;
            sm.slotplay.creditsBefore = EGMStatus.GetInstance().currentAmount;
            sm.slotplay.creditValue = EGMStatus.GetInstance().selectedCreditValue;
            if (spin == null)
            {
                sm.slotplay.totalBetAmount = betAmountcents;
            }
            else
            {
                sm.slotplay.totalBetAmount = betAmountcents;
            }
            decimal _TotalBetAmount_ = sm.slotplay.totalBetAmount;
            sm.slotplay.creditsOnPlay = EGMStatus.GetInstance().currentAmount + _TotalBetAmount_;
            if (spin == null)
            {
                spin = CreateNewPlaySpin(betAmountcents, winAmountcents);
            }
            sm.slotplay.physicalReelStops = spin.StopPositions;
            decimal _TotalScatterWin_ = default(decimal);
            decimal _TotalBaseWin_ = default(decimal);
            _TotalBaseWin_ = spin.TotalBaseWin;
            sm.slotplay.winningLines = spin.Winninglines.ToArray();
            sm.slotplay.baseWin = _TotalBaseWin_;
            List<string> paylinesList = new List<string>();
            EGMStatus.GetInstance().current_play.payline1amount = default(decimal);
            EGMStatus.GetInstance().current_play.payline2amount = default(decimal);
            EGMStatus.GetInstance().current_play.payline3amount = default(decimal);
            EGMStatus.GetInstance().current_play.payline4amount = default(decimal);
            EGMStatus.GetInstance().current_play.payline5amount = default(decimal);
            int wincents = 0;
            foreach (Winningline line in spin.Winninglines)
            {
                switch (line.LineNo)
                {
                    case 1:
                        EGMStatus.GetInstance().current_play.payline1amount = GetRands(line.TotalWin);
                        break;
                    case 2:
                        EGMStatus.GetInstance().current_play.payline2amount = GetRands(line.TotalWin);
                        break;
                    case 3:
                        EGMStatus.GetInstance().current_play.payline3amount = GetRands(line.TotalWin);
                        break;
                    case 4:
                        EGMStatus.GetInstance().current_play.payline4amount = GetRands(line.TotalWin);
                        break;
                    case 5:
                        EGMStatus.GetInstance().current_play.payline5amount = GetRands(line.TotalWin);
                        break;
                }
                wincents += line.TotalWin;
                paylinesList.Add($"{{{line.LineNo}, {GetRands(line.TotalWin)}}}");
            }
            sm.slotplay.payLines = paylinesList.ToArray();
            if (sm.slotplay.payLines.Count() > 0)
            {
                EGMStatus.GetInstance().current_play.baseWinning = true;
            }
            if (EGMStatus.GetInstance().currentAmount + _TotalBetAmount_ + _TotalBaseWin_ <= EGMSettings.GetInstance().creditLimit)
            {
                if (_TotalBaseWin_ >= EGMSettings.GetInstance().jackpotLimit)
                {
                    jackpot_amount = GetRands(spin.TotalWin);
                    InitiateJackpot();
                    EGMStatus.GetInstance().current_play.baseWinning = false;
                    sm.slotplay.creditsAfter = EGMStatus.GetInstance().currentAmount + _TotalBetAmount_ + _TotalBaseWin_;
                    sm.slotplay.winCredits = _TotalBaseWin_;
                    return FinishPlay(sm, spin);
                }
                jackpot_amount = default(decimal);
                sm.slotplay.creditsAfterBaseWin = sm.slotplay.creditsOnPlay + _TotalBaseWin_;
                sm.slotplay.creditsAfterMisteryWin = sm.slotplay.creditsOnPlay;
                sm.slotplay.creditsAfterScatterWin = sm.slotplay.creditsAfterBaseWin + _TotalScatterWin_;
                sm.slotplay.creditsAfterExpandedWin = sm.slotplay.creditsAfterScatterWin;
                sm.slotplay.winCredits = _TotalBaseWin_;
                sm.slotplay.creditsAfter = EGMStatus.GetInstance().currentAmount + _TotalBetAmount_ + _TotalBaseWin_;
                return FinishPlay(sm, spin);
            }
            sm.slotplay.winCredits = _TotalBaseWin_;
            sm.slotplay.exceededCredits = EGMStatus.GetInstance().currentAmount + _TotalBetAmount_ + _TotalBaseWin_ - EGMSettings.GetInstance().creditLimit;
            sm.slotplay.creditsAfter = EGMSettings.GetInstance().creditLimit;
            return FinishPlay(sm, spin);

        }



        /// <summary>
        /// Finish Play
        /// </summary>
        /// <param name="sm"></param>
        /// <param name="spin"></param>
        /// <param name="win"></param>
        /// <returns></returns>
        internal SpinMarshall FinishPlay(SpinMarshall sm, BaseSpin spin)
        {

            /*******************/
            /* Check remainin penny games */
            #region REMAINING PENNY GAMES
            if (EGMStatus.GetInstance().current_play.pennyGamesIndex > -1)
            {
                sm.slotplay.remainingPennyGames = EGMStatus.GetInstance().current_play.totalCurrentPennyGames - EGMStatus.GetInstance().current_play.pennyGamesIndex;
            }
            else
            {
                sm.slotplay.remainingPennyGames = EGMStatus.GetInstance().current_play.totalCurrentPennyGames;
            }
            #endregion
            /* Check expanding symbol */
            #region CHECK EXPANDING SYMBOL
            if (spin.ExpandingSymbol != null)
            {
                EGMStatus.GetInstance().current_play.ExtraData.ExpandedSymbol = spin.ExpandingSymbol;

                sm.slotplay.expandedSymbol = EGMStatus.GetInstance().current_play.ExtraData.ExpandedSymbol;
            }
            else
            {
                sm.slotplay.expandedSymbol = EGMStatus.GetInstance().current_play.ExtraData.ExpandedSymbol;
            }



            EGMStatus.GetInstance().current_play.CreditsWon = (int)(sm.slotplay.baseWin);
            EGMStatus.GetInstance().current_play.BaseCreditsWon = (int)(sm.slotplay.baseWin);
            EGMStatus.GetInstance().current_play.ScatterCreditsWon = (int)(sm.slotplay.scatterWin);
            EGMStatus.GetInstance().current_play.ExpandedCreditsWon = (int)(sm.slotplay.expandedWin);
            EGMStatus.GetInstance().current_play.MisteryCreditsWon = (int)(sm.slotplay.misteryWin);
            EGMStatus.GetInstance().current_play.ActionGameCreditsWon = (int)(sm.slotplay.actionGameWin);

            #endregion

            if (EGMStatus.GetInstance().frontend_play.thisstatus != FrontEndPlayStatus.PennyGamesBonus)
            {
                /* CHECK REEL SET FOR CURRENT SLOT PLAY */
                #region CHECK REEL SET FOR CURRENT SLOT PLAY
                ReelSet reelset = math_ctrller.GRHS09LP01.ReelSets.FirstOrDefault();
                if (reelset != null)
                {
                    sm.slotplay.reel1Disposition = reelset.Reel0.Symbols.Select(s => s.Id).ToArray();
                    sm.slotplay.reel2Disposition = reelset.Reel1.Symbols.Select(s => s.Id).ToArray();
                    sm.slotplay.reel3Disposition = reelset.Reel2.Symbols.Select(s => s.Id).ToArray();
                    sm.slotplay.reel4Disposition = reelset.Reel3.Symbols.Select(s => s.Id).ToArray();
                    sm.slotplay.reel5Disposition = reelset.Reel4.Symbols.Select(s => s.Id).ToArray();

                }
                #endregion

            }
            else
            {
                /* CHECK REEL SET FOR CURRENT PENNY SLOT PLAY */
                #region CHECK REEL SET FOR CURRENT SLOT PLAY
                ReelSet reelset = math_ctrller.GRHS09LP01.ReelSets.Where(set => set.ExpandingSymbol?.Id == EGMStatus.GetInstance().current_play.ExtraData?.ExpandedSymbol?.Id).FirstOrDefault();
                if (reelset != null)
                {
                    sm.slotplay.reel1Disposition = reelset.Reel0.Symbols.Select(s => s.Id).ToArray();
                    sm.slotplay.reel2Disposition = reelset.Reel1.Symbols.Select(s => s.Id).ToArray();
                    sm.slotplay.reel3Disposition = reelset.Reel2.Symbols.Select(s => s.Id).ToArray();
                    sm.slotplay.reel4Disposition = reelset.Reel3.Symbols.Select(s => s.Id).ToArray();
                    sm.slotplay.reel5Disposition = reelset.Reel4.Symbols.Select(s => s.Id).ToArray();

                }
                #endregion
            }

            sm.slotplay.awardedPennyGames = 0;
            for (int k = 0; k < EGMStatus.GetInstance().current_play.pennyGamesIndex; k++)
            {
                int scattertotalwin = 0;
                if (EGMStatus.GetInstance().current_play.getPennyGames()[k].ScatterWin != null)
                {
                    scattertotalwin = EGMStatus.GetInstance().current_play.getPennyGames()[k].ScatterWin.TotalWin;
                }
                sm.slotplay.awardedPennyGames += GetRands(scattertotalwin +
                                                  EGMStatus.GetInstance().current_play.getPennyGames()[k].TotalExpandedWin +
                                                  EGMStatus.GetInstance().current_play.getPennyGames()[k].TotalBaseWin +
                                                  EGMStatus.GetInstance().current_play.getPennyGames()[k].MisteryWin);
            }



            EGMStatus.GetInstance().current_play.spin = sm;

            EGMStatus.GetInstance().current_play.spin.slotplay.physicalReelStops = sm.slotplay.physicalReelStops;
            EGMStatus.GetInstance().current_play.spin.slotplay.Finished = sm.slotplay.Finished;


            if (sm.slotplay.exceededCredits > 0)
            {

                AddPromotionalCredits(sm.slotplay.winCredits - sm.slotplay.exceededCredits, sm.slotplay.totalBetAmount);


                handpay_cashable_amount = sm.slotplay.exceededCredits;
                handpay_nonrestricted_amount = 0;
                EGMStatus.GetInstance().current_play.baseWinning = false;
                EGMStatus.GetInstance().current_play.pennyGamesIndex = -1;
                EGMStatus.GetInstance().current_play.scatter_win = false;

                InitiateHandpay();

            }
            else
            {
                AddPromotionalCredits(sm.slotplay.winCredits, sm.slotplay.totalBetAmount);
            }
          
            PersistAllData(true, true);
            return sm;
        }

        internal SpinMarshall? Game_Slot_GetLastPlay()
        {
            return EGMStatus.GetInstance().current_play.spin;
        }

       
    }


}

