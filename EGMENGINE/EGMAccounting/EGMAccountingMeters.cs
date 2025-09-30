
using EGMENGINE.EGMDataPersisterModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace EGMENGINE.EGMAccountingModule
{
    internal enum SASMeter
    {
        TotalCoinIn = 0x00,
        TotalCoinOut = 0x01,
        TotalJackpotMeter = 0x02,
        TotalHandpayCancelledCredits = 0x03,
        TotalCancelledCredits = 0x04,
        TotalGames = 0x05,
        GamesWon = 0x06,
        GamesLost = 0x07,
        TotalCreditsFromBillAccepted = 0x0B,
        TotalCredits = 0x0C,
        CurrentRestrictedCredits = 0x1B,
        TotalMachinePaidPaytableWin = 0x1C,
        TotalWonCredits = 0x22,
        TotalHandpayCredits = 0x23,
        TotalDropCredits = 0x24,
        GamesSinceLastPowerUp = 0x25,
        GamesSinceDoorClosure = 0x26,
        NumberOfBillsCurrentlyInTheStacker = 0x3E,
        TotalValueOfBillsCurrentlyInTheStacker = 0x3F,
        Total1BillsAccepted = 0x40,
        Total2BillsAccepted = 0x41,
        Total5BillsAccepted = 0x42,
        Total10BillsAccepted = 0x43,
        Total20BillsAccepted = 0x44,
        Total25BillsAccepted = 0x45,
        Total50BillsAccepted = 0x46,
        Total100BillsAccepted = 0x47,
        Total200BillsAccepted = 0x48,
        Total250BillsAccepted = 0x49,
        Total500BillsAccepted = 0x4A,
        Total1000BillsAccepted = 0x4B,
        Total2000BillsAccepted = 0x4C,
        Total2500BillsAccepted = 0x4D,
        Total5000BillsAccepted = 0x4E,
        Total10000BillsAccepted = 0x4F,
        Total20000BillsAccepted = 0x50,
        Total25000BillsAccepted = 0x51,
        Total50000BillsAccepted = 0x52,
        Total100000BillsAccepted = 0x53,
        Total200000BillsAccepted = 0x54,
        Total250000BillsAccepted = 0x55,
        Total500000BillsAccepted = 0x56,
        Total1000000BillsAccepted = 0x57
}
    /// <summary>
    /// Class EGMSettings definition. Singleton
    /// </summary>
    internal class EGMAccountingMeters
    {
        public string MeterCHK;
        public int M00; // Total Coin in credits
        public int M01; // Total Coin out credits
        public int M02; // Total Jackpot Credits
        public int M03; // Total Hand Pay Cancelled credits
        public int M04; // Total Cancelled Credits
        public int M05; // Games Played
        public int M06; // Games Won
        public int M07; // Games Lost
        public int M08; // Total Credits from Coin Acceptor
        public int M09; // Total Credits paid from hopper
        public int M0A; // Total credits from coins to drop
        public int M0B; // Total credits from bills accepted
        public int M0C; // Current Credits
        public int M0D; // Total cashable ticket in (cents)
        public int M0E; // Total cashable ticket out (cents)
        public int M0F; // Total restricted ticket in (cents)
        public int M10; // Total restricted ticket out (cents)
        public int M11; // Total SAS cashable ticket in, including nonrestricted tickets (quantity)
        public int M12; // Total SAS cashable ticket out, including nonrestricted tickets (quantity)
        public int M13; // Total SAS restricted ticket in (quantity)       
        public int M14; // Total SAS restricted ticket out (quantity)
        public int M15; // Total ticket in credits
        public int M16; // Total ticket out credits
        public int M17; // Total electronic transfers to gaming machine, including cashable, nonrestricted, restricted and debit, whether transfer is to credit meter or to ticket (credits) 
        public int M18; // Total electronic transfers to host, including cashable, nonrestricted, restricted and win amounts (credits) 
        public int M19; // Total restricted amount played (credits)
        public int M1A; // Total nonrestricted amount played (credits)
        public int M1B; // Current Restricted Credits
        public int M1C; // Total machine paid paytable win, not including progressive or external bonus amounts (credits)
        public int M1D; // Total machine paid progressive win (credits)
        public int M1E; // Total machine paid external bonus win (credits)
        public int M1F; // Total attendant paid paytable win, not including progressive or external bonus amounts (credits)
        public int M20; // Total attendant paid progressive win (credits)
        public int M21; // Total attendant paid external bonus win (credits)
        public int M22; // Total won credits (sum of total coin out and total jackpot) 
        public int M23; // Total hand paid credits (sum of total hand paid cancelled credits and total jackpot
        public int M24; // Total drops credits
        public int M25; // Total games since last power reset
        public int M26; // Total games since last door closure
        public int M27; // Total credits from external coin acceptor
        public int M28; // Total cashable in credits, including non-restricted
        public int M29; // Total regular cashable ticket in credits
        public int M2A; // Total restricted promotional ticket in credits
        public int M2B; // Total nonrestricted promotional tickets in credits
        public int M2C; // Total cashable ticket out credits
        public int M2D; // Total restricted promotional ticket out credits
        public int M2E; // Electronic regular cashable transfers to gaming machine, not including external bonus awards (credits)
        public int M2F; // Electronic restricted promotional transfers to gaming machine, not including external bonus awards (credits) 
        public int M30;
        public int M31;
        public int M32;
        public int M33;
        public int M34;
        public int M35; // Total regular cashable ticket in count
        public int M36; // Total restricted promotional ticket out credits
        public int M37; // Total nonrestricted ticket in count
        public int M38; // Total cashable out count, including debit ticket
        public int M39; // Total restricted promotional ticket out count
        public int M3A;
        public int M3B;
        public int M3C;
        public int M3D;
        public int M3E; // Number of bills currently in stacker
        public int M3F; // Total value of bills currently in stacker (Credits)
        public int M40; // Total number of $1 bills accepted
        public int M41; // Total number of $2 bills accepted
        public int M42; // Total number of $5 bills accepted
        public int M43; // Total number of $10 bills accepted
        public int M44; // Total number of $20 bills accepted
        public int M45; // Total number of $25 bills accepted
        public int M46; // Total number of $50 bills accepted
        public int M47; // Total number of $100 bills accepted
        public int M48; // Total number of $200 bills accepted
        public int M49; // Total number of $250 bills accepted
        public int M4A; // Total number of $500 bills accepted
        public int M4B; // Total number of $1000 bills accepted
        public int M4C; // Total number of $2000 bills accepted
        public int M4D; // Total number of $2500 bills accepted
        public int M4E; // Total number of $5000 bills accepted
        public int M4F; // Total number of $10000 bills accepted
        public int M50; // Total number of $20000 bills accepted
        public int M51; // Total number of $25000 bills accepted
        public int M52; // Total number of $50000 bills accepted
        public int M53; // Total number of $100000 bills accepted
        public int M54; // Total number of $200000 bills accepted
        public int M55; // Total number of $250000 bills accepted
        public int M56; // Total number of $500000 bills accepted
        public int M57; // Total number of $1000000 bills accepted
        public int M58; //
        public int M59; //
        public int M5A; //
        public int M5B; //
        public int M5C; //
        public int M5D; //
        public int M5E; //
        public int M5F; //
        public int M60; //
        public int M61; //
        public int M62; //
        public int M63; //
        public int M64; //
        public int M65; //
        public int M66; //
        public int M67; //
        public int M68; //
        public int M69; //
        public int M6A; //
        public int M6B; //
        public int M6C; //
        public int M6D; //
        public int M6E; //
        public int M6F; //
        public int M70; //
        public int M71; //
        public int M72; //
        public int M73; //
        public int M74; //
        public int M75; //
        public int M76; //
        public int M77; //
        public int M78; //
        public int M79; //
        public int M7A; //
        public int M7B; //
        public int M7C; //
        public int M7D; //
        public int M7E; //
        public int M7F; //
        public int M80; // Regular cashable ticket in cents
        public int M81; // Regular cashable ticket in count
        public int M82; // Restricted ticket in cent
        public int M83; // Restricted ticket in count
        public int M84; // Nonrestricted ticket in cents
        public int M85; // Nonrestricted ticket in count
        public int M86; // Regular cashable ticket out cents
        public int M87; // Regular cashable ticket out count
        public int M88; // Restricted ticket out cents
        public int M89; // Restricted ticket out counts
        public int M8A; // Debit ticket out cents
        public int M8B; // Debit ticket out counts
        public int M8C; // Validated cancelled credit handpay, receipt printed cents
        public int M8D; // Validated cancelled credit handpay, receipt printed counts
        public int M8E; // Validated jackpot handpay, receipt printed cents
        public int M8F; // Validated jackpot handpay, receipt printed counts
        public int M90; // Validated cancelled credit handpay, no receipt cents
        public int M91; //  Validated cancelled credit handpay, no receipt counts
        public int M92; // Validated jackpot handpay, no receipt cents
        public int M93; // Validated jackpot handpay, no receipt counts
        public int MA0; // AFT in House cashable transfer to gaming machine (cents)
        public int MA1; // AFT in House cashable transfer to gaming machine (quantity)
        public int MA2; // AFT in House restricted transfer to gaming machine cents
        public int MA3; // AFT in House restricted transfer to gaming machine counts
        public int MA4; // AFT in House nonrestricted transfer to gaming machine (cents)
        public int MA5; // AFT in House nonrestricted transfer to gaming machine (quantity)
        public int MA6; // AFT debit transfer to gaming machine (cents)
        public int MA7; // AFT debit transfer to gaming machine (quantity)
        public int MA8; // AFT In House cashable transfer to ticket (cents)
        public int MA9; // AFT In House cashable transfer to ticket (quantity)
        public int MAA; // AFT In House restricted transfer to ticket (cents)
        public int MAB; // AFT In House restricted transfer to ticket (quantity)
        public int MAC; // AFT Debit transfer to ticket (cents)
        public int MAD; // AFT Debit transfer to ticket (quantity)
        public int MAE; // AFT Bonus cashable transfer to gaming machine (cents)
        public int MAF; // AFT Bonus cashable transfer to gaming machine (quantity) 
        public int MB0; // AFT Bonus nonrestricted transfer to gaming machine (cents)
        public int MB1; // AFT Bonus nonrestricted transfer to gaming machine (quantity)
        public int MB8; // AFT In House cashable transfer to host (cents)
        public int MB9; // AFT In House cashable transfer to host (quantity)
        public int MBA; // AFT In House restricted transfer to host (cents)
        public int MBB; // AFT In House restricted transfer to host (quantity)
        public int MBC; // AFT In House nonrestricted transfer to host (cents)
        public int MBD; // AFT In House nonrestricted transfer to host (quantity)
        public int MFA; //
        public int MFB; //
        public int MFC; //
        public int MFD; //
        public int MFE; // 
        public int MFF; //
        public int MTotalBillMeterInDollars;
        public int MTrueCoinIn;
        public int MTrueCoinOutMeter;
        public int MBonusingDeductible;
        public int MBonusingNoDeductible;
        public int MBonusingWagerMatch;
        public int MBasicTotalCoinIn;
        public int MBasicTotalCoinOut;
        public int MBasicTotalDrop;
        public int MBasicTotalJackPot;
        public int MBasicGamesPlayed;
        public int MBasicGamesWon;
        public int MBasicSlotDoorOpen;
        public int MBasicSlotDoorClose;
        public int MBasicPowerReset;
        public int MBasicLogicDoorOpen;
        public int MBasicLogicDoorClose;
        public int MBasicCashboxDoorOpen;
        public int MBasicCashboxDoorClose;
        public int MBasicDropDoorOpen;
        public int MBasicDropDoorClose;
        public int MBasicStackerOpen;
        public int MBasicStackerClose;
        public int MBillsJammed;
        public int MSASInterfaceError;
        public int MTotalTilts;
        public int MGeneralTilt;

        public EGMAccountingMeters()
        {

        }


    }
}
