using EGMENGINE.BillAccCTLModule;
using EGMENGINE.BillAccCTLModule.BillAccTypes;
using EGMENGINE.GLOBALTYPES;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EGMENGINE.BillAccCTLModule.Impl.VirtualBillAccCTL
{


    internal class VirtualBillAccCTL : IBillAccCTL
    {
        public event CTLBillAcceptedHandler BillAccepted;

        public event CTLBillAcceptorSingleEventHandler CashboxRemoved;

        public event CTLBillAcceptorSingleEventHandler CashboxReplaced;

        public event CTLBillAcceptorSingleEventHandler CashboxFull;
        public event CTLBillAcceptorSingleEventHandler NoteRejected;
        public event CTLBillAcceptorSingleEventHandler BillJam;
        public event CTLBillAcceptorAlertEventHandler BillAcceptorUnreachable;
        public event CTLBillAcceptorAlertEventHandler BillAcceptorCommRestored;


        private string port = "COM4";
        string IBillAccCTL.Port { get { return port; } set { port = value; } }

        private string pBillValidatorFirmware = "VFirmware";
        string IBillAccCTL.BillValidatorFirmware { get { return pBillValidatorFirmware; } set { pBillValidatorFirmware = value; } }
        
        private string pSerialNumber = "VSerialNumber";
        string IBillAccCTL.SerialNumber { get { return pSerialNumber; } set { pSerialNumber = value;  } }
        
        private Dictionary<int, int> pchannelAmounts = new Dictionary<int, int>();
        Dictionary<int, int> IBillAccCTL.channelamounts { get { return pchannelAmounts;  } set { pchannelAmounts = value;  } }
       
        private Dictionary<int, string> pchannelCurrencies = new Dictionary<int, string>();
        Dictionary<int, string> IBillAccCTL.channelcurrencies { get { return pchannelCurrencies; } set { pchannelCurrencies = value; } }

        private Dictionary<int, bool> penabledChannels = new Dictionary<int, bool>();
        Dictionary<int, bool> IBillAccCTL.enabledChannels {  get { return penabledChannels; } set { penabledChannels = value; } }

        private ConcurrentQueue<BillValidatorLog> logs = new ConcurrentQueue<BillValidatorLog>();

        internal BillAccStateMachine billaccSM;

        private bool enabled = true;

        public VirtualBillAccCTL()
        {
            billaccSM = new BillAccStateMachine();
            enabled = true;
            pchannelAmounts.Add(1, 10);
            pchannelAmounts.Add(2, 50);
            pchannelAmounts.Add(3, 100);
            pchannelAmounts.Add(4, 500);
            pchannelAmounts.Add(5, 1000);
            pchannelCurrencies.Add(1, "USD");
            pchannelCurrencies.Add(2, "USD");
            pchannelCurrencies.Add(3, "USD");
            pchannelCurrencies.Add(4, "USD");
            pchannelCurrencies.Add(5, "USD");
            penabledChannels.Add(1, true);
            penabledChannels.Add(2, true);
            penabledChannels.Add(3, true);
            penabledChannels.Add(4, true);
            penabledChannels.Add(5, true);
            logs.Enqueue(new BillValidatorLog(DateTime.Now, BillValidatorLogDetail.StackerFull));
            logs.Enqueue(new BillValidatorLog(DateTime.Now.AddMinutes(1), BillValidatorLogDetail.ValidatorDisabled));
            logs.Enqueue(new BillValidatorLog(DateTime.Now.AddMinutes(2), BillValidatorLogDetail.CashboxRemoved));
            logs.Enqueue(new BillValidatorLog(DateTime.Now.AddMinutes(3), BillValidatorLogDetail.SafeNoteJam));
            logs.Enqueue(new BillValidatorLog(DateTime.Now.AddMinutes(4), BillValidatorLogDetail.NoteStacked));
        }


        internal void Validate(int b)
        {
            if (billaccSM.Transition(BillAccStatus.Validating))
            {
                switch(b)
                {
                    case 5:
                        AcceptBill(5);
                        break;
                    case 10:
                        AcceptBill(10);
                        break;
                    case 20:
                        AcceptBill(20);
                        break;
                    case 50:
                        AcceptBill(50);
                        break;
                    case 100:
                        AcceptBill(100);
                        break;
                    case 200:
                        AcceptBill(200);
                        break;
                    case 500:
                        AcceptBill(500);
                        break;
                    case 1000:
                        AcceptBill(1000);
                        break;
                    default:
                        RejectBill(b);
                        break;
                }
            }
        }

        public void InsertBill(int b)
        {
            if (billaccSM.Transition(BillAccStatus.BillInserted))
                Validate(b);
        }

        internal void AcceptBill(int b)
        {
            if (billaccSM.Transition(BillAccStatus.Stacking))
            {
                BillAccepted?.Invoke(b, this);
                billaccSM.Transition(BillAccStatus.Idle);
            }
        }

        public void AcceptTicket()
        {
            if (billaccSM.Transition(BillAccStatus.Stacking))
            {
                billaccSM.Transition(BillAccStatus.Idle);
            }

        }

        internal void RejectBill(int b)
        {
            if (billaccSM.Transition(BillAccStatus.Rejecting))
            {
                //BillRejectedEvent(b, this);
                billaccSM.Transition(BillAccStatus.Idle);
            }
        }


        public void RejectTicket()
        {
            if (billaccSM.Transition(BillAccStatus.Rejecting))
            {
                billaccSM.Transition(BillAccStatus.Idle);
            }
        }

        public BillAccStatus CurrentState()
        {
            return billaccSM.status;
        }

        public void StartBillAccCTL()
        {

        }

        void IBillAccCTL.StartBillAccCTL()
        {

        }

        List<BillValidatorLog> IBillAccCTL.GetValidatorLogs()
        {
            List<BillValidatorLog> result = new List<BillValidatorLog>();
            BillValidatorLog log = new BillValidatorLog();
            while (logs.TryDequeue(out log))
            {
                result.Add(log);
            }

            return result;
        }
        bool IBillAccCTL.Enabled { get { return enabled; } set { enabled = value; } }


        void IBillAccCTL.EnableBillAcceptor()
        {
            enabled = true;
        }

        void IBillAccCTL.DisableBillAcceptor()
        {
            enabled = false;
        }

        void IBillAccCTL.ConfigBillDenominations(byte lsb, byte snd_byte, byte thrd_byte)
        {
            
        }
    }
}
