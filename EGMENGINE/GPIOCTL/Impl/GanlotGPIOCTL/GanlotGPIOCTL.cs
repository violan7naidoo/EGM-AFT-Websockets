using EGMENGINE.BillAccCTLModule;
using System;
using System.Collections.Generic;
using System.Text;

using GLU64 = System.UInt64;
using GLU32 = System.UInt32;
using GLU16 = System.UInt16;
using GLU8 = System.Byte;
using DWORD = System.UInt32;
using WORD = System.UInt16;
using BYTE = System.Byte;
using GXGGFMAPI;
using System.Net.NetworkInformation;
using System.Linq;
using EGMENGINE.GUI.MENUTYPES;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace EGMENGINE.GPIOCTL.Impl.GanlotGPIOCTL
{
    internal class GanlotGPIOCTL : IGPIOCTL
    {
        private Dictionary<OutputName, int> outputs;

        private Dictionary<ButtonName, int> buttons;

        private Dictionary<SensorName, int> sensors;

        private Dictionary<SensorName, bool> sensorpolarity;
        private bool keypolarity;
        private bool batterypolarity;

        private Dictionary<OutputName, bool> outputs_status;

        private Dictionary<ButtonName, bool> buttons_status;

        private Dictionary<SensorName, bool> sensors_status;

        private byte[] status = new byte[256];


        /// <summary>
        /// Init GanlotGPIOCTL
        /// </summary>
        public GanlotGPIOCTL()
        {
            // Mapping for each output to int or byte address or identifier of GXGGFMAPI
            outputs = new Dictionary<OutputName, int>
            {
                { OutputName.O_SPINBUTTONLIGHT, 7 },
                { OutputName.O_LINESBUTTONLIGHT, 4 },  // ¿GAME SELECT?
                { OutputName.O_BETBUTTONLIGHT, 2 },
                { OutputName.O_HELPBUTTONLIGHT, 3 },
                { OutputName.O_CASHOUTBUTTONLIGHT, 1 },
                { OutputName.O_TOWER1LIGHT, 17 },
                { OutputName.O_TOWER2LIGHT, 19 },
                { OutputName.O_MAXBETBUTTONLIGHT, 6 },
                { OutputName.O_SERVICEBUTTONLIGHT, 0 },
                { OutputName.O_AUTOSPINBUTTONLIGHT, 5 }
            };

            // Set all output status to false
            outputs_status = new Dictionary<OutputName, bool>
            {
                { OutputName.O_SPINBUTTONLIGHT, false },
                { OutputName.O_LINESBUTTONLIGHT, false },  // ¿GAME SELECT?
                { OutputName.O_BETBUTTONLIGHT, false },
                { OutputName.O_HELPBUTTONLIGHT, false },
                { OutputName.O_CASHOUTBUTTONLIGHT, false },
                { OutputName.O_TOWER1LIGHT, false },
                { OutputName.O_TOWER2LIGHT, false },
                { OutputName.O_MAXBETBUTTONLIGHT, false },
                { OutputName.O_SERVICEBUTTONLIGHT, false },
                { OutputName.O_AUTOSPINBUTTONLIGHT, false }
            };

            // Mapping for each button to int or byte address or identifier of GXGGFMAPI
            buttons = new Dictionary<ButtonName, int>
            {
                { ButtonName.B_SPIN, 7 },
                { ButtonName.B_LINES, 4 }, // ¿GAME SELECT?
                { ButtonName.B_BET, 2 },
                { ButtonName.B_MAXBET, 6 },
                { ButtonName.B_SERVICE, 0 },
                { ButtonName.B_HELP, 3 },
                { ButtonName.B_CASHOUT, 1 },
                { ButtonName.B_AUTOSPIN, 5 }

            };

            // Set all buttons status to false
            buttons_status = new Dictionary<ButtonName, bool>
            {
                { ButtonName.B_SPIN, false },
                { ButtonName.B_LINES, false }, // ¿GAME SELECT?
                { ButtonName.B_BET, false},
                { ButtonName.B_MAXBET,false },
                { ButtonName.B_SERVICE, false },
                { ButtonName.B_HELP, false },
                { ButtonName.B_CASHOUT, false },
                { ButtonName.B_AUTOSPIN, false },
                { ButtonName.B_KEY, false }

            };

            // Mapping for each sensor to int or byte address or identifier of GXGGFMAPI
            sensors = new Dictionary<SensorName, int>
            {
                 { SensorName.D_CASHBOXDOOR, 20 },
                 { SensorName.D_DROPBOXDOOR, 21 },
                 { SensorName.D_MAINDOOR, 22 },
                 { SensorName.D_BELLYDOOR, 23 },
                 { SensorName.D_LOGICDOOR, 24 }
            };

            /* POLARITY */

            sensorpolarity = new Dictionary<SensorName, bool>
            {
                 { SensorName.D_CASHBOXDOOR, true },
                 { SensorName.D_DROPBOXDOOR, false },
                 { SensorName.D_MAINDOOR, false },
                 { SensorName.D_BELLYDOOR, true },
                 { SensorName.D_LOGICDOOR, false }
            };
            keypolarity = false;
            batterypolarity = true; 
            /***************/


            // Set all sensors status to false
            sensors_status = new Dictionary<SensorName, bool>
            {
                 { SensorName.D_CASHBOXDOOR, false },
                 { SensorName.D_DROPBOXDOOR, false },
                 { SensorName.D_MAINDOOR, false },
                 { SensorName.D_BELLYDOOR, false },
                 { SensorName.D_LOGICDOOR, false }
            };

        }
        private void ConfigureDoors()
        {
            GXGGFMAPI.PMU_DOOR_CFG door_config = new GXGGFMAPI.PMU_DOOR_CFG();
            ERROR_CODE st;

            for (byte i = 0; i < 8; i++)
            {
                door_config.wDoorSetting |= (ushort)GXGGFMAPI.DOOR_CFG.PMU_DOOR_CLOSE_VIH.GetHashCode();
                door_config.wDoorSetting |= (ushort)GXGGFMAPI.DOOR_CFG.PMU_DOOR_ENABLE.GetHashCode();
                door_config.wDoorSetting |= (ushort)GXGGFMAPI.DOOR_CFG.PMU_DOOR_RECORD_FIFO.GetHashCode();
                door_config.wDoorSetting |= (ushort)GXGGFMAPI.DOOR_CFG.PMU_DOOR_FIFO_8.GetHashCode();
                door_config.wDoorSetting |= (ushort)GXGGFMAPI.DOOR_CFG.PMU_DOOR_RECORD_POOL.GetHashCode();
                st = (ERROR_CODE)GXGGFMAPI.GXGGFMAPIBridge.GXG_PMU_SetDoorConfig(i, door_config);
            }
        }

        private void PMU_NormaliDoorLogic()
        {
            GXGGFMAPI.PMU_DOOR_CFG door_config = new GXGGFMAPI.PMU_DOOR_CFG();
            ERROR_CODE st;

            for (byte i = 0; i < 8; i++)
            {
                st = (ERROR_CODE)GXGGFMAPI.GXGGFMAPIBridge.GXG_PMU_GetDoorConfig(i, ref door_config);
                door_config.wDoorSetting |= (ushort)GXGGFMAPI.DOOR_CFG.PMU_DOOR_CLOSE_VIL.GetHashCode();
                door_config.wDoorSetting &= (ushort)~GXGGFMAPI.DOOR_CFG.PMU_DOOR_CLOSE_VIH.GetHashCode();
                st = (ERROR_CODE)GXGGFMAPI.GXGGFMAPIBridge.GXG_PMU_SetDoorConfig(i, door_config);

            }
        }

        private void CheckButtons()
        {
            foreach (int value in buttons.Values)
            {
                GXG_GetOneGPI((byte)value, ref status[value]);
            }

            foreach (int value in buttons.Values)
            {
                byte state = 0x00;
                GXG_GetOneGPI((byte)value, ref state);
                if (status[value] != state)
                {
                    status[value] = state;
                    if (state == 1)
                    {
                        // Pressed
                        ThrowButtonPressed(true, buttons.Where(b => b.Value == value).FirstOrDefault().Key);
                    }
                    else
                    {

                    }
                }
            }

        }

        ERROR_CODE DispatchLog(bool onlydoor)
        {
            ERROR_CODE st;
            PMU_EVENT_LOG data = new PMU_EVENT_LOG();
            st = (ERROR_CODE)GXGGFMAPI.GXGGFMAPIBridge.GXG_PMU_DispatchLog(ref data);


            if (data.bEventID == 1) // Main door closed
            {
                ThrowSensorClosed(sensorpolarity[SensorName.D_MAINDOOR], SensorName.D_MAINDOOR);
            }
            if (data.bEventID == 2) // Main door open
            {
                ThrowSensorOpened(sensorpolarity[SensorName.D_MAINDOOR], SensorName.D_MAINDOOR);
            }
            if (data.bEventID == 3 && !onlydoor) // reset switch released
            {
                ThrowButtonReleased(keypolarity, ButtonName.B_KEY);
            }
            if (data.bEventID == 4 && !onlydoor) // reset switch pressed 
            {
                ThrowButtonPressed(keypolarity, ButtonName.B_KEY);
            }
            if (data.bEventID == 5) // Belly door closed
            {
                ThrowSensorClosed(sensorpolarity[SensorName.D_BELLYDOOR], SensorName.D_BELLYDOOR);
            }
            if (data.bEventID == 6) // Belly door open
            {
                ThrowSensorOpened(sensorpolarity[SensorName.D_BELLYDOOR], SensorName.D_BELLYDOOR);
            }
            if (data.bEventID == 7) // Cashbox door closed
            {
                ThrowSensorClosed(sensorpolarity[SensorName.D_CASHBOXDOOR], SensorName.D_CASHBOXDOOR);
            }
            if (data.bEventID == 8) // Cashbox door open
            {
                ThrowSensorOpened(sensorpolarity[SensorName.D_CASHBOXDOOR], SensorName.D_CASHBOXDOOR);
            }
            if (data.bEventID == 15) // Logic door closed
            {
                ThrowSensorClosed(sensorpolarity[SensorName.D_LOGICDOOR], SensorName.D_LOGICDOOR);
            }
            if (data.bEventID == 16) // Logic door open
            {
                ThrowSensorOpened(sensorpolarity[SensorName.D_LOGICDOOR], SensorName.D_LOGICDOOR);
            }
            if (data.bEventID == 20 && !onlydoor) // Battery Low
            {
                ThrowBatteryLow(batterypolarity);
            }
            if (data.bEventID == 22 && !onlydoor) // Battery Low
            {
                ThrowBatteryLow(batterypolarity);
            }
            if (data.bEventID == 24 && !onlydoor) // Battery Low
            {
                ThrowBatteryLow(batterypolarity);
            }

            return st;

        }
        void IGPIOCTL.StartGPIOCTL()
        {
            var st = (ERROR_CODE)GXGGFMAPI.GXGGFMAPIBridge.GXG_CORE_Init();
            GXGGFMAPI.BOARD_AVAIL sBoardAvail = new GXGGFMAPI.BOARD_AVAIL();
            st = (ERROR_CODE)GXGGFMAPI.GXGGFMAPIBridge.GXG_CORE_GetBoardAvail(ref sBoardAvail);
            st = (ERROR_CODE)GXGGFMAPI.GXGGFMAPIBridge.GXG_CORE_DisableIntrByType((byte)GXGGFMAPI.INTR_TYPE.INTR_TYPE_GPI);
            st = (ERROR_CODE)GXGGFMAPI.GXGGFMAPIBridge.GXG_CORE_DisableIntrByType((byte)GXGGFMAPI.INTR_TYPE.INTR_TYPE_PIC_IO);

            ConfigureDoors();

            // Register the callback function for handling the GPU event
            st = (ERROR_CODE)GXGGFMAPI.GXGGFMAPIBridge.GXG_CORE_EnableIntrByType((byte)GXGGFMAPI.INTR_TYPE.INTR_TYPE_GPI, sBoardAvail.GPIAvail, 0xFF, 0xFF);
            st = (ERROR_CODE)GXGGFMAPI.GXGGFMAPIBridge.GXG_CORE_EnableIntrByType((byte)GXGGFMAPI.INTR_TYPE.INTR_TYPE_PIC_IO, sBoardAvail.PICIOAvail & 0x07FF, 0xFF, 0xFF);

            // Input  
            #region Input

            #region GPI
            /*****************************/
            /************GPI**************/
            /*****************************/


            /************ BUTTONS READ INITIAL STATE ************/

            CheckButtons();
            /************* BUTTONS CONFIGURATIONS (INPUT) *************/


            // Register the callback function for handling the GPI event
            var dwStatus = (ERROR_CODE)GXGGFMAPI.GXGGFMAPIBridge.GXG_CORE_RegisterIntrCallBackByType((byte)GXGGFMAPI.INTR_TYPE.INTR_TYPE_GPI, new GXGGFMAPI.INTR_FUNC_GPI(data =>
            {
                GLU64 qwMask;
                int i;
                if (data.GPITrigger > 0)
                {
                    qwMask = 0x01;
                    for (i = 0; i < 64; i++)
                    {
                        if ((data.GPITrigger & qwMask) > 0)
                        {
                            if ((data.GPILevel & qwMask) > 0)
                            {
                                // Released
                                ThrowButtonReleased(true, buttons.Where(b => b.Value == i).FirstOrDefault().Key);
                            }
                            else
                            {
                                // Pressed
                                ThrowButtonPressed(true, buttons.Where(b => b.Value == i).FirstOrDefault().Key);
                            }
                        }
                        qwMask <<= 1;
                    }
                }
                if (data.CGPITrigger > 0)
                {
                    qwMask = 0x01;
                    for (i = 0; i < 64; i++)
                    {
                        if ((data.CGPITrigger & qwMask) > 0)
                        {
                            if ((data.CGPILevel & qwMask) > 0)
                            {
                                // Released
                                ThrowButtonReleased(true, buttons.Where(b => b.Value == i).FirstOrDefault().Key);
                            }
                            else
                            {
                                // Pressed
                                ThrowButtonPressed(true, buttons.Where(b => b.Value == i).FirstOrDefault().Key);
                            }
                        }
                        qwMask <<= 1;
                    }
                }

            }));
            if (ERROR_CODE.ERR_NONE != dwStatus)
            {
                // return 1;
            }

            #endregion

            #region PMU
            // Intrusion sensors
            /*****************************/
            /************PMU**************/
            /*****************************/

            // Read all events while off
            while (DispatchLog(true) == 0)
            {

            }

            /************ DOOR READ INITIAL STATE ************/

            //bool normalstate = true;
            //PMU_NormaliDoorLogic();

            //bool normalstate = false;
            //PMU_InvertDoorLogic();

            uint allstatus = 0;
            uint tocompare = 0;
            GXGGFMAPI.GXGGFMAPIBridge.GXG_PMU_GetPortStatus(ref tocompare);
            //  if (allstatus != tocompare)
            //  {
            allstatus = tocompare;
            if ((allstatus & (uint)Math.Pow(2, 0)) > 0) // Main door open
            {
                ThrowSensorOpened(sensorpolarity[SensorName.D_MAINDOOR], SensorName.D_MAINDOOR);

            }
            else // Main door closed
            {
                ThrowSensorClosed(sensorpolarity[SensorName.D_MAINDOOR], SensorName.D_MAINDOOR);
            }

            if ((allstatus & (uint)Math.Pow(2, 1)) > 0) // Reset switch released
            {
                ThrowButtonReleased(keypolarity, ButtonName.B_KEY);
            }
            else // Reset switch pressed 
            {
                ThrowButtonPressed(keypolarity, ButtonName.B_KEY);
            }

            if ((allstatus & (uint)Math.Pow(2, 2)) > 0) // Belly door open
            {
                ThrowSensorOpened(sensorpolarity[SensorName.D_BELLYDOOR], SensorName.D_BELLYDOOR);
            }
            else // Belly door closed
            {
                ThrowSensorClosed(sensorpolarity[SensorName.D_BELLYDOOR], SensorName.D_BELLYDOOR);
            }

            if ((allstatus & (uint)Math.Pow(2, 3)) > 0) // Cashbox door open
            {
                ThrowSensorOpened(sensorpolarity[SensorName.D_CASHBOXDOOR], SensorName.D_CASHBOXDOOR);
            }
            else // Cashbox door closed
            {
                ThrowSensorClosed(sensorpolarity[SensorName.D_CASHBOXDOOR], SensorName.D_CASHBOXDOOR);
            }

            if ((allstatus & (uint)Math.Pow(2, 7)) > 0) // Logic door open
            {
                ThrowSensorOpened(sensorpolarity[SensorName.D_LOGICDOOR], SensorName.D_LOGICDOOR);
            }
            else // Logic door closed
            {
                ThrowSensorClosed(sensorpolarity[SensorName.D_LOGICDOOR], SensorName.D_LOGICDOOR);
            }

            if (((allstatus & (uint)Math.Pow(2, 9)) > 0)
             || ((allstatus & (uint)Math.Pow(2, 10)) > 0)
             || ((allstatus & (uint)Math.Pow(2, 11)) > 0)) // Battery Low
            {
                ThrowBatteryLow(batterypolarity);
            }



            //      }


            /************* DOOR CONFIGURATIONS (INTRUSION) *************/
            // Register the callback function for handling the GPU event
            dwStatus = (ERROR_CODE)GXGGFMAPI.GXGGFMAPIBridge.GXG_CORE_RegisterIntrCallBackByType((byte)GXGGFMAPI.INTR_TYPE.INTR_TYPE_PIC_IO, new GXGGFMAPI.INTR_FUNC_PMU(data =>
            {
                DispatchLog(false);
            }));
            if (ERROR_CODE.ERR_NONE != dwStatus)
            {
                // return 1;
            }
            #endregion
            #endregion

            // Ouput

            #region Output
            foreach (OutputName output in outputs_status.Keys.ToList())
            {
                GLU8 value = 0;
                GXG_GetOneGPO((byte)outputs[output], ref value);
                outputs_status[output] = false;

            }
            #endregion

        }

        public event GPIOCTL_SensorOpenedEvent SensorOpened;

        public event GPIOCTL_SensorClosedEvent SensorClosed;

        public event GPIOCTL_ButtonPressedEvent ButtonPressed;

        public event GPIOCTL_ButtonReleasedEvent ButtonReleased;

        public event GPIOCTL_BatteryLowEvent BatteryLow;

        private void ThrowSensorOpened(bool normalstate, SensorName sensor)
        {
            if (normalstate)
            {
                SensorOpened.Invoke(sensor, this);
                sensors_status[sensor] = true;
            }
            else
            {
                SensorClosed.Invoke(sensor, this);
                sensors_status[sensor] = false;
            }


        }

        private void ThrowBatteryLow(bool normalstate)
        {
            if (normalstate)
                BatteryLow.Invoke(this);
        }

        private void ThrowSensorClosed(bool normalstate, SensorName sensor)
        {
            if (normalstate)
            {
                SensorClosed.Invoke(sensor, this);
                sensors_status[sensor] = false;
            }
            else
            {
                SensorOpened.Invoke(sensor, this);
                sensors_status[sensor] = true;
            }

        }
        private void ThrowButtonPressed(bool normalstate, ButtonName button)
        {
            if (normalstate)
            {
                ButtonPressed.Invoke(button, this);
                buttons_status[button] = true;
            }
            else
            {
                ButtonReleased.Invoke(button, this);
                buttons_status[button] = false;
            }
        }
        private void ThrowButtonReleased(bool normalstate, ButtonName button)
        {
            if (normalstate)
            {
                ButtonReleased.Invoke(button, this);
                buttons_status[button] = false;
            }
            else
            {
                ButtonPressed.Invoke(button, this);
                buttons_status[button] = true;
            }
        }

        /// <summary>
        /// SetOneGPO method. By index, sets the value to the respective output
        /// </summary>
        /// <param name="index">output index (int or byte)</param>
        /// <param name="value">output value (int or byte)</param>
        private void GXG_SetOneGPO(GLU8 index, GLU8 value)
        {

            ERROR_CODE dwStatus = (ERROR_CODE)GXGGFMAPIBridge.GXG_IO_SetGPOByIndex(index, value);
            if (dwStatus == ERROR_CODE.ERR_NO_DEV)
                ;
            else if (dwStatus != ERROR_CODE.ERR_NONE)
                ;
            else
                ;
        }

        /// <summary>
        /// GetOneGPO method. By index, gets the value to the respective return value variable
        /// </summary>
        /// <param name="index">output index (int or byte)</param>
        /// <param name="value">value reference</param>
        private void GXG_GetOneGPO(GLU8 index, ref GLU8 value)
        {
            ERROR_CODE dwStatus = (ERROR_CODE)GXGGFMAPIBridge.GXG_IO_GetGPOByIndex(index, ref value);
            if (dwStatus == ERROR_CODE.ERR_NO_DEV)
                ;
            else if (dwStatus != ERROR_CODE.ERR_NONE)
                ;
            else
                ;
        }


        /// <summary>
        /// GetOneGPI method. By index, gets the value to the respective return value variable
        /// </summary>
        /// <param name="index">input index (int or byte)</param>
        /// <param name="value">value reference</param>
        private void GXG_GetOneGPI(GLU8 index, ref GLU8 value)
        {
            ERROR_CODE dwStatus = (ERROR_CODE)GXGGFMAPIBridge.GXG_IO_GetGPIByIndex(index, ref value);
            if (dwStatus == ERROR_CODE.ERR_NO_DEV)
                ;
            else if (dwStatus != ERROR_CODE.ERR_NONE)
                ;
            else
                ;

        }

        /// <summary>
        /// Set Output Off implementation
        /// </summary>
        /// <param name="output"></param>
        void IGPIOCTL.SetOutputOff(OutputName output)
        {
            if (outputs.Keys.Contains(output))
                GXG_SetOneGPO((byte)outputs[output], 0);
            if (outputs_status.Keys.Contains(output))
                outputs_status[output] = false;

        }

        /// <summary>
        /// Set Output On implementation
        /// </summary>
        /// <param name="output"></param>
        void IGPIOCTL.SetOutputOn(OutputName output)
        {
            if (outputs.Keys.Contains(output))
                GXG_SetOneGPO((byte)outputs[output], 1);
            if (outputs_status.Keys.Contains(output))
                outputs_status[output] = true;

        }

        /// <summary>
        /// Set All outpus off implementation
        /// </summary>
        void IGPIOCTL.SetAllOutputOff()
        {
            foreach (OutputName output in outputs.Keys)
            {
                GXG_SetOneGPO((byte)outputs[output], 0);
                outputs_status[output] = false;
            }
        }

        /// <summary>
        /// Get Output status implementation
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        bool IGPIOCTL.GetOutputStatus(OutputName output)
        {
            if (outputs_status.Keys.Contains(output))
                return outputs_status[output];
            else
                return false;
        }

        bool IGPIOCTL.GetSensorStatus(SensorName sensor)
        {
            if (sensors_status.Keys.Contains(sensor))
                return sensors_status[sensor];
            else
                return false;
        }

        bool IGPIOCTL.GetButtonStatus(ButtonName button)
        {
            if (buttons_status.Keys.Contains(button))
                return buttons_status[button];
            else
                return false;
        }
    }
}
