using System;
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
using ITLlib;
using static System.Net.Mime.MediaTypeNames;
using static EGMENGINE.BillAccCTL.Impl.SSPBillAccCTL.ITLValidator.CValidator;

namespace EGMENGINE.BillAccCTLModule.Impl.SSPBillAccServiceCTL.MessageTypes
{

    internal class BillValidatorInfo
    {
        public string BillValidatorFirmware = "";
        public string SerialNumber = "";
        public Dictionary<int, int> channelamounts = new Dictionary<int, int>();
        public Dictionary<int, string> channelcurrencies = new Dictionary<int, string>();
        public Dictionary<int, bool> enabledChannels = new Dictionary<int, bool>();
    }

    internal class Event
    {
        public string EventName;
        public object data;
    }
    internal class BillDenom
    {
        public int denom;
    }

}
