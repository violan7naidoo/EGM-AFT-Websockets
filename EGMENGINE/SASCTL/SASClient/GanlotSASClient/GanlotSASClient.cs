
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using EGMENGINE.SASCTLModule.SASClient;
using GXGGFMAPI;
using EGMENGINE.EGMStatusModule.AFTTransferModule;
using System.Timers;
using EGMENGINE.SASCTLModule.SASClient.GanlotSASClient.GXGSASAPIWrap;
using System.ComponentModel.Design;
using System.Net.NetworkInformation;
using GXGGFMAPI;
using GXGCoreAPI;
using System.Net;

namespace EGMENGINE.SASCTLModule.SASClient.GanlotSASClient
{

    internal partial class GanlotSASClient : ISASClient
    {

        private static Dictionary<byte, byte[]> componentSet = new Dictionary<byte, byte[]>();

        private System.Timers.Timer m_pSASAFTLockTimer = new System.Timers.Timer(100);

        private System.Timers.Timer m_pSASHandlerTimer = new System.Timers.Timer(100);

        private bool gamedisabled_ = false;

        private byte sasId_address = 0x01;

        private bool billacceptordisabled_ = false;

        private uint dwDenomCfg_ = 0;

        private byte bActionFlag_ = 0x00;

        private bool autorebetdisabled_ = false;

        private bool sounddisabled_ = false;

        private bool reelspinorgameplaysounddisabled_ = false;

        private bool maintenancemode_ = false;

        private byte total_game_num_ = 0x00;

        private UInt32 enabled_game_mask_ = 0;

        private byte g_bSASValidLBPort = 0;
        private byte g_bSASAFTPort = 0;
        private byte g_bSASProgPort = 0;
        private int assetnumber = 12345;
        private int totalgames = 1;

        private GXGSASROMSigCalculator gXGSASROMSigCalculator;

        

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
        public GanlotSASClient(bool print_)
        {
            m_pSASAFTLockTimer.Elapsed += new ElapsedEventHandler((s, e) => { CheckAndHandleAFTTrans(); });

            m_pSASHandlerTimer.Elapsed += new ElapsedEventHandler((s, e) => { CheckAndHandleSAS(); });

            gXGSASROMSigCalculator = new GXGSASROMSigCalculator();

            string cmdLine;
            char[] exitKey = new char[] { 'q', 'Q' };
            char[] splitKey = new char[] { ' ', '\n' };
            string[] cmdKey;


        }


        public void SASHandlerp(SAS_INTR_DATA data)
        {

            SAS_INTR_TYPE type = (SAS_INTR_TYPE)data.Status;
            switch (type)
            {
                case SAS_INTR_TYPE.SAS_INTR_FINISH_SYNC:
                    SASLinkDown(false, new EventArgs());

                    break;
                case SAS_INTR_TYPE.SAS_INTR_LINK_DOWN:
                    SASLinkDown(true, new EventArgs());
                    break;

                // ### ADDED TO HANDLE AFT LOCK ###
                case SAS_INTR_TYPE.SAS_INTR_GET_AFT_LOCK_INFO:
                    // This interrupt is fired for any LP74 related to locking.
                    // The Ganlot library has already processed the poll and is now asking your application what to do.
                    // The lock *status* tells us what state the library is in (e.g., pending).
                    byte lockStatus = GXGSASAPIBridge.GET_AFT_LOCK_STATUS(ref data);

                    if (lockStatus == (byte)SAS_AFT_LOCK_STATUS.SAS_AFT_LOCK_STATUS_PENDING)
                    {
                        // The host sent a lock request (lock code 0x00). The SAS library has correctly
                        // entered the 'pending' state and is waiting for your game logic to confirm or deny the lock.
                        // We fire the event with lock code 0x00 to trigger the logic in sasctl.cs.
                        VirtualEGM74?.Invoke(data.PortNum, 0x00, EventArgs.Empty);
                    }
                    else if (lockStatus == (byte)SAS_AFT_LOCK_STATUS.SAS_AFT_LOCK_STATUS_NOT_LOCKED)
                    {
                        // This state is entered after a lock is cancelled (lock code 0x80), times out, or is broken by a link down.
                        // We fire the event with lock code 0x80 to ensure the main application logic unlocks the EGM.
                        VirtualEGM74?.Invoke(data.PortNum, 0x80, EventArgs.Empty);
                    }
                    break;
            }

        }

        public void CheckAndHandleSAS()
        {
            ushort status = 0;
            var dwStatus = GXGSASAPIBridge.GXG_SAS_GetHostCtrlMachineStatus(ref status);

            if (dwStatus != 0)
            {
                ClientCriticalError("Please Reboot: SAS CRITICAL ERROR " + dwStatus, new EventArgs());
                m_pSASHandlerTimer.Stop();
                m_pSASAFTLockTimer.Stop();
            }
            else
            {
                bool GameDisabled;
                bool SoundDisabled;
                bool BillAcceptorDisabled;
                bool ReelSpinOrGamePlaySoundDisabled;
                bool AutoRebetDisabled;
                bool MaintenanceMode;

                // Game Disabled
                if ((status & (ushort)BITMASK_SAS_MACHINE_STATUS.ENABLE_PLAY.GetHashCode()) > 0)
                {
                    GameDisabled = false;
                }
                else
                {
                    GameDisabled = true;
                }

                try
                {
                    // Enable/Disable game

                    bool gamedisabled = GameDisabled;
                    if (gamedisabled_ != gamedisabled)
                    {
                        gamedisabled_ = gamedisabled;
                        if (gamedisabled)
                            VirtualEGM01(0x01, new EventArgs());
                        else if (!gamedisabled)
                            VirtualEGM02(0x01, new EventArgs());
                    }
                }
                catch { }


                //  Enable Bill Acceptor
                if ((status & (ushort)BITMASK_SAS_MACHINE_STATUS.ENABLE_BA.GetHashCode()) > 0)
                {
                    BillAcceptorDisabled = false;
                }
                else
                {
                    BillAcceptorDisabled = true;
                }
                try
                {
                    // Enable/Disable bill acceptor
                    bool billacceptordisabled = BillAcceptorDisabled;

                    if (billacceptordisabled_ != billacceptordisabled)
                    {
                        billacceptordisabled_ = billacceptordisabled;
                        if (billacceptordisabled)
                            VirtualEGM07(0x01, new EventArgs());
                        else if (!billacceptordisabled)
                            VirtualEGM06(0x01, new EventArgs());
                    }
                }
                catch { }


                // Sound Disabled
                if ((status & (ushort)BITMASK_SAS_MACHINE_STATUS.ENABLE_ALL_SOUND.GetHashCode()) > 0)
                {
                    SoundDisabled = false;
                }
                else
                {
                    SoundDisabled = true;
                }
                try
                {
                    // Enable/Disable sound

                    bool sounddisabled = SoundDisabled;
                    if (sounddisabled_ != sounddisabled)
                    {
                        sounddisabled_ = sounddisabled;
                        if (sounddisabled)
                            VirtualEGM03(0x01, new EventArgs());
                        else if (!sounddisabled)
                            VirtualEGM04(0x01, new EventArgs());
                    }
                }
                catch { }


                // Reel Spin or Game Play Sound Disabled
                if ((status & (ushort)BITMASK_SAS_MACHINE_STATUS.ENABLE_GAME_PLAY_SOUND.GetHashCode()) > 0)
                {
                    ReelSpinOrGamePlaySoundDisabled = false;
                }
                else
                {
                    ReelSpinOrGamePlaySoundDisabled = true;
                }
                try
                {
                    // Enable/Disable Reel Spin Or Game Play Sound
                    bool reelspinorgameplaysounddisabled = ReelSpinOrGamePlaySoundDisabled;
                    if (reelspinorgameplaysounddisabled_ != reelspinorgameplaysounddisabled)
                    {
                        reelspinorgameplaysounddisabled_ = reelspinorgameplaysounddisabled;
                        if (reelspinorgameplaysounddisabled)
                            VirtualEGM05(0x01, new EventArgs());
                    }
                }
                catch { }


                // Enable Auto rebet
                if ((status & (ushort)BITMASK_SAS_MACHINE_STATUS.DISABLE_AUTO_BET.GetHashCode()) > 0)
                {
                    AutoRebetDisabled = false;
                }
                else
                {
                    AutoRebetDisabled = true;
                }
                try
                {
                    // Enable/Disable auto rebet
                    bool autorebetdisabled = AutoRebetDisabled;
                    if (autorebetdisabled_ != autorebetdisabled)
                    {
                        autorebetdisabled_ = autorebetdisabled;
                        if (autorebetdisabled)
                        {
                            VirtualEGMAA(0x01, 0x01, new EventArgs());
                        }
                        else if (!autorebetdisabled)
                        {
                            VirtualEGMAA(0x01, 0x00, new EventArgs());
                        }
                    }
                }
                catch { }

                //// Enter maintenance mode
                //if ((status & (ushort)BITMASK_SAS_MACHINE_STATUS.LEAVE_MAINTAIN_MODE.GetHashCode()) > 0)
                //{
                //    MaintenanceMode = false;
                //}
                //else
                //{
                //    MaintenanceMode = true;
                //}
                //try
                //{
                //    // Enter/Exit maintenance mode
                //    bool maintenancemode = MaintenanceMode;
                //    if (maintenancemode_ != maintenancemode)
                //    {
                //        maintenancemode_ = maintenancemode;
                //        if (maintenancemode)
                //        {
                //            VirtualEGM0A(0x01, new EventArgs());
                //        }
                //        else if (!maintenancemode)
                //        {
                //            VirtualEGM0B(0x01, new EventArgs());
                //        }
                //    }
                //}
                //catch { }





                try
                {
                    // Get Host Config Bill denomination
                    uint dwDenomCfg = 0;
                    byte bActionFlag = 0;
                    dwStatus = GXGSASAPIBridge.GXG_SAS_GetHostCfgBillDenom(ref dwDenomCfg, ref bActionFlag);
                    if (dwStatus == ERROR_CODE.ERR_NONE.GetHashCode())
                    {
                        if (dwDenomCfg_ != dwDenomCfg || bActionFlag_ != bActionFlag)
                        {
                            dwDenomCfg_ = dwDenomCfg;
                            bActionFlag_ = bActionFlag;
                            VirtualEGM08(0x01, BitConverter.GetBytes(dwDenomCfg), bActionFlag, new EventArgs());
                        }
                    }
                }
                catch { }



                try
                {
                    // Enable/Disable Game n
                    byte total_game_num = 0;
                    UInt32 enabled_game_mask = 0;
                    dwStatus = GXGSASAPIBridge.GXG_SAS_GetGameInfo(ref total_game_num, ref enabled_game_mask);
                    if (dwStatus == ERROR_CODE.ERR_NONE.GetHashCode())
                    {
                        if (total_game_num_ != total_game_num || enabled_game_mask_ != enabled_game_mask)
                        {
                            total_game_num_ = total_game_num;
                            for (int i = 0; i < 32; i++)
                            {
                                if ((enabled_game_mask_ & (2 ^ i)) != (enabled_game_mask & (2 ^ i)))
                                {
                                    byte enable = ((enabled_game_mask & (2 ^ i)) == 1) ? (byte)0x01 : (byte)0x00;
                                    VirtualEGM09(0x01, new byte[] { 0x00, (byte)i }, enable, new EventArgs());
                                }
                            }
                            enabled_game_mask_ = enabled_game_mask;
                        }
                    }
                }
                catch { }

                try
                {

                    // Verify all GXGSAS NVRAM
                    dwStatus = GXGSASAPIBridge.GXG_SAS_VerifyNVRAM(0, 768);
                    if (dwStatus == ERROR_CODE.ERR_SAS_NVRAM_DATA_CORRUPT.GetHashCode())
                    {
                        ClientCriticalError("NVRAM DATA CORRUPT", new EventArgs());
                    }
                    else if (dwStatus == 0)
                    {
                        ClientNoError("NVRAM DATA CORRUPT", new EventArgs()); ;
                    }
                }
                catch
                {

                }
            }

        }

        public void CheckAndHandleAFTLocks()
        {

            SAS_DATE_TIME dwDateTime = new SAS_DATE_TIME();
            // NOTE: Check if there is any pending AFT process for previous power cycle
            byte g_bCurrAvailTransStatus = 0;


            GXGSASAPIBridge.GXG_SAS_GetAFTEGMStatus((byte)(SAS_AFT_EGM_STATUS.SAS_AFT_EGM_AVAIL_TRANS_STATUS.GetHashCode()), ref g_bCurrAvailTransStatus);
            GXGSASAPIBridge.GXG_SAS_SetAFTLockStatus((byte)SAS_AFT_LOCK_ACTION.SAS_AFT_LOCK_ACTION_LOCKED, g_bCurrAvailTransStatus);
            var result = GXGSASAPIBridge.GXG_SAS_SetAFTLockStatus((byte)SAS_AFT_LOCK_ACTION.SAS_AFT_LOCK_ACTION_LOCKED, g_bCurrAvailTransStatus);
           if (result == 0)
            {
                Logger.Log("AFT LOCKED");
            }
            


        }

        public void CancelAFTLock()
        {
            byte g_bCurrAvailTransStatus = 0;
            // We also provide the status when unlocking.
            GXGSASAPIBridge.GXG_SAS_GetAFTEGMStatus((byte)(SAS_AFT_EGM_STATUS.SAS_AFT_EGM_AVAIL_TRANS_STATUS.GetHashCode()), ref g_bCurrAvailTransStatus);
            GXGSASAPIBridge.GXG_SAS_SetAFTLockStatus((byte)SAS_AFT_LOCK_ACTION.SAS_AFT_LOCK_ACTION_UNLOCKED, g_bCurrAvailTransStatus);
            Logger.Log("AFT UN-LOCKED");
        }

        public void CheckAndHandleAFTTrans()
        {

            SAS_DATE_TIME dwDateTime = new SAS_DATE_TIME();
            // NOTE: Check if there is any pending AFT process for previous power cycle
            var dwStatus = GXGSASAPIBridge.CheckSASAFTTransState();
            if ((SAS_AFT_TRANS_PROC_STATUS)dwStatus == SAS_AFT_TRANS_PROC_STATUS.SAS_AFT_TRANS_PROC_STATUS_PENDING)
            {
                // Fetch AFT transfer information
                var sAFTTransData = new SAS_AFT_TRANS_DATA();
                GXGSASAPIBridge.GXG_SAS_GetAFTTransData(ref sAFTTransData);

                VirtualEGMLP72(0x01, sAFTTransData.IsPartial == 1 ? (byte)0x01 : (byte)0x00, 0x00, sAFTTransData.TransType, sAFTTransData.CashAmount, sAFTTransData.ResAmount, sAFTTransData.NonresAmount, sAFTTransData.TransFlag, new byte[] { }, new byte[] { }, sAFTTransData.TransactionID, sAFTTransData.Expir, sAFTTransData.PoolID, new byte[] { }, new byte[] { }, new EventArgs());

                byte g_bCurrAvailTransStatus = 0;
                GXGSASAPIBridge.GXG_SAS_GetAFTEGMStatus((byte)(SAS_AFT_EGM_STATUS.SAS_AFT_EGM_AVAIL_TRANS_STATUS.GetHashCode()), ref g_bCurrAvailTransStatus);
                GXGSASAPIBridge.GXG_SAS_SetAFTLockStatus((byte)SAS_AFT_LOCK_ACTION.SAS_AFT_LOCK_ACTION_UNLOCKED, g_bCurrAvailTransStatus);

            }


            //if (IsGameRunning() == true)
            //{
            //    Console.WriteLine("Game is running now. We could not receive it.");

            //    GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //    GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans((byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_UNABLE_DO_TRANS_THIS_TIME,
            //                        0,
            //                        0,
            //                        0,
            //                        ref dwDateTime);

            //    Console.WriteLine("Complete update AFT transfer status.");

            //    out_aft_trans(); return;
            //}

            // NOTE: If host request print receipt, we have to check wthether printer is still work
            //if ((sAFTTransData.TransFlag & BIT_N(SAS_AFT_Transfer_Flag.BITMASK_SAS_AFT_TRANS_FLAG_REQ_RECEIPT.GetHashCode())) == 1)
            //{
            //    if (g_bPTErrBitMask > 0)
            //    {
            //        Console.WriteLine("Printer is not work properly. We could not receive it.");

            //        GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //        GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans((byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_UNABLE_PRINT_RECEIPT,
            //                            0,
            //                            0,
            //                            0,
            //                            ref dwDateTime);

            //        Console.WriteLine("Complete update AFT transfer status.");

            //    }
            //}




            //if (qwCompCashAmount > 0 || qwCompResAmount > 0 || qwCompNonresAmount > 0)
            //{
            //    // NOTE: Check if host request to print transfer receipt
            //    // NOTE: MUST NOT print receipt for zero amount transfer
            //    if ((sAFTTransData.TransFlag & BIT_N(SAS_AFT_Transfer_Flag.BITMASK_SAS_AFT_TRANS_FLAG_REQ_RECEIPT.GetHashCode())) == 1)
            //    {
            //        Console.WriteLine("Host request print AFT transfer receipt. Prepare print receipt now.");

            //        PrintAFTTransReceipt(sAFTTransData.TransType,
            //                                   sAFTTransData.TransactionID,
            //                                   qwCompCashAmount + qwCompNonresAmount,
            //                                   qwCompResAmount);

            //        // Complete AFT transfer receipt
            //        GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //        GXGSASAPIBridge.GXG_SAS_CompleteAFTReceipt(ref dwDateTime);

            //        Console.WriteLine("Complete update AFT transfer receipt status");


            //    }
            //    else
            //    {

            //    }
            //}


            // (There is maximum one AFT transfer request at a time)
            #region ToComent
            //if (g_atIsAFTTransReqCnt > 0)
            //{
            //    Console.WriteLine("Handle AFT transfer pending event");

            //    // NOTE: Remember to set AFT lock state to unlocked
            //    if (g_atIsAFTLocked == true)
            //    {
            //        g_atIsAFTLocked = false;

            //        m_pSASAFTLockTimer.Stop();

            //        /*
            //         * NOTE: Do not need call GXG_SAS_SetAFTLockStatus to set unlocked state here. The GXGSAS will enter
            //         * the unlocked state automatically in this case.
            //         * We could still call this function, but we have to ignore the return error code.
            //         */
            //        // ShowGXGFuncErr(GXG_SAS_SetAFTLockStatus(SAS_AFT_LOCK_ACTION_UNLOCKED, g_bCurrAvailTransStatus));

            //        UnlockEGM((int)EGM_LOCK_TYPE.EGM_LOCK_AFT_LOCK);
            //    }

            //    // Fetch AFT transfer information
            //    sAFTTransData = new SAS_AFT_TRANS_DATA();
            //    GXGSASAPIBridge.GXG_SAS_GetAFTTransData(ref sAFTTransData);

            //    qwTotalTransAmount = sAFTTransData.CashAmount +
            //            sAFTTransData.ResAmount +
            //            sAFTTransData.NonresAmount;

            //    if (g_atIsAFTTransReqCancel == true)
            //    {
            //        /*
            //         * NOTE: Host has ability to request cancel current AFT transfer request.
            //         * If we get this request, we could choose to cancel this by complete with status
            //         * SAS_AFT_COMP_TRANS_STATUS_CANCEL_BY_HOST or ignore the cancel request.
            //         */

            //        Console.WriteLine("Host request to cancel current AFT transfer. Cancel it,");

            //        // Complete AFT transfer
            //        GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //        GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans((byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_CANCEL_BY_HOST,
            //                            0,
            //                            0,
            //                            0,
            //                           ref dwDateTime);

            //        Console.WriteLine("Complete update AFT transfer status.");
            //    }

            //    /*
            //     * -------------------------------------------------
            //     *         Handle for Transfer Funds to EGM
            //     * -------------------------------------------------
            //     */
            //    if (sAFTTransData.TransType == (byte)SAS_AFT_TRANS_TYPE.SAS_AFT_TRANS_TYPE_INHOUSE_TO_EGM ||
            //        sAFTTransData.TransType == (byte)SAS_AFT_TRANS_TYPE.SAS_AFT_TRANS_TYPE_DEBIT_TO_EGM)
            //    {
            //        Console.WriteLine("Transfer Type = Transfer funds from host to EGM");

            //        // NOTE: Check if there is any tilts on the EGM
            //        if (g_atEGMLockCnt > 0)
            //        {
            //            Console.WriteLine("There are some tilts on the EGM. We could not receive it.");

            //            // Complete AFT transfer
            //            GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //            GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans((byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_UNABLE_DO_TRANS_THIS_TIME,
            //                                0,
            //                                0,
            //                                0,
            //                                ref dwDateTime);

            //            Console.WriteLine("Complete update AFT transfer status.");

            //            out_aft_trans(); return;
            //        }

            //        if (IsGameRunning() == true)
            //        {
            //            Console.WriteLine("Game is running now. We could not receive it.");

            //            GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //            GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans((byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_UNABLE_DO_TRANS_THIS_TIME,
            //                                0,
            //                                0,
            //                                0,
            //                                ref dwDateTime);

            //            Console.WriteLine("Complete update AFT transfer status.");

            //            out_aft_trans(); return;
            //        }

            //        // NOTE: If host request print receipt, we have to check wthether printer is still work
            //        if ((sAFTTransData.TransFlag & BIT_N(SAS_AFT_Transfer_Flag.BITMASK_SAS_AFT_TRANS_FLAG_REQ_RECEIPT.GetHashCode())) == 1)
            //        {
            //            if (g_bPTErrBitMask > 0)
            //            {
            //                Console.WriteLine("Printer is not work properly. We could not receive it.");

            //                GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //                GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans((byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_UNABLE_PRINT_RECEIPT,
            //                                    0,
            //                                    0,
            //                                    0,
            //                                    ref dwDateTime);

            //                Console.WriteLine("Complete update AFT transfer status.");

            //                goto out_aft_trans;
            //            }
            //        }

            //        Console.WriteLine("There is no any tilts on the EGM. We could receive it.");

            //        if (qwTotalTransAmount > 0)
            //        {
            //            /*
            //             * NOTE: We have to check the total amount for partial transfer to EGM.
            //             * If total amount larger than transfer limit or credit limit, the EGM must transfer
            //             * the restricted amount first, and then the nonrestricted amount, and finally the cashout
            //             * amount, until the limit is reached.
            //             */
            //            GLU64 qwRemainCurrCreditRoom = g_qwCreditLimit - g_qwCurrTotalCredit;
            //            GLU64 qwCurrTransLimit = (g_qwAFTTransLimit > qwRemainCurrCreditRoom) ? qwRemainCurrCreditRoom : g_qwAFTTransLimit;
            //            if (sAFTTransData.IsPartial == 1)
            //            {
            //                if (qwTotalTransAmount > qwCurrTransLimit)
            //                {
            //                    qwRemainAmount = qwCurrTransLimit;

            //                    if (qwRemainAmount > sAFTTransData.ResAmount)
            //                        qwCompResAmount = sAFTTransData.ResAmount;
            //                    else
            //                        qwCompResAmount = qwRemainAmount;
            //                    qwRemainAmount -= qwCompResAmount;

            //                    if (qwRemainAmount > sAFTTransData.NonresAmount)
            //                        qwCompNonresAmount = sAFTTransData.NonresAmount;
            //                    else
            //                        qwCompNonresAmount = qwRemainAmount;
            //                    qwRemainAmount -= qwCompNonresAmount;

            //                    qwCompCashAmount = qwRemainAmount;
            //                }
            //                else
            //                {
            //                    qwCompCashAmount = sAFTTransData.CashAmount;
            //                    qwCompResAmount = sAFTTransData.ResAmount;
            //                    qwCompNonresAmount = sAFTTransData.NonresAmount;
            //                }
            //            }
            //            else
            //            {
            //                /*
            //                 * NOTE: For full amount transfer, the GXGSAS was already checked the transfer limit and credit limit.
            //                 * (We still check in this example, but it could be ignore)
            //                 */
            //                if (qwTotalTransAmount > qwRemainCurrCreditRoom)
            //                {
            //                    Console.WriteLine("This transfer will cause the current credit exceed the credit limit. We could not receive it.");

            //                    // Complete AFT transfer
            //                    GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //                    GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans((byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_UNABLE_DO_TRANS_THIS_TIME,
            //                                        0,
            //                                        0,
            //                                        0,
            //                                        ref dwDateTime);

            //                    Console.WriteLine("Complete update AFT transfer status.");

            //                    goto out_aft_trans;
            //                }
            //                else
            //                {
            //                    qwCompCashAmount = sAFTTransData.CashAmount;
            //                    qwCompResAmount = sAFTTransData.ResAmount;
            //                    qwCompNonresAmount = sAFTTransData.NonresAmount;
            //                }
            //            }

            //            Console.WriteLine("Update current credits.");
            //            // The transfer amount is to EGM, so we have to update the current credit.
            //            if (qwCompCashAmount > 0)
            //            {
            //                g_qwCurrCashableCredit += qwCompCashAmount;
            //                GXGSASAPIBridge.GXG_SAS_SetCurrentCedit((byte)SAS_CREDIT_TYPE.SAS_CREDIT_CASHABLE, g_qwCurrCashableCredit);
            //            }
            //            if (qwCompResAmount > 0)
            //            {
            //                g_qwCurrResCredit += qwCompResAmount;
            //                GXGSASAPIBridge.GXG_SAS_SetCurrentCedit((byte)SAS_CREDIT_TYPE.SAS_CREDIT_RESTRICTED, g_qwCurrResCredit);

            //                /*
            //                 * NOTE: Mark current restricted credit is from foreign source.
            //                 * We will check the extended validatoin status BITMASK_SAS_EXT_VALID_STATUS_ALLOW_PRINT_FOREIGN_RES_TICKET
            //                 * while EGM cashout this restricted amount by printer.
            //                 */
            //                g_bIsResCreditForeign = new GLU8_BUF();
            //                GXGGFMAPIBridge.GXG_SRAM_WriteBlock8(g_bIsResCreditForeign, 0x0030, 1);
            //            }
            //            if (qwCompNonresAmount > 0)
            //            {
            //                g_qwCurrNonresCredit += qwCompNonresAmount;
            //                GXGSASAPIBridge.GXG_SAS_SetCurrentCedit((byte)SAS_CREDIT_TYPE.SAS_CREDIT_NONRESTRICTED, g_qwCurrNonresCredit);
            //            }
            //            g_qwCurrTotalCredit = g_qwCurrCashableCredit + g_qwCurrResCredit + g_qwCurrNonresCredit;
            //        }
            //        else
            //        {
            //            qwCompCashAmount = 0;
            //            qwCompResAmount = 0;
            //            qwCompNonresAmount = 0;
            //        }

            //        // Complete AFT transfer
            //        GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //        GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans(sAFTTransData.IsPartial,
            //                            qwCompCashAmount,
            //                            qwCompResAmount,
            //                            qwCompNonresAmount,
            //                            ref dwDateTime);

            //        Console.WriteLine("Complete update AFT transfer status.");

            //        if (qwCompCashAmount > 0 || qwCompResAmount > 0 || qwCompNonresAmount > 0)
            //        {
            //            // NOTE: Check if host request to print transfer receipt
            //            // NOTE: MUST NOT print receipt for zero amount transfer
            //            if ((sAFTTransData.TransFlag & BIT_N(SAS_AFT_Transfer_Flag.BITMASK_SAS_AFT_TRANS_FLAG_REQ_RECEIPT.GetHashCode())) == 1)
            //            {
            //                Console.WriteLine("Host request print AFT transfer receipt. Prepare print receipt now.");

            //                PrintAFTTransReceipt(sAFTTransData.TransType,
            //                                           sAFTTransData.TransactionID,
            //                                           qwCompCashAmount + qwCompNonresAmount,
            //                                           qwCompResAmount);

            //                // Complete AFT transfer receipt
            //                GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //                GXGSASAPIBridge.GXG_SAS_CompleteAFTReceipt(ref dwDateTime);

            //                Console.WriteLine("Complete update AFT transfer receipt status");


            //            }
            //            else
            //            {

            //            }
            //        }
            //    }
            //    /*
            //     * -------------------------------------------------
            //     *         Handle for Transfer Funds to Ticket
            //     * -------------------------------------------------
            //     */
            //    else if (sAFTTransData.TransType == SAS_AFT_TRANS_TYPE.SAS_AFT_TRANS_TYPE_INHOUSE_TO_TICKET.GetHashCode() ||
            //        sAFTTransData.TransType == SAS_AFT_TRANS_TYPE.SAS_AFT_TRANS_TYPE_DEBIT_TO_TICKET.GetHashCode())
            //    {
            //        Console.WriteLine("Transfer Type = Transfer funds from host to ticket");

            //        GLU8 bTicketType;
            //        GLU64 qwTicketAmount;

            //        // NOTE: Check if there is any tilts on the EGM
            //        if (g_atEGMLockCnt > 0)
            //        {
            //            Console.WriteLine("There are some tilts on the EGM. We could not receive it.");

            //            // Complete AFT transfer
            //            GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //            GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans((byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_UNABLE_DO_TRANS_THIS_TIME,
            //                                0,
            //                                0,
            //                                0,
            //                                ref dwDateTime);

            //            Console.WriteLine("Complete update AFT transfer status.");

            //            goto out_aft_trans;
            //        }

            //        if (IsGameRunning() == true)
            //        {
            //            Console.WriteLine("Game is running now. We could not receive it.");

            //            GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //            GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans((byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_UNABLE_DO_TRANS_THIS_TIME,
            //                                0,
            //                                0,
            //                                0,
            //                                ref dwDateTime);

            //            Console.WriteLine("Complete update AFT transfer status.");

            //            goto out_aft_trans;
            //        }

            //        // NOTE: Check if printer is work
            //        if (g_bPTErrBitMask > 0)
            //        {
            //            Console.WriteLine("Printer is not work properly. We could not receive it.");

            //            GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //            GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans((byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_TRANS_TO_TICKET_DEVICE_NOT_AVAIL,
            //                                0,
            //                                0,
            //                                0,
            //                                ref dwDateTime);

            //            Console.WriteLine("Complete update AFT transfer status.");

            //            goto out_aft_trans;
            //        }

            //        // NOTE: Check extended validation status to make sure host allow EGM print cashout ticket
            //        if ((g_bSASCombExtValidStatus & BIT_N(SAS_Extended_Validation_Status.BITMASK_SAS_EXT_VALID_STATUS_USE_PRINTER_FOR_CASHOUT.GetHashCode())) == 0)
            //        {
            //            Console.WriteLine("Printer could not be the cashout device now. We could not receive it.");


            //            GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //            GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans((byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_TRANS_TO_TICKET_DEVICE_NOT_AVAIL,
            //                                0,
            //                                0,
            //                                0,
            //                                ref dwDateTime);

            //            Console.WriteLine("Complete update AFT transfer status.");

            //            goto out_aft_trans;
            //        }

            //        // NOTE: Check extended validation status to make sure host allow EGM print restricted ticket
            //        if (qwCompResAmount > 0 &&
            //                (g_bSASCombExtValidStatus & BIT_N(SAS_Extended_Validation_Status.BITMASK_SAS_EXT_VALID_STATUS_ALLOW_PRINT_RES_TICKET.GetHashCode())) == 0)
            //        {
            //            Console.WriteLine("Do not allow print restricted ticket. We could not receive it.");

            //            GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //            GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans((byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_TRANS_TO_TICKET_DEVICE_NOT_AVAIL,
            //                                0,
            //                                0,
            //                                0,
            //                                ref dwDateTime);

            //            Console.WriteLine("Complete update AFT transfer status.");

            //            goto out_aft_trans;
            //        }

            //        // NOTE: Check if expiration YYYYMMDD format is valid or not
            //        if (sAFTTransData.Expir > 9999)
            //        {
            //            GLU32 dwTempCurrDate = 0;
            //            GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //            dwTempCurrDate = dwDateTime.DateTime[0] * 10000 + dwDateTime.DateTime[1] * 100 + dwDateTime.DateTime[2];

            //            // Check if the exipration is less than current date time
            //            if (sAFTTransData.Expir < dwTempCurrDate)
            //            {
            //                Console.WriteLine("Expiration is invalid. We could not receive it.");

            //                GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //                GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans((byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_EXPIR_NOT_VALID_FOR_TICKET,
            //                                    0,
            //                                    0,
            //                                    0,
            //                                    ref dwDateTime);

            //                Console.WriteLine("Complete update AFT transfer status.");

            //                goto out_aft_trans;
            //            }
            //        }

            //        /*
            //         * NOTE: Transfet to ticket will only contain one type amount at a time and there is no
            //         * nonrestricted amount.
            //         */
            //        qwCompCashAmount = sAFTTransData.CashAmount;
            //        qwCompResAmount = sAFTTransData.ResAmount;

            //        if (sAFTTransData.TransType == SAS_AFT_TRANS_TYPE.SAS_AFT_TRANS_TYPE_INHOUSE_TO_TICKET.GetHashCode())
            //        {
            //            if (qwCompCashAmount > 0)
            //            {
            //                Console.WriteLine("Ticket Type = In-house AFT cashable ticket");
            //                bTicketType = (byte)SAS_TICKET_TYPE.SAS_TICKET_TYPE_INHOUSE_CASHABLE_AFT.GetHashCode();
            //                qwTicketAmount = qwCompCashAmount;
            //            }
            //            else if (qwCompResAmount > 0)
            //            {
            //                Console.WriteLine("Ticket Type = In-house AFT restricted ticket");
            //                bTicketType = (byte)SAS_TICKET_TYPE.SAS_TICKET_TYPE_INHOUSE_RESTRICTED_AFT;
            //                qwTicketAmount = qwCompResAmount;
            //            }
            //            else // Zero amount
            //            {
            //                // NOTE: There is no standard rule to handle the zero amount ticket
            //                Console.WriteLine("Ticket Type = In-house AFT cashable ticket");
            //                bTicketType = (byte)SAS_TICKET_TYPE.SAS_TICKET_TYPE_INHOUSE_CASHABLE_AFT;
            //                qwTicketAmount = 0;
            //            }
            //        }
            //        else if (sAFTTransData.TransType == (byte)SAS_AFT_TRANS_TYPE.SAS_AFT_TRANS_TYPE_DEBIT_TO_TICKET)
            //        {
            //            Console.WriteLine("Ticket Type = Debit AFT cashable ticket");
            //            bTicketType = (byte)SAS_TICKET_TYPE.SAS_TICKET_TYPE_DEBIT_AFT;
            //            if (qwTotalTransAmount > 0)
            //            {
            //                qwTicketAmount = qwCompCashAmount;
            //            }
            //            else // Zero amount
            //            {
            //                // NOTE: There is no standard rule to handle the zero amount ticket
            //                qwTicketAmount = 0;
            //            }
            //        }

            //        g_dwAFTTransToTicketExpir = sAFTTransData.Expir;

            //        Console.WriteLine("Prepare to print AFT ticket");

            //        if ((g_dwSASFeatureMask[g_bCurrSelectGameNum] & SAS_Feature.BITMASK_SAS_FEATURE_VALID_MODE_SECURE.GetHashCode()) == 1)
            //        {
            //            LockEGM(EGM_LOCK_TYPE.EGM_LOCK_TO.GetHashCode(), "Process Printing AFT Ticket");
            //           // dwStatus = StartPrintTicket(SAS_VALID_MODE_SECURE, bTicketType, qwTicketAmount, sAFTTransData.PoolID);
            //        }
            //        else if ((g_dwSASFeatureMask[g_bCurrSelectGameNum] & SAS_Feature.BITMASK_SAS_FEATURE_VALID_MODE_SYSTEM.GetHashCode()) == 1)
            //        {
            //            LockEGM(EGM_LOCK_TYPE.EGM_LOCK_TO.GetHashCode(), "Process Printing AFT Ticket");
            //            //dwStatus = this->StartPrintTicket(SAS_VALID_MODE_SYSTEM, bTicketType, qwTicketAmount, sAFTTransData.PoolID);
            //        }
            //        UnlockEGM(EGM_LOCK_TYPE.EGM_LOCK_TO.GetHashCode());
            //        if (dwStatus != 0)
            //        {
            //            Console.WriteLine("Fail to print AFT ticket");

            //            GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //            GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans((byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_TRANS_TO_TICKET_DEVICE_NOT_AVAIL,
            //                                0,
            //                                0,
            //                                0,
            //                                ref dwDateTime);

            //            Console.WriteLine("Complete update AFT transfer status.");
            //        }
            //        else
            //        {
            //            GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //            GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans(sAFTTransData.IsPartial,
            //                                qwCompCashAmount,
            //                                qwCompResAmount,
            //                                qwCompNonresAmount,
            //                                ref dwDateTime);

            //            Console.WriteLine("Complete update AFT transfer status.");

            //            if (qwCompCashAmount > 0 || qwCompResAmount > 0 || qwCompNonresAmount > 0)
            //            {
            //                // NOTE: Check if host request to print transfer receipt
            //                // NOTE: MUST NOT print receipt for zero transfer amount
            //                if ((sAFTTransData.TransFlag & SAS_AFT_Transfer_Flag.BITMASK_SAS_AFT_TRANS_FLAG_REQ_RECEIPT.GetHashCode()) == 1)
            //                {
            //                    Console.WriteLine("Host request print AFT transfer receipt. Prepare print receipt now.");

            //                    PrintAFTTransReceipt(sAFTTransData.TransType,
            //                                               sAFTTransData.TransactionID,
            //                                               qwCompCashAmount + qwCompNonresAmount,
            //                                               qwCompResAmount);

            //                    // Complete AFT transfer receipt
            //                    GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //                    GXGSASAPIBridge.GXG_SAS_CompleteAFTReceipt(ref dwDateTime);

            //                    Console.WriteLine("Complete update AFT transfer receipt status");

            //                    //this->ShowBlockMsg(QString("AFT Transfer %1 Cents To Ticket. (With Receipt)")
            //                    //                   .arg(qwCompCashAmount + qwCompResAmount + qwCompNonresAmount),
            //                    //                   2000);
            //                }
            //                else
            //                {
            //                    //this->ShowBlockMsg(QString("AFT Transfer %1 Cents To Ticket. (No Receipt)")
            //                    //                   .arg(qwCompCashAmount + qwCompResAmount + qwCompNonresAmount),
            //                    //                   2000);
            //                }
            //            }
            //        }
            //    }
            //    /*
            //     * -------------------------------------------------
            //     *         Handle for Transfer Funds to Host
            //     * -------------------------------------------------
            //     */
            //    else if (sAFTTransData.TransType == SAS_AFT_TRANS_TYPE.SAS_AFT_TRANS_TYPE_INHOUSE_TO_HOST.GetHashCode())
            //    {
            //        /*
            //         * In real world, we will get these AFT transfer types in the following cases:
            //         * 1. While player remove the membership card/debit card
            //         * 2. Player push the cashout button and the AFT host cashout is enabled.
            //         * 3. Player win a game and the win amount will cause the credit limit to be exceeded, and the AFT host cashout is enabled.
            //         */
            //        Console.WriteLine("Transfer Type = Transfer funds from EGM to host");

            //        GLU8 bIsAmountValid = 1;

            //        // NOTE: Check if there is any tilts on the EGM
            //        // NOTE: Contact with your manuafacturer to check if allow transfer to host while EGM has some tilts.
            //        if (g_atEGMLockCnt > 0)
            //        {
            //            Console.WriteLine("There are some tilts on the EGM. We could not receive it.");

            //            GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //            GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans((byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_UNABLE_DO_TRANS_THIS_TIME,
            //                                0,
            //                                0,
            //                                0,
            //                                ref dwDateTime);

            //            Console.WriteLine("Complete update AFT transfer status.");

            //            goto out_aft_trans;
            //        }

            //        if (IsGameRunning() == true)
            //        {
            //            Console.WriteLine("Game is running now. We could not receive it.");

            //            GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //            GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans((byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_UNABLE_DO_TRANS_THIS_TIME,
            //                                0,
            //                                0,
            //                                0,
            //                                ref dwDateTime);

            //            Console.WriteLine("Complete update AFT transfer status.");

            //            goto out_aft_trans;
            //        }

            //        // GLI Test 2017/11/31
            //        // NOTE: We could not receive transfer to host command while EGM is in the cashout process
            //        if (g_atIsProcNonAFTCashout == true)
            //        {
            //            Console.WriteLine("Cashout is processing now. We could not receive it.");

            //            GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //            GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans((byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_UNABLE_DO_TRANS_THIS_TIME,
            //                                0,
            //                                0,
            //                                0,
            //                                ref dwDateTime);

            //            Console.WriteLine("Complete update AFT transfer status.");

            //            goto out_aft_trans;
            //        }

            //        // NOTE: If host request print receipt, we have to check wthether printer is still work
            //        if ((sAFTTransData.TransFlag & SAS_AFT_Transfer_Flag.BITMASK_SAS_AFT_TRANS_FLAG_REQ_RECEIPT.GetHashCode()) == 1)
            //        {
            //            if (g_bPTErrBitMask > 0)
            //            {
            //                Console.WriteLine("Printer is not work properly. We could not receive it.");

            //                GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //                GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans((byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_UNABLE_PRINT_RECEIPT,
            //                                    0,
            //                                    0,
            //                                    0,
            //                                    ref dwDateTime);

            //                Console.WriteLine("Complete update AFT transfer status.");

            //                goto out_aft_trans;
            //            }
            //        }

            //        if (qwTotalTransAmount > 0)
            //        {
            //            // NOTE: We only allow all zero amount or all 9999999999 with partial transfer
            //            // NOTE: Already check in the GXGSAS core
            //            // GLI Test 2017/11/30

            //            // NOTE: Check BITMASK_SAS_EGM_AFT_STATUS_TO_HOST_PARTIAL_AMOUNT_AVAIL for full transfer request
            //            if ((g_bCurrAFTStatus & SAS_AFT_Status.BITMASK_SAS_EGM_AFT_STATUS_TO_HOST_PARTIAL_AMOUNT_AVAIL.GetHashCode()) == 0)
            //            {
            //                /*
            //                 * If transfer to host of less than full available amount is NOT allowed, we
            //                 * have to check the request transfer amount and current credit.
            //                 */

            //                if (g_qwCurrCashableCredit > sAFTTransData.CashAmount)
            //                    bIsAmountValid = 0;
            //                if (g_qwCurrResCredit > sAFTTransData.ResAmount)
            //                    bIsAmountValid = 0;
            //                if (g_qwCurrNonresCredit > sAFTTransData.NonresAmount)
            //                    bIsAmountValid = 0;
            //            }
            //            else if (sAFTTransData.IsPartial == 0)
            //            {
            //                /*
            //                 * Host request full transfer but current credit is less than request transfer amount
            //                 */
            //                if (g_qwCurrCashableCredit < sAFTTransData.CashAmount)
            //                    bIsAmountValid = 0;
            //                if (g_qwCurrResCredit < sAFTTransData.ResAmount)
            //                    bIsAmountValid = 0;
            //                if (g_qwCurrNonresCredit < sAFTTransData.NonresAmount)
            //                    bIsAmountValid = 0;
            //            }

            //            if (bIsAmountValid == 0)
            //            {
            //                Console.WriteLine("Could not satisfy the full transfer amount. Reject it.");


            //                GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //                GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans((byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_NOT_VALID_TRANS_FUNC,
            //                    0,
            //                    0,
            //                    0,
            //                   ref dwDateTime);

            //                Console.WriteLine("Complete update AFT transfer status.");

            //                goto out_aft_trans;
            //            }
            //            else
            //            {
            //                /*
            //                 * NOTE: We have to check the total amount for partial transfer to host.
            //                 * If total amount larger than transfer limit, the EGM must transfer the restricted
            //                 * amount first, then the nonrestricted amount, then the cashout amount, until the
            //                 * limit is reached.
            //                 */
            //                if (sAFTTransData.IsPartial == 0)
            //                {
            //                    // Check the available amount for transfer
            //                    qwCompCashAmount = (g_qwCurrCashableCredit > sAFTTransData.CashAmount ? sAFTTransData.CashAmount : g_qwCurrCashableCredit);
            //                    qwCompResAmount = (g_qwCurrResCredit > sAFTTransData.ResAmount ? sAFTTransData.ResAmount : g_qwCurrResCredit);
            //                    qwCompNonresAmount = (g_qwCurrNonresCredit > sAFTTransData.NonresAmount ? sAFTTransData.NonresAmount : g_qwCurrNonresCredit);

            //                    qwTotalTransAmount = qwCompCashAmount + qwCompResAmount + qwCompNonresAmount;
            //                    if (qwTotalTransAmount > g_qwAFTTransLimit)
            //                    {
            //                        qwRemainAmount = g_qwAFTTransLimit;

            //                        if (qwRemainAmount > qwCompResAmount)
            //                            qwCompResAmount = qwCompResAmount;
            //                        else
            //                            qwCompResAmount = qwRemainAmount;
            //                        qwRemainAmount -= qwCompResAmount;

            //                        if (qwRemainAmount > qwCompNonresAmount)
            //                            qwCompNonresAmount = qwCompNonresAmount;
            //                        else
            //                            qwCompNonresAmount = qwRemainAmount;
            //                        qwRemainAmount -= qwCompNonresAmount;

            //                        qwCompCashAmount = qwRemainAmount;
            //                    }
            //                }
            //                else
            //                {
            //                    qwCompCashAmount = sAFTTransData.CashAmount;
            //                    qwCompResAmount = sAFTTransData.ResAmount;
            //                    qwCompNonresAmount = sAFTTransData.NonresAmount;
            //                }

            //                // Update current credit
            //                Console.WriteLine("Update current credit.");
            //                if (qwCompCashAmount > 0)
            //                {
            //                    g_qwCurrCashableCredit -= qwCompCashAmount;
            //                    GXGSASAPIBridge.GXG_SAS_SetCurrentCedit((byte)SAS_CREDIT_TYPE.SAS_CREDIT_CASHABLE.GetHashCode(), g_qwCurrCashableCredit);
            //                }
            //                if (qwCompResAmount > 0)
            //                {
            //                    g_qwCurrResCredit -= qwCompResAmount;
            //                    GXGSASAPIBridge.GXG_SAS_SetCurrentCedit((byte)SAS_CREDIT_TYPE.SAS_CREDIT_RESTRICTED.GetHashCode(), g_qwCurrResCredit);
            //                }
            //                if (qwCompNonresAmount > 0)
            //                {
            //                    g_qwCurrNonresCredit -= qwCompNonresAmount;
            //                    GXGSASAPIBridge.GXG_SAS_SetCurrentCedit((byte)SAS_CREDIT_TYPE.SAS_CREDIT_NONRESTRICTED.GetHashCode(), g_qwCurrNonresCredit);
            //                }
            //                g_qwCurrTotalCredit = g_qwCurrCashableCredit + g_qwCurrResCredit + g_qwCurrNonresCredit;
            //            }
            //        }
            //        else
            //        {
            //            qwCompCashAmount = 0;
            //            qwCompResAmount = 0;
            //            qwCompNonresAmount = 0;
            //        }

            //        // Complete AFT transfer
            //        GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //        dwStatus = GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans(sAFTTransData.IsPartial,
            //            qwCompCashAmount,
            //            qwCompResAmount,
            //            qwCompNonresAmount,
            //            ref dwDateTime);

            //        Console.WriteLine("Complete update AFT transfer status.");

            //        if (qwCompCashAmount > 0 || qwCompResAmount > 0 || qwCompNonresAmount > 0)
            //        {
            //            // NOTE: Check if host request to print transfer receipt
            //            // NOTE: MUST NOT print receipt for zero transfer amount
            //            if ((sAFTTransData.TransFlag & SAS_AFT_Transfer_Flag.BITMASK_SAS_AFT_TRANS_FLAG_REQ_RECEIPT.GetHashCode()) == 1)
            //            {
            //                Console.WriteLine("Host request print AFT transfer receipt. Prepare print receipt now.");

            //                PrintAFTTransReceipt(sAFTTransData.TransType,
            //                                           sAFTTransData.TransactionID,
            //                                           qwCompCashAmount + qwCompNonresAmount,
            //                                           qwCompResAmount);

            //                // Complete AFT transfer receipt
            //                GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //                GXGSASAPIBridge.GXG_SAS_CompleteAFTReceipt(ref dwDateTime);

            //                Console.WriteLine("Complete update AFT transfer receipt status");


            //            }
            //            else
            //            {

            //            }
            //        }

            //        g_atIsAFTHostCashoutPending = false;
            //        g_atIsAFTHostCashoutSuccess = true;

            //        /*
            //         * NOTE: If host request cashout, we have to cashout the remain credits.
            //         * The behavior is same as that a player push the cashout button.
            //         */
            //        if ((sAFTTransData.TransFlag & SAS_AFT_Transfer_Flag.BITMASK_SAS_AFT_TRANS_FLAG_REQ_CASHOUT.GetHashCode()) == 1)
            //        {
            //            g_atIsAFTReqCashout = true;
            //        }
            //    }
            //    /*
            //     * -------------------------------------------------
            //     *         Handle for Transfer Win Amount to Host
            //     * -------------------------------------------------
            //     */
            //    else if (sAFTTransData.TransType == SAS_AFT_TRANS_TYPE.SAS_AFT_TRANS_TYPE_WIN_AMOUNT_TO_HOST.GetHashCode())
            //    {
            //        GLU64 qwTotalWinAmountCent = 0;
            //        GLU64 qwRemainWinAmountCent = 0;

            //        Console.WriteLine("Transfer Type = Transfer win amount from EGM to host");

            //        // NOTE: We only accept this type while host cashout win is pending
            //        if (g_atIsAFTHostCashoutWinPending == false)
            //        {
            //            Console.WriteLine("These is no any pending win amount.");

            //            // Complete AFT transfer
            //            GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //            GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans((byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_NO_WON_CREDIT_AVAIL_FOR_CASHOUT,
            //                                0,
            //                                0,
            //                                0,
            //                                ref dwDateTime);

            //            Console.WriteLine("Complete update AFT transfer status.");

            //            goto out_aft_trans;
            //        }

            //        qwTotalWinAmountCent = (GLU64)(g_qwLastWinPaytableAmountCent + g_qwLastWinProgAmountCent + g_qwLastWinBonusAmountCent);

            //        if (g_atEGMLockCnt > 0)
            //        {
            //            Console.WriteLine("There are some tilts on the EGM. We could not receive it.");

            //            // Complete AFT transfer
            //            GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //            GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans((byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_UNABLE_DO_TRANS_THIS_TIME,
            //                                0,
            //                                0,
            //                                0,
            //                                ref dwDateTime);

            //            Console.WriteLine("Complete update AFT transfer status.");

            //            goto out_aft_trans;
            //        }

            //        // NOTE: If host request print receipt, we have to check wthether printer is still work
            //        if ((sAFTTransData.TransFlag & SAS_AFT_Transfer_Flag.BITMASK_SAS_AFT_TRANS_FLAG_REQ_RECEIPT.GetHashCode()) == 1)
            //        {
            //            if (g_bPTErrBitMask > 0)
            //            {
            //                Console.WriteLine("Printer is not work properly. We could not receive it.");

            //                GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //                GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans((byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_UNABLE_PRINT_RECEIPT,
            //                                    0,
            //                                    0,
            //                                    0,
            //                                    ref dwDateTime);

            //                Console.WriteLine("Complete update AFT transfer status.");

            //                goto out_aft_trans;
            //            }
            //        }

            //        if (sAFTTransData.IsPartial == 0)   // Full transfer
            //        {
            //            if (sAFTTransData.CashAmount <= qwTotalWinAmountCent)
            //            {
            //                qwRemainWinAmountCent = qwTotalWinAmountCent - sAFTTransData.CashAmount;
            //                qwCompCashAmount = sAFTTransData.CashAmount;
            //            }
            //            else
            //            {
            //                Console.WriteLine("Win amount is not match the full transfer request amount. We could not receive it.");

            //                // Complete AFT transfer
            //                GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //                GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans((byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_NOT_VALID_TRANS_FUNC,
            //                                    0,
            //                                    0,
            //                                    0,
            //                                    ref dwDateTime);

            //                Console.WriteLine("Complete update AFT transfer status.");

            //                goto out_aft_trans;
            //            }
            //        }
            //        else   // Partial transfer
            //        {
            //            if (sAFTTransData.CashAmount <= qwTotalWinAmountCent)
            //            {
            //                qwRemainWinAmountCent = qwTotalWinAmountCent - sAFTTransData.CashAmount;
            //                qwCompCashAmount = sAFTTransData.CashAmount;
            //            }
            //            else
            //            {
            //                qwCompCashAmount = qwTotalWinAmountCent;
            //            }
            //        }

            //        // Complete AFT transfer
            //        GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //        dwStatus = GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans(sAFTTransData.IsPartial,
            //            qwCompCashAmount,
            //            0,
            //            0,
            //            ref dwDateTime);

            //        g_qwLastRemainUnpaidWinAmountCent = qwRemainWinAmountCent;

            //        Console.WriteLine("Complete update AFT transfer status.");

            //        if (qwCompCashAmount > 0 || qwCompResAmount > 0 || qwCompNonresAmount > 0)
            //        {
            //            // NOTE: Check if host request to print transfer receipt
            //            // NOTE: MUST NOT print receipt for zero transfer amount
            //            if ((sAFTTransData.TransFlag & SAS_AFT_Transfer_Flag.BITMASK_SAS_AFT_TRANS_FLAG_REQ_RECEIPT.GetHashCode()) == 1)
            //            {
            //                Console.WriteLine("Host request print AFT transfer receipt. Prepare print receipt now.");

            //                PrintAFTTransReceipt(sAFTTransData.TransType,
            //                                           sAFTTransData.TransactionID,
            //                                           qwCompCashAmount + qwCompNonresAmount,
            //                                           qwCompResAmount);

            //                // Complete AFT transfer receipt
            //                GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //                GXGSASAPIBridge.GXG_SAS_CompleteAFTReceipt(ref dwDateTime);

            //                Console.WriteLine("Complete update AFT transfer receipt status");


            //            }
            //            else
            //            {

            //            }
            //        }

            //        g_atIsAFTHostCashoutWinPending = false;
            //        g_atIsAFTHostCashoutWinSuccess = true;

            //        /*
            //         * NOTE: If host request cashout, we have to cashout the remain credits.
            //         * The behavior is same as that a player push the cashout button.
            //         */
            //        if ((sAFTTransData.TransFlag & SAS_AFT_Transfer_Flag.BITMASK_SAS_AFT_TRANS_FLAG_REQ_CASHOUT.GetHashCode()) == 1)
            //        {
            //            g_atIsAFTReqCashout = true;
            //        }
            //    }
            //    /*
            //     * -------------------------------------------------
            //     *         Handle for Transfer Funds for Bonus
            //     * -------------------------------------------------
            //     */
            //    else
            //    {
            //        if (sAFTTransData.TransType == SAS_AFT_TRANS_TYPE.SAS_AFT_TRANS_TYPE_BONUS_JACKPOT_WIN_TO_EGM.GetHashCode())
            //        {
            //            Console.WriteLine("Transfer Type = Transfer bonus jackpot amount from host to EGM");
            //        }
            //        else
            //        {
            //            Console.WriteLine("Transfer Type = Transfer bonus cash amount from host to EGM");
            //        }

            //        /*
            //         * NOTE:
            //         * We could not accept the AFT bonus in the following cases:
            //         * 1. There is any tilts on the EGM such as door open.
            //         * 2. EGM is in handpay lock.
            //         * 3. EGM is in an operator menu.
            //         * 4. EGM is out of service.
            //         * We should accept the AFT bonus in the following cases:
            //         * 1. While game idle.
            //         * 2. While game is playing.
            //         * 3. While waiting for player input such as waiting to draw cards for card game.
            //         * 4. While in a double-up.
            //         */
            //        if (g_atEGMLockCnt > 0 ||
            //                g_atIsHandpayLock == true ||
            //                g_atIsOpMenuEntered == true)
            //        {
            //            Console.WriteLine("There are some tilts on the EGM. We could not receive it.");

            //            // Complete AFT transfer
            //            GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            //            GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans((byte)SAS_AFT_COMP_TRANS_STATUS.SAS_AFT_COMP_TRANS_STATUS_UNABLE_DO_TRANS_THIS_TIME,
            //                                0,
            //                                0,
            //                                0,
            //                                ref dwDateTime);

            //            Console.WriteLine("Complete update AFT transfer status.");

            //            goto out_aft_trans;
            //        }

            //        // Will handle in the CheckAndHandleBonus function
            //        g_atIsGetAFTBonus = true;
            //        goto out_aft_trans;
            //    }



            #endregion
        }



        /// <summary>
        /// Comienzo de ejecución del client. Abre el puerto en el que se pasa como parámetro y comienza a escuchar
        /// </summary>
        /// <param name="port"></param>
        public void Start(string port)
        {
            #region init

            UInt32 dwStatus;
            String apiVer = "";
            String drvVer = "";
            String fwVer = "";
            dwStatus = GXGGFMAPIBridge.GXG_CORE_Init();
            if (dwStatus != (Int32)ERROR_CODE.ERR_NONE)
            {
                return;
            }

            dwStatus = GXGSASAPIBridge.GXG_SAS_Init();
            if (dwStatus != (UInt32)ERROR_CODE.ERR_NONE)
            {
                GXGGFMAPIBridge.GXG_CORE_Release();
                return;
            }
            GXGSASAPIBridge.GXG_SAS_GetAPIVersion(ref apiVer);
            GXGSASAPIBridge.GXG_SAS_GetDriverVersion(ref drvVer);
            GXGSASAPIBridge.GXG_SAS_GetFirmwareVersion(ref fwVer);
            GXGSASAPIBridge.GXG_SAS_RegisterIntrCallBack(SASHandlerp);

            #endregion

            m_pSASAFTLockTimer.Start();
            m_pSASHandlerTimer.Start();

            gXGSASROMSigCalculator.StartThread();

        }

        /// <summary>
        /// Enable SAS
        /// </summary>
        public void Enable()
        {
            // Enable SAS function for all used ports
            GXGSASAPIBridge.GXG_SAS_SetEnabled(1, GXGSASAPIConst.FALSE);

            // Set all SAS port's address
            for (byte i = 0; i < GXGSASAPIConst.NUM_SUP_SAS_PORT; i++)
            {
                var dwStatus = GXGSASAPIBridge.GXG_SAS_SetEGMAddr(i, sasId_address);
                if (dwStatus != (UInt32)ERROR_CODE.ERR_NONE)
                {
                }
            }


            for (int i = 0; i < 3; i++)
            {
                if (i == g_bSASValidLBPort || i == g_bSASProgPort)
                {
                    GXGSASAPIBridge.GXG_SAS_SetEnabled((byte)i, 1);
                }
            }

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
            GXGGFMAPIBridge.GXG_CORE_Release();
            GXGSASAPIBridge.GXG_SAS_Release();

            m_pSASAFTLockTimer.Stop();
            m_pSASHandlerTimer.Stop();

            gXGSASROMSigCalculator.StopThread();

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
            byte gamen = 0x00;
            if (gameNumber.Length == 2)
            {
                gamen = gameNumber[1];
            }
            else if (gameNumber.Length == 1)
            {
                gamen = gameNumber[0];
            }
            SAS_GAME_DETAIL gamedetail = new SAS_GAME_DETAIL();
            var dwStatus = GXGSASAPIBridge.GXG_SAS_GetGameDetail(gamen, ref gamedetail); // nl
            if (dwStatus == 0)
            {

                gamedetail.CashoutLimit = cashoutLimit;
                GXGSASAPIBridge.GXG_SAS_SetGameDetail(gamen, gamedetail); // nl

            }

        }

        // Set Current Player Denomination
        public void SetCurrentPlayerDenomination(byte denom_code)
        {
            GXGSASAPIBridge.GXG_SAS_SetCurrentGameDenom(denom_code); // nl
        }

        // Enable Game Denomination
        public void EnableDenomination(byte[] gameNumber, byte code, ushort maxbet)
        {

            byte gamen = 0x00;
            if (gameNumber.Length == 2)
            {
                gamen = gameNumber[1];
            }
            else if (gameNumber.Length == 1)
            {
                gamen = gameNumber[0];
            }
            GXGSASAPIBridge.GXG_SAS_AddGameDenom(gamen, code, maxbet); // nl
        }

        // Delete Game Denomination
        public void DeleteDenomination(byte[] gameNumber, byte code)
        {
            byte gamen = 0x00;
            if (gameNumber.Length == 2)
            {
                gamen = gameNumber[1];
            }
            else if (gameNumber.Length == 1)
            {
                gamen = gameNumber[0];
            }
            GXGSASAPIBridge.GXG_SAS_DelGameDenom(gamen, code); // nl
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
            GXGSASAPIBridge.GXG_SAS_StartHandPay(new SAS_HANDPAY_INFO { Amount = handpayAmount, Level = 0x00, PartialPay = 0, ProgressiveGroup = 0x00, ResetID = 0x00, Type = (byte)SAS_HANDPAY_VALID_TYPE.SAS_HANDPAY_VALID_CASHOUT }); // nl
        }

        // Reset Handpay
        public void ResetHandpay(byte has_receipt, byte receipt_num)

        {
            SAS_DATE_TIME datenow = new SAS_DATE_TIME();
            GXGSASAPIBridge.GXG_SAS_GetDateTime(ref datenow); // nl
            GXGSASAPIBridge.GXG_SAS_ResetHandPay(datenow, has_receipt, receipt_num); // nl
        }

        // Send Exception
        public void SendException(byte address, byte exception, byte[] data)
        {

            GXGSASAPIBridge.GXG_SAS_InsertException(1, exception); // nl
        }

        // Update Meter
        public void UpdateMeter(byte code, byte[] gameNumber, int value)
        {
            GXGSASAPIBridge.GXG_SAS_SetMachineMeter(code, (uint)value); // nl
        }

        // Get Meter
        public uint GetMeter(byte code, byte[] gameNumber)
        {
            ulong res = 0;
            GXGSASAPIBridge.GXG_SAS_GetMeterEx(0x00, 0x00, code, ref res); // nl
            return (uint)res;
        }

        public void ResetMeters()
        {
            GXGSASAPIBridge.GXG_SAS_ResetAllMeter();
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
            // Complete AFT transfer
            SAS_DATE_TIME dwDateTime = new SAS_DATE_TIME();

            GXGSASAPIBridge.GetCurrentLocalTime(ref dwDateTime);
            GXGSASAPIBridge.GXG_SAS_CompleteAFTTrans(trans_status,
                                cash_amount,
                                res_amount,
                                nonres_amount,
                                ref dwDateTime);
        }

        public void SetCredits(byte type, ulong value)
        {
            GXGSASAPIBridge.GXG_SAS_SetCurrentCedit(type, value);
        }

        void ISASClient.SetBillValidatorEnabledInSAS(bool enabled)
        {
            billacceptordisabled_ = !enabled;
        }

        void ISASClient.SetSASBusy(bool enable)
        {
            GXGSASAPIBridge.GXG_SAS_SetEGMBusy(enable ? (byte)0x01 : (byte)0x00);
        }

        void ISASClient.SetMaintenanceMode(bool set)
        {
            maintenancemode_ = set;
        }

        void ISASClient.SetAssetNumber(int asset_number)
        {
            assetnumber = asset_number;
            GXGSASAPIBridge.GXG_SAS_SetAssetNumber((uint)asset_number);
        }

        private int ResetFullDefault()
        {
            try
            {
                Int32 dwStatus;
                Int32 dwTemp = 1;
                Int32[] dwDateTime = new Int32[6];
                byte[] bFeatures = new byte[3];
                int i = 0, j = 0;

                m_pSASHandlerTimer.Stop();

                gXGSASROMSigCalculator.StopThread();

                // Disable the GXGSAS function first before RAM clear
                GXGSASAPIBridge.GXG_SAS_SetEnabled(g_bSASValidLBPort, GXGSASAPIConst.FALSE);
                if (g_bSASProgPort != g_bSASValidLBPort)
                {
                    GXGSASAPIBridge.GXG_SAS_SetEnabled(g_bSASProgPort, GXGSASAPIConst.FALSE);
                }
                if (g_bSASAFTPort != g_bSASValidLBPort && g_bSASAFTPort != g_bSASProgPort)
                {
                    GXGSASAPIBridge.GXG_SAS_SetEnabled(g_bSASAFTPort, GXGSASAPIConst.FALSE);
                }

                // Clear and reset all NVRAM data
                GXGSASAPIBridge.GXG_SAS_ResetDefault();

                // Disable the GXGSAS function first before RAM clear
                GXGSASAPIBridge.GXG_SAS_SetEnabled(g_bSASValidLBPort, GXGSASAPIConst.FALSE);
                if (g_bSASProgPort != g_bSASValidLBPort)
                {
                    GXGSASAPIBridge.GXG_SAS_SetEnabled(g_bSASProgPort, GXGSASAPIConst.FALSE);
                }
                if (g_bSASAFTPort != g_bSASValidLBPort && g_bSASAFTPort != g_bSASProgPort)
                {
                    GXGSASAPIBridge.GXG_SAS_SetEnabled(g_bSASAFTPort, GXGSASAPIConst.FALSE);
                }

                // Clear and reset all NVRAM data
                GXGSASAPIBridge.GXG_SAS_ResetDefault();
                // GXGGFMAPIBridge.GXG_SRAM_EraseBlock8(0, 0x1000, 0x00);

                /*
                 * The following start set all setting to default value
                 */
                //GLU64_BUF g_qwCashoutLimit = new GLU64_BUF();
                //g_qwCashoutLimit.data = new ulong[1] { 999999 };
                //GXGGFMAPIBridge.GXG_SRAM_WriteBlock64(g_qwCashoutLimit, 0, 1);

                //GLU64_BUF g_qwJackpotLimit = new GLU64_BUF();
                //g_qwJackpotLimit.data = new ulong[1] { 999999 };
                //GXGGFMAPIBridge.GXG_SRAM_WriteBlock64(g_qwJackpotLimit, 0, 1);

                ulong g_qwCreditLimit = 9999999999999999;
                GXGSASAPIBridge.GXG_SAS_SetCreditLimit(g_qwCreditLimit);

                byte g_bSASAccDenom = 0x01;
                byte g_bTokenDenom = 0x00;
                string g_cEGMSerialNumStr = "77318989";
                GXGSASAPIBridge.GXG_SAS_SetEGMInfo(g_bSASAccDenom, g_bTokenDenom, g_cEGMSerialNumStr);

                byte g_bImpGameNum = (byte)totalgames;
                uint g_dwEnabledGameMask = 0xFFFF;   // Default enable all games
                GXGSASAPIBridge.GXG_SAS_SetGameInfo(g_bImpGameNum, g_dwEnabledGameMask);

                GXGSASAPIBridge.GXG_SAS_SetCurrentSelectGame(1);

                for (i = 0; i <= totalgames; i++)
                {
                    GXGSASAPIBridge.GXG_SAS_SetGameDetail((byte)i, new SAS_GAME_DETAIL());
                }

                for (i = 0; i <= totalgames; i++)
                {
                    uint g_dwSASFeatureMask = (uint)(BITMASK_SAS_FEATURE.AFT_BONUS_AWARDS.GetHashCode() &
                            ~BITMASK_SAS_FEATURE.LEGACY_BONUS_AWARDS.GetHashCode() |
                            BITMASK_SAS_FEATURE.METER_MODEL_WHEN_WIN.GetHashCode() |
                            BITMASK_SAS_FEATURE.EXTENDED_METER.GetHashCode() |
                            BITMASK_SAS_FEATURE.MULTI_DENOM_EXTENSION.GetHashCode() &
                            ~BITMASK_SAS_FEATURE.VALID_MODE_SYSTEM.GetHashCode() &
                            ~BITMASK_SAS_FEATURE.MULTI_PROG_LP.GetHashCode() &
                            ~BITMASK_SAS_FEATURE.VALID_MODE_SECURE.GetHashCode() &
                            ~BITMASK_SAS_FEATURE.VALID_EXTENSION.GetHashCode() &
                            ~BITMASK_SAS_FEATURE.TICKET_REDEMPTION.GetHashCode() |
                            BITMASK_SAS_FEATURE.TICKET_TO_TOTAL_DROP.GetHashCode());
                    GXGSASAPIBridge.GXG_SAS_SetFeature((byte)i, g_dwSASFeatureMask);

                }

                ushort g_wHostCtrlMachStatus = 0;
                GXGSASAPIBridge.GXG_SAS_GetHostCtrlMachineStatus(ref g_wHostCtrlMachStatus);

                uint g_dwHostCtrlBillDenomCfg = 0;
                byte g_bHostCtrlBAActionFlag = 0;
                GXGSASAPIBridge.GXG_SAS_GetHostCfgBillDenom(ref g_dwHostCtrlBillDenomCfg, ref g_bHostCtrlBAActionFlag);



                // Set AFT transfer limit to $1000.00
                ulong g_qwAFTTransLimit = 99999900;
                GXGSASAPIBridge.GXG_SAS_SetAFTTransLimit(g_qwAFTTransLimit);



                GXGSASAPIBridge.GXG_SAS_SetAssetNumber((uint)assetnumber);

                var g_atAFTRegStatus = SAS_AFT_REGISTER_STATUS.SAS_AFT_REG_STATUS_UNREGISTER;
                byte g_bAFTRegKey = 0;
                byte g_bAFTPosID = 0;
                GXGSASAPIBridge.GXG_SAS_GetAFTRegisterData(ref g_bAFTRegKey, ref g_bAFTPosID);

                var g_bCurrAvailTransStatus = BITMASK_SAS_EGM_AVAIL_TRANS_STATUS.TO_EGM_OK |
                        BITMASK_SAS_EGM_AVAIL_TRANS_STATUS.FROM_EGM_OK |
                        BITMASK_SAS_EGM_AVAIL_TRANS_STATUS.TO_PRINTER_OK;
                /* BITMASK_SAS_EGM_AVAIL_TRANS_STATUS_BONUS_TO_EGM_OK */ // We default use legacy bonus

                GXGSASAPIBridge.GXG_SAS_SetAFTEGMStatus((byte)(SAS_AFT_EGM_STATUS.SAS_AFT_EGM_AVAIL_TRANS_STATUS.GetHashCode()), (byte)g_bCurrAvailTransStatus);

                // Host cashout status is default controlled by host
                var g_bCurrHostCashOutStatus = BITMASK_SAS_EGM_HOST_CASHOUT_STATUS.CTRL_BY_HOST;
                GXGSASAPIBridge.GXG_SAS_SetAFTEGMStatus((byte)(SAS_AFT_EGM_STATUS.SAS_AFT_EGM_HOST_CASHOUT_STATUS.GetHashCode()), (byte)g_bCurrHostCashOutStatus);

                var g_bCurrAFTStatus = BITMASK_SAS_EGM_AFT_STATUS.PRINTER_AVAIL_FOR_RECEIPT |
                        BITMASK_SAS_EGM_AFT_STATUS.TO_HOST_PARTIAL_AMOUNT_AVAIL |
                        BITMASK_SAS_EGM_AFT_STATUS.ENABLE_INHOUSE_TRANS |
                        BITMASK_SAS_EGM_AFT_STATUS.ENABLE_DEBIT_TRANS;
                /* BITMASK_SAS_EGM_AFT_STATUS_ENABLE_BONUS_TRANS */ // We default use legacy bonus
                                                                    // NOTE: BITMASK_SAS_EGM_AFT_STATUS_ENABLE_ANY_AFT = Enable in-house, debit, bonus AFT
                                                                    // NOTE: BITMASK_SAS_EGM_AFT_STATUS_AFT_REGISTERED is controled by GXGSAS
                                                                    // NOTE: BITMASK_SAS_EGM_AFT_STATUS_SUP_CUSTOM_TICKET_DATA is not support by GXGSAS
                GXGSASAPIBridge.GXG_SAS_SetAFTEGMStatus((byte)(SAS_AFT_EGM_STATUS.SAS_AFT_EGM_AFT_STATUS), (byte)g_bCurrAFTStatus);

                /*
                 * Set progressive information (We use SAS-progressive in this example)
                 * 1. Set enabled progressive level (GXG_SAS_SetProgEnabledLevel)
                 * 2. Set progressive group ID (GXG_SAS_SetProgGroupID)
                 * 3. Enable progressive function (GXG_SAS_SetProgEnabled)
                 */
                for (i = 0; i <= totalgames; i++)
                {
                    GXGSASAPIBridge.GXG_SAS_SetProgEnabledLevel((byte)i, 0);
                }
                var g_bProgGroupID = 1;
                GXGSASAPIBridge.GXG_SAS_SetProgGroupID((byte)g_bProgGroupID, 0x00);
                var g_bIsProgEnabled = 0;
                GXGSASAPIBridge.GXG_SAS_SetProgEnabled((byte)0);

                // Default use normal handpay mode
                GXGSASAPIBridge.GXG_SAS_SetHandpayMode(0);

                /*
                 * NOTE: Remember to issue the following exception code after ram clear and reset settings
                 * 1. EC3A (Memory error reset)
                 * 2. EC3C (Operator chagned options)
                 */
                // GLI Test 2017/11/24
                // Suggest issue EC7A (Soft meter reset to zero) by GLI
                GXGSASAPIBridge.GXG_SAS_InsertException(0xFF, 0x3A);
                GXGSASAPIBridge.GXG_SAS_InsertException(0xFF, 0x7A);
                GXGSASAPIBridge.GXG_SAS_InsertException(0xFF, 0x3C);

                GLU8_BUF g_bSASAddr = new GLU8_BUF();
                g_bSASAddr.data = new byte[3] { 0, 0, 0 };
                GLU8_BUF g_bIsSASLinkUp = new GLU8_BUF();
                g_bIsSASLinkUp.data = new byte[3] { 0, 0, 0 };

                for (i = 0; i < 3; i++)
                {
                    g_bSASAddr.data[i] = 1;
                    g_bIsSASLinkUp.data[i] = 0;
                }
                g_bSASValidLBPort = 2;
                g_bSASProgPort = 0;
                g_bSASAFTPort = 1;

                // Reset SAS addr
                //GXGGFMAPIBridge.GXG_SRAM_WriteBlock8(g_bSASAddr, 0x0014, 3);

                // Set handpay mode
                GXGSASAPIBridge.GXG_SAS_SetHandpayMode(0);

                // Update all port setting
                GXGSASAPIBridge.GXG_SAS_SetValidPort(g_bSASValidLBPort);
                GXGSASAPIBridge.GXG_SAS_SetLegacyBonusPort(g_bSASValidLBPort);
                GXGSASAPIBridge.GXG_SAS_SetProgPort(g_bSASProgPort);
                GXGSASAPIBridge.GXG_SAS_SetAFTPort(g_bSASAFTPort);

                // Clear current credit

                // Reset data time
                SAS_DATE_TIME time = new SAS_DATE_TIME();
                GXGSASAPIBridge.GetCurrentLocalTime(ref time);
                GXGSASAPIBridge.GXG_SAS_SetDateTime(time);

                // NOTE: Suggest verify nvram again to make sure there is no any NVRAM hardware failure
                uint res = GXGSASAPIBridge.GXG_SAS_VerifyNVRAM(0, 768);
                if (res != ERROR_CODE.ERR_NONE.GetHashCode())
                {
                    return 1;
                }

                // Disable LPAA Host controled auto bet support
                GXGSASAPIBridge.GXG_SAS_SetLPEnabled(0xAA, 0);

                GXGSASAPIBridge.GXG_SAS_SetEGMAddr(0, g_bSASAddr.data[0]);
                GXGSASAPIBridge.GXG_SAS_SetEGMAddr(1, g_bSASAddr.data[1]);
                GXGSASAPIBridge.GXG_SAS_SetEGMAddr(2, g_bSASAddr.data[2]);

                GXGGFMAPIBridge.GXG_CORE_Release();
                GXGSASAPIBridge.GXG_SAS_Release();

                m_pSASAFTLockTimer.Stop();



            }
            catch
            {

            }
            return 0;
        }

        private int ResetDefault()
        {
            Int32 dwStatus;
            Int32 dwTemp = 1;
            Int32[] dwDateTime = new Int32[6];
            byte[] bFeatures = new byte[3];
            int i = 0, j = 0;

            // Disable the GXGSAS function first before RAM clear
            GXGSASAPIBridge.GXG_SAS_SetEnabled(g_bSASValidLBPort, GXGSASAPIConst.FALSE);
            if (g_bSASProgPort != g_bSASValidLBPort)
            {
                GXGSASAPIBridge.GXG_SAS_SetEnabled(g_bSASProgPort, GXGSASAPIConst.FALSE);
            }
            if (g_bSASAFTPort != g_bSASValidLBPort && g_bSASAFTPort != g_bSASProgPort)
            {
                GXGSASAPIBridge.GXG_SAS_SetEnabled(g_bSASAFTPort, GXGSASAPIConst.FALSE);
            }

            // Clear and reset all NVRAM data
            GXGSASAPIBridge.GXG_SAS_ResetDefault();
            // GXGGFMAPIBridge.GXG_SRAM_EraseBlock8(0, 0x1000, 0x00);

            /*
             * The following start set all setting to default value
             */
            //GLU64_BUF g_qwCashoutLimit = new GLU64_BUF();
            //g_qwCashoutLimit.data = new ulong[1] { 999999 };
            //GXGGFMAPIBridge.GXG_SRAM_WriteBlock64(g_qwCashoutLimit, 0, 1);

            //GLU64_BUF g_qwJackpotLimit = new GLU64_BUF();
            //g_qwJackpotLimit.data = new ulong[1] { 999999 };
            //GXGGFMAPIBridge.GXG_SRAM_WriteBlock64(g_qwJackpotLimit, 0, 1);

            ulong g_qwCreditLimit = 9999999999999999;
            GXGSASAPIBridge.GXG_SAS_SetCreditLimit(g_qwCreditLimit);

            byte g_bSASAccDenom = 0x01;
            byte g_bTokenDenom = 0x00;
            string g_cEGMSerialNumStr = "77318989";
            GXGSASAPIBridge.GXG_SAS_SetEGMInfo(g_bSASAccDenom, g_bTokenDenom, g_cEGMSerialNumStr);

            byte g_bImpGameNum = 1;
            uint g_dwEnabledGameMask = 0xFFFF;   // Default enable all games
            GXGSASAPIBridge.GXG_SAS_SetGameInfo(g_bImpGameNum, g_dwEnabledGameMask);

            GXGSASAPIBridge.GXG_SAS_SetCurrentSelectGame(1);

            for (i = 0; i <= totalgames; i++)
            {
                GXGSASAPIBridge.GXG_SAS_SetGameDetail((byte)i, new SAS_GAME_DETAIL());
            }

            for (i = 0; i <= 1; i++)
            {
                uint g_dwSASFeatureMask = (uint)(BITMASK_SAS_FEATURE.AFT_BONUS_AWARDS.GetHashCode() &
                        ~BITMASK_SAS_FEATURE.LEGACY_BONUS_AWARDS.GetHashCode() |
                        BITMASK_SAS_FEATURE.METER_MODEL_WHEN_WIN.GetHashCode() |
                        BITMASK_SAS_FEATURE.EXTENDED_METER.GetHashCode() |
                        BITMASK_SAS_FEATURE.MULTI_DENOM_EXTENSION.GetHashCode() &
                        ~BITMASK_SAS_FEATURE.VALID_MODE_SYSTEM.GetHashCode() &
                        ~BITMASK_SAS_FEATURE.MULTI_PROG_LP.GetHashCode() &
                        ~BITMASK_SAS_FEATURE.VALID_MODE_SECURE.GetHashCode() &
                        ~BITMASK_SAS_FEATURE.VALID_EXTENSION.GetHashCode() &
                        ~BITMASK_SAS_FEATURE.TICKET_REDEMPTION.GetHashCode() |
                        BITMASK_SAS_FEATURE.TICKET_TO_TOTAL_DROP.GetHashCode() |
                        BITMASK_SAS_FEATURE.AFT.GetHashCode());
                GXGSASAPIBridge.GXG_SAS_SetFeature((byte)i, g_dwSASFeatureMask);

            }

            ushort g_wHostCtrlMachStatus = 0;
            GXGSASAPIBridge.GXG_SAS_GetHostCtrlMachineStatus(ref g_wHostCtrlMachStatus);

            uint g_dwHostCtrlBillDenomCfg = 0;
            byte g_bHostCtrlBAActionFlag = 0;
            GXGSASAPIBridge.GXG_SAS_GetHostCfgBillDenom(ref g_dwHostCtrlBillDenomCfg, ref g_bHostCtrlBAActionFlag);



            // Set AFT transfer limit to $1000.00
            ulong g_qwAFTTransLimit = 99999900;
            GXGSASAPIBridge.GXG_SAS_SetAFTTransLimit(g_qwAFTTransLimit);


            GXGSASAPIBridge.GXG_SAS_SetAssetNumber((uint)assetnumber);

            var g_atAFTRegStatus = SAS_AFT_REGISTER_STATUS.SAS_AFT_REG_STATUS_UNREGISTER;
            byte g_bAFTRegKey = 0;
            byte g_bAFTPosID = 0;
            GXGSASAPIBridge.GXG_SAS_GetAFTRegisterData(ref g_bAFTRegKey, ref g_bAFTPosID);

            var g_bCurrAvailTransStatus = BITMASK_SAS_EGM_AVAIL_TRANS_STATUS.TO_EGM_OK |
                    BITMASK_SAS_EGM_AVAIL_TRANS_STATUS.FROM_EGM_OK |
                    BITMASK_SAS_EGM_AVAIL_TRANS_STATUS.TO_PRINTER_OK;
            /* BITMASK_SAS_EGM_AVAIL_TRANS_STATUS_BONUS_TO_EGM_OK */ // We default use legacy bonus

            GXGSASAPIBridge.GXG_SAS_SetAFTEGMStatus((byte)(SAS_AFT_EGM_STATUS.SAS_AFT_EGM_AVAIL_TRANS_STATUS.GetHashCode()), (byte)g_bCurrAvailTransStatus);

            // Host cashout status is default controlled by host
            var g_bCurrHostCashOutStatus = BITMASK_SAS_EGM_HOST_CASHOUT_STATUS.CTRL_BY_HOST;
            GXGSASAPIBridge.GXG_SAS_SetAFTEGMStatus((byte)(SAS_AFT_EGM_STATUS.SAS_AFT_EGM_HOST_CASHOUT_STATUS.GetHashCode()), (byte)g_bCurrHostCashOutStatus);

            var g_bCurrAFTStatus = BITMASK_SAS_EGM_AFT_STATUS.PRINTER_AVAIL_FOR_RECEIPT |
                    BITMASK_SAS_EGM_AFT_STATUS.TO_HOST_PARTIAL_AMOUNT_AVAIL |
                    BITMASK_SAS_EGM_AFT_STATUS.ENABLE_INHOUSE_TRANS |
                    BITMASK_SAS_EGM_AFT_STATUS.ENABLE_DEBIT_TRANS;
            /* BITMASK_SAS_EGM_AFT_STATUS_ENABLE_BONUS_TRANS */ // We default use legacy bonus
                                                                // NOTE: BITMASK_SAS_EGM_AFT_STATUS_ENABLE_ANY_AFT = Enable in-house, debit, bonus AFT
                                                                // NOTE: BITMASK_SAS_EGM_AFT_STATUS_AFT_REGISTERED is controled by GXGSAS
                                                                // NOTE: BITMASK_SAS_EGM_AFT_STATUS_SUP_CUSTOM_TICKET_DATA is not support by GXGSAS
            GXGSASAPIBridge.GXG_SAS_SetAFTEGMStatus((byte)(SAS_AFT_EGM_STATUS.SAS_AFT_EGM_AFT_STATUS), (byte)g_bCurrAFTStatus);

            /*
             * Set progressive information (We use SAS-progressive in this example)
             * 1. Set enabled progressive level (GXG_SAS_SetProgEnabledLevel)
             * 2. Set progressive group ID (GXG_SAS_SetProgGroupID)
             * 3. Enable progressive function (GXG_SAS_SetProgEnabled)
             */
            for (i = 0; i <= totalgames; i++)
            {
                GXGSASAPIBridge.GXG_SAS_SetProgEnabledLevel((byte)i, 0);
            }
            var g_bProgGroupID = 1;
            GXGSASAPIBridge.GXG_SAS_SetProgGroupID((byte)g_bProgGroupID, 0x00);
            var g_bIsProgEnabled = 0x01;
            GXGSASAPIBridge.GXG_SAS_SetProgEnabled((byte)0);

            // Default use normal handpay mode
            GXGSASAPIBridge.GXG_SAS_SetHandpayMode(0);

            /*
             * NOTE: Remember to issue the following exception code after ram clear and reset settings
             * 1. EC3A (Memory error reset)
             * 2. EC3C (Operator chagned options)
             */
            // GLI Test 2017/11/24
            // Suggest issue EC7A (Soft meter reset to zero) by GLI
            GXGSASAPIBridge.GXG_SAS_InsertException(0xFF, 0x3A);
            GXGSASAPIBridge.GXG_SAS_InsertException(0xFF, 0x7A);
            GXGSASAPIBridge.GXG_SAS_InsertException(0xFF, 0x3C);

            GLU8_BUF g_bSASAddr = new GLU8_BUF();
            g_bSASAddr.data = new byte[3] { 0, 0, 0 };
            GLU8_BUF g_bIsSASLinkUp = new GLU8_BUF();
            g_bIsSASLinkUp.data = new byte[3] { 0, 0, 0 };

            for (i = 0; i < 3; i++)
            {
                g_bSASAddr.data[i] = 1;
                g_bIsSASLinkUp.data[i] = 0;
            }
            g_bSASValidLBPort = 2;
            g_bSASProgPort = 0;
            g_bSASAFTPort = 1;

            // Reset SAS addr
            //GXGGFMAPIBridge.GXG_SRAM_WriteBlock8(g_bSASAddr, 0x0014, 3);

            // Set handpay mode
            GXGSASAPIBridge.GXG_SAS_SetHandpayMode(0);

            // Update all port setting
            GXGSASAPIBridge.GXG_SAS_SetValidPort(g_bSASValidLBPort);
            GXGSASAPIBridge.GXG_SAS_SetLegacyBonusPort(g_bSASValidLBPort);
            GXGSASAPIBridge.GXG_SAS_SetProgPort(g_bSASProgPort);
            GXGSASAPIBridge.GXG_SAS_SetAFTPort(g_bSASAFTPort);

            // Clear current credit

            // Reset data time
            SAS_DATE_TIME time = new SAS_DATE_TIME();
            GXGSASAPIBridge.GetCurrentLocalTime(ref time);
            GXGSASAPIBridge.GXG_SAS_SetDateTime(time);

            // NOTE: Suggest verify nvram again to make sure there is no any NVRAM hardware failure
            uint res = GXGSASAPIBridge.GXG_SAS_VerifyNVRAM(0, 768);
            if (res != ERROR_CODE.ERR_NONE.GetHashCode())
            {
                return 1;
            }

            // Disable LPAA Host controled auto bet support
            GXGSASAPIBridge.GXG_SAS_SetLPEnabled(0xAA, 0);

            GXGSASAPIBridge.GXG_SAS_SetEGMAddr(0, g_bSASAddr.data[0]);
            GXGSASAPIBridge.GXG_SAS_SetEGMAddr(1, g_bSASAddr.data[1]);
            GXGSASAPIBridge.GXG_SAS_SetEGMAddr(2, g_bSASAddr.data[2]);

            for (i = 0; i < 3; i++)
            {
                if (i == g_bSASValidLBPort || i == g_bSASAFTPort || i == g_bSASProgPort)
                {
                    GXGSASAPIBridge.GXG_SAS_SetEnabled((byte)i, 1);
                }
                else
                {
                    GXGSASAPIBridge.GXG_SAS_SetEnabled((byte)i, 0);
                }
            }

            return 0;
        }

        void ISASClient.EnableAFT()
        {
            int i = 0, j = 0;

            for (i = 0; i <= totalgames; i++)
            {
                GXGSASAPIBridge.GXG_SAS_SetGameDetail((byte)i, new SAS_GAME_DETAIL());
            }

            GXGSASAPIBridge.GXG_SAS_SetEnabled((byte)g_bSASAFTPort, 0);
            for (i = 0; i <= totalgames; i++)
            {

                uint g_dwSASFeatureMask = 0;
                GXGSASAPIBridge.GXG_SAS_GetFeature((byte)i, ref g_dwSASFeatureMask);
                g_dwSASFeatureMask = (uint)(g_dwSASFeatureMask |
                     (uint)BITMASK_SAS_FEATURE.AFT.GetHashCode());

                GXGSASAPIBridge.GXG_SAS_SetFeature((byte)i, g_dwSASFeatureMask);
            }



            GXGSASAPIBridge.GXG_SAS_SetAssetNumber(12345);

            var g_atAFTRegStatus = SAS_AFT_REGISTER_STATUS.SAS_AFT_REG_STATUS_UNREGISTER;
            byte g_bAFTRegKey = 0;
            byte g_bAFTPosID = 0;
            GXGSASAPIBridge.GXG_SAS_GetAFTRegisterData(ref g_bAFTRegKey, ref g_bAFTPosID);

            var g_bCurrAvailTransStatus = BITMASK_SAS_EGM_AVAIL_TRANS_STATUS.TO_EGM_OK |
                    BITMASK_SAS_EGM_AVAIL_TRANS_STATUS.FROM_EGM_OK |
                    BITMASK_SAS_EGM_AVAIL_TRANS_STATUS.TO_PRINTER_OK;
            /* BITMASK_SAS_EGM_AVAIL_TRANS_STATUS_BONUS_TO_EGM_OK */ // We default use legacy bonus

            GXGSASAPIBridge.GXG_SAS_SetAFTEGMStatus((byte)(SAS_AFT_EGM_STATUS.SAS_AFT_EGM_AVAIL_TRANS_STATUS.GetHashCode()), (byte)g_bCurrAvailTransStatus);

            // Host cashout status is default controlled by host
            var g_bCurrHostCashOutStatus = BITMASK_SAS_EGM_HOST_CASHOUT_STATUS.CTRL_BY_HOST;
            GXGSASAPIBridge.GXG_SAS_SetAFTEGMStatus((byte)(SAS_AFT_EGM_STATUS.SAS_AFT_EGM_HOST_CASHOUT_STATUS.GetHashCode()), (byte)g_bCurrHostCashOutStatus);

            var g_bCurrAFTStatus = BITMASK_SAS_EGM_AFT_STATUS.PRINTER_AVAIL_FOR_RECEIPT |
                    BITMASK_SAS_EGM_AFT_STATUS.TO_HOST_PARTIAL_AMOUNT_AVAIL |
                    BITMASK_SAS_EGM_AFT_STATUS.ENABLE_INHOUSE_TRANS |
                    BITMASK_SAS_EGM_AFT_STATUS.ENABLE_DEBIT_TRANS;
            /* BITMASK_SAS_EGM_AFT_STATUS_ENABLE_BONUS_TRANS */ // We default use legacy bonus
                                                                // NOTE: BITMASK_SAS_EGM_AFT_STATUS_ENABLE_ANY_AFT = Enable in-house, debit, bonus AFT
                                                                // NOTE: BITMASK_SAS_EGM_AFT_STATUS_AFT_REGISTERED is controled by GXGSAS
                                                                // NOTE: BITMASK_SAS_EGM_AFT_STATUS_SUP_CUSTOM_TICKET_DATA is not support by GXGSAS
            GXGSASAPIBridge.GXG_SAS_SetAFTEGMStatus((byte)(SAS_AFT_EGM_STATUS.SAS_AFT_EGM_AFT_STATUS), (byte)g_bCurrAFTStatus);



            GLU8_BUF g_bSASAddr = new GLU8_BUF();
            g_bSASAddr.data = new byte[3] { 0, 0, 0 };


            for (i = 0; i < 3; i++)
            {
                g_bSASAddr.data[i] = 1;
            }
            g_bSASAFTPort = 1;

            // Update all port setting
            GXGSASAPIBridge.GXG_SAS_SetAFTPort(g_bSASAFTPort);

            GXGSASAPIBridge.GXG_SAS_SetEGMAddr(1, g_bSASAddr.data[1]);

            for (i = 0; i < 3; i++)
            {
                if (i == g_bSASAFTPort)
                {
                    GXGSASAPIBridge.GXG_SAS_SetEnabled((byte)i, 1);
                }
            }


        }

        void ISASClient.DisableAFT()
        {
            byte g_bSASAFTPort = 1;

            GXGSASAPIBridge.GXG_SAS_SetEnabled((byte)g_bSASAFTPort, 0);
            for (int i = 0; i <= totalgames; i++)
            {

                uint g_dwSASFeatureMask = 0;
                GXGSASAPIBridge.GXG_SAS_GetFeature((byte)i, ref g_dwSASFeatureMask);
                g_dwSASFeatureMask = (uint)(g_dwSASFeatureMask &
                     (uint)~BITMASK_SAS_FEATURE.AFT.GetHashCode());

                GXGSASAPIBridge.GXG_SAS_SetFeature((byte)i, g_dwSASFeatureMask);
            }

        }


        int ISASClient.RequestFullRAMClear()
        {
            return ResetFullDefault();
        }
        int ISASClient.RequestPartialRAMClear()
        {
            return ResetDefault();

        }

        void ISASClient.SetSASAddress(byte address)
        {
            sasId_address = address;
            // Enable SAS function for all used ports
            GXGSASAPIBridge.GXG_SAS_SetEnabled(1, GXGSASAPIConst.FALSE);

            // Set all SAS port's address
            for (byte i = 0; i < GXGSASAPIConst.NUM_SUP_SAS_PORT; i++)
            {
                var dwStatus = GXGSASAPIBridge.GXG_SAS_SetEGMAddr(i, address);
                if (dwStatus != (UInt32)ERROR_CODE.ERR_NONE)
                {
                }
            }

            // Enable SAS function for all used ports
            GXGSASAPIBridge.GXG_SAS_SetEnabled(1, GXGSASAPIConst.TRUE);

        }

        void ISASClient.SetSerialNumber(int serial_number)
        {
            byte g_bSASAccDenom = 0x00;
            byte g_bTokenDenom = 0x00;
            string g_cEGMSerialNumStr = "";
            // Get EGM Info
            GXGSASAPIBridge.GXG_SAS_GetEGMInfo(ref g_bSASAccDenom, ref g_bTokenDenom, ref g_cEGMSerialNumStr);
            // Set the serial number only
            g_cEGMSerialNumStr = serial_number.ToString();
            GXGSASAPIBridge.GXG_SAS_SetEGMInfo(g_bSASAccDenom, g_bTokenDenom, g_cEGMSerialNumStr);
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
            SAS_GAME_DETAIL detail = new SAS_GAME_DETAIL();
            detail.GameIDStr = cGameIDStr;
            detail.AddiIDStr = cAddiIDStr;
            detail.GameNameStr = cGameNameStr;
            detail.PayTableIDStr = cPayTableIDStr;
            detail.PayTableNameStr = cPayTableNameStr;
            detail.MaxBet = (ushort)MaxBet;
            detail.GameOption = (ushort)GameOption;
            detail.PayBackPerc = (ushort)PaybackPerc;
            detail.CashoutLimit = (ushort)CashoutLimit;
            detail.WagerCatNum = (ushort)0;
            GXGSASAPIWrap.GXGSASAPIBridge.GXG_SAS_SetGameDetail(0, detail);
        }



        void ISASClient.AcceptTransfer(bool toEGM, bool toHost)
        {
            byte g_bCurrAvailTransStatus = 0;


            GXGSASAPIBridge.GXG_SAS_GetAFTEGMStatus((byte)(SAS_AFT_EGM_STATUS.SAS_AFT_EGM_AVAIL_TRANS_STATUS.GetHashCode()), ref g_bCurrAvailTransStatus);
            if (toEGM)
                g_bCurrAvailTransStatus = (byte)((uint)g_bCurrAvailTransStatus | (uint)BITMASK_SAS_EGM_AVAIL_TRANS_STATUS.TO_EGM_OK);
            if (toHost)
                g_bCurrAvailTransStatus = (byte)((uint)g_bCurrAvailTransStatus | (uint)BITMASK_SAS_EGM_AVAIL_TRANS_STATUS.FROM_EGM_OK);

            GXGSASAPIBridge.GXG_SAS_SetAFTEGMStatus((byte)(SAS_AFT_EGM_STATUS.SAS_AFT_EGM_AVAIL_TRANS_STATUS.GetHashCode()), (byte)g_bCurrAvailTransStatus);
        }

        void ISASClient.RejectTransfer(bool toEGM, bool toHost)
        {
            byte g_bCurrAvailTransStatus = 0;


            GXGSASAPIBridge.GXG_SAS_GetAFTEGMStatus((byte)(SAS_AFT_EGM_STATUS.SAS_AFT_EGM_AVAIL_TRANS_STATUS.GetHashCode()), ref g_bCurrAvailTransStatus);

            if (toEGM)
                g_bCurrAvailTransStatus = (byte)((uint)g_bCurrAvailTransStatus & (uint)~BITMASK_SAS_EGM_AVAIL_TRANS_STATUS.TO_EGM_OK);
            if (toHost)
                g_bCurrAvailTransStatus = (byte)((uint)g_bCurrAvailTransStatus & (uint)~BITMASK_SAS_EGM_AVAIL_TRANS_STATUS.FROM_EGM_OK);


            GXGSASAPIBridge.GXG_SAS_SetAFTEGMStatus((byte)(SAS_AFT_EGM_STATUS.SAS_AFT_EGM_AVAIL_TRANS_STATUS.GetHashCode()), (byte)g_bCurrAvailTransStatus);
        }


        #endregion














    }
}

