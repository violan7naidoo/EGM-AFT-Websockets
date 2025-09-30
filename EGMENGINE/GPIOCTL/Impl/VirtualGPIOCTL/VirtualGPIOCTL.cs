using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace EGMENGINE.GPIOCTL.Impl.VirtualGPIOCTL
{
    internal class VirtualGPIOCTL : IGPIOCTL
    {
        private Dictionary<OutputName, int> outputs;

        private Dictionary<ButtonName, int> buttons;

        private Dictionary<SensorName, int> sensors;

        private string InfoPipeEvents = "VGPIOExternalPipe";
        Timer _timer = new Timer(500);

        public VirtualGPIOCTL()
        {
            // Mapping for each output to int or byte address or identifier of GXGGFMAPI
            outputs = new Dictionary<OutputName, int>
            {
                { OutputName.O_SPINBUTTONLIGHT, 0 },
                { OutputName.O_LINESBUTTONLIGHT, 0 },
                { OutputName.O_BETBUTTONLIGHT, 0 },
                { OutputName.O_HELPBUTTONLIGHT, 0 },
                { OutputName.O_CASHOUTBUTTONLIGHT, 0 },
                { OutputName.O_TOWER1LIGHT, 0 },
                { OutputName.O_TOWER2LIGHT, 0 }

            };

            // Mapping for each button to int or byte address or identifier of GXGGFMAPI
            buttons = new Dictionary<ButtonName, int>
            {
                { ButtonName.B_SPIN, 0 },
                { ButtonName.B_LINES, 0 },
                { ButtonName.B_BET, 0 },
                { ButtonName.B_HELP, 0 },
                { ButtonName.B_CASHOUT, 0 },
                { ButtonName.B_SERVICE, 0 },
                { ButtonName.B_KEY, 0 },
                { ButtonName.B_AUTOSPIN, 0 },
                { ButtonName.B_MAXBET, 0 }


            };

            // Mapping for each sensor to int or byte address or identifier of GXGGFMAPI
            sensors = new Dictionary<SensorName, int>
            {
                 { SensorName.D_CASHBOXDOOR, 0 },
                 { SensorName.D_DROPBOXDOOR, 0 },
                 { SensorName.D_MAINDOOR, 0 },
                 { SensorName.D_BELLYDOOR, 0 },
            };

            _timer.Elapsed += (sender, e) => ListenPipe();
            _timer.Start();
        }

        void IGPIOCTL.StartGPIOCTL()
        {
            Task.Run(ListenPipe);
        }
        /// <summary>
        /// Listens the pipe for events
        /// </summary>
        void ListenPipe()
        {
            if (_timer.Enabled)
            {
                _timer.Enabled = false;
                try
                {
                    // Creates and open the pipe for receive events continuoslly
                    NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", InfoPipeEvents, PipeDirection.In);

                    pipeClient.Connect();

                    using var sr = new StreamReader(pipeClient);
                    string? temp;
                    while ((temp = sr.ReadLine()) != null)
                    {
                        try
                        {
                            switch (temp)
                            {
                                case "MainDoorOpen":
                                    {
                                        sensors[SensorName.D_MAINDOOR] = 1;
                                        SensorOpened(SensorName.D_MAINDOOR, this);
                                        break;
                                    }
                                case "MainDoorClosed":
                                    {
                                        sensors[SensorName.D_MAINDOOR] = 0;
                                        SensorClosed(SensorName.D_MAINDOOR, this);
                                        break;
                                    }
                                case "CardCageDoorOpen":
                                    {
                                        sensors[SensorName.D_CARDCAGEDOOR] = 1;
                                        SensorOpened(SensorName.D_CARDCAGEDOOR, this);
                                        break;
                                    }
                                case "CardCageDoorClosed":
                                    {
                                        sensors[SensorName.D_CARDCAGEDOOR] = 0;
                                        SensorClosed(SensorName.D_CARDCAGEDOOR, this);
                                        break;
                                    }
                                case "BellyDoorOpen":
                                    {
                                        sensors[SensorName.D_BELLYDOOR] = 1;
                                        SensorOpened(SensorName.D_BELLYDOOR, this);
                                        break;
                                    }
                                case "BellyDoorClosed":
                                    {
                                        sensors[SensorName.D_BELLYDOOR] = 0;
                                        SensorClosed(SensorName.D_BELLYDOOR, this);
                                        break;
                                    }
                                case "CashboxDoorOpen":
                                    {
                                        sensors[SensorName.D_CASHBOXDOOR] = 1;
                                        SensorOpened(SensorName.D_CASHBOXDOOR, this);
                                        break;
                                    }
                                case "CashboxDoorClosed":
                                    {
                                        sensors[SensorName.D_CASHBOXDOOR] = 0;
                                        SensorClosed(SensorName.D_CASHBOXDOOR, this);
                                        break;
                                    }
                                case "DropDoorOpen":
                                    {
                                        sensors[SensorName.D_DROPBOXDOOR] = 1;
                                        SensorOpened(SensorName.D_DROPBOXDOOR, this);
                                        break;
                                    }
                                case "DropDoorClosed":
                                    {
                                        sensors[SensorName.D_DROPBOXDOOR] = 0;
                                        SensorClosed(SensorName.D_DROPBOXDOOR, this);
                                        break;
                                    }
                                case "LogicDoorOpen":
                                    {
                                        sensors[SensorName.D_LOGICDOOR] = 1;
                                        SensorOpened(SensorName.D_LOGICDOOR, this);
                                        break;
                                    }
                                case "LogicDoorClosed":
                                    {
                                        sensors[SensorName.D_LOGICDOOR] = 0;
                                        SensorClosed(SensorName.D_LOGICDOOR, this);
                                        break;
                                    }
                                case "SpinButtonPressed":
                                    {
                                        buttons[ButtonName.B_SPIN] = 1;
                                        ButtonPressed(ButtonName.B_SPIN, this);
                                        break;
                                    }
                                case "SpinButtonReleased":
                                    {
                                        buttons[ButtonName.B_SPIN] = 0;
                                        break;
                                    }
                                case "KeyButtonPressed":
                                    {
                                        buttons[ButtonName.B_KEY] = 1;
                                        ButtonPressed(ButtonName.B_KEY, this);
                                        break;
                                    }
                                case "KeyButtonReleased":
                                    {
                                        buttons[ButtonName.B_KEY] = 0;
                                        break;
                                    }
                                case "ServiceButtonPressed":
                                    {
                                        buttons[ButtonName.B_SERVICE] = 1;
                                        ButtonPressed(ButtonName.B_SERVICE, this);
                                        break;
                                    }
                                case "ServiceButtonReleased":
                                    {
                                        buttons[ButtonName.B_SERVICE] = 0;
                                        break;
                                    }
                                case "LinesButtonPressed":
                                    {
                                        buttons[ButtonName.B_LINES] = 1;
                                        ButtonPressed(ButtonName.B_LINES, this);
                                        break;
                                    }
                                case "LinesButtonReleased":
                                    {
                                        buttons[ButtonName.B_LINES] = 0;
                                        break;
                                    }
                                case "BetButtonPressed":
                                    {
                                        buttons[ButtonName.B_BET] = 1;
                                        ButtonPressed(ButtonName.B_BET, this);
                                        break;
                                    }
                                case "BetButtonRelease":
                                    {
                                        buttons[ButtonName.B_BET] = 0;
                                        break;
                                    }
                                case "CashoutButtonPressed":
                                    {
                                        buttons[ButtonName.B_CASHOUT] = 1;
                                        ButtonPressed(ButtonName.B_CASHOUT, this);
                                        break;
                                    }
                                case "CashoutButtonReleased":
                                    {
                                        buttons[ButtonName.B_CASHOUT] = 0;
                                        break;
                                    }
                                case "MaxBetButtonPressed":
                                    {
                                        buttons[ButtonName.B_MAXBET] = 1;
                                        ButtonPressed(ButtonName.B_MAXBET, this);
                                        break;
                                    }
                                case "MaxBetButtonReleased":
                                    {
                                        buttons[ButtonName.B_MAXBET] = 0;
                                        break;
                                    }
                                case "AutoSpinButtonPressed":
                                    {
                                        buttons[ButtonName.B_AUTOSPIN] = 1;
                                        ButtonPressed(ButtonName.B_AUTOSPIN, this);
                                        break;
                                    }
                                case "AutoSpinButtonReleased":
                                    {
                                        buttons[ButtonName.B_AUTOSPIN] = 0;
                                        break;
                                    }
                                case "InfoButtonPressed":
                                    {
                                        buttons[ButtonName.B_HELP] = 1;
                                        ButtonPressed(ButtonName.B_HELP, this);
                                        break;
                                    }
                                case "InfoButtonReleased":
                                    {
                                        buttons[ButtonName.B_HELP] = 0;
                                        break;
                                    }
                                case "GameSelectButtonPressed":
                                    {
                                        Bill10Inserted(this);
                                        break;
                                    }
                                default:
                                    break;
                            }


                        }
                        catch
                        {

                        }

                    }
                }
                catch
                {

                }
                _timer.Enabled = true;

            }

        }





        public delegate void VIRTUALGPIOCTL_Bill10Inserted(Object sender);

        public event GPIOCTL_SensorOpenedEvent SensorOpened;

        public event GPIOCTL_SensorClosedEvent SensorClosed;

        public event GPIOCTL_ButtonPressedEvent ButtonPressed;

        public event GPIOCTL_ButtonReleasedEvent ButtonReleased;

        public event GPIOCTL_BatteryLowEvent BatteryLow;

        public event VIRTUALGPIOCTL_Bill10Inserted Bill10Inserted;

        bool IGPIOCTL.GetButtonStatus(ButtonName button)
        {
            return false;
        }

        bool IGPIOCTL.GetOutputStatus(OutputName output)
        {
            return false;
        }

        bool IGPIOCTL.GetSensorStatus(SensorName sensor)
        {
            try
            {
                return sensors[sensor] == 1;
            }
            catch
            {
                return true;
            }
        }

        void IGPIOCTL.SetAllOutputOff()
        {
        }

        void IGPIOCTL.SetOutputOff(OutputName output)
        {

        }

        void IGPIOCTL.SetOutputOn(OutputName output)
        {

        }


    }
}
