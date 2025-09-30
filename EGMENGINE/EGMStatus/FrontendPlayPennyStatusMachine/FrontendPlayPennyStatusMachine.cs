using System;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace EGMENGINE.GUI.GAMETYPES
{

    // La enumeracin FrontEndPlayStatus: Los estados de una transaccin actual
    public enum FrontEndPlayPennyStatus
    {
        Idle,
        ExpandedSymbolDraw,
        Playable,
        Playing,
        WinningState,
        ActionGamePlayable,
        ActionGamePlaying,
        ActionGameWinning,
        ScatterWinningState,
        ExpandingWinningState,
    }



    internal class FrontEndPlayPenny
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

        // Se modela la máquina de estados con todas sus transiciones
        // The state machine is modeled with all its transitions.
        private ValidTransition<FrontEndPlayPennyStatus>[] state_machine = new ValidTransition<FrontEndPlayPennyStatus>[] {
            CreateTransition(FrontEndPlayPennyStatus.Idle,
                            (new FrontEndPlayPennyStatus[] { FrontEndPlayPennyStatus.ExpandedSymbolDraw})),
            CreateTransition(FrontEndPlayPennyStatus.ExpandedSymbolDraw,
                            (new FrontEndPlayPennyStatus[] { FrontEndPlayPennyStatus.Playable})),
            CreateTransition(FrontEndPlayPennyStatus.Playable,
                            (new FrontEndPlayPennyStatus[] { FrontEndPlayPennyStatus.Idle, FrontEndPlayPennyStatus.Playing })),
            CreateTransition(FrontEndPlayPennyStatus.Playing,
                            (new FrontEndPlayPennyStatus[] {  FrontEndPlayPennyStatus.Idle, FrontEndPlayPennyStatus.Playable, FrontEndPlayPennyStatus.WinningState, FrontEndPlayPennyStatus.ScatterWinningState, FrontEndPlayPennyStatus.ExpandingWinningState })),
            CreateTransition(FrontEndPlayPennyStatus.WinningState,
                            (new FrontEndPlayPennyStatus[] { FrontEndPlayPennyStatus.ActionGamePlayable, FrontEndPlayPennyStatus.Idle, FrontEndPlayPennyStatus.Playable , FrontEndPlayPennyStatus.ScatterWinningState, FrontEndPlayPennyStatus.ExpandingWinningState})),
           CreateTransition(FrontEndPlayPennyStatus.ActionGamePlayable,
                            (new FrontEndPlayPennyStatus[] { FrontEndPlayPennyStatus.ActionGamePlaying })),
            CreateTransition(FrontEndPlayPennyStatus.ActionGamePlaying,
                            (new FrontEndPlayPennyStatus[] { FrontEndPlayPennyStatus.ActionGameWinning, FrontEndPlayPennyStatus.Playable, FrontEndPlayPennyStatus.ActionGamePlayable, FrontEndPlayPennyStatus.ScatterWinningState })),
            CreateTransition(FrontEndPlayPennyStatus.ActionGameWinning,
                            (new FrontEndPlayPennyStatus[] { FrontEndPlayPennyStatus.Playable, FrontEndPlayPennyStatus.ActionGamePlayable, FrontEndPlayPennyStatus.ScatterWinningState })),
            CreateTransition(FrontEndPlayPennyStatus.ScatterWinningState,
                            (new FrontEndPlayPennyStatus[] {  FrontEndPlayPennyStatus.Playable, FrontEndPlayPennyStatus.ExpandingWinningState})),
            CreateTransition(FrontEndPlayPennyStatus.ExpandingWinningState,
                            (new FrontEndPlayPennyStatus[] { FrontEndPlayPennyStatus.Idle, FrontEndPlayPennyStatus.Playable, FrontEndPlayPennyStatus.ActionGamePlayable})),
        };

        /* El status de la Transaction */
        /* The status of the Transaction */
        public FrontEndPlayPennyStatus thisstatus;

        public decimal Amount;
        public DateTime TrigerTS;
        public DateTime ResetTS;
        public string ResetMode;

        /* El timestamp de la última transición*/
        /* The timestamp of the last transition*/
        public DateTime LastTransitionTS;
        public FrontEndPlayPenny()
        {
            thisstatus = FrontEndPlayPennyStatus.Idle;
        }

        internal delegate void TransactionExecutedEvent(FrontEndPlayPennyStatus s, bool persist, EventArgs e);
        internal TransactionExecutedEvent TransitionExecuted;

        /// <summary>
        /// UpdateStatus
        /// </summary>
        /// <param name="status_"></param>
        internal void UpdateStatus(FrontEndPlayPennyStatus status_, bool persist_)
        {
            thisstatus = status_;
            TransitionExecuted(status_, persist_, new EventArgs());
        }


        // Función de transición. Retorna true si transicionó bien, retorna false si no pudo transicionar.
        // Transition function. Returns true if it transitioned well, returns false if it failed to transition.
        public bool Transition(FrontEndPlayPennyStatus status_)
        {
            // Obtiene la transición guardada de status, de la SM
            // Gets the saved status transition, from the GI
            ValidTransition<FrontEndPlayPennyStatus> transition = state_machine.Where(t => t.status == thisstatus).FirstOrDefault();
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
            if (thisstatus != FrontEndPlayPennyStatus.Idle)
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
            thisstatus = FrontEndPlayPennyStatus.Idle;
            LastTransitionTS = DateTime.Now;
        }
    }
}