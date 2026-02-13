using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using EGMENGINE.BillAccCTL;
using EGMENGINE.BillAccCTL.Impl.SSPBillAccCTL.ITLValidator;
using EGMENGINE.BillAccCTL.Impl.SSPBillAccCTL.ITLValidator.SSP;
using EGMENGINE.BillAccCTLModule.BillAccTypes;
using EGMENGINE.BillAccCTLModule.Impl.SSPBillAccServiceCTL.MessageTypes;
using EGMENGINE.GLOBALTYPES;
using EGMENGINE.GUI.MENUTYPES;
using ITLlib;
using Newtonsoft.Json;
using static System.Net.Mime.MediaTypeNames;
using static EGMENGINE.BillAccCTL.Impl.SSPBillAccCTL.ITLValidator.CValidator;

namespace EGMENGINE.BillAccCTLModule.Impl.SSPBillAccServiceCTL
{


    /// <summary>
    /// The SSP Bill Acceptor Service Controller. An indirect controller of validator, through windows service by pipes
    /// </summary>
    internal class SSPBillAccServiceCTL : IBillAccCTL
    {
        public event CTLBillAcceptedHandler BillAccepted;

        public event CTLBillAcceptorSingleEventHandler CashboxRemoved;

        public event CTLBillAcceptorSingleEventHandler CashboxReplaced;

        public event CTLBillAcceptorSingleEventHandler CashboxFull;
        public event CTLBillAcceptorSingleEventHandler NoteRejected;


        public event CTLBillAcceptorSingleEventHandler BillJam;
        public event CTLBillAcceptorAlertEventHandler BillAcceptorUnreachable;
        public event CTLBillAcceptorAlertEventHandler BillAcceptorCommRestored;

        internal BillAccStateMachine billaccSM; // The bill acceptor state machine

        internal BillValidatorLogDetail? currentvalidatorlogdetail = null;
        // SSP Bill Acc Service Controller has a task t
        Task t;

        /// Properties of bill validator (Firmware, Serial Number, Channel Amounts, Channel Currencies)
        private string pBillValidatorFirmware = "";
        private string pSerialNumber = "";
        int errorcount = 0;
        int validatorerrorcount = 0;
        bool billacceptorunreachable = false;
        bool billacceptorcommerror = false;
        bool billacceptorcommrestored = true;
        private string pBVMinVersion = "1.0.0.3";
        private Dictionary<int, int> pchannelamounts;
        private Dictionary<int, string> pchannelcurrencies;
        private Dictionary<int, bool> penabledChannels;

        private ConcurrentQueue<BillValidatorLog> logs = new ConcurrentQueue<BillValidatorLog>();

        /// Pipes names
        private string InfoPipeSend = "SrvRequestPipe";
        private string InfoPipeRcv = "SrvResponsePipe";
        private string EGMOnlinePipeSend = "SrvEGMOnlineRequestPipe";
        private string EGMOnlinePipeRcv = "SrvEGMOnlineResponsePipe";
        private string InfoPipeEvents = "SrvEventPipe";

        private bool onSendMessageToPipe = false;
        private bool onSendKeepAliveMessage = false;

        private bool enabled = false;
        private System.Timers.Timer syncTimer = new System.Timers.Timer(500);

        private System.Timers.Timer billacctimer = new System.Timers.Timer(2000);

        private string Myport = "NULL";
        private byte Mylsb = 255;
        private byte Mysnd_byte = 255;
        private byte Mythrd_byte = 255;

        string IBillAccCTL.Port { get { return Myport; } set { Myport = value; } }
        string IBillAccCTL.BillValidatorFirmware { get { return pBillValidatorFirmware; } set { pBillValidatorFirmware = value; } }
        string IBillAccCTL.SerialNumber { get { return pSerialNumber; } set { pSerialNumber = value; } }
        Dictionary<int, int> IBillAccCTL.channelamounts { get { return pchannelamounts; } set { pchannelamounts = value; } }
        Dictionary<int, string> IBillAccCTL.channelcurrencies { get { return pchannelcurrencies; } set { pchannelcurrencies = value; } }
        Dictionary<int, bool> IBillAccCTL.enabledChannels { get { return penabledChannels; } set { penabledChannels = value; } }
        bool IBillAccCTL.Enabled { get { return enabled; } set { enabled = value; } }


        /// <summary>
        /// Constructor of SSP Bill Acceptor Service Controller
        /// Initialization of components
        /// </summary>
        public SSPBillAccServiceCTL(string port)
        {
            Myport = port;
            billaccSM = new BillAccStateMachine();
            pSerialNumber = "";
            pchannelamounts = new Dictionary<int, int>();
            pchannelcurrencies = new Dictionary<int, string>();
            pBillValidatorFirmware = "";
            syncTimer.Elapsed += new ElapsedEventHandler((o, e) =>
            {
                if (!onSendKeepAliveMessage)
                {
                    string result = SendKeepAlivePoll();
                    if (result == "online")
                    {
                        errorcount = 0;
                        validatorerrorcount = 0;
                        if (!billacceptorcommrestored)
                        {
                            billacceptorcommrestored = true;
                            if (billacceptorcommerror)
                            {
                                billacceptorcommerror = false;
                                try { BillAcceptorCommRestored?.Invoke("Bill Acceptor Port comm Failure", this); ; } catch { }
                            }
                            if (billacceptorunreachable)
                            {
                                billacceptorunreachable = false;
                                try { BillAcceptorCommRestored?.Invoke("Bill Acceptor Service comm failure", this); ; } catch { }
                            }
                        }

                    }
                    else if (result == "billacceptorportcommfailure")
                    {
                        validatorerrorcount++;
                    }
                    else
                    {
                        errorcount++;
                    }
                    if (validatorerrorcount >= 10)
                    {
                        validatorerrorcount = 0;
                        billacceptorcommrestored = false;
                        billacceptorcommerror = true;
                        try { BillAcceptorUnreachable?.Invoke("Bill Acceptor Port comm Failure", this); ; } catch { }
                    }
                    if (errorcount >= 10)
                    {
                        errorcount = 0;
                        billacceptorcommrestored = false;
                        billacceptorunreachable = true;
                        try { BillAcceptorUnreachable?.Invoke("Bill Acceptor Service comm failure", this); ; } catch { }

                    }
                }
            });

            syncTimer.Start();


            Task.Run(() =>
            {
                // Send to service a command SrvGetInfo 
                string version = SendBillAccServiceCommand<string>("SrvGetVersion");
                int mybvMinversion = int.Parse(pBVMinVersion.Replace("1.0.0.", ""));
                int currentbvversion = int.Parse(version.Replace("1.0.0.", ""));
                if (currentbvversion < mybvMinversion)
                {
                    try { BillAcceptorUnreachable?.Invoke("BV Service outdated", this); ; } catch { }
                }
                //else if (currentbvversion-mybvMinversion > 20)
                //{
                //    try { BillAcceptorUnreachable?.Invoke("Bill Acceptor Service Version deprecated", this); ; } catch { }
                //}

            });


            //billacctimer.Elapsed += new ElapsedEventHandler((o, e) =>
            //{
            //    if (enabled)
            //    {
            //        if (!onSendMessageToPipe)
            //            SendBillAccServiceCommand<string>("SrvSetEnableValidator");
            //    }
            //    else
            //    {
            //        if (!onSendMessageToPipe)
            //            SendBillAccServiceCommand<string>("SrvSetDisableValidator");
            //    }
            //});

            //billacctimer.Start();
        }

        /// <summary>
        /// Function that sends a keep alive command to service via named pipes. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <returns></returns>
        private String SendKeepAlivePoll()
        {
            int tries = 5000;
            onSendKeepAliveMessage = true;
            while (tries > 0)
            {

                try
                {
                    // Creates and open the pipe for send request
                    NamedPipeServerStream pipeStreamSend = new NamedPipeServerStream(EGMOnlinePipeSend, PipeDirection.Out, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                    CancellationTokenSource cts = new CancellationTokenSource(1000);
                    pipeStreamSend.WaitForConnectionAsync(cts.Token);
                    using (StreamWriter sw = new StreamWriter(pipeStreamSend))
                    {
                        sw.WriteLine($"SrvSync {Myport} {Mylsb} {Mysnd_byte} {Mythrd_byte}");
                    }
                    pipeStreamSend.Close();
                    // Creates and open the pipe for receive the response
                    NamedPipeClientStream pipeStreamRcv = new NamedPipeClientStream(".", EGMOnlinePipeRcv, PipeDirection.In);
                    pipeStreamRcv.Connect();
                    using (StreamReader sr = new StreamReader(pipeStreamRcv))
                    {
                        while (sr.Peek() >= 0)
                        {
                            string json = sr.ReadLine();
                            pipeStreamRcv.Close();
                            String result;
                            try { result = JsonConvert.DeserializeObject<String>(json); } catch { onSendKeepAliveMessage = false; return default(String); }
                            onSendKeepAliveMessage = false;
                            return result;
                        }
                        pipeStreamRcv.Close();
                        tries--;

                    }
                }
                catch
                {
                    tries--;
                }
            }
            onSendKeepAliveMessage = false;
            return default(String);

        }



        /// <summary>
        /// Function that sends a custom command to service via named pipes. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <returns></returns>
        private T SendBillAccServiceCommand<T>(string command)
        {
            int tries = 5000;
            onSendMessageToPipe = true;
            while (tries > 0)
            {

                try
                {
                    // Creates and open the pipe for send request
                    NamedPipeServerStream pipeStreamSend = new NamedPipeServerStream(InfoPipeSend, PipeDirection.Out, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                    CancellationTokenSource cts = new CancellationTokenSource(1000);
                    pipeStreamSend.WaitForConnectionAsync(cts.Token);
                    using (StreamWriter sw = new StreamWriter(pipeStreamSend))
                    {
                        sw.WriteLine(command);
                    }
                    pipeStreamSend.Close();
                    // Creates and open the pipe for receive the response
                    NamedPipeClientStream pipeStreamRcv = new NamedPipeClientStream(".", InfoPipeRcv, PipeDirection.In);
                    pipeStreamRcv.Connect();
                    using (StreamReader sr = new StreamReader(pipeStreamRcv))
                    {
                        while (sr.Peek() >= 0)
                        {
                            string json = sr.ReadLine();
                            pipeStreamRcv.Close();
                            T result;
                            try { result = JsonConvert.DeserializeObject<T>(json); } catch { onSendMessageToPipe = false; return default(T); }
                            onSendMessageToPipe = false;
                            return result;
                        }
                        pipeStreamRcv.Close();
                        tries--;

                    }
                }
                catch
                {
                    tries--;
                }
            }
            onSendMessageToPipe = false;
            return default(T);

        }


        /// <summary>
        /// Listens the pipe for events
        /// </summary>
        void ListenPipe()
        {
            while (true)
            {
                // Creates and open the pipe for receive events continuoslly
                NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", InfoPipeEvents, PipeDirection.In);

                pipeClient.Connect();

                using var sr = new StreamReader(pipeClient);
                string? temp;
                while ((temp = sr.ReadLine()) != null)
                {
                    try
                    {
                        Event _event = JsonConvert.DeserializeObject<Event>(temp);

                        switch (_event.EventName)
                        {
                            case "CommEnabled":
                                {
                                    BillValidatorInfo info = JsonConvert.DeserializeObject<BillValidatorInfo>(_event.data.ToString());

                                    // Check if info is not null and save to the properties of SSPBillAcc
                                    if (info != null)
                                    {
                                        pBillValidatorFirmware = info.BillValidatorFirmware;
                                        pSerialNumber = info.SerialNumber; ;
                                        pchannelamounts = info.channelamounts;
                                        pchannelcurrencies = info.channelcurrencies;
                                        penabledChannels = info.enabledChannels;
                                    }
                                    break;
                                }
                            case "ChannelConfigUpdated":
                                {

                                    BillValidatorInfo info = JsonConvert.DeserializeObject<BillValidatorInfo>(_event.data.ToString());
                                    // Check if info is not null and save to the properties of SSPBillAcc
                                    if (info != null)
                                    {
                                        pBillValidatorFirmware = info.BillValidatorFirmware;
                                        pSerialNumber = info.SerialNumber; ;
                                        pchannelamounts = info.channelamounts;
                                        pchannelcurrencies = info.channelcurrencies;
                                        penabledChannels = info.enabledChannels;
                                    }
                                    break;
                                }
                            case "BillInserted":
                                {
                                    if (billaccSM.Transition(BillAccStatus.BillInserted))
                                        if (billaccSM.Transition(BillAccStatus.Validating))
                                            ;
                                    break;
                                }
                            case "BillAccepted":
                                {
                                    if (billaccSM.Transition(BillAccStatus.Stacking))
                                    {
                                        BillDenom denomObj = JsonConvert.DeserializeObject<BillDenom>(_event.data.ToString());
                                        EGM egmInstance = EGM.GetInstance();
                                        try { BillAccepted?.Invoke(denomObj.denom, this); ; } catch { }
                                        billaccSM.Transition(BillAccStatus.Idle);


                                        Logger.Log($"Bill accepted: {denomObj.denom}");

                                        // WebSocket is now sent from the event handler in EGM.cs to avoid duplicates

                                    }
                                    break;
                                }
                            case "EventLog":
                                {

                                    BillValidatorLogDetail detail = (BillValidatorLogDetail)Enum.Parse(typeof(BillValidatorLogDetail), _event.data.ToString());
                                    if (detail != currentvalidatorlogdetail)
                                    {
                                        currentvalidatorlogdetail = detail;
                                        if (detail == BillValidatorLogDetail.CashboxRemoved)
                                        {
                                            CashboxRemoved(this);
                                        }
                                        else if (detail == BillValidatorLogDetail.CashboxReplaced)
                                        {
                                            CashboxReplaced(this);
                                        }
                                        else if (detail == BillValidatorLogDetail.NoteRejecting)
                                        {
                                            NoteRejected(this);
                                        }
                                        else if (detail == BillValidatorLogDetail.StackerFull)
                                        {
                                            CashboxFull(this);
                                        }
                                        else if (detail == BillValidatorLogDetail.UnsafeNoteJam)
                                        {
                                            BillJam(this);
                                        }
                                        else if (detail == BillValidatorLogDetail.SafeNoteJam)
                                        {
                                            BillJam(this);
                                        }

                                        logs.Enqueue(new BillValidatorLog(DateTime.Now, detail));
                                    }
                                    break;
                                }
                            default:
                                break;
                        }


                    }
                    catch
                    {

                    }
                }
            }

        }

        /// <summary>
        /// Start Bill Acceptor Controller;
        /// 
        /// </summary>
        public void StartBillAccCTL()
        {
            Task.Run(ListenPipe);
        }

        /// <summary>
        /// Stop Bill Acceptor Contoller;
        /// </summary>
        public void StopBillAcc()
        {

        }

        public void AcceptTicket()
        {
            throw new NotImplementedException();
        }
        public void RejectTicket()
        {
            throw new NotImplementedException();
        }
        public BillAccStatus CurrentState()
        {
            return billaccSM.status;
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

        void IBillAccCTL.EnableBillAcceptor()
        {
            if (enabled)
            {
                if (!onSendMessageToPipe)
                    SendBillAccServiceCommand<string>("SrvSetEnableValidator");
            }
        }

        void IBillAccCTL.DisableBillAcceptor()
        {
            if (enabled)
            {
                if (!onSendMessageToPipe)
                    SendBillAccServiceCommand<string>("SrvSetDisableValidator");
            }

        }

        void IBillAccCTL.ConfigBillDenominations(byte lsb, byte snd_byte, byte thrd_byte)
        {
            Mylsb = lsb;
            Mysnd_byte = snd_byte;
            Mythrd_byte = thrd_byte;
        }
    }
}
