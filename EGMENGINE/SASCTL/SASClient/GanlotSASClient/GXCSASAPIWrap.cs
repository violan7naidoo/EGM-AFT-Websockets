using System;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices; //for import c++ dll

using GXGGFMAPI;

using GLU64 = System.UInt64;
using GLU32 = System.UInt32;
using GLU16 = System.UInt16;
using GLU8 = System.Byte;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace EGMENGINE.SASCTLModule.SASClient.GanlotSASClient.GXGSASAPIWrap
{



    internal static class GXGSASAPIConst
    {
        internal const int SAS_METER_SIZE = 8;
        internal const int NUM_SUP_SAS_PORT = 3;
        internal const int MAX_SUP_GAME_NUM = 16;
        internal const int SAS_EXCEPTION_QUEUE_SIZE = 40;
        internal const int SAS_VALID_RECORD_QUEUE_SIZE = 31;

        internal const int MAX_SAS_EXCEPTION_RTE_DATA_SIZE = 11;
        internal const int MAX_METER_CLUSTER_SIZE = 12;
        internal const int SAS_INTR_DATA_SIZE = 32;

        internal const GLU8 TRUE = 1;
        internal const GLU8 FALSE = 0;
    }


    #region structure
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct SAS_GAME_DETAIL
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 3)]
        internal String GameIDStr;        // Exact 2 bytes char string
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 4)]
        internal String AddiIDStr;        // Exact 3 bytes char string
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 21)]
        internal String GameNameStr;      // MAX 20 bytes char string
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 7)]
        internal String PayTableIDStr;    // Exact 6 bytes char string
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 21)]
        internal String PayTableNameStr;  // MAX 20 bytes char string
        internal GLU16 MaxBet;            // 2 bytes binary, MAX 9999
        internal GLU16 GameOption;        // 2 bytes binary
        internal GLU16 PayBackPerc;       // 2 bytes binary, MAX 9999
        internal GLU64 CashoutLimit;      // 8 bytes binary
        internal GLU16 WagerCatNum;       // 2 bytes binary, MAX 8
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SAS_EXCEPTION_INFO
    {
        internal GLU8 PortNum;
        internal GLU8 Type;
        internal GLU8 Code;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = GXGSASAPIConst.MAX_SAS_EXCEPTION_RTE_DATA_SIZE)]
        internal GLU8[] Data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SAS_METER_INFO
    {
        internal GLU8 Type;
        internal GLU8 Num;
        internal GLU8 Code;
        internal GLU64 Value;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SAS_METER_CLUSTER
    {
        internal GLU8 MeterNum;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = GXGSASAPIConst.MAX_METER_CLUSTER_SIZE)]
        internal GLU8[] Mode;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = GXGSASAPIConst.MAX_METER_CLUSTER_SIZE)]
        internal SAS_METER_INFO[] MeterInfo;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SAS_HANDPAY_INFO
    {
        internal GLU8 Type;
        internal GLU8 ProgressiveGroup;
        internal GLU8 Level;
        internal GLU64 Amount;
        internal GLU64 PartialPay;
        internal GLU8 ResetID;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct SAS_TICKET_DATA
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 41)]
        internal String TicketLocationStr;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 41)]
        internal String TicketAddress1Str;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 41)]
        internal String TicketAddress2Str;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 17)]
        internal String RestrictedTicketTitleStr;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 17)]
        internal String DebitTicketTitleStr;
        internal GLU32 CashoutTicketExpir;
        internal GLU32 RestrictedTicketExpir;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SAS_TICKET_RECORD
    {
        internal GLU8 TicketType;
        internal GLU16 TicketNum;	// 2 bytes binary
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        internal GLU32[] DateTime; // [0]: Year, [1]: Month, [2]: Date, [3]: Hour, [4]: Minute, [5]: Second
        internal GLU64 Amount;		// 8 bytes binary. Unit: Cents
        internal GLU32 Expir;		// 4 bytes binary
        internal GLU16 PoolID;		// 2 bytes binary
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct SAS_AFT_TRANS_DATA
    {
        internal GLU8 IsPartial;
        internal GLU8 TransType;
        internal GLU8 TransFlag;
        internal GLU64 CashAmount;
        internal GLU64 ResAmount;
        internal GLU64 NonresAmount;
        internal GLU32 Expir;
        internal GLU16 PoolID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 21)]
        internal String TransactionID;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct SAS_AFT_TRANS_RECEIPT_DATA
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 23)]
        internal String TransSrcDestStr;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 23)]
        internal String PatronNameStr;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 17)]
        internal String PatronAcctStr;
        internal GLU64 AcctBalance;
        internal GLU16 DebitCardNum;
        internal GLU64 TransFee;
        internal GLU64 TotalDebitAmount;
        internal GLU32 Date;
        internal GLU32 Time;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct SAS_AFT_RECEIPT_DATA
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 23)]
        internal String LocationStr;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 23)]
        internal String Address1Str;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 23)]
        internal String Address2Str;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 23)]
        internal String InHouseLine1Str;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 23)]
        internal String InHouseLine2Str;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 23)]
        internal String InHouseLine3Str;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 23)]
        internal String InHouseLine4Str;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 23)]
        internal String DebitLine1Str;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 23)]
        internal String DebitLine2Str;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 23)]
        internal String DebitLine3Str;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 23)]
        internal String DebitLine4Str;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SAS_PROG_HIT_DATA
    {
        internal GLU8 HitLevel;
        internal GLU64 Amount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SAS_INTR_DATA
    {
        internal GLU8 PortNum;
        internal GLU8 Status;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = GXGSASAPIConst.SAS_INTR_DATA_SIZE)]
        internal GLU8[] Data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SAS_STATUS_DATA
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        internal GLU8[] Data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SAS_FEATURE_CODE
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        internal GLU8[] Code;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SAS_DATE_TIME
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        internal GLU32[] DateTime;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct BCD_ARRAY
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        internal GLU8[] BCD;
    }
    #endregion

    internal delegate void INTR_FUNC_SAS(SAS_INTR_DATA data);

    internal class GXGSASAPIBridge
    {
        #region import definition
        /* API for Initial SAS Handler */
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_Init();
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_Release();
        [DllImport("GXGSASAPI.dll")]
        private static extern GLU32 GXG_SAS_GetAPIVersion(StringBuilder ver); // Need Wrap
        public static GLU32 GXG_SAS_GetAPIVersion(ref string ver)
        {
            GLU32 dwStatus = 0;
            StringBuilder sb = new StringBuilder(128);

            dwStatus = GXG_SAS_GetAPIVersion(sb);
            if (dwStatus == 0)
                ver = Convert.ToString(sb);
            return dwStatus;
        }

        [DllImport("GXGSASAPI.dll")]
        private static extern GLU32 GXG_SAS_GetDriverVersion(StringBuilder ver); // Need Wrap
        public static GLU32 GXG_SAS_GetDriverVersion(ref string ver)
        {
            GLU32 dwStatus = 0;
            StringBuilder sb = new StringBuilder(128);

            dwStatus = GXG_SAS_GetDriverVersion(sb);
            if (dwStatus == 0)
                ver = Convert.ToString(sb);
            return dwStatus;
        }

        [DllImport("GXGSASAPI.dll")]
        private static extern GLU32 GXG_SAS_GetFirmwareVersion(StringBuilder ver); // Need Wrap
        public static GLU32 GXG_SAS_GetFirmwareVersion(ref string ver)
        {
            GLU32 status = 0;
            StringBuilder sb = new StringBuilder(128);

            status = GXG_SAS_GetFirmwareVersion(sb);
            if (status == 0)
                ver = Convert.ToString(sb);
            return status;
        }
        [DllImport("GXGSASAPI.dll", CallingConvention = CallingConvention.Cdecl) ]
        private static extern GLU32 GXG_SAS_RegisterIntrCallBack(INTR_FUNC pFunc, IntPtr data); // Need Wrap
        public static GLU32 GXG_SAS_RegisterIntrCallBack(INTR_FUNC_SAS pFunc)
        {
            GLU32 dwStatus = 0;

            if (pFunc != null)
            {
                sasUserCallback = pFunc;
                sasIntrHandler = SASIntrBridgeHandler;
                dwStatus = GXG_SAS_RegisterIntrCallBack(sasIntrHandler, IntPtr.Zero);
            }

            return dwStatus;
        }

        /* API for SAS Control */
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetEnabled(GLU8 port_num, GLU8 enabled);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetLPEnabled(GLU8 lp_cmd, GLU8 enabled);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetEGMAddr(GLU8 port_num, GLU8 addr);

        [DllImport("GXGSASAPI.dll")]
        private static extern GLU32 GXG_SAS_GetStatus(GLU8 port_num, GLU8 type, ref GLU8 status, IntPtr status_data); // Need Wrap
        public static GLU32 GXG_SAS_GetStatus(GLU8 port_num, GLU8 type, ref GLU8 status, ref SAS_STATUS_DATA data)
        {
            GLU32 dwStatus = 0;
            IntPtr pData;

            pData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SAS_STATUS_DATA)));
            Marshal.StructureToPtr(data, pData, true);
            if (pData != IntPtr.Zero)
            {
                dwStatus = GXG_SAS_GetStatus(port_num, type, ref status, pData);
                if (status == 0)
                    data = (SAS_STATUS_DATA)Marshal.PtrToStructure(pData, typeof(SAS_STATUS_DATA));
                Marshal.FreeHGlobal(pData);
            }
            else
                dwStatus = (GLU32)ERROR_CODE.ERR_FAIL_ALLOC;
            return dwStatus;
        }
        public static GLU32 GXG_SAS_GetStatus(GLU8 port_num, GLU8 type, ref GLU8 status)
        {
            return GXG_SAS_GetStatus(port_num, type, ref status, IntPtr.Zero);
        }

        /* API for SAS Basic Configuration */
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_ResetDefault();
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetGameInfo(GLU8 total_game_num, GLU32 enabled_game_mask);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetGameInfo(ref GLU8 total_game_num, ref GLU32 enabled_game_mask);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetCurrentSelectGame(GLU8 game_num);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetCurrentSelectGame(ref GLU8 game_num);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetLastSelectGame(ref GLU8 game_num);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetEnabledGameMask(GLU32 game_mask);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetAFTTransReceiptData(ref SAS_AFT_TRANS_RECEIPT_DATA receipt_data);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetAFTReceiptData(ref SAS_AFT_RECEIPT_DATA receipt_data);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetEnabledGameMask(ref GLU32 game_mask);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetGameDetail(GLU8 game_num, SAS_GAME_DETAIL game_detail);

        [DllImport("GXGSASAPI.dll")]
        private static extern GLU32 GXG_SAS_GetGameDetail(GLU8 game_num, IntPtr game_detail); // Need Wrap
        public static GLU32 GXG_SAS_GetGameDetail(GLU8 game_num, ref SAS_GAME_DETAIL data)
        {
            GLU32 dwStatus = 0;
            IntPtr pData;

            pData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SAS_GAME_DETAIL)));
            Marshal.StructureToPtr(data, pData, true);
            if (pData != IntPtr.Zero)
            {
                dwStatus = GXG_SAS_GetGameDetail(game_num, pData);
                if (dwStatus == 0)
                    data = (SAS_GAME_DETAIL)Marshal.PtrToStructure(pData, typeof(SAS_GAME_DETAIL));
                Marshal.FreeHGlobal(pData);
            }
            else
                dwStatus = (GLU32)ERROR_CODE.ERR_FAIL_ALLOC;
            return dwStatus;
        }

        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetFeatureCode(GLU8 game_num, SAS_FEATURE_CODE features);

        [DllImport("GXGSASAPI.dll")]
        private static extern GLU32 GXG_SAS_GetFeatureCode(GLU8 game_num, IntPtr features); // Need Wrap
        public static GLU32 GXG_SAS_GetFeatureCode(GLU8 game_num, ref SAS_FEATURE_CODE data)
        {
            GLU32 dwStatus = 0;
            IntPtr pData;

            pData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SAS_FEATURE_CODE)));
            Marshal.StructureToPtr(data, pData, true);
            if (pData != IntPtr.Zero)
            {
                dwStatus = GXG_SAS_GetFeatureCode(game_num, pData);
                if (dwStatus == 0)
                    data = (SAS_FEATURE_CODE)Marshal.PtrToStructure(pData, typeof(SAS_FEATURE_CODE));
                Marshal.FreeHGlobal(pData);
            }
            else
                dwStatus = (GLU32)ERROR_CODE.ERR_FAIL_ALLOC;
            return dwStatus;
        }

        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetFeature(GLU8 game_num, GLU32 feature);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetFeature(GLU8 game_num, ref GLU32 feature);

        [DllImport("GXGSASAPI.dll", CharSet = CharSet.Ansi)]
        private static extern GLU32 GXG_SAS_SetEGMInfo(GLU8 sas_denom, GLU8 token_denom, StringBuilder serial_num); // Need Wrap
        public static GLU32 GXG_SAS_SetEGMInfo(GLU8 sas_denom, GLU8 token_denom, string serial_num)
        {
            StringBuilder sb = new StringBuilder(serial_num, 64);
            return GXG_SAS_SetEGMInfo(sas_denom, token_denom, sb);
        }

        [DllImport("GXGSASAPI.dll", CharSet = CharSet.Ansi)]
        private static extern GLU32 GXG_SAS_GetEGMInfo(ref GLU8 sas_denom, ref GLU8 token_denom, StringBuilder serial_num); // Need Wrap
        public static GLU32 GXG_SAS_GetEGMInfo(ref GLU8 sas_denom, ref GLU8 token_denom, ref string serial_num)
        {
            GLU32 dwStatus = 0;
            StringBuilder sb = new StringBuilder(64);

            dwStatus = GXG_SAS_GetEGMInfo(ref sas_denom, ref token_denom, sb);
            if (dwStatus == 0)
                serial_num = Convert.ToString(sb);
            return dwStatus;
        }

        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetAssetNumber(GLU32 asset_num);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetAssetNumber(ref GLU32 asset_num);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetHostCtrlMachineStatus(ref GLU16 status);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetHostCfgBillDenom(ref GLU32 denom_cfg, ref GLU8 action_flag);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetDateTime(SAS_DATE_TIME datetime);

        [DllImport("GXGSASAPI.dll")]
        private static extern GLU32 GXG_SAS_GetDateTime(IntPtr datetime); // Need Wrap
        public static GLU32 GXG_SAS_GetDateTime(ref SAS_DATE_TIME data)
        {
            GLU32 dwStatus = 0;
            IntPtr pData;

            pData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SAS_DATE_TIME)));
            Marshal.StructureToPtr(data, pData, true);
            if (pData != IntPtr.Zero)
            {
                dwStatus = GXG_SAS_GetDateTime(pData);
                if (dwStatus == 0)
                    data = (SAS_DATE_TIME)Marshal.PtrToStructure(pData, typeof(SAS_DATE_TIME));
                Marshal.FreeHGlobal(pData);
            }
            else
                dwStatus = (GLU32)ERROR_CODE.ERR_FAIL_ALLOC;
            return dwStatus;
        }
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetSASDenom(GLU8 denom_code);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetSASDenom(ref GLU8 denom_code);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetAFTTransData(ref SAS_AFT_TRANS_DATA trans_data);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_CompleteAFTTrans(GLU8 trans_status, GLU64 cash_amount, GLU64 res_amount, GLU64 nonres_amount, ref SAS_DATE_TIME datetime);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_CompleteAFTReceipt(ref SAS_DATE_TIME datetime);

        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetTokenDenom(GLU8 denom_code);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetTokenDenom(ref GLU8 denom_code);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetPoolID(ref GLU16 pool_id);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetCreditLimit(GLU64 value);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetCreditLimit(ref GLU64 value);

        /* API for SAS Multi-Denomination */
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_AddGameDenom(GLU8 game_num, GLU8 denom_code, GLU16 max_bet);

        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_DelGameDenom(GLU8 game_num, GLU8 denom_code);

        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetCurrentGameDenom(GLU8 denom_code);

        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetCurrentGameDenom(GLU8 game_num, ref GLU8 denom_code);

        /* API for SAS Exception */
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_InsertException(GLU8 port_num, GLU8 event_code);

        /* API for Meter */
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetMeterEx(GLU8 meter_type, GLU8 num, GLU8 meter_code, ref GLU64 value);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_IncMachineMeter(GLU8 meter_code, GLU64 value);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_DecMachineMeter(GLU8 meter_code, GLU64 value);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetMachineMeter(GLU8 meter_code, GLU64 value);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_IncGameMeter(GLU8 game_num, GLU8 meter_code, GLU64 value);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetGameMeter(GLU8 game_num, GLU8 meter_code, GLU64 value);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_IncDenomMeter(GLU8 denom_code, GLU8 meter_code, GLU64 value);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetDenomMeter(GLU8 denom_code, GLU8 meter_code, GLU64 value);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_IncWonMeter(GLU8 type, GLU64 amount);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetCurrentCedit(GLU8 type, GLU64 value);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetCurrentCedit(GLU8 type, ref GLU64 value);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_ResetAllMeter();
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_ResetBillMeter();

        /* API for Bill In & Coin In/Out */
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_BillAccepted(GLU8 country_code, GLU8 denom_code, GLU8 to_hopper);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_CoinAccepted(GLU64 amount, GLU8 to_hopper);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_CoinPaid(GLU64 amount);

        /* API for SAS ROM Signature */
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetROMSig(GLU8 port_num, GLU16 sig, GLU16 seed);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetROMSigSeed(GLU8 port_num, ref GLU16 seed);

        /* API for SAS Handpay */
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetHandpayMode(GLU8 mode);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetHandpayMode(ref GLU8 mode);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_StartHandPay(SAS_HANDPAY_INFO hp_info);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_ResetHandPay(SAS_DATE_TIME datetime, GLU8 has_receipt, GLU16 receipt_num);

        /* API for SAS TITO */
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetValidPort(GLU8 port_num);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetValidPort(ref GLU8 port_num);

        /* API for SAS Legacy Bonus */
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetLegacyBonusPort(GLU8 port_num);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetLegacyBonusPort(ref GLU8 port_num);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetEGMBusy(GLU8 flag);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_PayLegacyBonus(GLU64 amount, GLU8 tax_type, GLU8 is_handpay);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetCurrentLegacyBonus(ref GLU64 amount, ref GLU8 tax_type);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetGameDelay(ref GLU16 delay_100ms);

        /* API for SAS Game Event */
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GameStart(GLU64 bet_amount, GLU8 wager_cat_num);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GameEnd(GLU64 win_amount);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_AccessGameRecall(GLU8 game_num, GLU16 index);

        /* API for AFT */
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetAFTPort(GLU8 port_num);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetAFTPort(ref GLU8 port_num);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetAFTTransLimit(GLU64 value);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetAFTRegisterData(ref GLU8 reg_key, ref GLU8 pos_id);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetAFTEGMStatus(GLU8 type, GLU8 status);
        [DllImport("GXGSASAPI.dll")]

        public static extern GLU32 GXG_SAS_SetAFTLockStatus(GLU8 type, GLU8 status);
        [DllImport("GXGSASAPI.dll")]

        public static extern GLU32 GXG_SAS_GetAFTEGMStatus(GLU8 type, ref GLU8 status);
        
        /* API for Progressive */
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetProgPort(GLU8 port_num);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetProgPort(ref GLU8 port_num);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetProgEnabled(GLU8 enabled);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetProgEnabled(ref GLU8 enabled);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetProgGroupID(GLU8 group_id, GLU8 is_force);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetProgGroupID(ref GLU8 group_id);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_SetProgEnabledLevel(GLU8 game_num, GLU32 level_mask);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetProgEnabledLevel(GLU8 game_num, ref GLU32 level_mask);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_GetProgAmount(GLU8 level, ref GLU64 amount);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_HitProgWin(GLU8 level, GLU64 amount, GLU8 is_handpay);

        [DllImport("GXGSASAPI.dll")]
        private static extern GLU32 GXG_SAS_GetAllProgAmount(GLU8 num, IntPtr hit_data_list, ref GLU64 total_amount);  // Need Wrap
        public static GLU32 GXG_SAS_GetAllProgAmount(GLU8 num, ref SAS_PROG_HIT_DATA data, ref GLU64 total_amount)
        {
            GLU32 dwStatus = 0;
            IntPtr pData;

            pData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SAS_PROG_HIT_DATA)));
            Marshal.StructureToPtr(data, pData, true);
            if (pData != IntPtr.Zero)
            {
                dwStatus = GXG_SAS_GetAllProgAmount(num, pData, ref total_amount);
                if (dwStatus == 0)
                    data = (SAS_PROG_HIT_DATA)Marshal.PtrToStructure(pData, typeof(SAS_PROG_HIT_DATA));
                Marshal.FreeHGlobal(pData);
            }
            else
                dwStatus = (GLU32)ERROR_CODE.ERR_FAIL_ALLOC;
            return dwStatus;
        }

        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_HitAllProgWin(GLU8 num, SAS_PROG_HIT_DATA hit_data_list, GLU8 is_handpay);

        /* API for NVRAM */
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_VerifyNVRAM(GLU16 block_num, GLU16 length);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_EnableAutoNVRAMCheck(GLU8 enabled);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_InformNVRAMError(GLU8 type);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 GXG_SAS_CleanNVRAMError();

        /* Other useful tools */
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 SASCreditToCent(GLU64 value, GLU8 denom_code, ref GLU64 result);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 CentToSASCredit(GLU64 value, GLU8 denom_code, ref GLU64 result);

        [DllImport("GXGSASAPI.dll")]
        private static extern GLU32 QwordToBCD(GLU64 value, IntPtr result, GLU8 length);   // Need Wrap
        public static GLU32 QwordToBCD(GLU64 value, ref BCD_ARRAY data, GLU8 length)
        {
            GLU32 dwStatus = 0;
            IntPtr pData;

            pData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(BCD_ARRAY)));
            Marshal.StructureToPtr(data, pData, true);
            if (pData != IntPtr.Zero)
            {
                dwStatus = QwordToBCD(value, pData, length);
                if (dwStatus == 0)
                    data = (BCD_ARRAY)Marshal.PtrToStructure(pData, typeof(BCD_ARRAY));
                Marshal.FreeHGlobal(pData);
            }
            else
                dwStatus = (GLU32)ERROR_CODE.ERR_FAIL_ALLOC;
            return dwStatus;
        }

        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 BCDToQword(BCD_ARRAY bcd, ref GLU64 result, GLU8 length);
        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 SetCurrentLocalTime(SAS_DATE_TIME datetime);

        [DllImport("GXGSASAPI.dll")]
        private static extern GLU32 GetCurrentLocalTime(IntPtr datetime); // Need Wrap
        public static GLU32 GetCurrentLocalTime(ref SAS_DATE_TIME data)
        {
            GLU32 dwStatus = 0;
            IntPtr pData;

            pData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SAS_DATE_TIME)));
            Marshal.StructureToPtr(data, pData, true);
            if (pData != IntPtr.Zero)
            {
                dwStatus = GetCurrentLocalTime(pData);
                if (dwStatus == 0)
                    data = (SAS_DATE_TIME)Marshal.PtrToStructure(pData, typeof(SAS_DATE_TIME));
                Marshal.FreeHGlobal(pData);
            }
            else
                dwStatus = (GLU32)ERROR_CODE.ERR_FAIL_ALLOC;
            return dwStatus;
        }

        [DllImport("GXGSASAPI.dll")]
        public static extern GLU32 SetCurrentUTCTime(SAS_DATE_TIME datetime);

        [DllImport("GXGSASAPI.dll")]
        private static extern GLU32 GetCurrentUTCTime(IntPtr datetime);  // Need Wrap
        public static GLU32 GetCurrentUTCTime(ref SAS_DATE_TIME data)
        {
            GLU32 dwStatus = 0;
            IntPtr pData;

            pData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SAS_DATE_TIME)));
            Marshal.StructureToPtr(data, pData, true);
            if (pData != IntPtr.Zero)
            {
                dwStatus = GetCurrentUTCTime(pData);
                if (dwStatus == 0)
                    data = (SAS_DATE_TIME)Marshal.PtrToStructure(pData, typeof(SAS_DATE_TIME));
                Marshal.FreeHGlobal(pData);
            }
            else
                dwStatus = (GLU32)ERROR_CODE.ERR_FAIL_ALLOC;
            return dwStatus;
        }

        /* API for Register Interrupt(Event) Callback */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void INTR_FUNC(IntPtr pData1, IntPtr pData2);
        #endregion

        private static INTR_FUNC_SAS sasUserCallback = null;
        private static INTR_FUNC sasIntrHandler;

        private static void SASIntrBridgeHandler(IntPtr pData1, IntPtr pData2)
        {
            SAS_INTR_DATA p_sIntrInfo = (SAS_INTR_DATA)Marshal.PtrToStructure(pData1, typeof(SAS_INTR_DATA));
            SAS_TICKET_DATA ticket_data;
            //SasHandle* pSasHandle = (SasHandle*)pClass;
            int i = 0;
            GLU32 dwStatus = 0;
            ushort status = 0;

            SAS_INTR_TYPE type = (SAS_INTR_TYPE)p_sIntrInfo.Status;
            switch (type)
            {
                case SAS_INTR_TYPE.SAS_INTR_FINISH_SYNC:
                    //EMIT_SAS_INTR_TRACE(pSasHandle, QString("SAS Port(%1) link up").arg(p_sIntrInfo->PortNum));
                    //if (g_bIsSASLinkUp[p_sIntrInfo->PortNum] == FALSE)
                    //{
                    //    g_bIsSASLinkUp[p_sIntrInfo->PortNum] = TRUE;
                    //    emit pSasHandle->SASLinkStateChanged(p_sIntrInfo->PortNum, SAS_LINK_STATUS_NORMAL);
                    //}
                    break;
                case SAS_INTR_TYPE.SAS_INTR_LINK_DOWN:
                    //EMIT_SAS_INTR_TRACE(pSasHandle, QString("SAS Port(%1) link down").arg(p_sIntrInfo->PortNum));

                    if (GET_LINK_DOWN_TYPE(ref p_sIntrInfo) == (byte)SAS_LINK_STATUS.SAS_LINK_STATUS_LINK_DOWN_5S_NO_ADDR)
                    {
                        //    if (g_bIsSASLinkUp[p_sIntrInfo->PortNum] == TRUE)
                        //    {
                        //        g_bIsSASLinkUp[p_sIntrInfo->PortNum] = FALSE;
                        //    }
                        //    // NOTE: It is possible that we get 30S no acked and then get 5s no addr link down
                        //    emit pSasHandle->SASLinkStateChanged(p_sIntrInfo->PortNum, SAS_LINK_STATUS_LINK_DOWN_5S_NO_ADDR);
                    }
                    else
                    {

                        //    if (g_bIsSASLinkUp[p_sIntrInfo->PortNum] == TRUE)
                        //    {
                        //        g_bIsSASLinkUp[p_sIntrInfo->PortNum] = FALSE;
                        //        emit pSasHandle->SASLinkStateChanged(p_sIntrInfo->PortNum, SAS_LINK_STATUS_LINK_DOWN_30S_NO_ACK);

                    }
                    break;
                case SAS_INTR_TYPE.SAS_INTR_HOST_SET_RTE_MODE:
                    //EMIT_SAS_INTR_TRACE(pSasHandle,
                    //    QString("SAS Port(%1) %2 RTE mode").arg(p_sIntrInfo->PortNum).arg(GET_RTE_ENABLE_FLAG(p_sIntrInfo->Data) ? "Enter" : "Leave"));
                    string enableflag = GET_RTE_ENABLE_FLAG(ref p_sIntrInfo) == 1 ? "Enter" : "Leave";
                    break;
                case SAS_INTR_TYPE.SAS_INTR_EC_QUEUE_FULL:
                    //EMIT_SAS_INTR_TRACE(pSasHandle,
                    //    QString("SAS Port(%1) exception queue is %2full now").arg(p_sIntrInfo->PortNum).arg(GET_EC_QUEUE_FULL_FLAG(p_sIntrInfo->Data) ? "" : "not "));
                    string fullflg = GET_EC_QUEUE_FULL_FLAG(ref p_sIntrInfo) == 1 ? "" : "not ";
                    break;
                case SAS_INTR_TYPE.SAS_INTR_ROM_SIG_REQUEST:


                    GXGSASGlobalVariables.g_wROMSigSeed[p_sIntrInfo.PortNum] = GET_ROMSIG_SEED(ref p_sIntrInfo);
                    GXGSASGlobalVariables.g_atIsGetROMSigReqCnt[p_sIntrInfo.PortNum]++;
                    break;
                case SAS_INTR_TYPE.SAS_INTR_BILL_DENOM_STATUS_CHANGE:
                    //EMIT_SAS_INTR_TRACE(pSasHandle, QString("Bill denomination configuration is changed by host"));
                    //EMIT_SAS_INTR_TRACE(pSasHandle,
                    //    QString("Bill denomination enabled mask = 0x%1, action flag = %2").
                    //                    arg(GET_BILL_DENOM_STATUS(p_sIntrInfo->Data), 8, 16, QChar('0')).
                    //                    arg(GET_BILL_DENOM_ACTION_FLAG(p_sIntrInfo->Data)));
                    //g_dwHostCtrlBillDenomCfg = GET_BILL_DENOM_STATUS(p_sIntrInfo->Data);
                    //g_baEnabledMutex.lock () ;
                    //if (g_bHostCtrlBAActionFlag != GET_BILL_DENOM_ACTION_FLAG(p_sIntrInfo->Data))
                    //{
                    //    g_bHostCtrlBAActionFlag = GET_BILL_DENOM_ACTION_FLAG(p_sIntrInfo->Data);
                    //    g_atIsHostReqEnableBA = FALSE;
                    //}
                    //g_baEnabledMutex.unlock();
                    break;
                case SAS_INTR_TYPE.SAS_INTR_ENABLE_BA_REQUEST:
                    //EMIT_SAS_INTR_TRACE(pSasHandle, QString("Host request enable bill acceptor"));
                    //g_baEnabledMutex.lock () ;
                    //g_atIsHostReqEnableBA = TRUE;
                    //g_baEnabledMutex.unlock();
                    break;
                case SAS_INTR_TYPE.SAS_INTR_EN_DIS_GAME_N:
                    //EMIT_SAS_INTR_TRACE(pSasHandle, QString("Game enabled configuration is changed by host"));
                    //EMIT_SAS_INTR_TRACE(pSasHandle,
                    //    QString("Game enabled mask = 0x%1").arg(GET_GAME_ENABLE_STATUS(p_sIntrInfo->Data), 8, 16, QChar('0')));
                    //g_dwEnabledGameMask = GET_GAME_ENABLE_STATUS(p_sIntrInfo->Data);
                    //emit pSasHandle->SASEnabledGameChanged(GET_GAME_ENABLE_STATUS(p_sIntrInfo->Data));
                    break;
                case SAS_INTR_TYPE.SAS_INTR_GAME_DELAY:
                    //EMIT_SAS_INTR_TRACE(pSasHandle,
                    //    QString("Host set game delay = %1 ms").arg(GET_GAME_DELAY_TIME(p_sIntrInfo->Data)));
                    //g_gameDelayMutex.lock () ;
                    //g_wCurrGameDelayTime100MS = GET_GAME_DELAY_TIME(p_sIntrInfo->Data) / 100;
                    //g_atGameDelayUpdateCnt.ref ();
                    //g_gameDelayMutex.unlock();
                    break;
                case SAS_INTR_TYPE.SAS_INTR_SET_DATE_TIME:
                    //EMIT_SAS_INTR_TRACE(pSasHandle,
                    //    QString("Host set date time = %1/%2/%3 %4:%5:%6").
                    //                    arg(GET_DATE_TIME_YEAR(p_sIntrInfo->Data), 4, 10, QChar('0')).
                    //                    arg(GET_DATE_TIME_MONTH(p_sIntrInfo->Data), 2, 10, QChar('0')).
                    //                    arg(GET_DATE_TIME_DATE(p_sIntrInfo->Data), 2, 10, QChar('0')).
                    //                    arg(GET_DATE_TIME_HOUR(p_sIntrInfo->Data), 2, 10, QChar('0')).
                    //                    arg(GET_DATE_TIME_MIN(p_sIntrInfo->Data), 2, 10, QChar('0')).
                    //                    arg(GET_DATE_TIME_SEC(p_sIntrInfo->Data), 2, 10, QChar('0')));
                    //emit pSasHandle->SASDateTimeChanged((GLU32*)p_sIntrInfo->Data);
                    break;
                case SAS_INTR_TYPE.SAS_INTR_MACHINE_STATUS_CHANGE:
                    GET_MACHINE_STATUS(ref p_sIntrInfo);

                    break;
                case SAS_INTR_TYPE.SAS_INTR_SEC_VALID_NUM_SET:
                    //EMIT_SAS_INTR_TRACE(pSasHandle, "Secure validation number is set by host");
                    //emit pSasHandle->SASSecValidNumSet(true);
                    break;
                case SAS_INTR_TYPE.SAS_INTR_TICKET_REDEEM_RESULT:
                    //EMIT_SAS_INTR_TRACE(pSasHandle, "<Ticket Redeem> Get ticket redemption result:");
                    //if (GET_REDEEM_RESULT(p_sIntrInfo->Data) == SAS_TICKET_REDEEM_RESULT_PENDING)
                    //{
                    //    EMIT_SAS_INTR_TRACE(pSasHandle, QString("<Ticket Redeem> Result: Host allow accept ticket"));
                    //    EMIT_SAS_INTR_TRACE(pSasHandle, QString("<Ticket Redeem> Ticket type: 0x%1").arg(GET_REDEEM_TICKET_TYPE(p_sIntrInfo->Data), 2, 16, QChar('0')));
                    //    EMIT_SAS_INTR_TRACE(pSasHandle, QString("<Ticket Redeem> Ticket amount = %1 (cents)").arg(GET_REDEEM_TICKET_AMOUNT(p_sIntrInfo->Data)));
                    //    EMIT_SAS_INTR_TRACE(pSasHandle, QString("<Ticket Redeem> Change amount = %1 (cents)").arg(GET_REDEEM_CHANGE_AMOUNT(p_sIntrInfo->Data)));
                    //    EMIT_SAS_INTR_TRACE(pSasHandle, QString("<Ticket Redeem> Pool ID = %1").arg(GET_REDEEM_POOL_ID(p_sIntrInfo->Data)));
                    //}
                    //else if (GET_REDEEM_RESULT(p_sIntrInfo->Data) == SAS_TICKET_REDEEM_RESULT_HOST_REJECT)
                    //{
                    //    EMIT_SAS_INTR_TRACE(pSasHandle,
                    //                        QString("<Ticket Redeem> Result: Host reject ticket (0x%1)").arg(GET_REDEEM_STATUS(p_sIntrInfo->Data), 2, 16, QChar('0')));
                    //}
                    //else if (GET_REDEEM_RESULT(p_sIntrInfo->Data) == SAS_TICKET_REDEEM_RESULT_LP70_TIMEOUT)
                    //{
                    //    EMIT_SAS_INTR_TRACE(pSasHandle, "<Ticket Redeem> Result: Ticket reject due to LP70 timeout");
                    //}
                    //else if (GET_REDEEM_RESULT(p_sIntrInfo->Data) == SAS_TICKET_REDEEM_RESULT_LP71_TIMEOUT)
                    //{
                    //    EMIT_SAS_INTR_TRACE(pSasHandle, "<Ticket Redeem> Result: Ticket reject due to LP71 timeout");
                    //}
                    //else if (GET_REDEEM_RESULT(p_sIntrInfo->Data) == SAS_TICKET_REDEEM_RESULT_LINK_DOWN)
                    //{
                    //    EMIT_SAS_INTR_TRACE(pSasHandle, "<Ticket Redeem> Result: Ticket reject due to link down");
                    //}

                    //g_bTicketInResult = GET_REDEEM_RESULT(p_sIntrInfo->Data);
                    //g_bTicketInType = GET_REDEEM_TICKET_TYPE(p_sIntrInfo->Data);
                    //g_qwTicketInAmount = GET_REDEEM_TICKET_AMOUNT(p_sIntrInfo->Data);
                    //g_qwTicketInChangeAmount = GET_REDEEM_CHANGE_AMOUNT(p_sIntrInfo->Data);
                    //g_atIsGetRedeemResult = TRUE;
                    break;
                case SAS_INTR_TYPE.SAS_INTR_TICKET_REDEEM_FINISHED:
                    //EMIT_SAS_INTR_TRACE(pSasHandle, "Current ticket redeem cycle is finished");
                    break;
                case SAS_INTR_TYPE.SAS_INTR_EXT_VALID_STATUS_CHANGE:
                    //EMIT_SAS_INTR_TRACE(pSasHandle, "Host extended validation status is changed");
                    //EMIT_SAS_INTR_TRACE(pSasHandle,
                    //    QString("Extended validation status = 0x%1").arg(GET_EXT_VALID_STATUS(p_sIntrInfo->Data), 2, 16, QChar('0')));
                    //g_extValidStatusMutex.lock () ;
                    //g_bSASHostExtValidStatus = GET_EXT_VALID_STATUS(p_sIntrInfo->Data);
                    //g_bSASCombExtValidStatus = g_bSASEGMExtValidStatus & g_bSASHostExtValidStatus;
                    //g_extValidStatusMutex.unlock();
                    break;
                case SAS_INTR_TYPE.SAS_INTR_PREP_TICKET_PRINT_RESULT:
                    //EMIT_SAS_INTR_TRACE(pSasHandle, "<Prep Ticket> Get prepare ticket print result:");
                    //if (GET_SYS_PREP_TICKET_RESULT(p_sIntrInfo->Data) == 0)
                    //{
                    //    SAVE_SYS_PREP_TICKET_VALID_CODE(p_sIntrInfo->Data, g_bCurrSysValidCode);

                    //    EMIT_SAS_INTR_TRACE(pSasHandle, "<Prep Ticket> Result: ALLOW");
                    //    EMIT_SAS_INTR_TRACE(pSasHandle,
                    //        QString("Validation number = %1 %2 %3 %4 %5 %6 %7 %8 %9").
                    //                        arg(g_bCurrSysValidCode[0], 2, 16, QChar('0')).
                    //                        arg(g_bCurrSysValidCode[1], 2, 16, QChar('0')).
                    //                        arg(g_bCurrSysValidCode[2], 2, 16, QChar('0')).
                    //                        arg(g_bCurrSysValidCode[3], 2, 16, QChar('0')).
                    //                        arg(g_bCurrSysValidCode[4], 2, 16, QChar('0')).
                    //                        arg(g_bCurrSysValidCode[5], 2, 16, QChar('0')).
                    //                        arg(g_bCurrSysValidCode[6], 2, 16, QChar('0')).
                    //                        arg(g_bCurrSysValidCode[7], 2, 16, QChar('0')).
                    //                        arg(g_bCurrSysValidCode[8], 2, 16, QChar('0')));

                    //    g_atIsHostAllowPrint = TRUE;
                    //}
                    //else
                    //{
                    //    EMIT_SAS_INTR_TRACE(pSasHandle, "<Prep Ticket> Result: DENIED");

                    //    g_atIsHostAllowPrint = FALSE;
                    //}
                    //g_atIsGetPrepPrintResult = TRUE;
                    break;
                case SAS_INTR_TYPE.SAS_INTR_VALID_QUEUE_FULL:
                    //EMIT_SAS_INTR_TRACE(pSasHandle,
                    //    QString("Validation record queue is %1full now").arg(GET_VALID_QUEUE_FULL_FLAG(p_sIntrInfo->Data) ? "" : "not "));
                    //emit pSasHandle->SASValidQueueFull(GET_VALID_QUEUE_FULL_FLAG(p_sIntrInfo->Data) ? true : false);
                    break;
                case SAS_INTR_TYPE.SAS_INTR_HOST_CANCEL_VALID_CFG:
                    //EMIT_SAS_INTR_TRACE(pSasHandle, "Host cancelled the validation configuration");
                    //emit pSasHandle->SASSecValidNumSet(false);
                    break;
                case SAS_INTR_TYPE.SAS_INTR_GET_LEGACY_BONUS:
                    //EMIT_SAS_INTR_TRACE(pSasHandle,
                    //    QString("Get legacy bonus, tax type = %1, amount = %2").
                    //                    arg(GET_LEGACY_BONUS_TAX_TYPE(p_sIntrInfo->Data)).
                    //                    arg(GET_LEGACY_BONUS_AMOUNT(p_sIntrInfo->Data)));
                    break;
                case SAS_INTR_TYPE.SAS_INTR_GET_AFT_REGISTER_INFO:
                    //EMIT_SAS_INTR_TRACE(pSasHandle, "Get AFT register information");
                    //if (GET_AFT_REGISTER_STATUS(p_sIntrInfo->Data) == SAS_AFT_REG_STATUS_REGISTERED)
                    //{
                    //    EMIT_SAS_INTR_TRACE(pSasHandle, "AFT registered. Update AFT registration information.");

                    //    SAVE_AFT_REGISTER_REG_KEY(p_sIntrInfo->Data, g_bAFTRegKey);
                    //    SAVE_AFT_REGISTER_POS_ID(p_sIntrInfo->Data, g_bAFTPosID);
                    //}
                    //else if (GET_AFT_REGISTER_STATUS(p_sIntrInfo->Data) == SAS_AFT_REG_STATUS_UNREGISTER)
                    //{
                    //    EMIT_SAS_INTR_TRACE(pSasHandle, "AFT deregistered");
                    //}
                    //else if (GET_AFT_REGISTER_STATUS(p_sIntrInfo->Data) == SAS_AFT_REG_STATUS_PENDING)
                    //{
                    //    EMIT_SAS_INTR_TRACE(pSasHandle, "AFT register pending. (Wait operator do ACK action)");
                    //}
                    //g_aftRegMutex.lock () ;
                    //g_atAFTRegStatus = GET_AFT_REGISTER_STATUS(p_sIntrInfo->Data);
                    //g_atAFTRegInfoChangeCnt.ref ();
                    //g_aftRegMutex.unlock();
                    break;
                case SAS_INTR_TYPE.SAS_INTR_GET_AFT_LOCK_INFO:
                    //EMIT_SAS_INTR_TRACE(pSasHandle, "Get AFT lock information:");

                    //if (GET_AFT_LOCK_STATUS(p_sIntrInfo->Data) == SAS_AFT_LOCK_STATUS_PENDING)
                    //{
                    //    EMIT_SAS_INTR_TRACE(pSasHandle, "AFT lock pending");
                    //    EMIT_SAS_INTR_TRACE(pSasHandle,
                    //                        QString("Transfer Condition = ") +
                    //                        QSTR_SHOW_HEX_N(GET_AFT_LOCK_TRANS_CONDITION(p_sIntrInfo->Data), 2));
                    //    EMIT_SAS_INTR_TRACE(pSasHandle,
                    //                        QString("Lock Timeout = ") +
                    //                        QSTR_SHOW_DEC_N(GET_AFT_LOCK_TIMEOUT(p_sIntrInfo->Data), 1) +
                    //                        QString(" ms"));

                    //    g_aftLockMutex.lock () ;
                    //    g_bAFTLockTransConditionReq = GET_AFT_LOCK_TRANS_CONDITION(p_sIntrInfo->Data);
                    //    g_dwAFTLockTimeoutMS = GET_AFT_LOCK_TIMEOUT(p_sIntrInfo->Data);
                    //    g_atIsAFTLockPending = TRUE;
                    //    g_aftLockMutex.unlock();
                    //}
                    //else // if (GET_AFT_LOCK_STATUS(p_sIntrInfo->Data) == SAS_AFT_LOCK_STATUS_NOT_LOCKED)
                    //{
                    //    /*
                    //     * If we get SAS_AFT_LOCK_STATUS_NOT_LOCKED status, it means the AFT lock is unlocked due to
                    //     * the SAS link down
                    //     */
                    //    EMIT_SAS_INTR_TRACE(pSasHandle, "AFT lock unlocked");
                    //    g_atIsAFTLockInformUnlocked = TRUE;
                    //}
                    break;
                case SAS_INTR_TYPE.SAS_INTR_AFT_TRANS_PENDING:
                    //EMIT_SAS_INTR_TRACE(pSasHandle, "AFT transfer pending");
                    //g_atIsAFTTransReqCnt.ref ();
                    //g_atIsAFTTransReqCancel = FALSE;
                    break;
                case SAS_INTR_TYPE.SAS_INTR_AFT_TRANS_FINISHED:
                    //EMIT_SAS_INTR_TRACE(pSasHandle, "AFT transfer finished");
                    break;
                case SAS_INTR_TYPE.SAS_INTR_AFT_TRANS_REJECT:
                    //EMIT_SAS_INTR_TRACE(pSasHandle,
                    //                    QString("AFT transfer rejected, status = ") +
                    //                    QSTR_SHOW_HEX_N(GET_AFT_TRANS_REJECT_STATUS(p_sIntrInfo->Data), 2));

                    //if (GET_AFT_TRANS_REJECT_STATUS(p_sIntrInfo->Data) == SAS_AFT_REJECT_STATUS_HOST_CASHOUT_TIMEOUT)
                    //{
                    //    // NOTE: Host cashout process only abort after host timeout
                    //    if (g_atIsAFTHostCashoutPending == TRUE)
                    //    {
                    //        /*
                    //         * NOTE:
                    //         * For hard mode: Remain pending until operator select other cashout method
                    //         * For soft mode: Use other cashout method directly
                    //         */
                    //        if ((g_bCurrHostCashOutStatus & BITMASK_SAS_EGM_HOST_CASHOUT_STATUS_HARD_MODE) == 0)
                    //        {
                    //            g_atIsAFTHostCashoutPending = FALSE;
                    //        }
                    //        g_atIsAFTHostCashoutTimeout = TRUE;
                    //    }
                    //    if (g_atIsAFTHostCashoutWinPending == TRUE)
                    //    {
                    //        g_qwLastRemainUnpaidWinAmountCent = g_qwLastWinPaytableAmountCent + g_qwLastWinProgAmountCent;
                    //        /*
                    //         * NOTE:
                    //         * For hard mode: Remain pending until operator select other cashout method
                    //         * For soft mode: Use other cashout method directly
                    //         */
                    //        if ((g_bCurrHostCashOutStatus & BITMASK_SAS_EGM_HOST_CASHOUT_STATUS_HARD_MODE) == 0)
                    //        {
                    //            g_atIsAFTHostCashoutWinPending = FALSE;
                    //        }
                    //        g_atIsAFTHostCashoutTimeout = TRUE;
                    //    }
                    //}
                    break;
                case SAS_INTR_TYPE.SAS_INTR_AFT_HOST_CASHOUT_STATUS_CHANGE:
                    //EMIT_SAS_INTR_TRACE(pSasHandle,
                    //                    QString("Host cashout status changed, current status = ") +
                    //                    QSTR_SHOW_HEX_N(GET_AFT_HOST_CASHOUT_STATUS(p_sIntrInfo->Data), 2));
                    //g_bCurrHostCashOutStatus = GET_AFT_HOST_CASHOUT_STATUS(p_sIntrInfo->Data);
                    break;
                case SAS_INTR_TYPE.SAS_INTR_AFT_TRANS_HOST_REQ_CANCEL:
                    //EMIT_SAS_INTR_TRACE(pSasHandle, "Host request cancel current AFT transfer");
                    //g_atIsAFTTransReqCancel = TRUE;
                    break;
                case SAS_INTR_TYPE.SAS_INTR_PROG_LINK_STATUS_CHANGE:
                    //if (GET_PROG_LINK_STATUS(p_sIntrInfo->Data) == SAS_PROG_LINK_STATUS_NORMAL)
                    //{
                    //    EMIT_SAS_INTR_TRACE(pSasHandle, "SAS Progressive Link Up");
                    //}
                    //else
                    //{
                    //    EMIT_SAS_INTR_TRACE(pSasHandle, "SAS Progressive Link Down");
                    //}
                    //emit pSasHandle->SASProgLinkStateChanged(GET_PROG_LINK_STATUS(p_sIntrInfo->Data));
                    break;
                case SAS_INTR_TYPE.SAS_INTR_DETECT_NVRAM_ERROR:
                    //EMIT_SAS_INTR_TRACE(pSasHandle, QString("Detect NVRAM error at block (%1)").arg(GET_NVRAM_ERROR_BLOCK_NUM(p_sIntrInfo->Data)));
                    //emit pSasHandle->NVRAMErrorDetected();
                    break;
                default:
                    //EMIT_SAS_INTR_TRACE(pSasHandle, QString("Got undefined status(%1)").arg(p_sIntrInfo->Status, 2, 16, QChar('0')));
                    break;
            }


            if (pData1 != IntPtr.Zero && sasUserCallback != null)
            {
                SAS_INTR_DATA sasIntrData = (SAS_INTR_DATA)Marshal.PtrToStructure(pData1, typeof(SAS_INTR_DATA));
                sasUserCallback(sasIntrData);
            }
        }
        #region handle event data
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GLU8 GetIntrByteData(ref SAS_INTR_DATA intrData, int offset)
        {
            return intrData.Data[offset];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GLU16 GetIntrWordData(ref SAS_INTR_DATA intrData, int offset)
        {
            GLU16 value = Convert.ToUInt16(intrData.Data[offset]);
            GLU16 value8 = Convert.ToUInt16(intrData.Data[offset + 1]);
            value8 <<= 8;
            value += value8;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GLU32 GetIntrDwordData(ref SAS_INTR_DATA intrData, int offset)
        {
            GLU32 value = 0;
            for (int i = 0; i < 4; i++)
            {
                value += (Convert.ToUInt32(intrData.Data[offset + i]) << (8 * i));
            }
            return value;
        }

        public static GLU32 CheckSASAFTTransState()
        {
            GLU32 dwStatus;
            GLU8 bSASStatus = 0;


            dwStatus = GXG_SAS_GetStatus(0, (byte)SAS_STATUS_TYPE.SAS_STATUS_AFT_TRANS_PROC.GetHashCode(), ref bSASStatus, IntPtr.Zero);
            if (dwStatus != (byte)ERROR_CODE.ERR_NONE)
            {
                return dwStatus;
            }

            return bSASStatus;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GLU64 GetIntrQwordData(ref SAS_INTR_DATA intrData, int offset)
        {
            GLU64 value = 0;
            for (int i = 0; i < 8; i++)
            {
                value += (Convert.ToUInt64(intrData.Data[offset + i]) << (8 * i));
            }
            return value;
        }
        public static GLU8 GET_LINK_DOWN_TYPE(ref SAS_INTR_DATA intrData)
        {
            return GetIntrByteData(ref intrData, 0);
        }

        public static GLU8 GET_EC_QUEUE_FULL_FLAG(ref SAS_INTR_DATA intrData)
        {
            return GetIntrByteData(ref intrData, 0);
        }

        public static GLU16 GET_ROMSIG_SEED(ref SAS_INTR_DATA intrData)
        {
            return GetIntrWordData(ref intrData, 0);
        }

        public static GLU8 GET_RTE_ENABLE_FLAG(ref SAS_INTR_DATA intrData)
        {
            return GetIntrByteData(ref intrData, 0);
        }

        public static GLU16 GET_MACHINE_STATUS(ref SAS_INTR_DATA intrData)
        {
            return GetIntrWordData(ref intrData, 0);
        }

        public static GLU32 GET_BILL_DENOM_STATUS(ref SAS_INTR_DATA intrData)
        {
            return GetIntrDwordData(ref intrData, 0);
        }

        public static GLU8 GET_BILL_DENOM_ACTION_FLAG(ref SAS_INTR_DATA intrData)
        {
            return GetIntrByteData(ref intrData, 4);
        }

        public static GLU32 GET_GAME_ENABLE_STATUS(ref SAS_INTR_DATA intrData)
        {
            return GetIntrDwordData(ref intrData, 0);
        }

        public static GLU32 GET_GAME_DELAY_TIME(ref SAS_INTR_DATA intrData)
        {
            GLU32 temp = GetIntrWordData(ref intrData, 0);
            return temp * 100;
        }

        public static GLU32 GET_DATE_TIME_YEAR(ref SAS_INTR_DATA intrData)
        {
            return GetIntrDwordData(ref intrData, 0);
        }

        public static GLU32 GET_DATE_TIME_MONTH(ref SAS_INTR_DATA intrData)
        {
            return GetIntrDwordData(ref intrData, 4);
        }

        public static GLU32 GET_DATE_TIME_DATE(ref SAS_INTR_DATA intrData)
        {
            return GetIntrDwordData(ref intrData, 8);
        }

        public static GLU32 GET_DATE_TIME_HOUR(ref SAS_INTR_DATA intrData)
        {
            return GetIntrDwordData(ref intrData, 12);
        }

        public static GLU32 GET_DATE_TIME_MIN(ref SAS_INTR_DATA intrData)
        {
            return GetIntrDwordData(ref intrData, 16);
        }

        public static GLU32 GET_DATE_TIME_SEC(ref SAS_INTR_DATA intrData)
        {
            return GetIntrDwordData(ref intrData, 20);
        }

        public static GLU8 GET_REDEEM_RESULT(ref SAS_INTR_DATA intrData)
        {
            return GetIntrByteData(ref intrData, 0);
        }

        public static GLU8 GET_REDEEM_TICKET_TYPE(ref SAS_INTR_DATA intrData)
        {
            return GetIntrByteData(ref intrData, 1);
        }

        public static GLU8 GET_REDEEM_STATUS(ref SAS_INTR_DATA intrData)
        {
            return GetIntrByteData(ref intrData, 2);
        }

        public static GLU64 GET_REDEEM_TICKET_AMOUNT(ref SAS_INTR_DATA intrData)
        {
            return GetIntrQwordData(ref intrData, 3);
        }

        public static GLU64 GET_REDEEM_CHANGE_AMOUNT(ref SAS_INTR_DATA intrData)
        {
            return GetIntrQwordData(ref intrData, 11);
        }

        public static GLU16 GET_REDEEM_POOL_ID(ref SAS_INTR_DATA intrData)
        {
            return GetIntrWordData(ref intrData, 19);
        }

        public static GLU8 GET_EXT_VALID_STATUS(ref SAS_INTR_DATA intrData)
        {
            return GetIntrByteData(ref intrData, 0);
        }

        public static GLU8 GET_SYS_PREP_TICKET_RESULT(ref SAS_INTR_DATA intrData)
        {
            return GetIntrByteData(ref intrData, 0);
        }

        public static GLU8 GET_VALID_QUEUE_FULL_FLAG(ref SAS_INTR_DATA intrData)
        {
            return GetIntrByteData(ref intrData, 0);
        }

        public static GLU8 GET_LEGACY_BONUS_TAX_TYPE(ref SAS_INTR_DATA intrData)
        {
            return GetIntrByteData(ref intrData, 0);
        }

        public static GLU64 GET_LEGACY_BONUS_AMOUNT(ref SAS_INTR_DATA intrData)
        {
            return GetIntrQwordData(ref intrData, 1);
        }

        public static GLU8 GET_AFT_REGISTER_STATUS(ref SAS_INTR_DATA intrData)
        {
            return GetIntrByteData(ref intrData, 0);
        }

        public static GLU8 GET_AFT_LOCK_STATUS(ref SAS_INTR_DATA intrData)
        {
            return GetIntrByteData(ref intrData, 0);
        }

        public static GLU8 GET_AFT_LOCK_TRANS_CONDITION(ref SAS_INTR_DATA intrData)
        {
            return GetIntrByteData(ref intrData, 1);
        }

        public static GLU8 GET_AFT_LOCK_TIMEOUT(ref SAS_INTR_DATA intrData)
        {
            return GetIntrByteData(ref intrData, 2);
        }

        public static GLU8 GET_AFT_HOST_CASHOUT_STATUS(ref SAS_INTR_DATA intrData)
        {
            return GetIntrByteData(ref intrData, 0);
        }

        public static GLU8 GET_AFT_TRANS_REJECT_STATUS(ref SAS_INTR_DATA intrData)
        {
            return GetIntrByteData(ref intrData, 0);
        }

        public static GLU8 GET_PROG_LINK_STATUS(ref SAS_INTR_DATA intrData)
        {
            return GetIntrByteData(ref intrData, 0);
        }

        public static GLU8 GET_PROG_QUEUE_FULL_FLAG(ref SAS_INTR_DATA intrData)
        {
            return GetIntrByteData(ref intrData, 0);
        }

        public static GLU8 GET_NVRAM_ERROR_BLOCK_NUM(ref SAS_INTR_DATA intrData)
        {
            return GetIntrByteData(ref intrData, 0);
        }
        #endregion
    }
}

