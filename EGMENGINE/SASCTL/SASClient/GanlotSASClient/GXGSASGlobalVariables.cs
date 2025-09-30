using System;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices; //for import c++ dll


using GLU64 = System.UInt64;
using GLU32 = System.UInt32;
using GLU16 = System.UInt16;
using GLU8 = System.Byte;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace EGMENGINE.SASCTLModule.SASClient.GanlotSASClient.GXGSASAPIWrap
{
    internal static class GXGSASGlobalVariables
    {
        internal static int[] g_atIsGetROMSigReqCnt = new int[3] { 0, 0, 0 }; // The count for ROM signature request from host
        internal static GLU16[] g_wROMSigSeed = new GLU16[3] { 0, 0, 0 };                  // The seed for ROM signature process

    }


}

