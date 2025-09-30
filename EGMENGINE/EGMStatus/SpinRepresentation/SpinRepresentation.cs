using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace EGMENGINE.GUI.GAMETYPES
{
    // La enumeración SpinRepresentationStatus: Los estados de una spin actual
    // SpinRepresentationStatus enumeration: The status of a current spin
    internal enum SpinRepresentationStatus
    {
        Idle,
        RNGDrawn,
        ReelSpinning,
        ReelsStopped,
        BasePrizeShown,
        PennyGamesPlaying,
        FullyRepresented
    }

    /// <summary>
    /// ValidTransition Definition   
    /// It represents a transition. Given a Type T status, it lists all the states to which transition can be made in next_status type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ValidTransition<T>
    {
        public T status;
        public T[] next_status;
    }


    internal class SpinRepresentation
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
        private ValidTransition<SpinRepresentationStatus>[] state_machine = new ValidTransition<SpinRepresentationStatus>[] {
            CreateTransition(SpinRepresentationStatus.Idle,
                            new SpinRepresentationStatus[] { SpinRepresentationStatus.RNGDrawn }),
            CreateTransition(SpinRepresentationStatus.RNGDrawn,
                            new SpinRepresentationStatus[] { SpinRepresentationStatus.ReelSpinning }),
            CreateTransition(SpinRepresentationStatus.ReelSpinning,
                            new SpinRepresentationStatus[] { SpinRepresentationStatus.ReelsStopped }),
            CreateTransition(SpinRepresentationStatus.ReelsStopped,
                            new SpinRepresentationStatus[] { SpinRepresentationStatus.BasePrizeShown }),
            CreateTransition(SpinRepresentationStatus.BasePrizeShown,
                            new SpinRepresentationStatus[] { SpinRepresentationStatus.FullyRepresented, SpinRepresentationStatus.PennyGamesPlaying }),
            CreateTransition(SpinRepresentationStatus.PennyGamesPlaying,
                            new SpinRepresentationStatus[] { SpinRepresentationStatus.FullyRepresented }),
            CreateTransition(SpinRepresentationStatus.FullyRepresented,
                            new SpinRepresentationStatus[] { SpinRepresentationStatus.Idle }),
         };

        /* El status de la Transaction */
        /* The status of the Transaction */
        public SpinRepresentationStatus status;

        /* El timestamp de la última transición*/
        /* The timestamp of the last transition*/
        public DateTime LastTransitionTS;
        public SpinRepresentation()
        {
            status = SpinRepresentationStatus.Idle;
        }

        // Función de transición. Retorna true si transicionó bien, retorna false si no pudo transicionar.
        // Transition function. Returns true if it transitioned well, returns false if it failed to transition.
        public bool Transition(SpinRepresentationStatus status_)
        {
            // Obtiene la transición guardada de status, de la SM
            // Gets the saved status transition, from the GI
            ValidTransition<SpinRepresentationStatus> transition = state_machine.Where(t => t.status == status).FirstOrDefault();
            // Si puede transicionar.. retorna true y el status actual es el status nuevo
            // If it can transition... it returns true and the current status is the new status.
            if (transition.next_status.Contains(status_))
            {
                status = status_;
                LastTransitionTS = DateTime.Now;
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
            if (status == SpinRepresentationStatus.RNGDrawn
             || status == SpinRepresentationStatus.ReelSpinning
             || status == SpinRepresentationStatus.ReelsStopped
             || status == SpinRepresentationStatus.BasePrizeShown
             || status == SpinRepresentationStatus.PennyGamesPlaying)
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
            status = SpinRepresentationStatus.Idle;
            LastTransitionTS = DateTime.Now;
        }

    }
}
