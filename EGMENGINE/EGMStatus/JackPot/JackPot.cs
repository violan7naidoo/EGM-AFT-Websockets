using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace EGMENGINE.EGMStatusModule.JackPotModule
{
    // La enumeración AFTCurrentTransactionStatus: Los estados de una transacción actual
    // AFTCurrentTransactionStatus enumeration: The status of a current transaction
    internal enum JackPotStatus
    {
        Idle,
        JackpotPending,
        JackpotResetByKey

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


    internal class JackPot
    {
        internal delegate void TransactionExecutedEvent(JackPotStatus s, bool persist, EventArgs e);
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
        private ValidTransition<JackPotStatus>[] state_machine = new ValidTransition<JackPotStatus>[] {
             CreateTransition(JackPotStatus.Idle,
                            new JackPotStatus[] { JackPotStatus.JackpotPending }),
            CreateTransition(JackPotStatus.JackpotPending,
                            new JackPotStatus[] { JackPotStatus.JackpotResetByKey }),
               CreateTransition(JackPotStatus.JackpotResetByKey,
                            new JackPotStatus[] { JackPotStatus.Idle })


         };

        /* El status de la Transaction */
        /* The status of the Transaction */
        public JackPotStatus status;

        public decimal Amount;
        public DateTime TrigerTS;
        public DateTime ResetTS;
        public string ResetMode;

        /* El timestamp de la última transición*/
        /* The timestamp of the last transition*/
        public DateTime LastTransitionTS;
        public JackPot()
        {
            status = JackPotStatus.Idle;
        }

        /// <summary>
        /// UpdateStatus
        /// </summary>
        /// <param name="status_"></param>
        internal void UpdateStatus(JackPotStatus status_, bool persist_)
        {
            status = status_;
            TransitionExecuted(status, persist_, new EventArgs());
        }


        // Función de transición. Retorna true si transicionó bien, retorna false si no pudo transicionar.
        // Transition function. Returns true if it transitioned well, returns false if it failed to transition.
        public bool Transition(JackPotStatus status_)
        {
            // Obtiene la transición guardada de status, de la SM
            // Gets the saved status transition, from the GI
            ValidTransition<JackPotStatus> transition = state_machine.Where(t => t.status == status).FirstOrDefault();
            // Si puede transicionar.. retorna true y el status actual es el status nuevo
            // If it can transition... it returns true and the current status is the new status.
            if (transition.next_status.Contains(status_))
            {
                if (status_ == JackPotStatus.JackpotPending)
                {
                    TrigerTS = DateTime.Now;
                }
                else if (status_ == JackPotStatus.JackpotResetByKey)
                {
                    ResetTS = DateTime.Now;
                }
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
            if (status == JackPotStatus.JackpotPending)
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
            status = JackPotStatus.Idle;
            LastTransitionTS = DateTime.Now;
        }

    }
}
