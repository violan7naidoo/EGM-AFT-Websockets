using System;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;

namespace EGMENGINE.BillAccCTLModule
{

    // Enumeration of the statuses of the bill acceptor controller
    internal enum BillAccStatus
    {
        Idle,
        BillInserted,
        Validating,
        Stacking,
        Rejecting,
        Jammed
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

    /// <summary>
    ///  The state machine of the bill acceptor controller
    /// </summary>
    internal class BillAccStateMachine
    {
        /// <summary>
        /// Private function that creates a transition from state to different states;
        /// Example, CreateTransition(from, [to1, to2, to3]) generates
        /// from -> to1, from -> to2, from -> to3
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        private static ValidTransition<T> CreateTransition<T>(T from, T[] to)
        {
            return new ValidTransition<T>
            { /* FROM */
                status = from,
                /* TO */
                next_status = to
            };
        }

        // The state machine is modeled with all its transitions.
        private ValidTransition<BillAccStatus>[] state_machine = new ValidTransition<BillAccStatus>[] {


               CreateTransition(BillAccStatus.Idle,
                                               new BillAccStatus[] { BillAccStatus.BillInserted}),

               CreateTransition(BillAccStatus.BillInserted,
                                               new BillAccStatus[] {BillAccStatus.Validating, BillAccStatus.Idle }),

               CreateTransition(BillAccStatus.Validating,
                                               new BillAccStatus[] {BillAccStatus.Stacking, BillAccStatus.Rejecting}),

               CreateTransition(BillAccStatus.Stacking,
                                               new BillAccStatus[] { BillAccStatus.Idle, BillAccStatus.Jammed }),

               CreateTransition(BillAccStatus.Rejecting,
                                               new BillAccStatus[] { BillAccStatus.Idle, BillAccStatus.Jammed})




         };
        /* The internal current status of the state machine */
        internal BillAccStatus status;

        public BillAccStateMachine()
        {
            // I persist it through SaveData
            status = BillAccStatus.Idle;
        }

        // Transition function. Returns true if it transitioned well, returns false if it failed to transition.
        internal bool Transition(BillAccStatus status_)
        {
            // Gets the saved status transition, from the GI
            ValidTransition<BillAccStatus> transition = state_machine.Where(t => t.status == status).FirstOrDefault();
            // If it can transition... it returns true and the current status is the new status.
            if (transition.next_status.Contains(status_))
            {
                status = status_;
                //LastTransitionTS = DateTime.Now;
                // SaveData();
                return true;
            }
            else
            {
                return false;
            }
        }



    }



}