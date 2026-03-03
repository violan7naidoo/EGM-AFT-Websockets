using EGMENGINE.EGMStatusModule;
using EGMENGINE.EGMStatusModule.AFTTransferModule;
using EGMENGINE.GPIOCTL;
using EGMENGINE.SASCTLModule.SASClient;
using EGMENGINE.SASCTLModule.SASClient.DevSASClient;
using EGMENGINE.SASCTLModule.SASClient.GanlotSASClient;
using EGMENGINE.SASCTLModule.SASClient.GanlotSASClient.GXGSASAPIWrap;
using System;
using System.Linq;
using System.Net;
using System.Timers;
using System.Transactions;
using GLU16 = System.UInt16;
using GLU32 = System.UInt32;
using GLU64 = System.UInt64;
using GLU8 = System.Byte;

namespace EGMENGINE.SASCTLModule
{
    #region enum
    internal enum BILL_ACCEPTOR_ACTION_FLAG
    {
        DISABLE_BILL_ACCEPTOR_AFTER_EACH_ACCEPTED_BILL = 0,
        KEEP_BILL_ACCEPTOR_ENABLED_AFTER_EACH_ACCEPTED_BILL = 1
    }
    internal enum ENABLE_JACKPOT_HANDPAY_RESET_METHOD
    {
        STANDARD_HANDPAY = 0,
        RESET_TO_THE_CREDIT_METER = 1
    }

    internal enum ERROR_CODE : GLU32
    {
        ERR_NONE = 0,
        ERR_DEV_NOT_INIT = 0x100,       // Call API without initialize library
        ERR_INVALID_PARAM = 0x101,      // Invalid parameter
        ERR_FAIL_ALLOC = 0x102,     // Could not allocate memory
        ERR_FAIL_DEALLOC = 0x103,       // Could not deallocate memory
        ERR_NO_DEV = 0x104,     // The device is not found or not support
        ERR_NO_PERN = 0x105,        // No permission 
        ERR_DRV_FAIL = 0x106,       // System call fail
        ERR_IOC_NO_IMPL = 0x107,        // Non implement Ioctl message

        ERR_INTR_IS_ENABLED = 0x200,        // Interrupt is enabled
        ERR_INTR_IS_DISABLED = 0x201,       // Interrupt is disabled
        ERR_INTR_IS_REGISTERED = 0x202,     // Interrupt is registered
        ERR_INTR_IS_UNREGISTERED = 0x203,       // Interrupt is unregistered

        ERR_AES_TIMEOUT = 0x300,        // Timeout for handling the AES command
        ERR_SPI_TIMEOUT = 0x301,        // Timeout for handling the SPI command

        ERR_I2C_BUSY = 0x400,       // SMBUS/I2C bus is busy
        ERR_I2C_TIMEOUT = 0x401,        // SMBUS/I2C transaction timeout
        ERR_I2C_TRANS_FAIL = 0x402,     // Someone kill SMBUS transaction
        ERR_I2C_BUS_ERR = 0x403,        // SMBUS/I2C bus collision
        ERR_I2C_DEV_ERR = 0x404,        // SMBUS/I2C slave device error
        ERR_I2C_NO_SUP = 0x405,     // Reserved
        ERR_I2C_NO_DATA = 0x406,        // SMBUS/I2C slave device error

        ERR_PIC_DATA_INVALID = 0x500,       // PIC data format is not correct
        ERR_PIC_COMM_FAIL = 0x501,      // PIC response 0xE0
        ERR_PIC_DATA_CRC_FAIL = 0x502,      // Reserved
        ERR_PIC_LOG_EMPTY = 0x503,      // PIC data is empty

        ERR_PIC_NOT_SUPPORT = 0x504,        // This function not support on PIC old architecture
        ERR_PMU_NOT_SUPPORT = 0x505,        // This function not support on PIC new architecture

        //== Error code from PIC (for new architecture of PIC)========================================
        ERR_PMU_PKG_FORMAT = 0x511,
        ERR_PMU_PKG_CS = 0x512,
        ERR_PMU_PKG_LEN = 0x513,
        ERR_PMU_SYSTEM_IS_BUSY = 0x514,

        ERR_PMU_UNKOWN_CMD = 0x517,
        ERR_PMU_WRONG_PARAM = 0x518,
        ERR_PMU_CMD_NOT_SUPPORT = 0x519,

        ERR_PMU_NO_RECORD = 0x521,      // PMU data is empty
        ERR_PMU_CLR_LOG = 0x522,
        ERR_PMU_WRONG_EVENT_TYPE = 0x523,
        ERR_PMU_ID_CRC = 0x531,
        ERR_PMU_DEV_AVAIL = 0x532,
        ERR_PMU_INT_ENABLED = 0x541,
        ERR_PMU_INT_DISABLED = 0x542,
        ERR_PMU_INT_NOT_ENA = 0x543,
        ERR_PMU_INT_NO_EVENT_HAPPENED = 0x544,
        ERR_PMU_OLD_IS_RUNNING = 0x551,
        ERR_PMU_NOT_IN_OLD = 0x552,
        //===========================================================================================
        ERR_SPI_COMM_FAIL = 0x600,      // SPI communication is not correct
        ERR_SPI_SEC_WRONG_RSP = 0x601,
        ERR_SPI_SEC_WRONG_CS = 0x602,
        ERR_SPI_SEC_RSP_NAK = 0x603,
        ERR_SPI_SEC_ERR_CS = 0x604,
        ERR_SPI_SEC_ERR_BUSY = 0x605,
        ERR_SPI_SEC_ERR_DATA = 0x606,
        ERR_SPI_SEC_ERR_CMD = 0x607,
        ERR_SPI_SEC_ERR_EEPROM_RW = 0x608,
        ERR_SPI_SEC_ERR_FP_NOT_SET = 0x609,
        ERR_SPI_SEC_ERR_FW = 0x60A,
        ERR_SPI_SEC_ERR_EEPROM_BUS = 0x60B,
        ERR_SPI_SEC_ERR_RX_OVERFLOW = 0x60C,
        ERR_SPI_SEC_ERR_ONE_EEPROM_CS = 0x60D,
        ERR_SPI_SEC_ERR_TWO_EEPROM_CS = 0x60E,
        ERR_SPI_SEC_ERR_TIMEOUT = 0x60F,
        ERR_SPI_SEC_ERR_UNKNOWN = 0x610,

        ERR_SAS_NO_RESPONSE = 0x3001,       // No response from GXG SAS processor
        ERR_SAS_NO_ROM_SIG_REQUEST = 0x4001,
        ERR_SAS_GAME_START_WHILE_GAME_MENU = 0x4002,
        ERR_SAS_WAGER_EXCEED_MAX_BET = 0x4003,
        ERR_SAS_EXCEED_IMPLEMENT_GAME_NUM = 0x4004,
        ERR_SAS_RTC_NO_SYNC = 0x4005,
        ERR_SAS_ROM_SIG_SEED_NOT_MATCH = 0x4006,

        ERR_SAS_MD_NOT_SUP = 0x4801,
        ERR_SAS_MD_SAS_DENOM_NOT_SET = 0x4802,
        ERR_SAS_MD_NOT_MULTI_OF_SAS_DENOM = 0x4803,
        ERR_SAS_MD_CODE_IS_ENABLED = 0x4804,
        ERR_SAS_MD_CODE_IS_DISABLED = 0x4805,
        ERR_SAS_MD_CODE_ALREADY_ADD = 0x4806,
        ERR_SAS_MD_CODE_NOT_PRESENT = 0x4807,
        ERR_SAS_MD_CODE_IS_USED = 0x4808,

        ERR_SAS_LEGACY_BONUS_AMOUNT_NOT_MATCH = 0x5001,
        ERR_SAS_LEGACY_BONUS_NOT_CFG = 0x5002,

        ERR_SAS_HANDPAY_BUSY = 0x5801,
        ERR_SAS_HANDPAY_ALREADY_RESET = 0x5802,
        ERR_SAS_HANDPAY_RECEIPT_NOT_CFG = 0x5803,

        ERR_SAS_PREP_PRINT_BUSY = 0x6001,
        ERR_SAS_PREP_PRINT_NO_VALID_SEC_ID = 0x6002,
        ERR_SAS_PREP_PRINT_LINK_DOWN = 0x6003,
        ERR_SAS_PREP_PRINT_NOT_ALLOW = 0x6004,

        ERR_SAS_SEC_VALID_NUM_NOT_SET = 0x6401,

        ERR_SAS_REDEEM_PRE_NOT_FINISH = 0x6801,
        ERR_SAS_REDEEM_WHILE_LINK_DOWN = 0x6802,
        ERR_SAS_REDEEM_NOT_CFG = 0x6803,
        ERR_SAS_REDEEM_ALREADY_REJECT = 0x6804,
        ERR_SAS_REDEEM_NOT_ALLOW_FINISH = 0x6805,

        ERR_SAS_AFT_NOT_SUP = 0x7001,
        ERR_SAS_AFT_REG_ZERO_ASSET_NUM = 0x7002,
        ERR_SAS_AFT_REG_BUSY = 0x7003,
        ERR_SAS_AFT_REG_NO_PERN = 0x7004,
        ERR_SAS_AFT_LOCK_CONDITION_NOT_MATCH = 0x7011,
        ERR_SAS_AFT_LOCK_NO_PERN = 0x7012,
        ERR_SAS_AFT_TRANS_HOST_CANCELLED = 0x7021,
        ERR_SAS_AFT_TRANS_NO_PERN = 0x7022,
        ERR_SAS_AFT_TRANS_NO_PENDING_DATA = 0x7023,
        ERR_SAS_AFT_TRANS_AMOUNT_NOT_MATCH = 0x7024,
        ERR_SAS_AFT_TRANS_AMOUNT_EXCEED_LIMIT = 0x7025,
        ERR_SAS_AFT_TRANS_HOST_CASHOUT_NOT_ENABLED = 0x7026,
        ERR_SAS_AFT_TRANS_BUSY = 0x7027,

        ERR_SAS_PROG_QUEUE_IS_NOT_EMPTY = 0x8001,
        ERR_SAS_PROG_NO_ANY_ENABLED_LEVEL = 0x8002,
        ERR_SAS_PROG_IS_NOT_ENABLED = 0x8003,
        ERR_SAS_PROG_HIT_AMOUNT_NOT_MATCH = 0x8004,

        ERR_SAS_NVRAM_DATA_CORRUPT = 0xF001,

        ERR_SAS_TXRX_MSG_QUEUE_EMPTY = 0xF801,
        ERR_SAS_NOT_ENABLE_TRACE_TXRX = 0xF802,

        // For driver/api internal used
        ERR_INTR_QUEUE_EMPTY = 0xFF000,
    }

    internal enum SAS_PORT : GLU8
    {
        SAS_PORT_1 = 0,
        SAS_PORT_2 = 1,
        SAS_PORT_3 = 2,
        SAS_PORT_ALL = 0xFF,
    };


    internal sealed class BITMASK_SAS_EGM_AFT_STATUS
    {
        internal static readonly ulong PRINTER_AVAIL_FOR_RECEIPT = BIT_N(0);
        internal static readonly ulong TO_HOST_PARTIAL_AMOUNT_AVAIL = BIT_N(1);
        internal static readonly ulong SUP_CUSTOM_TICKET_DATA = BIT_N(2);
        internal static readonly ulong AFT_REGISTERED = BIT_N(3);
        internal static readonly ulong ENABLE_INHOUSE_TRANS = BIT_N(4);
        internal static readonly ulong ENABLE_BONUS_TRANS = BIT_N(5);
        internal static readonly ulong ENABLE_DEBIT_TRANS = BIT_N(6);
        internal static readonly ulong ENABLE_ANY_AFT = BIT_N(7);

        private static ulong BIT_N(int n)
        {
            return ((ulong)0x01 << (n));
        }
    }

    internal sealed class BITMASK_SAS_EGM_AVAIL_TRANS_STATUS
    {
        internal static readonly ulong TO_EGM_OK = BIT_N(0);
        internal static readonly ulong FROM_EGM_OK = BIT_N(1);
        internal static readonly ulong TO_PRINTER_OK = BIT_N(2);
        internal static readonly ulong WIN_AMOUNT_PENDING_CASHOUT_TO_HOST = BIT_N(3);
        internal static readonly ulong BONUS_TO_EGM_OK = BIT_N(4);

        private static ulong BIT_N(int n)
        {
            return ((ulong)0x01 << (n));
        }
    };

    internal sealed class BITMASK_SAS_EGM_HOST_CASHOUT_STATUS
    {

        internal static readonly ulong CTRL_BY_HOST = BIT_N(0);
        internal static readonly ulong ENABLED = BIT_N(1);
        internal static readonly ulong HARD_MODE = BIT_N(2);

        private static ulong BIT_N(int n)
        {
            return ((ulong)0x01 << (n));
        }
    };



    internal enum SAS_METER_TYPE
    {
        SAS_METER_TYPE_MACHINE = 0,
        SAS_METER_TYPE_PER_GAME,
        SAS_METER_TYPE_PER_DENOM,
    };

    internal enum SAS_METER_SET_MODE
    {
        SAS_METER_MODE_INCREASE = 0,
        SAS_METER_MODE_DECREASE,
        SAS_METER_MODE_REPLACE,
        SAS_METER_MODE_RESET,
    };

    internal enum SAS_WON_METER_TYPE
    {
        SAS_WON_METER_TYPE_PAYTABLE_WIN_WITHOUT_HANDPAY = 0,
        SAS_WON_METER_TYPE_PROG_WIN_WITHOUT_HANDPAY = 1,
        SAS_WON_METER_TYPE_BONUS_WIN_WITHOUT_HANDPAY = 2,
        SAS_WON_METER_TYPE_PAYTABLE_WIN_WITH_HANDPAY = 3,
        SAS_WON_METER_TYPE_PROG_WIN_WITH_HANDPAY = 4,
        SAS_WON_METER_TYPE_BONUS_WIN_WITH_HANDPAY = 5,
    };

    internal enum SAS_CREDIT_TYPE
    {
        SAS_CREDIT_CASHABLE = 0,
        SAS_CREDIT_RESTRICTED = 1,
        SAS_CREDIT_NONRESTRICTED = 2,
    };

    internal enum SAS_ROM_SIG_MECHA
    {
        SAS_ROM_SIG_MECHA_STANDARD = 0,
        SAS_ROM_SIG_MECHA_REBOOT_REMAIN = 1,
    };

    internal enum SAS_HANDPAY_LEVEL
    {
        SAS_HANDPAY_LEVEL_NON_PROG_WIN = 0x00,
        SAS_HANDPAY_LEVEL_NON_PROG_TOP_WIN = 0x40,
        SAS_HANDPAY_LEVEL_CANCELLED_CREDITS = 0x80,
    };

    internal enum SAS_HANDPAY_VALID_TYPE
    {
        SAS_HANDPAY_VALID_JACKPOT = 0,
        SAS_HANDPAY_VALID_CASHOUT = 1,
        SAS_HANDPAY_VALID_JACKPOT_WITH_PROG_AMOUNT = 2,
    };

    internal enum SAS_HANDPAY_MODE : GLU8
    {
        SAS_HANDPAY_MODE_NORMAL = 0,
        SAS_HANDPAY_MODE_LEGACY = 1,
    };

    internal enum SAS_TICKET_REDEEM_RESULT
    {
        SAS_TICKET_REDEEM_RESULT_PENDING = 0,
        SAS_TICKET_REDEEM_RESULT_HOST_REJECT = 1,
        SAS_TICKET_REDEEM_RESULT_LP70_TIMEOUT = 2,
        SAS_TICKET_REDEEM_RESULT_LP71_TIMEOUT = 3,
        SAS_TICKET_REDEEM_RESULT_LINK_DOWN = 4,
    };

    internal enum SAS_COMP_REDEEM_STATUS
    {
        SAS_COMP_REDEEM_STATUS_CASHABLE_TICKET_REDEEMED = 0x00,
        SAS_COMP_REDEEM_STATUS_RES_TICKET_REDEEMED = 0x01,
        SAS_COMP_REDEEM_STATUS_NONRES_TICKET_REDEEMED = 0x02,
        SAS_COMP_REDEEM_STATUS_REJECT_BY_HOST = 0x80,
        SAS_COMP_REDEEM_STATUS_VALID_NUM_NOT_MATCH = 0x81,
        SAS_COMP_REDEEM_STATUS_NOT_VALID_TRANS_FUNC = 0x82,
        SAS_COMP_REDEEM_STATUS_NOT_VALID_AMOUNT = 0x83,
        SAS_COMP_REDEEM_STATUS_AMOUNT_EXCEED_CREDIT_LIMIT = 0x84,
        SAS_COMP_REDEEM_STATUS_AMOUNT_NOT_EVEN_MULTIPLE_OF_DENOM = 0x85,
        SAS_COMP_REDEEM_STATUS_AMOUNT_NOT_MATCH = 0x86,
        SAS_COMP_REDEEM_STATUS_UNABLE_ACCEPT_THIS_TIME = 0x87,
        SAS_COMP_REDEEM_STATUS_TIMEOUT = 0x88,
        SAS_COMP_REDEEM_STATUS_LINK_DOWN = 0x89,
        SAS_COMP_REDEEM_STATUS_REDEEM_DISABLED = 0x8A,
        SAS_COMP_REDEEM_STATUS_VALIDATOR_FAILURE = 0x8B,
    };

    internal enum SAS_TICKET_PREP_PRINT_TYPE
    {
        SAS_TICKET_PREP_PRINT_TYPE_CASHABLE = 0,
        SAS_TICKET_PREP_PRINT_TYPE_RESTRICTED = 1,
    };

    internal enum SAS_TICKET_PREP_PRINT_RESULT
    {
        SAS_TICKET_PREP_PRINT_RESULT_HOST_ALLOW = 0,
        SAS_TICKET_PREP_PRINT_RESULT_HOST_REJECT = 3,
        SAS_TICKET_PREP_PRINT_RESULT_HOST_TIMEOUT = 4,
        SAS_TICKET_PREP_PRINT_RESULT_LINK_DOWN = 5,
    };

    internal enum SAS_TICKET_TYPE
    {
        SAS_TICKET_TYPE_CASHABLE_CASHOUT = 0x00,
        SAS_TICKET_TYPE_RESTRICTED_CASHOUT = 0x01,
        SAS_TICKET_TYPE_INHOUSE_CASHABLE_AFT = 0x02,
        SAS_TICKET_TYPE_INHOUSE_RESTRICTED_AFT = 0x03,
        SAS_TICKET_TYPE_DEBIT_AFT = 0x04,
        SAS_TICKET_TYPE_CASHABLE_REDEEM = 0x80,
        SAS_TICKET_TYPE_RESTRICTED_REDEEM = 0x81,
        SAS_TICKET_TYPE_NONRESTRICTED_REDEEM = 0x82,
    };

    internal enum SAS_AFT_REG_ACTION
    {
        SAS_AFT_REG_ACTION_REQUEST = 0,
        SAS_AFT_REG_ACTION_CANCEL,
        SAS_AFT_REG_ACTION_ACK,
    };

    internal enum SAS_AFT_LOCK_ACTION
    {
        SAS_AFT_LOCK_ACTION_UNLOCKED = 0,
        SAS_AFT_LOCK_ACTION_LOCKED = 1,
    };

    internal enum SAS_AFT_EGM_STATUS
    {
        SAS_AFT_EGM_AVAIL_TRANS_STATUS = 0,
        SAS_AFT_EGM_AFT_STATUS = 1,
        SAS_AFT_EGM_HOST_CASHOUT_STATUS = 2,
    };

    internal enum SAS_AFT_HOST_CASHOUT_TYPE
    {
        SAS_AFT_HOST_CASHOUT_TYPE_CASHOUT = 0,
        SAS_AFT_HOST_CASHOUT_TYPE_CASHOUT_WIN = 1,
    };

    internal enum SAS_AFT_TRANS_TYPE
    {
        SAS_AFT_TRANS_TYPE_INHOUSE_TO_EGM = 0x00,
        SAS_AFT_TRANS_TYPE_BONUS_COIN_OUT_WIN_TO_EGM = 0x10,
        SAS_AFT_TRANS_TYPE_BONUS_JACKPOT_WIN_TO_EGM = 0x11,
        SAS_AFT_TRANS_TYPE_INHOUSE_TO_TICKET = 0x20,
        SAS_AFT_TRANS_TYPE_DEBIT_TO_EGM = 0x40,
        SAS_AFT_TRANS_TYPE_DEBIT_TO_TICKET = 0x60,
        SAS_AFT_TRANS_TYPE_INHOUSE_TO_HOST = 0x80,
        SAS_AFT_TRANS_TYPE_WIN_AMOUNT_TO_HOST = 0x90,
    };

    internal enum SAS_AFT_REJECT_STATUS
    {
        SAS_AFT_REJECT_STATUS_HOST_CASHOUT_TIMEOUT = 0x00,
        SAS_AFT_REJECT_STATUS_HOST_CASHOUT_CANCEL_BY_HOST = 0x01,
        SAS_AFT_REJECT_STATUS_HOST_CASHOUT_REJECT_BY_HOST = 0x02,
    };

    internal enum SAS_AFT_COMP_TRANS_STATUS
    {
        SAS_AFT_COMP_TRANS_STATUS_FULL_TRANS_SUCCESS = 0x00,
        SAS_AFT_COMP_TRANS_STATUS_PARTIAL_TRANS_SUCCESS = 0x01,
        SAS_AFT_COMP_TRANS_STATUS_CANCEL_BY_HOST = 0x80,
        SAS_AFT_COMP_TRANS_STATUS_TRANSACTION_ID_NOT_UNIQUE = 0x81,
        SAS_AFT_COMP_TRANS_STATUS_NOT_VALID_TRANS_FUNC = 0x82,
        SAS_AFT_COMP_TRANS_STATUS_NOT_VALID_AMOUNT_EXPIR = 0x83,
        SAS_AFT_COMP_TRANS_STATUS_AMOUNT_EXCEED_TRANS_LIMIT = 0x84,
        SAS_AFT_COMP_TRANS_STATUS_AMOUNT_NOT_EVEN_MULTIPLE_OF_DENOM = 0x85,
        SAS_AFT_COMP_TRANS_STATUS_UNABLE_DO_PARTIAL_TRANS_TO_HOST = 0x86,
        SAS_AFT_COMP_TRANS_STATUS_UNABLE_DO_TRANS_THIS_TIME = 0x87,
        SAS_AFT_COMP_TRANS_STATUS_EGM_NOT_REGISTERED = 0x88,
        SAS_AFT_COMP_TRANS_STATUS_REG_KEY_NOT_MATCH = 0x89,
        SAS_AFT_COMP_TRANS_STATUS_NO_POS_ID = 0x8A,
        SAS_AFT_COMP_TRANS_STATUS_NO_WON_CREDIT_AVAIL_FOR_CASHOUT = 0x8B,
        SAS_AFT_COMP_TRANS_STATUS_NO_DENOM_SET = 0x8C,
        SAS_AFT_COMP_TRANS_STATUS_EXPIR_NOT_VALID_FOR_TICKET = 0x8D,
        SAS_AFT_COMP_TRANS_STATUS_TRANS_TO_TICKET_DEVICE_NOT_AVAIL = 0x8E,
        SAS_AFT_COMP_TRANS_STATUS_RESTRICTED_POOL_NOT_MATCH = 0x8F,
        SAS_AFT_COMP_TRANS_STATUS_UNABLE_PRINT_RECEIPT = 0x90,
        SAS_AFT_COMP_TRANS_STATUS_RECEIPT_DATA_MISSING = 0x91,
        SAS_AFT_COMP_TRANS_STATUS_RECEIPT_NOT_ALLOW_FOR_BONUS_TRANS = 0x92,
        SAS_AFT_COMP_TRANS_STATUS_ASSET_NUM_NOT_SET_OR_NOT_MATCH = 0x93,
        SAS_AFT_COMP_TRANS_STATUS_NOT_LOCKED = 0x94,
        SAS_AFT_COMP_TRANS_STATUS_TRANSACTION_ID_NOT_VALID = 0x95,
        SAS_AFT_COMP_TRANS_STATUS_UNEXPECTED_ERR = 0x9F,
    };

    internal enum SAS_INTR_TYPE
    {
        SAS_INTR_FINISH_SYNC = 0x01,
        SAS_INTR_LINK_DOWN = 0x02,
        SAS_INTR_HOST_SET_RTE_MODE = 0x03,
        SAS_INTR_EC_QUEUE_FULL = 0x04,

        SAS_INTR_MACHINE_STATUS_CHANGE = 0x10,
        SAS_INTR_ROM_SIG_REQUEST = 0x11,
        SAS_INTR_BILL_DENOM_STATUS_CHANGE = 0x12,
        SAS_INTR_EN_DIS_GAME_N = 0x13,
        SAS_INTR_GAME_DELAY = 0x14,
        SAS_INTR_SET_DATE_TIME = 0x15,
        SAS_INTR_ENABLE_BA_REQUEST = 0x16,

        SAS_INTR_TICKET_REDEEM_RESULT = 0x20,
        SAS_INTR_TICKET_REDEEM_FINISHED = 0x21,

        SAS_INTR_EXT_VALID_STATUS_CHANGE = 0x30,
        SAS_INTR_PREP_TICKET_PRINT_RESULT = 0x32,
        SAS_INTR_VALID_QUEUE_FULL = 0x33,
        SAS_INTR_SEC_VALID_NUM_SET = 0x34,
        SAS_INTR_HOST_CANCEL_VALID_CFG = 0x35,

        SAS_INTR_GET_LEGACY_BONUS = 0x40,

        SAS_INTR_GET_AFT_REGISTER_INFO = 0x50,
        SAS_INTR_GET_AFT_LOCK_INFO = 0x51,

        SAS_INTR_AFT_TRANS_PENDING = 0x53,
        SAS_INTR_AFT_TRANS_FINISHED = 0x54,
        SAS_INTR_AFT_HOST_CASHOUT_STATUS_CHANGE = 0x55,
        SAS_INTR_AFT_TRANS_REJECT = 0x56,
        SAS_INTR_AFT_TRANS_HOST_REQ_CANCEL = 0x57,

        SAS_INTR_PROG_WIN_RECORD_QUEUE_FULL = 0x60,
        SAS_INTR_PROG_LINK_STATUS_CHANGE = 0x61,

        SAS_INTR_DETECT_NVRAM_ERROR = 0xF0,
    }

    /* Start Define SAS Status */
    internal enum SAS_STATUS_TYPE : GLU8
    {
        SAS_STATUS_LINK = 0,
        SAS_STATUS_ROM_SIG = 1,
        SAS_STATUS_EXCEPTION_QUEUE = 2,
        SAS_STATUS_HANDPAY_PROC = 3,
        SAS_STATUS_HANDPAY_QUEUE = 4,
        SAS_STATUS_TI_PROC = 5,
        SAS_STATUS_PREP_TO_PROC = 6,
        SAS_STATUS_VALID_RECORD_QUEUE = 7,
        SAS_STATUS_LEGACY_BONUS = 8,
        SAS_STATUS_MACHINE_CFG = 9,
        SAS_STATUS_BILL_DENOM_CFG = 10,
        SAS_STATUS_DATE_TIME = 11,

        SAS_STATUS_SEC_VALID_NUM = 13,
        SAS_STATUS_GAME_ENABLE = 14,
        SAS_STATUS_AFT_REGISTER = 15,
        SAS_STATUS_AFT_LOCK = 16,
        SAS_STATUS_AFT_TRANS_PROC = 17,

        SAS_STATUS_PROG_QUEUE = 30,
        SAS_STATUS_PROG_LINK = 31,

        SAS_STATUS_NVRAM = 99,
        SAS_STATUS_END,
    };

    internal enum SAS_LINK_STATUS : GLU8
    {
        SAS_LINK_STATUS_NORMAL = 0,
        SAS_LINK_STATUS_NO_SYNC = 1,
        SAS_LINK_STATUS_LINK_DOWN_5S_NO_ADDR = 2,
        SAS_LINK_STATUS_LINK_DOWN_30S_NO_ACK = 3,
    };

    internal enum SAS_ROM_SIG_STATUS
    {
        SAS_ROM_SIG_STATUS_IDLE = 0,
        SAS_ROM_SIG_STATUS_REQ = 1,
    };

    internal enum SAS_HP_PROC_STATUS
    {
        SAS_HP_PROC_STATUS_IDLE = 0,
        SAS_HP_PROC_STATUS_WAIT_RESET = 1,
    };

    internal enum SAS_TI_PROC_STATUS
    {
        SAS_TI_PROC_STATUS_IDLE = 0,
        SAS_TI_PROC_STATUS_WAIT_HOST = 1,
        SAS_TI_PROC_STATUS_PENDING = 2,
        SAS_TI_PROC_STATUS_WAIT_FINISH = 3,
        SAS_TI_PROC_STATUS_HOST_REJECT = 4,
        SAS_TI_PROC_STATUS_HOST_TIMEOUT = 5,
        SAS_TI_PROC_STATUS_LINK_DOWN = 6,
    };

    internal enum SAS_PREP_TO_PROC_STATUS
    {
        SAS_PREP_TO_PROC_STATUS_IDLE = 0,
        SAS_PREP_TO_PROC_STATUS_WAIT_HOST = 1,
        SAS_PREP_TO_PROC_STATUS_HOST_ALLOW = 2,
        SAS_PREP_TO_PROC_STATUS_HOST_REJECT = 3,
        SAS_PREP_TO_PROC_STATUS_HOST_TIMEOUT = 4,
        SAS_PREP_TO_PROC_STATUS_HOST_LINK_DOWN = 5,
    };

    internal enum SAS_LEGACY_BONUS_STATUS
    {
        SAS_LEGACY_BONUS_STATUS_NONE = 0,
        SAS_LEGACY_BONUS_STATUS_REQ = 1,
    };

    internal enum SAS_MACHINE_CFG_STATUS
    {
        SAS_MACHINE_CFG_STATUS_NO_CHANGE = 0,
        SAS_MACHINE_CFG_STATUS_CHANGE = 1,
    };

    internal enum SAS_BILL_DENOM_CFG_STATUS
    {
        SAS_BILL_DENOM_CFG_STATUS_NO_CHANGE = 0,
        SAS_BILL_DENOM_CFG_STATUS_CHANGE = 1,
    };

    internal enum SAS_DATE_TIME_STATUS
    {
        SAS_DATE_TIME_STATUS_NO_CHANGE = 0,
        SAS_DATE_TIME_STATUS_CHANGE = 1,
    };

    internal enum SAS_SEC_VALID_NUM_STATUS
    {
        SAS_SEC_VALID_NUM_STATUS_SET = 0,
        SAS_SEC_VALID_NUM_STATUS_NOT_SET = 1,
    };

    internal enum SAS_GAME_ENABLE_STATUS
    {
        SAS_GAME_ENABLE_STATUS_NO_CHANGE = 0,
        SAS_GAME_ENABLE_STATUS_CHANGE = 1,
    };

    internal enum SAS_AFT_REGISTER_STATUS
    {
        SAS_AFT_REG_STATUS_UNREGISTER = 0,
        SAS_AFT_REG_STATUS_PENDING = 1,
        SAS_AFT_REG_STATUS_REGISTERED = 2,
    };

    internal enum SAS_AFT_LOCK_STATUS
    {
        SAS_AFT_LOCK_STATUS_NOT_LOCKED = 0,
        SAS_AFT_LOCK_STATUS_PENDING = 1,
        SAS_AFT_LOCK_STATUS_LOCKED = 2,
    };

    internal enum SAS_AFT_TRANS_PROC_STATUS
    {
        SAS_AFT_TRANS_PROC_STATUS_IDLE = 0,
        SAS_AFT_TRANS_PROC_STATUS_WAIT_CASHOUT_TRANS = 1,
        SAS_AFT_TRANS_PROC_STATUS_PENDING = 2,
        SAS_AFT_TRANS_PROC_STATUS_WAIT_RECEIPT = 3,
        SAS_AFT_TRANS_PROC_STATUS_WAIT_FINISH = 4,
    };

    internal enum SAS_PROG_LINK_STATUS
    {
        SAS_PROG_LINK_STATUS_NORMAL = 0,
        SAS_PROG_LINK_STATUS_DOWN = 1,
    };

    internal enum SAS_NVRAM_STATUS : GLU8
    {
        SAS_NVRAM_STATUS_NORMAL = 0,
        SAS_NVRAM_STATUS_CFG_ERR_ONLY = 1,
        SAS_NVRAM_STATUS_METER_ERR_ONLY = 2,
        SAS_NVRAM_STATUS_ALL_ERR = 3,
        SAS_NVRAM_STATUS_UNKNOWN = 4,
    };

    [Flags]
    internal enum BITMASK_SAS_FEATURE : GLU32
    {
        LEGACY_BONUS_AWARDS = 0x01,
        VALID_EXTENSION = 0x02,
        VALID_MODE_SECURE = 0x04,
        VALID_MODE_SYSTEM = 0x08,
        TICKET_REDEMPTION = 0x10,
        METER_MODEL_WHEN_WIN = 0x20,
        METER_MODEL_WHEN_PAID = 0x40,
        TICKET_TO_TOTAL_DROP = 0x80,
        EXTENDED_METER = 0x100,
        AFT = 0x200,
        AFT_BONUS_AWARDS = 0x400,
        MULTI_DENOM_EXTENSION = 0x800,
        TOURNAMENT = 0x1000,
        COMPONENT_AUTHENTICATION = 0x2000,
        MULTI_PROG_LP = 0x4000,
    }

    [Flags]
    internal enum BITMASK_SAS_MACHINE_STATUS : GLU16
    {
        ENABLE_PLAY = 0x01,
        ENABLE_ALL_SOUND = 0x02,
        ENABLE_GAME_PLAY_SOUND = 0x04,
        ENABLE_BA = 0x10,
        LEAVE_MAINTAIN_MODE = 0x20,
        DISABLE_AUTO_BET = 0x40,
    }
    #endregion

    #region struct

    internal struct CfgBillDenominations
    {
        public bool Enable_1 { get; }
        public bool Enable_2 { get; }
        public bool Enable_5 { get; }
        public bool Enable_10 { get; }
        public bool Enable_20 { get; }
        public bool Enable_25 { get; }
        public bool Enable_50 { get; }
        public bool Enable_100 { get; }
        public bool Enable_200 { get; }
        public bool Enable_250 { get; }
        public bool Enable_500 { get; }
        public bool Enable_1000 { get; }
        public bool Enable_2000 { get; }
        public bool Enable_2500 { get; }
        public bool Enable_5000 { get; }
        public bool Enable_10000 { get; }
        public bool Enable_20000 { get; }
        public bool Enable_25000 { get; }
        public bool Enable_50000 { get; }
        public bool Enable_100000 { get; }
        public bool Enable_200000 { get; }
        public bool Enable_250000 { get; }
        public bool Enable_500000 { get; }
        public bool Enable_1000000 { get; }

        public CfgBillDenominations(bool _Enable_1, bool _Enable_2, bool _Enable_5, bool _Enable_10, bool _Enable_20, bool _Enable_25, bool _Enable_50, bool _Enable_100,
                                    bool _Enable_200, bool _Enable_250, bool _Enable_500, bool _Enable_1000, bool _Enable_2000, bool _Enable_2500, bool _Enable_5000, bool _Enable_10000,
                                    bool _Enable_20000, bool _Enable_25000, bool _Enable_50000, bool _Enable_100000, bool _Enable_200000, bool _Enable_250000, bool _Enable_500000, bool _Enable_1000000)
        {
            Enable_1 = _Enable_1;
            Enable_2 = _Enable_2;
            Enable_5 = _Enable_5;
            Enable_10 = _Enable_10;
            Enable_20 = _Enable_20;
            Enable_25 = _Enable_25;
            Enable_50 = _Enable_50;
            Enable_100 = _Enable_100;
            Enable_200 = _Enable_200;
            Enable_250 = _Enable_250;
            Enable_500 = _Enable_500;
            Enable_1000 = _Enable_1000;
            Enable_2000 = _Enable_2000;
            Enable_2500 = _Enable_2500;
            Enable_5000 = _Enable_5000;
            Enable_10000 = _Enable_10000;
            Enable_20000 = _Enable_20000;
            Enable_25000 = _Enable_25000;
            Enable_50000 = _Enable_50000;
            Enable_100000 = _Enable_100000;
            Enable_200000 = _Enable_200000;
            Enable_250000 = _Enable_250000;
            Enable_500000 = _Enable_500000;
            Enable_1000000 = _Enable_1000000;
        }
    }
    #endregion



    internal delegate void SASGameDisabledHandler(EventArgs e);

    internal delegate void SASGameEnabledHandler(EventArgs e);
    internal delegate void SASSoundDisabledHandler(EventArgs e);

    internal delegate void SASSoundEnabledHandler(EventArgs e);

    internal delegate void SASBillAcceptorEnabledHandler(EventArgs e);

    internal delegate void SASBillAcceptorDisabledHandler(EventArgs e);

    internal delegate void SASRemoteHandpayResetHandler(EventArgs e);
    internal delegate void EnterMaintenanceModeHandler(EventArgs e);
    internal delegate void ExitMaintenanceModeHandler(EventArgs e);


    internal delegate void SASReelSpinOrGamePlaySoundDisabledHandler(EventArgs e);

    internal delegate void SASEnableDisableGameNHandler(int gameNumber, bool enabled, EventArgs e);

    internal delegate void SASMultipleJackpotHandler(byte[] minimumwin, byte[] maximunwin, byte multiplier_taxstatus, byte enable_disable, byte wagerType, EventArgs e);

    internal delegate void SASEnableJackpotHandpayResetHandler(ENABLE_JACKPOT_HANDPAY_RESET_METHOD resetMethod, EventArgs e);

    internal delegate void SASHostCfgBillDenominationHandler(CfgBillDenominations cfg, BILL_ACCEPTOR_ACTION_FLAG flag, EventArgs e);

    internal delegate void SASEnableDisableAutoRebetHandler(bool enabled, EventArgs e);

    internal delegate void SASTiltDetectedHandler(string msg, EventArgs e);
    internal delegate void SASTiltLinkDownHandler(bool linkdown, EventArgs e);

    internal delegate void SASTiltNoDetectedHandler(string msg, EventArgs e);

    internal delegate void AFTTransferIncomingHandler(EventArgs e);

    internal delegate void AFTTransferRejectedHandler(EventArgs e);

    internal delegate void AFTTransferCompletedHandler(string type, string debitcredit, decimal qwCompCashAmount, decimal qwCompResAmount, decimal qwCompNonresAmount, EventArgs e);

    internal enum SASEvent
    {


        SlotDoorOpen = 0x11,
        SlotDoorClosed = 0x12,
        DropDoorOpen = 0x13,
        DropDoorClosed = 0x14,
        CardCageDoorOpen = 0x15,
        CardCageDoorClosed = 0x16,
        ACPowerAppliedToGamingMachine = 0x17,
        ACPowerLostFromGamingMachine = 0x18,
        CashboxDoorOpen = 0x19,
        CashboxDoorClosed = 0x1A,
        CashboxRemoved = 0x1B,
        CashboxInstalled = 0x1C,
        BellyDoorOpen = 0x1D,
        BellyDoorClosed = 0x1E,
        CashboxFull = 0x27,
        BillJam = 0x28,
        BillRejected = 0x2B,
        BatteryLow = 0x3B,
        OperatorChangedOptions = 0x3C,
        Bill5Inserted = 0x48,
        Bill10Inserted = 0x49,
        Bill20Inserted = 0x4A,
        Bill50Inserted = 0x4B,
        Bill100Inserted = 0x4C,
        Bill500Inserted = 0x4E,
        Bill200Inserted = 0x50,
        HandpayPending_DEPRECATED = 0x51,
        HandpayReset_DEPRECATED = 0x52,
        CashoutButtonPressed = 0x66,
        ChangeLampOn = 0x71,
        ChangeLampOff = 0x72,
        SoftMetersToZero = 0x7A,
        Playing = 0x7E,
        EndPlay = 0x7F,
        JackpotOcurred = 0x7C,
        AttendantMenuEntered = 0x82,
        AttendantMenuExited = 0x83,
        OperatorMenuEntered = 0x84,
        OperatorMenuExited = 0x85,
        GameIsOutOfServiceByAttendant = 0x86

    }


    internal class SASCTL
    {
        private readonly object _lp72DeferredLock = new object();
        private DeferredLP72Cashout _deferredLp72Cashout = null;

        // You already have this in EGM; you must bridge it here or reference it.
        private bool _settlementPending = false;



        private sealed class DeferredLP72Cashout
        {
            public byte Address;
            public byte TransferCode;
            public byte TransactionIndex;
            public byte TransferType;
            public ulong CashableAmount;
            public ulong RestrictedAmount;
            public ulong NonRestrictedAmount;
            public byte TransferFlags;
            public byte[] AssetNumber;
            public byte[] RegistrationKey;
            public string TransactionID;
            public uint Expiration;
            public ushort PoolID;
            public byte[] ReceiptData;
            public byte[] LockTimeout;
        }
        private ISASClient client;
        private bool Initiated = false;
        private AFTTransfer transfer;
        private decimal sasReportedDenomination;
        private ulong sasCreditLimit;
        private ulong inHouseInLimit;
        private ulong inHouseOutLimit;
        private ulong sasCashCredit;
        private ulong sasRestCredit;
        private ulong sasNonRestCredit;
        private bool AFTCashInGMLockCnt;
        internal bool AFTCashOutGMLockCnt;
        internal bool CreditLimitExceeded;
        private static SASCTL sasctl_;
        internal SASGameDisabledHandler SASGameDisabled;
        internal SASGameEnabledHandler SASGameEnabled;
        internal SASSoundDisabledHandler SASSoundDisabled;
        internal SASSoundEnabledHandler SASSoundEnabled;
        internal SASReelSpinOrGamePlaySoundDisabledHandler SASReelSpinOrGamePlaySoundDisabled;
        internal SASBillAcceptorEnabledHandler SASBillAcceptorEnabled;
        internal SASBillAcceptorDisabledHandler SASBillAcceptorDisabled;
        internal SASHostCfgBillDenominationHandler SasHostCfgBillDenomination;
        internal SASRemoteHandpayResetHandler SASRemoteHandpayReset;
        internal EnterMaintenanceModeHandler EnterMaintenanceMode;
        internal ExitMaintenanceModeHandler ExitMaintenanceMode;
        internal SASMultipleJackpotHandler SASMultipleJackpot;
        internal SASEnableJackpotHandpayResetHandler SASEnableJackpotHandpayReset;
        internal SASEnableDisableAutoRebetHandler SASEnableDisableAutoRebet;
        internal SASEnableDisableGameNHandler SASEnableDisableGameN;
        internal AFTTransferRejectedHandler AFTTransferRejected;
        internal AFTTransferCompletedHandler AFTTransferCompleted;
        internal AFTTransferIncomingHandler AFTTransferIncoming;
        internal SASTiltDetectedHandler SASTiltDetected;
        internal SASTiltLinkDownHandler SASTiltLinkDown;
        internal SASTiltNoDetectedHandler SASNoTiltDetected;

        private ulong g_qwCurrTotalCredit;

        // ================================
        // SAFE NUMERIC CONVERSION HELPERS
        // ================================

        private static ulong ToUlongCreditsSafe(decimal? amount, decimal denom, string name)
        {
            if (!amount.HasValue) return 0UL;

            if (denom <= 0m)
            {
                Logger.Log($"[SASCTL][WARN] Denomination invalid ({denom}) while converting {name}. Returning 0.");
                return 0UL;
            }

            decimal credits = amount.Value / denom;

            // Remove fractional part
            credits = decimal.Truncate(credits);

            if (credits < 0m)
            {
                Logger.Log($"[SASCTL][WARN] {name} became NEGATIVE after denom conversion: {credits}. Clamping to 0.");
                return 0UL;
            }

            const decimal U64_MAX = 18446744073709551615m;

            if (credits > U64_MAX)
            {
                Logger.Log($"[SASCTL][WARN] {name} too large after denom conversion: {credits}. Clamping to UInt64.MaxValue.");
                return ulong.MaxValue;
            }

            return (ulong)credits;
        }

        private static ulong AddUlongSaturating(ulong a, ulong b)
        {
            ulong r = a + b;
            if (r < a) return ulong.MaxValue;
            return r;
        }
        private ulong g_bCurrAFTStatus;

        protected SASCTL()
        {

        }

        internal void InstantiateSASCTL(bool unitydevelopment)
        {
            if (!unitydevelopment)
                client = new GanlotSASClient(true);
            else
                client = new DevSASClient(true);

            transfer = new AFTTransfer();

            client.VirtualEGM01 += new VirtualEGM01Handler(HandleLP01);
            client.VirtualEGM02 += new VirtualEGM02Handler(HandleLP02);
            client.VirtualEGM03 += new VirtualEGM03Handler(HandleLP03);
            client.VirtualEGM04 += new VirtualEGM04Handler(HandleLP04);
            client.VirtualEGM05 += new VirtualEGM05Handler(HandleLP05);
            client.VirtualEGM06 += new VirtualEGM06Handler(HandleLP06);
            client.VirtualEGM07 += new VirtualEGM07Handler(HandleLP07);
            client.VirtualEGM08 += new VirtualEGM08Handler(HandleLP08);
            client.VirtualEGM09 += new VirtualEGM09Handler(HandleLP09);
            client.VirtualEGM0A += new VirtualEGM0AHandler(HandleLP0A);
            client.VirtualEGM0B += new VirtualEGM0BHandler(HandleLP0B);
            client.VirtualEGM8B += new VirtualEGM8BHandler(HandleLP8B);
            client.VirtualEGM94 += new VirtualEGM94Handler(HandleLP94);
            client.VirtualEGMA8 += new VirtualEGMA8Handler(HandleLPA8);
            client.VirtualEGMAA += new VirtualEGMAAHandler(HandleLPAA);
            client.VirtualEGMLP72 += new VirtualEGMLP72Handler(HandleLP72);
            client.VirtualEGM74 += new VirtualEGM74Handler(HandleLP74);
            client.SASLinkDown += new SASLinkDownHandler((b, e) =>
            {
                SASTiltLinkDown(b, e);
            });

            client.ClientCriticalError += new ClientCriticalErrorHandler((msg, e) =>
            {
                SASTiltDetected(msg, e);
            });
            client.ClientNoError += new ClientNoErrorHandler((msg, e) =>
            {
                SASNoTiltDetected(msg, e);
            });
            g_bCurrAFTStatus = BITMASK_SAS_EGM_AFT_STATUS.PRINTER_AVAIL_FOR_RECEIPT |
                           BITMASK_SAS_EGM_AFT_STATUS.TO_HOST_PARTIAL_AMOUNT_AVAIL |
                            BITMASK_SAS_EGM_AFT_STATUS.ENABLE_INHOUSE_TRANS |
                           BITMASK_SAS_EGM_AFT_STATUS.ENABLE_DEBIT_TRANS;
            AFTCashInGMLockCnt = false;
            AFTCashOutGMLockCnt = false;
            CreditLimitExceeded = false;
        }

        public void UpdateSASInfo(
    decimal? reportedDenom,
    decimal? creditLimit,
    decimal? cashCredit,
    decimal? restCredit,
    decimal? nonRestCredit,
    decimal? _inHouseInLimit,
    decimal? _inHouseOutLimit)
        {
            // Update denomination safely
            if (reportedDenom.HasValue)
            {
                if (reportedDenom.Value > 0m)
                {
                    sasReportedDenomination = reportedDenom.Value;
                }
                else
                {
                    Logger.Log($"[SASCTL][WARN] Invalid reportedDenom: {reportedDenom.Value}");
                }
            }

            if (sasReportedDenomination <= 0m)
            {
                Logger.Log("[SASCTL][ERROR] Denomination is invalid. Skipping UpdateSASInfo.");
                return;
            }

            // Convert all credit values safely
            if (cashCredit.HasValue)
                sasCashCredit = ToUlongCreditsSafe(cashCredit, sasReportedDenomination, "cashCredit");

            if (restCredit.HasValue)
                sasRestCredit = ToUlongCreditsSafe(restCredit, sasReportedDenomination, "restCredit");

            if (nonRestCredit.HasValue)
                sasNonRestCredit = ToUlongCreditsSafe(nonRestCredit, sasReportedDenomination, "nonRestCredit");

            if (creditLimit.HasValue)
                sasCreditLimit = ToUlongCreditsSafe(creditLimit, sasReportedDenomination, "creditLimit");

            if (_inHouseInLimit.HasValue)
            {
                if (_inHouseInLimit.Value == 0m)
                    inHouseInLimit = ulong.MaxValue;
                else
                    inHouseInLimit = ToUlongCreditsSafe(_inHouseInLimit, sasReportedDenomination, "_inHouseInLimit");
            }

            if (_inHouseOutLimit.HasValue)
            {
                if (_inHouseOutLimit.Value == 0m)
                    inHouseOutLimit = ulong.MaxValue;
                else
                    inHouseOutLimit = ToUlongCreditsSafe(_inHouseOutLimit, sasReportedDenomination, "_inHouseOutLimit");
            }

            // Safe total calculation
            g_qwCurrTotalCredit = 0UL;
            g_qwCurrTotalCredit = AddUlongSaturating(g_qwCurrTotalCredit, sasCashCredit);
            g_qwCurrTotalCredit = AddUlongSaturating(g_qwCurrTotalCredit, sasRestCredit);
            g_qwCurrTotalCredit = AddUlongSaturating(g_qwCurrTotalCredit, sasNonRestCredit);
        }

        public void Init(decimal reportedDenom, decimal creditLimit, decimal cashCredit, decimal restCredit, decimal nonRestCredit, decimal inHouseInLimit, decimal inHouseOutLimit, bool enableSAS)
        {
            sasReportedDenomination = reportedDenom;
            // Convert all decimal to ulong by reported denomination
            UpdateSASInfo(reportedDenom, creditLimit, cashCredit, restCredit, nonRestCredit, inHouseInLimit, inHouseOutLimit);


            if (enableSAS)
            {
                client.Start("");
            }
        }

        public void EnableSAS(bool startclient)
        {
            client.Start("");
            client.Enable();

        }

        public void DisableSAS()
        {
            client.Stop();

        }


        internal void HandleLP01(byte address, EventArgs e)
        {
            SASGameDisabled(e);
        }
        internal void HandleLP02(byte address, EventArgs e)
        {
            SASGameEnabled(e);
        }
        internal void HandleLP03(byte address, EventArgs e)
        {
            SASSoundDisabled(e);
        }
        internal void HandleLP04(byte address, EventArgs e)
        {
            SASSoundEnabled(e);
        }
        internal void HandleLP05(byte address, EventArgs e)
        {
            SASReelSpinOrGamePlaySoundDisabled(e);
        }
        internal void HandleLP06(byte address, EventArgs e)
        {
            SASBillAcceptorEnabled(e);
        }
        internal void HandleLP07(byte address, EventArgs e)
        {
            SASBillAcceptorDisabled(e);
        }
        internal void HandleLP09(byte address, byte[] gameNumber, byte enable, EventArgs e)
        {
            if (gameNumber.Length >= 2)
            {
                SASEnableDisableGameN(gameNumber[1], (enable == 0x00) ? false : true, e);
            }
            else if (gameNumber.Length == 1)
            {
                SASEnableDisableGameN(gameNumber[0], (enable == 0x00) ? false : true, e);
            }
        }
        internal void HandleLP0A(byte address, EventArgs e)
        {
            EnterMaintenanceMode(e);
        }
        internal void HandleLP0B(byte address, EventArgs e)
        {
            ExitMaintenanceMode(e);
        }
        internal void HandleLP8B(byte address, byte[] minimumwin, byte[] maximunwin, byte multiplier_taxstatus, byte enable_disable, byte wagerType, EventArgs e)
        {
            SASMultipleJackpot(minimumwin, maximunwin, multiplier_taxstatus, enable_disable, wagerType, e);
        }
        internal void HandleLP94(byte address, EventArgs e)
        {
            SASRemoteHandpayReset(e);
        }
        internal void HandleLPAA(byte address, byte enabledisable, EventArgs e)
        {
            bool enabled = false;
            if (enabledisable == 0x00)
            {
                enabled = false;
            }
            else if (enabledisable == 0x01)
            {
                enabled = true;
            }
            SASEnableDisableAutoRebet(enabled, e);
        }
        internal void HandleLPA8(byte address, byte resetMethod, EventArgs e)
        {
            ENABLE_JACKPOT_HANDPAY_RESET_METHOD method = ENABLE_JACKPOT_HANDPAY_RESET_METHOD.STANDARD_HANDPAY;
            if (resetMethod == 0x00)
            {
                method = ENABLE_JACKPOT_HANDPAY_RESET_METHOD.STANDARD_HANDPAY;
            }
            else if (resetMethod == 0x01)
            {
                method = ENABLE_JACKPOT_HANDPAY_RESET_METHOD.RESET_TO_THE_CREDIT_METER;
            }

            SASEnableJackpotHandpayReset(method, e);

        }
        internal void HandleLP08(byte address, byte[] denominations, byte actionFlag, EventArgs e)
        {
            if (denominations.Length >= 3)
            {
                CfgBillDenominations cfg = new CfgBillDenominations((denominations[0] & (byte)Math.Pow(2, 0)) >= 1,
                                                                   (denominations[0] & (byte)Math.Pow(2, 1)) >= 1,
                                                                   (denominations[0] & (byte)Math.Pow(2, 2)) >= 1,
                                                                   (denominations[0] & (byte)Math.Pow(2, 3)) >= 1,
                                                                   (denominations[0] & (byte)Math.Pow(2, 4)) >= 1,
                                                                   (denominations[0] & (byte)Math.Pow(2, 5)) >= 1,
                                                                   (denominations[0] & (byte)Math.Pow(2, 6)) >= 1,
                                                                   (denominations[0] & (byte)Math.Pow(2, 7)) >= 1,
                                                                   (denominations[1] & (byte)Math.Pow(2, 0)) >= 1,
                                                                   (denominations[1] & (byte)Math.Pow(2, 1)) >= 1,
                                                                   (denominations[1] & (byte)Math.Pow(2, 2)) >= 1,
                                                                   (denominations[1] & (byte)Math.Pow(2, 3)) >= 1,
                                                                   (denominations[1] & (byte)Math.Pow(2, 4)) >= 1,
                                                                   (denominations[1] & (byte)Math.Pow(2, 5)) >= 1,
                                                                   (denominations[1] & (byte)Math.Pow(2, 6)) >= 1,
                                                                   (denominations[1] & (byte)Math.Pow(2, 7)) >= 1,
                                                                   (denominations[2] & (byte)Math.Pow(2, 0)) >= 1,
                                                                   (denominations[2] & (byte)Math.Pow(2, 1)) >= 1,
                                                                   (denominations[2] & (byte)Math.Pow(2, 2)) >= 1,
                                                                   (denominations[2] & (byte)Math.Pow(2, 3)) >= 1,
                                                                   (denominations[2] & (byte)Math.Pow(2, 4)) >= 1,
                                                                   (denominations[2] & (byte)Math.Pow(2, 5)) >= 1,
                                                                   (denominations[2] & (byte)Math.Pow(2, 6)) >= 1,
                                                                   (denominations[2] & (byte)Math.Pow(2, 7)) >= 1);

                BILL_ACCEPTOR_ACTION_FLAG flag = BILL_ACCEPTOR_ACTION_FLAG.DISABLE_BILL_ACCEPTOR_AFTER_EACH_ACCEPTED_BILL;

                if ((actionFlag & 0x01) == 0x01)

                {
                    flag = BILL_ACCEPTOR_ACTION_FLAG.KEEP_BILL_ACCEPTOR_ENABLED_AFTER_EACH_ACCEPTED_BILL;
                }
                else
                {
                    flag = BILL_ACCEPTOR_ACTION_FLAG.DISABLE_BILL_ACCEPTOR_AFTER_EACH_ACCEPTED_BILL;
                }

                SasHostCfgBillDenomination(cfg, flag, new EventArgs());
            }
        }

        private void CompleteDeferredLP72Cashout()
        {
            DeferredLP72Cashout pending = null;

            lock (_lp72DeferredLock)
            {
                if (_deferredLp72Cashout == null) return;
                pending = _deferredLp72Cashout;
                _deferredLp72Cashout = null;
            }

            Logger.Log($"Completing deferred LP72 CASH OUT. TxID={pending.TransactionID}");

            // Only cashout deferrals should be here
            if (pending.TransferType != (byte)SAS_AFT_TRANS_TYPE.SAS_AFT_TRANS_TYPE_INHOUSE_TO_HOST)
            {
                Logger.Log($"Deferred LP72 is not a TO_HOST cashout. Ignoring. TxID={pending.TransactionID}");
                return;
            }

            // If transfer engine is not in a safe state, we can either reject or try to recover.
            // SAFEST: reject to avoid deadlocks/inconsistent credits.
            if (transfer.status != AFTTransferStatus.Idle)
            {
                Logger.Log($"Cannot complete deferred LP72 because transfer is busy (status={transfer.status}). Rejecting. TxID={pending.TransactionID}");

                client.FinishAFTTransfer(
                    (byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_UNABLE_DO_TRANS_THIS_TIME,
                    0, 0, 0);

                // Clean state machine
                transfer.Transition(AFTTransferStatus.TransferRejected);
                transfer.Transition(AFTTransferStatus.TransferInterrogated);
                AFTTransferRejected(new EventArgs());
                transfer.Transition(AFTTransferStatus.Idle);
                return;
            }

            // Start the state machine cleanly for completion (NO replay of LP72!)
            if (!transfer.Transition(AFTTransferStatus.TransferIncoming))
                return;

            AFTTransferIncoming(new EventArgs());
            transfer.Transition(AFTTransferStatus.TransferPending);

            GLU64 cashableAmount = pending.CashableAmount;
            GLU64 restrictedAmount = pending.RestrictedAmount;
            GLU64 nonRestrictedAmount = pending.NonRestrictedAmount;

            GLU64 qwTotalTransAmount = cashableAmount + restrictedAmount + nonRestrictedAmount;

            // Track as debit (cashout)
            transfer.amount = (decimal)-1 * qwTotalTransAmount * sasReportedDenomination;

            // --- Original cashout checks (same intent as HandleLP72 cashout path) ---
            GLU8 bIsAmountValid = 1;

            // If cashout is locked out by tilt/etc
            if (AFTCashOutGMLockCnt)
            {
                client.FinishAFTTransfer(
                    (byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_UNABLE_DO_TRANS_THIS_TIME,
                    0, 0, 0);

                transfer.Transition(AFTTransferStatus.TransferRejected);
                transfer.Transition(AFTTransferStatus.TransferInterrogated);
                AFTTransferRejected(new EventArgs());
                transfer.Transition(AFTTransferStatus.Idle);
                return;
            }

            // In-house out limit
            if (qwTotalTransAmount > inHouseOutLimit)
            {
                client.FinishAFTTransfer(
                    (byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_AMOUNT_EXCEED_TRANS_LIMIT,
                    0, 0, 0);

                transfer.Transition(AFTTransferStatus.TransferRejected);
                transfer.Transition(AFTTransferStatus.TransferInterrogated);
                AFTTransferRejected(new EventArgs());
                transfer.Transition(AFTTransferStatus.Idle);
                return;
            }

            GLU64 qwCompCashAmount = 0;
            GLU64 qwCompResAmount = 0;
            GLU64 qwCompNonresAmount = 0;
            GLU64 qwRemainAmount = 0;

            if (qwTotalTransAmount > 0)
            {
                // Full/partial validity check rules (keep consistent with your HandleLP72)
                if ((g_bCurrAFTStatus & BITMASK_SAS_EGM_AFT_STATUS.TO_HOST_PARTIAL_AMOUNT_AVAIL) == 0)
                {
                    // NOTE: this looks inverted in your original snippet, but I'm preserving your intent:
                    // if partial NOT available, host expects full exact amounts to be valid.
                    if (sasCashCredit < cashableAmount) bIsAmountValid = 0;
                    if (sasRestCredit < restrictedAmount) bIsAmountValid = 0;
                    if (sasNonRestCredit < nonRestrictedAmount) bIsAmountValid = 0;
                }
                else if (pending.TransferCode == 0) // Full transfer
                {
                    if (sasCashCredit < cashableAmount) bIsAmountValid = 0;
                    if (sasRestCredit < restrictedAmount) bIsAmountValid = 0;
                    if (sasNonRestCredit < nonRestrictedAmount) bIsAmountValid = 0;
                }

                if (bIsAmountValid == 0)
                {
                    client.FinishAFTTransfer(
                        (byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_AMOUNT_EXCEED_TRANS_LIMIT,
                        0, 0, 0);

                    transfer.Transition(AFTTransferStatus.TransferRejected);
                    transfer.Transition(AFTTransferStatus.TransferInterrogated);
                    AFTTransferRejected(new EventArgs());
                    transfer.Transition(AFTTransferStatus.Idle);
                    return;
                }

                // Compute completion amounts (same as your cashout block)
                if (pending.TransferCode == 0) // Full transfer
                {
                    qwCompCashAmount = (sasCashCredit > cashableAmount ? cashableAmount : sasCashCredit);
                    qwCompResAmount = (sasRestCredit > restrictedAmount ? restrictedAmount : sasRestCredit);
                    qwCompNonresAmount = (sasNonRestCredit > nonRestrictedAmount ? nonRestrictedAmount : sasNonRestCredit);

                    qwTotalTransAmount = qwCompCashAmount + qwCompResAmount + qwCompNonresAmount;

                    if (qwTotalTransAmount > sasCreditLimit)
                    {
                        qwRemainAmount = sasCreditLimit;

                        qwCompResAmount = (qwRemainAmount > qwCompResAmount) ? qwCompResAmount : qwRemainAmount;
                        qwRemainAmount -= qwCompResAmount;

                        qwCompNonresAmount = (qwRemainAmount > qwCompNonresAmount) ? qwCompNonresAmount : qwRemainAmount;
                        qwRemainAmount -= qwCompNonresAmount;

                        qwCompCashAmount = qwRemainAmount;
                    }
                }
                else
                {
                    qwCompCashAmount = (sasCashCredit > cashableAmount ? cashableAmount : sasCashCredit);
                    qwCompResAmount = (sasRestCredit > restrictedAmount ? restrictedAmount : sasRestCredit);
                    qwCompNonresAmount = (sasNonRestCredit > nonRestrictedAmount ? nonRestrictedAmount : sasNonRestCredit);
                }

                // Update SAS-side credits (keep your clamp behavior)
                if (qwCompCashAmount > 0)
                    sasCashCredit = sasCashCredit >= qwCompCashAmount ? sasCashCredit - qwCompCashAmount : 0;

                if (qwCompResAmount > 0)
                    sasRestCredit = sasRestCredit >= qwCompResAmount ? sasRestCredit - qwCompResAmount : 0;

                if (qwCompNonresAmount > 0)
                    sasNonRestCredit = sasNonRestCredit >= qwCompNonresAmount ? sasNonRestCredit - qwCompNonresAmount : 0;

                g_qwCurrTotalCredit = sasCashCredit + sasRestCredit + sasNonRestCredit;
            }

            // Finish the deferred transfer NOW (this is what host was waiting for)
            client.FinishAFTTransfer(pending.TransferCode, qwCompCashAmount, qwCompResAmount, qwCompNonresAmount);

            if (transfer.Transition(AFTTransferStatus.TransferCompleted))
            {
                transfer.Transition(AFTTransferStatus.TransferInterrogated);

                AFTTransferCompleted(
                    "Cash Out",
                    "Debit",
                    qwCompCashAmount * sasReportedDenomination,
                    qwCompResAmount * sasReportedDenomination,
                    qwCompNonresAmount * sasReportedDenomination,
                    new EventArgs());

                transfer.Transition(AFTTransferStatus.Idle);
            }
        }

        public void SetSettlementPending(bool pending)
        {
            bool wasPending = _settlementPending;
            _settlementPending = pending;

            // When settlement clears, complete any deferred LP72 cashout immediately
            if (wasPending && !pending)
            {
                CompleteDeferredLP72Cashout();
            }
        }

        internal void HandleLP72(
    byte address,
    byte transferCode,
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
    byte[] lockTimeout,
    EventArgs ee)
        {
            GLU64 qwRemainAmount = 0;
            GLU64 qwCompResAmount = 0;
            GLU64 qwCompNonresAmount = 0;
            GLU64 qwCompCashAmount = 0;

            CreditLimitExceeded = false;

            // If we cannot enter TransferIncoming, do not proceed.
            if (!transfer.Transition(AFTTransferStatus.TransferIncoming))
                return;

            AFTTransferIncoming(new EventArgs());
            transfer.Transition(AFTTransferStatus.TransferPending); // responsibility of implementation

            var qwTotalTransAmount = cashableAmount + restrictedAmount + nonRestrictedAmount;

            // =========================
            // CASH IN (Immediate)
            // =========================
            if (transferType == (byte)SAS_AFT_TRANS_TYPE.SAS_AFT_TRANS_TYPE_INHOUSE_TO_EGM
                || transferType == (byte)SAS_AFT_TRANS_TYPE.SAS_AFT_TRANS_TYPE_DEBIT_TO_EGM)
            {
                transfer.amount = qwTotalTransAmount * sasReportedDenomination;

                // NOTE: Check if there is any tilts on the EGM
                if (AFTCashInGMLockCnt)
                {
                    client.FinishAFTTransfer(
                        (byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_UNABLE_DO_TRANS_THIS_TIME,
                        0, 0, 0);

                    transfer.Transition(AFTTransferStatus.TransferRejected);
                    transfer.Transition(AFTTransferStatus.TransferInterrogated);

                    AFTTransferRejected(new EventArgs());
                    transfer.Transition(AFTTransferStatus.Idle);
                    return;
                }

                if (qwTotalTransAmount > inHouseInLimit)
                {
                    client.FinishAFTTransfer(
                        (byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_AMOUNT_EXCEED_TRANS_LIMIT,
                        0, 0, 0);

                    transfer.Transition(AFTTransferStatus.TransferRejected);
                    transfer.Transition(AFTTransferStatus.TransferInterrogated);

                    AFTTransferRejected(new EventArgs());
                    transfer.Transition(AFTTransferStatus.Idle);
                    return;
                }

                if (qwTotalTransAmount > 0)
                {
                    // Partial / full rules unchanged
                    GLU64 qwRemainCurrCreditRoom = (GLU64)sasCreditLimit - (GLU64)g_qwCurrTotalCredit;
                    GLU64 qwCurrTransLimit = (sasCreditLimit > qwRemainCurrCreditRoom) ? qwRemainCurrCreditRoom : sasCreditLimit;

                    if (transferCode == 0x01)
                    {
                        if (qwTotalTransAmount > qwCurrTransLimit)
                        {
                            qwRemainAmount = qwCurrTransLimit;

                            if (qwRemainAmount > restrictedAmount)
                                qwCompResAmount = restrictedAmount;
                            else
                                qwCompResAmount = qwRemainAmount;
                            qwRemainAmount -= qwCompResAmount;

                            if (qwRemainAmount > nonRestrictedAmount)
                                qwCompNonresAmount = nonRestrictedAmount;
                            else
                                qwCompNonresAmount = qwRemainAmount;
                            qwRemainAmount -= qwCompNonresAmount;

                            qwCompCashAmount = qwRemainAmount;
                        }
                        else
                        {
                            qwCompCashAmount = cashableAmount;
                            qwCompResAmount = restrictedAmount;
                            qwCompNonresAmount = nonRestrictedAmount;
                        }
                    }
                    else
                    {
                        if (qwTotalTransAmount > qwRemainCurrCreditRoom)
                        {
                            client.FinishAFTTransfer(
                                (byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_AMOUNT_EXCEED_TRANS_LIMIT,
                                0, 0, 0);

                            CreditLimitExceeded = true;
                        }
                        else
                        {
                            qwCompCashAmount = cashableAmount;
                            qwCompResAmount = restrictedAmount;
                            qwCompNonresAmount = nonRestrictedAmount;
                        }
                    }

                    // Update SAS-side credits (unchanged)
                    if (qwCompCashAmount > 0) sasCashCredit += qwCompCashAmount;
                    if (qwCompResAmount > 0) sasRestCredit += qwCompResAmount;
                    if (qwCompNonresAmount > 0) sasNonRestCredit += qwCompNonresAmount;

                    g_qwCurrTotalCredit = sasCashCredit + sasRestCredit + sasNonRestCredit;
                }
                else
                {
                    qwCompCashAmount = 0;
                    qwCompResAmount = 0;
                    qwCompNonresAmount = 0;
                }

                // Complete AFT transfer (unchanged)
                client.FinishAFTTransfer(transferCode, qwCompCashAmount, qwCompResAmount, qwCompNonresAmount);

                if (transfer.Transition(AFTTransferStatus.TransferCompleted))
                {
                    transfer.Transition(AFTTransferStatus.TransferInterrogated);

                    AFTTransferCompleted(
                        "Cash In",
                        "Credit",
                        qwCompCashAmount * sasReportedDenomination,
                        qwCompResAmount * sasReportedDenomination,
                        qwCompNonresAmount * sasReportedDenomination,
                        new EventArgs());

                    transfer.Transition(AFTTransferStatus.Idle);
                }

                return;
            }

            // =========================
            // CASH OUT (May be Deferred)
            // =========================
            if (transferType == (byte)SAS_AFT_TRANS_TYPE.SAS_AFT_TRANS_TYPE_INHOUSE_TO_HOST)
            {
                transfer.amount = (decimal)-1 * qwTotalTransAmount * sasReportedDenomination;

                // -------------------------------------------------------
                // ✅ UPDATED: Deferral during settlement ONLY
                // -------------------------------------------------------
                // IMPORTANT CHANGES vs your current code:
                // 1) Do NOT call transfer.Transition(...Idle) here.
                // 2) Do NOT reference EGM.GetInstance().RoundHasUnsettledBet() here (it broke your server/startup flow).
                // 3) Keep transfer in TransferPending and simply return after storing the LP72 payload.
                // 4) Completion must happen via CompleteDeferredLP72Cashout() when SetSettlementPending(false) is called.
                //
                if (_settlementPending)
                {
                    lock (_lp72DeferredLock)
                    {
                        if (_deferredLp72Cashout != null)
                        {
                            // Second LP72 while one already deferred → reject safely
                            client.FinishAFTTransfer(
                                (byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_UNABLE_DO_TRANS_THIS_TIME,
                                0, 0, 0);

                            transfer.Transition(AFTTransferStatus.TransferRejected);
                            transfer.Transition(AFTTransferStatus.TransferInterrogated);
                            AFTTransferRejected(new EventArgs());
                            transfer.Transition(AFTTransferStatus.Idle);

                            Logger.Log("LP72 rejected: another deferred cashout already pending.");
                            return;
                        }

                        _deferredLp72Cashout = new DeferredLP72Cashout
                        {
                            Address = address,
                            TransferCode = transferCode,
                            TransactionIndex = transactionIndex,
                            TransferType = transferType,
                            CashableAmount = cashableAmount,
                            RestrictedAmount = restrictedAmount,
                            NonRestrictedAmount = nonRestrictedAmount,
                            TransferFlags = tranferFlags,
                            AssetNumber = assetNumber,
                            RegistrationKey = registrationKey,
                            TransactionID = transactionID,
                            Expiration = expiration,
                            PoolID = poolID,
                            ReceiptData = receiptData,
                            LockTimeout = lockTimeout
                        };
                    }

                    Logger.Log($"LP72 CASH OUT deferred due to settlement pending. TxID={transactionID}, Amount={qwTotalTransAmount}");

                    // ✅ DO NOT reset the AFT state machine here.
                    // Host is holding the lock and expects FinishAFTTransfer later.
                    // We will complete it in CompleteDeferredLP72Cashout() when settlement clears.
                    return;
                }

                // -----------------------------
                // Original CASH OUT logic below
                // -----------------------------
                GLU8 bIsAmountValid = 1;

                if (AFTCashOutGMLockCnt)
                {
                    client.FinishAFTTransfer(
                        (byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_UNABLE_DO_TRANS_THIS_TIME,
                        0, 0, 0);

                    transfer.Transition(AFTTransferStatus.TransferRejected);
                    transfer.Transition(AFTTransferStatus.TransferInterrogated);

                    AFTTransferRejected(new EventArgs());
                    transfer.Transition(AFTTransferStatus.Idle);
                    return;
                }

                if (qwTotalTransAmount > inHouseOutLimit)
                {
                    client.FinishAFTTransfer(
                        (byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_AMOUNT_EXCEED_TRANS_LIMIT,
                        0, 0, 0);

                    transfer.Transition(AFTTransferStatus.TransferRejected);
                    transfer.Transition(AFTTransferStatus.TransferInterrogated);

                    AFTTransferRejected(new EventArgs());
                    transfer.Transition(AFTTransferStatus.Idle);
                    return;
                }

                if (qwTotalTransAmount > 0)
                {
                    // Full/partial validity checks (keep your existing intent)
                    if ((g_bCurrAFTStatus & BITMASK_SAS_EGM_AFT_STATUS.TO_HOST_PARTIAL_AMOUNT_AVAIL) == 0)
                    {
                        // NOTE: your current code uses '>' here, which is inverted for typical "enough credit" checks,
                        // but preserving your existing behavior to avoid changing semantics unexpectedly.
                        if (sasCashCredit > cashableAmount) bIsAmountValid = 0;
                        if (sasRestCredit > restrictedAmount) bIsAmountValid = 0;
                        if (sasNonRestCredit > nonRestrictedAmount) bIsAmountValid = 0;
                    }
                    else if (transferCode == 0)
                    {
                        if (sasCashCredit < cashableAmount) bIsAmountValid = 0;
                        if (sasRestCredit < restrictedAmount) bIsAmountValid = 0;
                        if (sasNonRestCredit < nonRestrictedAmount) bIsAmountValid = 0;
                    }

                    if (bIsAmountValid == 0)
                    {
                        client.FinishAFTTransfer(
                            (byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_AMOUNT_EXCEED_TRANS_LIMIT,
                            0, 0, 0);

                        transfer.Transition(AFTTransferStatus.TransferRejected);
                        transfer.Transition(AFTTransferStatus.TransferInterrogated);

                        AFTTransferRejected(new EventArgs());
                        transfer.Transition(AFTTransferStatus.Idle);
                        return;
                    }

                    // Compute completion amounts
                    if (transferCode == 0) // Full transfer
                    {
                        qwCompCashAmount = (sasCashCredit > cashableAmount ? cashableAmount : sasCashCredit);
                        qwCompResAmount = (sasRestCredit > restrictedAmount ? restrictedAmount : sasRestCredit);
                        qwCompNonresAmount = (sasNonRestCredit > nonRestrictedAmount ? nonRestrictedAmount : sasNonRestCredit);

                        qwTotalTransAmount = qwCompCashAmount + qwCompResAmount + qwCompNonresAmount;
                        if (qwTotalTransAmount > sasCreditLimit)
                        {
                            qwRemainAmount = sasCreditLimit;

                            qwCompResAmount = (qwRemainAmount > qwCompResAmount) ? qwCompResAmount : qwRemainAmount;
                            qwRemainAmount -= qwCompResAmount;

                            qwCompNonresAmount = (qwRemainAmount > qwCompNonresAmount) ? qwCompNonresAmount : qwRemainAmount;
                            qwRemainAmount -= qwCompNonresAmount;

                            qwCompCashAmount = qwRemainAmount;
                        }
                    }
                    else
                    {
                        qwCompCashAmount = (sasCashCredit > cashableAmount ? cashableAmount : sasCashCredit);
                        qwCompResAmount = (sasRestCredit > restrictedAmount ? restrictedAmount : sasRestCredit);
                        qwCompNonresAmount = (sasNonRestCredit > nonRestrictedAmount ? nonRestrictedAmount : sasNonRestCredit);
                    }

                    // Update current SAS credits (keep your safety clamp)
                    if (qwCompCashAmount > 0)
                        sasCashCredit = sasCashCredit >= qwCompCashAmount ? sasCashCredit - qwCompCashAmount : 0;

                    if (qwCompResAmount > 0)
                        sasRestCredit = sasRestCredit >= qwCompResAmount ? sasRestCredit - qwCompResAmount : 0;

                    if (qwCompNonresAmount > 0)
                        sasNonRestCredit = sasNonRestCredit >= qwCompNonresAmount ? sasNonRestCredit - qwCompNonresAmount : 0;

                    g_qwCurrTotalCredit = sasCashCredit + sasRestCredit + sasNonRestCredit;
                }
                else
                {
                    qwCompCashAmount = 0;
                    qwCompResAmount = 0;
                    qwCompNonresAmount = 0;
                }

                // Complete AFT transfer
                client.FinishAFTTransfer(transferCode, qwCompCashAmount, qwCompResAmount, qwCompNonresAmount);

                if (transfer.Transition(AFTTransferStatus.TransferCompleted))
                {
                    transfer.Transition(AFTTransferStatus.TransferInterrogated);

                    AFTTransferCompleted(
                        "Cash Out",
                        "Debit",
                        qwCompCashAmount * sasReportedDenomination,
                        qwCompResAmount * sasReportedDenomination,
                        qwCompNonresAmount * sasReportedDenomination,
                        new EventArgs());

                    transfer.Transition(AFTTransferStatus.Idle);
                }

                return;
            }

            // If we got here, the transferType is not handled.
            // Cleanly return to Idle to avoid blocking.
            transfer.Transition(AFTTransferStatus.TransferInterrogated);
            transfer.Transition(AFTTransferStatus.Idle);
        }

        internal void HandleLP74(byte address, byte lockCode, EventArgs e)
        {
            switch (lockCode)
            {
                case 0x00: // Host requesting lock

                    if (CanLockForAFT())
                    {
                        Logger.Log("AFT Lock granted.");
                        client.CheckAndHandleAFTLocks();
                    }
                    else
                    {
                        Logger.Log("AFT Lock rejected.");
                        client.CancelAFTLock();
                        client.SetSASBusy(false);
                    }

                    break;

                case 0x80: // Host cancelling lock
                    Logger.Log("AFT Lock cancelled by host.");
                    client.CancelAFTLock();
                    client.SetSASBusy(false);
                    break;
            }
        }

        // SASCTL fields
        private volatile bool _roundHasUnsettledBet = false;

        // Called by EGM/websocket layer
        public void SetRoundHasUnsettledBet(bool hasUnsettledBet)
        {
            _roundHasUnsettledBet = hasUnsettledBet;
        }

        // SASCTL internal use
        private bool RoundHasUnsettledBet() => _roundHasUnsettledBet;

        private bool CanLockForAFT()
        {
            var status = EGMStatus.GetInstance();

            bool hasNoTilts = status.currentTilts.Count == 0;
            bool isNotInMenu = !status.menuActive && !status.maintenanceMode;
            bool aftNotBusy = !AFTInProgress();

            // ✅ IMPORTANT: block lock while settlement is pending OR wager unsettled
            // (prevents UILock during a live wager)
            bool noUnsettledRound = !_settlementPending && !_roundHasUnsettledBet;

            return hasNoTilts && isNotInMenu && aftNotBusy && noUnsettledRound;
        }

        private bool IsEgmInValidStateForAFT()
        {
            var status = EGMStatus.GetInstance();
            // Condition 1: The game must not be in the middle of a spin.
            bool isGameIdle = status.current_play.spin.slotplay.Finished;

            // Condition 2: There must be no active tilts (e.g., door open, error).
            bool hasNoTilts = status.currentTilts.Count == 0;

            // Condition 3: The EGM must not be in an operator/attendant menu or maintenance mode.
            bool isNotInMenu = !status.menuActive && !status.maintenanceMode;

            // Condition4: Check current credit in EGM so that the player can do a AFT in during NO_MORE_BETS
            decimal credits = status.current_play.spin.slotplay.creditValue;


            // Log the reason for failure for easier debugging
            if (!isGameIdle) Logger.Log("AFT Lock Reject Reason: Game is not idle.");
            if (!hasNoTilts) Logger.Log($"AFT Lock Reject Reason: Active tilts present. First tilt: {status.currentTilts.FirstOrDefault()?.Description}");
            if (!isNotInMenu) Logger.Log("AFT Lock Reject Reason: EGM is in menu or maintenance mode.");
            if (credits==0) Logger.Log("EGM Available to do a AFT IN whilst NO_MORE_BETS.");

            return (isGameIdle||(credits == 0)) && hasNoTilts && isNotInMenu;
        }

        public static SASCTL GetInstance()
        {
            if (sasctl_ == null)
            {
                sasctl_ = new SASCTL();
                //
            }
            return sasctl_;
        }


        public void SASBillException(int bill)
        {
            if (bill == 5)
                LaunchExceptionByEvent(SASEvent.Bill5Inserted);
            else if (bill == 10)
                LaunchExceptionByEvent(SASEvent.Bill10Inserted);
            else if (bill == 20)
                LaunchExceptionByEvent(SASEvent.Bill20Inserted);
            else if (bill == 50)
                LaunchExceptionByEvent(SASEvent.Bill50Inserted);
            else if (bill == 100)
                LaunchExceptionByEvent(SASEvent.Bill100Inserted);
            else if (bill == 200)
                LaunchExceptionByEvent(SASEvent.Bill200Inserted);
            else if (bill == 500)
                LaunchExceptionByEvent(SASEvent.Bill500Inserted);

        }

        public void SASDoorOpenException(SensorName doorType)
        {
            switch (doorType)
            {
                case SensorName.D_MAINDOOR:
                    LaunchExceptionByEvent(SASEvent.SlotDoorOpen);
                    break;
                case SensorName.D_DROPBOXDOOR:
                    LaunchExceptionByEvent(SASEvent.DropDoorOpen);
                    break;
                case SensorName.D_BELLYDOOR:
                    LaunchExceptionByEvent(SASEvent.BellyDoorOpen);
                    break;
                case SensorName.D_CASHBOXDOOR:
                    LaunchExceptionByEvent(SASEvent.CashboxDoorOpen);
                    break;
                case SensorName.D_CARDCAGEDOOR:
                    LaunchExceptionByEvent(SASEvent.CardCageDoorOpen);
                    break;
                default:
                    break;
            }
        }

        public void SASDoorClosedException(SensorName doorType)
        {
            switch (doorType)
            {
                case SensorName.D_MAINDOOR:
                    LaunchExceptionByEvent(SASEvent.SlotDoorClosed);
                    break;
                case SensorName.D_DROPBOXDOOR:
                    LaunchExceptionByEvent(SASEvent.DropDoorClosed);
                    break;
                case SensorName.D_BELLYDOOR:
                    LaunchExceptionByEvent(SASEvent.BellyDoorClosed);
                    break;
                case SensorName.D_CASHBOXDOOR:
                    LaunchExceptionByEvent(SASEvent.CashboxDoorClosed);
                    break;
                case SensorName.D_CARDCAGEDOOR:
                    LaunchExceptionByEvent(SASEvent.CardCageDoorClosed);
                    break;
                default:
                    break;
            }
        }

        public void SetCashoutLimit(byte gamen, decimal value)
        {
            var ulongvalue = (ulong)(value / sasReportedDenomination);
            client.SetCashoutLimit(new byte[] { 0x00, gamen }, ulongvalue);
        }

        public void SetCurrentPlayerDenomination(byte denom)
        {
            client.SetCurrentPlayerDenomination(denom);
        }

        public void EnableDenomination(byte gamenumber, byte denom, ushort maxbet)
        {
            client.EnableDenomination(new byte[] { 0x00, gamenumber }, denom, maxbet);
        }

        public void DisableDenomination(byte gamenumber, byte denom)
        {
            client.DeleteDenomination(new byte[] { 0x00, gamenumber }, denom);
        }


        public void ResetMeters()
        {
            client.ResetMeters();
        }
        public void LaunchExceptionByEvent(SASEvent event_)
        {
            SASCTL.GetInstance().CreditLimitExceeded = false;
            client.SendException(0x01, (byte)event_.GetHashCode(), new byte[] { });
        }

        public void StartHandpay(decimal handpayAmount, byte level, ulong partialPay, byte progressivegroup, byte resetId, byte type)
        {
            var ulongvalue = (ulong)(handpayAmount / sasReportedDenomination);
            client.RegisterHandpay(ulongvalue, level, partialPay, progressivegroup, resetId, type);
        }

        public void ResetHandpay(byte has_receipt, byte receipt_num)
        {
            client.ResetHandpay(has_receipt, receipt_num);
        }

        public void SetCredits(GLU8 type, decimal value)
        {
            var ulongvalue = (ulong)(value / sasReportedDenomination);
            client.SetCredits(type, ulongvalue);

        }

        public GLU32 GetMeter(byte code)
        {
            return client.GetMeter(code, new byte[] { 0x00, 0x00 });
        }

        public void SetMeter(string title, byte code, int value, bool sasreported)
        {
            client.UpdateMeter(code, new byte[] { 0x00, 0x00 }, value);

        }

        public void SetBillValidatorEnabledInSAS(bool enabled)
        {
            client.SetBillValidatorEnabledInSAS(enabled);
        }

        public void EnableAFT()
        {
            client.EnableAFT();
        }

        public void DisableAFT()
        {
            client.DisableAFT();
        }


        public void SetSASBusy(bool enabled)
        {
            client.SetSASBusy(enabled);
        }

        public void SetMaintenanceMode(bool set)
        {
            client.SetMaintenanceMode(set);
        }

        public void SetAssetNumber(int asset_number)
        {
            client.SetAssetNumber(asset_number);
        }

        public void SetSerialNumber(int serial_number)
        {
            client.SetSerialNumber(serial_number);
        }

        public void RequestPartialRAMClear()
        {
            client.RequestPartialRAMClear();
        }

        public void RequestFullRAMClear()
        {
            client.RequestFullRAMClear();
        }


        public void SetSASAddress(byte address)
        {
            client.SetSASAddress(address);
        }

        public bool AFTInProgress()
        {
            return transfer.status != AFTTransferStatus.Idle;
        }

        public decimal AFTInProgressTotalAmount()
        {
            return transfer.amount;
        }


        public void AcceptTransfer(bool toEGM, bool toHost)
        {
            AFTCashInGMLockCnt = false;
            client.AcceptTransfer(toEGM, toHost);

        }

        public void RejectTransfer(bool toEGM, bool toHost)
        {
            AFTCashInGMLockCnt = true;
            client.RejectTransfer(toEGM, toHost);

        }

        public void SetSASGameDetails(string cGameIDStr,
                                           string cAddiIDStr,
                                           string cGameNameStr,
                                           string cPayTableIDStr,
                                           string cPayTableNameStr,
                                           int MaxBet,
                                           int GameOption,
                                           int PaybackPerc,
                                           int CashoutLimit,
                                           int WagerCatNum)
        {
            client.SetSASGameDetails(cGameIDStr,
                                     cAddiIDStr,
                                     cGameNameStr,
                                     cPayTableIDStr,
                                     cPayTableNameStr,
                                     MaxBet,
                                     GameOption,
                                     PaybackPerc,
                                     CashoutLimit,
                                     WagerCatNum);
        }

    }
}