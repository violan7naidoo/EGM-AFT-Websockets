using EGMENGINE.EGMPlayModule.Impl.Slot;
using EGMENGINE;
using EGMENGINE.GPIOCTL;
using EGMENGINE.GUI.GAMETYPES;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using SlotMathCore.Model;
using EGMENGINE.EGMStatusModule;
using EGMENGINE.GLOBALTYPES;

namespace EGMENGINE.GUI
{
    public class GameGUIController
    {

        private static GameGUIController gameGUIController_;
        private BaseSpin currentSpin;
        private SpinRepresentationStatus currentSpinRepresentationStatus;
        internal bool acceptPlayRequest;
        public static GameGUIController GetInstance()
        {
            if (gameGUIController_ == null)
            {
                gameGUIController_ = new GameGUIController();

                //
            }
            return gameGUIController_;
        }

        protected GameGUIController() {
            acceptPlayRequest = true; // Forced temporaly
            EGM.GetInstance();
        }

        public SpinMarshall GUI_SpinButtonPressed(int winAmountcents, int betAmountcents)
        {
            if (acceptPlayRequest)
            {
                return EGM.GetInstance().Game_SlotCreatePlay(winAmountcents, betAmountcents);
            }
            else
            {
                return null;
            }
           
        }

        /// <summary>
        /// Increase Selected Credit Value
        /// </summary>
        /// <returns></returns>
        public void IncreaseSelectedCreditValue()
        {
            EGM.GetInstance().Game_IncreaseSelectedCreditValue();
        }

        /// <summary>
        /// Decrease Selected Credit Value
        /// </summary>
        /// <returns></returns>
        public void DecreaseSelectedCreditValue()
        {
            EGM.GetInstance().Game_DecreaseSelectedCreditValue();
        }

        public SpinMarshall? GUI_GetLastPlay()
        {
            return EGM.GetInstance().Game_Slot_GetLastPlay();
        }

        /// <summary>
        /// Auto Spin Toggle
        /// </summary>
        public void AutoSpinToggle()
        {
            EGM.GetInstance().Game_AutoSpinToggle();
        }

        /// <summary>
        /// Increase Lines Bet
        /// </summary>
        public void IncreaseLinesBet()
        {
            EGM.GetInstance().Game_IncreaseLinesBet();
        }

        /// <summary>
        /// Decrease Lines Bet
        /// </summary>
        public void DecreaseLinesBet()
        {   
            EGM.GetInstance().Game_DecreaseLinesBet();

        }

        /// <summary>
        /// Increase Bet
        /// </summary>
        public void IncreaseBet()
        {
            EGM.GetInstance().Game_IncreaseBet();
        }

        /// <summary>
        /// Decrease Bet
        /// </summary>
        public void DecreaseBet()
        {
            EGM.GetInstance().Game_DecreaseBet();
        }

        public GameUIState GUI_getGameUIState()
        {
            /* Instancio una clase para estructura respuesta */
            GameUIState guis = EGM.GetInstance().Game_getGameUIState();

            /* Devolver un SpinMarshall */
            return guis;


        }

        public FrontEndPlayStatus GUI_get_FrontEndPlayStatus()
        {
            return EGM.GetInstance().getFrontEndPlayStatus();
        }

        public FrontEndPlayPennyStatus GUI_get_FrontEndPlayPennyStatus()
        {
            return EGM.GetInstance().getFrontEndPlayPennyStatus();
        }

        public void GUI_EnterShowingHelp()
        {
            EGM.GetInstance().Game_EnterShowingHelp();
        }

        public void GUI_ExitShowingHelp()
        {
            EGM.GetInstance().Game_ExitShowingHelp();
        }


        public void AnimationStatusUpdate(AnimationEvent anevent)
        {
            EGM.GetInstance().Game_AnimationUpdate(anevent);
        }

        public BaseSpin GetCurrentSpin()
        {
            return currentSpin;
        }

        internal BaseSpin StartSpin(string gameId, int bpl, int lines)
        {
            return new BaseSpin(bpl, lines);
        }

    }
}
