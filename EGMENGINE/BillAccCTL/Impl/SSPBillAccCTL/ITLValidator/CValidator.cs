using EGMENGINE.BillAccCTL.Impl.SSPBillAccCTL.ITLValidator.Helpers;
using EGMENGINE.BillAccCTL.Impl.SSPBillAccCTL.ITLValidator.SSP;
using EGMENGINE.BillAccCTLModule;
using EGMENGINE.BillAccCTLModule.BillAccTypes;
using EGMENGINE.GLOBALTYPES;
using ITLlib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace EGMENGINE.BillAccCTL.Impl.SSPBillAccCTL.ITLValidator
{
    internal class CValidator
    {
        public event ValidatorBillEvent Validator_Bill_Inserted;
        public event ValidatorBillEvent Validator_Bill_Accepted;

        public delegate void ValidatorBillEvent(int billNum, Object sender);

        public event ValidatorLogEvent LogEvent;

        public delegate void ValidatorLogEvent(BillValidatorLog log, Object sender);



        // ssp library variables
        SSPComms m_eSSP;
        SSP_COMMAND m_cmd;
        SSP_KEYS keys;
        SSP_FULL_KEY sspKey;
        SSP_COMMAND_INFO info;

        // variable declarations


        // The protocol version this validator is using, set in setup request
        int m_ProtocolVersion;

        // A variable to hold the type of validator, this variable is initialised using the setup request command
        char m_UnitType;

        // Two variables to hold the number of notes accepted by the validator and the value of those
        // notes when added up
        int m_NumStackedNotes;

        // Variable to hold the number of channels in the validator dataset
        int m_NumberOfChannels;

        // The multiplier by which the channel values are multiplied to give their
        // real penny value. E.g. £5.00 on channel 1, the value would be 5 and the multiplier
        // 100.
        int m_ValueMultiplier;

        //Integer to hold total number of Hold messages to be issued before releasing note from escrow
        int m_HoldNumber;

        //Integer to hold number of hold messages still to be issued
        int m_HoldCount;

        //Bool to hold flag set to true if a note is being held in escrow
        bool m_NoteHeld;

        // A list of dataset data, sorted by value. Holds the info on channel number, value, currency,
        // level and whether it is being recycled.
        List<ChannelData> m_UnitDataList;

        // constructor
        public CValidator()
        {
            m_eSSP = new SSPComms();
            m_cmd = new SSP_COMMAND();
            keys = new SSP_KEYS();
            sspKey = new SSP_FULL_KEY();
            info = new SSP_COMMAND_INFO();

            m_NumberOfChannels = 0;
            m_ValueMultiplier = 1;
            m_UnitType = (char)0xFF;
            m_UnitDataList = new List<ChannelData>();
            m_HoldCount = 0;
            m_HoldNumber = 0;
        }

        /* Variable Access */

        // access to ssp variables
        // the pointer which gives access to library functions such as open com port, send command etc
        public SSPComms SSPComms
        {
            get { return m_eSSP; }
            set { m_eSSP = value; }
        }

        // a pointer to the command structure, this struct is filled with info and then compiled into
        // a packet by the library and sent to the validator
        public SSP_COMMAND CommandStructure
        {
            get { return m_cmd; }
            set { m_cmd = value; }
        }

        // pointer to an information structure which accompanies the command structure
        public SSP_COMMAND_INFO InfoStructure
        {
            get { return info; }
            set { info = value; }
        }

        // access to the type of unit, this will only be valid after the setup request
        public char UnitType
        {
            get { return m_UnitType; }
        }

        // access to number of channels being used by the validator
        public int NumberOfChannels
        {
            get { return m_NumberOfChannels; }
            set { m_NumberOfChannels = value; }
        }

        // access to number of notes stacked
        public int NumberOfNotesStacked
        {
            get { return m_NumStackedNotes; }
            set { m_NumStackedNotes = value; }
        }

        // access to value multiplier
        public int Multiplier
        {
            get { return m_ValueMultiplier; }
            set { m_ValueMultiplier = value; }
        }
        // acccess to hold number
        public int HoldNumber
        {
            get { return m_HoldNumber; }
            set { m_HoldNumber = value; }

        }
        //Access to flag showing note is held in escrow
        public bool NoteHeld
        {
            get { return m_NoteHeld; }
        }
        // get a channel value
        public int GetChannelValue(int channelNum)
        {
            if (channelNum >= 1 && channelNum <= m_NumberOfChannels)
            {
                foreach (ChannelData d in m_UnitDataList)
                {
                    if (d.Channel == channelNum)
                        return d.Value;
                }
            }
            return -1;
        }

        // get a channel currency
        public string GetChannelCurrency(int channelNum)
        {
            if (channelNum >= 1 && channelNum <= m_NumberOfChannels)
            {
                foreach (ChannelData d in m_UnitDataList)
                {
                    if (d.Channel == channelNum)
                        return new string(d.Currency);
                }
            }
            return "";
        }

        /* Command functions */

        // The enable command allows the validator to receive and act on commands sent to it.
        public void EnableValidator()
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_ENABLE;
            m_cmd.CommandDataLength = 1;

            if (!SendCommand()) return;
            // check response
            if (CheckGenericResponses()) { }
        }

        // Disable command stops the validator from acting on commands.
        public void DisableValidator()
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_DISABLE;
            m_cmd.CommandDataLength = 1;

            if (!SendCommand()) return;
            // check response
            if (CheckGenericResponses())
            { }
        }
        // Return Note command returns note held in escrow to bezel. 
        public void ReturnNote()
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_REJECT_BANKNOTE;
            m_cmd.CommandDataLength = 1;
            if (!SendCommand()) return;

            if (CheckGenericResponses())
            {
                m_HoldCount = 0;
            }
        }
        // The reset command instructs the validator to restart (same effect as switching on and off)
        public void Reset()
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_RESET;
            m_cmd.CommandDataLength = 1;
            if (!SendCommand()) return;

            if (CheckGenericResponses()) { }
                //.AppendText("Resetting unit\r\n");
        }

        // This command just sends a sync command to the validator
        public bool SendSync()
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_SYNC;
            m_cmd.CommandDataLength = 1;
            if (!SendCommand()) return false;

            if (CheckGenericResponses()) { }
                //.AppendText("Successfully sent sync\r\n");
            return true;
        }

        // This function sets the protocol version in the validator to the version passed across. Whoever calls
        // this needs to check the response to make sure the version is supported.
        public void SetProtocolVersion(byte pVersion)
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_HOST_PROTOCOL_VERSION;
            m_cmd.CommandData[1] = pVersion;
            m_cmd.CommandDataLength = 2;
            if (!SendCommand()) return;
        }

        // This function sends the command LAST REJECT CODE which gives info about why a note has been rejected. It then
        // outputs the info to a passed across textbox.
        public void QueryRejection()
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_LAST_REJECT_CODE;
            m_cmd.CommandDataLength = 1;
            if (!SendCommand()) return;

            if (CheckGenericResponses())
            {
              
            }
        }

        // This function performs a number of commands in order to setup the encryption between the host and the validator.
        public bool NegotiateKeys()
        {
            // make sure encryption is off
            m_cmd.EncryptionStatus = false;

            // send sync
            m_cmd.CommandData[0] = CCommands.SSP_CMD_SYNC;
            m_cmd.CommandDataLength = 1;

            if (!SendCommand()) return false;

            m_eSSP.InitiateSSPHostKeys(keys, m_cmd);

            // send generator
            m_cmd.CommandData[0] = CCommands.SSP_CMD_SET_GENERATOR;
            m_cmd.CommandDataLength = 9;

            // Convert generator to bytes and add to command data.
            BitConverter.GetBytes(keys.Generator).CopyTo(m_cmd.CommandData, 1);

            if (!SendCommand()) return false;

            // send modulus
            m_cmd.CommandData[0] = CCommands.SSP_CMD_SET_MODULUS;
            m_cmd.CommandDataLength = 9;

            // Convert modulus to bytes and add to command data.
            BitConverter.GetBytes(keys.Modulus).CopyTo(m_cmd.CommandData, 1);

            if (!SendCommand()) return false;

            // send key exchange
            m_cmd.CommandData[0] = CCommands.SSP_CMD_REQUEST_KEY_EXCHANGE;
            m_cmd.CommandDataLength = 9;

            // Convert host intermediate key to bytes and add to command data.
            BitConverter.GetBytes(keys.HostInter).CopyTo(m_cmd.CommandData, 1);


            if (!SendCommand()) return false;

            // Read slave intermediate key.
            keys.SlaveInterKey = BitConverter.ToUInt64(m_cmd.ResponseData, 1);

            m_eSSP.CreateSSPHostEncryptionKey(keys);

            // get full encryption key
            m_cmd.Key.FixedKey = 0x0123456701234567;
            m_cmd.Key.VariableKey = keys.KeyHost;


            return true;
        }

        public class ChannelConfig
        {
            public int channelNumber;
            public int billValue;
            public string currency;

            public ChannelConfig()
            {
                channelNumber = -1;
                billValue = 0;
                currency = "";
            }
        }
        public class ValidatorSettingResponse
        {
            public string UnitType;
            public string Firmware;
            public int NumberOfChannels;
            public int ValueMultiplier;
            public int ProtocolVersion;
            public ChannelConfig[] Channels;
            public ValidatorSettingResponse()
            {
                UnitType = "Unknown Type";
                Firmware = "Unknown Firmware";
                NumberOfChannels = 0;
                ValueMultiplier = 0;
                ProtocolVersion = 0;
                Channels = new ChannelConfig[] { };
            }
        }
        // This function uses the setup request command to get all the information about the validator.
        public ValidatorSettingResponse ValidatorSetupRequest()
        {
            StringBuilder sbDisplay = new StringBuilder(1000);
            ValidatorSettingResponse resp = new ValidatorSettingResponse();
            // send setup request
            m_cmd.CommandData[0] = CCommands.SSP_CMD_SETUP_REQUEST;
            m_cmd.CommandDataLength = 1;

            if (!SendCommand()) return resp;

            // display setup request


            // unit type
            int index = 1;
            m_UnitType = (char)m_cmd.ResponseData[index++];
            switch (m_UnitType)
            {
                case (char)0x00: resp.UnitType = "Validator"; break;
                case (char)0x03: resp.UnitType = "SMART Hopper"; break;
                case (char)0x06: resp.UnitType = "SMART Payout"; break;
                case (char)0x07: resp.UnitType = "NV11"; break;
                case (char)0x0D: resp.UnitType = "TEBS"; break;
                default: break;
            }

            // firmware
            char a = (char)m_cmd.ResponseData[index++];
            char b = (char)m_cmd.ResponseData[index++];
            char c = (char)m_cmd.ResponseData[index++];
            char d = (char)m_cmd.ResponseData[index++];
            resp.Firmware = $"{a}{b}.{c}{d}";

            // country code.
            // legacy code so skip it.
            index += 3;

            // value multiplier.
            // legacy code so skip it.
            index += 3;

            // Number of channels
            m_NumberOfChannels = m_cmd.ResponseData[index++];
            resp.NumberOfChannels = m_NumberOfChannels;

            // channel values.
            // legacy code so skip it.
            index += m_NumberOfChannels; // Skip channel values

            // channel security
            // legacy code so skip it.
            index += m_NumberOfChannels;

            // real value multiplier
            // (big endian)
            m_ValueMultiplier = m_cmd.ResponseData[index + 2];
            m_ValueMultiplier += m_cmd.ResponseData[index + 1] << 8;
            m_ValueMultiplier += m_cmd.ResponseData[index] << 16;
            resp.ValueMultiplier = m_ValueMultiplier;
            index += 3;


            // protocol version
            m_ProtocolVersion = m_cmd.ResponseData[index++];
            resp.ProtocolVersion = m_ProtocolVersion;

            // Add channel data to list then display.
            // Clear list.
            m_UnitDataList.Clear();
            // Loop through all channels.

            for (byte i = 0; i < m_NumberOfChannels; i++)
            {
                ChannelData loopChannelData = new ChannelData();
                // Channel number.
                loopChannelData.Channel = (byte)(i + 1);

                // Channel value.
                loopChannelData.Value = BitConverter.ToInt32(m_cmd.ResponseData, index + (m_NumberOfChannels * 3) + (i * 4)) * m_ValueMultiplier;

                // Channel Currency
                loopChannelData.Currency[0] = (char)m_cmd.ResponseData[index + (i * 3)];
                loopChannelData.Currency[1] = (char)m_cmd.ResponseData[(index + 1) + (i * 3)];
                loopChannelData.Currency[2] = (char)m_cmd.ResponseData[(index + 2) + (i * 3)];

                // Channel level.
                loopChannelData.Level = 0;

                // Channel recycling
                loopChannelData.Recycling = false;

                // Add data to list.
                m_UnitDataList.Add(loopChannelData);

                ChannelConfig chnconf = new ChannelConfig();
                chnconf.channelNumber = loopChannelData.Channel;
                chnconf.billValue = loopChannelData.Value / m_ValueMultiplier;
                chnconf.currency = $"{loopChannelData.Currency[0]}{loopChannelData.Currency[1]}{loopChannelData.Currency[2]}";
                resp.Channels = resp.Channels.Append(chnconf).ToArray();
            }

            
            // Sort the list by .Value.
            m_UnitDataList.Sort((d1, d2) => d1.Value.CompareTo(d2.Value));

            return resp;

        }

        // This function sends the set inhibits command to set the inhibits on the validator. An additional two
        // bytes are sent along with the command byte to indicate the status of the inhibits on the channels.
        // For example 0xFF and 0xFF in binary is 11111111 11111111. This indicates all 16 channels supported by
        // the validator are uninhibited. If a user wants to inhibit channels 8-16, they would send 0x00 and 0xFF.
        public void SetInhibits()
        {
            // set inhibits
            m_cmd.CommandData[0] = CCommands.SSP_CMD_SET_CHANNEL_INHIBITS;
            m_cmd.CommandData[1] = 0xFF;
            m_cmd.CommandData[2] = 0xFF;
            m_cmd.CommandDataLength = 3;

            if (!SendCommand()) return;
            if (CheckGenericResponses())
            {
            }
        }

        // This function sends the set inhibits command to set the inhibits on the validator. An additional two
        // bytes are sent along with the command byte to indicate the status of the inhibits on the channels.
        // For example 0xFF and 0xFF in binary is 11111111 11111111. This indicates all 16 channels supported by
        // the validator are uninhibited. If a user wants to inhibit channels 8-16, they would send 0x00 and 0xFF.
        public void SetInhibits(byte lsb, byte secondbyte, byte thirdbyte)
        {
            // set inhibits
            m_cmd.CommandData[0] = CCommands.SSP_CMD_SET_CHANNEL_INHIBITS;
            m_cmd.CommandData[1] = lsb;
            m_cmd.CommandData[2] = secondbyte;
            m_cmd.CommandData[3] = thirdbyte;
            m_cmd.CommandDataLength = 4;

            if (!SendCommand()) return;
            if (CheckGenericResponses())
            {
            }
        }

        // This function gets the serial number of the device.  An optional Device parameter can be used
        // for TEBS systems to specify which device's serial number should be returned.
        // 0x00 = NV200
        // 0x01 = SMART Payout
        // 0x02 = Tamper Evident Cash Box.
        public string GetSerialNumber(byte Device)
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_GET_SERIAL_NUMBER;
            m_cmd.CommandData[1] = Device;
            m_cmd.CommandDataLength = 2;


            if (!SendCommand()) return "-1";
            if (CheckGenericResponses())
            {
                // Response data is big endian, so reverse bytes 1 to 4.
                Array.Reverse(m_cmd.ResponseData, 1, 4);
                return BitConverter.ToUInt32(m_cmd.ResponseData, 1).ToString();
            }
            else
            {
                return "-1";
            }
        }

        public string GetSerialNumber()
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_GET_SERIAL_NUMBER;
            m_cmd.CommandDataLength = 1;

            if (!SendCommand()) return "-1";
            if (CheckGenericResponses())
            {
                // Response data is big endian, so reverse bytes 1 to 4.
                Array.Reverse(m_cmd.ResponseData, 1, 4);
                return BitConverter.ToUInt32(m_cmd.ResponseData, 1).ToString();
            }
            else
                return "-1";
        }
        // The poll function is called repeatedly to poll to validator for information, it returns as
        // a response in the command structure what events are currently happening.
        public bool DoPoll()
        {
            byte i;
            // If a not is to be held in escrow, send hold commands, as poll releases note.
            if (m_HoldCount > 0)
            {
                m_NoteHeld = true;
                m_HoldCount--;
                m_cmd.CommandData[0] = CCommands.SSP_CMD_HOLD;
                m_cmd.CommandDataLength = 1;
                if (!SendCommand()) return false;
                return true;

            }
            //send poll
            m_cmd.CommandData[0] = CCommands.SSP_CMD_POLL;
            m_cmd.CommandDataLength = 1;
            m_NoteHeld = false;

            if (!SendCommand()) return false;

            //parse poll response
            int noteVal = 0;
            for (i = 1; i < m_cmd.ResponseDataLength; i++)
            {
                switch (m_cmd.ResponseData[i])
                {
                    // This response indicates that the unit was reset and this is the first time a poll
                    // has been called since the reset.
                    case CCommands.SSP_POLL_SLAVE_RESET:
                        LogEvent(new BillValidatorLog(DateTime.Now, BillValidatorLogDetail.SlaveReset), this);
                        break;
                    // A note is currently being read by the validator sensors. The second byte of this response
                    // is zero until the note's type has been determined, it then changes to the channel of the 
                    // scanned note.
                    case CCommands.SSP_POLL_READ_NOTE:
                        LogEvent(new BillValidatorLog(DateTime.Now, BillValidatorLogDetail.ReadNote), this);
                        if (m_cmd.ResponseData[i + 1] > 0)
                        {
                            noteVal = GetChannelValue(m_cmd.ResponseData[i + 1]);
                            m_HoldCount = m_HoldNumber;
                            Validator_Bill_Inserted(noteVal / m_ValueMultiplier, this);
                        }
                        else
                        i++;
                        break;
                    // A credit event has been detected, this is when the validator has accepted a note as legal currency.
                    case CCommands.SSP_POLL_CREDIT_NOTE:
                        LogEvent(new BillValidatorLog(DateTime.Now, BillValidatorLogDetail.CreditNote), this);
                        noteVal = GetChannelValue(m_cmd.ResponseData[i + 1]);
                        m_NumStackedNotes++;
                        i++;
                        Validator_Bill_Accepted(noteVal / m_ValueMultiplier, this);
                        break;
                    // A note is being rejected from the validator. This will carry on polling while the note is in transit.
                    case CCommands.SSP_POLL_NOTE_REJECTING:
                        LogEvent(new BillValidatorLog(DateTime.Now, BillValidatorLogDetail.NoteRejecting), this);
                        break;
                    // A note has been rejected from the validator, the note will be resting in the bezel. This response only
                    // appears once.
                    case CCommands.SSP_POLL_NOTE_REJECTED:
                        LogEvent(new BillValidatorLog(DateTime.Now, BillValidatorLogDetail.NoteRejected), this);
                        QueryRejection();
                        break;
                    // A note is in transit to the cashbox.
                    case CCommands.SSP_POLL_NOTE_STACKING:
                        LogEvent(new BillValidatorLog(DateTime.Now, BillValidatorLogDetail.NoteStacking), this);
                        break;
                    // A note has reached the cashbox.
                    case CCommands.SSP_POLL_NOTE_STACKED:
                        LogEvent(new BillValidatorLog(DateTime.Now, BillValidatorLogDetail.NoteStacked), this);
                        break;
                    // A safe jam has been detected. This is where the user has inserted a note and the note
                    // is jammed somewhere that the user cannot reach.
                    case CCommands.SSP_POLL_SAFE_NOTE_JAM:
                        LogEvent(new BillValidatorLog(DateTime.Now, BillValidatorLogDetail.SafeNoteJam), this);
                        break;
                    // An unsafe jam has been detected. This is where a user has inserted a note and the note
                    // is jammed somewhere that the user can potentially recover the note from.
                    case CCommands.SSP_POLL_UNSAFE_NOTE_JAM:
                        LogEvent(new BillValidatorLog(DateTime.Now, BillValidatorLogDetail.UnsafeNoteJam), this);
                        break;
                    // The validator is disabled, it will not execute any commands or do any actions until enabled.
                    case CCommands.SSP_POLL_DISABLED:
                        break;
                    // A fraud attempt has been detected. The second byte indicates the channel of the note that a fraud
                    // has been attempted on.
                    case CCommands.SSP_POLL_FRAUD_ATTEMPT:
                        LogEvent(new BillValidatorLog(DateTime.Now, BillValidatorLogDetail.FraudAttempt), this);
                        i++;
                        break;
                    // The stacker (cashbox) is full. 
                    case CCommands.SSP_POLL_STACKER_FULL:
                        LogEvent(new BillValidatorLog(DateTime.Now, BillValidatorLogDetail.StackerFull), this);
                        break;
                    // A note was detected somewhere inside the validator on startup and was rejected from the front of the
                    // unit.
                    case CCommands.SSP_POLL_NOTE_CLEARED_FROM_FRONT:
                        LogEvent(new BillValidatorLog(DateTime.Now, BillValidatorLogDetail.NoteClearedFromFront), this);
                        i++;
                        break;
                    // A note was detected somewhere inside the validator on startup and was cleared into the cashbox.
                    case CCommands.SSP_POLL_NOTE_CLEARED_TO_CASHBOX:
                        LogEvent(new BillValidatorLog(DateTime.Now, BillValidatorLogDetail.NoteClearedToCashbox), this);
                        i++;
                        break;
                    // The cashbox has been removed from the unit. This will continue to poll until the cashbox is replaced.
                    case CCommands.SSP_POLL_CASHBOX_REMOVED:
                        LogEvent(new BillValidatorLog(DateTime.Now, BillValidatorLogDetail.CashboxRemoved), this);
                        break;
                    // The cashbox has been replaced, this will only display on a poll once.
                    case CCommands.SSP_POLL_CASHBOX_REPLACED:
                        LogEvent(new BillValidatorLog(DateTime.Now, BillValidatorLogDetail.CashboxReplaced), this);
                        break;
                    // A bar code ticket has been detected and validated. The ticket is in escrow, continuing to poll will accept
                    // the ticket, sending a reject command will reject the ticket.
                    //case CCommands.SSP_POLL_BAR_CODE_VALIDATED:
                    //    .AppendText("Bar code ticket validated\r\n");
                    //    break;
                    //// A bar code ticket has been accepted (equivalent to note credit).
                    //case CCommands.SSP_POLL_BAR_CODE_ACK:
                    //    .AppendText("Bar code ticket accepted\r\n");
                    //    break;
                    // The validator has detected its note path is open. The unit is disabled while the note path is open.
                    // Only available in protocol versions 6 and above.
                    case CCommands.SSP_POLL_NOTE_PATH_OPEN:
                        LogEvent(new BillValidatorLog(DateTime.Now, BillValidatorLogDetail.NotePathOpen), this);

                        break;
                    // All channels on the validator have been inhibited so the validator is disabled. Only available on protocol
                    // versions 7 and above.
                    case CCommands.SSP_POLL_CHANNEL_DISABLE:
                        LogEvent(new BillValidatorLog(DateTime.Now, BillValidatorLogDetail.ChannelDisable), this);
                        break;
                    default:
                        break;
                }
            }
            return true;
        }

        /* Non-Command functions */

        // This function calls the open com port function of the SSP library.
        public bool OpenComPort()
        {
            if (!m_eSSP.OpenSSPComPort(m_cmd))
            {
                return false;
            }
            return true;
        }

        /* Exception and Error Handling */

        // This is used for generic response error catching, it outputs the info in a
        // meaningful way.
        private bool CheckGenericResponses()
        {
            if (m_cmd.ResponseData[0] == CCommands.SSP_RESPONSE_OK)
                return true;
            else
            {

                    return false;
            }
        }

        public bool SendCommand()
        {
            // Backup data and length in case we need to retry
            byte[] backup = new byte[255];
            m_cmd.CommandData.CopyTo(backup, 0);
            byte length = m_cmd.CommandDataLength;

            // attempt to send the command
            if (m_eSSP.SSPSendCommand(m_cmd, info) == false)
            {
                m_eSSP.CloseComPort();
                return false;
            }

            return true;
        }
    };
}
