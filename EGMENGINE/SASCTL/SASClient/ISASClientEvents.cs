using System;
using System.Collections.Generic;
using System.Text;

namespace EGMENGINE.SASCTLModule.SASClient
{
    internal delegate byte VirtualEGMGetAddressHandler(EventArgs e);

    internal delegate void VirtualEGM01Handler(byte address, EventArgs e);
    internal delegate void VirtualEGM02Handler(byte address, EventArgs e);
    internal delegate void VirtualEGM03Handler(byte address, EventArgs e);
    internal delegate void VirtualEGM04Handler(byte address, EventArgs e);
    internal delegate void VirtualEGM05Handler(byte address, EventArgs e);
    internal delegate void VirtualEGM06Handler(byte address, EventArgs e);
    internal delegate void VirtualEGM07Handler(byte address, EventArgs e);
    internal delegate void VirtualEGM08Handler(byte address, byte[] billDenominations, byte billAcceptor, EventArgs e);
    internal delegate void VirtualEGM09Handler(byte address, byte[] gameNumber, byte EnableDisable, EventArgs e);
    internal delegate void VirtualEGM0AHandler(byte address, EventArgs e);
    internal delegate void VirtualEGM0BHandler(byte address, EventArgs e);
    internal delegate void VirtualEGM0EHandler(byte status, EventArgs e);
    internal delegate void VirtualEGMLP72Handler(byte address, byte transferCode,
                                                             byte transactionIndex,
                                                             byte transferType,
                                                             ulong cashableAmount,
                                                             ulong restrictedAmount,
                                                             ulong nonRestrictedAmount,
                                                             byte tranferFlags,
                                                             byte[] assetNumber,
                                                             byte[] registrationKey,
                                                             string transactionID,
                                                             uint expiration,
                                                             ushort poolID,
                                                             byte[] receiptData,
                                                             byte[] lockTimeout, EventArgs ee);
    internal delegate void VirtualEGMLP72IntHandler(byte address, byte transferCode,
                                                                byte transactionIndex, EventArgs ee);
    internal delegate void VirtualEGM73Handler(byte address, byte RegistrationCode, EventArgs e);
    internal delegate void VirtualEGM75Handler(byte address, string location,
                                                           string address1,
                                                           string address2,
                                                           string inHouseLine1,
                                                           string inHouseLine2,
                                                           string inHouseLine3,
                                                           string inHouseLine4,
                                                           string debitLine1,
                                                           string debitLine2,
                                                           string debitLine3,
                                                           string debitLine4, EventArgs e);
    internal delegate void VirtualEGM7FHandler(byte address, DateTime dateTime, EventArgs e);
    internal delegate void VirtualEGM8BHandler(byte address, byte[] minimumwin, byte[] maximunwin, byte multiplier_taxstatus, byte enable_disable, byte wagerType, EventArgs e);
    internal delegate void VirtualEGM94Handler(byte address, EventArgs e);
    internal delegate void VirtualEGMA8Handler(byte address, byte resetMethod, EventArgs e);
    internal delegate void VirtualEGMAAHandler(byte address, byte EnableDisable, EventArgs e);
    internal delegate void CommandSentHandler(string cmd, bool crc, bool retry, EventArgs e);
    internal delegate void CommandReceivedHandler(string cmd, bool crc, EventArgs e);
    internal delegate void SmibLinkDownHandler(bool truth, EventArgs e);
    internal delegate void ClientCriticalErrorHandler(string error, EventArgs e);
    internal delegate void ClientNoErrorHandler(string error, EventArgs e);
    internal delegate void SASLinkDownHandler(bool linkdown, EventArgs e);

    internal delegate void VirtualEGM74Handler(byte address, byte lockStatus, EventArgs e);
    internal delegate void VirtualEGMLP74IntHandler(byte address, byte transferCode,
                                                                byte transactionIndex, EventArgs ee);


}
