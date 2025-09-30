
using EGMENGINE.EGMDataPersisterModule;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace EGMENGINE.EGMAccountingModule
{

    /// <summary>
    /// Class EGMSettings definition. Singleton
    /// </summary>
    internal class EGMAccounting
    {


        private static EGMAccounting egmaccounting_;

        internal EGMAccountingMeters meters;
        internal EGMAccountingAFTTransfers transfers;
        internal EGMAccountingLastBills bills;
        internal EGMAccountingRamClearHistory ramclears;
        internal EGMAccountingSystemLogs systemlogs;
        internal EGMAccountingLastPlays plays;
        internal EGMAccountingHandpay handpays;
        internal List<byte> MeterCodes;
        /// <summary>
        /// EGMStatus Constructor
        /// </summary>
        protected EGMAccounting()
        {
            meters = new EGMAccountingMeters();
            transfers = new EGMAccountingAFTTransfers();
            bills = new EGMAccountingLastBills();
            ramclears = new EGMAccountingRamClearHistory();
            systemlogs = new EGMAccountingSystemLogs();
            plays = new EGMAccountingLastPlays();
            handpays = new EGMAccountingHandpay();
            MeterCodes = new List<byte>
            {
                0x00,
                0x01,
                0x02,
                0x03,
                0x04,
                0x05,
                0x06,
                0x07,
                0x08,
                0x09,
                0x0A,
                0x0B,
                0x0C,
                0x0D,
                0x0E,
                0x0F,
                0x10,
                0x11,
                0x12,
                0x13,
                0x14,
                0x15,
                0x16,
                0x17,
                0x18,
                0x19,
                0x1A,
                0x1B,
                0x1C,
                0x1D,
                0x1E,
                0x1F,
                0x20,
                0x21,
                0x22,
                0x23,
                0x24,
                0x25,
                0x26,
                0x27,
                0x28,
                0x29,
                0x2A,
                0x2B,
                0x2C,
                0x2D,
                0x2E,
                0x2F,
                0x30,
                0x31,
                0x32,
                0x33,
                0x34,
                0x35,
                0x36,
                0x37,
                0x38,
                0x39,
                0x3A,
                0x3B,
                0x3C,
                0x3D,
                0x3E,
                0x3F,
                0x40,
                0x41,
                0x42,
                0x43,
                0x44,
                0x45,
                0x46,
                0x47,
                0x48,
                0x49,
                0x4A,
                0x4B,
                0x4C,
                0x4D,
                0x4E,
                0x4F,
                0x50,
                0x51,
                0x52,
                0x53,
                0x54,
                0x55,
                0x56,
                0x57,
                0x58,
                0x59,
                0x5A,
                0x5B,
                0x5C,
                0x5D,
                0x5E,
                0x5F,
                0x60,
                0x61,
                0x62,
                0x63,
                0x64,
                0x65,
                0x66,
                0x67,
                0x68,
                0x69,
                0x6A,
                0x6B,
                0x6C,
                0x6D,
                0x6E,
                0x6F,
                0x70,
                0x71,
                0x72,
                0x73,
                0x74,
                0x75,
                0x76,
                0x77,
                0x78,
                0x79,
                0x7A,
                0x7B,
                0x7C,
                0x7D,
                0x7E,
                0x7F,
                0x80,
                0x81,
                0x82,
                0x83,
                0x84,
                0x85,
                0x86,
                0x87,
                0x88,
                0x89,
                0x8A,
                0x8B,
                0x8C,
                0x8D,
                0x8E,
                0x8F,
                0x90,
                0x91,
                0x92,
                0x93,
                0xA0,
                0xA1,
                0xA2,
                0xA3,
                0xA4,
                0xA5,
                0xA6,
                0xA7,
                0xA8,
                0xA9,
                0xAA,
                0xAB,
                0xAC,
                0xAD,
                0xAE,
                0xAF,
                0xB0,
                0xB1,
                0xB8,
                0xB9,
                0xBA,
                0xBB,
                0xBC,
                0xBD,
                0xFA,
                0xFB,
                0xFC,
                0xFD,
                0xFE,
                0xFF
            };
        }

        public void resetAccounting()
        {
            meters = new EGMAccountingMeters();
            transfers = new EGMAccountingAFTTransfers();
            bills = new EGMAccountingLastBills();
            systemlogs = new EGMAccountingSystemLogs();
            plays = new EGMAccountingLastPlays();
            handpays = new EGMAccountingHandpay();
        }

        public void UpdateMeter(byte code, int value)
        {
            switch (code)
            {
                case 0x00: meters.M00 = value; break;
                case 0x01: meters.M01 = value; break;
                case 0x02: meters.M02 = value; break;
                case 0x03: meters.M03 = value; break;
                case 0x04: meters.M04 = value; break;
                case 0x05: meters.M05 = value; break;
                case 0x06: meters.M06 = value; break;
                case 0x07: meters.M07 = value; break;
                case 0x08: meters.M08 = value; break;
                case 0x09: meters.M09 = value; break;
                case 0x0A: meters.M0A = value; break;
                case 0x0B: meters.M0B = value; break;
                case 0x0C: meters.M0C = value; break;
                case 0x0D: meters.M0D = value; break;
                case 0x0E: meters.M0E = value; break;
                case 0x0F: meters.M0F = value; break;
                case 0x10: meters.M10 = value; break;
                case 0x11: meters.M11 = value; break;
                case 0x12: meters.M12 = value; break;
                case 0x13: meters.M13 = value; break;
                case 0x14: meters.M14 = value; break;
                case 0x15: meters.M15 = value; break;
                case 0x16: meters.M16 = value; break;
                case 0x17: meters.M17 = value; break;
                case 0x18: meters.M18 = value; break;
                case 0x19: meters.M19 = value; break;
                case 0x1A: meters.M1A = value; break;
                case 0x1B: meters.M1B = value; break;
                case 0x1C: meters.M1C = value; break;
                case 0x1D: meters.M1D = value; break;
                case 0x1E: meters.M1E = value; break;
                case 0x1F: meters.M1F = value; break;
                case 0x20: meters.M20 = value; break;
                case 0x21: meters.M21 = value; break;
                case 0x22: meters.M22 = value; break;
                case 0x23: meters.M23 = value; break;
                case 0x24: meters.M24 = value; break;
                case 0x25: meters.M25 = value; break;
                case 0x26: meters.M26 = value; break;
                case 0x27: meters.M27 = value; break;
                case 0x28: meters.M28 = value; break;
                case 0x29: meters.M29 = value; break;
                case 0x2A: meters.M2A = value; break;
                case 0x2B: meters.M2B = value; break;
                case 0x2C: meters.M2C = value; break;
                case 0x2D: meters.M2D = value; break;
                case 0x2E: meters.M2E = value; break;
                case 0x2F: meters.M2F = value; break;
                case 0x30: meters.M30 = value; break;
                case 0x31: meters.M31 = value; break;
                case 0x32: meters.M32 = value; break;
                case 0x33: meters.M33 = value; break;
                case 0x34: meters.M34 = value; break;
                case 0x35: meters.M35 = value; break;
                case 0x36: meters.M36 = value; break;
                case 0x37: meters.M37 = value; break;
                case 0x38: meters.M38 = value; break;
                case 0x39: meters.M39 = value; break;
                case 0x3A: meters.M3A = value; break;
                case 0x3B: meters.M3B = value; break;
                case 0x3C: meters.M3C = value; break;
                case 0x3D: meters.M3D = value; break;
                case 0x3E: meters.M3E = value; break;
                case 0x3F: meters.M3F = value; break;
                case 0x40: meters.M40 = value; break;
                case 0x41: meters.M41 = value; break;
                case 0x42: meters.M42 = value; break;
                case 0x43: meters.M43 = value; break;
                case 0x44: meters.M44 = value; break;
                case 0x45: meters.M45 = value; break;
                case 0x46: meters.M46 = value; break;
                case 0x47: meters.M47 = value; break;
                case 0x48: meters.M48 = value; break;
                case 0x49: meters.M49 = value; break;
                case 0x4A: meters.M4A = value; break;
                case 0x4B: meters.M4B = value; break;
                case 0x4C: meters.M4C = value; break;
                case 0x4D: meters.M4D = value; break;
                case 0x4E: meters.M4E = value; break;
                case 0x4F: meters.M4F = value; break;
                case 0x50: meters.M50 = value; break;
                case 0x51: meters.M51 = value; break;
                case 0x52: meters.M52 = value; break;
                case 0x53: meters.M53 = value; break;
                case 0x54: meters.M54 = value; break;
                case 0x55: meters.M55 = value; break;
                case 0x56: meters.M56 = value; break;
                case 0x57: meters.M57 = value; break;
                case 0x58: meters.M58 = value; break;
                case 0x59: meters.M59 = value; break;
                case 0x5A: meters.M5A = value; break;
                case 0x5B: meters.M5B = value; break;
                case 0x5C: meters.M5C = value; break;
                case 0x5D: meters.M5D = value; break;
                case 0x5E: meters.M5E = value; break;
                case 0x5F: meters.M5F = value; break;
                case 0x60: meters.M60 = value; break;
                case 0x61: meters.M61 = value; break;
                case 0x62: meters.M62 = value; break;
                case 0x63: meters.M63 = value; break;
                case 0x64: meters.M64 = value; break;
                case 0x65: meters.M65 = value; break;
                case 0x66: meters.M66 = value; break;
                case 0x67: meters.M67 = value; break;
                case 0x68: meters.M68 = value; break;
                case 0x69: meters.M69 = value; break;
                case 0x6A: meters.M6A = value; break;
                case 0x6B: meters.M6B = value; break;
                case 0x6C: meters.M6C = value; break;
                case 0x6D: meters.M6D = value; break;
                case 0x6E: meters.M6E = value; break;
                case 0x6F: meters.M6F = value; break;
                case 0x70: meters.M70 = value; break;
                case 0x71: meters.M71 = value; break;
                case 0x72: meters.M72 = value; break;
                case 0x73: meters.M73 = value; break;
                case 0x74: meters.M74 = value; break;
                case 0x75: meters.M75 = value; break;
                case 0x76: meters.M76 = value; break;
                case 0x77: meters.M77 = value; break;
                case 0x78: meters.M78 = value; break;
                case 0x79: meters.M79 = value; break;
                case 0x7A: meters.M7A = value; break;
                case 0x7B: meters.M7B = value; break;
                case 0x7C: meters.M7C = value; break;
                case 0x7D: meters.M7D = value; break;
                case 0x7E: meters.M7E = value; break;
                case 0x7F: meters.M7F = value; break;
                case 0x80: meters.M80 = value; break;
                case 0x81: meters.M81 = value; break;
                case 0x82: meters.M82 = value; break;
                case 0x83: meters.M83 = value; break;
                case 0x84: meters.M84 = value; break;
                case 0x85: meters.M85 = value; break;
                case 0x86: meters.M86 = value; break;
                case 0x87: meters.M87 = value; break;
                case 0x88: meters.M88 = value; break;
                case 0x89: meters.M89 = value; break;
                case 0x8A: meters.M8A = value; break;
                case 0x8B: meters.M8B = value; break;
                case 0x8C: meters.M8C = value; break;
                case 0x8D: meters.M8D = value; break;
                case 0x8E: meters.M8E = value; break;
                case 0x8F: meters.M8F = value; break;
                case 0x90: meters.M90 = value; break;
                case 0x91: meters.M91 = value; break;
                case 0x92: meters.M92 = value; break;
                case 0x93: meters.M93 = value; break;
                case 0xA0: meters.MA0 = value; break;
                case 0xA1: meters.MA1 = value; break;
                case 0xA2: meters.MA2 = value; break;
                case 0xA3: meters.MA3 = value; break;
                case 0xA4: meters.MA4 = value; break;
                case 0xA5: meters.MA5 = value; break;
                case 0xA6: meters.MA6 = value; break;
                case 0xA7: meters.MA7 = value; break;
                case 0xA8: meters.MA8 = value; break;
                case 0xA9: meters.MA9 = value; break;
                case 0xAA: meters.MAA = value; break;
                case 0xAB: meters.MAB = value; break;
                case 0xAC: meters.MAC = value; break;
                case 0xAD: meters.MAD = value; break;
                case 0xAE: meters.MAE = value; break;
                case 0xAF: meters.MAF = value; break;
                case 0xB0: meters.MB0 = value; break;
                case 0xB1: meters.MB1 = value; break;
                case 0xB8: meters.MB8 = value; break;
                case 0xB9: meters.MB9 = value; break;
                case 0xBA: meters.MBA = value; break;
                case 0xBB: meters.MBB = value; break;
                case 0xBC: meters.MBC = value; break;
                case 0xBD: meters.MBD = value; break;
                case 0xFA: meters.MFA = value; break;
                case 0xFB: meters.MFB = value; break;
                case 0xFC: meters.MFC = value; break;
                case 0xFD: meters.MFD = value; break;
                case 0xFE: meters.MFE = value; break;
                case 0xFF: meters.MFF = value; break;
                default:
                    break;
            }

        }

        public void UpdateMeter(string name, int value)
        {
            switch (name)
            {
                case "TotalBillMeterInDollars": meters.MTotalBillMeterInDollars = value; break;
                case "TrueCoinIn": meters.MTrueCoinIn = value; break;
                case "TrueCoinOut": meters.MTrueCoinOutMeter = value; break;
                case "BonusingDeductible": meters.MBonusingDeductible = value; break;
                case "BonusingNoDeductible": meters.MBonusingNoDeductible = value; break;
                case "BonusingWagerMatch": meters.MBonusingWagerMatch = value; break;
                case "BasicTotalConIn": meters.MBasicTotalCoinIn = value; break;
                case "BasicTotalCoinOut": meters.MBasicTotalCoinOut = value; break;
                case "BasicTotalDrop": meters.MBasicTotalDrop = value; break;
                case "BasicTotalJackPot": meters.MBasicTotalJackPot = value; break;
                case "BasicGamesPlayed": meters.MBasicGamesPlayed = value; break;
                case "BasicGamesWon": meters.MBasicGamesWon = value; break;
                case "BasicSlotDoorOpen": meters.MBasicSlotDoorOpen = value; break;
                case "BasicSlotDoorClose": meters.MBasicSlotDoorClose = value; break;
                case "BasicPowerReset": meters.MBasicPowerReset = value; break;
                case "BasicLogicDoorOpen": meters.MBasicLogicDoorOpen = value; break;
                case "BasicLogicDoorClose": meters.MBasicLogicDoorClose = value; break;
                case "BasicCashboxDoorOpen": meters.MBasicCashboxDoorOpen = value; break;
                case "BasicCashboxDoorClose": meters.MBasicCashboxDoorClose = value; break;
                case "BasicDropDoorOpen": meters.MBasicDropDoorOpen = value; break;
                case "BasicDropDoorClose": meters.MBasicDropDoorClose = value; break;
                case "BasicStackerOpen": meters.MBasicStackerOpen = value; break;
                case "BasicStackerClose": meters.MBasicStackerClose = value; break;
                case "BillsJammed": meters.MBillsJammed = value; break;
                case "SASInterfaceError": meters.MSASInterfaceError = value; break;
                case "TotalTilts": meters.MTotalTilts = value; break;
                case "GeneralTilt": meters.MGeneralTilt = value; break;

                default:
                    break;
            }

        }

        public int GetMeter(byte code)
        {
            switch (code)
            {
                case 0x00: return meters.M00;
                case 0x01: return meters.M01;
                case 0x02: return meters.M02;
                case 0x03: return meters.M03;
                case 0x04: return meters.M04;
                case 0x05: return meters.M05;
                case 0x06: return meters.M06;
                case 0x07: return meters.M07;
                case 0x08: return meters.M08;
                case 0x09: return meters.M09;
                case 0x0A: return meters.M0A;
                case 0x0B: return meters.M0B;
                case 0x0C: return meters.M0C;
                case 0x0D: return meters.M0D;
                case 0x0E: return meters.M0E;
                case 0x0F: return meters.M0F;
                case 0x10: return meters.M10;
                case 0x11: return meters.M11;
                case 0x12: return meters.M12;
                case 0x13: return meters.M13;
                case 0x14: return meters.M14;
                case 0x15: return meters.M15;
                case 0x16: return meters.M16;
                case 0x17: return meters.M17;
                case 0x18: return meters.M18;
                case 0x19: return meters.M19;
                case 0x1A: return meters.M1A;
                case 0x1B: return meters.M1B;
                case 0x1C: return meters.M1C;
                case 0x1D: return meters.M1D;
                case 0x1E: return meters.M1E;
                case 0x1F: return meters.M1F;
                case 0x20: return meters.M20;
                case 0x21: return meters.M21;
                case 0x22: return meters.M22;
                case 0x23: return meters.M23;
                case 0x24: return meters.M24;
                case 0x25: return meters.M25;
                case 0x26: return meters.M26;
                case 0x27: return meters.M27;
                case 0x28: return meters.M28;
                case 0x29: return meters.M29;
                case 0x2A: return meters.M2A;
                case 0x2B: return meters.M2B;
                case 0x2C: return meters.M2C;
                case 0x2D: return meters.M2D;
                case 0x2E: return meters.M2E;
                case 0x2F: return meters.M2F;
                case 0x30: return meters.M30;
                case 0x31: return meters.M31;
                case 0x32: return meters.M32;
                case 0x33: return meters.M33;
                case 0x34: return meters.M34;
                case 0x35: return meters.M35;
                case 0x36: return meters.M36;
                case 0x37: return meters.M37;
                case 0x38: return meters.M38;
                case 0x39: return meters.M39;
                case 0x3A: return meters.M3A;
                case 0x3B: return meters.M3B;
                case 0x3C: return meters.M3C;
                case 0x3D: return meters.M3D;
                case 0x3E: return meters.M3E;
                case 0x3F: return meters.M3F;
                case 0x40: return meters.M40;
                case 0x41: return meters.M41;
                case 0x42: return meters.M42;
                case 0x43: return meters.M43;
                case 0x44: return meters.M44;
                case 0x45: return meters.M45;
                case 0x46: return meters.M46;
                case 0x47: return meters.M47;
                case 0x48: return meters.M48;
                case 0x49: return meters.M49;
                case 0x4A: return meters.M4A;
                case 0x4B: return meters.M4B;
                case 0x4C: return meters.M4C;
                case 0x4D: return meters.M4D;
                case 0x4E: return meters.M4E;
                case 0x4F: return meters.M4F;
                case 0x50: return meters.M50;
                case 0x51: return meters.M51;
                case 0x52: return meters.M52;
                case 0x53: return meters.M53;
                case 0x54: return meters.M54;
                case 0x55: return meters.M55;
                case 0x56: return meters.M56;
                case 0x57: return meters.M57;
                case 0x58: return meters.M58;
                case 0x59: return meters.M59;
                case 0x5A: return meters.M5A;
                case 0x5B: return meters.M5B;
                case 0x5C: return meters.M5C;
                case 0x5D: return meters.M5D;
                case 0x5E: return meters.M5E;
                case 0x5F: return meters.M5F;
                case 0x60: return meters.M60;
                case 0x61: return meters.M61;
                case 0x62: return meters.M62;
                case 0x63: return meters.M63;
                case 0x64: return meters.M64;
                case 0x65: return meters.M65;
                case 0x66: return meters.M66;
                case 0x67: return meters.M67;
                case 0x68: return meters.M68;
                case 0x69: return meters.M69;
                case 0x6A: return meters.M6A;
                case 0x6B: return meters.M6B;
                case 0x6C: return meters.M6C;
                case 0x6D: return meters.M6D;
                case 0x6E: return meters.M6E;
                case 0x6F: return meters.M6F;
                case 0x70: return meters.M70;
                case 0x71: return meters.M71;
                case 0x72: return meters.M72;
                case 0x73: return meters.M73;
                case 0x74: return meters.M74;
                case 0x75: return meters.M75;
                case 0x76: return meters.M76;
                case 0x77: return meters.M77;
                case 0x78: return meters.M78;
                case 0x79: return meters.M79;
                case 0x7A: return meters.M7A;
                case 0x7B: return meters.M7B;
                case 0x7C: return meters.M7C;
                case 0x7D: return meters.M7D;
                case 0x7E: return meters.M7E;
                case 0x7F: return meters.M7F;
                case 0x80: return meters.M80;
                case 0x81: return meters.M81;
                case 0x82: return meters.M82;
                case 0x83: return meters.M83;
                case 0x84: return meters.M84;
                case 0x85: return meters.M85;
                case 0x86: return meters.M86;
                case 0x87: return meters.M87;
                case 0x88: return meters.M88;
                case 0x89: return meters.M89;
                case 0x8A: return meters.M8A;
                case 0x8B: return meters.M8B;
                case 0x8C: return meters.M8C;
                case 0x8D: return meters.M8D;
                case 0x8E: return meters.M8E;
                case 0x8F: return meters.M8F;
                case 0x90: return meters.M90;
                case 0x91: return meters.M91;
                case 0x92: return meters.M92;
                case 0x93: return meters.M93;
                case 0xA0: return meters.MA0;
                case 0xA1: return meters.MA1;
                case 0xA2: return meters.MA2;
                case 0xA3: return meters.MA3;
                case 0xA4: return meters.MA4;
                case 0xA5: return meters.MA5;
                case 0xA6: return meters.MA6;
                case 0xA7: return meters.MA7;
                case 0xA8: return meters.MA8;
                case 0xA9: return meters.MA9;
                case 0xAA: return meters.MAA;
                case 0xAB: return meters.MAB;
                case 0xAC: return meters.MAC;
                case 0xAD: return meters.MAD;
                case 0xAE: return meters.MAE;
                case 0xAF: return meters.MAF;
                case 0xB0: return meters.MB0;
                case 0xB1: return meters.MB1;
                case 0xB8: return meters.MB8;
                case 0xB9: return meters.MB9;
                case 0xBA: return meters.MBA;
                case 0xBB: return meters.MBB;
                case 0xBC: return meters.MBC;
                case 0xBD: return meters.MBD;
                case 0xFA: return meters.MFA;
                case 0xFB: return meters.MFB;
                case 0xFC: return meters.MFC;
                case 0xFD: return meters.MFD;
                case 0xFE: return meters.MFE;
                case 0xFF: return meters.MFF;
                default:
                    return 0;
                    break;
            }

        }

        public int GetMeter(string name)
        {
            switch (name)
            {
                case "TotalBillMeterInDollars": return meters.MTotalBillMeterInDollars;
                case "TrueCoinIn": return meters.MTrueCoinIn;
                case "TrueCoinOut": return meters.MTrueCoinOutMeter;
                case "BonusingDeductible": return meters.MBonusingDeductible;
                case "BonusingNoDeductible": return meters.MBonusingNoDeductible;
                case "BonusingWagerMatch": return meters.MBonusingWagerMatch;
                case "BasicTotalConIn": return meters.MBasicTotalCoinIn;
                case "BasicTotalCoinOut": return meters.MBasicTotalCoinOut;
                case "BasicTotalDrop": return meters.MBasicTotalDrop;
                case "BasicTotalJackPot": return meters.MBasicTotalJackPot;
                case "BasicGamesPlayed": return meters.MBasicGamesPlayed;
                case "BasicGamesWon": return meters.MBasicGamesWon;
                case "BasicSlotDoorOpen": return meters.MBasicSlotDoorOpen;
                case "BasicSlotDoorClose": return meters.MBasicSlotDoorClose;
                case "BasicPowerReset": return meters.MBasicPowerReset;
                case "BasicLogicDoorOpen": return meters.MBasicLogicDoorOpen;
                case "BasicLogicDoorClose": return meters.MBasicLogicDoorClose;
                case "BasicCashboxDoorOpen": return meters.MBasicCashboxDoorOpen;
                case "BasicCashboxDoorClose": return meters.MBasicCashboxDoorClose;
                case "BasicDropDoorOpen": return meters.MBasicDropDoorOpen;
                case "BasicDropDoorClose": return meters.MBasicDropDoorClose;
                case "BasicStackerOpen": return meters.MBasicStackerOpen;
                case "BasicStackerClose": return meters.MBasicStackerClose;
                case "BillsJammed": return meters.MBillsJammed;
                case "SASInterfaceError": return meters.MSASInterfaceError;
                case "TotalTilts": return meters.MTotalTilts;
                case "GeneralTilt": return meters.MGeneralTilt;

                default: return 0;
            }
        }

        public static EGMAccounting GetInstance()
        {
            if (egmaccounting_ == null)
            {
                egmaccounting_ = new EGMAccounting();
            }
            return egmaccounting_;
        }


    }
}
