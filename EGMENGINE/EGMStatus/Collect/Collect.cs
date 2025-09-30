using EGMENGINE.EGMStatusModule.HandPayModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EGMENGINE.EGMStatusModule.CollectModule
{
    // La enumeración AFTCurrentTransactionStatus: Los estados de una transacción actual
    // AFTCurrentTransactionStatus enumeration: The status of a current transaction
    internal enum CollectStatus
    {
        Idle,
        Waiting72,
        HandpayInProgress,
        AFTCollectInProgress,
        Completed,

    }



    internal class Collect
    {
        internal delegate void TransactionExecutedEvent(CollectStatus s, bool persist, EventArgs e);
        internal TransactionExecutedEvent TransitionExecuted;
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
        private ValidTransition<CollectStatus>[] state_machine = new ValidTransition<CollectStatus>[] {
             CreateTransition(CollectStatus.Idle,
                            new CollectStatus[] { CollectStatus.Waiting72 , CollectStatus.HandpayInProgress }),
            CreateTransition(CollectStatus.Waiting72,
                            new CollectStatus[] { CollectStatus.AFTCollectInProgress, CollectStatus.HandpayInProgress }),
            CreateTransition(CollectStatus.HandpayInProgress,
                            new CollectStatus[] { CollectStatus.Completed }),
            CreateTransition(CollectStatus.AFTCollectInProgress,
                            new CollectStatus[] { CollectStatus.HandpayInProgress, CollectStatus.Completed  }),
            CreateTransition(CollectStatus.Completed,
                            new CollectStatus[] { CollectStatus.Idle })

         };

        /* El status de la Transaction */
        /* The status of the Transaction */
        public CollectStatus status;
        /* El timestamp de la última transición*/
        /* The timestamp of the last transition*/
        public DateTime LastTransitionTS;
        public Collect()
        {
            status = CollectStatus.Idle;
        }


        /// <summary>
        /// UpdateStatus
        /// </summary>
        /// <param name="status_"></param>
        internal void UpdateStatus(CollectStatus status_, bool persist)
        {
            status = status_;
            TransitionExecuted(status, persist, new EventArgs());
        }

        // Función de transición. Retorna true si transicionó bien, retorna false si no pudo transicionar.
        // Transition function. Returns true if it transitioned well, returns false if it failed to transition.
        public bool Transition(CollectStatus status_)
        {
            // Obtiene la transición guardada de status, de la SM
            // Gets the saved status transition, from the GI
            ValidTransition<CollectStatus> transition = state_machine.Where(t => t.status == status).FirstOrDefault();
            // Si puede transicionar.. retorna true y el status actual es el status nuevo
            // If it can transition... it returns true and the current status is the new status.
            if (transition.next_status.Contains(status_))
            {
                LastTransitionTS = DateTime.Now;
                UpdateStatus(status_, true);
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
            if (status == CollectStatus.Waiting72
             || status == CollectStatus.HandpayInProgress
             || status == CollectStatus.AFTCollectInProgress)
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
            status = CollectStatus.Idle;
            LastTransitionTS = DateTime.Now;
        }

    }
}
