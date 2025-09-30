using System;

namespace EGMENGINE.GPIOCTL
{
    public enum SensorName
    {
        D_MAINDOOR,
        D_BELLYDOOR,
        D_CASHBOXDOOR,
        D_DROPBOXDOOR,
        D_CARDCAGEDOOR,
        D_LOGICDOOR
    }

    public enum ButtonName
    {
        B_SPIN,
        B_BET,
        B_HELP,
        B_LINES,
        B_CASHOUT,
        B_SERVICE,
        B_KEY,
        B_MAXBET,
        B_AUTOSPIN
    }

    public enum OutputName
    {
        O_TOWER1LIGHT,
        O_TOWER2LIGHT,
        O_SPINBUTTONLIGHT,
        O_BETBUTTONLIGHT,
        O_HELPBUTTONLIGHT,
        O_LINESBUTTONLIGHT,
        O_CASHOUTBUTTONLIGHT,
        O_MAXBETBUTTONLIGHT,
        O_SERVICEBUTTONLIGHT,
        O_AUTOSPINBUTTONLIGHT
    }
    internal delegate void GPIOCTL_SensorOpenedEvent(SensorName sensor, Object sender);

    internal delegate void GPIOCTL_SensorClosedEvent(SensorName sensor, Object sender);

    internal delegate void GPIOCTL_ButtonPressedEvent(ButtonName button, Object sender);

    internal delegate void GPIOCTL_ButtonReleasedEvent(ButtonName button, Object sender);
    internal delegate void GPIOCTL_BatteryLowEvent(Object sender);


    /// <summary>
    /// GPIO CTL interface. It is a property of EGM
    /// </summary>
    internal interface IGPIOCTL
    {
        /// Events of GPIOCTL
        public event GPIOCTL_SensorOpenedEvent SensorOpened;
        public event GPIOCTL_SensorClosedEvent SensorClosed;
        public event GPIOCTL_ButtonPressedEvent ButtonPressed;
        public event GPIOCTL_ButtonReleasedEvent ButtonReleased;
        public event GPIOCTL_BatteryLowEvent BatteryLow;

        /// <summary>
        /// Used by EGM in GPIO_SetOutputStatus
        /// </summary>
        /// <param name="output"></param>
        public void SetOutputOn(OutputName output);
        /// <summary>
        /// Used by EGM in GPIO_SetOutputStatus
        /// </summary>
        /// <param name="output"></param>
        public void SetOutputOff(OutputName output);
        /// <summary>
        /// Used by EGM in GPIO_SetAllOutputsOff
        /// </summary>
        /// <param name="output"></param>
        public void SetAllOutputOff();
        /// <summary>
        /// Used by EGM in GPIO_GetOutputStatus
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        public bool GetOutputStatus(OutputName output);
        /// <summary>
        /// Used by EGM in InitGPIOController
        /// </summary>
        public void StartGPIOCTL();

        public bool GetSensorStatus(SensorName sensor);
        public bool GetButtonStatus(ButtonName button);

    }
}