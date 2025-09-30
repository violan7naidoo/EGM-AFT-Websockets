using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using EGMENGINE.BillAccCTL;
using EGMENGINE.BillAccCTL.Impl.SSPBillAccCTL.ITLValidator;
using EGMENGINE.BillAccCTL.Impl.SSPBillAccCTL.ITLValidator.SSP;
using EGMENGINE.BillAccCTLModule.BillAccTypes;
using EGMENGINE.GLOBALTYPES;
using ITLlib;
using static System.Net.Mime.MediaTypeNames;
using static EGMENGINE.BillAccCTL.Impl.SSPBillAccCTL.ITLValidator.CValidator;

namespace EGMENGINE.BillAccCTLModule.Impl.SSPBillAccCTL
{

    /// <summary>
    /// The SSP Bill Acceptor Controller. The direct controller to bill acceptor
    /// </summary>
    internal class SSPBillAccCTL : IBillAccCTL
    {
        public event CTLBillAcceptedHandler BillAccepted;

        public event CTLBillAcceptorSingleEventHandler CashboxRemoved;

        public event CTLBillAcceptorSingleEventHandler CashboxReplaced;
        public event CTLBillAcceptorSingleEventHandler CashboxFull;
        public event CTLBillAcceptorSingleEventHandler BillJam;
        public event CTLBillAcceptorSingleEventHandler NoteRejected;
        public event CTLBillAcceptorAlertEventHandler BillAcceptorUnreachable;
        public event CTLBillAcceptorAlertEventHandler BillAcceptorCommRestored;


        internal BillAccStateMachine billaccSM; // The bill acceptor state machine

        internal CValidator billvalidator;

        bool Running = false; // Indicates the status of the main poll loop

        int reconnectionAttempts = 10, reconnectionInterval = 3; // Connection info to deal with retrying connection to validator
        volatile bool Connected = false, ConnectionFail = false; // Threading bools to indicate status of connection with validator
        System.Timers.Timer reconnectionTimer = new System.Timers.Timer(); // Timer used to give a delay between reconnect attempts
        Thread ConnectionThread; // Thread used to connect to the validator
        System.Timers.Timer pollingTimer; // Timer for polling to bill validator
        int pollTimer = 250; // Timer in ms between polls

        /// Properties of bill validator (Firmware, Serial Number, Channel Amounts, Channel Currencies)
        private string pBillValidatorFirmware = "";
        private string pSerialNumber = "";
        private Dictionary<int, int> pchannelamounts;
        private Dictionary<int, string> pchannelcurrencies;
        private Dictionary<int, bool> penabledChannels;

        private ConcurrentQueue<BillValidatorLog> logs = new ConcurrentQueue<BillValidatorLog>();

        private string port = "COM4";
        string IBillAccCTL.Port { get { return port; } set { port = value; } }
        string IBillAccCTL.BillValidatorFirmware { get { return pBillValidatorFirmware; } set { pBillValidatorFirmware = value; } }
        string IBillAccCTL.SerialNumber { get { return pSerialNumber; } set { pSerialNumber = value; } }
        Dictionary<int, int> IBillAccCTL.channelamounts { get { return pchannelamounts; } set { pchannelamounts = value; } }
        Dictionary<int, string> IBillAccCTL.channelcurrencies { get { return pchannelcurrencies; } set { pchannelcurrencies = value; } }
        Dictionary<int, bool> IBillAccCTL.enabledChannels { get { return penabledChannels; } set { penabledChannels = value; } }
        bool IBillAccCTL.Enabled { get { return true; } set { bool v = value; } }


        /// <summary>
        /// Constructor of SSP Bill Acceptor Controller
        /// Initialization of components and events suscriptions
        /// </summary>
        public SSPBillAccCTL()
        {
            billaccSM = new BillAccStateMachine();
            billvalidator = new CValidator();

            // Suscribe the Validator_Bill_Inserted event to the following method
            billvalidator.Validator_Bill_Inserted += new ValidatorBillEvent((billNum, obj) =>
            {
                // Transition to BillInserted -> Validating
                if (billaccSM.Transition(BillAccStatus.BillInserted))
                    if (billaccSM.Transition(BillAccStatus.Validating))
                        ;
            });
            // Suscribe the Validator_Bill_Accepted event to the following method
            billvalidator.Validator_Bill_Accepted += new ValidatorBillEvent((billNum, obj) => 
            {
                // Transition to Stacking
                if (billaccSM.Transition(BillAccStatus.Stacking))
                {
                    // Throw the BillAccepted event with the bill num as parameter
                    try { BillAccepted?.Invoke(billNum, this);  ;} catch { }
                    // Transition to Idle
                    billaccSM.Transition(BillAccStatus.Idle);
                }
            });
            // Suscribe the Validator Event Log to the following method
            billvalidator.LogEvent += new ValidatorLogEvent((log, obj) =>
            {
                if (log.detail == BillValidatorLogDetail.CashboxRemoved)
                {
                    CashboxRemoved?.Invoke(this);
                }
                else if (log.detail == BillValidatorLogDetail.CashboxReplaced)
                {
                    CashboxReplaced?.Invoke(this);
                }
                else if (log.detail == BillValidatorLogDetail.StackerFull)
                {
                    CashboxFull?.Invoke(this);
                }
                else if (log.detail == BillValidatorLogDetail.NoteRejecting)
                {
                    NoteRejected?.Invoke(this);
                }
                else if (log.detail == BillValidatorLogDetail.UnsafeNoteJam)
                {
                    BillJam?.Invoke(this);
                }
                else if (log.detail == BillValidatorLogDetail.SafeNoteJam)
                {
                    BillJam?.Invoke(this);
                }
                logs.Enqueue(log);
            });

            // Initialization of channel amounts
            pchannelamounts = new Dictionary<int, int>();
            // Initialization of channel currencies
            pchannelcurrencies = new Dictionary<int, string>();
            // Initializatoin of enabled channels
            penabledChannels = new Dictionary<int, bool>();


        }


        /// <summary>
        /// Start Bill Acceptor Controller;
        /// 
        /// </summary>
        public void StartBillAccCTL()
        {
            // bill validator config 
            billvalidator.CommandStructure.ComPort = "COM16";
            billvalidator.CommandStructure.SSPAddress = 0;
            billvalidator.CommandStructure.Timeout = 3000;
            // Polling timer config
            pollingTimer = new System.Timers.Timer();
            pollingTimer.Interval = pollTimer;
            pollingTimer.Elapsed += pollingTimer_Tick;
            // Reconnection timer config
            reconnectionTimer = new System.Timers.Timer();
            reconnectionTimer.Interval = reconnectionInterval;
            reconnectionTimer.Elapsed += reconnectionTimer_Tick;

            // connect to the validator
            if (ConnectToValidator())
            {
                Running = true;
                pollingTimer.Start();

            }
        }

        /// <summary>
        /// Stop Bill Acceptor Contoller;
        /// </summary>
        public void StopBillAcc()
        {
            Running = false;
            pollingTimer.Stop();
            billvalidator.SSPComms.CloseComPort();

        }
     
        // This function opens the com port and attempts to connect with the validator. It then negotiates
        // the keys for encryption and performs some other setup commands.
        private bool ConnectToValidator()
        {
            // setup the timer
            reconnectionTimer.Interval = reconnectionInterval * 1000; // for ms

            // run for number of attempts specified
            for (int i = 0; i < reconnectionAttempts; i++)
            {
                // reset timer
                reconnectionTimer.Enabled = true;

                // close com port in case it was open
                billvalidator.SSPComms.CloseComPort();

                // turn encryption off for first stage
                billvalidator.CommandStructure.EncryptionStatus = false;

                // open com port and negotiate keys
                if (billvalidator.OpenComPort() && billvalidator.NegotiateKeys())
                {
                    billvalidator.CommandStructure.EncryptionStatus = true; // now encrypting
                    // find the max protocol version this validator supports
                    byte maxPVersion = FindMaxProtocolVersion();
                    if (maxPVersion > 6)
                    {
                        billvalidator.SetProtocolVersion(maxPVersion);
                    }
                    else
                    {
                        return false;
                    }
                    // get info from the validator and store useful vars
                    ValidatorSettingResponse resp = billvalidator.ValidatorSetupRequest();
                    pBillValidatorFirmware = resp.Firmware;
                    foreach (ChannelConfig conf in resp.Channels)
                    {
                        if (!pchannelamounts.Keys.Contains(conf.channelNumber))
                            pchannelamounts.Add(conf.channelNumber, -1);
                        if (!pchannelcurrencies.Keys.Contains(conf.channelNumber))
                            pchannelcurrencies.Add(conf.channelNumber, "-");
                        if (!penabledChannels.Keys.Contains(conf.channelNumber))
                            penabledChannels.Add(conf.channelNumber, false);
                        pchannelamounts[conf.channelNumber] = conf.billValue;
                        pchannelcurrencies[conf.channelNumber] = conf.currency;
                        penabledChannels[conf.channelNumber] = true; // Inhibit set to true by default
                    }
                    // Get Serial number
                    pSerialNumber = billvalidator.GetSerialNumber();
                    // check this unit is supported by this program
                    if (!IsUnitTypeSupported(billvalidator.UnitType))
                    {
                        return false;
                    }
                    // inhibits, this sets which channels can receive notes
                    billvalidator.SetInhibits();
                    // enable, this allows the validator to receive and act on commands
                    billvalidator.EnableValidator();

                    return true;
                }
                while (reconnectionTimer.Enabled) { } // wait for reconnectionTimer to tick
            }
            return false;
        }

        // This function finds the maximum protocol version that a validator supports. To do this
        // it attempts to set a protocol version starting at 6 in this case, and then increments the
        // version until error 0xF8 is returned from the validator which indicates that it has failed
        // to set it. The function then returns the version number one less than the failed version.
        private byte FindMaxProtocolVersion()
        {
            // not dealing with protocol under level 6
            // attempt to set in validator
            byte b = 0x06;
            while (true)
            {
                billvalidator.SetProtocolVersion(b);
                if (billvalidator.CommandStructure.ResponseData[0] == CCommands.SSP_RESPONSE_FAIL)
                    return --b;
                b++;
                if (b > 20)
                    return 0x06; // return default if protocol 'runs away'
            }
        }

        // This function checks whether the type of validator is supported by this program. This program only
        // supports Note Validators so any other type should be rejected.
        private bool IsUnitTypeSupported(char type)
        {
            if (type == (char)0x00)
                return true;
            return false;
        }

        private void pollingTimer_Tick(object sender, EventArgs e)
        {
            pollingTimer.Enabled = false;

            // if the poll fails, try to reconnect
            if (!billvalidator.DoPoll())
            {
                ConnectToValidator();
            }

            pollingTimer.Enabled = true;
        }

        private void reconnectionTimer_Tick(object sender, EventArgs e)
        {
            if (sender is System.Timers.Timer)
            {
                System.Timers.Timer t = sender as System.Timers.Timer;
                t.Enabled = false;
            }
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
            billvalidator.EnableValidator();
        }

        void IBillAccCTL.DisableBillAcceptor()
        {
            billvalidator.DisableValidator();
        }


        int getsasbill(int byte_index, int bit_index)
        {
            switch ($"{byte_index}-{bit_index}")
            {
                case "0-0":
                    return 1;
                case "0-1":
                    return 2;
                case "0-2":
                    return 5;
                case "0-3":
                    return 10;
                case "0-4":
                    return 20;
                case "0-5":
                    return 25;
                case "0-6":
                    return 50;
                case "0-7":
                    return 100;
                case "1-0":
                    return 200;
                case "1-1":
                    return 250;
                case "1-2":
                    return 500;
                case "1-3":
                    return 1000;
                case "1-4":
                    return 2000;
                case "1-5":
                    return 2500;
                case "1-6":
                    return 5000;
                case "1-7":
                    return 10000;
                case "2-0":
                    return 20000;
                case "2-1":
                    return 25000;
                case "2-2":
                    return 50000;
                case "2-3":
                    return 100000;
                case "2-4":
                    return 200000;
                case "2-5":
                    return 250000;
                case "2-6":
                    return 500000;
                case "2-7":
                    return 1000000;
                default:
                    return 0;
            }
        }

        byte calculate_channel_bit_value(int for_i, int index, bool inhibit)
        {
            if (for_i != index)
                return (byte)(Math.Pow(2, index));
            else
            {
                if (inhibit)
                    return 0;
                else
                    return (byte)(Math.Pow(2, index));
            }
        }

        void set_inhibits_from_sas_bytes(byte[] bill_denominations)
        {
            byte lsb = 0xFF, snd_byte = 0xFF, thrd_byte = 0xFF;

            if (bill_denominations.Length >= 3)
            {
                for (int index = 0; index < bill_denominations.Length; index++)
                {
                    byte current_byte = bill_denominations[index];
                    int bill_value = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        bool inhibit = (current_byte & (byte)(Math.Pow(2, i))) == 0;
                        bill_value = getsasbill(index, i);
                        int channel_n = pchannelamounts.Where(c => c.Value == bill_value).FirstOrDefault().Key;
                        if (penabledChannels.Keys.Contains(channel_n))
                        {
                            penabledChannels[channel_n] = !inhibit;
                        }
                        if (channel_n >= 1 && channel_n <= 8)
                        {
                            //lsb
                            lsb = (byte)(lsb & (calculate_channel_bit_value(channel_n - 1, 0, inhibit) |
                                        calculate_channel_bit_value(channel_n - 1, 1, inhibit) |
                                        calculate_channel_bit_value(channel_n - 1, 2, inhibit) |
                                        calculate_channel_bit_value(channel_n - 1, 3, inhibit) |
                                        calculate_channel_bit_value(channel_n - 1, 4, inhibit) |
                                        calculate_channel_bit_value(channel_n - 1, 5, inhibit) |
                                        calculate_channel_bit_value(channel_n - 1, 6, inhibit) |
                                        calculate_channel_bit_value(channel_n - 1, 7, inhibit)));
                        }
                        else if (channel_n >= 9 && channel_n <= 16)
                        {
                            // snd_byte
                            snd_byte = (byte)(snd_byte & (calculate_channel_bit_value(channel_n - 1, 0, inhibit) |
                                        calculate_channel_bit_value(channel_n - 1, 1, inhibit) |
                                        calculate_channel_bit_value(channel_n - 1, 2, inhibit) |
                                        calculate_channel_bit_value(channel_n - 1, 3, inhibit) |
                                        calculate_channel_bit_value(channel_n - 1, 4, inhibit) |
                                        calculate_channel_bit_value(channel_n - 1, 5, inhibit) |
                                        calculate_channel_bit_value(channel_n - 1, 6, inhibit) |
                                        calculate_channel_bit_value(channel_n - 1, 7, inhibit)));
                        }
                        else if (channel_n >= 17 && channel_n <= 24)
                        {
                            // thrd_byte
                            thrd_byte = (byte)(thrd_byte & (calculate_channel_bit_value(channel_n - 1, 0, inhibit) |
                                        calculate_channel_bit_value(channel_n - 1, 1, inhibit) |
                                        calculate_channel_bit_value(channel_n - 1, 2, inhibit) |
                                        calculate_channel_bit_value(channel_n - 1, 3, inhibit) |
                                        calculate_channel_bit_value(channel_n - 1, 4, inhibit) |
                                        calculate_channel_bit_value(channel_n - 1, 5, inhibit) |
                                        calculate_channel_bit_value(channel_n - 1, 6, inhibit) |
                                        calculate_channel_bit_value(channel_n - 1, 7, inhibit)));
                        }
                    }
                }
            }
            billvalidator.SetInhibits(lsb, snd_byte, thrd_byte);
        }
        void IBillAccCTL.ConfigBillDenominations(byte lsb, byte snd_byte, byte thrd_byte)
        {
            throw new NotImplementedException();
        }


    }
}
