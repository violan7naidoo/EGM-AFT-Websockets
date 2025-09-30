using System;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;

namespace EGMENGINE.BillAccCTLModule.BillAccTypes
{

    internal delegate void CTLBillAcceptedHandler(int denom, Object sender);

    internal delegate void CTLBillRejectedHandler(int denom, Object sender);

    internal delegate void CTLBillAcceptorAlertEventHandler(string txt, Object sender);

    internal delegate void CTLBillAcceptorSingleEventHandler(Object sender);
   

}