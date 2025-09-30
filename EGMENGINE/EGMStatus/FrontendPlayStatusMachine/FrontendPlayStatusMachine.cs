using System;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using EGMENGINE.EGMStatusModule.HandPayModule;

namespace EGMENGINE.GUI.GAMETYPES
{

    // La enumeracin FrontEndPlayStatus: Los estados de una transaccin actual
    public enum FrontEndPlayStatus
    {
        Starting,
        WaitingForCredits,
        WaitingForBet,
        Playable,
        Playing,
        WinningState,
        ActionGamePlayable,
        ActionGamePlaying,
        ActionGameWinning,
        ScatterWinningState,
        MisteryWinningState,
        PennyGamesBonus,
        Jackpot,
        Handpay,
        UILock,
        Menu,
        Tilt,
        CriticalTilt,
        MaintenanceMode,
        ShowingHelp,
        PostFRCInitialization,
        DisabledByHost
    }



    internal class FrontEndPlay
    {
        private static ValidTransition<T> CreateTransition<T>(T from, T[] to)
        {
            return new ValidTransition<T>
            { /* FROM */
                status = from,
                /* TO */
                next_status = to
            };
        }

        internal delegate void TransactionExecutedEvent(FrontEndPlayStatus s, bool persist, EventArgs e);
        internal TransactionExecutedEvent TransitionExecuted;

        // Se modela la máquina de estados con todas sus transiciones
        // The state machine is modeled with all its transitions.
        private ValidTransition<FrontEndPlayStatus>[] state_machine = new ValidTransition<FrontEndPlayStatus>[] {
            CreateTransition(FrontEndPlayStatus.Starting,
                            (new FrontEndPlayStatus[] { FrontEndPlayStatus.DisabledByHost, FrontEndPlayStatus.PostFRCInitialization, FrontEndPlayStatus.Handpay, FrontEndPlayStatus.Jackpot, FrontEndPlayStatus.WaitingForCredits, FrontEndPlayStatus.WaitingForBet, FrontEndPlayStatus.Playable, FrontEndPlayStatus.WinningState, FrontEndPlayStatus.ScatterWinningState, FrontEndPlayStatus.PennyGamesBonus, FrontEndPlayStatus.Playing, FrontEndPlayStatus.Menu,  FrontEndPlayStatus.Tilt, FrontEndPlayStatus.MaintenanceMode})),
            CreateTransition(FrontEndPlayStatus.UILock,
                            (new FrontEndPlayStatus[] { FrontEndPlayStatus.PostFRCInitialization, FrontEndPlayStatus.DisabledByHost, FrontEndPlayStatus.Handpay, FrontEndPlayStatus.WaitingForCredits, FrontEndPlayStatus.WaitingForBet, FrontEndPlayStatus.Playable  })),
            CreateTransition(FrontEndPlayStatus.WaitingForCredits,
                            (new FrontEndPlayStatus[] { FrontEndPlayStatus.PostFRCInitialization, FrontEndPlayStatus.Handpay, FrontEndPlayStatus.DisabledByHost, FrontEndPlayStatus.UILock, FrontEndPlayStatus.Menu, FrontEndPlayStatus.ShowingHelp,  FrontEndPlayStatus.Tilt, FrontEndPlayStatus.MaintenanceMode,   FrontEndPlayStatus.WaitingForBet,  FrontEndPlayStatus.Playable,  })),
            CreateTransition(FrontEndPlayStatus.WaitingForBet,
                            (new FrontEndPlayStatus[] { FrontEndPlayStatus.PostFRCInitialization, FrontEndPlayStatus.DisabledByHost, FrontEndPlayStatus.UILock, FrontEndPlayStatus.Handpay, FrontEndPlayStatus.Menu, FrontEndPlayStatus.ShowingHelp, FrontEndPlayStatus.Tilt, FrontEndPlayStatus.MaintenanceMode, FrontEndPlayStatus.Playable, FrontEndPlayStatus.WaitingForCredits })),
            CreateTransition(FrontEndPlayStatus.Playable,
                            (new FrontEndPlayStatus[] { FrontEndPlayStatus.PostFRCInitialization, FrontEndPlayStatus.DisabledByHost, FrontEndPlayStatus.UILock, FrontEndPlayStatus.Handpay, FrontEndPlayStatus.Menu, FrontEndPlayStatus.ShowingHelp, FrontEndPlayStatus.Tilt, FrontEndPlayStatus.MaintenanceMode, FrontEndPlayStatus.Playing,  FrontEndPlayStatus.WaitingForCredits,  FrontEndPlayStatus.WaitingForBet})),
            CreateTransition(FrontEndPlayStatus.Playing,
                            (new FrontEndPlayStatus[] { FrontEndPlayStatus.WaitingForBet, FrontEndPlayStatus.WaitingForCredits, FrontEndPlayStatus.Playable, FrontEndPlayStatus.MisteryWinningState, FrontEndPlayStatus.DisabledByHost, FrontEndPlayStatus.Tilt, FrontEndPlayStatus.MaintenanceMode, FrontEndPlayStatus.WinningState, FrontEndPlayStatus.ScatterWinningState, FrontEndPlayStatus.Jackpot, FrontEndPlayStatus.Handpay })),
            CreateTransition(FrontEndPlayStatus.WinningState,
                            (new FrontEndPlayStatus[] { FrontEndPlayStatus.DisabledByHost, FrontEndPlayStatus.Tilt, FrontEndPlayStatus.ActionGamePlayable, FrontEndPlayStatus.Playable, FrontEndPlayStatus.ScatterWinningState, FrontEndPlayStatus.MaintenanceMode, })),
            CreateTransition(FrontEndPlayStatus.ActionGamePlayable,
                            (new FrontEndPlayStatus[] { FrontEndPlayStatus.ActionGamePlaying, FrontEndPlayStatus.Tilt })),
            CreateTransition(FrontEndPlayStatus.ActionGamePlaying,
                            (new FrontEndPlayStatus[] { FrontEndPlayStatus.ActionGameWinning, FrontEndPlayStatus.Tilt, FrontEndPlayStatus.Playable, FrontEndPlayStatus.ActionGamePlayable, FrontEndPlayStatus.ScatterWinningState })),
            CreateTransition(FrontEndPlayStatus.ActionGameWinning,
                            (new FrontEndPlayStatus[] {  FrontEndPlayStatus.Tilt, FrontEndPlayStatus.Playable, FrontEndPlayStatus.ActionGamePlayable, FrontEndPlayStatus.ScatterWinningState })),
            CreateTransition(FrontEndPlayStatus.ScatterWinningState,
                            (new FrontEndPlayStatus[] { FrontEndPlayStatus.PennyGamesBonus})),
            CreateTransition(FrontEndPlayStatus.MisteryWinningState,
                            (new FrontEndPlayStatus[] { FrontEndPlayStatus.Playable})),
            CreateTransition(FrontEndPlayStatus.PennyGamesBonus,
                            (new FrontEndPlayStatus[] { FrontEndPlayStatus.Playable, FrontEndPlayStatus.Tilt, FrontEndPlayStatus.MaintenanceMode, FrontEndPlayStatus.Handpay })),
            CreateTransition(FrontEndPlayStatus.Jackpot,
                            (new FrontEndPlayStatus[] { FrontEndPlayStatus.Playable, FrontEndPlayStatus.Tilt, FrontEndPlayStatus.MaintenanceMode,FrontEndPlayStatus.Handpay })),
            CreateTransition(FrontEndPlayStatus.Handpay,
                            (new FrontEndPlayStatus[] { FrontEndPlayStatus.PennyGamesBonus, FrontEndPlayStatus.DisabledByHost, FrontEndPlayStatus.Playable, FrontEndPlayStatus.Tilt, FrontEndPlayStatus.MaintenanceMode, FrontEndPlayStatus.WaitingForCredits,  FrontEndPlayStatus.WaitingForBet})),
            CreateTransition(FrontEndPlayStatus.ShowingHelp,
                            (new FrontEndPlayStatus[] { FrontEndPlayStatus.Tilt, FrontEndPlayStatus.MaintenanceMode, FrontEndPlayStatus.WaitingForCredits, FrontEndPlayStatus.WaitingForBet, FrontEndPlayStatus.Playable})),
            CreateTransition(FrontEndPlayStatus.Tilt,
                            (new FrontEndPlayStatus[] { FrontEndPlayStatus.Menu, FrontEndPlayStatus.ScatterWinningState, FrontEndPlayStatus.DisabledByHost, FrontEndPlayStatus.CriticalTilt, FrontEndPlayStatus.ActionGameWinning, FrontEndPlayStatus.ActionGamePlayable, FrontEndPlayStatus.ShowingHelp, FrontEndPlayStatus.PennyGamesBonus, FrontEndPlayStatus.Playable, FrontEndPlayStatus.Playing, FrontEndPlayStatus.WinningState, FrontEndPlayStatus.WaitingForBet, FrontEndPlayStatus.MaintenanceMode, FrontEndPlayStatus.WaitingForCredits})),
            CreateTransition(FrontEndPlayStatus.Menu,
                            (new FrontEndPlayStatus[] { FrontEndPlayStatus.PostFRCInitialization, FrontEndPlayStatus.PennyGamesBonus,  FrontEndPlayStatus.ScatterWinningState, FrontEndPlayStatus.WinningState, FrontEndPlayStatus.DisabledByHost, FrontEndPlayStatus.Tilt, FrontEndPlayStatus.WaitingForCredits, FrontEndPlayStatus.WaitingForBet, FrontEndPlayStatus.Playable, FrontEndPlayStatus.Playing, FrontEndPlayStatus.MaintenanceMode})),
            CreateTransition(FrontEndPlayStatus.MaintenanceMode,
                            (new FrontEndPlayStatus[] { FrontEndPlayStatus.DisabledByHost, FrontEndPlayStatus.Menu, FrontEndPlayStatus.Playable, FrontEndPlayStatus.WaitingForBet, FrontEndPlayStatus.WaitingForCredits, FrontEndPlayStatus.Playing,FrontEndPlayStatus.Handpay, FrontEndPlayStatus.Jackpot, FrontEndPlayStatus.WinningState, FrontEndPlayStatus.Tilt,  FrontEndPlayStatus.ShowingHelp})),
            CreateTransition(FrontEndPlayStatus.PostFRCInitialization,
                            (new FrontEndPlayStatus[] { FrontEndPlayStatus.DisabledByHost, FrontEndPlayStatus.Menu })),
            CreateTransition(FrontEndPlayStatus.DisabledByHost,
                             new FrontEndPlayStatus[] { FrontEndPlayStatus.UILock, FrontEndPlayStatus.Handpay, FrontEndPlayStatus.MaintenanceMode, FrontEndPlayStatus.Menu, FrontEndPlayStatus.Playable, FrontEndPlayStatus.WaitingForCredits, FrontEndPlayStatus.WaitingForBet})
        };

        /* El status de la Transaction */
        /* The status of the Transaction */
        public FrontEndPlayStatus thisstatus;

        public decimal Amount;
        public DateTime TrigerTS;
        public DateTime ResetTS;
        public string ResetMode;

        /* El timestamp de la última transición*/
        /* The timestamp of the last transition*/
        public DateTime LastTransitionTS;
        public FrontEndPlay()
        {
            thisstatus = FrontEndPlayStatus.Starting;
        }

        internal bool CanTransitionFromCurrentState(FrontEndPlayStatus status)
        {
            return state_machine.Where(s => s.status == thisstatus).FirstOrDefault().next_status.Contains(status);
        }
        /// <summary>
        /// UpdateStatus
        /// </summary>
        /// <param name="status_"></param>
        internal void UpdateStatus(FrontEndPlayStatus status_, bool persist_)
        {
            thisstatus = status_;
            TransitionExecuted(thisstatus, persist_, new EventArgs());
        }

        /// <summary>
        /// UpdateStatus
        /// </summary>
        /// <param name="status_"></param>
        internal FrontEndPlayStatus[] GetPossibleNextStatus()
        {
            // Gets the saved status transition, from the GI
            ValidTransition<FrontEndPlayStatus> transition = state_machine.Where(t => t.status == thisstatus).FirstOrDefault();


            return transition.next_status;
        }


        // Función de transición. Retorna true si transicionó bien, retorna false si no pudo transicionar.
        // Transition function. Returns true if it transitioned well, returns false if it failed to transition.
        public bool Transition(FrontEndPlayStatus status_)
        {
            // Obtiene la transición guardada de status, de la SM
            // Gets the saved status transition, from the GI
            ValidTransition<FrontEndPlayStatus> transition = state_machine.Where(t => t.status == thisstatus).FirstOrDefault();
            // Si puede transicionar.. retorna true y el status actual es el status nuevo
            // If it can transition... it returns true and the current status is the new status.
            if (transition.next_status.Contains(status_))
            {
                LastTransitionTS = DateTime.Now;
                thisstatus = status_;
                return true;
            }
            else
            {
                return false;
            }
        }

        /* Determina cuando la state machine está en proceso, en algún estado intermedio */
        /* Determines when the state machine is in process, in some intermediate state. */
        public bool WorkInProgress()
        {
            // Si el estado está en uno de estos estados no iniciales
            // If the state is in one of the following non-initial states
            if (thisstatus != FrontEndPlayStatus.Starting)
            {
                return true;
            }
            else
                return false;
        }

        /* Reseteo el state  */
        /* Reset the state  */
        public void ResetState()
        {
            thisstatus = FrontEndPlayStatus.Starting;
            LastTransitionTS = DateTime.Now;
        }
    }
}