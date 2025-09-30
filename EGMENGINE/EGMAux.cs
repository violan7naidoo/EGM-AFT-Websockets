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
using Newtonsoft.Json;
using System.Reflection;

namespace EGMENGINE
{

    /// <summary>
    /// Class EGM definition. Singleton
    /// </summary>
    internal partial class EGM
    {

        #region AUXILIARY METHODS

        private decimal handpay_cashable_amount;
        private decimal handpay_nonrestricted_amount;
        /// <summary>
        /// private method. Auxiliary function to link the egm settings sas id to sas address
        /// </summary>
        private void LinkEGMSettingsSASIdToSASAddress()
        {
            // Set SAS Address
            byte address = (byte)EGMSettings.GetInstance().sasSettings.mainConfiguration.SASId;
            if (address > 0x20) address = 0x01;
            SASCTL.GetInstance().SetSASAddress((byte)address);

        }

        /// <summary>
        /// private method. Auxiliary function to enter the maintenance mode with fully logic
        /// </summary>
        private void EnterMaintenanceMode()
        {

            EGMStatus.GetInstance().maintenanceMode = true;

        }

        /// <summary>
        /// private method. Auxiliary function to exit the maintenance mode with fully logic
        /// </summary>
        private void ExitMaintenanceMode()
        {

            EGMStatus.GetInstance().maintenanceMode = false;

        }

        /// <summary>
        /// private method. Auxiliary function to convert a decimal value to a SASFormat, using the SASReportedDenomination. (Division)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private int ToSASFormat(decimal value)
        {
            return (int)(value / EGMSettings.GetInstance().sasSettings.mainConfiguration.SASReportedDenomination);
        }

        /// <summary>
        /// private method. Auxiliary function to convert a integer value, in SASFormat, to a decimal value using the SASReportedDenominaiton. (multiplication
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private decimal FromSASFormat(int value)
        {
            return (decimal)((decimal)value * (decimal)EGMSettings.GetInstance().sasSettings.mainConfiguration.SASReportedDenomination);
        }
        /// <summary>
        /// EGM Auxiliary method. Updates the current credit in all sections of Engine: SAS, Accounting and Status based on current credits, restricted credits and non restricted 
        /// </summary>
        /// <param name="totalwin"></param>
        /// <param name="totalbet"></param>
        internal void AddPromotionalCredits(decimal totalwin, decimal totalbet)
        {
            if (EGMStatus.GetInstance().currentRestrictedAmount >= totalbet)
            {
                AddAmount(totalwin, -totalbet, 0, false);
            }
            else
            {
                decimal remainForNonRestricted = totalbet - EGMStatus.GetInstance().currentRestrictedAmount;
                if (EGMStatus.GetInstance().currentNonRestrictedAmount >= remainForNonRestricted)
                {
                    AddAmount(totalwin, -EGMStatus.GetInstance().currentRestrictedAmount, -remainForNonRestricted, false);

                }
                else
                {
                    decimal remainForCashable = remainForNonRestricted - EGMStatus.GetInstance().currentNonRestrictedAmount;
                    AddAmount(totalwin - remainForCashable, -EGMStatus.GetInstance().currentRestrictedAmount, -EGMStatus.GetInstance().currentNonRestrictedAmount, false);

                }
            }
        }

        /// <summary>
        /// EGM Auxiliary method. Updates the current credit in all sections of Engine: SAS, Accounting and Status 
        /// </summary>
        /// <param name="cashable"></param>
        /// <param name="restricted"></param>
        /// <param name="nonrestricted"></param>
        private void AddAmount(decimal cashable, decimal restricted, decimal nonrestricted, bool frontend)
        {

            // Add to credits the amounts
            EGMStatus.GetInstance().AddAmount(cashable, restricted, nonrestricted);
            // Update Current SAS Info attributes
            SASCTL.GetInstance().UpdateSASInfo(null, null, EGMStatus.GetInstance().currentCashableAmount, EGMStatus.GetInstance().currentRestrictedAmount, EGMStatus.GetInstance().currentNonRestrictedAmount, null, null);
            // Update SAS Credits
            if (cashable != 0)
                SASCTL.GetInstance().SetCredits((byte)SAS_CREDIT_TYPE.SAS_CREDIT_CASHABLE, EGMStatus.GetInstance().currentCashableAmount);
            if (restricted != 0)
                SASCTL.GetInstance().SetCredits((byte)SAS_CREDIT_TYPE.SAS_CREDIT_RESTRICTED, EGMStatus.GetInstance().currentRestrictedAmount);
            if (nonrestricted != 0)
                SASCTL.GetInstance().SetCredits((byte)SAS_CREDIT_TYPE.SAS_CREDIT_NONRESTRICTED, EGMStatus.GetInstance().currentNonRestrictedAmount);
            // Update events by event
            UpdateMetersByEvent(Meters_EventName.CreditsModified);

        }
        /// <summary>
        /// EGM Auxiliary method. It is used to  initiate a handpay
        /// </summary>
        private void InitiateHandpay()
        {

            // Check that current cashable credits is non zero
            if (handpay_cashable_amount + handpay_nonrestricted_amount > 0)
            {
                // TRANSITION: Handpay from Idle -> HandpayPending
                EGMStatus.GetInstance().current_handpay.Transition(HandpayStatus.HandpayPending);
            }

        }

        /// <summary>
        /// EGM Auxiliary method. It is used to  initiate a jackpot
        /// </summary>
        private void InitiateJackpot()
        {

            EGMStatus.GetInstance().current_jackpot.Transition(JackPotStatus.JackpotPending);

        }


        /// <summary>
        /// EGM Auxiliary method. Aid to the other method of EGM to add a system logs
        /// </summary>
        /// <param name="event_"></param>
        /// <param name="user"></param>
        private void AddSystemLog(string event_, string user)
        {
            EGMAccounting.GetInstance().systemlogs.AddNewSystemLog(new SystemLog(DateTime.Now, event_, user), true);

        }

        private int ToGLIInt(int a)
        {
            return a < 0 ? (a - int.MinValue + 1) : a;
        }

        /// <summary>
        /// EGM Add Meter. SAS function to add to the meter the argument [Meter=Meter+Value]
        /// </summary>
        /// <param name="code"></param>
        /// <param name="value"></param>
        private void EGMIncrementMeter(byte code, int value, bool saspersist)
        {
            int oldvalue = EGMAccounting.GetInstance().GetMeter(code);
            EGMAccounting.GetInstance().UpdateMeter(code, ToGLIInt(oldvalue + value));
            if (saspersist)
                SASCTL.GetInstance().SetMeter("", code, ToGLIInt(oldvalue + value), false);

        }
        /// <summary>
        /// EGM Update Meter. SAS function to update values, that is replace the old value with the argument [Meter=Value]
        /// </summary>
        /// <param name="code"></param>
        /// <param name="value"></param>
        private void EGMUpdateMeter(byte code, int value)
        {
            EGMAccounting.GetInstance().UpdateMeter(code, ToGLIInt(value));
            SASCTL.GetInstance().SetMeter("", code, ToGLIInt(value), false);
        }

        /// <summary>
        /// EGM Add Meter. SAS function to add to the meter the argument [Meter=Meter+Value]
        /// </summary>
        /// <param name="code"></param>
        /// <param name="value"></param>
        private void EGMIncrementMeter(string name, int value)
        {
            int oldvalue = EGMAccounting.GetInstance().GetMeter(name);
            EGMAccounting.GetInstance().UpdateMeter(name, ToGLIInt(oldvalue + value));

        }
        /// <summary>
        /// EGM Update Meter. SAS function to update values, that is replace the old value with the argument [Meter=Value]
        /// </summary>
        /// <param name="code"></param>
        /// <param name="value"></param>
        private void EGMUpdateMeter(string name, int value)
        {
            EGMAccounting.GetInstance().UpdateMeter(name, ToGLIInt(value));
        }

        /// <summary>
        /// Add a tilt by its description or name
        /// </summary>
        /// <param name="name"></param>
        private void AddCriticalTilt(string name)
        {


            if (EGMStatus.GetInstance().currentTilts.Where(t => t.Description == name).Count() == 0)
            {
                EGMStatus.GetInstance().criticaltilt = true;
                UpdateMetersByEvent(Meters_EventName.Tilt);
                EGMStatus.GetInstance().currentTilts.Add(new Tilt(DateTime.Now, name));
                SASCTL.GetInstance().SetSASBusy(true);
            }

        }

             /// <summary>
        /// Add a tilt by its description or name
        /// </summary>
        /// <param name="name"></param>
        private void AddTilt(string name, bool iscritical)
        {

            criticaltilt = true;

            if (EGMStatus.GetInstance().currentTilts.Where(t => t.Description == name).Count() == 0)
            {
                UpdateMetersByEvent(Meters_EventName.Tilt);
                EGMStatus.GetInstance().currentTilts.Add(new Tilt(DateTime.Now, name));
                SASCTL.GetInstance().SetSASBusy(true);
            }

        }

        /// <summary>
        /// Add a tilt by its description or name
        /// </summary>
        /// <param name="name"></param>
        private void AddTilt(string name)
        {


            if (EGMStatus.GetInstance().currentTilts.Where(t => t.Description == name).Count() == 0)
            {
                UpdateMetersByEvent(Meters_EventName.Tilt);
                EGMStatus.GetInstance().currentTilts.Add(new Tilt(DateTime.Now, name));
                SASCTL.GetInstance().SetSASBusy(true);
            }

        }

        /// <summary>
        /// Remove a tilt by its description or name
        /// </summary>
        /// <param name="name"></param>
        private void RemoveTilt(string name)
        {
            EGMStatus.GetInstance().currentTilts.RemoveAll(t => t.Description == name);
            if (EGMStatus.GetInstance().currentTilts.Count() == 0)
            {
                SASCTL.GetInstance().SetSASBusy(false);
            }
        }

        private enum Meters_EventName
        {
            EGMInitialization,
            /// <summary>
            /// Requires 1 value: TotalHandpayCancelledCredits;
            /// </summary>
            CashoutPressed,
            /// <summary>
            /// Requires 1 value: TotalHandpayCredits;
            /// </summary>
            HandpayReset,
            /// <summary>
            /// Requires 1 value: TotalCoinIn;
            /// </summary>
            PlayStarted,
            /// <summary>
            /// Requires 1 value: TotalCoinOut;
            /// </summary>
            PlayWon,
            PlayLost,
            PlayFinished,
            SlotDoorOpen,
            SlotDoorClosed,
            BellyDoorOpen,
            BellyDoorClosed,
            CashboxDoorOpen,
            CashboxDoorClosed,
            CardCageDoorOpen,
            CardCageDoorClosed,
            DropDoorOpen,
            DropDoorClosed,
            Tilt,
            LogicDoorOpen,
            LogicDoorClosed,
            BillJam,
            StackerOpen,
            StackerClosed,
            SASInterfaceError,
            /// <summary>
            /// Requires 1 value: TotalValueOfBillsCurrentlyInTheStacker;
            /// </summary>
            BillInserted,
            PartialRamClear,
            CreditsModified,
            /// <summary>
            /// Requires 1 value: TotalJackpotValue
            /// </summary>
            JackpotOcurred,
            /// <summary>
            /// Requires 1 value: AFT transfer value
            /// </summary>
            AFTTransferCompleted
        }

        private void SetOutput(OutputName output, bool switch_)
        {
            if (switch_)
                gpioCTL.SetOutputOn(output);
            else
                gpioCTL.SetOutputOff(output);

        }

        private bool CheckConditionForFrontendPennyState(FrontEndPlayPennyStatus st)
        {
            switch (st)
            {
                case FrontEndPlayPennyStatus.WinningState:// FOR WINNING STATE
                    return EGMStatus.GetInstance().current_play.baseWinning // winning flag
                        && EGMStatus.GetInstance().current_animation_event != AnimationEvent.ReelsSpinning;// reel is not spinning       
                case FrontEndPlayPennyStatus.ExpandingWinningState:// FOR WINNING STATE
                    return EGMStatus.GetInstance().current_play.expanded_win // expanded win flag
                        && EGMStatus.GetInstance().current_animation_event != AnimationEvent.ReelsSpinning;// reel is not spinning
                case FrontEndPlayPennyStatus.ScatterWinningState:// FOR SCATTER WINNING STATE
                    return EGMStatus.GetInstance().current_play.actionGameIndex == -1 // No action games 
                        && EGMStatus.GetInstance().current_play.scatter_win  // scatter win flag
                        && EGMStatus.GetInstance().current_animation_event != AnimationEvent.ReelsSpinning; // reel is not spinning        
                case FrontEndPlayPennyStatus.Playing: // FOR WAITING FOR PLAYING
                    return EGMStatus.GetInstance().current_animation_event == AnimationEvent.ReelsSpinning; // reel is spinning
                default:
                    return true;

            }
        }

        /// <summary>
        /// Reprocess PennyGames EGM Status Frontend
        /// Called by GameGUIController, it is used to update continuosly the EGMStatus frontend      /// </summary>
        private void ReprocessEGMStatus_Frontend_PennyGames()
        {
            if (CheckConditionForFrontendPennyState(FrontEndPlayPennyStatus.WinningState)) // Winning and not ReelsSpinning
            {
                if (EGMStatus.GetInstance().frontend_play_penny.Transition(FrontEndPlayPennyStatus.WinningState))
                {
                    ReprocessEGMStatus_OutputByPennyFrontendState(FrontEndPlayPennyStatus.WinningState);
                }
            }
            else if (CheckConditionForFrontendPennyState(FrontEndPlayPennyStatus.ScatterWinningState))
            {

                if (EGMStatus.GetInstance().frontend_play_penny.Transition(FrontEndPlayPennyStatus.ScatterWinningState))
                {
                    ReprocessEGMStatus_OutputByPennyFrontendState(FrontEndPlayPennyStatus.ScatterWinningState);

                }
            }
            else if (CheckConditionForFrontendPennyState(FrontEndPlayPennyStatus.ExpandingWinningState))
            {
                if (EGMStatus.GetInstance().frontend_play_penny.Transition(FrontEndPlayPennyStatus.ExpandingWinningState))
                {
                    ReprocessEGMStatus_OutputByPennyFrontendState(FrontEndPlayPennyStatus.ExpandingWinningState);

                }
            }
            else if (EGMStatus.GetInstance().current_play.actionGameIndex >= 0)
            {
                if (EGMStatus.GetInstance().current_animation_event == AnimationEvent.WheelSpinning)
                {
                    if (EGMStatus.GetInstance().frontend_play_penny.Transition(FrontEndPlayPennyStatus.ActionGamePlaying))
                    {
                        ReprocessEGMStatus_OutputByPennyFrontendState(FrontEndPlayPennyStatus.ActionGamePlaying);

                        billAcc.DisableBillAcceptor();
                    }
                }
                else if (EGMStatus.GetInstance().current_play.actiongamewinning && EGMStatus.GetInstance().current_animation_event != AnimationEvent.WheelSpinning)
                {
                    if (EGMStatus.GetInstance().frontend_play_penny.Transition(FrontEndPlayPennyStatus.ActionGameWinning))
                    {
                        ReprocessEGMStatus_OutputByPennyFrontendState(FrontEndPlayPennyStatus.ActionGameWinning);

                    }

                }
                else
                {
                    // action games index passed the total count, that is , already processed the action games
                    if (EGMStatus.GetInstance().current_play.actionGameIndex == EGMStatus.GetInstance().current_play.getActionGames().Count())
                    {
                        EGMStatus.GetInstance().current_play.actionGameIndex = -1;
                        EGMStatus.GetInstance().current_play.totalCurrentActionGames = 0;

                   
                    }
                    else if (EGMStatus.GetInstance().frontend_play_penny.Transition(FrontEndPlayPennyStatus.ActionGamePlayable))
                    {
                        EGMStatus.GetInstance().current_play.ResetPlay();
                        EGMDataPersisterCTL.GetInstance().PersistEGMSlotPlay();

                        ReprocessEGMStatus_OutputByPennyFrontendState(FrontEndPlayPennyStatus.ActionGamePlayable);

                        billAcc.DisableBillAcceptor(); // ?
                    }
                }
            }
            else if (CheckConditionForFrontendPennyState(FrontEndPlayPennyStatus.Playing)) // ReelsSpinning
            {
                if (EGMStatus.GetInstance().frontend_play_penny.Transition(FrontEndPlayPennyStatus.Playing))
                {
                    SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.Playing);
                    ReprocessEGMStatus_OutputByPennyFrontendState(FrontEndPlayPennyStatus.Playing);

                }
            }
            else if (EGMStatus.GetInstance().current_animation_event == AnimationEvent.ExpandedSymbolDrawFinished) // ExpandedSymbolDrawFinished
            {
                EGMStatus.GetInstance().current_animation_event = null;
                if (EGMStatus.GetInstance().frontend_play_penny.thisstatus == FrontEndPlayPennyStatus.Playable)
                {

                }
            }
            else if (EGMStatus.GetInstance().current_animation_event == AnimationEvent.ExpandedSymbolDrawing)
            {

            }
            else
            {
                // action games index passed the total count, that is , already processed the action games
                if (EGMStatus.GetInstance().current_play.actionGameIndex == EGMStatus.GetInstance().current_play.getActionGames().Count())
                {
                    EGMStatus.GetInstance().current_play.actionGameIndex = -1;
                    EGMStatus.GetInstance().current_play.totalCurrentActionGames = 0;
                }

                // penny games index passed the total count, that is , already processed the penny games
                if (EGMStatus.GetInstance().current_play.pennyGamesIndex == EGMStatus.GetInstance().current_play.getPennyGames().Count())
                {
                    EGMStatus.GetInstance().current_play.pennyGamesIndex = -1;
                    EGMStatus.GetInstance().current_play.totalCurrentPennyGames = 0;

                    if (EGMStatus.GetInstance().frontend_play_penny.Transition(FrontEndPlayPennyStatus.Idle))
                    {
                        EGMStatus.GetInstance().current_play.ExtraData.ExpandedSymbol = null;
                    }

                }
                else
                {
                    if (EGMStatus.GetInstance().frontend_play_penny.Transition(FrontEndPlayPennyStatus.Playable))
                    {
                        EGMStatus.GetInstance().current_play.ResetPlay();
                        EGMDataPersisterCTL.GetInstance().PersistEGMSlotPlay();

                        ReprocessEGMStatus_OutputByPennyFrontendState(FrontEndPlayPennyStatus.Playable);
                    }
                }
            }

        }

        private bool CheckConditionForFrontendState(FrontEndPlayStatus st)
        {


            switch (st)
            {
                case FrontEndPlayStatus.PostFRCInitialization: // FOR FULL RAM CLEAR INITIALIZATION
                    return EGMStatus.GetInstance().fullramclearperformed; // full ram clear performed
                case FrontEndPlayStatus.Tilt: // FOR TILT
                    return EGMStatus.GetInstance().currentTilts.Count() > 0 // At least one active tilt
                        && !EGMStatus.GetInstance().menuActive // No menu activated
                        && !EGMStatus.GetInstance().disabledByHost;
                case FrontEndPlayStatus.CriticalTilt: // FOR TILT
                    return EGMStatus.GetInstance().currentTilts.Count() > 0 // At least one active tilt
                        && (EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.Tilt
                        || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.CriticalTilt)
                        && EGMStatus.GetInstance().criticaltilt
                        && !EGMStatus.GetInstance().current_play.baseWinning // No winning
                        && !EGMStatus.GetInstance().current_play.actiongamewinning // No action game winning
                        && EGMStatus.GetInstance().current_play.pennyGamesIndex == -1
                        && !EGMStatus.GetInstance().menuActive // No menu activated
                        && !EGMStatus.GetInstance().disabledByHost;
                case FrontEndPlayStatus.UILock: // FOR UILOCK
                    return SASCTL.GetInstance().AFTInProgress() ||
                           (EGMStatus.GetInstance().current_collect.WorkInProgress()
                         && !EGMStatus.GetInstance().current_handpay.WorkInProgress()); // AFT In Progress
                case FrontEndPlayStatus.PennyGamesBonus: // FOR PENNYGAMESBONUS
                    return EGMStatus.GetInstance().current_play.pennyGamesIndex >= 0;
                case FrontEndPlayStatus.MaintenanceMode: // FOR MAINTENANCE MODE
                    return EGMStatus.GetInstance().maintenanceMode // MaintenanceMode flag
                        && !EGMStatus.GetInstance().menuActive; // No Menu Active
                case FrontEndPlayStatus.Menu: // FOR MENU
                    return (EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.Playable
                        || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.WaitingForBet
                        || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.Menu
                        || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.PostFRCInitialization
                        || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.WaitingForCredits
                        || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.Starting
                        || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.MaintenanceMode
                        || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.DisabledByHost
                        || EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.Tilt)
                        && EGMStatus.GetInstance().menuActive
                        && !EGMStatus.GetInstance().fullramclearperformed; // Menu Active
                case FrontEndPlayStatus.ShowingHelp: // FOR SHOWING HELP
                    return EGMStatus.GetInstance().showInfo; // Show Info flag 
                case FrontEndPlayStatus.Handpay: // FOR HANDPAY
                    return EGMStatus.GetInstance().current_handpay.WorkInProgress() // current handpay in progress
                        && EGMStatus.GetInstance().current_animation_event != AnimationEvent.ReelsSpinning; // reel is not spinning
                case FrontEndPlayStatus.Jackpot: // FOR JACKPOT
                    return EGMStatus.GetInstance().current_jackpot.WorkInProgress() // current jackpot in progress
                        && EGMStatus.GetInstance().current_animation_event != AnimationEvent.ReelsSpinning; // reel is not spinning
                case FrontEndPlayStatus.WinningState:// FOR WINNING STATE
                    return EGMStatus.GetInstance().current_play.baseWinning // winning flag
                        && EGMStatus.GetInstance().current_animation_event != AnimationEvent.ReelsSpinning;// reel is not spinning
                case FrontEndPlayStatus.MisteryWinningState:// FOR MISTERY WINNING STATE
                    return EGMStatus.GetInstance().current_play.mistery_win // mistery win flag
                        && EGMStatus.GetInstance().current_animation_event != AnimationEvent.ReelsSpinning; // reel is not spinning
                case FrontEndPlayStatus.ScatterWinningState:// FOR SCATTER WINNING STATE
                    return EGMStatus.GetInstance().current_play.scatter_win  // scatter win flag
                        && EGMStatus.GetInstance().current_animation_event != AnimationEvent.ReelsSpinning; // reel is not spinning
                case FrontEndPlayStatus.WaitingForCredits:// FOR WAITING FOR CREDITS
                    return (EGMStatus.GetInstance().currentAmount <  // current frontend credits less than
                              (EGMStatus.GetInstance().betxline * EGMStatus.GetInstance().betLines)  // bet by line times lines
                                 * EGMStatus.GetInstance().selectedCreditValue)// times selected denomination monetaryvalue
                                 && !EGMStatus.GetInstance().disabledByHost;
                case FrontEndPlayStatus.WaitingForBet: // FOR WAITING FOR WAITING FOR BET
                    return (EGMStatus.GetInstance().betxline * EGMStatus.GetInstance().betLines == 0)  // bet by line is zero, or lines is zero
                        && !EGMStatus.GetInstance().disabledByHost;
                case FrontEndPlayStatus.Playing: // FOR WAITING FOR PLAYING
                    return EGMStatus.GetInstance().current_animation_event == AnimationEvent.ReelsSpinning; // reel is spinning
                case FrontEndPlayStatus.ActionGamePlayable:
                    return EGMStatus.GetInstance().current_play.actionGameIndex >= 0
                        && !EGMStatus.GetInstance().current_play.actiongamewinning
                        && EGMStatus.GetInstance().current_animation_event != AnimationEvent.WheelSpinning;
                case FrontEndPlayStatus.ActionGameWinning:
                    return EGMStatus.GetInstance().current_play.actionGameIndex >= 0
                        && EGMStatus.GetInstance().current_play.actiongamewinning
                        && EGMStatus.GetInstance().current_animation_event != AnimationEvent.WheelSpinning;
                case FrontEndPlayStatus.ActionGamePlaying:
                    return EGMStatus.GetInstance().current_play.actionGameIndex >= 0
                        && EGMStatus.GetInstance().current_animation_event == AnimationEvent.WheelSpinning;
                case FrontEndPlayStatus.DisabledByHost:
                    return EGMStatus.GetInstance().disabledByHost
                        && !EGMStatus.GetInstance().current_play.baseWinning // No winning
                        && !EGMStatus.GetInstance().current_play.actiongamewinning // No action game winning
                        && EGMStatus.GetInstance().current_play.pennyGamesIndex == -1
                        && EGMStatus.GetInstance().current_animation_event != AnimationEvent.ReelsSpinning
                        && !EGMStatus.GetInstance().menuActive // No menu activated
                        && EGMStatus.GetInstance().frontend_play.thisstatus != FrontEndPlayStatus.Jackpot
                        && EGMStatus.GetInstance().frontend_play.thisstatus != FrontEndPlayStatus.ActionGameWinning
                        && EGMStatus.GetInstance().frontend_play.thisstatus != FrontEndPlayStatus.ActionGamePlayable
                        && EGMStatus.GetInstance().frontend_play.thisstatus != FrontEndPlayStatus.ActionGamePlaying
                        && EGMStatus.GetInstance().frontend_play.thisstatus != FrontEndPlayStatus.MisteryWinningState
                        && EGMStatus.GetInstance().frontend_play.thisstatus != FrontEndPlayStatus.ScatterWinningState
                        && EGMStatus.GetInstance().frontend_play.thisstatus != FrontEndPlayStatus.PennyGamesBonus;

                default:
                    return true;

            }

        }

        /// <summary>
        /// Reprocess EGM Status Frontend
        /// Called by GameGUIController, it is used to update continuosly the EGMStatus frontend
        /// </summary>
        private void ReprocessEGMStatus_Frontend()
        {
            if (CheckConditionForFrontendState(FrontEndPlayStatus.PostFRCInitialization))
            {
                Menu_EnterToMenu();

                if (EGMStatus.GetInstance().frontend_play.Transition(FrontEndPlayStatus.PostFRCInitialization))
                {
                    billAcc.DisableBillAcceptor();
                    SASCTL.GetInstance().RejectTransfer(true, true);


                }
            }
            else if (CheckConditionForFrontendState(FrontEndPlayStatus.CriticalTilt))
            {
                if (EGMStatus.GetInstance().frontend_play.Transition(FrontEndPlayStatus.CriticalTilt))
                {
                    billAcc.DisableBillAcceptor();

                    ReprocessEGMStatus_OutputByFrontendState(FrontEndPlayStatus.CriticalTilt);

                }
                SASCTL.GetInstance().RejectTransfer(true, true);

            }
            else if (CheckConditionForFrontendState(FrontEndPlayStatus.Tilt))
            {
                if (EGMStatus.GetInstance().frontend_play.Transition(FrontEndPlayStatus.Tilt))
                {
                    billAcc.DisableBillAcceptor();
                    ReprocessEGMStatus_OutputByFrontendState(FrontEndPlayStatus.Tilt);
                }
                SASCTL.GetInstance().RejectTransfer(true, true);

            }
            else if (CheckConditionForFrontendState(FrontEndPlayStatus.UILock))
            {
                if (EGMStatus.GetInstance().frontend_play.Transition(FrontEndPlayStatus.UILock))
                {
                    ReprocessEGMStatus_OutputByFrontendState(FrontEndPlayStatus.UILock);
                    billAcc.DisableBillAcceptor();
                    //  SASCTL.GetInstance().RejectTransfer(true, true);
                }
            }
            else if (CheckConditionForFrontendState(FrontEndPlayStatus.PennyGamesBonus))
            {
                if (!EGMStatus.GetInstance().autoSpin)
                {
                    try { UIPriorityEvent(this, EventType.AutoSpinToggled, new EventArgs()); } catch { }

                    EGMStatus.GetInstance().autoSpin = true;

                }
                /* PennyGamesBonus */
                if (EGMStatus.GetInstance().frontend_play.Transition(FrontEndPlayStatus.PennyGamesBonus))
                {
                    ReprocessEGMStatus_OutputByFrontendState(FrontEndPlayStatus.PennyGamesBonus);

                    billAcc.DisableBillAcceptor(); // ?
                    if (EGMStatus.GetInstance().frontend_play_penny.Transition(FrontEndPlayPennyStatus.ExpandedSymbolDraw))
                    {
                        EGMStatus.GetInstance().current_animation_event = AnimationEvent.ExpandedSymbolDrawing;
                        SASCTL.GetInstance().RejectTransfer(true, true);
                    }
                }
                if (EGMStatus.GetInstance().frontend_play.thisstatus == FrontEndPlayStatus.PennyGamesBonus)
                {
                    ReprocessEGMStatus_Frontend_PennyGames();
                }
                SASCTL.GetInstance().RejectTransfer(true, true);

            }
            else if (CheckConditionForFrontendState(FrontEndPlayStatus.MaintenanceMode))
            {
                if (EGMStatus.GetInstance().frontend_play.Transition(FrontEndPlayStatus.MaintenanceMode))
                {
                    ReprocessEGMStatus_OutputByFrontendState(FrontEndPlayStatus.MaintenanceMode);

                    billAcc.DisableBillAcceptor();
                }
                SASCTL.GetInstance().RejectTransfer(true, true);

            }
            else if (CheckConditionForFrontendState(FrontEndPlayStatus.Menu))
            {
                if (EGMStatus.GetInstance().frontend_play.Transition(FrontEndPlayStatus.Menu))
                {
                    billAcc.DisableBillAcceptor();
                    ReprocessEGMStatus_OutputByFrontendState(FrontEndPlayStatus.Menu);
                }
                SASCTL.GetInstance().RejectTransfer(true, true);

            }
            else if (CheckConditionForFrontendState(FrontEndPlayStatus.ShowingHelp))
            {
                if (EGMStatus.GetInstance().frontend_play.Transition(FrontEndPlayStatus.ShowingHelp))
                {
                    ReprocessEGMStatus_OutputByFrontendState(FrontEndPlayStatus.ShowingHelp);

                    billAcc.DisableBillAcceptor();
                }
            }
            else if (CheckConditionForFrontendState(FrontEndPlayStatus.Handpay))
            {
                if (EGMStatus.GetInstance().frontend_play.Transition(FrontEndPlayStatus.Handpay))
                {
                    billAcc.DisableBillAcceptor();

                    ReprocessEGMStatus_OutputByFrontendState(FrontEndPlayStatus.Handpay);



                }
                SASCTL.GetInstance().RejectTransfer(true, true);

            }
            else if (CheckConditionForFrontendState(FrontEndPlayStatus.Jackpot))
            {
                if (EGMStatus.GetInstance().frontend_play.Transition(FrontEndPlayStatus.Jackpot))
                {
                    ReprocessEGMStatus_OutputByFrontendState(FrontEndPlayStatus.Jackpot);

                }

                SASCTL.GetInstance().RejectTransfer(true, true);

            }
            else if (CheckConditionForFrontendState(FrontEndPlayStatus.WinningState))
            {
                if (EGMStatus.GetInstance().frontend_play.Transition(FrontEndPlayStatus.WinningState))
                {
                    ReprocessEGMStatus_OutputByFrontendState(FrontEndPlayStatus.WinningState);

                }
                SASCTL.GetInstance().RejectTransfer(true, true);

            }
            else if (CheckConditionForFrontendState(FrontEndPlayStatus.MisteryWinningState))
            {
                if (EGMStatus.GetInstance().frontend_play.Transition(FrontEndPlayStatus.MisteryWinningState))
                {
                    ReprocessEGMStatus_OutputByFrontendState(FrontEndPlayStatus.ActionGamePlaying);

                }
                SASCTL.GetInstance().RejectTransfer(true, true);

            }
            else if (CheckConditionForFrontendState(FrontEndPlayStatus.ActionGamePlayable))
            {
                // action games index passed the total count, that is , already processed the action games
                if (EGMStatus.GetInstance().current_play.actionGameIndex == EGMStatus.GetInstance().current_play.getActionGames().Count())
                {
                    EGMStatus.GetInstance().current_play.actionGameIndex = -1;
                    EGMStatus.GetInstance().current_play.totalCurrentActionGames = 0;

                }
                else
                {

                    if (EGMStatus.GetInstance().frontend_play.Transition(FrontEndPlayStatus.ActionGamePlayable))
                    {
                        EGMStatus.GetInstance().current_play.ResetPlay();
                        EGMDataPersisterCTL.GetInstance().PersistEGMSlotPlay();

                        ReprocessEGMStatus_OutputByFrontendState(FrontEndPlayStatus.ActionGamePlayable);
                        SASCTL.GetInstance().RejectTransfer(true, true);

                        billAcc.DisableBillAcceptor(); // ?
                    }

                }
            }
            else if (CheckConditionForFrontendState(FrontEndPlayStatus.ActionGamePlaying))
            {
                if (EGMStatus.GetInstance().frontend_play.Transition(FrontEndPlayStatus.ActionGamePlaying))
                {
                    ReprocessEGMStatus_OutputByFrontendState(FrontEndPlayStatus.ActionGamePlaying);
                    SASCTL.GetInstance().RejectTransfer(true, true);

                    billAcc.DisableBillAcceptor();
                }

            }
            else if (CheckConditionForFrontendState(FrontEndPlayStatus.ActionGameWinning))
            {
                if (EGMStatus.GetInstance().frontend_play.Transition(FrontEndPlayStatus.ActionGameWinning))
                {
                    ReprocessEGMStatus_OutputByFrontendState(FrontEndPlayStatus.ActionGameWinning);
                    SASCTL.GetInstance().RejectTransfer(true, true);
                }

            }           
            else if (CheckConditionForFrontendState(FrontEndPlayStatus.ScatterWinningState))
            {
                if (EGMStatus.GetInstance().frontend_play.Transition(FrontEndPlayStatus.ScatterWinningState))
                {
                    ReprocessEGMStatus_OutputByFrontendState(FrontEndPlayStatus.ScatterWinningState);

                }
                SASCTL.GetInstance().RejectTransfer(true, true);

            }
            else if (CheckConditionForFrontendState(FrontEndPlayStatus.WaitingForCredits))
            {
                if (EGMStatus.GetInstance().currentAmount > 0)
                {
                    Game_DecreaseSelectedCreditValue();
                }
                if (EGMStatus.GetInstance().frontend_play.Transition(FrontEndPlayStatus.WaitingForCredits))
                {
                    ReprocessEGMStatus_OutputByFrontendState(FrontEndPlayStatus.WaitingForCredits);

                    billAcc.EnableBillAcceptor();
                }
                SASCTL.GetInstance().AcceptTransfer(true, true);

            }
            else if (CheckConditionForFrontendState(FrontEndPlayStatus.WaitingForBet))
            {

                if (EGMStatus.GetInstance().frontend_play.Transition(FrontEndPlayStatus.WaitingForBet))
                {
                    ReprocessEGMStatus_OutputByFrontendState(FrontEndPlayStatus.WaitingForBet);
                    SASCTL.GetInstance().AcceptTransfer(true, true);

                    billAcc.EnableBillAcceptor();
                }
                SASCTL.GetInstance().AcceptTransfer(true, true);

            }
            else if (CheckConditionForFrontendState(FrontEndPlayStatus.Playing))
            {
                if (EGMStatus.GetInstance().frontend_play.Transition(FrontEndPlayStatus.Playing))
                {
                    EGMStatus.GetInstance().cashoutwithrestricted = false;

                    SASCTL.GetInstance().LaunchExceptionByEvent(SASEvent.Playing);

                    ReprocessEGMStatus_OutputByFrontendState(FrontEndPlayStatus.Playing);


                    billAcc.DisableBillAcceptor();
                }
                SASCTL.GetInstance().RejectTransfer(true, true);

            }
            else if (CheckConditionForFrontendState(FrontEndPlayStatus.DisabledByHost))
            {
                if (EGMStatus.GetInstance().frontend_play.Transition(FrontEndPlayStatus.DisabledByHost))
                {
                    billAcc.DisableBillAcceptor();

                }
                SASCTL.GetInstance().RejectTransfer(true, true);

            }
            else if (CheckConditionForFrontendState(FrontEndPlayStatus.Playable))
            {
                if (EGMStatus.GetInstance().frontend_play.Transition(FrontEndPlayStatus.Playable))
                {
                    if (EGMStatus.GetInstance().autoSpin != EGMStatus.GetInstance().userautoSpin)
                    {
                        EGMStatus.GetInstance().autoSpin = EGMStatus.GetInstance().userautoSpin;
                        if (EGMStatus.GetInstance().autoSpin)
                        {
                            UIPriorityEvent(this, EventType.AutoSpinToggled, new EventArgs());
                        }
                    }

                    EGMStatus.GetInstance().current_play.ResetPlay();
                    EGMDataPersisterCTL.GetInstance().PersistEGMSlotPlay();

                    ReprocessEGMStatus_OutputByFrontendState(FrontEndPlayStatus.Playable);
                    if (!(EGMStatus.GetInstance().autoSpin))
                            billAcc.EnableBillAcceptor();

                }
                SASCTL.GetInstance().AcceptTransfer(true, true);

            }

            // DisabledByHost
            // Playing
            // ActionGamePlayable
            // ActionGamePlaying
        }

        private void ReprocessEGMStatus_OutputByPennyFrontendState(FrontEndPlayPennyStatus state)
        {
            if (state == FrontEndPlayPennyStatus.Playable)
            {

                // AUTOSPIN; SPIN;
                ReprocessEGMStatus_Output(() =>
                {
                    EGMStatus.GetInstance().o_spinbuttonlightstatus = true;
                    EGMStatus.GetInstance().o_helpbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_linesbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_maxbetbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_servicebuttonlightstatus = false;
                    EGMStatus.GetInstance().o_cashoutbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_betbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_autospinbuttonlightstatus = true;

                });
            }

            else if (state == FrontEndPlayPennyStatus.Playing ||
                     state == FrontEndPlayPennyStatus.ScatterWinningState ||
                     state == FrontEndPlayPennyStatus.ActionGamePlayable ||
                     state == FrontEndPlayPennyStatus.WinningState)
            {
                ReprocessEGMStatus_Output(() =>
                {
                    // SPIN
                    EGMStatus.GetInstance().o_spinbuttonlightstatus = true;
                    EGMStatus.GetInstance().o_helpbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_linesbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_maxbetbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_servicebuttonlightstatus = false;
                    EGMStatus.GetInstance().o_cashoutbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_betbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_autospinbuttonlightstatus = false;

                });
            }
            else if (state == FrontEndPlayPennyStatus.ActionGamePlaying ||
                     state == FrontEndPlayPennyStatus.ActionGameWinning)
            {
                ReprocessEGMStatus_Output(() =>
                {
                    EGMStatus.GetInstance().o_spinbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_helpbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_linesbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_maxbetbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_servicebuttonlightstatus = false;
                    EGMStatus.GetInstance().o_cashoutbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_betbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_autospinbuttonlightstatus = false;

                });
            }

        }


        private void ReprocessEGMStatus_OutputByFrontendState(FrontEndPlayStatus state)
        {
            if (state == FrontEndPlayStatus.WaitingForCredits)
            {
                ReprocessEGMStatus_Output(() =>
                {
                    //  AUTOSPIN; HELP;
                    EGMStatus.GetInstance().o_spinbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_helpbuttonlightstatus = true;
                    EGMStatus.GetInstance().o_linesbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_maxbetbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_servicebuttonlightstatus = false;
                    EGMStatus.GetInstance().o_cashoutbuttonlightstatus = (EGMStatus.GetInstance().currentCashableAmount + EGMStatus.GetInstance().currentNonRestrictedAmount) > 0;
                    EGMStatus.GetInstance().o_betbuttonlightstatus = EGMStatus.GetInstance().currentAmount > 0;
                    EGMStatus.GetInstance().o_autospinbuttonlightstatus = true;

                });
            }
            else if (state == FrontEndPlayStatus.WaitingForBet)
            {
                // AUTOSPIN; CASHOUT; INFO-HELP; BET; MAXBET
                ReprocessEGMStatus_Output(() =>
                {
                    EGMStatus.GetInstance().o_spinbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_helpbuttonlightstatus = true;
                    EGMStatus.GetInstance().o_linesbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_maxbetbuttonlightstatus = true;
                    EGMStatus.GetInstance().o_servicebuttonlightstatus = false;
                    EGMStatus.GetInstance().o_cashoutbuttonlightstatus = true;
                    EGMStatus.GetInstance().o_betbuttonlightstatus = true;
                    EGMStatus.GetInstance().o_autospinbuttonlightstatus = true;

                });
            }
            else if (state == FrontEndPlayStatus.Playable)
            {
                // AUTOSPIN; SPIN; CASHOUT, INFO-HELP; LINES; SERVICE; BET; MAXBET
                ReprocessEGMStatus_Output(() =>
                {
                    EGMStatus.GetInstance().o_spinbuttonlightstatus = true;
                    EGMStatus.GetInstance().o_helpbuttonlightstatus = true;
                    EGMStatus.GetInstance().o_linesbuttonlightstatus = true;
                    EGMStatus.GetInstance().o_maxbetbuttonlightstatus = true;
                    EGMStatus.GetInstance().o_servicebuttonlightstatus = true;
                    EGMStatus.GetInstance().o_cashoutbuttonlightstatus = true;
                    EGMStatus.GetInstance().o_betbuttonlightstatus = true;
                    EGMStatus.GetInstance().o_autospinbuttonlightstatus = true;

                });
            }

            else if (state == FrontEndPlayStatus.Playing ||
                     state == FrontEndPlayStatus.ScatterWinningState ||
                     state == FrontEndPlayStatus.ActionGamePlayable ||
                     state == FrontEndPlayStatus.WinningState)
            {
                ReprocessEGMStatus_Output(() =>
                {
                    // SPIN
                    EGMStatus.GetInstance().o_spinbuttonlightstatus = true;
                    EGMStatus.GetInstance().o_helpbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_linesbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_maxbetbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_servicebuttonlightstatus = false;
                    EGMStatus.GetInstance().o_cashoutbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_betbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_autospinbuttonlightstatus = false;

                });
            }
            else if (state == FrontEndPlayStatus.Jackpot ||
                     state == FrontEndPlayStatus.Handpay ||
                     state == FrontEndPlayStatus.Tilt ||
                     state == FrontEndPlayStatus.CriticalTilt ||
                     state == FrontEndPlayStatus.MaintenanceMode ||
                     state == FrontEndPlayStatus.Menu ||
                     state == FrontEndPlayStatus.DisabledByHost ||
                     state == FrontEndPlayStatus.MisteryWinningState ||
                     state == FrontEndPlayStatus.ActionGamePlaying ||
                     state == FrontEndPlayStatus.ActionGameWinning ||
                     state == FrontEndPlayStatus.UILock)
            {
                ReprocessEGMStatus_Output(() =>
                {
                    EGMStatus.GetInstance().o_spinbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_helpbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_linesbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_maxbetbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_servicebuttonlightstatus = false;
                    EGMStatus.GetInstance().o_cashoutbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_betbuttonlightstatus = false;
                    EGMStatus.GetInstance().o_autospinbuttonlightstatus = false;

                });
            }

        }

        private void ReprocessEGMStatus_Output(Action act)
        {
            act();
            SetOutput(OutputName.O_TOWER2LIGHT, EGMStatus.GetInstance().o_tower2lightStatus);
            SetOutput(OutputName.O_AUTOSPINBUTTONLIGHT, EGMStatus.GetInstance().o_autospinbuttonlightstatus);
            SetOutput(OutputName.O_BETBUTTONLIGHT, EGMStatus.GetInstance().o_betbuttonlightstatus);
            SetOutput(OutputName.O_CASHOUTBUTTONLIGHT, EGMStatus.GetInstance().o_cashoutbuttonlightstatus);
            SetOutput(OutputName.O_HELPBUTTONLIGHT, EGMStatus.GetInstance().o_helpbuttonlightstatus);
            SetOutput(OutputName.O_LINESBUTTONLIGHT, EGMStatus.GetInstance().o_linesbuttonlightstatus);
            SetOutput(OutputName.O_MAXBETBUTTONLIGHT, EGMStatus.GetInstance().o_maxbetbuttonlightstatus);
            SetOutput(OutputName.O_SERVICEBUTTONLIGHT, EGMStatus.GetInstance().o_servicebuttonlightstatus);
            SetOutput(OutputName.O_SPINBUTTONLIGHT, EGMStatus.GetInstance().o_spinbuttonlightstatus);
            SetOutput(OutputName.O_TOWER1LIGHT, EGMStatus.GetInstance().o_tower1lightStatus);
        }


        private void UpdateMetersByEvent(Meters_EventName eventname, int[] value)
        {
            switch (eventname)
            {
                case Meters_EventName.BillJam:
                    {
                        EGMIncrementMeter("BillsJammed", 1);

                        break;
                    }
                case Meters_EventName.StackerOpen:
                    {
                        EGMIncrementMeter("BasicStackerOpen", 1);

                        break;
                    }
                case Meters_EventName.StackerClosed:
                    {
                        EGMIncrementMeter("BasicStackerClose", 1);

                        break;
                    }
                case Meters_EventName.SASInterfaceError:
                    {
                        EGMIncrementMeter("SASInterfaceError", 1);

                        break;
                    }
                case Meters_EventName.EGMInitialization:
                    {
                        // Increment PowerReset
                        EGMIncrementMeter("BasicPowerReset", 1);
                        // Update GamesSincePowerUp
                        EGMUpdateMeter((byte)SASMeter.GamesSinceLastPowerUp, 0);

                        break;
                    }
                case Meters_EventName.CashoutPressed:
                    {
                        // Validation
                        if (value.Length <= 0) { break; }
                        //This includes, at a minimum, all credits in the Total Hand Paid Cancelled Credits meter
                        EGMIncrementMeter((byte)SASMeter.TotalCancelledCredits, value[0], true);

                        break;
                    }
                case Meters_EventName.HandpayReset:
                    {
                        // Validation
                        if (value.Length <= 0) { break; }

                        EGMIncrementMeter((byte)SASMeter.TotalHandpayCancelledCredits, value[0], true);
                        // The cumulative sum of all credits paid by an attendant
                        EGMIncrementMeter((byte)SASMeter.TotalHandpayCredits, value[0], true);
                        break;
                    }
                case Meters_EventName.PlayStarted:
                    {
                        // Validation
                        if (value.Length <= 0) { break; }

                        EGMIncrementMeter((byte)SASMeter.TotalCoinIn, value[0], true);
                        break;
                    }
                case Meters_EventName.PlayWon:
                    {
                        // Validation
                        if (value.Length <= 0) { break; }

                        EGMIncrementMeter((byte)SASMeter.GamesWon, 1, true); // Total Won Games
                        EGMIncrementMeter("BasicGamesWon", 1); // Total Won Games
                                                               // EGMIncrementMeter((byte)SASMeter.GamesWon, 1, true); // Total Won Games
                        EGMIncrementMeter((byte)SASMeter.TotalCoinOut, value[0], true);
                        EGMIncrementMeter((byte)SASMeter.TotalMachinePaidPaytableWin, value[0], true);
                        EGMIncrementMeter((byte)SASMeter.TotalWonCredits, value[0], true);

                        break;
                    }
                case Meters_EventName.PlayLost:
                    {
                        EGMIncrementMeter("BasicGamesWon", 0); // Total Won Games
                        EGMIncrementMeter((byte)SASMeter.GamesLost, 1, true); // Total Lost Games
                        //EGMIncrementMeter((byte)SASMeter.GamesLost, 1, true); // Total Won Games
                        break;
                    }
                case Meters_EventName.PlayFinished:
                    {
                        EGMIncrementMeter("BasicGamesPlayed", 1); // Total Won Games
                        EGMIncrementMeter((byte)SASMeter.TotalGames, 1, true); // Total Games
                        //EGMIncrementMeter((byte)SASMeter.TotalGames, 1, true); // Total Won Games
                        EGMIncrementMeter((byte)SASMeter.GamesSinceDoorClosure, 1, true); // Games Since Door Closure
                        EGMIncrementMeter((byte)SASMeter.GamesSinceLastPowerUp, 1, true); // Games Since Last Power Up

                        break;
                    }
                case Meters_EventName.SlotDoorOpen:
                    {
                        // Increment the respective meter
                        EGMIncrementMeter("BasicSlotDoorOpen", 1);
                        // Update the Games since Door Closed
                        EGMUpdateMeter((byte)SASMeter.GamesSinceDoorClosure, 0);
                        break;
                    }
                case Meters_EventName.SlotDoorClosed:
                    {
                        // Increment the respective meter
                        EGMIncrementMeter("BasicSlotDoorClose", 1);

                        break;
                    }
                case Meters_EventName.LogicDoorOpen:
                    {
                        // Increment the respective meter
                        EGMIncrementMeter("BasicLogicDoorOpen", 1);
                        break;
                    }
                case Meters_EventName.LogicDoorClosed:
                    {
                        // Increment the respective meter
                        EGMIncrementMeter("BasicLogicDoorClose", 1);
                        break;
                    }
                case Meters_EventName.CardCageDoorOpen:
                    {

                        break;
                    }
                case Meters_EventName.CardCageDoorClosed:
                    {

                        break;
                    }
                case Meters_EventName.BellyDoorOpen:
                    {

                        break;
                    }
                case Meters_EventName.BellyDoorClosed:
                    {

                        break;
                    }
                case Meters_EventName.CashboxDoorOpen:
                    {
                        EGMIncrementMeter("BasicCashboxDoorOpen", 1);

                        break;
                    }
                case Meters_EventName.CashboxDoorClosed:
                    {
                        EGMIncrementMeter("BasicCashboxDoorClose", 1);

                        break;
                    }
                case Meters_EventName.DropDoorOpen:
                    {
                        EGMIncrementMeter("BasicDropDoorOpen", 1);

                        break;
                    }
                case Meters_EventName.DropDoorClosed:
                    {
                        EGMIncrementMeter("BasicDropDoorClose", 1);

                        break;
                    }
                case Meters_EventName.BillInserted:
                    {
                        // Validation
                        if (value.Length <= 0) { break; }

                        // Update SAS Meter
                        int bill = value[0];
                        EGMIncrementMeter((byte)SASMeter.TotalCreditsFromBillAccepted, ToSASFormat((decimal)bill), true); // Total credits from bills accepted
                        EGMIncrementMeter((byte)SASMeter.TotalDropCredits, ToSASFormat((decimal)bill), true); // Total credits from bills accepted

                        switch (bill)
                        {
                            case 1:
                                EGMIncrementMeter((byte)SASMeter.Total1BillsAccepted, 1, true);
                                EGMIncrementMeter((byte)SASMeter.NumberOfBillsCurrentlyInTheStacker, 1, true);
                                EGMIncrementMeter((byte)SASMeter.TotalValueOfBillsCurrentlyInTheStacker, ToSASFormat((decimal)bill), true);
                                break;
                            case 2:
                                EGMIncrementMeter((byte)SASMeter.Total2BillsAccepted, 1, true);
                                EGMIncrementMeter((byte)SASMeter.NumberOfBillsCurrentlyInTheStacker, 1, true);
                                EGMIncrementMeter((byte)SASMeter.TotalValueOfBillsCurrentlyInTheStacker, ToSASFormat((decimal)bill), true);
                                break;
                            case 5:
                                EGMIncrementMeter((byte)SASMeter.Total5BillsAccepted, 1, true);
                                EGMIncrementMeter((byte)SASMeter.NumberOfBillsCurrentlyInTheStacker, 1, true);
                                EGMIncrementMeter((byte)SASMeter.TotalValueOfBillsCurrentlyInTheStacker, ToSASFormat((decimal)bill), true);
                                break;
                            case 10:
                                EGMIncrementMeter((byte)SASMeter.Total10BillsAccepted, 1, true);
                                EGMIncrementMeter((byte)SASMeter.NumberOfBillsCurrentlyInTheStacker, 1, true);
                                EGMIncrementMeter((byte)SASMeter.TotalValueOfBillsCurrentlyInTheStacker, ToSASFormat((decimal)bill), true);
                                break;
                            case 20:
                                EGMIncrementMeter((byte)SASMeter.Total20BillsAccepted, 1, true);
                                EGMIncrementMeter((byte)SASMeter.NumberOfBillsCurrentlyInTheStacker, 1, true);
                                EGMIncrementMeter((byte)SASMeter.TotalValueOfBillsCurrentlyInTheStacker, ToSASFormat((decimal)bill), true);
                                break;
                            case 25:
                                EGMIncrementMeter((byte)SASMeter.Total25BillsAccepted, 1, true);
                                EGMIncrementMeter((byte)SASMeter.NumberOfBillsCurrentlyInTheStacker, 1, true);
                                EGMIncrementMeter((byte)SASMeter.TotalValueOfBillsCurrentlyInTheStacker, ToSASFormat((decimal)bill), true);
                                break;
                            case 50:
                                EGMIncrementMeter((byte)SASMeter.Total50BillsAccepted, 1, true);
                                EGMIncrementMeter((byte)SASMeter.NumberOfBillsCurrentlyInTheStacker, 1, true);
                                EGMIncrementMeter((byte)SASMeter.TotalValueOfBillsCurrentlyInTheStacker, ToSASFormat((decimal)bill), true);
                                break;
                            case 100:
                                EGMIncrementMeter((byte)SASMeter.Total100BillsAccepted, 1, true);
                                EGMIncrementMeter((byte)SASMeter.NumberOfBillsCurrentlyInTheStacker, 1, true);
                                EGMIncrementMeter((byte)SASMeter.TotalValueOfBillsCurrentlyInTheStacker, ToSASFormat((decimal)bill), true);
                                break;
                            case 200:
                                EGMIncrementMeter((byte)SASMeter.Total200BillsAccepted, 1, true);
                                EGMIncrementMeter((byte)SASMeter.NumberOfBillsCurrentlyInTheStacker, 1, true);
                                EGMIncrementMeter((byte)SASMeter.TotalValueOfBillsCurrentlyInTheStacker, ToSASFormat((decimal)bill), true);
                                break;
                            case 250:
                                EGMIncrementMeter((byte)SASMeter.Total250BillsAccepted, 1, true);
                                EGMIncrementMeter((byte)SASMeter.NumberOfBillsCurrentlyInTheStacker, 1, true);
                                EGMIncrementMeter((byte)SASMeter.TotalValueOfBillsCurrentlyInTheStacker, ToSASFormat((decimal)bill), true);
                                break;
                            case 500:
                                EGMIncrementMeter((byte)SASMeter.Total500BillsAccepted, 1, true);
                                EGMIncrementMeter((byte)SASMeter.NumberOfBillsCurrentlyInTheStacker, 1, true);
                                EGMIncrementMeter((byte)SASMeter.TotalValueOfBillsCurrentlyInTheStacker, ToSASFormat((decimal)bill), true);
                                break;
                            case 1000:
                                EGMIncrementMeter((byte)SASMeter.Total1000BillsAccepted, 1, true);
                                EGMIncrementMeter((byte)SASMeter.NumberOfBillsCurrentlyInTheStacker, 1, true);
                                EGMIncrementMeter((byte)SASMeter.TotalValueOfBillsCurrentlyInTheStacker, ToSASFormat((decimal)bill), true);
                                break;
                            case 2000:
                                EGMIncrementMeter((byte)SASMeter.Total2000BillsAccepted, 1, true);
                                EGMIncrementMeter((byte)SASMeter.NumberOfBillsCurrentlyInTheStacker, 1, true);
                                EGMIncrementMeter((byte)SASMeter.TotalValueOfBillsCurrentlyInTheStacker, ToSASFormat((decimal)bill), true);
                                break;
                            case 2500:
                                EGMIncrementMeter((byte)SASMeter.Total2500BillsAccepted, 1, true);
                                EGMIncrementMeter((byte)SASMeter.NumberOfBillsCurrentlyInTheStacker, 1, true);
                                EGMIncrementMeter((byte)SASMeter.TotalValueOfBillsCurrentlyInTheStacker, ToSASFormat((decimal)bill), true);
                                break;
                            case 5000:
                                EGMIncrementMeter((byte)SASMeter.Total5000BillsAccepted, 1, true);
                                EGMIncrementMeter((byte)SASMeter.NumberOfBillsCurrentlyInTheStacker, 1, true);
                                EGMIncrementMeter((byte)SASMeter.TotalValueOfBillsCurrentlyInTheStacker, ToSASFormat((decimal)bill), true);
                                break;
                            case 10000:
                                EGMIncrementMeter((byte)SASMeter.Total10000BillsAccepted, 1, true);
                                EGMIncrementMeter((byte)SASMeter.NumberOfBillsCurrentlyInTheStacker, 1, true);
                                EGMIncrementMeter((byte)SASMeter.TotalValueOfBillsCurrentlyInTheStacker, ToSASFormat((decimal)bill), true);
                                break;
                            case 20000:
                                EGMIncrementMeter((byte)SASMeter.Total20000BillsAccepted, 1, true);
                                EGMIncrementMeter((byte)SASMeter.NumberOfBillsCurrentlyInTheStacker, 1, true);
                                EGMIncrementMeter((byte)SASMeter.TotalValueOfBillsCurrentlyInTheStacker, ToSASFormat((decimal)bill), true);
                                break;
                            case 25000:
                                EGMIncrementMeter((byte)SASMeter.Total25000BillsAccepted, 1, true);
                                EGMIncrementMeter((byte)SASMeter.NumberOfBillsCurrentlyInTheStacker, 1, true);
                                EGMIncrementMeter((byte)SASMeter.TotalValueOfBillsCurrentlyInTheStacker, ToSASFormat((decimal)bill), true);
                                break;
                            case 50000:
                                EGMIncrementMeter((byte)SASMeter.Total50000BillsAccepted, 1, true);
                                EGMIncrementMeter((byte)SASMeter.NumberOfBillsCurrentlyInTheStacker, 1, true);
                                EGMIncrementMeter((byte)SASMeter.TotalValueOfBillsCurrentlyInTheStacker, ToSASFormat((decimal)bill), true);
                                break;
                            case 100000:
                                EGMIncrementMeter((byte)SASMeter.Total100000BillsAccepted, 1, true);
                                EGMIncrementMeter((byte)SASMeter.NumberOfBillsCurrentlyInTheStacker, 1, true);
                                EGMIncrementMeter((byte)SASMeter.TotalValueOfBillsCurrentlyInTheStacker, ToSASFormat((decimal)bill), true);
                                break;
                            case 200000:
                                EGMIncrementMeter((byte)SASMeter.Total200000BillsAccepted, 1, true);
                                EGMIncrementMeter((byte)SASMeter.NumberOfBillsCurrentlyInTheStacker, 1, true);
                                EGMIncrementMeter((byte)SASMeter.TotalValueOfBillsCurrentlyInTheStacker, ToSASFormat((decimal)bill), true);
                                break;
                            case 250000:
                                EGMIncrementMeter((byte)SASMeter.Total250000BillsAccepted, 1, true);
                                EGMIncrementMeter((byte)SASMeter.NumberOfBillsCurrentlyInTheStacker, 1, true);
                                EGMIncrementMeter((byte)SASMeter.TotalValueOfBillsCurrentlyInTheStacker, ToSASFormat((decimal)bill), true);
                                break;
                            case 500000:
                                EGMIncrementMeter((byte)SASMeter.Total500000BillsAccepted, 1, true);
                                EGMIncrementMeter((byte)SASMeter.NumberOfBillsCurrentlyInTheStacker, 1, true);
                                EGMIncrementMeter((byte)SASMeter.TotalValueOfBillsCurrentlyInTheStacker, ToSASFormat((decimal)bill), true);
                                break;
                            case 1000000:
                                EGMIncrementMeter((byte)SASMeter.Total1000000BillsAccepted, 1, true);
                                EGMIncrementMeter((byte)SASMeter.NumberOfBillsCurrentlyInTheStacker, 1, true);
                                EGMIncrementMeter((byte)SASMeter.TotalValueOfBillsCurrentlyInTheStacker, ToSASFormat((decimal)bill), true);
                                break;
                            default:
                                break;
                        }
                        break;
                    }
                case Meters_EventName.PartialRamClear:
                    {


                        break;
                    }
                case Meters_EventName.CreditsModified:
                    {
                        // Update SAS Meters
                        EGMUpdateMeter((byte)SASMeter.CurrentRestrictedCredits, ToSASFormat(EGMStatus.GetInstance().currentRestrictedAmount)); // Current Restricted Credits
                        EGMUpdateMeter((byte)SASMeter.TotalCredits, ToSASFormat(EGMStatus.GetInstance().currentAmount)); // Current Credits
                        break;
                    }
                case Meters_EventName.JackpotOcurred:
                    {
                        // Validation
                        if (value.Length <= 0) { break; }
                        int jackpotamount = value[0];
                        // Update Jackpot Meter
                        EGMIncrementMeter((byte)SASMeter.TotalJackpotMeter, ToSASFormat(jackpotamount), true);
                        EGMIncrementMeter((byte)SASMeter.TotalWonCredits, ToSASFormat(jackpotamount), true);

                        break;
                    }
                case Meters_EventName.Tilt:
                    {
                        EGMIncrementMeter("TotalTilts", 1);

                        break;
                    }
                case Meters_EventName.AFTTransferCompleted:
                    {
                        // Validation
                        if (value.Length <= 0) { break; }

                        // Update SAS Meter
                        int mvalue = value[0];
                        EGMIncrementMeter((byte)SASMeter.TotalDropCredits, mvalue, false); // Total credits from bills accepted
                        break;
                    }
                default:
                    break;
            }
        }

        private void UpdateMetersByEvent(Meters_EventName eventname)
        {
            UpdateMetersByEvent(eventname, new int[] { });
        }

        private void UpdateSASGameDetails()
        {
            // Set SAS Game Details
            SASCTL.GetInstance().SetSASGameDetails(EGMSettings.GetInstance().sasSettings.vltConfiguration.GameID,
                                                   EGMSettings.GetInstance().sasSettings.vltConfiguration.AdditionalID,
                                                   "GameName",
                                                   "GOLDRUSH",
                                                   "GOLDRUSH",
                                                   500,
                                                   /*GameOption*/ 192, /* TBD */
                                                   /*PaybackPerc*/ 0920, /* TBD */
                                                   /*Cashout Limit*/ 0, /* TBD*/
                                                   /*Wager Cat Num*/ 8642 /* TBD */);
        }



        #endregion


    }
}

