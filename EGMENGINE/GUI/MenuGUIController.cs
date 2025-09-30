using EGMENGINE.GLOBALTYPES;
using EGMENGINE.BillAccCTLModule.BillAccTypes;
using EGMENGINE.GPIOCTL;
using EGMENGINE.GUI.MENUTYPES;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;
using System.Linq;
using System.Linq.Expressions;
using EGMENGINE.EGMSettingsModule;
using EGMENGINE.EGMAccountingModule;
using System.IO;
using SlotMathCore.Model;

namespace EGMENGINE.GUI
{
    public class MenuGUIController
    {

        private static MenuGUIController menuGUIController_;
        private List<EventType> current_events = new List<EventType>();
        private MenuSceneNameAction[,,,,,,] MenuSecurityAccess = new MenuSceneNameAction[,,,,,,] { };
        public static MenuGUIController GetInstance()
        {
            if (menuGUIController_ == null)
            {
                menuGUIController_ = new MenuGUIController();

                //
            }
            return menuGUIController_;
        }


        private void SetMenuSceneAction(UserRole? user, MenuSceneName? name, bool? MainDoorOpen, bool? LogicDoorOpen, bool? credits, bool? ramclearperformed, bool? billacceptorenabled, MenuSceneNameAction action)
        {
            if (user == null && name == null && MainDoorOpen == null && LogicDoorOpen == null && credits == null && ramclearperformed == null && billacceptorenabled == null)
            {
                // Inicialización del array con el valor por defecto
                for (int u = 0; u < MenuSecurityAccess.GetLength(0); u++)
                {
                    for (int n = 0; n < MenuSecurityAccess.GetLength(1); n++)
                    {
                        for (int m = 0; m < MenuSecurityAccess.GetLength(2); m++)
                        {
                            for (int l = 0; l < MenuSecurityAccess.GetLength(3); l++)
                            {
                                for (int c = 0; c < MenuSecurityAccess.GetLength(4); c++)
                                {
                                    for (int r = 0; r < MenuSecurityAccess.GetLength(5); r++)
                                    {
                                        for (int b = 0; b < MenuSecurityAccess.GetLength(6); b++)
                                        {
                                            MenuSecurityAccess[u, n, m, l, c, r, b] = action;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if (user != null && name == null && MainDoorOpen == null && LogicDoorOpen == null && credits == null && ramclearperformed == null && billacceptorenabled == null)
            {
                // Inicialización del array con el valor por defecto
                for (int n = 0; n < MenuSecurityAccess.GetLength(1); n++)
                {
                    for (int m = 0; m < MenuSecurityAccess.GetLength(2); m++)
                    {
                        for (int l = 0; l < MenuSecurityAccess.GetLength(3); l++)
                        {
                            for (int c = 0; c < MenuSecurityAccess.GetLength(4); c++)
                            {
                                for (int r = 0; r < MenuSecurityAccess.GetLength(5); r++)
                                {
                                    for (int b = 0; b < MenuSecurityAccess.GetLength(6); b++)
                                    {
                                        MenuSecurityAccess[user.Value.GetHashCode(), n, m, l, c, r, b] = action;
                                    }
                                }
                            }
                        }
                    }
                }

            }
            else if (user == null && name != null && MainDoorOpen == null && LogicDoorOpen == null && credits == null && ramclearperformed == null && billacceptorenabled == null)
            {
                // Inicialización del array con el valor por defecto
                for (int u = 0; u < MenuSecurityAccess.GetLength(0); u++)
                {
                    for (int m = 0; m < MenuSecurityAccess.GetLength(2); m++)
                    {
                        for (int l = 0; l < MenuSecurityAccess.GetLength(3); l++)
                        {
                            for (int c = 0; c < MenuSecurityAccess.GetLength(4); c++)
                            {
                                for (int r = 0; r < MenuSecurityAccess.GetLength(5); r++)
                                {
                                    for (int b = 0; b < MenuSecurityAccess.GetLength(6); b++)
                                    {
                                        MenuSecurityAccess[u, name.Value.GetHashCode(), m, l, c, r, b] = action;
                                    }
                                }
                            }
                        }
                    }
                }

            }
            else if (user == null && name != null && MainDoorOpen != null && LogicDoorOpen == null && credits == null && ramclearperformed == null && billacceptorenabled == null)
            {
                // Inicialización del array con el valor por defecto
                for (int u = 0; u < MenuSecurityAccess.GetLength(0); u++)
                {
                    for (int l = 0; l < MenuSecurityAccess.GetLength(3); l++)
                    {
                        for (int c = 0; c < MenuSecurityAccess.GetLength(4); c++)
                        {
                            for (int r = 0; r < MenuSecurityAccess.GetLength(5); r++)
                            {
                                for (int b = 0; b < MenuSecurityAccess.GetLength(6); b++)
                                {
                                    MenuSecurityAccess[u, name.GetHashCode(), MainDoorOpen.Value ? 1 : 0, l, c, r, b] = action;
                                }
                            }
                        }
                    }

                }
            }

            else if (user == null && name != null && MainDoorOpen == null && LogicDoorOpen != null && credits == null && ramclearperformed == null && billacceptorenabled == null)
            {
                // Inicialización del array con el valor por defecto
                for (int u = 0; u < MenuSecurityAccess.GetLength(0); u++)
                {
                    for (int m = 0; m < MenuSecurityAccess.GetLength(2); m++)
                    {
                        for (int c = 0; c < MenuSecurityAccess.GetLength(4); c++)
                        {
                            for (int r = 0; r < MenuSecurityAccess.GetLength(5); r++)
                            {
                                for (int b = 0; b < MenuSecurityAccess.GetLength(6); b++)
                                {
                                    MenuSecurityAccess[u, name.GetHashCode(), m, LogicDoorOpen.Value ? 1 : 0, c, r, b] = action;
                                }
                            }
                        }
                    }

                }
            }
            else if (user == null && name != null && MainDoorOpen == null && LogicDoorOpen == null && credits != null && ramclearperformed == null && billacceptorenabled == null)
            {
                // Inicialización del array con el valor por defecto
                for (int u = 0; u < MenuSecurityAccess.GetLength(0); u++)
                {
                    for (int m = 0; m < MenuSecurityAccess.GetLength(2); m++)
                    {
                        for (int l = 0; l < MenuSecurityAccess.GetLength(3); l++)
                        {
                            for (int r = 0; r < MenuSecurityAccess.GetLength(5); r++)
                            {
                                for (int b = 0; b < MenuSecurityAccess.GetLength(6); b++)
                                {
                                    MenuSecurityAccess[u, name.GetHashCode(), m, l, credits.Value ? 1 : 0, r, b] = action;
                                }
                            }
                        }
                    }

                }
            }
            else if (user != null && name != null && MainDoorOpen == null && LogicDoorOpen == null && credits == null && ramclearperformed == null && billacceptorenabled == null)
            {
                // Inicialización del array con el valor por defecto
                for (int m = 0; m < MenuSecurityAccess.GetLength(2); m++)
                {
                    for (int l = 0; l < MenuSecurityAccess.GetLength(3); l++)
                    {
                        for (int c = 0; c < MenuSecurityAccess.GetLength(4); c++)
                        {
                            for (int r = 0; r < MenuSecurityAccess.GetLength(5); r++)
                            {
                                for (int b = 0; b < MenuSecurityAccess.GetLength(6); b++)
                                {
                                    MenuSecurityAccess[user.GetHashCode(), name.GetHashCode(), m, l, c, r, b] = action;
                                }
                            }
                        }
                    }
                }
            }
            else if (user != null && name != null && MainDoorOpen != null && LogicDoorOpen == null && credits == null && ramclearperformed == null && billacceptorenabled == null)
            {
                // Inicialización del array con el valor por defecto
                for (int l = 0; l < MenuSecurityAccess.GetLength(3); l++)
                {
                    for (int c = 0; c < MenuSecurityAccess.GetLength(4); c++)
                    {
                        for (int r = 0; r < MenuSecurityAccess.GetLength(5); r++)
                        {
                            for (int b = 0; b < MenuSecurityAccess.GetLength(6); b++)
                            {
                                MenuSecurityAccess[user.GetHashCode(), name.GetHashCode(), MainDoorOpen.Value ? 1 : 0, l, c, r, b] = action;
                            }
                        }
                    }
                }

            }
            else if (user != null && name != null && MainDoorOpen == null && LogicDoorOpen != null && credits == null && ramclearperformed == null && billacceptorenabled == null)
            {
                // Inicialización del array con el valor por defecto
                for (int m = 0; m < MenuSecurityAccess.GetLength(2); m++)
                {
                    for (int c = 0; c < MenuSecurityAccess.GetLength(4); c++)
                    {
                        for (int r = 0; r < MenuSecurityAccess.GetLength(5); r++)
                        {
                            for (int b = 0; b < MenuSecurityAccess.GetLength(6); b++)
                            {
                                MenuSecurityAccess[user.GetHashCode(), name.GetHashCode(), m, LogicDoorOpen.Value ? 1 : 0, c, r, b] = action;
                            }
                        }
                    }
                }

            }
            else if (user != null && name != null && MainDoorOpen == null && LogicDoorOpen == null && credits != null && ramclearperformed == null && billacceptorenabled == null)
            {

                for (int m = 0; m < MenuSecurityAccess.GetLength(2); m++)
                {
                    for (int l = 0; l < MenuSecurityAccess.GetLength(3); l++)
                    {
                        for (int r = 0; r < MenuSecurityAccess.GetLength(5); r++)
                        {
                            for (int b = 0; b < MenuSecurityAccess.GetLength(6); b++)
                            {
                                MenuSecurityAccess[user.GetHashCode(), name.GetHashCode(), m, l, credits.Value ? 1 : 0, r, b] = action;
                            }
                        }
                    }
                }

            }
            else if (user != null && name != null && MainDoorOpen != null && LogicDoorOpen != null && credits == null && ramclearperformed == null && billacceptorenabled == null)
            {
                for (int c = 0; c < MenuSecurityAccess.GetLength(4); c++)
                {
                    for (int r = 0; r < MenuSecurityAccess.GetLength(5); r++)
                    {
                        for (int b = 0; b < MenuSecurityAccess.GetLength(6); b++)
                        {
                            MenuSecurityAccess[user.GetHashCode(), name.GetHashCode(), MainDoorOpen.Value ? 1 : 0, LogicDoorOpen.Value ? 1 : 0, c, r, b] = action;
                        }
                    }
                }
            }
            else if (user != null && name != null && MainDoorOpen != null && LogicDoorOpen != null && credits == null && ramclearperformed == null && billacceptorenabled != null)
            {
                for (int c = 0; c < MenuSecurityAccess.GetLength(4); c++)
                {
                    for (int r = 0; r < MenuSecurityAccess.GetLength(5); r++)
                    {
                        MenuSecurityAccess[user.GetHashCode(), name.GetHashCode(), MainDoorOpen.Value ? 1 : 0, LogicDoorOpen.Value ? 1 : 0, c, r, billacceptorenabled.Value ? 1 : 0] = action;
                    }
                }
            }
            else if (user != null && name != null && MainDoorOpen != null && LogicDoorOpen == null && credits != null && ramclearperformed == null && billacceptorenabled == null)
            {
                for (int l = 0; l < MenuSecurityAccess.GetLength(3); l++)
                {
                    for (int r = 0; r < MenuSecurityAccess.GetLength(5); r++)
                    {
                        for (int b = 0; b < MenuSecurityAccess.GetLength(6); b++)
                        {
                            MenuSecurityAccess[user.GetHashCode(), name.GetHashCode(), MainDoorOpen.Value ? 1 : 0, l, credits.Value ? 1 : 0, r, b] = action;
                        }
                    }
                }
            }
            else if (user != null && name != null && MainDoorOpen == null && LogicDoorOpen != null && credits != null && ramclearperformed == null && billacceptorenabled == null)
            {
                for (int m = 0; m < MenuSecurityAccess.GetLength(2); m++)
                {
                    for (int r = 0; r < MenuSecurityAccess.GetLength(5); r++)
                    {
                        for (int b = 0; b < MenuSecurityAccess.GetLength(6); b++)
                        {
                            MenuSecurityAccess[user.GetHashCode(), name.GetHashCode(), m, LogicDoorOpen.Value ? 1 : 0, credits.Value ? 1 : 0, r, b] = action;
                        }
                    }
                }
            }
            else if (user != null && name != null && MainDoorOpen != null && LogicDoorOpen != null && credits != null && ramclearperformed == null && billacceptorenabled == null)
            {
                for (int r = 0; r < MenuSecurityAccess.GetLength(5); r++)
                {
                    for (int b = 0; b < MenuSecurityAccess.GetLength(6); b++)
                    {
                        MenuSecurityAccess[user.GetHashCode(), name.GetHashCode(), MainDoorOpen.Value ? 1 : 0, LogicDoorOpen.Value ? 1 : 0, credits.Value ? 1 : 0, r, b] = action;
                    }
                }
            }
            else if (user != null && name != null && MainDoorOpen != null && LogicDoorOpen != null && credits != null && ramclearperformed != null && billacceptorenabled == null)
            {
                for (int b = 0; b < MenuSecurityAccess.GetLength(6); b++)
                {
                    MenuSecurityAccess[user.GetHashCode(), name.GetHashCode(), MainDoorOpen.Value ? 1 : 0, LogicDoorOpen.Value ? 1 : 0, credits.Value ? 1 : 0, ramclearperformed.Value ? 1 : 0, b] = action;
                }

            }

        }


        public EventType? ConsumeEvent()
        {
            if (current_events.Count > 0)
            {
                EventType et = current_events.FirstOrDefault();
                current_events.RemoveAt(0);
                return et;
            }
            return null;
        }

        protected MenuGUIController()
        {
            EGM.GetInstance();
            EGM.GetInstance().UIPriorityEvent += new UIPriorityEventHandler((o, et, t) =>
            {
                current_events.Add(et);
            });
            MenuSecurityAccess = new MenuSceneNameAction[5/*Logged User, from 0 to 4*/, 52 /* Screen, from 0 to 50*/, 2 /*Main Door Open? 0,1*/, 2 /*Logic Door Open? 0,1*/, 2 /*Has credits? 0,1*/, 2 /*Ram Clear Performed? 0,1*/, 2 /*Bill Acceptor Enabled? 0, 1*/];

            SetMenuSceneAction(null, null, null, null, null, null, null, MenuSceneNameAction.NotAuthorized);

            SetMenuSceneAction(UserRole.Manufacturer, MenuSceneName.Statistics, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Manufacturer, MenuSceneName.Statistics_GameStatistics, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Manufacturer, MenuSceneName.Statistics_LastBills, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Manufacturer, MenuSceneName.Statistics_AFTTransaction, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Manufacturer, MenuSceneName.Statistics_LastPlays, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Manufacturer, MenuSceneName.Diagnostics, /*MainDoor*/ true, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Manufacturer, MenuSceneName.Diagnostics_InputOutputTester, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Manufacturer, MenuSceneName.Diagnostics_BillValidatorTest, /*MainDoor*/ true, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Manufacturer, MenuSceneName.Diagnostics_TouchScreenCalibrationUtility, /*MainDoor*/ true, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Manufacturer, MenuSceneName.GameIdentification_SystemID, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Manufacturer, MenuSceneName.GameIdentification_ROMIdInformation, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Manufacturer, MenuSceneName.Configuration, /* MainDoor */ true, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Manufacturer, MenuSceneName.Configuration_GameConfiguration, /* MainDoor */ true, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Manufacturer, MenuSceneName.Configuration_GameConfiguration_BetAndMoneyLimits, /*MainDoor*/ true, null, /* Credits*/ false, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Manufacturer, MenuSceneName.Configuration_CashInCashOutConfiguration, /* MainDoor */ true, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Manufacturer, MenuSceneName.Configuration_SASConfiguration, /* MainDoor */ true, null, /* Credits*/ false, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Manufacturer, MenuSceneName.Configuration_SASConfiguration_SASMainConfiguration, /* MainDoor */ true, /* Logic Door */ true, /* Credits*/ false, /* Ram Clear Performerd */ true, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Manufacturer, MenuSceneName.Configuration_SASConfiguration_SASMainConfiguration, /* MainDoor */ true, /* Logic Door */ true, /* Credits*/ false, /* Ram Clear Performerd */ false, null, MenuSceneNameAction.ReadOnly);
            SetMenuSceneAction(UserRole.Manufacturer, MenuSceneName.Configuration_SASConfiguration_SASHostConfiguration, /* MainDoor */ true, null, /* Credits*/ false, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Manufacturer, MenuSceneName.Configuration_BillsConfiguration,/* MainDoor */ true, /* Logic Door */ true, null, null, /* Bill Acceptor Enabled*/ true, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Manufacturer, MenuSceneName.Configuration_SetDateAndTime, /* MainDoor */ true, /* Logic Door */ true, null, null, null, MenuSceneNameAction.ReadOnly);
            SetMenuSceneAction(UserRole.Manufacturer, MenuSceneName.Configuration_SetDateAndTime, /* MainDoor */ true, /* Logic Door */ true, /* Credits */ false, /* Ram Clear Performerd */ true, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Manufacturer, MenuSceneName.Configuration_SetPassword, /* MainDoor */ true, /* Logic Door */ true, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Manufacturer, MenuSceneName.Configuration_RAMClear, /* MainDoor */ true, /* Logic Door */ true, null, null, null, MenuSceneNameAction.FullAccess);


            SetMenuSceneAction(UserRole.Technician, MenuSceneName.Statistics, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Technician, MenuSceneName.Statistics_GameStatistics, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Technician, MenuSceneName.Statistics_LastBills, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Technician, MenuSceneName.Statistics_AFTTransaction, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Technician, MenuSceneName.Statistics_LastPlays, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Technician, MenuSceneName.Diagnostics, /*MainDoor*/ true, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Technician, MenuSceneName.Diagnostics_BillValidatorTest, /*MainDoor*/ true, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Technician, MenuSceneName.Diagnostics_TouchScreenCalibrationUtility, /*MainDoor*/ true, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Technician, MenuSceneName.GameIdentification_SystemID, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Technician, MenuSceneName.GameIdentification_ROMIdInformation, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Technician, MenuSceneName.Configuration, /* MainDoor */ true, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Technician, MenuSceneName.Configuration_SASConfiguration, /* MainDoor */ true, null, /* Credits*/ false, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Technician, MenuSceneName.Configuration_SASConfiguration_SASHostConfiguration, /* MainDoor */ true, null, /* Credits*/ false, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Technician, MenuSceneName.Configuration_SetDateAndTime, /* MainDoor */ true, /* Logic Door */ true, null, null, null, MenuSceneNameAction.ReadOnly);

            SetMenuSceneAction(UserRole.Operator, MenuSceneName.Statistics, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Operator, MenuSceneName.Statistics_GameStatistics, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Operator, MenuSceneName.Statistics_LastBills, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Operator, MenuSceneName.Statistics_AFTTransaction, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Operator, MenuSceneName.Statistics_LastPlays, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Operator, MenuSceneName.Diagnostics, /*MainDoor*/ true, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Operator, MenuSceneName.Diagnostics_BillValidatorTest, /*MainDoor*/ true, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Operator, MenuSceneName.Diagnostics_TouchScreenCalibrationUtility, /*MainDoor*/ true, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Operator, MenuSceneName.GameIdentification_SystemID, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Operator, MenuSceneName.GameIdentification_ROMIdInformation, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Operator, MenuSceneName.Configuration, /* MainDoor */ true, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Operator, MenuSceneName.Configuration_GameConfiguration, /* MainDoor */ true, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Operator, MenuSceneName.Configuration_GameConfiguration_BetAndMoneyLimits, /*MainDoor*/ true, null, /* Credits*/ false, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Operator, MenuSceneName.Configuration_SASConfiguration, /* MainDoor */ true, null, /* Credits*/ false, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Operator, MenuSceneName.Configuration_SASConfiguration_SASMainConfiguration, /* MainDoor */ true, /* Logic Door */ true, /* Credits*/ false,  /* Ram Clear Performerd */ true, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Operator, MenuSceneName.Configuration_SASConfiguration_SASMainConfiguration, /* MainDoor */ true, /* Logic Door */ true, /* Credits*/ false, /* Ram Clear Performerd */ false, null, MenuSceneNameAction.ReadOnly);
            SetMenuSceneAction(UserRole.Operator, MenuSceneName.Configuration_SASConfiguration_SASHostConfiguration, /* MainDoor */ true, null, /* Credits*/ false, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Operator, MenuSceneName.Configuration_BillsConfiguration,/* MainDoor */ true, /* Logic Door */ true, null, null, /* Bill Acceptor Enabled*/ true, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Operator, MenuSceneName.Configuration_SetDateAndTime, /* MainDoor */ true, /* Logic Door */ true, null, null, null, MenuSceneNameAction.ReadOnly);
            SetMenuSceneAction(UserRole.Operator, MenuSceneName.Configuration_SetDateAndTime, /* MainDoor */ true, /* Logic Door */ true, /* Credits */ false, /* Ram Clear Performerd */ true, true, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Operator, MenuSceneName.Configuration_SetPassword, /* MainDoor */ true, /* Logic Door */ true, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Operator, MenuSceneName.Configuration_RAMClear, /* MainDoor */ true, /* Logic Door */ true, null, null, null, MenuSceneNameAction.FullAccess);

            SetMenuSceneAction(UserRole.Attendant, MenuSceneName.Statistics, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Attendant, MenuSceneName.Statistics_GameStatistics, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Attendant, MenuSceneName.Statistics_LastBills, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Attendant, MenuSceneName.Statistics_AFTTransaction, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Attendant, MenuSceneName.Statistics_LastPlays, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Attendant, MenuSceneName.GameIdentification_SystemID, null, null, null, null, null, MenuSceneNameAction.FullAccess);
            SetMenuSceneAction(UserRole.Attendant, MenuSceneName.GameIdentification_ROMIdInformation, null, null, null, null, null, MenuSceneNameAction.FullAccess);
        }

        /************/
        /*** GAME ***/
        /************/
        #region Game

        /// <summary>
        /// {{ GET }}
        /// Current Credits
        /// </summary>
        /// <returns></returns>
        public decimal GUI_GetCurrentCredits()
        {
            return EGM.GetInstance().EGM_GetCurrentCredits();
        }


        /// <summary>
        /// {{ GET }}
        /// EGM Engine Version
        /// </summary>
        /// <returns></returns>
        public string GUI_GetEGMEngineVersion()
        {
            return "777";
        }


        #endregion

        /**************************/
        /*** GAME CONFIGURATION ***/
        /**************************/
        #region Game Configuration

        /// <summary>
        /// {{ GET }}
        /// Get the SystemId property
        /// </summary>
        /// <returns></returns>
        public GameConfiguration_SystemId GUI_GameConfiguration_Get_SystemId()
        {
            GameConfiguration_SystemId result = new GameConfiguration_SystemId();
            result.systemId = "PGS-6310S";

            return result;

        }


        static string ComputeFolderHash(string folderPath, string hashAlgorithmName)
        {
            using (var hashAlgorithm = HashAlgorithm.Create(hashAlgorithmName))
            {
                if (hashAlgorithm == null)
                    throw new ArgumentException("Algoritmo de hash no soportado.", nameof(hashAlgorithmName));

                var hashBuilder = new StringBuilder();

                // Obtener archivos ordenados para consistencia
                var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
                Array.Sort(files);

                foreach (var file in files)
                {
                    // Leer archivo y calcular hash
                    byte[] fileBytes = File.ReadAllBytes(file);
                    byte[] fileHash = hashAlgorithm.ComputeHash(fileBytes);

                    // Agregar hash del archivo al resultado global
                    hashBuilder.Append(BitConverter.ToString(fileHash).Replace("-", "").ToLower());
                }

                // Calcular hash final combinando todos los hashes de los archivos
                byte[] finalHash = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(hashBuilder.ToString()));
                return BitConverter.ToString(finalHash).Replace("-", "").ToLower();
            }
        }


        /// <summary>
        /// {{ GET }}
        /// Get the RomId property
        /// </summary>
        /// <returns></returns>
        public GameConfiguration_ROMId GUI_GameConfiguration_Get_RomId()
        {
            GameConfiguration_ROMId result = new GameConfiguration_ROMId();
            result.ROMId = "2Dma92mD";

            if (!EGM.GetInstance().unitydevelopment)
            {
                result.ROMId_MD5 = ComputeFolderHash(AppDomain.CurrentDomain.BaseDirectory, "MD5");

                result.ROMId_SHA1 = ComputeFolderHash(AppDomain.CurrentDomain.BaseDirectory, "SHA1");
            }
            else
            {
                result.ROMId_MD5 = "aop8ad0s9d8a90d8a90d80";

                result.ROMId_SHA1 = "kajdlajdlakd8a09";
            }

            return result;

        }

        /// <summary>
        /// {{ GET }}
        /// Get the BV Firmware property
        /// </summary>
        /// <returns></returns>
        public GameConfiguration_BillValidatorFirmware GUI_GameConfiguration_Get_BillValidatorFirmware()
        {
            GameConfiguration_BillValidatorFirmware result = new GameConfiguration_BillValidatorFirmware();
            result.BVFirmware = EGM.GetInstance().billAcc.BillValidatorFirmware;

            return result;

        }
        #endregion

        /****************/
        /*** SECURITY ***/
        /****************/
        #region Security

        /// <summary>
        /// {{ SET }}
        /// Attempt login given a numeric pin
        /// </summary>
        /// <param name="pin"></param>
        /// <returns></returns>
        public UserRole GUI_Security_AttemptLogin(int pin)
        {
            return EGM.GetInstance().EGM_AttemptLogin(pin);
        }

        /// <summary>
        /// {{ GET }}
        /// Current Logged User
        /// </summary>
        /// <returns></returns>
        public UserRole GUI_Security_Get_LoggedUser()
        {
            return EGM.GetInstance().EGM_GetLoggedUser();
        }

        /// <summary>
        /// {{ SET }}
        /// Set Password with new pin
        /// </summary>
        /// <param name="newpin"></param>
        public UserPwdResponse GUI_Security_Set_Password(int newpin, UserRole role)
        {
            return EGM.GetInstance().Menu_UpdatePassword(role, newpin);
        }

        /// <summary>
        /// {{ SET }}
        /// GUI Exit Menu
        /// </summary>
        public void GUI_Menu_ExitToGame(bool sasexception)
        {
            EGM.GetInstance().Menu_ExitToGame(sasexception);
        }

        /// <summary>
        /// {{ GET }}
        /// </summary>
        /// GUI Get MaintenanceStatus
        /// <returns></returns>
        public bool GUI_Menu_GETMaintenanceStatus()
        {
            return EGM.GetInstance().Menu_GETMaintenanceStatus();
        }

        /// <summary>
        /// {{ GET }}
        /// GUI Get Mandatory PC
        /// </summary>
        /// <returns></returns>
        public bool GUI_Menu_GETMandatoryPC()
        {
            return EGM.GetInstance().Menu_GetMandatoryPC();
        }
        /// <summary>
        /// {{ GET }}
        /// GUI Checker if set datetime
        /// </summary>
        /// <returns></returns>
        public bool GUI_Menu_CanSetDateTime()
        {
            return EGM.GetInstance().Menu_CanSetDateTime();
        }
        /// <summary>
        /// {{ GET }}
        /// GUI Checker if set SAS
        /// </summary>
        /// <returns></returns>
        public bool GUI_Menu_CanSetSAS()
        {
            return EGM.GetInstance().Menu_CanSetSAS();
        }


        /// <summary>
        /// {{ SET }}
        /// GUI Enter Maintenance Mode
        /// </summary>
        public void GUI_Menu_EnterMaintenanceMode()
        {
            EGM.GetInstance().Menu_EnterMaintenanceMode();
        }

        /// <summary>
        /// {{ SET }}
        /// GUI Exit Maintenance Mode
        /// </summary>
        public void GUI_Menu_ExitMaintenanceMode()
        {
            EGM.GetInstance().Menu_ExitMaintenanceMode();
        }



        #endregion

        /*********************/
        /*** CONFIGURATION ***/
        /*********************/
        #region Configuration

        /* DATE and TIME Configuration */

        #region BetAndMoneyLimitConfiguration

        /// <summary>
        /// {{ GET }}
        /// Bet and Money limits configuration
        /// </summary>
        /// <returns></returns>
        public Configuration_BetAndMoneyLimitConfiguration GUI_Get_Configuration_BetAndMoneyLimitConfiguration()
        {
            return EGM.GetInstance().Menu_GetBetAndMoneyLimits();
        }

        /// <summary>
        /// {{ SET }}
        /// Bet and Money limits configuration
        /// </summary>
        /// <returns></returns>
        public void GUI_Set_Configuration_BetAndMoneyLimitConfiguration(Configuration_BetAndMoneyLimitConfiguration configuration)
        {
            EGM.GetInstance().Menu_SetBetAndMoneyLimits(configuration);
        }


        #endregion
        #region DateAndTimeconfiguration
        /// <summary>
        /// {{ GET }}
        /// DateTime Configuration
        /// </summary>
        /// <returns></returns>
        public Configuration_DateTimeConfiguration GUI_Get_Configuration_DateTimeConfiguration()
        {
            Configuration_DateTimeConfiguration result = new Configuration_DateTimeConfiguration();
            DateTime now = DateTime.Now;
            result.Year = now.Year;
            result.Hour = now.Hour;
            result.Minute = now.Minute;
            result.Month = now.Month;
            result.Day = now.Day;

            return result;
        }


        /// <summary>
        /// {{ SET }}
        /// DateTime Configuration
        /// </summary>
        /// <param name="configuration"></param>
        public void GUI_Set_Configuration_DateTimeConfiguration(Configuration_DateTimeConfiguration configuration)
        {
            // Set system date and time
            SystemTime updatedTime = new SystemTime();
            updatedTime.Year = (ushort)configuration.Year;
            updatedTime.Month = (ushort)configuration.Month;
            updatedTime.Day = (ushort)configuration.Day;
            updatedTime.Hour = (ushort)configuration.Hour;
            updatedTime.Minute = (ushort)configuration.Minute;
            updatedTime.Second = (ushort)0;

            EGM.GetInstance().Menu_SetDateTime(updatedTime);

        }
        #endregion
        /* SAS Configuration */
        #region SASConfiguration

        /// <summary>
        /// {{ GET }}
        /// SAS Configuration
        /// </summary>
        /// <returns></returns>
        public Configuration_SASConfiguration GUI_Get_Configuration_SASConfiguration()
        {
            Configuration_SASConfiguration result = new Configuration_SASConfiguration();

            result.MainConfiguration = new SASMainConfig(EGM.GetInstance().EGM_GetEGMSettings().sasSettings.mainConfiguration.SASEnabled,
                                                         EGM.GetInstance().EGM_GetEGMSettings().sasSettings.mainConfiguration.SASId,
                                                         EGM.GetInstance().EGM_GetEGMSettings().sasSettings.mainConfiguration.AssetNumber,
                                                         EGM.GetInstance().EGM_GetEGMSettings().sasSettings.mainConfiguration.SerialNumber,
                                                         EGM.GetInstance().EGM_GetEGMSettings().sasSettings.mainConfiguration.TiltOnASASDisconnection,
                                                         EGM.GetInstance().EGM_GetEGMSettings().sasSettings.mainConfiguration.AccountingDenomination,
                                                         EGM.GetInstance().EGM_GetEGMSettings().sasSettings.mainConfiguration.SASReportedDenomination,
                                                         EGM.GetInstance().EGM_GetEGMSettings().sasSettings.mainConfiguration.InHouseInLimit,
                                                         EGM.GetInstance().EGM_GetEGMSettings().sasSettings.mainConfiguration.InHouseOutLimit);

            result.HostConfiguration = new SASHostConfig(EGM.GetInstance().EGM_GetEGMSettings().sasSettings.hostConfiguration.BillAcceptorEnabled,
                                                         EGM.GetInstance().EGM_GetEGMSettings().sasSettings.hostConfiguration.SoundEnabled,
                                                         EGM.GetInstance().EGM_GetEGMSettings().sasSettings.hostConfiguration.RealTimeModeEnabled,
                                                         EGM.GetInstance().EGM_GetEGMSettings().sasSettings.hostConfiguration.ValidateHandpaysReceipts,
                                                         EGM.GetInstance().EGM_GetEGMSettings().sasSettings.hostConfiguration.TicketsForForeignRestrictedAmounts,
                                                         EGM.GetInstance().EGM_GetEGMSettings().sasSettings.hostConfiguration.TicketsRedemptionEnabled);

            result.VLTConfig = new SASVLTConfig(EGM.GetInstance().EGM_GetEGMSettings().sasSettings.vltConfiguration.SASVersion,
                                                EGM.GetInstance().EGM_GetEGMSettings().sasSettings.vltConfiguration.GameID,
                                                EGM.GetInstance().EGM_GetEGMSettings().sasSettings.vltConfiguration.AdditionalID,
                                                EGM.GetInstance().EGM_GetEGMSettings().sasSettings.vltConfiguration.AccountingDenomination,
                                                EGM.GetInstance().EGM_GetEGMSettings().sasSettings.vltConfiguration.MultidenominationEnabled,
                                                EGM.GetInstance().EGM_GetEGMSettings().sasSettings.vltConfiguration.AuthenticationEnabled,
                                                EGM.GetInstance().EGM_GetEGMSettings().sasSettings.vltConfiguration.ExtendedMetersEnabled,
                                                EGM.GetInstance().EGM_GetEGMSettings().sasSettings.vltConfiguration.TicketsCounterEnabled);

            return result;

        }

        /// <summary>
        /// {{ SET}}
        /// SAS Configuration
        /// </summary>
        /// <param name="input"></param>
        public void GUI_Set_Configuration_SASConfiguration(Configuration_SASConfiguration input)
        {
            EGM.GetInstance().Menu_UpdateSASConfiguration(input);
        }
        #endregion

        /* Bills Configuration */
        #region BillsConfiguration
        /// <summary>
        /// {{ GET }}
        /// Bills Configuration
        /// </summary>
        /// <returns></returns>
        public Configuration_BillsConfiguration GUI_Get_Configuration_BillsConfiguration()
        {
            Configuration_BillsConfiguration result = new Configuration_BillsConfiguration();

            foreach (Channel c in EGM.GetInstance().Menu_GetBillDenominations())
            {
                result.channels.Add(c.channelNum, c);
            }
            return result;


        }

        /// <summary>
        /// {{ SET }}
        /// Bills Configuration
        /// </summary>
        /// <param name="configuration"></param>
        public void GUI_Set_Configuration_BillsConfiguration(Configuration_BillsConfiguration configuration)
        {
            EGM.GetInstance().Menu_SetBillDenomination(configuration.channels.Values.ToList());

        }

        #endregion

        /* CashIn CashOut Configuration */
        #region CashIn CashOut Configuration

        /// <summary>
        /// {{ GET }}
        /// Cash In Cash Out Configuration
        /// </summary>
        /// <returns></returns>
        public Configuration_CashInCashOutConfiguration GUI_Get_Configuration_CashInCashOutConfiguration()
        {
            Configuration_CashInCashOutConfiguration result = new Configuration_CashInCashOutConfiguration();

            result.AFTEnabled = EGM.GetInstance().EGM_GetEGMSettings().AFTEnabled;
            result.HandpayEnabled = EGM.GetInstance().EGM_GetEGMSettings().HandpayEnabled;
            result.BillAcceptor = EGM.GetInstance().EGM_GetEGMSettings().BillAcceptor;
            result.PartialPay = EGM.GetInstance().EGM_GetEGMSettings().PartialPay;
            result.CashOutOrder = EGM.GetInstance().EGM_GetEGMSettings().CashOutOrder;

            return result;
        }

        /// <summary>
        /// {{ SET }}
        /// Cash In Cash Out Configuration
        /// </summary>
        /// <returns></returns>
        public void GUI_Set_Configuration_CashInCashOutConfiguration(Configuration_CashInCashOutConfiguration input)
        {
            EGM.GetInstance().Menu_UpdateCashinCashoutConfiguration(input);

        }

        #endregion

        #endregion

        /******************/
        /*** STATISTICS ***/
        /******************/
        #region Statistics

        /// <summary>
        /// {{ SET }}
        /// Partial RamClear
        /// </summary>
        public void GUI_Set_RamClear_Partial()
        {
            EGM.GetInstance().Menu_RamClearPartial();
        }

        /// <summary>
        /// {{ SET }}
        /// Full RamClear
        /// </summary>
        public void GUI_Set_RamClear_Full()
        {
            EGM.GetInstance().Menu_RamClearFull();
        }

        /// <summary>
        /// {{ GET }}
        /// Get RamClear History
        /// </summary>
        public List<RamClearLog> GUI_Get_RamClearHistory()
        {
            return EGM.GetInstance().Menu_GetRamClearHistory();
        }

        /* Last Plays */
        #region Last Plays

        /// <summary>
        /// {{ GET }}
        /// Get the Last Plays history
        /// </summary>
        /// <returns></returns>
        public Statistics_LastPlays GUI_Get_Statistics_LastPlays()
        {
            Statistics_LastPlays result = new Statistics_LastPlays();

            foreach (LastPlay play in EGM.GetInstance().Menu_GetLastPlays())
            {
                result.lastPlays.Add(play);
            }
            return result;
        }


        LastPlay lastplay = new LastPlay();
        public bool GUI_On_View_Statistic_LastPlay()
        {
            return EGM.GetInstance().On_View_Statistic_LastPlay();
        }

        public LastPlay GUI_Get_View_Statistic_LastPlay()
        {
            return lastplay;
        }

        public void GUI_Enter_View_Statistic_LastPlay(LastPlay _lastplay)
        {
            lastplay = _lastplay;
            EGM.GetInstance().Enter_View_Statistic_LastPlay();
        }

        public void GUI_Exit_View_Statistic_LastPlay()
        {
            EGM.GetInstance().Exit_View_Statistic_LastPlay();
        }

        #endregion

        /* General Game Statistics */
        #region General Game Statistics

        /// <summary>
        /// private method. Auxiliary function to convert a integer value, in SASFormat, to a decimal value using the SASReportedDenominaiton. (multiplication
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private decimal FromSASFormat(int value)
        {
            return (decimal)((decimal)value * (decimal)EGM.GetInstance().Menu_GetSasReportedDenomination());
        }

        /// <summary>
        /// {{ GET }}
        /// General game statistics
        /// </summary>
        /// <returns></returns>
        public Statistics_GeneralGameStatistics GUI_Get_Statistics_GeneralGameStatistics()
        {
            // AFTIN AFTOUT
            int aftin = 0;
            int aftout = 0;
            foreach (var aft in EGM.GetInstance().Menu_GetAFTTransactionHistory())
            {
                if (aft.DebitCredit == "Credit")
                {
                    aftin += aft.CashableAmount + aft.RestrictedAmount + aft.NonRestrictedAmount;
                }
                else if (aft.DebitCredit == "Debit")
                {
                    aftout += aft.CashableAmount + aft.RestrictedAmount + aft.NonRestrictedAmount;
                }
            }
            aftout *= -1;

            // BILLS IN
            int billsin = 0;
            for (byte c = 0x40; c <= 0x57; c++)
            {
                billsin += EGM.GetInstance().EGM_GetMeter(c);
            }

            // COIN IN
            int coinin = EGM.GetInstance().EGM_GetMeter(0x00);

            // COIN OUT
            int coinout = EGM.GetInstance().EGM_GetMeter(0x01);

            Statistics_GeneralGameStatistics result = new Statistics_GeneralGameStatistics();



            result.AFTIn = new GameGeneralStatisticDetail(FromSASFormat(aftin), aftin); // AFT In
            result.AFTOut = new GameGeneralStatisticDetail(FromSASFormat(aftout), aftout); // AFTOut         
            result.BillsIn = new GameGeneralStatisticDetail(0, billsin); // BillsIn
            result.TotalCreditsFromBillAccepted = new GameGeneralStatisticDetail(FromSASFormat(EGM.GetInstance().EGM_GetMeter(0x0B)), 0);
            result.TotalCancelledCredits = new GameGeneralStatisticDetail(FromSASFormat(EGM.GetInstance().EGM_GetMeter(0x04)), 0); // Total Cancelled Credits
            result.TotalHandpayCancelledCredits = new GameGeneralStatisticDetail(FromSASFormat(EGM.GetInstance().EGM_GetMeter(0x03)), 0); // Total Cancelled Credits
            result.BillsInStacker = new GameGeneralStatisticDetail(0, EGM.GetInstance().EGM_GetMeter(0x3E)); // BillsInStacker
            result.CoinIn = new GameGeneralStatisticDetail(FromSASFormat(coinin), coinin); // Coin In
            result.CoinOut = new GameGeneralStatisticDetail(FromSASFormat(coinout), coinout); // Coin Out
            result.Credits = new GameGeneralStatisticDetail(EGM.GetInstance().EGM_GetCurrentCredits(), EGM.GetInstance().EGM_GetMeter(0x0C)); // Credits
            result.Handpay = new GameGeneralStatisticDetail(0, EGMAccounting.GetInstance().handpays.GetHandpayHistory().Count()); // Handpay
            result.SinceDoorClosed = new GameGeneralStatisticDetail(0, EGM.GetInstance().EGM_GetMeter(0x26)); // Since Door Closed
            result.SincePowerCycle = new GameGeneralStatisticDetail(0, EGM.GetInstance().EGM_GetMeter(0x25)); // Since Power Cycle
            if (coinout != 0)
                result.ActualPayback = new GameGeneralStatisticDetail((decimal)((decimal)coinout / (decimal)coinin), 0); // TheoreticalPayback
            else
                result.ActualPayback = new GameGeneralStatisticDetail((decimal)0, 0); // ActualPayback   
            result.TheoreticalPayback = new GameGeneralStatisticDetail((decimal)EGM.GetInstance().Menu_GetTheoreticalPayback(), 0); // TheoreticalPayback
            result.TotalDrop = new GameGeneralStatisticDetail(FromSASFormat(EGM.GetInstance().EGM_GetMeter(0x24)), EGM.GetInstance().EGM_GetMeter(0x24)); // Total Drop
            result.TotalGames = new GameGeneralStatisticDetail(0, EGM.GetInstance().EGM_GetMeter(0x05)); // Total Games
            result.TotalGamesWon = new GameGeneralStatisticDetail(0, EGM.GetInstance().EGM_GetMeter(0x06)); // Total Games Won
            result.TotalGamesLost = new GameGeneralStatisticDetail(0, EGM.GetInstance().EGM_GetMeter(0x07)); // Total Games Lost

            return result;
        }

        /// <summary>
        /// {{ GET }}
        /// Get specific game statistics
        /// </summary>
        /// <param name="gameId"></param>
        /// <returns></returns>
        public Statistics_WarningAndErrors GUI_Get_Statistics_WarningAndErrors()
        {
            Statistics_WarningAndErrors result = new Statistics_WarningAndErrors();
            result.statistic = EGM.GetInstance().Menu_GetWarningAndErrors();
            return result;

        }

        /// <summary>
        /// {{ GET }}
        /// Get specific game statistics
        /// </summary>
        /// <param name="gameId"></param>
        /// <returns></returns>
        public Statistics_Game GUI_Get_Statistics_GameStatistics(int gameId)
        {
            if (gameId == 1)
            {
                Statistics_Game result = new Statistics_Game();
                int r = RandomNumberGenerator.GetInt32(0, 200);
                result.statistics.Add(new GameStatistic((decimal)0.1, (decimal)(r * 0.1), r, 0, 0, RandomNumberGenerator.GetInt32(0, 200), RandomNumberGenerator.GetInt32(0, 200), (decimal)(RandomNumberGenerator.GetInt32(0, 200) * 0.2), (decimal)(RandomNumberGenerator.GetInt32(0, 200) * 0.01)));
                r = RandomNumberGenerator.GetInt32(0, 200);
                result.statistics.Add(new GameStatistic((decimal)0.1, (decimal)(r * 0.1), r, 0, 0, RandomNumberGenerator.GetInt32(0, 200), RandomNumberGenerator.GetInt32(0, 200), (decimal)(RandomNumberGenerator.GetInt32(0, 200) * 0.2), (decimal)(RandomNumberGenerator.GetInt32(0, 200) * 0.01)));
                r = RandomNumberGenerator.GetInt32(0, 200);
                result.statistics.Add(new GameStatistic((decimal)0.1, (decimal)(r * 0.1), r, 0, 0, RandomNumberGenerator.GetInt32(0, 200), RandomNumberGenerator.GetInt32(0, 200), (decimal)(RandomNumberGenerator.GetInt32(0, 200) * 0.2), (decimal)(RandomNumberGenerator.GetInt32(0, 200) * 0.01)));
                r = RandomNumberGenerator.GetInt32(0, 200);
                result.statistics.Add(new GameStatistic((decimal)0.1, (decimal)(r * 0.1), r, 0, 0, RandomNumberGenerator.GetInt32(0, 200), RandomNumberGenerator.GetInt32(0, 200), (decimal)(RandomNumberGenerator.GetInt32(0, 200) * 0.2), (decimal)(RandomNumberGenerator.GetInt32(0, 200) * 0.01)));
                r = RandomNumberGenerator.GetInt32(0, 200);
                result.statistics.Add(new GameStatistic((decimal)0.1, (decimal)(r * 0.1), r, 0, 0, RandomNumberGenerator.GetInt32(0, 200), RandomNumberGenerator.GetInt32(0, 200), (decimal)(RandomNumberGenerator.GetInt32(0, 200) * 0.2), (decimal)(RandomNumberGenerator.GetInt32(0, 200) * 0.01)));
                r = RandomNumberGenerator.GetInt32(0, 200);
                result.statistics.Add(new GameStatistic((decimal)0.1, (decimal)(r * 0.1), r, 0, 0, RandomNumberGenerator.GetInt32(0, 200), RandomNumberGenerator.GetInt32(0, 200), (decimal)(RandomNumberGenerator.GetInt32(0, 200) * 0.2), (decimal)(RandomNumberGenerator.GetInt32(0, 200) * 0.01)));
                r = RandomNumberGenerator.GetInt32(0, 200);
                result.statistics.Add(new GameStatistic((decimal)0.1, (decimal)(r * 0.1), r, 0, 0, RandomNumberGenerator.GetInt32(0, 200), RandomNumberGenerator.GetInt32(0, 200), (decimal)(RandomNumberGenerator.GetInt32(0, 200) * 0.2), (decimal)(RandomNumberGenerator.GetInt32(0, 200) * 0.01)));

                return result;

            }
            else
            {
                return new Statistics_Game();
            }
        }


        #endregion

        /* System Logs*/
        #region System Logs

        /// <summary>
        /// {{ GET }}
        /// Get the system logs statistic
        /// </summary>
        /// <returns></returns>
        public Statistics_SystemLogs GUI_Get_Statistics_SystemLogs()
        {
            Statistics_SystemLogs result = new Statistics_SystemLogs();
            foreach (SystemLog log in EGM.GetInstance().Menu_GetSystemLogs())
            {
                result.AddLog(log.Date, log.EventDetail, log.User);
            }


            return result;
        }

        #endregion

        /* Advanced Funds Transfer (AFT) */
        #region AFT

        /// <summary>
        /// {{ GET }}
        /// Get the AFT Transaction History
        /// </summary>
        /// <returns></returns>
        public List<AccountingAFTTransfer> GUI_Get_Statistics_AFTTransactionHistory()
        {
            return EGM.GetInstance().Menu_GetAFTTransactionHistory();
        }

        #endregion

        /* Handpays */
        #region Handpays

        /// <summary>
        /// {{ GET }}
        /// Get the Handpay Transaction history
        /// </summary>
        /// <returns></returns>
        public List<HandpayTransaction> GUI_Get_Statistics_HandpayTransactionHistory()
        {
            return EGM.GetInstance().Menu_GetHandpayHistory();
        }
        #endregion

        /*** Last Bills ***/
        #region LastBills
        public Statistics_LastBills GUI_Get_Statistics_LastBills()
        {
            Statistics_LastBills result = new Statistics_LastBills();
            foreach (LastBill bill in EGM.GetInstance().Menu_GetLastBills())
            {
                result.lastbills.Add(bill);
            }
            return result;
        }

        #endregion

        #endregion

        /*******************/
        /*** DIAGNOSTICS ***/
        /*******************/
        #region Diagnostics

        /* Input Output Tester */
        #region InputOutputTester

        /// <summary>
        /// {{ SET }}
        /// All Outputs Off
        /// </summary>
        public void GUI_Set_Diagnostics_OutputTester_AllOutputsOff()
        {
            EGM.GetInstance().GPIO_SetAllOutputsOff();
        }

        /// <summary>
        /// {{ SET }}
        /// Set Output Status
        /// </summary>
        /// <param name="output"></param>
        /// <param name="on"></param>
        public void GUI_Set_Diagnostics_OutputTester(OutputName output, bool on)
        {
            EGM.GetInstance().GPIO_SetOutputStatus(output, on);
        }

        /// <summary>
        /// {{ GET }}
        /// Diagnostics Get Bill Validator Port
        /// </summary>
        /// <param name="configuration"></param>
        public string GUI_Get_Diagnostics_BillValidator_Port()
        {
            return EGM.GetInstance().EGM_GetEGMSettings().BillAcceptorComPort;

        }
        /*** Last Bills ***/
        #region BillsByDenomination
        public Statistics_BillsByDenomination GUI_Get_Statistics_BillsByDenomination()
        {
            Statistics_BillsByDenomination result = new Statistics_BillsByDenomination();
            result = EGM.GetInstance().Menu_BillsByDenomination();
            return result;
        }

        #endregion

        /// <summary>
        /// {{ GET }}
        /// Input Output Tester
        /// </summary>
        /// <returns></returns>
        public Diagnostics_InputOutputTester GUI_Get_Diagnostics_InputOutputTester()
        {
            Diagnostics_InputOutputTester result = new Diagnostics_InputOutputTester();
            result.inputs = new Inputs(EGM.GetInstance().GPIO_GetButtonStatus(ButtonName.B_SPIN),
                                       EGM.GetInstance().GPIO_GetButtonStatus(ButtonName.B_AUTOSPIN),
                                       EGM.GetInstance().GPIO_GetButtonStatus(ButtonName.B_CASHOUT),
                                       EGM.GetInstance().GPIO_GetButtonStatus(ButtonName.B_HELP),
                                       EGM.GetInstance().GPIO_GetButtonStatus(ButtonName.B_LINES),
                                       EGM.GetInstance().GPIO_GetButtonStatus(ButtonName.B_SERVICE),
                                       EGM.GetInstance().GPIO_GetButtonStatus(ButtonName.B_BET),
                                       EGM.GetInstance().GPIO_GetButtonStatus(ButtonName.B_MAXBET),
                                       EGM.GetInstance().GPIO_GetButtonStatus(ButtonName.B_KEY),
                                       EGM.GetInstance().GPIO_GetSensorStatus(SensorName.D_MAINDOOR),
                                       EGM.GetInstance().GPIO_GetSensorStatus(SensorName.D_BELLYDOOR),
                                       EGM.GetInstance().GPIO_GetSensorStatus(SensorName.D_DROPBOXDOOR),
                                       EGM.GetInstance().GPIO_GetSensorStatus(SensorName.D_LOGICDOOR),
                                       false,
                                       false,
                                       EGM.GetInstance().GPIO_GetSensorStatus(SensorName.D_CASHBOXDOOR));
            result.outputs = new Outputs(EGM.GetInstance().GPIO_GetOutputStatus(OutputName.O_SPINBUTTONLIGHT),
                                         EGM.GetInstance().GPIO_GetOutputStatus(OutputName.O_AUTOSPINBUTTONLIGHT),
                                         EGM.GetInstance().GPIO_GetOutputStatus(OutputName.O_CASHOUTBUTTONLIGHT),
                                         EGM.GetInstance().GPIO_GetOutputStatus(OutputName.O_HELPBUTTONLIGHT),
                                         EGM.GetInstance().GPIO_GetOutputStatus(OutputName.O_LINESBUTTONLIGHT),
                                         EGM.GetInstance().GPIO_GetOutputStatus(OutputName.O_SERVICEBUTTONLIGHT),
                                         EGM.GetInstance().GPIO_GetOutputStatus(OutputName.O_BETBUTTONLIGHT),
                                         EGM.GetInstance().GPIO_GetOutputStatus(OutputName.O_MAXBETBUTTONLIGHT),
                                         EGM.GetInstance().GPIO_GetOutputStatus(OutputName.O_TOWER2LIGHT),
                                         EGM.GetInstance().GPIO_GetOutputStatus(OutputName.O_TOWER1LIGHT),
                                         false);
            return result;
        }


        #endregion

        /* Bill Validation Test*/
        #region BillValidationTest

        /// <summary>
        /// {{ GET }}
        /// Get Bill Validator Logs for diagnostics.
        /// </summary>
        /// <returns></returns>
        public List<BillValidatorLog> GUI_Get_Diagnostics_BillValidatorLogs()
        {
            return EGM.GetInstance().Menu_GetBillValidatorLogs();
        }

        /// <summary>
        /// {{ GET }}
        /// Get Bill Validator CRC
        /// </summary>
        /// <returns></returns>
        public String GUI_Get_Diagnostics_BillValidator_CRC()
        {
            return "TBD";
        }

        #endregion

        #endregion



        /*******************/
        /*** MENU ********/
        /*******************/
        #region Menu Navigation

        public MenuSceneNameAction GUI_Menu_Validate_MenuAccess(MenuSceneName name)
        {
            return MenuSecurityAccess[GUI_Security_Get_LoggedUser().GetHashCode(), name.GetHashCode(), GUI_Get_Diagnostics_InputOutputTester().inputs.MainDoorOpen ? 1 : 0, GUI_Get_Diagnostics_InputOutputTester().inputs.LogicDoorOpen ? 1 : 0, GUI_GetCurrentCredits() > 0 ? 1 : 0, GUI_Get_Statistics_GeneralGameStatistics().TotalDrop.amount == 0 && GUI_Get_Statistics_GeneralGameStatistics().TotalDrop.quantities == 0 ? 1 : 0, GUI_Get_Configuration_CashInCashOutConfiguration().BillAcceptor ? 1 : 0];
        }

        public string GUI_Menu_Get_MenuInfoLabel()
        {
            return EGM.GetInstance().Menu_Get_MenuInfoLabel();
        }

        #endregion


    }
}
