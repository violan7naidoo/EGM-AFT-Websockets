using EGMENGINE.BillAccCTLModule.BillAccTypes;
using EGMENGINE.GLOBALTYPES;
using System;
using System.Collections.Generic;

namespace EGMENGINE.BillAccCTLModule
{
    internal interface IBillAccCTL
    {

        public string BillValidatorFirmware { get; set; }
        public string SerialNumber { get; set; }
        public bool Enabled { get; set; }
        public string Port { get; set; }
        public Dictionary<int, int> channelamounts { get; set; }
        public Dictionary<int, string> channelcurrencies { get; set; }
        public Dictionary<int, bool> enabledChannels { get; set; }

        public event CTLBillAcceptedHandler BillAccepted;
        public event CTLBillAcceptorSingleEventHandler CashboxRemoved;
        public event CTLBillAcceptorSingleEventHandler CashboxReplaced;
        public event CTLBillAcceptorSingleEventHandler CashboxFull;
        public event CTLBillAcceptorSingleEventHandler NoteRejected;
        public event CTLBillAcceptorSingleEventHandler BillJam;
        public event CTLBillAcceptorAlertEventHandler BillAcceptorUnreachable;
        public event CTLBillAcceptorAlertEventHandler BillAcceptorCommRestored;

        public List<BillValidatorLog> GetValidatorLogs();
        public void StartBillAccCTL();
        public BillAccStatus CurrentState();
        public void AcceptTicket();
        public void RejectTicket();
        public void EnableBillAcceptor();
        public void DisableBillAcceptor();
        public void ConfigBillDenominations(byte lsb, byte snd_byte, byte thrd_byte);

    }
}