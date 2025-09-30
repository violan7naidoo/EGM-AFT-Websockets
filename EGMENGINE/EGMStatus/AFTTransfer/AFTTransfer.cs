using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EGMENGINE.EGMStatusModule.AFTTransferModule
{
    // La enumeración AFTCurrentTransactionStatus: Los estados de una transacción actual
    // AFTCurrentTransactionStatus enumeration: The status of a current transaction
    internal enum AFTTransferStatus
    {
        Idle,
        TransferIncoming,
        TransferPending,
        TransferCompleted,
        TransferRejected,
        TransferInterrogated,

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


    internal class AFTTransfer
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
        private ValidTransition<AFTTransferStatus>[] state_machine = new ValidTransition<AFTTransferStatus>[] {
             CreateTransition(AFTTransferStatus.Idle,
                            new AFTTransferStatus[] { AFTTransferStatus.TransferIncoming }),
            CreateTransition(AFTTransferStatus.TransferIncoming,
                            new AFTTransferStatus[] { AFTTransferStatus.TransferPending, AFTTransferStatus.Idle }),
            CreateTransition(AFTTransferStatus.TransferPending,
                            new AFTTransferStatus[] { AFTTransferStatus.TransferCompleted, AFTTransferStatus.TransferRejected }),
            CreateTransition(AFTTransferStatus.TransferCompleted,
                            new AFTTransferStatus[] { AFTTransferStatus.TransferInterrogated }),
            CreateTransition(AFTTransferStatus.TransferRejected,
                            new AFTTransferStatus[] { AFTTransferStatus.TransferInterrogated }),
            CreateTransition(AFTTransferStatus.TransferInterrogated,
                            new AFTTransferStatus[] { AFTTransferStatus.Idle })

         };

        /* El status de la Transaction */
        /* The status of the Transaction */
        public AFTTransferStatus status;
        /* El timestamp de la última transición*/
        /* The timestamp of the last transition*/
        public DateTime LastTransitionTS;
        /* The amount of current transfer */
        public decimal amount;
        public AFTTransfer()
        {
            status = AFTTransferStatus.Idle;
        }

        // Función de transición. Retorna true si transicionó bien, retorna false si no pudo transicionar.
        // Transition function. Returns true if it transitioned well, returns false if it failed to transition.
        public bool Transition(AFTTransferStatus status_)
        {
            // Obtiene la transición guardada de status, de la SM
            // Gets the saved status transition, from the GI
            ValidTransition<AFTTransferStatus> transition = state_machine.Where(t => t.status == status).FirstOrDefault();
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
            if (status == AFTTransferStatus.TransferIncoming
             || status == AFTTransferStatus.TransferPending
             || status == AFTTransferStatus.TransferCompleted
             || status == AFTTransferStatus.TransferInterrogated)
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
            status = AFTTransferStatus.Idle;
            LastTransitionTS = DateTime.Now;
        }

    }
}
