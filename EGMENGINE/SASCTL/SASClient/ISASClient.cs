using EGMENGINE.SASCTLModule.SASClient.GanlotSASClient.GXGSASAPIWrap;
using System;
using System.Collections.Generic;
using System.Text;
namespace EGMENGINE.SASCTLModule.SASClient
{

    interface ISASClient
    {
        /* Además vamos a tener ciertos eventos que lanza el SASClient a la virtualEGM, como peticiones o como avisos */
        // Getting adress of Virtual EGM
        event VirtualEGMGetAddressHandler VirtualEGMGetAddress;
        // 01 from VirtualEGM
        event VirtualEGM01Handler VirtualEGM01;
        // 02 from VirtualEGM
        event VirtualEGM02Handler VirtualEGM02;
        // 03 from VirtualEGM
        event VirtualEGM03Handler VirtualEGM03;
        // 04 from VirtualEGM
        event VirtualEGM04Handler VirtualEGM04;
        // 05 from VirtualEGM
        event VirtualEGM05Handler VirtualEGM05;
        // 06 from VirtualEGM
        event VirtualEGM06Handler VirtualEGM06;
        // 07 from VirtualEGM
        event VirtualEGM07Handler VirtualEGM07;
        // 08 from VirtualEGM
        event VirtualEGM08Handler VirtualEGM08;
        // 09 from VirtualEGM
        event VirtualEGM09Handler VirtualEGM09;
        // 0A from VirtualEGM
        event VirtualEGM0AHandler VirtualEGM0A;
        // 0B from VirtualEGM
        event VirtualEGM0BHandler VirtualEGM0B;
        // 0E from VirtualEGM
        event VirtualEGM0EHandler VirtualEGM0E;
        // 72 from VirtualEGM (Transfers)                                                                                            
        event VirtualEGMLP72Handler VirtualEGMLP72;
        // 72 from VirtualEGM (Interrogate)                                                                                            
        event VirtualEGMLP72IntHandler VirtualEGMLP72Int;
        // Register Gaming Machine
        event VirtualEGM73Handler VirtualEGM73;
        // 75 from VirtualEGM
        event VirtualEGM75Handler VirtualEGM75;
        // 7F from VirtualEGM
        event VirtualEGM7FHandler VirtualEGM7F;
        // 8B from VirtualEGM
        event VirtualEGM8BHandler VirtualEGM8B;
        // AA from VirtualEGM
        event VirtualEGMAAHandler VirtualEGMAA;
        // 94 from VirtualEGM
        public event VirtualEGM94Handler VirtualEGM94;
        // A8 from VirtualEGM
        public event VirtualEGMA8Handler VirtualEGMA8;
        // Un evento que se lanza cuando se envía un long poll
        event CommandSentHandler CommandSent;
        // Un evento que se lanza cuando se recibe un long poll
        event CommandReceivedHandler CommandReceived;
        // Un evento que se lanza cuando no hay conexion con la SMIB (chirping)
        event SmibLinkDownHandler SmibLinkDown;
        // Un evento que se lanza cuando hay un error crítico en el sas controller
        event ClientCriticalErrorHandler ClientCriticalError;
        // Un evento que se lanza cuando no hay ningún error en el sas controller
        event ClientNoErrorHandler ClientNoError;
        // Un evento que se lanza cuando no hay conexiòn a SAS
        event SASLinkDownHandler SASLinkDown;

        event VirtualEGM74Handler VirtualEGM74;
        event VirtualEGMLP74IntHandler VirtualEGMLP74Int;
        /// <summary>
        /// Chequeo de CRC, toma los dos últimos bytes y lo compara con el crc del resto del array de bytes
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        bool CheckCRC(byte[] bytes);


        /// <summary>
        /// Comienzo de ejecución del client. Abre el puerto en el que se pasa como parámetro y comienza a escuchar
        /// </summary>
        /// <param name="port"></param>
        void Start(string port);

        /// <summary>
        /// Habilitar SAS
        /// </summary>
        /// <param name="port"></param>
        void Enable();

        /// <summary>
        /// Detiene la ejecución del ciclo
        /// </summary>
        void Stop();

        /// <summary>
        /// Intérprete de long polls
        /// </summary>
        /// <param name="buffer"></param>
        void AnalyzeLongPoll(byte[] buffer);

        void SetBillValidatorEnabledInSAS(bool enabled);
        // Set Cash out limit
        void SetCashoutLimit(byte[] gameNumber, ulong cashoutLimit);
        // Set Current Player Denomination
        void SetCurrentPlayerDenomination(byte pldenom);
        // Enable Game Denomination
        void EnableDenomination(byte[] gameNumber, byte code, ushort maxbet);
        // Delete Game Denomination
        void DeleteDenomination(byte[] gameNumber, byte code);
        // Response Registration
        void SendRegistrationResponse(byte address, byte command, byte length, byte reg_status, byte[] asset_number, byte[] registration_key, byte[] pos_id);

        // Response Registration
        void SendExtendedMetersResponse(byte command, byte address, byte[] gameNumber, byte[] meters);

        // Response for single meter
        void SendGamingMachineSingleMeterAccountingResponse(byte address, byte single_meter_accounting_long_poll, byte[] meter);

        // Response and Completeness of Transfer

        void FinishAFTTransfer(byte trans_status, ulong cash_amount, ulong res_amount, ulong nonres_amount);

        // Send Exception
        void SendException(byte address, byte exception, byte[] data);

        // Update Meter
        void UpdateMeter(byte code, byte[] gameNumber, int value);

        // SetCredits
        void SetCredits(byte type, ulong value);

        // GetMeter
        uint GetMeter(byte code, byte[] gameNumber);

        // Reset All Meters
        void ResetMeters();

        // Update Features
        void UpdateFeatures(byte features1, byte features2, byte features3);

        // Update Features
        void UpdateFeatures(bool EnableJackpotMultiplier,
                            bool EnableAFTBonusAwards,
                            bool EnableLegacyBonusAwards,
                            bool EnableTournament,
                            bool EnableValidationExtensions,
                            bool EnableValidationStyleSystem,
                            bool EnableValidationStyleSecureEnhanced,
                            bool EnableTicketRedemption,
                            bool EnableMeterModelWonCreditsMeteredWhenWon,
                            bool EnableMeterModelWonCreditsMeteredWhenPlayedOrPaid,
                            bool EnableTicketsToTotalDropAndTotalCancelledCredits,
                            bool EnableExtendedMeters,
                            bool EnableComponentAuthentication,
                            bool EnableAFT,
                            bool EnableMultiDenomExtensions,
                            bool EnableMaxPollingRate,
                            bool EnableReportingOfMultipleProgressiveWins);


        // Register Handpay
        void RegisterHandpay(ulong handpayAmount, byte level, ulong PartialPay, byte progressivegroup, byte resetId, byte Type);

        // Reset Handpay
        void ResetHandpay(byte has_receipt, byte receipt_num);

        // Set Busy
        void SetSASBusy(bool enable);

        // Set Maintenance Mode
        void SetMaintenanceMode(bool set);

        // Set Asset Number
        void SetAssetNumber(int asset_number);
        // Set Serial Number
        void SetSerialNumber(int serial_number);
        // RequestRAMClear to Client (Exceptions)
        int RequestPartialRAMClear();
        int RequestFullRAMClear();
        // Set SAS Address
        void SetSASAddress(byte address);
        // Set SAS Game Details
        void SetSASGameDetails(string cGameIDStr,
                               string cAddiIDStr,
                               string cGameNameStr,
                               string cPayTableIDStr,
                               string cPayTableNameStr,
                               int MaxBet,
                               int GameOption,
                               int PaybackPerc,
                               int CashoutLimit,
                               int WagerCatNum);
        void EnableAFT();
        void DisableAFT();
        void AcceptTransfer(bool toEGM, bool toHost);
        void RejectTransfer(bool toEGM, bool toHost);

        public void CheckAndHandleAFTLocks();
        public void CancelAFTLock();

    }
}
