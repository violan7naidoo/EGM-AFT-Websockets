
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using EGMENGINE.SASCTLModule.SASClient;
using EGMENGINE.EGMStatusModule.AFTTransferModule;
using System.Timers;
using System.ComponentModel.Design;

namespace EGMENGINE.SASCTLModule.SASClient.DevSASClient
{

    internal partial class DevSASClient : ISASClient
    {
        
        private static Dictionary<byte, byte[]> componentSet = new Dictionary<byte, byte[]>();

        private System.Timers.Timer m_pSASAFTLockTimer = new System.Timers.Timer(100);

        private System.Timers.Timer m_pSASHandlerTimer = new System.Timers.Timer(100);

        private bool gamedisabled_ = false;

        private bool billacceptordisabled_ = false;

        private uint dwDenomCfg_ = 0;

        private byte bActionFlag_ = 0x00;

        private bool autorebetdisabled_ = false;

        private bool sounddisabled_ = false;

        private bool reelspinorgameplaysounddisabled_ = false;

        private bool maintenancemode_ = false;

        private byte total_game_num_ = 0x00;

        private UInt32 enabled_game_mask_ = 0;


        #region "Events"
        /* Además vamos a tener ciertos eventos que lanza el SASClient a la virtualEGM, como peticiones o como avisos */
        // Getting adress of Virtual EGM
        public event VirtualEGMGetAddressHandler VirtualEGMGetAddress;
        // 01 from VirtualEGM
        public event VirtualEGM01Handler VirtualEGM01;
        // 02 from VirtualEGM
        public event VirtualEGM02Handler VirtualEGM02;
        // 03 from VirtualEGM
        public event VirtualEGM03Handler VirtualEGM03;
        // 04 from VirtualEGM
        public event VirtualEGM04Handler VirtualEGM04;
        // 05 from VirtualEGM
        public event VirtualEGM05Handler VirtualEGM05;
        // 06 from VirtualEGM
        public event VirtualEGM06Handler VirtualEGM06;
        // 07 from VirtualEGM
        public event VirtualEGM07Handler VirtualEGM07;
        // 08 from VirtualEGM
        public event VirtualEGM08Handler VirtualEGM08;
        // 09 from VirtualEGM
        public event VirtualEGM09Handler VirtualEGM09;
        // 0A from VirtualEGM
        public event VirtualEGM0AHandler VirtualEGM0A;
        // 0B from VirtualEGM
        public event VirtualEGM0BHandler VirtualEGM0B;
        // 0E from VirtualEGM
        public event VirtualEGM0EHandler VirtualEGM0E;
        // 72 from VirtualEGM (Transfers)                                                                                            
        public event VirtualEGMLP72Handler VirtualEGMLP72;
        // 72 from VirtualEGM (Interrogate)                                                                                            
        public event VirtualEGMLP72IntHandler VirtualEGMLP72Int;
        // 75 from VirtualEGM
        public event VirtualEGM75Handler VirtualEGM75;
        // 7F from VirtualEGM
        public event VirtualEGM7FHandler VirtualEGM7F;
        // 8B from VirtualEGM
        public event VirtualEGM8BHandler VirtualEGM8B;
        // 94 from VirtualEGM
        public event VirtualEGM94Handler VirtualEGM94;
        // A8 from VirtualEGM
        public event VirtualEGMA8Handler VirtualEGMA8;
        // AA from VirtualEGM
        public event VirtualEGMAAHandler VirtualEGMAA;

        // Register Gaming Machine
        public event VirtualEGM73Handler VirtualEGM73;

        // Un evento que se lanza cuando se envía un long poll
        public event CommandSentHandler CommandSent;
        // Un evento que se lanza cuando se recibe un long poll
        public event CommandReceivedHandler CommandReceived;
        // Un evento que se lanza cuando no hay conexion con la SMIB (chirping)
        public event SmibLinkDownHandler SmibLinkDown;
        // Un evento que se lanza cuando hay un error crítico en el sas controller
        public event ClientCriticalErrorHandler ClientCriticalError;
        // Un evento que se lanza cuando no hay ningún error en el sas controller
        public event ClientNoErrorHandler ClientNoError;
        // Un evento que se lanza cuando no hay ningún error en el sas controller
        public event SASLinkDownHandler SASLinkDown;

        public event VirtualEGM74Handler VirtualEGM74;
        public event VirtualEGMLP74IntHandler VirtualEGMLP74Int;
        #endregion








        /****************************************************************************************************************************/
        /****************************************************************************************************************************/
        /****************************************************************************************************************************/
        /****************************************************************************************************************************/
        /****************************************************************************************************************************/
        /***********************************************     CLIENT CONTROL    ******************************************************/
        /****************************************************************************************************************************/
        /****************************************************************************************************************************/
        /****************************************************************************************************************************/
        /****************************************************************************************************************************/
        /****************************************************************************************************************************/
        /****************************************************************************************************************************/

        #region CLIENT CONTROL
        /// <summary>
        /// Chequeo de CRC, toma los dos últimos bytes y lo compara con el crc del resto del array de bytes
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public bool CheckCRC(byte[] bytes)
        {
          
            return true;

        }
        #endregion





        /********************************************/
        /**************  RUNNING ********************/
        /********************************************/

        #region RUNNING


        /// <summary>
        /// Inicialización del client, con los diferentes timers
        /// </summary>
        /// <param name="print_"></param>
        public DevSASClient(bool print_)
        {
          

        }

        public void CheckAndHandleSAS()
        {
            
        }

        public void CheckAndHandleAFTTrans()
        {

        
        }

        public void CheckAndHandleAFTLocks() { }
        public void CancelAFTLock() { }

        /// <summary>
        /// Comienzo de ejecución del client. Abre el puerto en el que se pasa como parámetro y comienza a escuchar
        /// </summary>
        /// <param name="port"></param>
        public void Start(string port)
        {


        }


        /// <summary>
        /// Escritura en el puerto, escribe lo que se encuentra en buffer, e imprime un log dependiendo si print1 está activado o no
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="print1"></param>
        public void writePort(byte[] buffer, bool print1)
        {

        }


        /// <summary>
        /// Detiene la ejecución del ciclo
        /// </summary>
        public void Stop()
        {
        }

        #endregion


        /****************************************************************************************************************************/
        /****************************************************************************************************************************/
        /****************************************************************************************************************************/
        /****************************************************************************************************************************/
        /****************************************************************************************************************************/
        /****************************************     ANALYZE LONGPOLLS  (ORDENADO POR LONGPOLL)    *********************************/
        /****************************************************************************************************************************/
        /****************************************************************************************************************************/
        /****************************************************************************************************************************/
        /****************************************************************************************************************************/
        /****************************************************************************************************************************/
        /****************************************************************************************************************************/

        #region "Analyze Longpolls"

  
        /// <summary>
        /// Intérprete de long polls
        /// </summary>
        /// <param name="buffer"></param>
        public void AnalyzeLongPoll(byte[] buffer)
        {

        }


        #endregion


        /****************************************************************************************************************************/
        /****************************************************************************************************************************/
        /****************************************************************************************************************************/
        /****************************************************************************************************************************/
        /****************************************************************************************************************************/
        /****************************************     RESPONSES    (ORDENADO POR LONGPOLL)            *******************************/
        /****************************************************************************************************************************/
        /****************************************************************************************************************************/
        /****************************************************************************************************************************/
        /****************************************************************************************************************************/
        /****************************************************************************************************************************/
        /****************************************************************************************************************************/

        #region "Responses"

        // Set Cashout Limit
        public void SetCashoutLimit(byte[] gameNumber, ulong cashoutLimit)
        {
         

        }

        // Set Current Player Denomination
        public void SetCurrentPlayerDenomination(byte denom_code)
        {
        }

        // Enable Game Denomination
        public void EnableDenomination(byte[] gameNumber, byte code, ushort maxbet)
        {

        }

        // Delete Game Denomination
        public void DeleteDenomination(byte[] gameNumber, byte code)
        {
          
        }


        // Response Registration
        public void SendRegistrationResponse(byte address,
                                          byte command,
                                          byte length,
                                          byte reg_status,
                                          byte[] asset_number,
                                          byte[] registration_key,
                                          byte[] pos_id)
        {


        }

        // Response Registration
        public void SendExtendedMetersResponse(byte command,
                                                byte address,
                                                byte[] gameNumber,
                                                byte[] meters)
        {


        }

        // Response for single meter
        public void SendGamingMachineSingleMeterAccountingResponse(byte address,
                                                                   byte single_meter_accounting_long_poll,
                                                                   byte[] meter)
        {


        }

        private string ByteToArrayStr(byte[] arr)
        {
            return BitConverter.ToString(arr).Replace("-", "");
        }

        // Register Handpay
        public void RegisterHandpay(ulong handpayAmount, byte level, ulong PartialPay, byte progressivegroup, byte resetId, byte Type)
        {
        }

        // Reset Handpay
        public void ResetHandpay(byte has_receipt, byte receipt_num)

        {
          
        }

        // Send Exception
        public void SendException(byte address, byte exception, byte[] data)
        {

        }

        // Update Meter
        public void UpdateMeter(byte code, byte[] gameNumber, int value)
        {
        }

        // Get Meter
        public uint GetMeter(byte code, byte[] gameNumber)
        {
            ulong res = 0;

            return (uint)res;
        }

        public void ResetMeters()
        {
        }
        /// <summary>
        /// Dado un byte y una posición, obtengo el bit de esa posición
        /// </summary>
        /// <param name="b">Un byte</param>
        /// <param name="pos">Una posición</param>
        /// <returns>El bit de b en esa posición</returns>
        private static byte GetByteWithPos(byte b, int pos)
        {
            return (byte)(b & (1 << pos));
        }

        // Update Features
        public void UpdateFeatures(byte features1, byte features2, byte features3)
        {
           


        }

        public void UpdateFeatures(bool EnableJackpotMultiplier, 
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
                                   bool EnableReportingOfMultipleProgressiveWins)
        {
           


        }

        public void FinishAFTTransfer(byte trans_status, ulong cash_amount, ulong res_amount, ulong nonres_amount)
        {
          
        }

        public void SetCredits(byte type, ulong value)
        {
        }

        void ISASClient.SetBillValidatorEnabledInSAS(bool enabled)
        {
            billacceptordisabled_ = !enabled;
        }

        void ISASClient.SetSASBusy(bool enable)
        {
        }

        void ISASClient.SetMaintenanceMode(bool set)
        {
            maintenancemode_ = set;
        }

        void ISASClient.SetAssetNumber(int asset_number)
        {

        }

        public int RequestFullRAMClear()
        {
            return 0;
        }

        public int RequestPartialRAMClear()
        {
            return 0;
        }

        void ISASClient.SetSASAddress(byte address)
        {

        }

        void ISASClient.SetSerialNumber(int serial_number)
        {

        }

        void ISASClient.SetSASGameDetails(string cGameIDStr,
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

        }

        void ISASClient.EnableAFT()
        {

        }

        void ISASClient.DisableAFT()
        {

        }

        public void Enable()
        {

        }

        void ISASClient.AcceptTransfer(bool toEGM, bool toHost)
        {
        }

        void ISASClient.RejectTransfer(bool toEGM, bool toHost)
        {
        }



        #endregion














    }
}

