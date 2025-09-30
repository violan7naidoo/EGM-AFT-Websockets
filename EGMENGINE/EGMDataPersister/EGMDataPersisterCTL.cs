using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using EGMENGINE.GLOBALTYPES;
using EGMENGINE.EGMSettingsModule;
using EGMENGINE.EGMStatusModule;
using EGMENGINE.EGMAccountingModule;
using Mono.Data.Sqlite;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Globalization;
using EGMENGINE.EGMStatusModule.HandPayModule;
using EGMENGINE.EGMStatusModule.CollectModule;
using EGMENGINE.EGMStatusModule.JackPotModule;
using System.Data.SqlTypes;
using EGMENGINE.GUI.GAMETYPES;
using EGMENGINE.EGMSettingsModule.EGMSASConfig;
using SlotMathCore.Model;
using EGMENGINE.EGMPlayModule;
using System.Runtime.InteropServices;
using EGMENGINE.IntegrityControlModule;
using System.Net.NetworkInformation;
using System.IO.Hashing;

namespace EGMENGINE.EGMDataPersisterModule
{
    internal class EGMDataPersisterCTL
    {
        private static EGMDataPersisterCTL egmdataPersister_;
        private int lastbillMax = 30;
        private int lastAFTTransferMax = 128;
        private string datalocation;
        private bool databaseon;
        private static EGMAccountingMeters localaccountingmeter;
        private string dbchecksum = "17-FC-70-F6";


        // Init EGMDataPersisterCTL
        protected EGMDataPersisterCTL()
        {

        }

        string CalculateChecksum(string input)
        {
            // Use input string to calculate MD5 hash

            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            string result = BitConverter.ToString(Crc32.Hash(inputBytes));

            return result;
        }
        internal string InitEGMDataPersister(bool unitydevelopment, bool localganlot)
        {
            if (!unitydevelopment)
            {
                if (localganlot)
                    datalocation = "C:/Gamedata\\EGMEngineDB.db";
                else
                    datalocation = "D:/Gamedata\\EGMEngineDB.db";

            }
            else
            {
                datalocation = "H:/Gamedata\\EGMEngineDB.db";
            }
            databaseon = true;
            if (!File.Exists(datalocation))
            {
                IntegrityController.GetInstance().RAMERROR = true;
                databaseon = false;
                return "RAM ERROR - Missing";
            }
            string checksum = "";
            using (var connection = new SqliteConnection($"Data Source={datalocation}"))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT sql FROM sqlite_master WHERE type IN ('table', 'index', 'view', 'trigger')";

                    using (var command = new SqliteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        string schema = "";
                        while (reader.Read())
                        {
                            try
                            {
                                schema += reader.GetString(0) + "\n";  // Concatenar todo el esquema
                            }
                            catch
                            {

                            }
                        }


                        // Calcular y mostrar el checksum
                        checksum = CalculateChecksum(schema);
                    }
                }
                catch (Exception ex)
                {
                    return ex.Message;


                }
                finally
                {
                    connection.Close();
                }
            }
            if (checksum != dbchecksum)
            {
                IntegrityController.GetInstance().RAMERROR = true;
                return "DATABASE MISMATCH";
            }
            // Truncate Last Bills and AFTTransfers. Based on lastbillMax and lastAFTTransferMax
            using (var connection = new SqliteConnection($"Data Source={datalocation}"))
            {
                try
                {
                    connection.Open();

                    // Configurar PRAGMA synchronous para garantizar la escritura física en disco
                    using (var pragmaCommand = connection.CreateCommand())
                    {
                        pragmaCommand.CommandText = "PRAGMA synchronous = EXTRA;";
                        pragmaCommand.ExecuteNonQuery();
                    }

                    #region TruncateLastBills

                    using (SqliteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"SELECT MAX(Id) as MaxId from EGMAccountingLastBills;";

                        using (var reader = command.ExecuteReader())
                        {
                            Dictionary<string, int> columns = new Dictionary<string, int>();

                            for (var i = 0; i < reader.FieldCount; i++)
                            {
                                columns.Add(reader.GetName(i), i);
                            }

                            while (reader.Read())
                            {
                                try
                                {
                                    var MaxId = reader.GetInt32(columns["MaxId"]);
                                    using (SqliteCommand command1 = connection.CreateCommand())
                                    {
                                        command1.CommandText = @"DELETE FROM EGMAccountingLastBills WHERE Id <= $Id;";

                                        command1.Parameters.AddWithValue("$Id", MaxId - lastbillMax);

                                        command1.ExecuteReader();
                                    }
                                }
                                catch
                                {

                                }
                            }
                        }
                    }

                    #endregion

                    #region TrucateAFTTransfers

                    using (SqliteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"SELECT MAX(Id) as MaxId from EGMAccountingAFTTransfers;";

                        using (var reader = command.ExecuteReader())
                        {
                            Dictionary<string, int> columns = new Dictionary<string, int>();

                            for (var i = 0; i < reader.FieldCount; i++)
                            {
                                columns.Add(reader.GetName(i), i);
                            }

                            while (reader.Read())
                            {
                                try
                                {
                                    var MaxId = reader.GetInt32(columns["MaxId"]);
                                    using (SqliteCommand command1 = connection.CreateCommand())
                                    {
                                        command1.CommandText = @"DELETE FROM EGMAccountingAFTTransfers WHERE Id <= $Id;";

                                        command1.Parameters.AddWithValue("$Id", MaxId - lastAFTTransferMax);

                                        command1.ExecuteReader();
                                    }
                                }
                                catch
                                {

                                }
                            }
                        }
                    }

                    #endregion

                }
                catch
                {

                }
                finally
                {
                    connection.Close();
                }
            }

            return "OK";
        }

        #region "EGM STATUS"

        /// <summary>
        /// Persist EGM Slot Play. 
        /// </summary>
        internal void PersistEGMSlotPlay()
        {
            if (IntegrityController.GetInstance().RAMERROR)
                return;
            using (var connection = new SqliteConnection($"Data Source={datalocation}"))
            {
                try
                {

                    // Create the command with the placeholders
                    connection.Open();



                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        //   File.AppendAllLines("Log.txt", new string[] { DateTime.Now.ToString(), "ERROR" });
                        return;
                    }

                    // Configurar PRAGMA synchronous para garantizar la escritura física en disco
                    //using (var pragmaCommand = connection.CreateCommand())
                    //{
                    //    pragmaCommand.CommandText = "PRAGMA journal_mode = WAL; PRAGMA synchronous =  EXTRA;";
                    //    pragmaCommand.ExecuteNonQuery();
                    //}

                    using (var command = connection.CreateCommand())
                    {

                        // Create the command with the placeholders
                        command.CommandText = @"UPDATE EGMCurrentSlotPlay
                                                   SET MeterCHK = $MeterCHK,
                                                       ScatterWin = $ScatterWin,
                                                       ExpandedWin = $ExpandedWin,
                                                       BaseWin = $BaseWin,
                                                       MisteryWin = $MisteryWin,
                                                       ActionGameWin = $ActionGameWin,
                                                       Reel1Disposition = $Reel1Disposition,
                                                       Reel2Disposition = $Reel2Disposition,
                                                       Reel3Disposition = $Reel3Disposition,
                                                       Reel4Disposition = $Reel4Disposition,
                                                       Reel5Disposition = $Reel5Disposition,
                                                       ScatterReelPositions = $ScatterReelPositions,
                                                       PhysicalReelStops = $PhysicalReelStops,
                                                       PayLines = $PayLines,
                                                       ExpandedPayLines = $ExpandedPayLines,
                                                       ExpandedSymbol_Id = $ExpandedSymbol_Id,
                                                       ExpandedSymbol_Name = $ExpandedSymbol_Name,
                                                       ExpandedSymbol_Category = $ExpandedSymbol_Category,
                                                       ExpandedSymbol_Abbreviation = $ExpandedSymbol_Abbreviation,
                                                       ExpandedSymbol_IsWild = $ExpandedSymbol_IsWild,
                                                       ExpandedSymbol_IsScatter = $ExpandedSymbol_IsScatter,
                                                       WinningLines = $WinningLines,
                                                       ExpandedWinningLines = $ExpandedWinningLines,
                                                       RemainingPennyGames = $RemainingPennyGames,
                                                       AwardedPennyGames = $AwardedPennyGames,
                                                       ActionGameSegment = $ActionGameSegment,
                                                       RemainingActionGames = $RemainingActionGames,
                                                       CreditsBefore = $CreditsBefore,
                                                       CreditsAfter = $CreditsAfter,
                                                       CreditsAfterBaseWin = $CreditsAfterBaseWin,
                                                       CreditsAfterScatterWin = $CreditsAfterScatterWin,
                                                       CreditsAfterExpandedWin = $CreditsAfterExpandedWin,
                                                       CreditsAfterMisteryWin = $CreditsAfterMisteryWin,
                                                       CreditsAfterActionGameWin = $CreditsAfterActionGameWin,
                                                       BetCredits = $BetCredits,
                                                       CreditsOnPlay = $CreditsOnPlay,
                                                       WinCredits = $WinCredits,
                                                       ExceededCredits = $ExceededCredits,
                                                       Bonus = $Bonus,
                                                       CreditValue = $CreditValue,
                                                       Finished = $Finished,
                                                       TSPlayStart = $TSPlayStart,
                                                       TSPlayEnd = $TSPlayEnd,
                                                       Status = $Status                                                
                                                WHERE ID = 0;";

                        // Make SQL Parameters with some info of EGMStatus

                        command.Parameters.AddWithValue("$MeterCHK", IntegrityController.GetInstance().GetMeterCHK_EGMSlotPlay(EGMStatus.GetInstance().current_play));
                        command.Parameters.AddWithValue("$ScatterWin", EGMStatus.GetInstance().current_play.spin.slotplay.scatterWin);
                        command.Parameters.AddWithValue("$ExpandedWin", EGMStatus.GetInstance().current_play.spin.slotplay.expandedWin);
                        command.Parameters.AddWithValue("$BaseWin", EGMStatus.GetInstance().current_play.spin.slotplay.baseWin);
                        command.Parameters.AddWithValue("$MisteryWin", EGMStatus.GetInstance().current_play.spin.slotplay.misteryWin);
                        command.Parameters.AddWithValue("$ActionGameWin", EGMStatus.GetInstance().current_play.spin.slotplay.actionGameWin);
                        command.Parameters.AddWithValue("$Reel1Disposition", JsonConvert.SerializeObject(EGMStatus.GetInstance().current_play.spin?.slotplay.reel1Disposition));
                        command.Parameters.AddWithValue("$Reel2Disposition", JsonConvert.SerializeObject(EGMStatus.GetInstance().current_play.spin?.slotplay.reel2Disposition));
                        command.Parameters.AddWithValue("$Reel3Disposition", JsonConvert.SerializeObject(EGMStatus.GetInstance().current_play.spin?.slotplay.reel3Disposition));
                        command.Parameters.AddWithValue("$Reel4Disposition", JsonConvert.SerializeObject(EGMStatus.GetInstance().current_play.spin?.slotplay.reel4Disposition));
                        command.Parameters.AddWithValue("$Reel5Disposition", JsonConvert.SerializeObject(EGMStatus.GetInstance().current_play.spin?.slotplay.reel5Disposition));
                        command.Parameters.AddWithValue("$ScatterReelPositions", JsonConvert.SerializeObject(EGMStatus.GetInstance().current_play.spin?.slotplay.scatterReelPositions));
                        command.Parameters.AddWithValue("$PhysicalReelStops", JsonConvert.SerializeObject(EGMStatus.GetInstance().current_play.spin?.slotplay.physicalReelStops));
                        command.Parameters.AddWithValue("$PayLines", JsonConvert.SerializeObject(EGMStatus.GetInstance().current_play.spin?.slotplay.payLines));
                        command.Parameters.AddWithValue("$ExpandedPayLines", JsonConvert.SerializeObject(EGMStatus.GetInstance().current_play.spin?.slotplay.expandedPayLines));
                        command.Parameters.AddWithValue("$ExpandedSymbol_Id", EGMStatus.GetInstance().current_play.spin?.slotplay.expandedSymbol?.Id ?? "");
                        command.Parameters.AddWithValue("$ExpandedSymbol_Name", EGMStatus.GetInstance().current_play.spin?.slotplay.expandedSymbol?.Name ?? "");
                        command.Parameters.AddWithValue("$ExpandedSymbol_Category", EGMStatus.GetInstance().current_play.spin?.slotplay.expandedSymbol?.Category ?? "");
                        command.Parameters.AddWithValue("$ExpandedSymbol_Abbreviation", EGMStatus.GetInstance().current_play.spin?.slotplay.expandedSymbol?.Abbreviation ?? "");
                        command.Parameters.AddWithValue("$ExpandedSymbol_IsWild", EGMStatus.GetInstance().current_play.spin?.slotplay.expandedSymbol?.IsWild ?? false);
                        command.Parameters.AddWithValue("$ExpandedSymbol_IsScatter", EGMStatus.GetInstance().current_play.spin?.slotplay.expandedSymbol?.IsScatter ?? false);
                        command.Parameters.AddWithValue("$WinningLines", JsonConvert.SerializeObject(EGMStatus.GetInstance().current_play.spin?.slotplay.winningLines));
                        command.Parameters.AddWithValue("$ExpandedWinningLines", JsonConvert.SerializeObject(EGMStatus.GetInstance().current_play.spin?.slotplay.expandedWinningLines));
                        command.Parameters.AddWithValue("$RemainingPennyGames", EGMStatus.GetInstance().current_play.spin?.slotplay.remainingPennyGames);
                        command.Parameters.AddWithValue("$AwardedPennyGames", EGMStatus.GetInstance().current_play.spin?.slotplay.awardedPennyGames);
                        command.Parameters.AddWithValue("$ActionGameSegment", EGMStatus.GetInstance().current_play.spin?.slotplay.actiongame_segment);
                        command.Parameters.AddWithValue("$RemainingActionGames", EGMStatus.GetInstance().current_play.spin?.slotplay.remainingActionGames);
                        command.Parameters.AddWithValue("$CreditsBefore", EGMStatus.GetInstance().current_play.spin?.slotplay.creditsBefore);
                        command.Parameters.AddWithValue("$CreditsAfter", EGMStatus.GetInstance().current_play.spin?.slotplay.creditsAfter);
                        command.Parameters.AddWithValue("$CreditsAfterBaseWin", EGMStatus.GetInstance().current_play.spin?.slotplay.creditsAfterBaseWin);
                        command.Parameters.AddWithValue("$CreditsAfterScatterWin", EGMStatus.GetInstance().current_play.spin?.slotplay.creditsAfterScatterWin);
                        command.Parameters.AddWithValue("$CreditsAfterExpandedWin", EGMStatus.GetInstance().current_play.spin?.slotplay.creditsAfterExpandedWin);
                        command.Parameters.AddWithValue("$CreditsAfterMisteryWin", EGMStatus.GetInstance().current_play.spin?.slotplay.creditsAfterMisteryWin);
                        command.Parameters.AddWithValue("$CreditsAfterActionGameWin", EGMStatus.GetInstance().current_play.spin?.slotplay.creditsAfterActionGameWin);
                        command.Parameters.AddWithValue("$BetCredits", EGMStatus.GetInstance().current_play.spin?.slotplay.totalBetAmount);
                        command.Parameters.AddWithValue("$CreditsOnPlay", EGMStatus.GetInstance().current_play.spin?.slotplay.creditsOnPlay);
                        command.Parameters.AddWithValue("$WinCredits", EGMStatus.GetInstance().current_play.spin?.slotplay.winCredits);
                        command.Parameters.AddWithValue("$ExceededCredits", EGMStatus.GetInstance().current_play.spin?.slotplay.exceededCredits);
                        command.Parameters.AddWithValue("$Bonus", EGMStatus.GetInstance().current_play.spin?.slotplay.bonus);
                        command.Parameters.AddWithValue("$CreditValue", EGMStatus.GetInstance().current_play.spin?.slotplay.creditValue);
                        command.Parameters.AddWithValue("$Finished", EGMStatus.GetInstance().current_play.spin?.slotplay.Finished);
                        command.Parameters.AddWithValue("$TSPlayStart", EGMStatus.GetInstance().current_play.spin?.slotplay.TSPlayStart);
                        command.Parameters.AddWithValue("$TSPlayEnd", EGMStatus.GetInstance().current_play.spin?.slotplay.TSPlayEnd);
                        command.Parameters.AddWithValue("$Status", EGMStatus.GetInstance().current_play.spin?.slotplay.status);


                        // Execute

                        command.ExecuteNonQuery();


                    }


                }
                catch (Exception ex)
                {
                    //   File.AppendAllLines("Log.txt", new string[] { DateTime.Now.ToString(), ex.Message });
                }
                finally
                {
                    connection.Close();
                }



                // Execute
            }
        }



        /// <summary>
        /// Persist EGM Status. 
        /// </summary>
        internal void PersistEGMStatus(bool forcepersist)
        {
            if (IntegrityController.GetInstance().RAMERROR)
                return;

            if (EGMStatus_CompareCurrentWithSnapshot() || forcepersist)
            {
                using (var connection = new SqliteConnection($"Data Source={datalocation}"))
                {
                    try
                    {

                        // Create the command with the placeholders
                        connection.Open();

                        if (connection.State != System.Data.ConnectionState.Open)
                        {
                            // File.AppendAllLines("Log.txt", new string[] { DateTime.Now.ToString(), "ERROR" });
                            return;
                        }

                        // Configurar PRAGMA synchronous para garantizar la escritura física en disco
                        //using (var pragmaCommand = connection.CreateCommand())
                        //{
                        //    pragmaCommand.CommandText = "PRAGMA journal_mode = WAL; PRAGMA synchronous =  EXTRA;";
                        //    pragmaCommand.ExecuteNonQuery();
                        //}

                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = @"UPDATE EGMStatus
                                           SET MeterCHK = $MeterCHK,
                                               CurrentCredits = $CurrentCredits,
                                               CurrentRestrictedCredits = $CurrentRestrictedCredits,
                                               CurrentNonRestrictedCredits = $CurrentNonRestrictedCredits,
                                               CurrentCashableCredits = $CurrentCashableCredits,
                                               CurrentBet = $CurrentBet,
                                               MenuActive = $MenuActive,
                                               DisabledByHost = $DisabledByHost,
                                               SoundEnabled = $SoundEnabled,
                                               BetLines = $BetLines,
                                               BetxLine = $BetxLine,
                                               HandpayStatus = $HandpayStatus,
                                               HandpayAmount = $HandpayAmount,
                                               HandpayTriggerTS = $HandpayTriggerTS,
                                               HandpayResetTS = $HandpayResetTS,
                                               HandpayResetMode = $HandpayResetMode,
                                               CollectStatus = $CollectStatus,
                                               MaintenanceMode = $MaintenanceMode,
                                               JackpotAmount = $JackpotAmount,
                                               JackpotStatus = $JackpotStatus,
                                               JackpotTriggerTS = $JackpotTriggerTS,
                                               JackpotResetTS = $JackpotResetTS,
                                               JackpotResetMode = $JackpotResetMode,
                                               Tower1Light = $Tower1Light,
                                               Tower2Light = $Tower2Light,
                                               SpinButtonLight = $SpinButtonLight,
                                               BetButtonLight = $BetButtonLight,
                                               HelpButtonLight = $HelpButtonLight,
                                               LinesButtonLight = $LinesButtonLight,
                                               CashoutButtonLight = $CashoutButtonLight,
                                               MaxbetButtonLight = $MaxbetButtonLight,
                                               ServiceButtonLight = $ServiceButtonLight,
                                               AutoSpinButtonLight = $AutoSpinButtonLight,
                                               MainDoorStatus = $MainDoorStatus,
                                               BellyDoorStatus = $BellyDoorStatus,
                                               CashboxDoorStatus = $CashboxDoorStatus,
                                               DropboxDoorStatus = $DropboxDoorStatus,
                                               CardcageDoorStatus = $CardcageDoorStatus,
                                               LogicDoorStatus = $LogicDoorStatus,
                                               SetDateTime = $SetDateTime,
                                               FullRamClearPerformed = $FullRamClearPerformed,
                                               CurrentAnimationEvent = $CurrentAnimationEvent,
                                               Status = $Status,
                                               PennyStatus = $PennyStatus,
                                               PayLine1Amount = $PayLine1Amount,
                                               PayLine2Amount = $PayLine2Amount,
                                               PayLine3Amount = $PayLine3Amount,
                                               PayLine4Amount = $PayLine4Amount,
                                               PayLine5Amount = $PayLine5Amount,
                                               CreditsWon = $CreditsWon,
                                               BaseCreditsWon = $BaseCreditsWon,
                                               ScatterCreditsWon = $ScatterCreditsWon,
                                               ExpandedCreditsWon = $ExpandedCreditsWon,
                                               MisteryCreditsWon = $MisteryCreditsWon,
                                               ActionGameCreditsWon = $ActionGameCreditsWon,
                                               ScatterWin = $ScatterWin,
                                               MisteryWin = $MisteryWin,
                                               ExpandedWin = $ExpandedWin,
                                               BaseWinning = $BaseWinning,
                                               PennyGameIndex = $PennyGameIndex,
                                               TotalCurrentPennyGames = $TotalCurrentPennyGames,
                                               TotalCurrentActionGames = $TotalCurrentActionGames,
                                               ActionGameIndex = $ActionGameIndex,
                                               ActionGameWinning = $ActionGameWinning,
                                               ExtraData = $ExtraData,
                                               SelectedCreditValue = $SelectedCreditValue
                                              WHERE ID = 0;";

                            // Make SQL Parameters with some info of EGMStatus
                            command.Parameters.AddWithValue("$MeterCHK", IntegrityController.GetInstance().GetMeterCHK_EGMStatus(EGMStatus.GetInstance()));
                            command.Parameters.AddWithValue("$CurrentCredits", EGMStatus.GetInstance().currentAmount);
                            command.Parameters.AddWithValue("$CurrentRestrictedCredits", EGMStatus.GetInstance().currentRestrictedAmount);
                            command.Parameters.AddWithValue("$CurrentNonRestrictedCredits", EGMStatus.GetInstance().currentNonRestrictedAmount);
                            command.Parameters.AddWithValue("$CurrentCashableCredits", EGMStatus.GetInstance().currentCashableAmount);
                            command.Parameters.AddWithValue("$CurrentBet", EGMStatus.GetInstance().currentBet);
                            command.Parameters.AddWithValue("$MenuActive", EGMStatus.GetInstance().menuActive == true ? 1 : 0);
                            command.Parameters.AddWithValue("$DisabledByHost", EGMStatus.GetInstance().disabledByHost == true ? 1 : 0);
                            command.Parameters.AddWithValue("$SoundEnabled", EGMStatus.GetInstance().soundEnabled == true ? 1 : 0);
                            command.Parameters.AddWithValue("$BetLines", EGMStatus.GetInstance().betLines);
                            command.Parameters.AddWithValue("$BetxLine", EGMStatus.GetInstance().betxline);
                            command.Parameters.AddWithValue("$HandpayStatus", EGMStatus.GetInstance().current_handpay.status.GetHashCode());
                            command.Parameters.AddWithValue("$HandpayAmount", EGMStatus.GetInstance().current_handpay.Amount);
                            command.Parameters.AddWithValue("$HandpayTriggerTS", ToDateTimeString(EGMStatus.GetInstance().current_handpay.TrigerTS));
                            command.Parameters.AddWithValue("$HandpayResetTS", ToDateTimeString(EGMStatus.GetInstance().current_handpay.ResetTS));
                            command.Parameters.AddWithValue("$HandpayResetMode", EGMStatus.GetInstance().current_handpay.ResetMode);
                            command.Parameters.AddWithValue("$JackpotStatus", EGMStatus.GetInstance().current_jackpot.status.GetHashCode());
                            command.Parameters.AddWithValue("$JackpotAmount", EGMStatus.GetInstance().current_jackpot.Amount);
                            command.Parameters.AddWithValue("$JackpotTriggerTS", ToDateTimeString(EGMStatus.GetInstance().current_jackpot.TrigerTS));
                            command.Parameters.AddWithValue("$JackpotResetTS", ToDateTimeString(EGMStatus.GetInstance().current_jackpot.ResetTS));
                            command.Parameters.AddWithValue("$JackpotResetMode", EGMStatus.GetInstance().current_jackpot.ResetMode);
                            command.Parameters.AddWithValue("$CollectStatus", EGMStatus.GetInstance().current_collect.status.GetHashCode());
                            command.Parameters.AddWithValue("$MaintenanceMode", EGMStatus.GetInstance().maintenanceMode == true ? 1 : 0);
                            command.Parameters.AddWithValue("$Tower1Light", EGMStatus.GetInstance().o_tower1lightStatus == true ? 1 : 0);
                            command.Parameters.AddWithValue("$Tower2Light", EGMStatus.GetInstance().o_tower2lightStatus == true ? 1 : 0);
                            command.Parameters.AddWithValue("$SpinButtonLight", EGMStatus.GetInstance().o_spinbuttonlightstatus == true ? 1 : 0);
                            command.Parameters.AddWithValue("$BetButtonLight", EGMStatus.GetInstance().o_betbuttonlightstatus == true ? 1 : 0);
                            command.Parameters.AddWithValue("$HelpButtonLight", EGMStatus.GetInstance().o_helpbuttonlightstatus == true ? 1 : 0);
                            command.Parameters.AddWithValue("$LinesButtonLight", EGMStatus.GetInstance().o_linesbuttonlightstatus == true ? 1 : 0);
                            command.Parameters.AddWithValue("$CashoutButtonLight", EGMStatus.GetInstance().o_cashoutbuttonlightstatus == true ? 1 : 0);
                            command.Parameters.AddWithValue("$MaxbetButtonLight", EGMStatus.GetInstance().o_maxbetbuttonlightstatus == true ? 1 : 0);
                            command.Parameters.AddWithValue("$ServiceButtonLight", EGMStatus.GetInstance().o_servicebuttonlightstatus == true ? 1 : 0);
                            command.Parameters.AddWithValue("$AutoSpinButtonLight", EGMStatus.GetInstance().o_autospinbuttonlightstatus == true ? 1 : 0);
                            command.Parameters.AddWithValue("$MainDoorStatus", EGMStatus.GetInstance().s_mainDoorStatus == true ? 1 : 0);
                            command.Parameters.AddWithValue("$BellyDoorStatus", EGMStatus.GetInstance().s_bellyDoorStatus == true ? 1 : 0);
                            command.Parameters.AddWithValue("$CashboxDoorStatus", EGMStatus.GetInstance().s_cashBoxDoorStatus == true ? 1 : 0);
                            command.Parameters.AddWithValue("$DropboxDoorStatus", EGMStatus.GetInstance().s_dropBoxDoorStatus == true ? 1 : 0);
                            command.Parameters.AddWithValue("$CardcageDoorStatus", EGMStatus.GetInstance().s_cardCageDoorStatus == true ? 1 : 0);
                            command.Parameters.AddWithValue("$LogicDoorStatus", EGMStatus.GetInstance().s_logicDoorStatus == true ? 1 : 0);
                            command.Parameters.AddWithValue("$SetDateTime", EGMStatus.GetInstance().setDateTime == true ? 1 : 0);
                            command.Parameters.AddWithValue("$FullRamClearPerformed", EGMStatus.GetInstance().fullramclearperformed == true ? 1 : 0);
                            command.Parameters.AddWithValue("$CurrentAnimationEvent", EGMStatus.GetInstance().current_animation_event == null ? -1 : EGMStatus.GetInstance().current_animation_event.GetHashCode());
                            command.Parameters.AddWithValue("$Status", EGMStatus.GetInstance().frontend_play.thisstatus.GetHashCode());
                            command.Parameters.AddWithValue("$PennyStatus", EGMStatus.GetInstance().frontend_play_penny.thisstatus.GetHashCode());
                            //command.Parameters.AddWithValue("$LastSpinMarshall", EGMStatus.GetInstance().lastplay.GetHashCode());
                            command.Parameters.AddWithValue("$PayLine1Amount", EGMStatus.GetInstance().current_play.payline1amount);
                            command.Parameters.AddWithValue("$PayLine2Amount", EGMStatus.GetInstance().current_play.payline2amount);
                            command.Parameters.AddWithValue("$PayLine3Amount", EGMStatus.GetInstance().current_play.payline3amount);
                            command.Parameters.AddWithValue("$PayLine4Amount", EGMStatus.GetInstance().current_play.payline4amount);
                            command.Parameters.AddWithValue("$PayLine5Amount", EGMStatus.GetInstance().current_play.payline5amount);
                            command.Parameters.AddWithValue("$CreditsWon", EGMStatus.GetInstance().current_play.CreditsWon);
                            command.Parameters.AddWithValue("$BaseCreditsWon", EGMStatus.GetInstance().current_play.BaseCreditsWon);
                            command.Parameters.AddWithValue("$ScatterCreditsWon", EGMStatus.GetInstance().current_play.ScatterCreditsWon);
                            command.Parameters.AddWithValue("$ExpandedCreditsWon", EGMStatus.GetInstance().current_play.ExpandedCreditsWon);
                            command.Parameters.AddWithValue("$MisteryCreditsWon", EGMStatus.GetInstance().current_play.MisteryCreditsWon);
                            command.Parameters.AddWithValue("$ActionGameCreditsWon", EGMStatus.GetInstance().current_play.ActionGameCreditsWon);
                            command.Parameters.AddWithValue("$ScatterWin", EGMStatus.GetInstance().current_play.scatter_win == true ? 1 : 0);
                            command.Parameters.AddWithValue("$MisteryWin", EGMStatus.GetInstance().current_play.mistery_win == true ? 1 : 0);
                            command.Parameters.AddWithValue("$ExpandedWin", EGMStatus.GetInstance().current_play.expanded_win == true ? 1 : 0);
                            command.Parameters.AddWithValue("$BaseWinning", EGMStatus.GetInstance().current_play.baseWinning == true ? 1 : 0);
                            command.Parameters.AddWithValue("$PennyGameIndex", EGMStatus.GetInstance().current_play.pennyGamesIndex);
                            command.Parameters.AddWithValue("$TotalCurrentPennyGames", EGMStatus.GetInstance().current_play.totalCurrentPennyGames);
                            command.Parameters.AddWithValue("$TotalCurrentActionGames", EGMStatus.GetInstance().current_play.totalCurrentActionGames);
                            command.Parameters.AddWithValue("$ActionGameIndex", EGMStatus.GetInstance().current_play.actionGameIndex);
                            command.Parameters.AddWithValue("$ActionGameWinning", EGMStatus.GetInstance().current_play.actiongamewinning);
                            command.Parameters.AddWithValue("$SelectedCreditValue", EGMStatus.GetInstance().selectedCreditValue);

                            try
                            {
                                command.Parameters.AddWithValue("$ExtraData", EGMStatus.GetInstance().current_play.ExtraDataStr);
                            }
                            catch
                            {
                                command.Parameters.AddWithValue("$ExtraData", "");

                            }


                            // Execute                           
                            command.ExecuteNonQuery();



                        }

                    }
                    catch (Exception ex)
                    {
                        //  File.AppendAllLines("Log.txt", new string[] { DateTime.Now.ToString(), ex.Message });

                    }
                    finally
                    {
                        connection.Close();
                    }

                    // Execute
                }
            }
        }


        private bool EGMStatus_CompareCurrentWithSnapshot()
        {



            bool different = false;

            if (EGMStatus.GetOldInstance().currentBet != EGMStatus.GetInstance().currentBet) { EGMStatus.GetOldInstance().currentBet = EGMStatus.GetInstance().currentBet; different = true; }
            if (EGMStatus.GetOldInstance().currentAmount != EGMStatus.GetInstance().currentAmount) { EGMStatus.GetOldInstance().currentAmount = EGMStatus.GetInstance().currentAmount; different = true; }
            if (EGMStatus.GetOldInstance().selectedCreditValue != EGMStatus.GetInstance().selectedCreditValue) { EGMStatus.GetOldInstance().selectedCreditValue = EGMStatus.GetInstance().selectedCreditValue; different = true; }
            if (EGMStatus.GetOldInstance().currentCashableAmount != EGMStatus.GetInstance().currentCashableAmount) { EGMStatus.GetOldInstance().currentCashableAmount = EGMStatus.GetInstance().currentCashableAmount; different = true; }
            if (EGMStatus.GetOldInstance().currentNonRestrictedAmount != EGMStatus.GetInstance().currentNonRestrictedAmount) { EGMStatus.GetOldInstance().currentNonRestrictedAmount = EGMStatus.GetInstance().currentNonRestrictedAmount; different = true; }
            if (EGMStatus.GetOldInstance().currentRestrictedAmount != EGMStatus.GetInstance().currentRestrictedAmount) { EGMStatus.GetOldInstance().currentRestrictedAmount = EGMStatus.GetInstance().currentRestrictedAmount; different = true; }
            if (EGMStatus.GetOldInstance().menuActive != EGMStatus.GetInstance().menuActive) { EGMStatus.GetOldInstance().menuActive = EGMStatus.GetInstance().menuActive; different = true; }
            if (EGMStatus.GetOldInstance().disabledByHost != EGMStatus.GetInstance().disabledByHost) { EGMStatus.GetOldInstance().disabledByHost = EGMStatus.GetInstance().disabledByHost; different = true; }
            if (EGMStatus.GetOldInstance().soundEnabled != EGMStatus.GetInstance().soundEnabled) { EGMStatus.GetOldInstance().soundEnabled = EGMStatus.GetInstance().soundEnabled; different = true; }
            if (EGMStatus.GetOldInstance().betLines != EGMStatus.GetInstance().betLines) { EGMStatus.GetOldInstance().betLines = EGMStatus.GetInstance().betLines; different = true; }
            if (EGMStatus.GetOldInstance().betxline != EGMStatus.GetInstance().betxline) { EGMStatus.GetOldInstance().betxline = EGMStatus.GetInstance().betxline; different = true; }
            if (EGMStatus.GetOldInstance().current_handpay.Amount != EGMStatus.GetInstance().current_handpay.Amount) { EGMStatus.GetOldInstance().current_handpay.Amount = EGMStatus.GetInstance().current_handpay.Amount; different = true; }
            if (EGMStatus.GetOldInstance().current_handpay.TrigerTS != EGMStatus.GetInstance().current_handpay.TrigerTS) { EGMStatus.GetOldInstance().current_handpay.TrigerTS = EGMStatus.GetInstance().current_handpay.TrigerTS; different = true; }
            if (EGMStatus.GetOldInstance().current_handpay.ResetTS != EGMStatus.GetInstance().current_handpay.ResetTS) { EGMStatus.GetOldInstance().current_handpay.ResetTS = EGMStatus.GetInstance().current_handpay.ResetTS; different = true; }
            if (EGMStatus.GetOldInstance().current_handpay.ResetMode != EGMStatus.GetInstance().current_handpay.ResetMode) { EGMStatus.GetOldInstance().current_handpay.ResetMode = EGMStatus.GetInstance().current_handpay.ResetMode; different = true; }
            if (EGMStatus.GetOldInstance().current_jackpot.Amount != EGMStatus.GetInstance().current_jackpot.Amount) { EGMStatus.GetOldInstance().current_jackpot.Amount = EGMStatus.GetInstance().current_jackpot.Amount; different = true; }
            if (EGMStatus.GetOldInstance().current_jackpot.TrigerTS != EGMStatus.GetInstance().current_jackpot.TrigerTS) { EGMStatus.GetOldInstance().current_jackpot.TrigerTS = EGMStatus.GetInstance().current_jackpot.TrigerTS; different = true; }
            if (EGMStatus.GetOldInstance().current_jackpot.ResetTS != EGMStatus.GetInstance().current_jackpot.ResetTS) { EGMStatus.GetOldInstance().current_jackpot.ResetTS = EGMStatus.GetInstance().current_jackpot.ResetTS; different = true; }
            if (EGMStatus.GetOldInstance().current_jackpot.ResetMode != EGMStatus.GetInstance().current_jackpot.ResetMode) { EGMStatus.GetOldInstance().current_jackpot.ResetMode = EGMStatus.GetInstance().current_jackpot.ResetMode; different = true; }
            if (EGMStatus.GetOldInstance().maintenanceMode != EGMStatus.GetInstance().maintenanceMode) { EGMStatus.GetOldInstance().maintenanceMode = EGMStatus.GetInstance().maintenanceMode; different = true; }
            if (EGMStatus.GetOldInstance().current_collect.status != EGMStatus.GetInstance().current_collect.status) { EGMStatus.GetOldInstance().current_collect.status = EGMStatus.GetInstance().current_collect.status; different = true; }
            if (EGMStatus.GetOldInstance().current_jackpot.status != EGMStatus.GetInstance().current_jackpot.status) { EGMStatus.GetOldInstance().current_jackpot.status = EGMStatus.GetInstance().current_jackpot.status; different = true; }
            if (EGMStatus.GetOldInstance().o_tower1lightStatus != EGMStatus.GetInstance().o_tower1lightStatus) { EGMStatus.GetOldInstance().o_tower1lightStatus = EGMStatus.GetInstance().o_tower1lightStatus; different = true; }
            if (EGMStatus.GetOldInstance().o_tower2lightStatus != EGMStatus.GetInstance().o_tower2lightStatus) { EGMStatus.GetOldInstance().o_tower2lightStatus = EGMStatus.GetInstance().o_tower2lightStatus; different = true; }
            if (EGMStatus.GetOldInstance().o_spinbuttonlightstatus != EGMStatus.GetInstance().o_spinbuttonlightstatus) { EGMStatus.GetOldInstance().o_spinbuttonlightstatus = EGMStatus.GetInstance().o_spinbuttonlightstatus; different = true; }
            if (EGMStatus.GetOldInstance().o_betbuttonlightstatus != EGMStatus.GetInstance().o_betbuttonlightstatus) { EGMStatus.GetOldInstance().o_betbuttonlightstatus = EGMStatus.GetInstance().o_betbuttonlightstatus; different = true; }
            if (EGMStatus.GetOldInstance().o_helpbuttonlightstatus != EGMStatus.GetInstance().o_helpbuttonlightstatus) { EGMStatus.GetOldInstance().o_helpbuttonlightstatus = EGMStatus.GetInstance().o_helpbuttonlightstatus; different = true; }
            if (EGMStatus.GetOldInstance().o_linesbuttonlightstatus != EGMStatus.GetInstance().o_linesbuttonlightstatus) { EGMStatus.GetOldInstance().o_linesbuttonlightstatus = EGMStatus.GetInstance().o_linesbuttonlightstatus; different = true; }
            if (EGMStatus.GetOldInstance().o_cashoutbuttonlightstatus != EGMStatus.GetInstance().o_cashoutbuttonlightstatus) { EGMStatus.GetOldInstance().o_cashoutbuttonlightstatus = EGMStatus.GetInstance().o_cashoutbuttonlightstatus; different = true; }
            if (EGMStatus.GetOldInstance().o_maxbetbuttonlightstatus != EGMStatus.GetInstance().o_maxbetbuttonlightstatus) { EGMStatus.GetOldInstance().o_maxbetbuttonlightstatus = EGMStatus.GetInstance().o_maxbetbuttonlightstatus; different = true; }
            if (EGMStatus.GetOldInstance().o_servicebuttonlightstatus != EGMStatus.GetInstance().o_servicebuttonlightstatus) { EGMStatus.GetOldInstance().o_servicebuttonlightstatus = EGMStatus.GetInstance().o_servicebuttonlightstatus; different = true; }
            if (EGMStatus.GetOldInstance().o_autospinbuttonlightstatus != EGMStatus.GetInstance().o_autospinbuttonlightstatus) { EGMStatus.GetOldInstance().o_autospinbuttonlightstatus = EGMStatus.GetInstance().o_autospinbuttonlightstatus; different = true; }
            if (EGMStatus.GetOldInstance().s_mainDoorStatus != EGMStatus.GetInstance().s_mainDoorStatus) { EGMStatus.GetOldInstance().s_mainDoorStatus = EGMStatus.GetInstance().s_mainDoorStatus; different = true; }
            if (EGMStatus.GetOldInstance().s_bellyDoorStatus != EGMStatus.GetInstance().s_bellyDoorStatus) { EGMStatus.GetOldInstance().s_bellyDoorStatus = EGMStatus.GetInstance().s_bellyDoorStatus; different = true; }
            if (EGMStatus.GetOldInstance().s_cashBoxDoorStatus != EGMStatus.GetInstance().s_cashBoxDoorStatus) { EGMStatus.GetOldInstance().s_cashBoxDoorStatus = EGMStatus.GetInstance().s_cashBoxDoorStatus; different = true; }
            if (EGMStatus.GetOldInstance().s_dropBoxDoorStatus != EGMStatus.GetInstance().s_dropBoxDoorStatus) { EGMStatus.GetOldInstance().s_dropBoxDoorStatus = EGMStatus.GetInstance().s_dropBoxDoorStatus; different = true; }
            if (EGMStatus.GetOldInstance().s_cardCageDoorStatus != EGMStatus.GetInstance().s_cardCageDoorStatus) { EGMStatus.GetOldInstance().s_cardCageDoorStatus = EGMStatus.GetInstance().s_cardCageDoorStatus; different = true; }
            if (EGMStatus.GetOldInstance().s_logicDoorStatus != EGMStatus.GetInstance().s_logicDoorStatus) { EGMStatus.GetOldInstance().s_logicDoorStatus = EGMStatus.GetInstance().s_logicDoorStatus; different = true; }
            if (EGMStatus.GetOldInstance().setDateTime != EGMStatus.GetInstance().setDateTime) { EGMStatus.GetOldInstance().setDateTime = EGMStatus.GetInstance().setDateTime; different = true; }
            if (EGMStatus.GetOldInstance().fullramclearperformed != EGMStatus.GetInstance().fullramclearperformed) { EGMStatus.GetOldInstance().fullramclearperformed = EGMStatus.GetInstance().fullramclearperformed; different = true; }
            if (EGMStatus.GetOldInstance().current_play.ActionGameCreditsWon != EGMStatus.GetInstance().current_play.ActionGameCreditsWon) { EGMStatus.GetOldInstance().current_play.ActionGameCreditsWon = EGMStatus.GetInstance().current_play.ActionGameCreditsWon; different = true; }
            if (EGMStatus.GetOldInstance().current_play.actionGameIndex != EGMStatus.GetInstance().current_play.actionGameIndex) { EGMStatus.GetOldInstance().current_play.actionGameIndex = EGMStatus.GetInstance().current_play.actionGameIndex; different = true; }
            if (EGMStatus.GetOldInstance().current_play.actiongamewinning != EGMStatus.GetInstance().current_play.actiongamewinning) { EGMStatus.GetOldInstance().current_play.actiongamewinning = EGMStatus.GetInstance().current_play.actiongamewinning; different = true; }
            if (EGMStatus.GetOldInstance().current_play.BaseCreditsWon != EGMStatus.GetInstance().current_play.BaseCreditsWon) { EGMStatus.GetOldInstance().current_play.BaseCreditsWon = EGMStatus.GetInstance().current_play.BaseCreditsWon; different = true; }
            if (EGMStatus.GetOldInstance().current_play.baseWinning != EGMStatus.GetInstance().current_play.baseWinning) { EGMStatus.GetOldInstance().fullramclearperformed = EGMStatus.GetInstance().fullramclearperformed; different = true; }
            if (EGMStatus.GetOldInstance().current_play.CreditsWon != EGMStatus.GetInstance().current_play.CreditsWon) { EGMStatus.GetOldInstance().current_play.CreditsWon = EGMStatus.GetInstance().current_play.CreditsWon; different = true; }
            if (EGMStatus.GetOldInstance().current_play.ExpandedCreditsWon != EGMStatus.GetInstance().current_play.ExpandedCreditsWon) { EGMStatus.GetOldInstance().current_play.ExpandedCreditsWon = EGMStatus.GetInstance().current_play.ExpandedCreditsWon; different = true; }
            if (EGMStatus.GetOldInstance().current_play.expanded_win != EGMStatus.GetInstance().current_play.expanded_win) { EGMStatus.GetOldInstance().current_play.expanded_win = EGMStatus.GetInstance().current_play.expanded_win; different = true; }
            if (EGMStatus.GetOldInstance().current_play.MisteryCreditsWon != EGMStatus.GetInstance().current_play.MisteryCreditsWon) { EGMStatus.GetOldInstance().current_play.MisteryCreditsWon = EGMStatus.GetInstance().current_play.MisteryCreditsWon; different = true; }
            if (EGMStatus.GetOldInstance().current_play.mistery_win != EGMStatus.GetInstance().current_play.mistery_win) { EGMStatus.GetOldInstance().current_play.mistery_win = EGMStatus.GetInstance().current_play.mistery_win; different = true; }
            if (EGMStatus.GetOldInstance().current_play.payline1amount != EGMStatus.GetInstance().current_play.payline1amount) { EGMStatus.GetOldInstance().current_play.payline1amount = EGMStatus.GetInstance().current_play.payline1amount; different = true; }
            if (EGMStatus.GetOldInstance().current_play.payline2amount != EGMStatus.GetInstance().current_play.payline2amount) { EGMStatus.GetOldInstance().current_play.payline2amount = EGMStatus.GetInstance().current_play.payline2amount; different = true; }
            if (EGMStatus.GetOldInstance().current_play.payline3amount != EGMStatus.GetInstance().current_play.payline3amount) { EGMStatus.GetOldInstance().current_play.payline3amount = EGMStatus.GetInstance().current_play.payline3amount; different = true; }
            if (EGMStatus.GetOldInstance().current_play.payline4amount != EGMStatus.GetInstance().current_play.payline4amount) { EGMStatus.GetOldInstance().current_play.payline4amount = EGMStatus.GetInstance().current_play.payline4amount; different = true; }
            if (EGMStatus.GetOldInstance().current_play.payline5amount != EGMStatus.GetInstance().current_play.payline5amount) { EGMStatus.GetOldInstance().current_play.payline5amount = EGMStatus.GetInstance().current_play.payline5amount; different = true; }
            if (EGMStatus.GetOldInstance().current_play.ScatterCreditsWon != EGMStatus.GetInstance().current_play.ScatterCreditsWon) { EGMStatus.GetOldInstance().current_play.ScatterCreditsWon = EGMStatus.GetInstance().current_play.ScatterCreditsWon; different = true; }
            if (EGMStatus.GetOldInstance().current_play.scatter_win != EGMStatus.GetInstance().current_play.scatter_win) { EGMStatus.GetOldInstance().current_play.scatter_win = EGMStatus.GetInstance().current_play.scatter_win; different = true; }
            if (EGMStatus.GetOldInstance().current_play.totalCurrentActionGames != EGMStatus.GetInstance().current_play.totalCurrentActionGames) { EGMStatus.GetOldInstance().current_play.totalCurrentActionGames = EGMStatus.GetInstance().current_play.totalCurrentActionGames; different = true; }
            if (EGMStatus.GetOldInstance().current_play.totalCurrentPennyGames != EGMStatus.GetInstance().current_play.totalCurrentPennyGames) { EGMStatus.GetOldInstance().current_play.totalCurrentPennyGames = EGMStatus.GetInstance().current_play.totalCurrentPennyGames; different = true; }

            //if (EGMStatus.GetOldInstance().lastplay != EGMStatus.GetInstance().lastplay) { EGMStatus.GetOldInstance().lastplay = EGMStatus.GetInstance().lastplay; different = true; }


            return different;
        }

        private void EGMStatus_Snapshot()
        {
            EGMStatus.GetOldInstance().currentBet = EGMStatus.GetInstance().currentBet;
            EGMStatus.GetOldInstance().currentAmount = EGMStatus.GetInstance().currentAmount;
            EGMStatus.GetOldInstance().currentCashableAmount = EGMStatus.GetInstance().currentCashableAmount;
            EGMStatus.GetOldInstance().currentNonRestrictedAmount = EGMStatus.GetInstance().currentNonRestrictedAmount;
            EGMStatus.GetOldInstance().currentRestrictedAmount = EGMStatus.GetInstance().currentRestrictedAmount;
            EGMStatus.GetOldInstance().menuActive = EGMStatus.GetInstance().menuActive;
            EGMStatus.GetOldInstance().disabledByHost = EGMStatus.GetInstance().disabledByHost;
            EGMStatus.GetOldInstance().soundEnabled = EGMStatus.GetInstance().soundEnabled;
            EGMStatus.GetOldInstance().betLines = EGMStatus.GetInstance().betLines;
            EGMStatus.GetOldInstance().betxline = EGMStatus.GetInstance().betxline;
            EGMStatus.GetOldInstance().current_handpay.Amount = EGMStatus.GetInstance().current_handpay.Amount;
            EGMStatus.GetOldInstance().current_handpay.TrigerTS = EGMStatus.GetInstance().current_handpay.TrigerTS;
            EGMStatus.GetOldInstance().current_handpay.ResetTS = EGMStatus.GetInstance().current_handpay.ResetTS;
            EGMStatus.GetOldInstance().current_handpay.ResetMode = EGMStatus.GetInstance().current_handpay.ResetMode;
            EGMStatus.GetOldInstance().current_jackpot.Amount = EGMStatus.GetInstance().current_jackpot.Amount;
            EGMStatus.GetOldInstance().current_jackpot.TrigerTS = EGMStatus.GetInstance().current_jackpot.TrigerTS;
            EGMStatus.GetOldInstance().current_jackpot.ResetTS = EGMStatus.GetInstance().current_jackpot.ResetTS;
            EGMStatus.GetOldInstance().current_jackpot.ResetMode = EGMStatus.GetInstance().current_jackpot.ResetMode;
            EGMStatus.GetOldInstance().maintenanceMode = EGMStatus.GetInstance().maintenanceMode;
            EGMStatus.GetOldInstance().current_collect.status = EGMStatus.GetInstance().current_collect.status;
            EGMStatus.GetOldInstance().current_jackpot.status = EGMStatus.GetInstance().current_jackpot.status;
            EGMStatus.GetOldInstance().o_tower1lightStatus = EGMStatus.GetInstance().o_tower1lightStatus;
            EGMStatus.GetOldInstance().o_tower2lightStatus = EGMStatus.GetInstance().o_tower2lightStatus;
            EGMStatus.GetOldInstance().o_spinbuttonlightstatus = EGMStatus.GetInstance().o_spinbuttonlightstatus;
            EGMStatus.GetOldInstance().o_betbuttonlightstatus = EGMStatus.GetInstance().o_betbuttonlightstatus;
            EGMStatus.GetOldInstance().o_helpbuttonlightstatus = EGMStatus.GetInstance().o_helpbuttonlightstatus;
            EGMStatus.GetOldInstance().o_linesbuttonlightstatus = EGMStatus.GetInstance().o_linesbuttonlightstatus;
            EGMStatus.GetOldInstance().o_cashoutbuttonlightstatus = EGMStatus.GetInstance().o_cashoutbuttonlightstatus;
            EGMStatus.GetOldInstance().o_maxbetbuttonlightstatus = EGMStatus.GetInstance().o_maxbetbuttonlightstatus;
            EGMStatus.GetOldInstance().o_servicebuttonlightstatus = EGMStatus.GetInstance().o_servicebuttonlightstatus;
            EGMStatus.GetOldInstance().o_autospinbuttonlightstatus = EGMStatus.GetInstance().o_autospinbuttonlightstatus;
            EGMStatus.GetOldInstance().s_mainDoorStatus = EGMStatus.GetInstance().s_mainDoorStatus;
            EGMStatus.GetOldInstance().s_bellyDoorStatus = EGMStatus.GetInstance().s_bellyDoorStatus;
            EGMStatus.GetOldInstance().s_cashBoxDoorStatus = EGMStatus.GetInstance().s_cashBoxDoorStatus;
            EGMStatus.GetOldInstance().s_dropBoxDoorStatus = EGMStatus.GetInstance().s_dropBoxDoorStatus;
            EGMStatus.GetOldInstance().s_cardCageDoorStatus = EGMStatus.GetInstance().s_cardCageDoorStatus;
            EGMStatus.GetOldInstance().selectedCreditValue = EGMStatus.GetInstance().selectedCreditValue;
            EGMStatus.GetOldInstance().s_logicDoorStatus = EGMStatus.GetInstance().s_logicDoorStatus;
            EGMStatus.GetOldInstance().setDateTime = EGMStatus.GetInstance().setDateTime;
            EGMStatus.GetOldInstance().fullramclearperformed = EGMStatus.GetInstance().fullramclearperformed;
            EGMStatus.GetOldInstance().current_play = new EGMStatus_CurrentPlay();
            EGMStatus.GetOldInstance().current_play.ActionGameCreditsWon = EGMStatus.GetInstance().current_play.ActionGameCreditsWon;
            EGMStatus.GetOldInstance().current_play.actionGameIndex = EGMStatus.GetInstance().current_play.actionGameIndex;
            EGMStatus.GetOldInstance().current_play.actiongamewinning = EGMStatus.GetInstance().current_play.actiongamewinning;
            EGMStatus.GetOldInstance().current_play.BaseCreditsWon = EGMStatus.GetInstance().current_play.BaseCreditsWon;
            EGMStatus.GetOldInstance().current_play.baseWinning = EGMStatus.GetInstance().current_play.baseWinning;
            EGMStatus.GetOldInstance().current_play.CreditsWon = EGMStatus.GetInstance().current_play.CreditsWon;
            EGMStatus.GetOldInstance().current_play.ExpandedCreditsWon = EGMStatus.GetInstance().current_play.ExpandedCreditsWon;
            EGMStatus.GetOldInstance().current_play.expanded_win = EGMStatus.GetInstance().current_play.expanded_win;
            EGMStatus.GetOldInstance().current_play.MisteryCreditsWon = EGMStatus.GetInstance().current_play.MisteryCreditsWon;
            EGMStatus.GetOldInstance().current_play.mistery_win = EGMStatus.GetInstance().current_play.mistery_win;
            EGMStatus.GetOldInstance().current_play.payline1amount = EGMStatus.GetInstance().current_play.payline1amount;
            EGMStatus.GetOldInstance().current_play.payline2amount = EGMStatus.GetInstance().current_play.payline2amount;
            EGMStatus.GetOldInstance().current_play.payline3amount = EGMStatus.GetInstance().current_play.payline3amount;
            EGMStatus.GetOldInstance().current_play.payline4amount = EGMStatus.GetInstance().current_play.payline4amount;
            EGMStatus.GetOldInstance().current_play.payline5amount = EGMStatus.GetInstance().current_play.payline5amount;
            EGMStatus.GetOldInstance().current_play.pennyGamesIndex = EGMStatus.GetInstance().current_play.pennyGamesIndex;
            EGMStatus.GetOldInstance().current_play.ScatterCreditsWon = EGMStatus.GetInstance().current_play.ScatterCreditsWon;
            EGMStatus.GetOldInstance().current_play.scatter_win = EGMStatus.GetInstance().current_play.scatter_win;
            EGMStatus.GetOldInstance().current_play.spin = EGMStatus.GetInstance().current_play.spin;
            EGMStatus.GetOldInstance().current_play.totalCurrentActionGames = EGMStatus.GetInstance().current_play.totalCurrentActionGames;
            EGMStatus.GetOldInstance().current_play.totalCurrentPennyGames = EGMStatus.GetInstance().current_play.totalCurrentPennyGames;

            //EGMStatus.GetOldInstance().lastplay = EGMStatus.GetInstance().lastplay;


        }

        // Read the EGMSTatus
        internal void ReadEGMStatus()
        {
            if (!(databaseon))
                return;
            using (var connection = new SqliteConnection($"Data Source={datalocation}"))
            {
                try
                {
                    connection.Open();

                    // Configurar PRAGMA synchronous para garantizar la escritura física en disco
                    //using (var pragmaCommand = connection.CreateCommand())
                    //{
                    //    pragmaCommand.CommandText = "PRAGMA journal_mode = WAL; PRAGMA synchronous =  EXTRA;";
                    //    pragmaCommand.ExecuteNonQuery();
                    //}

                    var command = connection.CreateCommand();
                    command.CommandText = @"
                                SELECT *
                                FROM EGMStatus;";

                    using (var reader = command.ExecuteReader())
                    {
                        Dictionary<string, int> columns = new Dictionary<string, int>();

                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            columns.Add(reader.GetName(i), i);
                        }

                        while (reader.Read())
                        {
                            try { EGMStatus.GetInstance().MeterCHK = reader.GetString(columns["MeterCHK"]); } catch { }
                            try { EGMStatus.GetInstance().currentBet = reader.GetDecimal(columns["CurrentBet"]); } catch { }
                            try { EGMStatus.GetInstance().currentAmount = reader.GetDecimal(columns["CurrentCredits"]); } catch { }
                            try { EGMStatus.GetInstance().currentCashableAmount = reader.GetDecimal(columns["CurrentCashableCredits"]); } catch { }
                            try { EGMStatus.GetInstance().currentNonRestrictedAmount = reader.GetDecimal(columns["CurrentNonRestrictedCredits"]); } catch { }
                            try { EGMStatus.GetInstance().currentRestrictedAmount = reader.GetDecimal(columns["CurrentRestrictedCredits"]); } catch { }
                            try { EGMStatus.GetInstance().menuActive = reader.GetBoolean(columns["MenuActive"]); } catch { }
                            try { EGMStatus.GetInstance().disabledByHost = reader.GetBoolean(columns["DisabledByHost"]); } catch { }
                            try { EGMStatus.GetInstance().soundEnabled = reader.GetBoolean(columns["SoundEnabled"]); } catch { }
                            try { EGMStatus.GetInstance().betLines = reader.GetInt32(columns["BetLines"]); } catch { }
                            try { EGMStatus.GetInstance().betxline = reader.GetInt32(columns["BetxLine"]); } catch { }
                            EGMStatus.GetInstance().betLines = EGMStatus.GetInstance().betLines < EGMSettings.GetInstance().minBetLines ? EGMSettings.GetInstance().minBetLines : EGMStatus.GetInstance().betLines;
                            EGMStatus.GetInstance().betxline = EGMStatus.GetInstance().betxline < EGMSettings.GetInstance().minBetxline ? EGMSettings.GetInstance().minBetxline : EGMStatus.GetInstance().betxline;
                            try { EGMStatus.GetInstance().current_handpay.Amount = reader.GetDecimal(columns["HandpayAmount"]); } catch { }
                            try { EGMStatus.GetInstance().current_handpay.TrigerTS = FromDateTimeString(reader.GetString(columns["HandpayTriggerTS"])); } catch { }
                            try { EGMStatus.GetInstance().current_handpay.ResetTS = FromDateTimeString(reader.GetString(columns["HandpayResetTS"])); } catch { }
                            try { EGMStatus.GetInstance().current_handpay.ResetMode = reader.GetString(columns["HandpayResetMode"]); } catch { }
                            try { EGMStatus.GetInstance().current_jackpot.Amount = reader.GetDecimal(columns["JackpotAmount"]); } catch { }
                            try { EGMStatus.GetInstance().current_jackpot.TrigerTS = FromDateTimeString(reader.GetString(columns["JackpotTriggerTS"])); } catch { }
                            try { EGMStatus.GetInstance().current_jackpot.ResetTS = FromDateTimeString(reader.GetString(columns["JackpotResetTS"])); } catch { }
                            try { EGMStatus.GetInstance().current_jackpot.ResetMode = reader.GetString(columns["JackpotResetMode"]); } catch { }
                            try { EGMStatus.GetInstance().maintenanceMode = reader.GetBoolean(columns["MaintenanceMode"]); } catch { }
                            try { EGMStatus.GetInstance().current_handpay.UpdateStatus((HandpayStatus)reader.GetInt32(columns["HandpayStatus"]), false); } catch { }
                            try { EGMStatus.GetInstance().current_collect.UpdateStatus((CollectStatus)reader.GetInt32(columns["CollectStatus"]), false); } catch { }
                            try { EGMStatus.GetInstance().current_jackpot.UpdateStatus((JackPotStatus)reader.GetInt32(columns["JackpotStatus"]), false); } catch { }
                            try { EGMStatus.GetInstance().o_tower1lightStatus = reader.GetBoolean(columns["Tower1Light"]); } catch { }
                            try { EGMStatus.GetInstance().o_tower2lightStatus = reader.GetBoolean(columns["Tower2Light"]); } catch { }
                            try { EGMStatus.GetInstance().o_spinbuttonlightstatus = reader.GetBoolean(columns["SpinButtonLight"]); } catch { }
                            try { EGMStatus.GetInstance().o_betbuttonlightstatus = reader.GetBoolean(columns["BetButtonLight"]); } catch { }
                            try { EGMStatus.GetInstance().o_helpbuttonlightstatus = reader.GetBoolean(columns["HelpButtonLight"]); } catch { }
                            try { EGMStatus.GetInstance().o_linesbuttonlightstatus = reader.GetBoolean(columns["LinesButtonLight"]); } catch { }
                            try { EGMStatus.GetInstance().o_cashoutbuttonlightstatus = reader.GetBoolean(columns["CashoutButtonLight"]); } catch { }
                            try { EGMStatus.GetInstance().o_maxbetbuttonlightstatus = reader.GetBoolean(columns["MaxbetButtonLight"]); } catch { }
                            try { EGMStatus.GetInstance().o_servicebuttonlightstatus = reader.GetBoolean(columns["ServiceButtonLight"]); } catch { }
                            try { EGMStatus.GetInstance().o_autospinbuttonlightstatus = reader.GetBoolean(columns["AutoSpinButtonLight"]); } catch { }
                            try { EGMStatus.GetInstance().s_mainDoorStatus = reader.GetBoolean(columns["MainDoorStatus"]); } catch { }
                            try { EGMStatus.GetInstance().s_bellyDoorStatus = reader.GetBoolean(columns["BellyDoorStatus"]); } catch { }
                            try { EGMStatus.GetInstance().s_cashBoxDoorStatus = reader.GetBoolean(columns["CashboxDoorStatus"]); } catch { }
                            try { EGMStatus.GetInstance().s_dropBoxDoorStatus = reader.GetBoolean(columns["DropboxDoorStatus"]); } catch { }
                            try { EGMStatus.GetInstance().s_cardCageDoorStatus = reader.GetBoolean(columns["CardcageDoorStatus"]); } catch { }
                            try { EGMStatus.GetInstance().s_logicDoorStatus = reader.GetBoolean(columns["LogicDoorStatus"]); } catch { }
                            try { EGMStatus.GetInstance().setDateTime = reader.GetBoolean(columns["SetDateTime"]); } catch { }
                            try { EGMStatus.GetInstance().fullramclearperformed = reader.GetBoolean(columns["FullRamClearPerformed"]); } catch { }
                            try { EGMStatus.GetInstance().current_animation_event = (AnimationEvent)reader.GetInt32(columns["CurrentAnimationEvent"]); } catch { }
                            try { EGMStatus.GetInstance().selectedCreditValue = reader.GetDecimal(columns["SelectedCreditvalue"]); } catch { }
                            EGMStatus.GetInstance().current_play = new EGMStatus_CurrentPlay();
                            try { EGMStatus.GetInstance().current_play.ActionGameCreditsWon = reader.GetInt32(columns["ActionGameCreditsWon"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.actionGameIndex = reader.GetInt32(columns["ActionGameIndex"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.actiongamewinning = reader.GetBoolean(columns["ActionGameWinning"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.BaseCreditsWon = reader.GetInt32(columns["BaseCreditsWon"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.baseWinning = reader.GetBoolean(columns["BaseWinning"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.CreditsWon = reader.GetInt32(columns["CreditsWon"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.ExpandedCreditsWon = reader.GetInt32(columns["ExpandedCreditsWon"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.expanded_win = reader.GetBoolean(columns["ExpandedWin"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.MisteryCreditsWon = reader.GetInt32(columns["MisteryCreditsWon"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.mistery_win = reader.GetBoolean(columns["MisteryWin"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.payline1amount = reader.GetDecimal(columns["PayLine1Amount"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.payline2amount = reader.GetDecimal(columns["PayLine2Amount"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.payline3amount = reader.GetDecimal(columns["PayLine3Amount"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.payline4amount = reader.GetDecimal(columns["PayLine4Amount"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.payline5amount = reader.GetDecimal(columns["PayLine5Amount"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.pennyGamesIndex = reader.GetInt32(columns["PennyGameIndex"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.ScatterCreditsWon = reader.GetInt32(columns["ScatterCreditsWon"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.scatter_win = reader.GetBoolean(columns["ScatterWin"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.ExtraData = JsonConvert.DeserializeObject<ExtraPlayData?>(reader.GetString(columns["ExtraData"])); } catch { }
                            try { EGMStatus.GetInstance().current_play.ExtraDataStr = reader.GetString(columns["ExtraData"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.totalCurrentActionGames = reader.GetInt32(columns["TotalCurrentActionGames"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.totalCurrentPennyGames = reader.GetInt32(columns["TotalCurrentPennyGames"]); } catch { }


                            ReadEGMSlotPlay();
                            //try { EGMStatus.GetInstance().lastplay = reader.GetString(columns["LastSpinMarshall"]); } catch { }
                            try { EGMStatus.GetInstance().frontend_play.UpdateStatus((FrontEndPlayStatus)reader.GetInt32(columns["Status"]), false); } catch { }
                            try { EGMStatus.GetInstance().frontend_play_penny.UpdateStatus((FrontEndPlayPennyStatus)reader.GetInt32(columns["PennyStatus"]), false); } catch { }
                            IntegrityController.GetInstance().CheckEGMStatus(EGMStatus.GetInstance());
                            EGMStatus_Snapshot();

                        }
                    }
                }
                catch
                {

                }
                finally
                {
                    connection.Close();
                }

            }

        }

        // Read the ReadEGMSlotPlay
        private void ReadEGMSlotPlay()
        {
            if (!(databaseon))
                return;
            using (var connection = new SqliteConnection($"Data Source={datalocation}"))
            {
                try
                {
                    connection.Open();

                    // Configurar PRAGMA synchronous para garantizar la escritura física en disco
                    //using (var pragmaCommand = connection.CreateCommand())
                    //{
                    //    pragmaCommand.CommandText = "PRAGMA journal_mode = WAL; PRAGMA synchronous =  EXTRA;";
                    //    pragmaCommand.ExecuteNonQuery();
                    //}

                    var command = connection.CreateCommand();
                    command.CommandText = @"
                                SELECT *
                                FROM EGMCurrentSlotPlay;";

                    using (var reader = command.ExecuteReader())
                    {
                        Dictionary<string, int> columns = new Dictionary<string, int>();

                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            columns.Add(reader.GetName(i), i);
                        }

                        while (reader.Read())
                        {
                            EGMStatus.GetInstance().current_play.spin = new SpinMarshall();
                            EGMStatus.GetInstance().current_play.spin.slotplay = new EGMPlayModule.Impl.Slot.SlotPlay();

                            try { EGMStatus.GetInstance().current_play.MeterCHK = reader.GetString(columns["MeterCHK"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.actionGameWin = reader.GetDecimal(columns["ActionGameWin"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.actiongame_segment = reader.GetInt32(columns["ActionGameSegment"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.baseWin = reader.GetDecimal(columns["BaseWin"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.totalBetAmount = reader.GetDecimal(columns["BetCredits"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.bonus = reader.GetDecimal(columns["Bonus"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.creditsAfter = reader.GetDecimal(columns["CreditsAfter"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.creditsAfterActionGameWin = reader.GetDecimal(columns["CreditsAfterActionGameWin"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.creditsAfterBaseWin = reader.GetDecimal(columns["CreditsAfterBaseWin"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.creditsAfterExpandedWin = reader.GetDecimal(columns["CreditsAfterExpandedWin"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.creditsAfterMisteryWin = reader.GetDecimal(columns["CreditsAfterMisteryWin"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.creditsAfterScatterWin = reader.GetDecimal(columns["CreditsAfterScatterWin"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.creditsBefore = reader.GetDecimal(columns["CreditsBefore"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.creditsOnPlay = reader.GetDecimal(columns["CreditsOnPlay"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.creditValue = reader.GetDecimal(columns["CreditValue"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.exceededCredits = reader.GetDecimal(columns["ExceededCredits"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.expandedPayLines = JsonConvert.DeserializeObject<string[]?>(reader.GetString(columns["ExpandedWinningLines"])); } catch { }
                            EGMStatus.GetInstance().current_play.spin.slotplay.expandedSymbol = new Symbol();
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.expandedSymbol.Abbreviation = reader.GetString(columns["ExpandedSymbol_Abbreviation"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.expandedSymbol.Category = reader.GetString(columns["ExpandedSymbol_Category"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.expandedSymbol.Id = reader.GetString(columns["ExpandedSymbol_Id"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.expandedSymbol.IsScatter = reader.GetBoolean(columns["ExpandedSymbol_IsScatter"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.expandedSymbol.IsWild = reader.GetBoolean(columns["ExpandedSymbol_IsWild"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.expandedSymbol.Name = reader.GetString(columns["ExpandedSymbol_Name"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.expandedWin = reader.GetDecimal(columns["ExpandedWin"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.expandedWinningLines = JsonConvert.DeserializeObject<Winningline[]?>(reader.GetString(columns["ExpandedWinningLines"])); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.misteryWin = reader.GetDecimal(columns["MisteryWin"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.payLines = JsonConvert.DeserializeObject<string[]?>(reader.GetString(columns["PayLines"])); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.physicalReelStops = JsonConvert.DeserializeObject<int[]?>(reader.GetString(columns["PhysicalReelStops"])); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.reel1Disposition = JsonConvert.DeserializeObject<string[]?>(reader.GetString(columns["Reel1Disposition"])); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.reel2Disposition = JsonConvert.DeserializeObject<string[]?>(reader.GetString(columns["Reel2Disposition"])); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.reel3Disposition = JsonConvert.DeserializeObject<string[]?>(reader.GetString(columns["Reel3Disposition"])); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.reel4Disposition = JsonConvert.DeserializeObject<string[]?>(reader.GetString(columns["Reel4Disposition"])); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.reel5Disposition = JsonConvert.DeserializeObject<string[]?>(reader.GetString(columns["Reel5Disposition"])); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.remainingActionGames = reader.GetInt32(columns["RemainingActionGames"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.remainingPennyGames = reader.GetInt32(columns["RemainingPennyGames"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.awardedPennyGames = reader.GetDecimal(columns["AwardedPennyGames"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.scatterReelPositions = JsonConvert.DeserializeObject<int[]?>(reader.GetString(columns["ScatterReelPositions"])); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.scatterWin = reader.GetDecimal(columns["ScatterWin"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.status = (PlayRepresentationStatus)reader.GetInt32(columns["Status"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.TSPlayEnd = reader.GetDateTime(columns["TSPlayStart"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.TSPlayStart = reader.GetDateTime(columns["TSPlayEnd"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.winCredits = reader.GetDecimal(columns["WinCredits"]); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.winningLines = JsonConvert.DeserializeObject<Winningline[]?>(reader.GetString(columns["WinningLines"])); } catch { }
                            try { EGMStatus.GetInstance().current_play.spin.slotplay.Finished = reader.GetBoolean(columns["Finished"]); } catch { }

                            //try { EGMStatus.GetInstance().lastplay = reader.GetString(columns["LastSpinMarshall"]); } catch { }
                            IntegrityController.GetInstance().CheckEGMSlotPlay(EGMStatus.GetInstance().current_play);

                            EGMStatus_Snapshot();

                        }
                    }
                }
                catch
                {

                }
                finally
                {
                    connection.Close();
                }
            }

        }


        #endregion

        #region "EGM SETTINGS"


        /// <summary>
        /// Persist EGM Settings
        /// </summary>
        internal void PersistEGMSettings(bool forcepersist)
        {
            if (IntegrityController.GetInstance().RAMERROR)
                return;
            if (EGMSettings_CompareCurrentWithSnapshot() || forcepersist)
            {
                using (var connection = new SqliteConnection($"Data Source={datalocation}"))
                {
                    try
                    {
                        connection.Open();

                        // Configurar PRAGMA synchronous para garantizar la escritura física en disco
                        //using (var pragmaCommand = connection.CreateCommand())
                        //{
                        //    pragmaCommand.CommandText = "PRAGMA journal_mode = WAL; PRAGMA synchronous =  EXTRA;";
                        //    pragmaCommand.ExecuteNonQuery();
                        //}

                        // Create the command with the placeholders
                        var command = connection.CreateCommand();
                        command.CommandText = @"UPDATE EGMSettings
                                        SET AttendantPin = $AttendantPin,
                                           TechnicianPin = $TechnicianPin,
                                           OperatorPin = $OperatorPin,
                                           ManufacturerPin = $ManufacturerPin,
                                           PartialPay = $PartialPay,
                                           BillAcceptor = $BillAcceptor,
                                           HandpayEnabled = $HandpayEnabled,
                                           AFTEnabled = $AFTEnabled,
                                           CreditLimit = $CreditLimit,
                                           SASConfigured = $SASConfigured,
                                           HostBillAcceptorEnabled = $HostBillAcceptorEnabled,
                                           HostSoundEnabled = $HostSoundEnabled,
                                           HostRealTimeModeEnabled = $HostRealTimeModeEnabled,
                                           HostValidateHandpaysReceipts = $HostValidateHandpaysReceipts,
                                           HostTicketsForForeignRestrictedAmounts = $HostTicketsForForeignRestrictedAmounts,
                                           HostTicketsRedemptionEnabled = $HostTicketsRedemptionEnabled,
                                           MainSASEnabled = $MainSASEnabled,
                                           MainSASId = $MainSASId,
                                           MainAssetNumber = $MainAssetNumber,
                                           MainSerialNumber = $MainSerialNumber,
                                           MainTiltOnASASDisconnection = $MainTiltOnASASDisconnection,
                                           MainAccountingDenomination = $MainAccountingDenomination,
                                           MainSASReportedDenomination = $MainSASReportedDenomination,
                                           MainInHouseInLimit = $MainInHouseInLimit,
                                           MainInHouseOutLimit = $MainInHouseOutLimit,
                                           VLTSASVersion = $VLTSASVersion,
                                           VLTGameID = $VLTGameID,
                                           VLTAdditionalID = $VLTAdditionalID,
                                           VLTMultidenominationEnabled = $VLTMultidenominationEnabled,
                                           VLTAuthenticationEnabled = $VLTAuthenticationEnabled,
                                           VLTExtendedMetersEnabled = $VLTExtendedMetersEnabled,
                                           VLTTicketsCounterEnabled = $VLTTicketsCounterEnabled,
                                           VLTAccountingDenomination = $VLTAccountingDenomination,
                                           MaxBetLines = $MaxBetLines,
                                           MaxBetxLine = $MaxBetxLine,
                                           JackpotLimit = $JackpotLimit,
                                           JackpotEnabled = $JackpotEnabled,
                                           MinBetLines = $MinBetLines,
                                           MinBetxLine = $MinBetxLine,
                                           BillAcceptorComPort = $BillAcceptorComPort,
                                           BillAcceptorChannelSet1 = $BillAcceptorChannelSet1,
                                           BillAcceptorChannelSet2 = $BillAcceptorChannelSet2,
                                           BillAcceptorChannelSet3 = $BillAcceptorChannelSet3
                                       WHERE ID = 0;";

                        // Make SQL Parameters with some info of EGMStatus
                        command.Parameters.AddWithValue("$AttendantPin", EGMSettings.GetInstance().AttendantPin);
                        command.Parameters.AddWithValue("$TechnicianPin", EGMSettings.GetInstance().TechnicianPin);
                        command.Parameters.AddWithValue("$OperatorPin", EGMSettings.GetInstance().OperatorPin);
                        command.Parameters.AddWithValue("$ManufacturerPin", EGMSettings.GetInstance().ManufacturerPin);
                        command.Parameters.AddWithValue("$PartialPay", EGMSettings.GetInstance().PartialPay == true ? 1 : 0);
                        command.Parameters.AddWithValue("$BillAcceptor", EGMSettings.GetInstance().BillAcceptor == true ? 1 : 0);
                        command.Parameters.AddWithValue("$HandpayEnabled", EGMSettings.GetInstance().HandpayEnabled == true ? 1 : 0);
                        command.Parameters.AddWithValue("$AFTEnabled", EGMSettings.GetInstance().AFTEnabled == true ? 1 : 0);
                        command.Parameters.AddWithValue("$JackpotEnabled", EGMSettings.GetInstance().jackpotEnabled == true ? 1 : 0);
                        command.Parameters.AddWithValue("$CreditLimit", EGMSettings.GetInstance().creditLimit);
                        command.Parameters.AddWithValue("$JackpotLimit", EGMSettings.GetInstance().jackpotLimit);
                        command.Parameters.AddWithValue("$SASConfigured", EGMSettings.GetInstance().sasSettings.SASConfigured == true ? 1 : 0);
                        command.Parameters.AddWithValue("$HostBillAcceptorEnabled", EGMSettings.GetInstance().sasSettings.hostConfiguration.BillAcceptorEnabled == true ? 1 : 0);
                        command.Parameters.AddWithValue("$HostSoundEnabled", EGMSettings.GetInstance().sasSettings.hostConfiguration.SoundEnabled == true ? 1 : 0);
                        command.Parameters.AddWithValue("$HostRealTimeModeEnabled", EGMSettings.GetInstance().sasSettings.hostConfiguration.RealTimeModeEnabled == true ? 1 : 0);
                        command.Parameters.AddWithValue("$HostValidateHandpaysReceipts", EGMSettings.GetInstance().sasSettings.hostConfiguration.ValidateHandpaysReceipts == true ? 1 : 0);
                        command.Parameters.AddWithValue("$HostTicketsForForeignRestrictedAmounts", EGMSettings.GetInstance().sasSettings.hostConfiguration.TicketsForForeignRestrictedAmounts == true ? 1 : 0);
                        command.Parameters.AddWithValue("$HostTicketsRedemptionEnabled", EGMSettings.GetInstance().sasSettings.hostConfiguration.TicketsRedemptionEnabled == true ? 1 : 0);
                        command.Parameters.AddWithValue("$MainSASEnabled", EGMSettings.GetInstance().sasSettings.mainConfiguration.SASEnabled == true ? 1 : 0);
                        command.Parameters.AddWithValue("$MainSASId", EGMSettings.GetInstance().sasSettings.mainConfiguration.SASId);
                        command.Parameters.AddWithValue("$MainAssetNumber", EGMSettings.GetInstance().sasSettings.mainConfiguration.AssetNumber);
                        command.Parameters.AddWithValue("$MainSerialNumber", EGMSettings.GetInstance().sasSettings.mainConfiguration.SerialNumber);
                        command.Parameters.AddWithValue("$MainTiltOnASASDisconnection", EGMSettings.GetInstance().sasSettings.mainConfiguration.TiltOnASASDisconnection == true ? 1 : 0);
                        command.Parameters.AddWithValue("$MainAccountingDenomination", EGMSettings.GetInstance().sasSettings.mainConfiguration.AccountingDenomination);
                        command.Parameters.AddWithValue("$MainSASReportedDenomination", EGMSettings.GetInstance().sasSettings.mainConfiguration.SASReportedDenomination);
                        command.Parameters.AddWithValue("$MainInHouseInLimit", EGMSettings.GetInstance().sasSettings.mainConfiguration.InHouseInLimit);
                        command.Parameters.AddWithValue("$MainInHouseOutLimit", EGMSettings.GetInstance().sasSettings.mainConfiguration.InHouseOutLimit);
                        command.Parameters.AddWithValue("$VLTSASVersion", EGMSettings.GetInstance().sasSettings.vltConfiguration.SASVersion);
                        command.Parameters.AddWithValue("$VLTGameID", EGMSettings.GetInstance().sasSettings.vltConfiguration.GameID);
                        command.Parameters.AddWithValue("$VLTAdditionalID", EGMSettings.GetInstance().sasSettings.vltConfiguration.AdditionalID);
                        command.Parameters.AddWithValue("$VLTMultidenominationEnabled", EGMSettings.GetInstance().sasSettings.vltConfiguration.MultidenominationEnabled == true ? 1 : 0);
                        command.Parameters.AddWithValue("$VLTAuthenticationEnabled", EGMSettings.GetInstance().sasSettings.vltConfiguration.AuthenticationEnabled == true ? 1 : 0);
                        command.Parameters.AddWithValue("$VLTExtendedMetersEnabled", EGMSettings.GetInstance().sasSettings.vltConfiguration.ExtendedMetersEnabled == true ? 1 : 0);
                        command.Parameters.AddWithValue("$VLTTicketsCounterEnabled", EGMSettings.GetInstance().sasSettings.vltConfiguration.TicketsCounterEnabled == true ? 1 : 0);
                        command.Parameters.AddWithValue("$VLTAccountingDenomination", EGMSettings.GetInstance().sasSettings.vltConfiguration.AccountingDenomination);
                        command.Parameters.AddWithValue("$MaxBetLines", EGMSettings.GetInstance().maxBetLines);
                        command.Parameters.AddWithValue("$MaxBetxLine", EGMSettings.GetInstance().maxBetxline);
                        command.Parameters.AddWithValue("$MinBetLines", EGMSettings.GetInstance().minBetLines);
                        command.Parameters.AddWithValue("$MinBetxLine", EGMSettings.GetInstance().minBetxline);
                        command.Parameters.AddWithValue("$BillAcceptorComPort", EGMSettings.GetInstance().BillAcceptorComPort);
                        command.Parameters.AddWithValue("$BillAcceptorChannelSet1", EGMSettings.GetInstance().BillAcceptorChannelSet1);
                        command.Parameters.AddWithValue("$BillAcceptorChannelSet2", EGMSettings.GetInstance().BillAcceptorChannelSet2);
                        command.Parameters.AddWithValue("$BillAcceptorChannelSet3", EGMSettings.GetInstance().BillAcceptorChannelSet3);

                        // Execute


                        command.ExecuteReader();

                    }
                    catch
                    {

                    }
                    finally
                    {
                        connection.Close();
                    }

                }
            }
        }
        private bool EGMSettings_CompareCurrentWithSnapshot()
        {
            bool different = false;

            if (EGMSettings.GetOldInstance().AttendantPin != EGMSettings.GetInstance().AttendantPin) { EGMSettings.GetOldInstance().AttendantPin = EGMSettings.GetInstance().AttendantPin; different = true; }
            if (EGMSettings.GetOldInstance().TechnicianPin != EGMSettings.GetInstance().TechnicianPin) { EGMSettings.GetOldInstance().TechnicianPin = EGMSettings.GetInstance().TechnicianPin; different = true; }
            if (EGMSettings.GetOldInstance().OperatorPin != EGMSettings.GetInstance().OperatorPin) { EGMSettings.GetOldInstance().OperatorPin = EGMSettings.GetInstance().OperatorPin; different = true; }
            if (EGMSettings.GetOldInstance().ManufacturerPin != EGMSettings.GetInstance().ManufacturerPin) { EGMSettings.GetOldInstance().ManufacturerPin = EGMSettings.GetInstance().ManufacturerPin; different = true; }
            if (EGMSettings.GetOldInstance().creditLimit != EGMSettings.GetInstance().creditLimit) { EGMSettings.GetOldInstance().creditLimit = EGMSettings.GetInstance().creditLimit; different = true; }
            if (EGMSettings.GetOldInstance().jackpotLimit != EGMSettings.GetInstance().jackpotLimit) { EGMSettings.GetOldInstance().jackpotLimit = EGMSettings.GetInstance().jackpotLimit; different = true; }
            if (EGMSettings.GetOldInstance().PartialPay != EGMSettings.GetInstance().PartialPay) { EGMSettings.GetOldInstance().PartialPay = EGMSettings.GetInstance().PartialPay; different = true; }
            if (EGMSettings.GetOldInstance().BillAcceptor != EGMSettings.GetInstance().BillAcceptor) { EGMSettings.GetOldInstance().BillAcceptor = EGMSettings.GetInstance().BillAcceptor; different = true; }
            if (EGMSettings.GetOldInstance().AFTEnabled != EGMSettings.GetInstance().AFTEnabled) { EGMSettings.GetOldInstance().AFTEnabled = EGMSettings.GetInstance().AFTEnabled; different = true; }
            if (EGMSettings.GetOldInstance().HandpayEnabled != EGMSettings.GetInstance().HandpayEnabled) { EGMSettings.GetOldInstance().HandpayEnabled = EGMSettings.GetInstance().HandpayEnabled; different = true; }
            if (EGMSettings.GetOldInstance().jackpotEnabled != EGMSettings.GetInstance().jackpotEnabled) { EGMSettings.GetOldInstance().jackpotEnabled = EGMSettings.GetInstance().jackpotEnabled; different = true; }
            if (EGMSettings.GetOldInstance().maxBetLines != EGMSettings.GetInstance().maxBetLines) { EGMSettings.GetOldInstance().maxBetLines = EGMSettings.GetInstance().maxBetLines; different = true; }
            if (EGMSettings.GetOldInstance().maxBetxline != EGMSettings.GetInstance().maxBetxline) { EGMSettings.GetOldInstance().maxBetxline = EGMSettings.GetInstance().maxBetxline; different = true; }
            if (EGMSettings.GetOldInstance().minBetLines != EGMSettings.GetInstance().minBetLines) { EGMSettings.GetOldInstance().minBetLines = EGMSettings.GetInstance().minBetLines; different = true; }
            if (EGMSettings.GetOldInstance().minBetxline != EGMSettings.GetInstance().minBetxline) { EGMSettings.GetOldInstance().minBetxline = EGMSettings.GetInstance().minBetxline; different = true; }
            if (EGMSettings.GetOldInstance().sasSettings.SASConfigured != EGMSettings.GetInstance().sasSettings.SASConfigured) { EGMSettings.GetOldInstance().sasSettings.SASConfigured = EGMSettings.GetInstance().sasSettings.SASConfigured; different = true; }
            if (EGMSettings.GetOldInstance().sasSettings.hostConfiguration.BillAcceptorEnabled != EGMSettings.GetInstance().sasSettings.hostConfiguration.BillAcceptorEnabled) { EGMSettings.GetOldInstance().sasSettings.hostConfiguration.BillAcceptorEnabled = EGMSettings.GetInstance().sasSettings.hostConfiguration.BillAcceptorEnabled; different = true; }
            if (EGMSettings.GetOldInstance().sasSettings.hostConfiguration.SoundEnabled != EGMSettings.GetInstance().sasSettings.hostConfiguration.SoundEnabled) { EGMSettings.GetOldInstance().sasSettings.hostConfiguration.SoundEnabled = EGMSettings.GetInstance().sasSettings.hostConfiguration.SoundEnabled; different = true; }
            if (EGMSettings.GetOldInstance().sasSettings.hostConfiguration.RealTimeModeEnabled != EGMSettings.GetInstance().sasSettings.hostConfiguration.RealTimeModeEnabled) { EGMSettings.GetOldInstance().sasSettings.hostConfiguration.RealTimeModeEnabled = EGMSettings.GetInstance().sasSettings.hostConfiguration.RealTimeModeEnabled; different = true; }
            if (EGMSettings.GetOldInstance().sasSettings.hostConfiguration.ValidateHandpaysReceipts != EGMSettings.GetInstance().sasSettings.hostConfiguration.ValidateHandpaysReceipts) { EGMSettings.GetOldInstance().sasSettings.hostConfiguration.ValidateHandpaysReceipts = EGMSettings.GetInstance().sasSettings.hostConfiguration.ValidateHandpaysReceipts; different = true; }
            if (EGMSettings.GetOldInstance().sasSettings.hostConfiguration.TicketsForForeignRestrictedAmounts != EGMSettings.GetInstance().sasSettings.hostConfiguration.TicketsForForeignRestrictedAmounts) { EGMSettings.GetOldInstance().sasSettings.hostConfiguration.TicketsForForeignRestrictedAmounts = EGMSettings.GetInstance().sasSettings.hostConfiguration.TicketsForForeignRestrictedAmounts; different = true; }
            if (EGMSettings.GetOldInstance().sasSettings.hostConfiguration.TicketsRedemptionEnabled != EGMSettings.GetInstance().sasSettings.hostConfiguration.TicketsRedemptionEnabled) { EGMSettings.GetOldInstance().sasSettings.hostConfiguration.TicketsRedemptionEnabled = EGMSettings.GetInstance().sasSettings.hostConfiguration.TicketsRedemptionEnabled; different = true; }
            if (EGMSettings.GetOldInstance().sasSettings.mainConfiguration.SASEnabled != EGMSettings.GetInstance().sasSettings.mainConfiguration.SASEnabled) { EGMSettings.GetOldInstance().sasSettings.mainConfiguration.SASEnabled = EGMSettings.GetInstance().sasSettings.mainConfiguration.SASEnabled; different = true; }
            if (EGMSettings.GetOldInstance().sasSettings.mainConfiguration.SASId != EGMSettings.GetInstance().sasSettings.mainConfiguration.SASId) { EGMSettings.GetOldInstance().sasSettings.mainConfiguration.SASId = EGMSettings.GetInstance().sasSettings.mainConfiguration.SASId; different = true; }
            if (EGMSettings.GetOldInstance().sasSettings.mainConfiguration.AssetNumber != EGMSettings.GetInstance().sasSettings.mainConfiguration.AssetNumber) { EGMSettings.GetOldInstance().sasSettings.mainConfiguration.AssetNumber = EGMSettings.GetInstance().sasSettings.mainConfiguration.AssetNumber; different = true; }
            if (EGMSettings.GetOldInstance().sasSettings.mainConfiguration.SerialNumber != EGMSettings.GetInstance().sasSettings.mainConfiguration.SerialNumber) { EGMSettings.GetOldInstance().sasSettings.mainConfiguration.SerialNumber = EGMSettings.GetInstance().sasSettings.mainConfiguration.SerialNumber; different = true; }
            if (EGMSettings.GetOldInstance().sasSettings.mainConfiguration.TiltOnASASDisconnection != EGMSettings.GetInstance().sasSettings.mainConfiguration.TiltOnASASDisconnection) { EGMSettings.GetOldInstance().sasSettings.mainConfiguration.TiltOnASASDisconnection = EGMSettings.GetInstance().sasSettings.mainConfiguration.TiltOnASASDisconnection; different = true; }
            if (EGMSettings.GetOldInstance().sasSettings.mainConfiguration.AccountingDenomination != EGMSettings.GetInstance().sasSettings.mainConfiguration.AccountingDenomination) { EGMSettings.GetOldInstance().sasSettings.mainConfiguration.AccountingDenomination = EGMSettings.GetInstance().sasSettings.mainConfiguration.AccountingDenomination; different = true; }
            if (EGMSettings.GetOldInstance().sasSettings.mainConfiguration.SASReportedDenomination != EGMSettings.GetInstance().sasSettings.mainConfiguration.SASReportedDenomination) { EGMSettings.GetOldInstance().sasSettings.mainConfiguration.SASReportedDenomination = EGMSettings.GetInstance().sasSettings.mainConfiguration.SASReportedDenomination; different = true; }
            if (EGMSettings.GetOldInstance().sasSettings.mainConfiguration.InHouseInLimit != EGMSettings.GetInstance().sasSettings.mainConfiguration.InHouseInLimit) { EGMSettings.GetOldInstance().sasSettings.mainConfiguration.InHouseInLimit = EGMSettings.GetInstance().sasSettings.mainConfiguration.InHouseInLimit; different = true; }
            if (EGMSettings.GetOldInstance().sasSettings.mainConfiguration.InHouseOutLimit != EGMSettings.GetInstance().sasSettings.mainConfiguration.InHouseOutLimit) { EGMSettings.GetOldInstance().sasSettings.mainConfiguration.InHouseOutLimit = EGMSettings.GetInstance().sasSettings.mainConfiguration.InHouseOutLimit; different = true; }
            if (EGMSettings.GetOldInstance().sasSettings.vltConfiguration.SASVersion != EGMSettings.GetInstance().sasSettings.vltConfiguration.SASVersion) { EGMSettings.GetOldInstance().sasSettings.vltConfiguration.SASVersion = EGMSettings.GetInstance().sasSettings.vltConfiguration.SASVersion; different = true; }
            if (EGMSettings.GetOldInstance().sasSettings.vltConfiguration.GameID != EGMSettings.GetInstance().sasSettings.vltConfiguration.GameID) { EGMSettings.GetOldInstance().sasSettings.vltConfiguration.GameID = EGMSettings.GetInstance().sasSettings.vltConfiguration.GameID; different = true; }
            if (EGMSettings.GetOldInstance().sasSettings.vltConfiguration.AdditionalID != EGMSettings.GetInstance().sasSettings.vltConfiguration.AdditionalID) { EGMSettings.GetOldInstance().sasSettings.vltConfiguration.AdditionalID = EGMSettings.GetInstance().sasSettings.vltConfiguration.AdditionalID; different = true; }
            if (EGMSettings.GetOldInstance().sasSettings.vltConfiguration.AuthenticationEnabled != EGMSettings.GetInstance().sasSettings.vltConfiguration.AuthenticationEnabled) { EGMSettings.GetOldInstance().sasSettings.vltConfiguration.AuthenticationEnabled = EGMSettings.GetInstance().sasSettings.vltConfiguration.AuthenticationEnabled; different = true; }
            if (EGMSettings.GetOldInstance().sasSettings.vltConfiguration.MultidenominationEnabled != EGMSettings.GetInstance().sasSettings.vltConfiguration.MultidenominationEnabled) { EGMSettings.GetOldInstance().sasSettings.vltConfiguration.MultidenominationEnabled = EGMSettings.GetInstance().sasSettings.vltConfiguration.MultidenominationEnabled; different = true; }
            if (EGMSettings.GetOldInstance().sasSettings.vltConfiguration.ExtendedMetersEnabled != EGMSettings.GetInstance().sasSettings.vltConfiguration.ExtendedMetersEnabled) { EGMSettings.GetOldInstance().sasSettings.vltConfiguration.ExtendedMetersEnabled = EGMSettings.GetInstance().sasSettings.vltConfiguration.ExtendedMetersEnabled; different = true; }
            if (EGMSettings.GetOldInstance().sasSettings.vltConfiguration.TicketsCounterEnabled != EGMSettings.GetInstance().sasSettings.vltConfiguration.TicketsCounterEnabled) { EGMSettings.GetOldInstance().sasSettings.vltConfiguration.TicketsCounterEnabled = EGMSettings.GetInstance().sasSettings.vltConfiguration.TicketsCounterEnabled; different = true; }
            if (EGMSettings.GetOldInstance().sasSettings.vltConfiguration.AccountingDenomination != EGMSettings.GetInstance().sasSettings.vltConfiguration.AccountingDenomination) { EGMSettings.GetOldInstance().sasSettings.vltConfiguration.AccountingDenomination = EGMSettings.GetInstance().sasSettings.vltConfiguration.AccountingDenomination; different = true; }
            if (EGMSettings.GetOldInstance().BillAcceptorComPort != EGMSettings.GetInstance().BillAcceptorComPort) { EGMSettings.GetOldInstance().BillAcceptorComPort = EGMSettings.GetInstance().BillAcceptorComPort; different = true; }
            if (EGMSettings.GetOldInstance().BillAcceptorChannelSet1 != EGMSettings.GetInstance().BillAcceptorChannelSet1) { EGMSettings.GetOldInstance().BillAcceptorChannelSet1 = EGMSettings.GetInstance().BillAcceptorChannelSet1; different = true; }
            if (EGMSettings.GetOldInstance().BillAcceptorChannelSet2 != EGMSettings.GetInstance().BillAcceptorChannelSet2) { EGMSettings.GetOldInstance().BillAcceptorChannelSet2 = EGMSettings.GetInstance().BillAcceptorChannelSet2; different = true; }
            if (EGMSettings.GetOldInstance().BillAcceptorChannelSet3 != EGMSettings.GetInstance().BillAcceptorChannelSet3) { EGMSettings.GetOldInstance().BillAcceptorChannelSet3 = EGMSettings.GetInstance().BillAcceptorChannelSet3; different = true; }


            return different;

        }
        private void EGMSettings_Snapshot()
        {
            EGMSettings.GetOldInstance().AttendantPin = EGMSettings.GetInstance().AttendantPin;
            EGMSettings.GetOldInstance().TechnicianPin = EGMSettings.GetInstance().TechnicianPin;
            EGMSettings.GetOldInstance().OperatorPin = EGMSettings.GetInstance().OperatorPin;
            EGMSettings.GetOldInstance().ManufacturerPin = EGMSettings.GetInstance().ManufacturerPin;
            EGMSettings.GetOldInstance().creditLimit = EGMSettings.GetInstance().creditLimit;
            EGMSettings.GetOldInstance().jackpotLimit = EGMSettings.GetInstance().jackpotLimit;
            EGMSettings.GetOldInstance().PartialPay = EGMSettings.GetInstance().PartialPay;
            EGMSettings.GetOldInstance().BillAcceptor = EGMSettings.GetInstance().BillAcceptor;
            EGMSettings.GetOldInstance().AFTEnabled = EGMSettings.GetInstance().AFTEnabled;
            EGMSettings.GetOldInstance().HandpayEnabled = EGMSettings.GetInstance().HandpayEnabled;
            EGMSettings.GetOldInstance().jackpotEnabled = EGMSettings.GetInstance().jackpotEnabled;
            EGMSettings.GetOldInstance().maxBetLines = EGMSettings.GetInstance().maxBetLines;
            EGMSettings.GetOldInstance().maxBetxline = EGMSettings.GetInstance().maxBetxline;
            EGMSettings.GetOldInstance().minBetLines = EGMSettings.GetInstance().minBetLines;
            EGMSettings.GetOldInstance().minBetxline = EGMSettings.GetInstance().minBetxline;
            EGMSettings.GetOldInstance().sasSettings.SASConfigured = EGMSettings.GetInstance().sasSettings.SASConfigured;
            EGMSettings.GetOldInstance().sasSettings.hostConfiguration.BillAcceptorEnabled = EGMSettings.GetInstance().sasSettings.hostConfiguration.BillAcceptorEnabled;
            EGMSettings.GetOldInstance().sasSettings.hostConfiguration.SoundEnabled = EGMSettings.GetInstance().sasSettings.hostConfiguration.SoundEnabled;
            EGMSettings.GetOldInstance().sasSettings.hostConfiguration.RealTimeModeEnabled = EGMSettings.GetInstance().sasSettings.hostConfiguration.RealTimeModeEnabled;
            EGMSettings.GetOldInstance().sasSettings.hostConfiguration.ValidateHandpaysReceipts = EGMSettings.GetInstance().sasSettings.hostConfiguration.ValidateHandpaysReceipts;
            EGMSettings.GetOldInstance().sasSettings.hostConfiguration.TicketsForForeignRestrictedAmounts = EGMSettings.GetInstance().sasSettings.hostConfiguration.TicketsForForeignRestrictedAmounts;
            EGMSettings.GetOldInstance().sasSettings.hostConfiguration.TicketsRedemptionEnabled = EGMSettings.GetInstance().sasSettings.hostConfiguration.TicketsRedemptionEnabled;
            EGMSettings.GetOldInstance().sasSettings.mainConfiguration.SASEnabled = EGMSettings.GetInstance().sasSettings.mainConfiguration.SASEnabled;
            EGMSettings.GetOldInstance().sasSettings.mainConfiguration.SASId = EGMSettings.GetInstance().sasSettings.mainConfiguration.SASId;
            EGMSettings.GetOldInstance().sasSettings.mainConfiguration.AssetNumber = EGMSettings.GetInstance().sasSettings.mainConfiguration.AssetNumber;
            EGMSettings.GetOldInstance().sasSettings.mainConfiguration.SerialNumber = EGMSettings.GetInstance().sasSettings.mainConfiguration.SerialNumber;
            EGMSettings.GetOldInstance().sasSettings.mainConfiguration.TiltOnASASDisconnection = EGMSettings.GetInstance().sasSettings.mainConfiguration.TiltOnASASDisconnection;
            EGMSettings.GetOldInstance().sasSettings.mainConfiguration.AccountingDenomination = EGMSettings.GetInstance().sasSettings.mainConfiguration.AccountingDenomination;
            EGMSettings.GetOldInstance().sasSettings.mainConfiguration.SASReportedDenomination = EGMSettings.GetInstance().sasSettings.mainConfiguration.SASReportedDenomination;
            EGMSettings.GetOldInstance().sasSettings.mainConfiguration.InHouseInLimit = EGMSettings.GetInstance().sasSettings.mainConfiguration.InHouseInLimit;
            EGMSettings.GetOldInstance().sasSettings.mainConfiguration.InHouseOutLimit = EGMSettings.GetInstance().sasSettings.mainConfiguration.InHouseOutLimit;
            EGMSettings.GetOldInstance().sasSettings.vltConfiguration.SASVersion = EGMSettings.GetInstance().sasSettings.vltConfiguration.SASVersion;
            EGMSettings.GetOldInstance().sasSettings.vltConfiguration.GameID = EGMSettings.GetInstance().sasSettings.vltConfiguration.GameID;
            EGMSettings.GetOldInstance().sasSettings.vltConfiguration.AdditionalID = EGMSettings.GetInstance().sasSettings.vltConfiguration.AdditionalID;
            EGMSettings.GetOldInstance().sasSettings.vltConfiguration.AuthenticationEnabled = EGMSettings.GetInstance().sasSettings.vltConfiguration.AuthenticationEnabled;
            EGMSettings.GetOldInstance().sasSettings.vltConfiguration.MultidenominationEnabled = EGMSettings.GetInstance().sasSettings.vltConfiguration.MultidenominationEnabled;
            EGMSettings.GetOldInstance().sasSettings.vltConfiguration.ExtendedMetersEnabled = EGMSettings.GetInstance().sasSettings.vltConfiguration.ExtendedMetersEnabled;
            EGMSettings.GetOldInstance().sasSettings.vltConfiguration.TicketsCounterEnabled = EGMSettings.GetInstance().sasSettings.vltConfiguration.TicketsCounterEnabled;
            EGMSettings.GetOldInstance().sasSettings.vltConfiguration.AccountingDenomination = EGMSettings.GetInstance().sasSettings.vltConfiguration.AccountingDenomination;
            EGMSettings.GetOldInstance().BillAcceptorComPort = EGMSettings.GetInstance().BillAcceptorComPort;
            EGMSettings.GetOldInstance().BillAcceptorChannelSet1 = EGMSettings.GetInstance().BillAcceptorChannelSet1;
            EGMSettings.GetOldInstance().BillAcceptorChannelSet2 = EGMSettings.GetInstance().BillAcceptorChannelSet2;
            EGMSettings.GetOldInstance().BillAcceptorChannelSet3 = EGMSettings.GetInstance().BillAcceptorChannelSet3;

        }
        /// <summary>
        /// Read the EGMSettings
        /// </summary>
        internal void ReadEGMSettings()
        {
            if (!(databaseon))
                return;
            using (var connection = new SqliteConnection($"Data Source={datalocation}"))
            {
                try
                {
                    connection.Open();

                    // Configurar PRAGMA synchronous para garantizar la escritura física en disco
                    //using (var pragmaCommand = connection.CreateCommand())
                    //{
                    //    pragmaCommand.CommandText = "PRAGMA journal_mode = WAL; PRAGMA synchronous =  EXTRA;";
                    //    pragmaCommand.ExecuteNonQuery();
                    //}

                    var command = connection.CreateCommand();
                    command.CommandText = @"
                                SELECT *
                                FROM EGMSettings";

                    using (var reader = command.ExecuteReader())
                    {
                        Dictionary<string, int> columns = new Dictionary<string, int>();

                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            columns.Add(reader.GetName(i), i);
                        }

                        while (reader.Read())
                        {

                            try { EGMSettings.GetInstance().AttendantPin = reader.GetInt32(columns["AttendantPin"]); } catch { }
                            try { EGMSettings.GetInstance().TechnicianPin = reader.GetInt32(columns["TechnicianPin"]); } catch { }
                            try { EGMSettings.GetInstance().OperatorPin = reader.GetInt32(columns["OperatorPin"]); } catch { }
                            try { EGMSettings.GetInstance().ManufacturerPin = reader.GetInt32(columns["ManufacturerPin"]); } catch { }
                            try { EGMSettings.GetInstance().creditLimit = reader.GetDecimal(columns["CreditLimit"]); } catch { }
                            try { EGMSettings.GetInstance().jackpotLimit = reader.GetDecimal(columns["JackpotLimit"]); } catch { }
                            try { EGMSettings.GetInstance().PartialPay = reader.GetBoolean(columns["PartialPay"]); } catch { }
                            try { EGMSettings.GetInstance().BillAcceptor = reader.GetBoolean(columns["BillAcceptor"]); } catch { }
                            try { EGMSettings.GetInstance().AFTEnabled = reader.GetBoolean(columns["AFTEnabled"]); } catch { }
                            try { EGMSettings.GetInstance().HandpayEnabled = reader.GetBoolean(columns["HandpayEnabled"]); } catch { }
                            try { EGMSettings.GetInstance().jackpotEnabled = reader.GetBoolean(columns["JackpotEnabled"]); } catch { }
                            try { EGMSettings.GetInstance().maxBetLines = reader.GetInt32(columns["MaxBetLines"]); } catch { }
                            try { EGMSettings.GetInstance().maxBetxline = reader.GetInt32(columns["MaxBetxLine"]); } catch { }
                            try { EGMSettings.GetInstance().minBetLines = reader.GetInt32(columns["MinBetLines"]); } catch { }
                            try { EGMSettings.GetInstance().minBetxline = reader.GetInt32(columns["MinBetxLine"]); } catch { }
                            try { EGMSettings.GetInstance().sasSettings.SASConfigured = reader.GetBoolean(columns["SASConfigured"]); } catch { }
                            try { EGMSettings.GetInstance().sasSettings.hostConfiguration.BillAcceptorEnabled = reader.GetBoolean(columns["HostBillAcceptorEnabled"]); } catch { }
                            try { EGMSettings.GetInstance().sasSettings.hostConfiguration.SoundEnabled = reader.GetBoolean(columns["HostSoundEnabled"]); } catch { }
                            try { EGMSettings.GetInstance().sasSettings.hostConfiguration.RealTimeModeEnabled = reader.GetBoolean(columns["HostRealTimeModeEnabled"]); } catch { }
                            try { EGMSettings.GetInstance().sasSettings.hostConfiguration.ValidateHandpaysReceipts = reader.GetBoolean(columns["HostValidateHandpaysReceipts"]); } catch { }
                            try { EGMSettings.GetInstance().sasSettings.hostConfiguration.TicketsForForeignRestrictedAmounts = reader.GetBoolean(columns["HostTicketsForForeignRestrictedAmounts"]); } catch { }
                            try { EGMSettings.GetInstance().sasSettings.hostConfiguration.TicketsRedemptionEnabled = reader.GetBoolean(columns["HostTicketsRedemptionEnabled"]); } catch { }
                            try { EGMSettings.GetInstance().sasSettings.mainConfiguration.SASEnabled = reader.GetBoolean(columns["MainSASEnabled"]); } catch { }
                            try { EGMSettings.GetInstance().sasSettings.mainConfiguration.SASId = reader.GetInt32(columns["MainSASId"]); } catch { }
                            try { EGMSettings.GetInstance().sasSettings.mainConfiguration.AssetNumber = reader.GetInt32(columns["MainAssetNumber"]); } catch { }
                            try { EGMSettings.GetInstance().sasSettings.mainConfiguration.SerialNumber = reader.GetInt32(columns["MainSerialNumber"]); } catch { }
                            try { EGMSettings.GetInstance().sasSettings.mainConfiguration.TiltOnASASDisconnection = reader.GetBoolean(columns["MainTiltOnASASDisconnection"]); } catch { }
                            try { EGMSettings.GetInstance().sasSettings.mainConfiguration.AccountingDenomination = reader.GetDecimal(columns["MainAccountingDenomination"]); } catch { }
                            try { EGMSettings.GetInstance().sasSettings.mainConfiguration.SASReportedDenomination = reader.GetDecimal(columns["MainSASReportedDenomination"]); } catch { }
                            try { EGMSettings.GetInstance().sasSettings.mainConfiguration.InHouseInLimit = reader.GetDecimal(columns["MainInHouseInLimit"]); } catch { }
                            try { EGMSettings.GetInstance().sasSettings.mainConfiguration.InHouseOutLimit = reader.GetDecimal(columns["MainInHouseOutLimit"]); } catch { }
                            try { EGMSettings.GetInstance().sasSettings.vltConfiguration.SASVersion = reader.GetString(columns["VLTSASVersion"]); } catch { }
                            try { EGMSettings.GetInstance().sasSettings.vltConfiguration.GameID = reader.GetString(columns["VLTGameID"]); } catch { }
                            try { EGMSettings.GetInstance().sasSettings.vltConfiguration.AdditionalID = reader.GetString(columns["VLTAdditionalID"]); } catch { }
                            try { EGMSettings.GetInstance().sasSettings.vltConfiguration.AuthenticationEnabled = reader.GetBoolean(columns["VLTAuthenticationEnabled"]); } catch { }
                            try { EGMSettings.GetInstance().sasSettings.vltConfiguration.MultidenominationEnabled = reader.GetBoolean(columns["VLTMultidenominationEnabled"]); } catch { }
                            try { EGMSettings.GetInstance().sasSettings.vltConfiguration.ExtendedMetersEnabled = reader.GetBoolean(columns["VLTExtendedMetersEnabled"]); } catch { }
                            try { EGMSettings.GetInstance().sasSettings.vltConfiguration.TicketsCounterEnabled = reader.GetBoolean(columns["VLTTicketsCounterEnabled"]); } catch { }
                            try { EGMSettings.GetInstance().sasSettings.vltConfiguration.AccountingDenomination = reader.GetDecimal(columns["VLTAccountingDenomination"]); } catch { }
                            try { EGMSettings.GetInstance().BillAcceptorComPort = reader.GetString(columns["BillAcceptorComPort"]); } catch { }
                            try { EGMSettings.GetInstance().BillAcceptorChannelSet1 = reader.GetInt32(columns["BillAcceptorChannelSet1"]); } catch { }
                            try { EGMSettings.GetInstance().BillAcceptorChannelSet2 = reader.GetInt32(columns["BillAcceptorChannelSet2"]); } catch { }
                            try { EGMSettings.GetInstance().BillAcceptorChannelSet3 = reader.GetInt32(columns["BillAcceptorChannelSet3"]); } catch { }


                            EGMSettings_Snapshot();

                        }

                    }
                }
                catch
                {

                }
                finally
                {
                    connection.Close();
                }

            }


        }


        #endregion

        #region "EGM ACCOUNTING"

        /// <summary>
        /// Persist the EGM Accounting
        /// </summary>
        internal void PersistEGMAccounting(bool forcepersist)
        {
            if (IntegrityController.GetInstance().RAMERROR)
                return;

            if (EGMAccounting.GetInstance().transfers.newTransfers.Count() > 0
              || !EGMAccounting.GetInstance().transfers.sync
              || EGMAccounting.GetInstance().bills.newlastbills.Count() > 0
              || !EGMAccounting.GetInstance().bills.sync
              || EGMAccounting.GetInstance().systemlogs.newLogs.Count() > 0
              || !EGMAccounting.GetInstance().systemlogs.sync
              || EGMAccounting.GetInstance().plays.newlastplays.Count() > 0
              || !EGMAccounting.GetInstance().plays.sync
              || EGMAccounting.GetInstance().handpays.newHandpays.Count() > 0
              || !EGMAccounting.GetInstance().handpays.sync
              || EGMAccountingMeters_CompareCurrentWithSnapshot() || forcepersist
              || EGMAccounting.GetInstance().ramclears.newRamClears.Count() > 0
              || !EGMAccounting.GetInstance().ramclears.sync)
            {
                using (var connection = new SqliteConnection($"Data Source={datalocation}"))
                {
                    try
                    {
                        connection.Open();

                        // Configurar PRAGMA synchronous para garantizar la escritura física en disco
                        //using (var pragmaCommand = connection.CreateCommand())
                        //{
                        //    pragmaCommand.CommandText = "PRAGMA journal_mode = WAL; PRAGMA synchronous =  EXTRA;";
                        //    pragmaCommand.ExecuteNonQuery();
                        //}

                        // INSERT or DELETE
                        #region EGMAccountingAFTTransfer
                        var AllTransfers = EGMAccounting.GetInstance().transfers.newTransfers.ToList();

                        // Insertion
                        foreach (AccountingAFTTransfer transfer in AllTransfers)
                        {
                            var command = connection.CreateCommand();
                            command.CommandText = @"INSERT INTO EGMAccountingAFTTransfers (
                                                                  Type,
                                                                  Date,
                                                                  DebitCredit,
                                                                  CurrencyAmount,
                                                                  CashableAmount,
                                                                  RestrictedAmount,
                                                                  NonRestrictedAmount
                                            )
                                              VALUES (
                                                  $Type,
                                                  $Date,
                                                  $DebitCredit,
                                                  $CurrencyAmount,
                                                  $CashableAmount,
                                                  $RestrictedAmount,
                                                  $NonRestrictedAmount
                                              );";

                            command.Parameters.AddWithValue("$Type", transfer.Type);
                            command.Parameters.AddWithValue("$Date", ToDateTimeString(transfer.Timestamp));
                            command.Parameters.AddWithValue("$DebitCredit", transfer.DebitCredit);
                            command.Parameters.AddWithValue("$CurrencyAmount", transfer.CurrencyAmount);
                            command.Parameters.AddWithValue("$CashableAmount", transfer.CashableAmount);
                            command.Parameters.AddWithValue("$RestrictedAmount", transfer.RestrictedAmount);
                            command.Parameters.AddWithValue("$NonRestrictedAmount", transfer.NonRestrictedAmount);


                            command.ExecuteNonQuery();
                        }
                        // Clear the new last transfers for avoid repetitions

                        EGMAccounting.GetInstance().transfers.newTransfers.Clear();

                        // if there is no transfer, lets delete all persistence of transfer
                        if (EGMAccounting.GetInstance().transfers.GetTranfersHistory().Count() == 0
                        && !EGMAccounting.GetInstance().transfers.sync)
                        {
                            EGMAccounting.GetInstance().transfers.sync = true;
                            var command = connection.CreateCommand();
                            command.CommandText = @"DELETE FROM EGMAccountingAFTTransfers;";
                            command.ExecuteNonQuery();
                        }
                        #endregion

                        // INSERT or DELETE
                        #region EGMAccountingLastBills
                        var AllLastBills = EGMAccounting.GetInstance().bills.newlastbills.ToList();

                        // Insertion
                        foreach (LastBill bill in AllLastBills)
                        {
                            var command = connection.CreateCommand();
                            command.CommandText = @"INSERT INTO EGMAccountingLastBills (
                                                                  DateTime,
                                                                  Denomination
                                            )
                                              VALUES (
                                                  $Date,
                                                  $Denomination
                                              );";

                            command.Parameters.AddWithValue("$Date", ToDateTimeString(bill.date));
                            command.Parameters.AddWithValue("$Denomination", bill.Denomination);

                            command.ExecuteNonQuery();

                        }
                        // Clear the new last bills for avoid repetitions
                        EGMAccounting.GetInstance().bills.newlastbills.Clear();

                        // if there is no bill, lets delete all persistence of bills
                        if (EGMAccounting.GetInstance().bills.GetLastBills().Count() == 0
                         && !EGMAccounting.GetInstance().bills.sync)
                        {
                            EGMAccounting.GetInstance().bills.sync = true;
                            var command = connection.CreateCommand();
                            command.CommandText = @"DELETE FROM EGMAccountingLastBills;";
                            command.ExecuteNonQuery();
                        }
                        #endregion

                        // INSERT or DELETE
                        #region EGMAccountingSystemLogs
                        var AllSystemLogs = EGMAccounting.GetInstance().systemlogs.newLogs.ToList();

                        // Insertion
                        foreach (SystemLog log in AllSystemLogs)
                        {
                            var command = connection.CreateCommand();
                            command.CommandText = @"INSERT INTO EGMAccountingSystemLogs (
                                                TimeStamp,
                                                Detail,
                                                User
                                            )
                                            VALUES (
                                                $TimeStamp,
                                                $Detail,
                                                $User
                                            );";

                            command.Parameters.AddWithValue("$TimeStamp", ToDateTimeString(log.Date));
                            command.Parameters.AddWithValue("$Detail", log.EventDetail);
                            command.Parameters.AddWithValue("$User", log.User);

                            command.ExecuteNonQuery();

                        }
                        // Clear the new logs for avoid repetitions
                        EGMAccounting.GetInstance().systemlogs.newLogs.Clear();

                        // if there is no log, lets delete all persistence of logs
                        if (EGMAccounting.GetInstance().systemlogs.GetSystemLogs().Count() == 0
                         && !EGMAccounting.GetInstance().systemlogs.sync)
                        {
                            EGMAccounting.GetInstance().systemlogs.sync = true;
                            var command = connection.CreateCommand();
                            command.CommandText = @"DELETE FROM EGMAccountingSystemLogs;";
                            command.ExecuteNonQuery();
                        }
                        #endregion

                        // INSERT or DELETE
                        #region EGMAccountingLastPlays
                        var AllLastPlays = EGMAccounting.GetInstance().plays.newlastplays.ToList();

                        // Insertion
                        foreach (LastPlay play in AllLastPlays)
                        {
                            var command = connection.CreateCommand();
                            command.CommandText = @"INSERT INTO EGMAccountingLastPlays (
                                                   DateTime,
                                                   CreditsBefore,
                                                   CreditsAfter,
                                                   CreditsWagered,
                                                   CreditsWon,
                                                   CreditValue,
                                                   Prize,
                                                   Bonus,
                                                   Total,
                                                   Reel1Stop,
                                                   Reel2Stop,
                                                   Reel3Stop,
                                                   Reel4Stop,
                                                   Reel5Stop,
                                                   WinningLines,
                                                   PennyGamesTrigger,
                                                   PennyGamesPrize,
                                                   ActionGamesTrigger,
                                                   ActionGamesPrize
                                               )
                                               VALUES (
                                                   $DateTime,
                                                   $CreditsBefore,
                                                   $CreditsAfter,
                                                   $CreditsWagered,
                                                   $CreditsWon,
                                                   $CreditValue,
                                                   $Prize,
                                                   $Bonus,
                                                   $Total,
                                                   $Reel1Stop,
                                                   $Reel2Stop,
                                                   $Reel3Stop,
                                                   $Reel4Stop,
                                                   $Reel5Stop,
                                                   $WinningLines,
                                                   $PennyGamesTrigger,
                                                   $PennyGamesPrize,
                                                   $ActionGamesTrigger,
                                                   $ActionGamesPrize
                                               );";


                            command.Parameters.AddWithValue("$DateTime", ToDateTimeString(play.Date));
                            command.Parameters.AddWithValue("$CreditsBefore", play.CreditsBefore);
                            command.Parameters.AddWithValue("$CreditsAfter", play.CreditsAfter);
                            command.Parameters.AddWithValue("$CreditsWagered", play.CreditsWagered);
                            command.Parameters.AddWithValue("$CreditsWon", play.CreditsWon);
                            command.Parameters.AddWithValue("$CreditValue", play.CreditValue);
                            command.Parameters.AddWithValue("$Prize", play.Prize);
                            command.Parameters.AddWithValue("$Bonus", play.Bonus);
                            command.Parameters.AddWithValue("$Total", play.Total);
                            command.Parameters.AddWithValue("$Reel1Stop", play.Reel1Stop);
                            command.Parameters.AddWithValue("$Reel2Stop", play.Reel2Stop);
                            command.Parameters.AddWithValue("$Reel3Stop", play.Reel3Stop);
                            command.Parameters.AddWithValue("$Reel4Stop", play.Reel4Stop);
                            command.Parameters.AddWithValue("$Reel5Stop", play.Reel5Stop);
                            command.Parameters.AddWithValue("$WinningLines", JsonConvert.SerializeObject(play.WinningLines));
                            command.Parameters.AddWithValue("$PennyGamesTrigger", play.PennyGamesTrigger);
                            command.Parameters.AddWithValue("$PennyGamesPrize", play.PennyGamesPrize);
                            command.Parameters.AddWithValue("$ActionGamesTrigger", play.ActionGamesTrigger);
                            command.Parameters.AddWithValue("$ActionGamesPrize", play.ActionGamesPrize);


                            command.ExecuteReader();

                        }
                        // Clear the new last plays for avoid repetitions
                        EGMAccounting.GetInstance().plays.newlastplays.Clear();

                        // if there is no play, lets delete all persistence of play
                        if (EGMAccounting.GetInstance().plays.GetLastPlays().Count() == 0
                        && !EGMAccounting.GetInstance().plays.sync)
                        {
                            EGMAccounting.GetInstance().plays.sync = true;
                            var command = connection.CreateCommand();
                            command.CommandText = @"DELETE FROM EGMAccountingLastPlays;";
                            command.ExecuteNonQuery();
                        }
                        #endregion

                        // INSERT or DELETE
                        #region EGMAccountingHandpay
                        var AllHandpays = EGMAccounting.GetInstance().handpays.newHandpays.ToList();

                        // Insertion
                        foreach (HandpayTransaction handpay in AllHandpays)
                        {
                            var command = connection.CreateCommand();
                            command.CommandText = @"INSERT INTO EGMAccountingHandpay (
                                                                         DateTime,
                                                                         Type,
                                                                         Amount
                                                                     )
                                                                     VALUES (
                                                                         $DateTime,
                                                                         $Type,
                                                                         $Amount
                                                                     );";

                            command.Parameters.AddWithValue("$DateTime", ToDateTimeString(handpay.Date));
                            command.Parameters.AddWithValue("$Type", handpay.Type);
                            command.Parameters.AddWithValue("$Amount", handpay.Amount);


                            command.ExecuteNonQuery();

                        }
                        // Clear the new last handpays for avoid repetitions
                        EGMAccounting.GetInstance().handpays.newHandpays.Clear();

                        // if there is no handpay, lets delete all persistence of handpay
                        if (EGMAccounting.GetInstance().handpays.GetHandpayHistory().Count() == 0
                         && !EGMAccounting.GetInstance().handpays.sync)
                        {
                            EGMAccounting.GetInstance().handpays.sync = true;
                            var command = connection.CreateCommand();
                            command.CommandText = @"DELETE FROM EGMAccountingHandpay;";
                            command.ExecuteNonQuery();
                        }
                        #endregion

                        // UPDATE
                        #region EGMAccountingMeters

                        if (EGMAccountingMeters_CompareCurrentWithSnapshot() || forcepersist)
                        {
                            using (var command = connection.CreateCommand())
                            {
                                command.CommandText = @"UPDATE EGMAccountingMeters
                                           SET MeterCHK = $MeterCHK,
                                               M00 = $M00,
                                               M01 = $M01,
                                               M02 = $M02,
                                               M03 = $M03,
                                               M04 = $M04,
                                               M05 = $M05,
                                               M06 = $M06,
                                               M07 = $M07,
                                               M08 = $M08,
                                               M09 = $M09,
                                               M0A = $M0A,
                                               M0B = $M0B,
                                               M0C = $M0C,
                                               M0D = $M0D,
                                               M0E = $M0E,
                                               M0F = $M0F,
                                               M10 = $M10,
                                               M11 = $M11,
                                               M12 = $M12,
                                               M13 = $M13,
                                               M14 = $M14,
                                               M15 = $M15,
                                               M16 = $M16,
                                               M17 = $M17,
                                               M18 = $M18,
                                               M19 = $M19,
                                               M1A = $M1A,
                                               M1B = $M1B,
                                               M1C = $M1C,
                                               M1D = $M1D,
                                               M1E = $M1E,
                                               M1F = $M1F,
                                               M20 = $M20,
                                               M21 = $M21,
                                               M22 = $M22,
                                               M23 = $M23,
                                               M24 = $M24,
                                               M25 = $M25,
                                               M26 = $M26,
                                               M27 = $M27,
                                               M28 = $M28,
                                               M29 = $M29,
                                               M2A = $M2A,
                                               M2B = $M2B,
                                               M2C = $M2C,
                                               M2D = $M2D,
                                               M2E = $M2E,
                                               M2F = $M2F,
                                               M30 = $M30,
                                               M31 = $M31,
                                               M32 = $M32,
                                               M33 = $M33,
                                               M34 = $M34,
                                               M35 = $M35,
                                               M36 = $M36,
                                               M37 = $M37,
                                               M38 = $M38,
                                               M39 = $M39,
                                               M3A = $M3A,
                                               M3B = $M3B,
                                               M3C = $M3C,
                                               M3D = $M3D,
                                               M3E = $M3E,
                                               M3F = $M3F,
                                               M40 = $M40,
                                               M41 = $M41,
                                               M42 = $M42,
                                               M43 = $M43,
                                               M44 = $M44,
                                               M45 = $M45,
                                               M46 = $M46,
                                               M47 = $M47,
                                               M48 = $M48,
                                               M49 = $M49,
                                               M4A = $M4A,
                                               M4B = $M4B,
                                               M4C = $M4C,
                                               M4D = $M4D,
                                               M4E = $M4E,
                                               M4F = $M4F,
                                               M50 = $M50,
                                               M51 = $M51,
                                               M52 = $M52,
                                               M53 = $M53,
                                               M54 = $M54,
                                               M55 = $M55,
                                               M56 = $M56,
                                               M57 = $M57,
                                               M58 = $M58,
                                               M59 = $M59,
                                               M5A = $M5A,
                                               M5B = $M5B,
                                               M5C = $M5C,
                                               M5D = $M5D,
                                               M5E = $M5E,
                                               M5F = $M5F,
                                               M60 = $M60,
                                               M61 = $M61,
                                               M62 = $M62,
                                               M63 = $M63,
                                               M64 = $M64,
                                               M65 = $M65,
                                               M66 = $M66,
                                               M67 = $M67,
                                               M68 = $M68,
                                               M69 = $M69,
                                               M6A = $M6A,
                                               M6B = $M6B,
                                               M6C = $M6C,
                                               M6D = $M6D,
                                               M6E = $M6E,
                                               M6F = $M6F,
                                               M70 = $M70,
                                               M71 = $M71,
                                               M72 = $M72,
                                               M73 = $M73,
                                               M74 = $M74,
                                               M75 = $M75,
                                               M76 = $M76,
                                               M77 = $M77,
                                               M78 = $M78,
                                               M79 = $M79,
                                               M7A = $M7A,
                                               M7B = $M7B,
                                               M7C = $M7C,
                                               M7D = $M7D,
                                               M7E = $M7E,
                                               M7F = $M7F,
                                               M80 = $M80,
                                               M81 = $M81,
                                               M82 = $M82,
                                               M83 = $M83,
                                               M84 = $M84,
                                               M85 = $M85,
                                               M86 = $M86,
                                               M87 = $M87,
                                               M88 = $M88,
                                               M89 = $M89,
                                               M8A = $M8A,
                                               M8B = $M8B,
                                               M8C = $M8C,
                                               M8D = $M8D,
                                               M8E = $M8E,
                                               M8F = $M8F,
                                               M90 = $M90,
                                               M91 = $M91,
                                               M92 = $M92,
                                               M93 = $M93,
                                               MA0 = $MA0,
                                               MA1 = $MA1,
                                               MA2 = $MA2,
                                               MA3 = $MA3,
                                               MA4 = $MA4,
                                               MA5 = $MA5,
                                               MA6 = $MA6,
                                               MA7 = $MA7,
                                               MA8 = $MA8,
                                               MA9 = $MA9,
                                               MAA = $MAA,
                                               MAB = $MAB,
                                               MAC = $MAC,
                                               MAD = $MAD,
                                               MAE = $MAE,
                                               MAF = $MAF,
                                               MB0 = $MB0,
                                               MB1 = $MB1,
                                               MB8 = $MB8,
                                               MB9 = $MB9,
                                               MBA = $MBA,
                                               MBB = $MBB,
                                               MBC = $MBC,
                                               MBD = $MBD,
                                               MFA = $MFA,
                                               MFB = $MFB,
                                               MFC = $MFC,
                                               MFD = $MFD,
                                               MFE = $MFE,
                                               MFF = $MFF,
                                               MTotalBillMeterInDollars = $MTotalBillMeterInDollars,
                                               MTrueCoinIn = $MTrueCoinIn,
                                               MTrueCoinOut = $MTrueCoinOut,
                                               MBonusingDeductible = $MBonusingDeductible,
                                               MBonusingNoDeductibl = $MBonusingNoDeductibl,
                                               MBonusingWagerMatch = $MBonusingWagerMatch,
                                               MBasicTotalConIn = $MBasicTotalConIn,
                                               MBasicTotalCoinOut = $MBasicTotalCoinOut,
                                               MBasicTotalDrop = $MBasicTotalDrop,
                                               MBasicTotalJackPot = $MBasicTotalJackPot,
                                               MBasicGamesPlayed = $MBasicGamesPlayed,
                                               MBasicGamesWon = $MBasicGamesWon,
                                               MBasicSlotDoorOpen = $MBasicSlotDoorOpen,
                                               MBasicSlotDoorClose = $MBasicSlotDoorClose,
                                               MBasicPowerReset = $MBasicPowerReset,
                                               MBasicLogicDoorOpen = $MBasicLogicDoorOpen,
                                               MBasicLogicDoorClose = $MBasicLogicDoorClose,
                                               MTotalTilts = $MTotalTilts
                                         WHERE Game = 0;
                                        ";


                                command.Parameters.AddWithValue("$MeterCHK", IntegrityController.GetInstance().GetMeterCHK_EGMAccountingMeters(EGMAccounting.GetInstance().meters));
                                command.Parameters.AddWithValue("$M00", EGMAccounting.GetInstance().meters.M00);
                                command.Parameters.AddWithValue("$M01", EGMAccounting.GetInstance().meters.M01);
                                command.Parameters.AddWithValue("$M02", EGMAccounting.GetInstance().meters.M02);
                                command.Parameters.AddWithValue("$M03", EGMAccounting.GetInstance().meters.M03);
                                command.Parameters.AddWithValue("$M04", EGMAccounting.GetInstance().meters.M04);
                                command.Parameters.AddWithValue("$M05", EGMAccounting.GetInstance().meters.M05);
                                command.Parameters.AddWithValue("$M06", EGMAccounting.GetInstance().meters.M06);
                                command.Parameters.AddWithValue("$M07", EGMAccounting.GetInstance().meters.M07);
                                command.Parameters.AddWithValue("$M08", EGMAccounting.GetInstance().meters.M08);
                                command.Parameters.AddWithValue("$M09", EGMAccounting.GetInstance().meters.M09);
                                command.Parameters.AddWithValue("$M0A", EGMAccounting.GetInstance().meters.M0A);
                                command.Parameters.AddWithValue("$M0B", EGMAccounting.GetInstance().meters.M0B);
                                command.Parameters.AddWithValue("$M0C", EGMAccounting.GetInstance().meters.M0C);
                                command.Parameters.AddWithValue("$M0D", EGMAccounting.GetInstance().meters.M0D);
                                command.Parameters.AddWithValue("$M0E", EGMAccounting.GetInstance().meters.M0E);
                                command.Parameters.AddWithValue("$M0F", EGMAccounting.GetInstance().meters.M0F);
                                command.Parameters.AddWithValue("$M10", EGMAccounting.GetInstance().meters.M10);
                                command.Parameters.AddWithValue("$M11", EGMAccounting.GetInstance().meters.M11);
                                command.Parameters.AddWithValue("$M12", EGMAccounting.GetInstance().meters.M12);
                                command.Parameters.AddWithValue("$M13", EGMAccounting.GetInstance().meters.M13);
                                command.Parameters.AddWithValue("$M14", EGMAccounting.GetInstance().meters.M14);
                                command.Parameters.AddWithValue("$M15", EGMAccounting.GetInstance().meters.M15);
                                command.Parameters.AddWithValue("$M16", EGMAccounting.GetInstance().meters.M16);
                                command.Parameters.AddWithValue("$M17", EGMAccounting.GetInstance().meters.M17);
                                command.Parameters.AddWithValue("$M18", EGMAccounting.GetInstance().meters.M18);
                                command.Parameters.AddWithValue("$M19", EGMAccounting.GetInstance().meters.M19);
                                command.Parameters.AddWithValue("$M1A", EGMAccounting.GetInstance().meters.M1A);
                                command.Parameters.AddWithValue("$M1B", EGMAccounting.GetInstance().meters.M1B);
                                command.Parameters.AddWithValue("$M1C", EGMAccounting.GetInstance().meters.M1C);
                                command.Parameters.AddWithValue("$M1D", EGMAccounting.GetInstance().meters.M1D);
                                command.Parameters.AddWithValue("$M1E", EGMAccounting.GetInstance().meters.M1E);
                                command.Parameters.AddWithValue("$M1F", EGMAccounting.GetInstance().meters.M1F);
                                command.Parameters.AddWithValue("$M20", EGMAccounting.GetInstance().meters.M20);
                                command.Parameters.AddWithValue("$M21", EGMAccounting.GetInstance().meters.M21);
                                command.Parameters.AddWithValue("$M22", EGMAccounting.GetInstance().meters.M22);
                                command.Parameters.AddWithValue("$M23", EGMAccounting.GetInstance().meters.M23);
                                command.Parameters.AddWithValue("$M24", EGMAccounting.GetInstance().meters.M24);
                                command.Parameters.AddWithValue("$M25", EGMAccounting.GetInstance().meters.M25);
                                command.Parameters.AddWithValue("$M26", EGMAccounting.GetInstance().meters.M26);
                                command.Parameters.AddWithValue("$M27", EGMAccounting.GetInstance().meters.M27);
                                command.Parameters.AddWithValue("$M28", EGMAccounting.GetInstance().meters.M28);
                                command.Parameters.AddWithValue("$M29", EGMAccounting.GetInstance().meters.M29);
                                command.Parameters.AddWithValue("$M2A", EGMAccounting.GetInstance().meters.M2A);
                                command.Parameters.AddWithValue("$M2B", EGMAccounting.GetInstance().meters.M2B);
                                command.Parameters.AddWithValue("$M2C", EGMAccounting.GetInstance().meters.M2C);
                                command.Parameters.AddWithValue("$M2D", EGMAccounting.GetInstance().meters.M2D);
                                command.Parameters.AddWithValue("$M2E", EGMAccounting.GetInstance().meters.M2E);
                                command.Parameters.AddWithValue("$M2F", EGMAccounting.GetInstance().meters.M2F);
                                command.Parameters.AddWithValue("$M30", EGMAccounting.GetInstance().meters.M30);
                                command.Parameters.AddWithValue("$M31", EGMAccounting.GetInstance().meters.M31);
                                command.Parameters.AddWithValue("$M32", EGMAccounting.GetInstance().meters.M32);
                                command.Parameters.AddWithValue("$M33", EGMAccounting.GetInstance().meters.M33);
                                command.Parameters.AddWithValue("$M34", EGMAccounting.GetInstance().meters.M34);
                                command.Parameters.AddWithValue("$M35", EGMAccounting.GetInstance().meters.M35);
                                command.Parameters.AddWithValue("$M36", EGMAccounting.GetInstance().meters.M36);
                                command.Parameters.AddWithValue("$M37", EGMAccounting.GetInstance().meters.M37);
                                command.Parameters.AddWithValue("$M38", EGMAccounting.GetInstance().meters.M38);
                                command.Parameters.AddWithValue("$M39", EGMAccounting.GetInstance().meters.M39);
                                command.Parameters.AddWithValue("$M3A", EGMAccounting.GetInstance().meters.M3A);
                                command.Parameters.AddWithValue("$M3B", EGMAccounting.GetInstance().meters.M3B);
                                command.Parameters.AddWithValue("$M3C", EGMAccounting.GetInstance().meters.M3C);
                                command.Parameters.AddWithValue("$M3D", EGMAccounting.GetInstance().meters.M3D);
                                command.Parameters.AddWithValue("$M3E", EGMAccounting.GetInstance().meters.M3E);
                                command.Parameters.AddWithValue("$M3F", EGMAccounting.GetInstance().meters.M3F);
                                command.Parameters.AddWithValue("$M40", EGMAccounting.GetInstance().meters.M40);
                                command.Parameters.AddWithValue("$M41", EGMAccounting.GetInstance().meters.M41);
                                command.Parameters.AddWithValue("$M42", EGMAccounting.GetInstance().meters.M42);
                                command.Parameters.AddWithValue("$M43", EGMAccounting.GetInstance().meters.M43);
                                command.Parameters.AddWithValue("$M44", EGMAccounting.GetInstance().meters.M44);
                                command.Parameters.AddWithValue("$M45", EGMAccounting.GetInstance().meters.M45);
                                command.Parameters.AddWithValue("$M46", EGMAccounting.GetInstance().meters.M46);
                                command.Parameters.AddWithValue("$M47", EGMAccounting.GetInstance().meters.M47);
                                command.Parameters.AddWithValue("$M48", EGMAccounting.GetInstance().meters.M48);
                                command.Parameters.AddWithValue("$M49", EGMAccounting.GetInstance().meters.M49);
                                command.Parameters.AddWithValue("$M4A", EGMAccounting.GetInstance().meters.M4A);
                                command.Parameters.AddWithValue("$M4B", EGMAccounting.GetInstance().meters.M4B);
                                command.Parameters.AddWithValue("$M4C", EGMAccounting.GetInstance().meters.M4C);
                                command.Parameters.AddWithValue("$M4D", EGMAccounting.GetInstance().meters.M4D);
                                command.Parameters.AddWithValue("$M4E", EGMAccounting.GetInstance().meters.M4E);
                                command.Parameters.AddWithValue("$M4F", EGMAccounting.GetInstance().meters.M4F);
                                command.Parameters.AddWithValue("$M50", EGMAccounting.GetInstance().meters.M50);
                                command.Parameters.AddWithValue("$M51", EGMAccounting.GetInstance().meters.M51);
                                command.Parameters.AddWithValue("$M52", EGMAccounting.GetInstance().meters.M52);
                                command.Parameters.AddWithValue("$M53", EGMAccounting.GetInstance().meters.M53);
                                command.Parameters.AddWithValue("$M54", EGMAccounting.GetInstance().meters.M54);
                                command.Parameters.AddWithValue("$M55", EGMAccounting.GetInstance().meters.M55);
                                command.Parameters.AddWithValue("$M56", EGMAccounting.GetInstance().meters.M56);
                                command.Parameters.AddWithValue("$M57", EGMAccounting.GetInstance().meters.M57);
                                command.Parameters.AddWithValue("$M58", EGMAccounting.GetInstance().meters.M58);
                                command.Parameters.AddWithValue("$M59", EGMAccounting.GetInstance().meters.M59);
                                command.Parameters.AddWithValue("$M5A", EGMAccounting.GetInstance().meters.M5A);
                                command.Parameters.AddWithValue("$M5B", EGMAccounting.GetInstance().meters.M5B);
                                command.Parameters.AddWithValue("$M5C", EGMAccounting.GetInstance().meters.M5C);
                                command.Parameters.AddWithValue("$M5D", EGMAccounting.GetInstance().meters.M5D);
                                command.Parameters.AddWithValue("$M5E", EGMAccounting.GetInstance().meters.M5E);
                                command.Parameters.AddWithValue("$M5F", EGMAccounting.GetInstance().meters.M5F);
                                command.Parameters.AddWithValue("$M60", EGMAccounting.GetInstance().meters.M60);
                                command.Parameters.AddWithValue("$M61", EGMAccounting.GetInstance().meters.M61);
                                command.Parameters.AddWithValue("$M62", EGMAccounting.GetInstance().meters.M62);
                                command.Parameters.AddWithValue("$M63", EGMAccounting.GetInstance().meters.M63);
                                command.Parameters.AddWithValue("$M64", EGMAccounting.GetInstance().meters.M64);
                                command.Parameters.AddWithValue("$M65", EGMAccounting.GetInstance().meters.M65);
                                command.Parameters.AddWithValue("$M66", EGMAccounting.GetInstance().meters.M66);
                                command.Parameters.AddWithValue("$M67", EGMAccounting.GetInstance().meters.M67);
                                command.Parameters.AddWithValue("$M68", EGMAccounting.GetInstance().meters.M68);
                                command.Parameters.AddWithValue("$M69", EGMAccounting.GetInstance().meters.M69);
                                command.Parameters.AddWithValue("$M6A", EGMAccounting.GetInstance().meters.M6A);
                                command.Parameters.AddWithValue("$M6B", EGMAccounting.GetInstance().meters.M6B);
                                command.Parameters.AddWithValue("$M6C", EGMAccounting.GetInstance().meters.M6C);
                                command.Parameters.AddWithValue("$M6D", EGMAccounting.GetInstance().meters.M6D);
                                command.Parameters.AddWithValue("$M6E", EGMAccounting.GetInstance().meters.M6E);
                                command.Parameters.AddWithValue("$M6F", EGMAccounting.GetInstance().meters.M6F);
                                command.Parameters.AddWithValue("$M70", EGMAccounting.GetInstance().meters.M70);
                                command.Parameters.AddWithValue("$M71", EGMAccounting.GetInstance().meters.M71);
                                command.Parameters.AddWithValue("$M72", EGMAccounting.GetInstance().meters.M72);
                                command.Parameters.AddWithValue("$M73", EGMAccounting.GetInstance().meters.M73);
                                command.Parameters.AddWithValue("$M74", EGMAccounting.GetInstance().meters.M74);
                                command.Parameters.AddWithValue("$M75", EGMAccounting.GetInstance().meters.M75);
                                command.Parameters.AddWithValue("$M76", EGMAccounting.GetInstance().meters.M76);
                                command.Parameters.AddWithValue("$M77", EGMAccounting.GetInstance().meters.M77);
                                command.Parameters.AddWithValue("$M78", EGMAccounting.GetInstance().meters.M78);
                                command.Parameters.AddWithValue("$M79", EGMAccounting.GetInstance().meters.M79);
                                command.Parameters.AddWithValue("$M7A", EGMAccounting.GetInstance().meters.M7A);
                                command.Parameters.AddWithValue("$M7B", EGMAccounting.GetInstance().meters.M7B);
                                command.Parameters.AddWithValue("$M7C", EGMAccounting.GetInstance().meters.M7C);
                                command.Parameters.AddWithValue("$M7D", EGMAccounting.GetInstance().meters.M7D);
                                command.Parameters.AddWithValue("$M7E", EGMAccounting.GetInstance().meters.M7E);
                                command.Parameters.AddWithValue("$M7F", EGMAccounting.GetInstance().meters.M7F);
                                command.Parameters.AddWithValue("$M80", EGMAccounting.GetInstance().meters.M80);
                                command.Parameters.AddWithValue("$M81", EGMAccounting.GetInstance().meters.M81);
                                command.Parameters.AddWithValue("$M82", EGMAccounting.GetInstance().meters.M82);
                                command.Parameters.AddWithValue("$M83", EGMAccounting.GetInstance().meters.M83);
                                command.Parameters.AddWithValue("$M84", EGMAccounting.GetInstance().meters.M84);
                                command.Parameters.AddWithValue("$M85", EGMAccounting.GetInstance().meters.M85);
                                command.Parameters.AddWithValue("$M86", EGMAccounting.GetInstance().meters.M86);
                                command.Parameters.AddWithValue("$M87", EGMAccounting.GetInstance().meters.M87);
                                command.Parameters.AddWithValue("$M88", EGMAccounting.GetInstance().meters.M88);
                                command.Parameters.AddWithValue("$M89", EGMAccounting.GetInstance().meters.M89);
                                command.Parameters.AddWithValue("$M8A", EGMAccounting.GetInstance().meters.M8A);
                                command.Parameters.AddWithValue("$M8B", EGMAccounting.GetInstance().meters.M8B);
                                command.Parameters.AddWithValue("$M8C", EGMAccounting.GetInstance().meters.M8C);
                                command.Parameters.AddWithValue("$M8D", EGMAccounting.GetInstance().meters.M8D);
                                command.Parameters.AddWithValue("$M8E", EGMAccounting.GetInstance().meters.M8E);
                                command.Parameters.AddWithValue("$M8F", EGMAccounting.GetInstance().meters.M8F);
                                command.Parameters.AddWithValue("$M90", EGMAccounting.GetInstance().meters.M90);
                                command.Parameters.AddWithValue("$M91", EGMAccounting.GetInstance().meters.M91);
                                command.Parameters.AddWithValue("$M92", EGMAccounting.GetInstance().meters.M92);
                                command.Parameters.AddWithValue("$M93", EGMAccounting.GetInstance().meters.M93);
                                command.Parameters.AddWithValue("$MA0", EGMAccounting.GetInstance().meters.MA0);
                                command.Parameters.AddWithValue("$MA1", EGMAccounting.GetInstance().meters.MA1);
                                command.Parameters.AddWithValue("$MA2", EGMAccounting.GetInstance().meters.MA2);
                                command.Parameters.AddWithValue("$MA3", EGMAccounting.GetInstance().meters.MA3);
                                command.Parameters.AddWithValue("$MA4", EGMAccounting.GetInstance().meters.MA4);
                                command.Parameters.AddWithValue("$MA5", EGMAccounting.GetInstance().meters.MA5);
                                command.Parameters.AddWithValue("$MA6", EGMAccounting.GetInstance().meters.MA6);
                                command.Parameters.AddWithValue("$MA7", EGMAccounting.GetInstance().meters.MA7);
                                command.Parameters.AddWithValue("$MA8", EGMAccounting.GetInstance().meters.MA8);
                                command.Parameters.AddWithValue("$MA9", EGMAccounting.GetInstance().meters.MA9);
                                command.Parameters.AddWithValue("$MAA", EGMAccounting.GetInstance().meters.MAA);
                                command.Parameters.AddWithValue("$MAB", EGMAccounting.GetInstance().meters.MAB);
                                command.Parameters.AddWithValue("$MAC", EGMAccounting.GetInstance().meters.MAC);
                                command.Parameters.AddWithValue("$MAD", EGMAccounting.GetInstance().meters.MAD);
                                command.Parameters.AddWithValue("$MAE", EGMAccounting.GetInstance().meters.MAE);
                                command.Parameters.AddWithValue("$MAF", EGMAccounting.GetInstance().meters.MAF);
                                command.Parameters.AddWithValue("$MB0", EGMAccounting.GetInstance().meters.MB0);
                                command.Parameters.AddWithValue("$MB1", EGMAccounting.GetInstance().meters.MB1);
                                command.Parameters.AddWithValue("$MB8", EGMAccounting.GetInstance().meters.MB8);
                                command.Parameters.AddWithValue("$MB9", EGMAccounting.GetInstance().meters.MB9);
                                command.Parameters.AddWithValue("$MBA", EGMAccounting.GetInstance().meters.MBA);
                                command.Parameters.AddWithValue("$MBB", EGMAccounting.GetInstance().meters.MBB);
                                command.Parameters.AddWithValue("$MBC", EGMAccounting.GetInstance().meters.MBC);
                                command.Parameters.AddWithValue("$MBD", EGMAccounting.GetInstance().meters.MBD);
                                command.Parameters.AddWithValue("$MFA", EGMAccounting.GetInstance().meters.MFA);
                                command.Parameters.AddWithValue("$MFB", EGMAccounting.GetInstance().meters.MFB);
                                command.Parameters.AddWithValue("$MFC", EGMAccounting.GetInstance().meters.MFC);
                                command.Parameters.AddWithValue("$MFD", EGMAccounting.GetInstance().meters.MFD);
                                command.Parameters.AddWithValue("$MFE", EGMAccounting.GetInstance().meters.MFE);
                                command.Parameters.AddWithValue("$MFF", EGMAccounting.GetInstance().meters.MFF);
                                command.Parameters.AddWithValue("$MTotalBillMeterInDollars", EGMAccounting.GetInstance().meters.MTotalBillMeterInDollars);
                                command.Parameters.AddWithValue("$MTrueCoinIn", EGMAccounting.GetInstance().meters.MTrueCoinIn);
                                command.Parameters.AddWithValue("$MTrueCoinOut", EGMAccounting.GetInstance().meters.MTrueCoinOutMeter);
                                command.Parameters.AddWithValue("$MBonusingDeductible", EGMAccounting.GetInstance().meters.MBonusingDeductible);
                                command.Parameters.AddWithValue("$MBonusingNoDeductibl", EGMAccounting.GetInstance().meters.MBonusingNoDeductible);
                                command.Parameters.AddWithValue("$MBonusingWagerMatch", EGMAccounting.GetInstance().meters.MBonusingWagerMatch);
                                command.Parameters.AddWithValue("$MBasicTotalConIn", EGMAccounting.GetInstance().meters.MBasicTotalCoinIn);
                                command.Parameters.AddWithValue("$MBasicTotalCoinOut", EGMAccounting.GetInstance().meters.MBasicTotalCoinOut);
                                command.Parameters.AddWithValue("$MBasicTotalDrop", EGMAccounting.GetInstance().meters.MBasicTotalDrop);
                                command.Parameters.AddWithValue("$MBasicTotalJackPot", EGMAccounting.GetInstance().meters.MBasicTotalJackPot);
                                command.Parameters.AddWithValue("$MBasicGamesPlayed", EGMAccounting.GetInstance().meters.MBasicGamesPlayed);
                                command.Parameters.AddWithValue("$MBasicGamesWon", EGMAccounting.GetInstance().meters.MBasicGamesWon);
                                command.Parameters.AddWithValue("$MBasicSlotDoorOpen", EGMAccounting.GetInstance().meters.MBasicSlotDoorOpen);
                                command.Parameters.AddWithValue("$MBasicSlotDoorClose", EGMAccounting.GetInstance().meters.MBasicSlotDoorClose);
                                command.Parameters.AddWithValue("$MBasicPowerReset", EGMAccounting.GetInstance().meters.MBasicPowerReset);
                                command.Parameters.AddWithValue("$MBasicLogicDoorClose", EGMAccounting.GetInstance().meters.MBasicLogicDoorClose);
                                command.Parameters.AddWithValue("$MBasicLogicDoorOpen", EGMAccounting.GetInstance().meters.MBasicLogicDoorOpen);
                                command.Parameters.AddWithValue("$MBasicCashboxDoorOpen", EGMAccounting.GetInstance().meters.MBasicCashboxDoorOpen);
                                command.Parameters.AddWithValue("$MBasicCashboxDoorClose", EGMAccounting.GetInstance().meters.MBasicCashboxDoorClose);
                                command.Parameters.AddWithValue("$MBasicDropDoorOpen", EGMAccounting.GetInstance().meters.MBasicDropDoorOpen);
                                command.Parameters.AddWithValue("$MBasicDropDoorClose", EGMAccounting.GetInstance().meters.MBasicDropDoorClose);
                                command.Parameters.AddWithValue("$MBasicStackerOpen", EGMAccounting.GetInstance().meters.MBasicStackerOpen);
                                command.Parameters.AddWithValue("$MBasicStackerClose", EGMAccounting.GetInstance().meters.MBasicStackerClose);
                                command.Parameters.AddWithValue("$MBillsJammed", EGMAccounting.GetInstance().meters.MBillsJammed);
                                command.Parameters.AddWithValue("$MSASInterfaceError", EGMAccounting.GetInstance().meters.MSASInterfaceError);
                                command.Parameters.AddWithValue("$MTotalTilts", EGMAccounting.GetInstance().meters.MTotalTilts);

                                command.ExecuteNonQuery();

                                EGMAccountingMeters_Snapshot();
                            }
                        }
                        #endregion

                        // INSERT
                        #region EGMAccountingRamClears
                        var AllRamClears = EGMAccounting.GetInstance().ramclears.newRamClears;
                        // Insertion
                        foreach (RamClearLog ramClear in AllRamClears)
                        {
                            var command = connection.CreateCommand();
                            command.CommandText = @"INSERT INTO EGMAccountingRamClears (
                                               User,
                                               TimeStamp,
                                               CoinIn,
                                               CoinOut,
                                               GamePlays
                                           )
                                           VALUES (
                                               $User,
                                               $TimeStamp,
                                               $CoinIn,
                                               $CoinOut,
                                               $GamePlays
                                           );";

                            command.Parameters.AddWithValue("$User", ramClear.User);
                            command.Parameters.AddWithValue("$TimeStamp", ToDateTimeString(ramClear.TimeStamp));
                            command.Parameters.AddWithValue("$CoinIn", ramClear.CoinIn);
                            command.Parameters.AddWithValue("$CoinOut", ramClear.CoinOut);
                            command.Parameters.AddWithValue("$GamePlays", ramClear.GamePlays);

                            command.ExecuteNonQuery();

                        }
                        // Clear the new ram clears for avoid repetitions
                        EGMAccounting.GetInstance().ramclears.newRamClears.Clear();


                        // if there is no handpay, lets delete all persistence of handpay
                        if (EGMAccounting.GetInstance().ramclears.GetRamClearHistory().Count() == 0
                         && !EGMAccounting.GetInstance().ramclears.sync)
                        {
                            EGMAccounting.GetInstance().ramclears.sync = true;
                            var command = connection.CreateCommand();
                            command.CommandText = @"DELETE FROM EGMAccountingRamClears;";
                            command.ExecuteNonQuery();
                        }
                        #endregion

                    }
                    catch (Exception ex)
                    {
                        //  File.AppendAllLines("Log.txt", new string[] { DateTime.Now.ToString(), ex.Message });

                    }
                    finally
                    {
                        connection.Close();
                    }

                }
            }
        }
        private bool EGMAccountingMeters_CompareCurrentWithSnapshot()
        {
            bool different = false;

            if (localaccountingmeter.M00 != EGMAccounting.GetInstance().meters.M00) { different = true; }
            if (localaccountingmeter.M01 != EGMAccounting.GetInstance().meters.M01) { different = true; }
            if (localaccountingmeter.M02 != EGMAccounting.GetInstance().meters.M02) { different = true; }
            if (localaccountingmeter.M03 != EGMAccounting.GetInstance().meters.M03) { different = true; }
            if (localaccountingmeter.M04 != EGMAccounting.GetInstance().meters.M04) { different = true; }
            if (localaccountingmeter.M05 != EGMAccounting.GetInstance().meters.M05) { different = true; }
            if (localaccountingmeter.M06 != EGMAccounting.GetInstance().meters.M06) { different = true; }
            if (localaccountingmeter.M07 != EGMAccounting.GetInstance().meters.M07) { different = true; }
            if (localaccountingmeter.M08 != EGMAccounting.GetInstance().meters.M08) { different = true; }
            if (localaccountingmeter.M09 != EGMAccounting.GetInstance().meters.M09) { different = true; }
            if (localaccountingmeter.M0A != EGMAccounting.GetInstance().meters.M0A) { different = true; }
            if (localaccountingmeter.M0B != EGMAccounting.GetInstance().meters.M0B) { different = true; }
            if (localaccountingmeter.M0C != EGMAccounting.GetInstance().meters.M0C) { different = true; }
            if (localaccountingmeter.M0D != EGMAccounting.GetInstance().meters.M0D) { different = true; }
            if (localaccountingmeter.M0E != EGMAccounting.GetInstance().meters.M0E) { different = true; }
            if (localaccountingmeter.M0F != EGMAccounting.GetInstance().meters.M0F) { different = true; }
            if (localaccountingmeter.M10 != EGMAccounting.GetInstance().meters.M10) { different = true; }
            if (localaccountingmeter.M11 != EGMAccounting.GetInstance().meters.M11) { different = true; }
            if (localaccountingmeter.M12 != EGMAccounting.GetInstance().meters.M12) { different = true; }
            if (localaccountingmeter.M13 != EGMAccounting.GetInstance().meters.M13) { different = true; }
            if (localaccountingmeter.M14 != EGMAccounting.GetInstance().meters.M14) { different = true; }
            if (localaccountingmeter.M15 != EGMAccounting.GetInstance().meters.M15) { different = true; }
            if (localaccountingmeter.M16 != EGMAccounting.GetInstance().meters.M16) { different = true; }
            if (localaccountingmeter.M17 != EGMAccounting.GetInstance().meters.M17) { different = true; }
            if (localaccountingmeter.M18 != EGMAccounting.GetInstance().meters.M18) { different = true; }
            if (localaccountingmeter.M19 != EGMAccounting.GetInstance().meters.M19) { different = true; }
            if (localaccountingmeter.M1A != EGMAccounting.GetInstance().meters.M1A) { different = true; }
            if (localaccountingmeter.M1B != EGMAccounting.GetInstance().meters.M1B) { different = true; }
            if (localaccountingmeter.M1C != EGMAccounting.GetInstance().meters.M1C) { different = true; }
            if (localaccountingmeter.M1D != EGMAccounting.GetInstance().meters.M1D) { different = true; }
            if (localaccountingmeter.M1E != EGMAccounting.GetInstance().meters.M1E) { different = true; }
            if (localaccountingmeter.M1F != EGMAccounting.GetInstance().meters.M1F) { different = true; }
            if (localaccountingmeter.M20 != EGMAccounting.GetInstance().meters.M20) { different = true; }
            if (localaccountingmeter.M21 != EGMAccounting.GetInstance().meters.M21) { different = true; }
            if (localaccountingmeter.M22 != EGMAccounting.GetInstance().meters.M22) { different = true; }
            if (localaccountingmeter.M23 != EGMAccounting.GetInstance().meters.M23) { different = true; }
            if (localaccountingmeter.M24 != EGMAccounting.GetInstance().meters.M24) { different = true; }
            if (localaccountingmeter.M25 != EGMAccounting.GetInstance().meters.M25) { different = true; }
            if (localaccountingmeter.M26 != EGMAccounting.GetInstance().meters.M26) { different = true; }
            if (localaccountingmeter.M27 != EGMAccounting.GetInstance().meters.M27) { different = true; }
            if (localaccountingmeter.M28 != EGMAccounting.GetInstance().meters.M28) { different = true; }
            if (localaccountingmeter.M29 != EGMAccounting.GetInstance().meters.M29) { different = true; }
            if (localaccountingmeter.M2A != EGMAccounting.GetInstance().meters.M2A) { different = true; }
            if (localaccountingmeter.M2B != EGMAccounting.GetInstance().meters.M2B) { different = true; }
            if (localaccountingmeter.M2C != EGMAccounting.GetInstance().meters.M2C) { different = true; }
            if (localaccountingmeter.M2D != EGMAccounting.GetInstance().meters.M2D) { different = true; }
            if (localaccountingmeter.M2E != EGMAccounting.GetInstance().meters.M2E) { different = true; }
            if (localaccountingmeter.M2F != EGMAccounting.GetInstance().meters.M2F) { different = true; }
            if (localaccountingmeter.M30 != EGMAccounting.GetInstance().meters.M30) { different = true; }
            if (localaccountingmeter.M31 != EGMAccounting.GetInstance().meters.M31) { different = true; }
            if (localaccountingmeter.M32 != EGMAccounting.GetInstance().meters.M32) { different = true; }
            if (localaccountingmeter.M33 != EGMAccounting.GetInstance().meters.M33) { different = true; }
            if (localaccountingmeter.M34 != EGMAccounting.GetInstance().meters.M34) { different = true; }
            if (localaccountingmeter.M35 != EGMAccounting.GetInstance().meters.M35) { different = true; }
            if (localaccountingmeter.M36 != EGMAccounting.GetInstance().meters.M36) { different = true; }
            if (localaccountingmeter.M37 != EGMAccounting.GetInstance().meters.M37) { different = true; }
            if (localaccountingmeter.M38 != EGMAccounting.GetInstance().meters.M38) { different = true; }
            if (localaccountingmeter.M39 != EGMAccounting.GetInstance().meters.M39) { different = true; }
            if (localaccountingmeter.M3A != EGMAccounting.GetInstance().meters.M3A) { different = true; }
            if (localaccountingmeter.M3B != EGMAccounting.GetInstance().meters.M3B) { different = true; }
            if (localaccountingmeter.M3C != EGMAccounting.GetInstance().meters.M3C) { different = true; }
            if (localaccountingmeter.M3D != EGMAccounting.GetInstance().meters.M3D) { different = true; }
            if (localaccountingmeter.M3E != EGMAccounting.GetInstance().meters.M3E) { different = true; }
            if (localaccountingmeter.M3F != EGMAccounting.GetInstance().meters.M3F) { different = true; }
            if (localaccountingmeter.M40 != EGMAccounting.GetInstance().meters.M40) { different = true; }
            if (localaccountingmeter.M41 != EGMAccounting.GetInstance().meters.M41) { different = true; }
            if (localaccountingmeter.M42 != EGMAccounting.GetInstance().meters.M42) { different = true; }
            if (localaccountingmeter.M43 != EGMAccounting.GetInstance().meters.M43) { different = true; }
            if (localaccountingmeter.M44 != EGMAccounting.GetInstance().meters.M44) { different = true; }
            if (localaccountingmeter.M45 != EGMAccounting.GetInstance().meters.M45) { different = true; }
            if (localaccountingmeter.M46 != EGMAccounting.GetInstance().meters.M46) { different = true; }
            if (localaccountingmeter.M47 != EGMAccounting.GetInstance().meters.M47) { different = true; }
            if (localaccountingmeter.M48 != EGMAccounting.GetInstance().meters.M48) { different = true; }
            if (localaccountingmeter.M49 != EGMAccounting.GetInstance().meters.M49) { different = true; }
            if (localaccountingmeter.M4A != EGMAccounting.GetInstance().meters.M4A) { different = true; }
            if (localaccountingmeter.M4B != EGMAccounting.GetInstance().meters.M4B) { different = true; }
            if (localaccountingmeter.M4C != EGMAccounting.GetInstance().meters.M4C) { different = true; }
            if (localaccountingmeter.M4D != EGMAccounting.GetInstance().meters.M4D) { different = true; }
            if (localaccountingmeter.M4E != EGMAccounting.GetInstance().meters.M4E) { different = true; }
            if (localaccountingmeter.M4F != EGMAccounting.GetInstance().meters.M4F) { different = true; }
            if (localaccountingmeter.M50 != EGMAccounting.GetInstance().meters.M50) { different = true; }
            if (localaccountingmeter.M51 != EGMAccounting.GetInstance().meters.M51) { different = true; }
            if (localaccountingmeter.M52 != EGMAccounting.GetInstance().meters.M52) { different = true; }
            if (localaccountingmeter.M53 != EGMAccounting.GetInstance().meters.M53) { different = true; }
            if (localaccountingmeter.M54 != EGMAccounting.GetInstance().meters.M54) { different = true; }
            if (localaccountingmeter.M55 != EGMAccounting.GetInstance().meters.M55) { different = true; }
            if (localaccountingmeter.M56 != EGMAccounting.GetInstance().meters.M56) { different = true; }
            if (localaccountingmeter.M57 != EGMAccounting.GetInstance().meters.M57) { different = true; }
            if (localaccountingmeter.M58 != EGMAccounting.GetInstance().meters.M58) { different = true; }
            if (localaccountingmeter.M59 != EGMAccounting.GetInstance().meters.M59) { different = true; }
            if (localaccountingmeter.M5A != EGMAccounting.GetInstance().meters.M5A) { different = true; }
            if (localaccountingmeter.M5B != EGMAccounting.GetInstance().meters.M5B) { different = true; }
            if (localaccountingmeter.M5C != EGMAccounting.GetInstance().meters.M5C) { different = true; }
            if (localaccountingmeter.M5D != EGMAccounting.GetInstance().meters.M5D) { different = true; }
            if (localaccountingmeter.M5E != EGMAccounting.GetInstance().meters.M5E) { different = true; }
            if (localaccountingmeter.M5F != EGMAccounting.GetInstance().meters.M5F) { different = true; }
            if (localaccountingmeter.M60 != EGMAccounting.GetInstance().meters.M60) { different = true; }
            if (localaccountingmeter.M61 != EGMAccounting.GetInstance().meters.M61) { different = true; }
            if (localaccountingmeter.M62 != EGMAccounting.GetInstance().meters.M62) { different = true; }
            if (localaccountingmeter.M63 != EGMAccounting.GetInstance().meters.M63) { different = true; }
            if (localaccountingmeter.M64 != EGMAccounting.GetInstance().meters.M64) { different = true; }
            if (localaccountingmeter.M65 != EGMAccounting.GetInstance().meters.M65) { different = true; }
            if (localaccountingmeter.M66 != EGMAccounting.GetInstance().meters.M66) { different = true; }
            if (localaccountingmeter.M67 != EGMAccounting.GetInstance().meters.M67) { different = true; }
            if (localaccountingmeter.M68 != EGMAccounting.GetInstance().meters.M68) { different = true; }
            if (localaccountingmeter.M69 != EGMAccounting.GetInstance().meters.M69) { different = true; }
            if (localaccountingmeter.M6A != EGMAccounting.GetInstance().meters.M6A) { different = true; }
            if (localaccountingmeter.M6B != EGMAccounting.GetInstance().meters.M6B) { different = true; }
            if (localaccountingmeter.M6C != EGMAccounting.GetInstance().meters.M6C) { different = true; }
            if (localaccountingmeter.M6D != EGMAccounting.GetInstance().meters.M6D) { different = true; }
            if (localaccountingmeter.M6E != EGMAccounting.GetInstance().meters.M6E) { different = true; }
            if (localaccountingmeter.M6F != EGMAccounting.GetInstance().meters.M6F) { different = true; }
            if (localaccountingmeter.M70 != EGMAccounting.GetInstance().meters.M70) { different = true; }
            if (localaccountingmeter.M71 != EGMAccounting.GetInstance().meters.M71) { different = true; }
            if (localaccountingmeter.M72 != EGMAccounting.GetInstance().meters.M72) { different = true; }
            if (localaccountingmeter.M73 != EGMAccounting.GetInstance().meters.M73) { different = true; }
            if (localaccountingmeter.M74 != EGMAccounting.GetInstance().meters.M74) { different = true; }
            if (localaccountingmeter.M75 != EGMAccounting.GetInstance().meters.M75) { different = true; }
            if (localaccountingmeter.M76 != EGMAccounting.GetInstance().meters.M76) { different = true; }
            if (localaccountingmeter.M77 != EGMAccounting.GetInstance().meters.M77) { different = true; }
            if (localaccountingmeter.M78 != EGMAccounting.GetInstance().meters.M78) { different = true; }
            if (localaccountingmeter.M79 != EGMAccounting.GetInstance().meters.M79) { different = true; }
            if (localaccountingmeter.M7A != EGMAccounting.GetInstance().meters.M7A) { different = true; }
            if (localaccountingmeter.M7B != EGMAccounting.GetInstance().meters.M7B) { different = true; }
            if (localaccountingmeter.M7C != EGMAccounting.GetInstance().meters.M7C) { different = true; }
            if (localaccountingmeter.M7D != EGMAccounting.GetInstance().meters.M7D) { different = true; }
            if (localaccountingmeter.M7E != EGMAccounting.GetInstance().meters.M7E) { different = true; }
            if (localaccountingmeter.M7F != EGMAccounting.GetInstance().meters.M7F) { different = true; }
            if (localaccountingmeter.M80 != EGMAccounting.GetInstance().meters.M80) { different = true; }
            if (localaccountingmeter.M81 != EGMAccounting.GetInstance().meters.M81) { different = true; }
            if (localaccountingmeter.M82 != EGMAccounting.GetInstance().meters.M82) { different = true; }
            if (localaccountingmeter.M83 != EGMAccounting.GetInstance().meters.M83) { different = true; }
            if (localaccountingmeter.M84 != EGMAccounting.GetInstance().meters.M84) { different = true; }
            if (localaccountingmeter.M85 != EGMAccounting.GetInstance().meters.M85) { different = true; }
            if (localaccountingmeter.M86 != EGMAccounting.GetInstance().meters.M86) { different = true; }
            if (localaccountingmeter.M87 != EGMAccounting.GetInstance().meters.M87) { different = true; }
            if (localaccountingmeter.M88 != EGMAccounting.GetInstance().meters.M88) { different = true; }
            if (localaccountingmeter.M89 != EGMAccounting.GetInstance().meters.M89) { different = true; }
            if (localaccountingmeter.M8A != EGMAccounting.GetInstance().meters.M8A) { different = true; }
            if (localaccountingmeter.M8B != EGMAccounting.GetInstance().meters.M8B) { different = true; }
            if (localaccountingmeter.M8C != EGMAccounting.GetInstance().meters.M8C) { different = true; }
            if (localaccountingmeter.M8D != EGMAccounting.GetInstance().meters.M8D) { different = true; }
            if (localaccountingmeter.M8E != EGMAccounting.GetInstance().meters.M8E) { different = true; }
            if (localaccountingmeter.M8F != EGMAccounting.GetInstance().meters.M8F) { different = true; }
            if (localaccountingmeter.M90 != EGMAccounting.GetInstance().meters.M90) { different = true; }
            if (localaccountingmeter.M91 != EGMAccounting.GetInstance().meters.M91) { different = true; }
            if (localaccountingmeter.M92 != EGMAccounting.GetInstance().meters.M92) { different = true; }
            if (localaccountingmeter.M93 != EGMAccounting.GetInstance().meters.M93) { different = true; }
            if (localaccountingmeter.MA0 != EGMAccounting.GetInstance().meters.MA0) { different = true; }
            if (localaccountingmeter.MA1 != EGMAccounting.GetInstance().meters.MA1) { different = true; }
            if (localaccountingmeter.MA2 != EGMAccounting.GetInstance().meters.MA2) { different = true; }
            if (localaccountingmeter.MA3 != EGMAccounting.GetInstance().meters.MA3) { different = true; }
            if (localaccountingmeter.MA4 != EGMAccounting.GetInstance().meters.MA4) { different = true; }
            if (localaccountingmeter.MA5 != EGMAccounting.GetInstance().meters.MA5) { different = true; }
            if (localaccountingmeter.MA6 != EGMAccounting.GetInstance().meters.MA6) { different = true; }
            if (localaccountingmeter.MA7 != EGMAccounting.GetInstance().meters.MA7) { different = true; }
            if (localaccountingmeter.MA8 != EGMAccounting.GetInstance().meters.MA8) { different = true; }
            if (localaccountingmeter.MA9 != EGMAccounting.GetInstance().meters.MA9) { different = true; }
            if (localaccountingmeter.MAA != EGMAccounting.GetInstance().meters.MAA) { different = true; }
            if (localaccountingmeter.MAB != EGMAccounting.GetInstance().meters.MAB) { different = true; }
            if (localaccountingmeter.MAC != EGMAccounting.GetInstance().meters.MAC) { different = true; }
            if (localaccountingmeter.MAD != EGMAccounting.GetInstance().meters.MAD) { different = true; }
            if (localaccountingmeter.MAE != EGMAccounting.GetInstance().meters.MAE) { different = true; }
            if (localaccountingmeter.MAF != EGMAccounting.GetInstance().meters.MAF) { different = true; }
            if (localaccountingmeter.MB0 != EGMAccounting.GetInstance().meters.MB0) { different = true; }
            if (localaccountingmeter.MB1 != EGMAccounting.GetInstance().meters.MB1) { different = true; }
            if (localaccountingmeter.MB8 != EGMAccounting.GetInstance().meters.MB8) { different = true; }
            if (localaccountingmeter.MB9 != EGMAccounting.GetInstance().meters.MB9) { different = true; }
            if (localaccountingmeter.MBA != EGMAccounting.GetInstance().meters.MBA) { different = true; }
            if (localaccountingmeter.MBB != EGMAccounting.GetInstance().meters.MBB) { different = true; }
            if (localaccountingmeter.MBC != EGMAccounting.GetInstance().meters.MBC) { different = true; }
            if (localaccountingmeter.MBD != EGMAccounting.GetInstance().meters.MBD) { different = true; }
            if (localaccountingmeter.MFA != EGMAccounting.GetInstance().meters.MFA) { different = true; }
            if (localaccountingmeter.MFB != EGMAccounting.GetInstance().meters.MFB) { different = true; }
            if (localaccountingmeter.MFC != EGMAccounting.GetInstance().meters.MFC) { different = true; }
            if (localaccountingmeter.MFD != EGMAccounting.GetInstance().meters.MFD) { different = true; }
            if (localaccountingmeter.MFE != EGMAccounting.GetInstance().meters.MFE) { different = true; }
            if (localaccountingmeter.MFF != EGMAccounting.GetInstance().meters.MFF) { different = true; }
            if (localaccountingmeter.MTotalBillMeterInDollars != EGMAccounting.GetInstance().meters.MTotalBillMeterInDollars) { different = true; }
            if (localaccountingmeter.MTrueCoinIn != EGMAccounting.GetInstance().meters.MTrueCoinIn) { different = true; }
            if (localaccountingmeter.MTrueCoinOutMeter != EGMAccounting.GetInstance().meters.MTrueCoinOutMeter) { different = true; }
            if (localaccountingmeter.MBonusingDeductible != EGMAccounting.GetInstance().meters.MBonusingDeductible) { different = true; }
            if (localaccountingmeter.MBonusingNoDeductible != EGMAccounting.GetInstance().meters.MBonusingNoDeductible) { different = true; }
            if (localaccountingmeter.MBonusingWagerMatch != EGMAccounting.GetInstance().meters.MBonusingWagerMatch) { different = true; }
            if (localaccountingmeter.MBasicTotalCoinIn != EGMAccounting.GetInstance().meters.MBasicTotalCoinIn) { different = true; }
            if (localaccountingmeter.MBasicTotalCoinOut != EGMAccounting.GetInstance().meters.MBasicTotalCoinOut) { different = true; }
            if (localaccountingmeter.MBasicTotalDrop != EGMAccounting.GetInstance().meters.MBasicTotalDrop) { different = true; }
            if (localaccountingmeter.MBasicTotalJackPot != EGMAccounting.GetInstance().meters.MBasicTotalJackPot) { different = true; }
            if (localaccountingmeter.MBasicGamesPlayed != EGMAccounting.GetInstance().meters.MBasicGamesPlayed) { different = true; }
            if (localaccountingmeter.MBasicGamesWon != EGMAccounting.GetInstance().meters.MBasicGamesWon) { different = true; }
            if (localaccountingmeter.MBasicSlotDoorOpen != EGMAccounting.GetInstance().meters.MBasicSlotDoorOpen) { different = true; }
            if (localaccountingmeter.MBasicSlotDoorClose != EGMAccounting.GetInstance().meters.MBasicSlotDoorClose) { different = true; }
            if (localaccountingmeter.MBasicPowerReset != EGMAccounting.GetInstance().meters.MBasicPowerReset) { different = true; }
            if (localaccountingmeter.MBasicLogicDoorOpen != EGMAccounting.GetInstance().meters.MBasicLogicDoorOpen) { different = true; }
            if (localaccountingmeter.MBasicLogicDoorClose != EGMAccounting.GetInstance().meters.MBasicLogicDoorClose) { different = true; }
            if (localaccountingmeter.MBasicCashboxDoorOpen != EGMAccounting.GetInstance().meters.MBasicCashboxDoorOpen) { different = true; }
            if (localaccountingmeter.MBasicCashboxDoorClose != EGMAccounting.GetInstance().meters.MBasicCashboxDoorClose) { different = true; }
            if (localaccountingmeter.MBasicDropDoorOpen != EGMAccounting.GetInstance().meters.MBasicDropDoorOpen) { different = true; }
            if (localaccountingmeter.MBasicDropDoorClose != EGMAccounting.GetInstance().meters.MBasicDropDoorClose) { different = true; }
            if (localaccountingmeter.MBasicStackerOpen != EGMAccounting.GetInstance().meters.MBasicStackerOpen) { different = true; }
            if (localaccountingmeter.MBasicStackerClose != EGMAccounting.GetInstance().meters.MBasicStackerClose) { different = true; }
            if (localaccountingmeter.MBillsJammed != EGMAccounting.GetInstance().meters.MBillsJammed) { different = true; }
            if (localaccountingmeter.MSASInterfaceError != EGMAccounting.GetInstance().meters.MSASInterfaceError) { different = true; }
            if (localaccountingmeter.MTotalTilts != EGMAccounting.GetInstance().meters.MTotalTilts) { different = true; }


            return different;

        }
        private void EGMAccountingMeters_Snapshot()
        {
            localaccountingmeter.MeterCHK = EGMAccounting.GetInstance().meters.MeterCHK;
            localaccountingmeter.M00 = EGMAccounting.GetInstance().meters.M00;
            localaccountingmeter.M01 = EGMAccounting.GetInstance().meters.M01;
            localaccountingmeter.M02 = EGMAccounting.GetInstance().meters.M02;
            localaccountingmeter.M03 = EGMAccounting.GetInstance().meters.M03;
            localaccountingmeter.M04 = EGMAccounting.GetInstance().meters.M04;
            localaccountingmeter.M05 = EGMAccounting.GetInstance().meters.M05;
            localaccountingmeter.M06 = EGMAccounting.GetInstance().meters.M06;
            localaccountingmeter.M07 = EGMAccounting.GetInstance().meters.M07;
            localaccountingmeter.M08 = EGMAccounting.GetInstance().meters.M08;
            localaccountingmeter.M09 = EGMAccounting.GetInstance().meters.M09;
            localaccountingmeter.M0A = EGMAccounting.GetInstance().meters.M0A;
            localaccountingmeter.M0B = EGMAccounting.GetInstance().meters.M0B;
            localaccountingmeter.M0C = EGMAccounting.GetInstance().meters.M0C;
            localaccountingmeter.M0D = EGMAccounting.GetInstance().meters.M0D;
            localaccountingmeter.M0E = EGMAccounting.GetInstance().meters.M0E;
            localaccountingmeter.M0F = EGMAccounting.GetInstance().meters.M0F;
            localaccountingmeter.M10 = EGMAccounting.GetInstance().meters.M10;
            localaccountingmeter.M11 = EGMAccounting.GetInstance().meters.M11;
            localaccountingmeter.M12 = EGMAccounting.GetInstance().meters.M12;
            localaccountingmeter.M13 = EGMAccounting.GetInstance().meters.M13;
            localaccountingmeter.M14 = EGMAccounting.GetInstance().meters.M14;
            localaccountingmeter.M15 = EGMAccounting.GetInstance().meters.M15;
            localaccountingmeter.M16 = EGMAccounting.GetInstance().meters.M16;
            localaccountingmeter.M17 = EGMAccounting.GetInstance().meters.M17;
            localaccountingmeter.M18 = EGMAccounting.GetInstance().meters.M18;
            localaccountingmeter.M19 = EGMAccounting.GetInstance().meters.M19;
            localaccountingmeter.M1A = EGMAccounting.GetInstance().meters.M1A;
            localaccountingmeter.M1B = EGMAccounting.GetInstance().meters.M1B;
            localaccountingmeter.M1C = EGMAccounting.GetInstance().meters.M1C;
            localaccountingmeter.M1D = EGMAccounting.GetInstance().meters.M1D;
            localaccountingmeter.M1E = EGMAccounting.GetInstance().meters.M1E;
            localaccountingmeter.M1F = EGMAccounting.GetInstance().meters.M1F;
            localaccountingmeter.M20 = EGMAccounting.GetInstance().meters.M20;
            localaccountingmeter.M21 = EGMAccounting.GetInstance().meters.M21;
            localaccountingmeter.M22 = EGMAccounting.GetInstance().meters.M22;
            localaccountingmeter.M23 = EGMAccounting.GetInstance().meters.M23;
            localaccountingmeter.M24 = EGMAccounting.GetInstance().meters.M24;
            localaccountingmeter.M25 = EGMAccounting.GetInstance().meters.M25;
            localaccountingmeter.M26 = EGMAccounting.GetInstance().meters.M26;
            localaccountingmeter.M27 = EGMAccounting.GetInstance().meters.M27;
            localaccountingmeter.M28 = EGMAccounting.GetInstance().meters.M28;
            localaccountingmeter.M29 = EGMAccounting.GetInstance().meters.M29;
            localaccountingmeter.M2A = EGMAccounting.GetInstance().meters.M2A;
            localaccountingmeter.M2B = EGMAccounting.GetInstance().meters.M2B;
            localaccountingmeter.M2C = EGMAccounting.GetInstance().meters.M2C;
            localaccountingmeter.M2D = EGMAccounting.GetInstance().meters.M2D;
            localaccountingmeter.M2E = EGMAccounting.GetInstance().meters.M2E;
            localaccountingmeter.M2F = EGMAccounting.GetInstance().meters.M2F;
            localaccountingmeter.M30 = EGMAccounting.GetInstance().meters.M30;
            localaccountingmeter.M31 = EGMAccounting.GetInstance().meters.M31;
            localaccountingmeter.M32 = EGMAccounting.GetInstance().meters.M32;
            localaccountingmeter.M33 = EGMAccounting.GetInstance().meters.M33;
            localaccountingmeter.M34 = EGMAccounting.GetInstance().meters.M34;
            localaccountingmeter.M35 = EGMAccounting.GetInstance().meters.M35;
            localaccountingmeter.M36 = EGMAccounting.GetInstance().meters.M36;
            localaccountingmeter.M37 = EGMAccounting.GetInstance().meters.M37;
            localaccountingmeter.M38 = EGMAccounting.GetInstance().meters.M38;
            localaccountingmeter.M39 = EGMAccounting.GetInstance().meters.M39;
            localaccountingmeter.M3A = EGMAccounting.GetInstance().meters.M3A;
            localaccountingmeter.M3B = EGMAccounting.GetInstance().meters.M3B;
            localaccountingmeter.M3C = EGMAccounting.GetInstance().meters.M3C;
            localaccountingmeter.M3D = EGMAccounting.GetInstance().meters.M3D;
            localaccountingmeter.M3E = EGMAccounting.GetInstance().meters.M3E;
            localaccountingmeter.M3F = EGMAccounting.GetInstance().meters.M3F;
            localaccountingmeter.M40 = EGMAccounting.GetInstance().meters.M40;
            localaccountingmeter.M41 = EGMAccounting.GetInstance().meters.M41;
            localaccountingmeter.M42 = EGMAccounting.GetInstance().meters.M42;
            localaccountingmeter.M43 = EGMAccounting.GetInstance().meters.M43;
            localaccountingmeter.M44 = EGMAccounting.GetInstance().meters.M44;
            localaccountingmeter.M45 = EGMAccounting.GetInstance().meters.M45;
            localaccountingmeter.M46 = EGMAccounting.GetInstance().meters.M46;
            localaccountingmeter.M47 = EGMAccounting.GetInstance().meters.M47;
            localaccountingmeter.M48 = EGMAccounting.GetInstance().meters.M48;
            localaccountingmeter.M49 = EGMAccounting.GetInstance().meters.M49;
            localaccountingmeter.M4A = EGMAccounting.GetInstance().meters.M4A;
            localaccountingmeter.M4B = EGMAccounting.GetInstance().meters.M4B;
            localaccountingmeter.M4C = EGMAccounting.GetInstance().meters.M4C;
            localaccountingmeter.M4D = EGMAccounting.GetInstance().meters.M4D;
            localaccountingmeter.M4E = EGMAccounting.GetInstance().meters.M4E;
            localaccountingmeter.M4F = EGMAccounting.GetInstance().meters.M4F;
            localaccountingmeter.M50 = EGMAccounting.GetInstance().meters.M50;
            localaccountingmeter.M51 = EGMAccounting.GetInstance().meters.M51;
            localaccountingmeter.M52 = EGMAccounting.GetInstance().meters.M52;
            localaccountingmeter.M53 = EGMAccounting.GetInstance().meters.M53;
            localaccountingmeter.M54 = EGMAccounting.GetInstance().meters.M54;
            localaccountingmeter.M55 = EGMAccounting.GetInstance().meters.M55;
            localaccountingmeter.M56 = EGMAccounting.GetInstance().meters.M56;
            localaccountingmeter.M57 = EGMAccounting.GetInstance().meters.M57;
            localaccountingmeter.M58 = EGMAccounting.GetInstance().meters.M58;
            localaccountingmeter.M59 = EGMAccounting.GetInstance().meters.M59;
            localaccountingmeter.M5A = EGMAccounting.GetInstance().meters.M5A;
            localaccountingmeter.M5B = EGMAccounting.GetInstance().meters.M5B;
            localaccountingmeter.M5C = EGMAccounting.GetInstance().meters.M5C;
            localaccountingmeter.M5D = EGMAccounting.GetInstance().meters.M5D;
            localaccountingmeter.M5E = EGMAccounting.GetInstance().meters.M5E;
            localaccountingmeter.M5F = EGMAccounting.GetInstance().meters.M5F;
            localaccountingmeter.M60 = EGMAccounting.GetInstance().meters.M60;
            localaccountingmeter.M61 = EGMAccounting.GetInstance().meters.M61;
            localaccountingmeter.M62 = EGMAccounting.GetInstance().meters.M62;
            localaccountingmeter.M63 = EGMAccounting.GetInstance().meters.M63;
            localaccountingmeter.M64 = EGMAccounting.GetInstance().meters.M64;
            localaccountingmeter.M65 = EGMAccounting.GetInstance().meters.M65;
            localaccountingmeter.M66 = EGMAccounting.GetInstance().meters.M66;
            localaccountingmeter.M67 = EGMAccounting.GetInstance().meters.M67;
            localaccountingmeter.M68 = EGMAccounting.GetInstance().meters.M68;
            localaccountingmeter.M69 = EGMAccounting.GetInstance().meters.M69;
            localaccountingmeter.M6A = EGMAccounting.GetInstance().meters.M6A;
            localaccountingmeter.M6B = EGMAccounting.GetInstance().meters.M6B;
            localaccountingmeter.M6C = EGMAccounting.GetInstance().meters.M6C;
            localaccountingmeter.M6D = EGMAccounting.GetInstance().meters.M6D;
            localaccountingmeter.M6E = EGMAccounting.GetInstance().meters.M6E;
            localaccountingmeter.M6F = EGMAccounting.GetInstance().meters.M6F;
            localaccountingmeter.M70 = EGMAccounting.GetInstance().meters.M70;
            localaccountingmeter.M71 = EGMAccounting.GetInstance().meters.M71;
            localaccountingmeter.M72 = EGMAccounting.GetInstance().meters.M72;
            localaccountingmeter.M73 = EGMAccounting.GetInstance().meters.M73;
            localaccountingmeter.M74 = EGMAccounting.GetInstance().meters.M74;
            localaccountingmeter.M75 = EGMAccounting.GetInstance().meters.M75;
            localaccountingmeter.M76 = EGMAccounting.GetInstance().meters.M76;
            localaccountingmeter.M77 = EGMAccounting.GetInstance().meters.M77;
            localaccountingmeter.M78 = EGMAccounting.GetInstance().meters.M78;
            localaccountingmeter.M79 = EGMAccounting.GetInstance().meters.M79;
            localaccountingmeter.M7A = EGMAccounting.GetInstance().meters.M7A;
            localaccountingmeter.M7B = EGMAccounting.GetInstance().meters.M7B;
            localaccountingmeter.M7C = EGMAccounting.GetInstance().meters.M7C;
            localaccountingmeter.M7D = EGMAccounting.GetInstance().meters.M7D;
            localaccountingmeter.M7E = EGMAccounting.GetInstance().meters.M7E;
            localaccountingmeter.M7F = EGMAccounting.GetInstance().meters.M7F;
            localaccountingmeter.M80 = EGMAccounting.GetInstance().meters.M80;
            localaccountingmeter.M81 = EGMAccounting.GetInstance().meters.M81;
            localaccountingmeter.M82 = EGMAccounting.GetInstance().meters.M82;
            localaccountingmeter.M83 = EGMAccounting.GetInstance().meters.M83;
            localaccountingmeter.M84 = EGMAccounting.GetInstance().meters.M84;
            localaccountingmeter.M85 = EGMAccounting.GetInstance().meters.M85;
            localaccountingmeter.M86 = EGMAccounting.GetInstance().meters.M86;
            localaccountingmeter.M87 = EGMAccounting.GetInstance().meters.M87;
            localaccountingmeter.M88 = EGMAccounting.GetInstance().meters.M88;
            localaccountingmeter.M89 = EGMAccounting.GetInstance().meters.M89;
            localaccountingmeter.M8A = EGMAccounting.GetInstance().meters.M8A;
            localaccountingmeter.M8B = EGMAccounting.GetInstance().meters.M8B;
            localaccountingmeter.M8C = EGMAccounting.GetInstance().meters.M8C;
            localaccountingmeter.M8D = EGMAccounting.GetInstance().meters.M8D;
            localaccountingmeter.M8E = EGMAccounting.GetInstance().meters.M8E;
            localaccountingmeter.M8F = EGMAccounting.GetInstance().meters.M8F;
            localaccountingmeter.M90 = EGMAccounting.GetInstance().meters.M90;
            localaccountingmeter.M91 = EGMAccounting.GetInstance().meters.M91;
            localaccountingmeter.M92 = EGMAccounting.GetInstance().meters.M92;
            localaccountingmeter.M93 = EGMAccounting.GetInstance().meters.M93;
            localaccountingmeter.MA0 = EGMAccounting.GetInstance().meters.MA0;
            localaccountingmeter.MA1 = EGMAccounting.GetInstance().meters.MA1;
            localaccountingmeter.MA2 = EGMAccounting.GetInstance().meters.MA2;
            localaccountingmeter.MA3 = EGMAccounting.GetInstance().meters.MA3;
            localaccountingmeter.MA4 = EGMAccounting.GetInstance().meters.MA4;
            localaccountingmeter.MA5 = EGMAccounting.GetInstance().meters.MA5;
            localaccountingmeter.MA6 = EGMAccounting.GetInstance().meters.MA6;
            localaccountingmeter.MA7 = EGMAccounting.GetInstance().meters.MA7;
            localaccountingmeter.MA8 = EGMAccounting.GetInstance().meters.MA8;
            localaccountingmeter.MA9 = EGMAccounting.GetInstance().meters.MA9;
            localaccountingmeter.MAA = EGMAccounting.GetInstance().meters.MAA;
            localaccountingmeter.MAB = EGMAccounting.GetInstance().meters.MAB;
            localaccountingmeter.MAC = EGMAccounting.GetInstance().meters.MAC;
            localaccountingmeter.MAD = EGMAccounting.GetInstance().meters.MAD;
            localaccountingmeter.MAE = EGMAccounting.GetInstance().meters.MAE;
            localaccountingmeter.MAF = EGMAccounting.GetInstance().meters.MAF;
            localaccountingmeter.MB0 = EGMAccounting.GetInstance().meters.MB0;
            localaccountingmeter.MB1 = EGMAccounting.GetInstance().meters.MB1;
            localaccountingmeter.MB8 = EGMAccounting.GetInstance().meters.MB8;
            localaccountingmeter.MB9 = EGMAccounting.GetInstance().meters.MB9;
            localaccountingmeter.MBA = EGMAccounting.GetInstance().meters.MBA;
            localaccountingmeter.MBB = EGMAccounting.GetInstance().meters.MBB;
            localaccountingmeter.MBC = EGMAccounting.GetInstance().meters.MBC;
            localaccountingmeter.MBD = EGMAccounting.GetInstance().meters.MBD;
            localaccountingmeter.MFA = EGMAccounting.GetInstance().meters.MFA;
            localaccountingmeter.MFB = EGMAccounting.GetInstance().meters.MFB;
            localaccountingmeter.MFC = EGMAccounting.GetInstance().meters.MFC;
            localaccountingmeter.MFD = EGMAccounting.GetInstance().meters.MFD;
            localaccountingmeter.MFE = EGMAccounting.GetInstance().meters.MFE;
            localaccountingmeter.MFF = EGMAccounting.GetInstance().meters.MFF;
            localaccountingmeter.MTotalBillMeterInDollars = EGMAccounting.GetInstance().meters.MTotalBillMeterInDollars;
            localaccountingmeter.MTrueCoinIn = EGMAccounting.GetInstance().meters.MTrueCoinIn;
            localaccountingmeter.MTrueCoinOutMeter = EGMAccounting.GetInstance().meters.MTrueCoinOutMeter;
            localaccountingmeter.MBonusingDeductible = EGMAccounting.GetInstance().meters.MBonusingDeductible;
            localaccountingmeter.MBonusingNoDeductible = EGMAccounting.GetInstance().meters.MBonusingNoDeductible;
            localaccountingmeter.MBonusingWagerMatch = EGMAccounting.GetInstance().meters.MBonusingWagerMatch;
            localaccountingmeter.MBasicTotalCoinIn = EGMAccounting.GetInstance().meters.MBasicTotalCoinIn;
            localaccountingmeter.MBasicTotalCoinOut = EGMAccounting.GetInstance().meters.MBasicTotalCoinOut;
            localaccountingmeter.MBasicTotalDrop = EGMAccounting.GetInstance().meters.MBasicTotalDrop;
            localaccountingmeter.MBasicTotalJackPot = EGMAccounting.GetInstance().meters.MBasicTotalJackPot;
            localaccountingmeter.MBasicGamesPlayed = EGMAccounting.GetInstance().meters.MBasicGamesPlayed;
            localaccountingmeter.MBasicGamesWon = EGMAccounting.GetInstance().meters.MBasicGamesWon;
            localaccountingmeter.MBasicSlotDoorOpen = EGMAccounting.GetInstance().meters.MBasicSlotDoorOpen;
            localaccountingmeter.MBasicSlotDoorClose = EGMAccounting.GetInstance().meters.MBasicSlotDoorClose;
            localaccountingmeter.MBasicPowerReset = EGMAccounting.GetInstance().meters.MBasicPowerReset;
            localaccountingmeter.MBasicLogicDoorOpen = EGMAccounting.GetInstance().meters.MBasicLogicDoorOpen;
            localaccountingmeter.MBasicLogicDoorClose = EGMAccounting.GetInstance().meters.MBasicLogicDoorClose;
            localaccountingmeter.MBasicCashboxDoorOpen = EGMAccounting.GetInstance().meters.MBasicCashboxDoorOpen;
            localaccountingmeter.MBasicCashboxDoorClose = EGMAccounting.GetInstance().meters.MBasicCashboxDoorClose;
            localaccountingmeter.MBasicDropDoorOpen = EGMAccounting.GetInstance().meters.MBasicDropDoorOpen;
            localaccountingmeter.MBasicDropDoorClose = EGMAccounting.GetInstance().meters.MBasicDropDoorClose;
            localaccountingmeter.MBasicStackerOpen = EGMAccounting.GetInstance().meters.MBasicStackerOpen;
            localaccountingmeter.MBasicStackerClose = EGMAccounting.GetInstance().meters.MBasicStackerClose;
            localaccountingmeter.MBillsJammed = EGMAccounting.GetInstance().meters.MBillsJammed;
            localaccountingmeter.MSASInterfaceError = EGMAccounting.GetInstance().meters.MSASInterfaceError;
            localaccountingmeter.MTotalTilts = EGMAccounting.GetInstance().meters.MTotalTilts;
        }
        // Read the EGMAccounting
        internal void ReadEGMAccounting()
        {
            if (!(databaseon))
                return;
            using (var connection = new SqliteConnection($"Data Source={datalocation}"))
            {
                try
                {
                    connection.Open();

                    //// Configurar PRAGMA synchronous para garantizar la escritura física en disco
                    //using (var pragmaCommand = connection.CreateCommand())
                    //{
                    //    pragmaCommand.CommandText = "PRAGMA journal_mode = WAL; PRAGMA synchronous =  EXTRA;";
                    //    pragmaCommand.ExecuteNonQuery();
                    //}

                    #region EGMAccountingLastBills
                    using (SqliteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                                SELECT *
                                FROM EGMAccountingLastBills";

                        using (var reader = command.ExecuteReader())
                        {
                            Dictionary<string, int> columns = new Dictionary<string, int>();

                            for (var i = 0; i < reader.FieldCount; i++)
                            {
                                columns.Add(reader.GetName(i), i);
                            }

                            while (reader.Read())
                            {
                                try
                                {

                                    LastBill bill = new LastBill(FromDateTimeString(reader.GetString(columns["DateTime"])),
                                                                 reader.GetInt32(columns["Denomination"]));
                                    EGMAccountingModule.EGMAccounting.GetInstance().bills.AddNewBill(bill, false);
                                }
                                catch
                                {

                                }
                            }
                        }
                    }

                    #endregion

                    #region EGMAccountingLastPlays
                    using (SqliteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                                SELECT EGMAccountingLastPlays.ID,
                                       DateTime,
                                       CreditsBefore,
                                       CreditsAfter,
                                       CreditsWagered,
                                       CreditsWon,
                                       CreditValue,
                                       Prize,
                                       Bonus,
                                       Total,                                     
                                       Reel1Stop,
                                       Reel2Stop,
                                       Reel3Stop,
                                       Reel4Stop,
                                       Reel5Stop,
                                       WinningLines,
                                       PennyGamesTrigger,
                                       PennyGamesPrize,
                                       ActionGamesTrigger,
                                       ActionGamesPrize
                                  FROM EGMAccountingLastPlays;
                                ";

                        using (var reader = command.ExecuteReader())
                        {
                            Dictionary<string, int> columns = new Dictionary<string, int>();

                            for (var i = 0; i < reader.FieldCount; i++)
                            {
                                columns.Add(reader.GetName(i), i);
                            }

                            while (reader.Read())
                            {
                                try
                                {
                                    Winningline[] winninglines = new Winningline[] { };
                                    try
                                    {
                                        winninglines = JsonConvert.DeserializeObject<Winningline[]>(reader.GetString("WinningLines")).ToArray();
                                    }
                                    catch
                                    {

                                    }

                                    LastPlay play = new LastPlay(FromDateTimeString(reader.GetString(columns["DateTime"])),
                                                                 reader.GetDecimal("CreditsBefore"),
                                                                 reader.GetDecimal("CreditsAfter"),
                                                                 reader.GetDecimal("CreditsWagered"),
                                                                 reader.GetDecimal("CreditsWon"),
                                                                 0,
                                                                 0,
                                                                 0,
                                                                 0,
                                                                 reader.GetDecimal("CreditValue"),
                                                                 reader.GetDecimal("Prize"),
                                                                 reader.GetDecimal("Bonus"),
                                                                 reader.GetDecimal("Total"),
                                                                 reader.GetInt32("Reel1Stop"),
                                                                 reader.GetInt32("Reel2Stop"),
                                                                 reader.GetInt32("Reel3Stop"),
                                                                 reader.GetInt32("Reel4Stop"),
                                                                 reader.GetInt32("Reel5Stop"),
                                                                 winninglines,
                                                                 reader.GetBoolean("PennyGamesTrigger"),
                                                                 reader.GetDecimal("PennyGamesPrize"),
                                                                 reader.GetBoolean("ActionGamesTrigger"),
                                                                 reader.GetDecimal("ActionGamesPrize"));
                                    EGMAccountingModule.EGMAccounting.GetInstance().plays.AddNewPlay(play, false);
                                }
                                catch
                                {

                                }
                            }
                        }
                    }

                    #endregion

                    #region EGMAccountingAFTTransfers 
                    using (SqliteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                                SELECT *
                                FROM EGMAccountingAFTTransfers";

                        using (var reader = command.ExecuteReader())
                        {
                            Dictionary<string, int> columns = new Dictionary<string, int>();

                            for (var i = 0; i < reader.FieldCount; i++)
                            {
                                columns.Add(reader.GetName(i), i);
                            }

                            while (reader.Read())
                            {
                                try
                                {

                                    AccountingAFTTransfer transfer = new AccountingAFTTransfer(
                                                reader.GetString(columns["Type"]),
                                                FromDateTimeString(reader.GetString(columns["Date"])),
                                                reader.GetString(columns["DebitCredit"]),
                                                reader.GetDecimal(columns["CurrencyAmount"]),
                                                reader.GetInt32(columns["CashableAmount"]),
                                                reader.GetInt32(columns["RestrictedAmount"]),
                                                reader.GetInt32(columns["NonRestrictedAmount"])
                                        );
                                    EGMAccountingModule.EGMAccounting.GetInstance().transfers.AddNewTransfer(transfer, false);
                                }
                                catch
                                {

                                }
                            }
                        }
                    }

                    #endregion

                    #region EGMAccountingMeters 

                    using (SqliteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                                    SELECT *
                                    FROM EGMAccountingMeters";

                        using (var reader = command.ExecuteReader())
                        {
                            Dictionary<string, int> columns = new Dictionary<string, int>();

                            for (var i = 0; i < reader.FieldCount; i++)
                            {
                                columns.Add(reader.GetName(i), i);
                            }

                            while (reader.Read())
                            {
                                try
                                {
                                    try { EGMAccounting.GetInstance().meters.MeterCHK = reader.GetString(columns["MeterCHK"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M00 = reader.GetInt32(columns["M00"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M01 = reader.GetInt32(columns["M01"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M02 = reader.GetInt32(columns["M02"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M03 = reader.GetInt32(columns["M03"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M04 = reader.GetInt32(columns["M04"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M05 = reader.GetInt32(columns["M05"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M06 = reader.GetInt32(columns["M06"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M07 = reader.GetInt32(columns["M07"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M08 = reader.GetInt32(columns["M08"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M09 = reader.GetInt32(columns["M09"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M0A = reader.GetInt32(columns["M0A"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M0B = reader.GetInt32(columns["M0B"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M0C = reader.GetInt32(columns["M0C"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M0D = reader.GetInt32(columns["M0D"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M0E = reader.GetInt32(columns["M0E"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M0F = reader.GetInt32(columns["M0F"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M10 = reader.GetInt32(columns["M10"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M11 = reader.GetInt32(columns["M11"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M12 = reader.GetInt32(columns["M12"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M13 = reader.GetInt32(columns["M13"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M14 = reader.GetInt32(columns["M14"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M15 = reader.GetInt32(columns["M15"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M16 = reader.GetInt32(columns["M16"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M17 = reader.GetInt32(columns["M17"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M18 = reader.GetInt32(columns["M18"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M19 = reader.GetInt32(columns["M19"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M1A = reader.GetInt32(columns["M1A"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M1B = reader.GetInt32(columns["M1B"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M1C = reader.GetInt32(columns["M1C"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M1D = reader.GetInt32(columns["M1D"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M1E = reader.GetInt32(columns["M1E"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M1F = reader.GetInt32(columns["M1F"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M20 = reader.GetInt32(columns["M20"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M21 = reader.GetInt32(columns["M21"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M22 = reader.GetInt32(columns["M22"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M23 = reader.GetInt32(columns["M23"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M24 = reader.GetInt32(columns["M24"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M25 = reader.GetInt32(columns["M25"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M26 = reader.GetInt32(columns["M26"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M27 = reader.GetInt32(columns["M27"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M28 = reader.GetInt32(columns["M28"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M29 = reader.GetInt32(columns["M29"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M2A = reader.GetInt32(columns["M2A"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M2B = reader.GetInt32(columns["M2B"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M2C = reader.GetInt32(columns["M2C"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M2D = reader.GetInt32(columns["M2D"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M2E = reader.GetInt32(columns["M2E"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M2F = reader.GetInt32(columns["M2F"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M30 = reader.GetInt32(columns["M30"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M31 = reader.GetInt32(columns["M31"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M32 = reader.GetInt32(columns["M32"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M33 = reader.GetInt32(columns["M33"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M34 = reader.GetInt32(columns["M34"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M35 = reader.GetInt32(columns["M35"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M36 = reader.GetInt32(columns["M36"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M37 = reader.GetInt32(columns["M37"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M38 = reader.GetInt32(columns["M38"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M39 = reader.GetInt32(columns["M39"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M3A = reader.GetInt32(columns["M3A"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M3B = reader.GetInt32(columns["M3B"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M3C = reader.GetInt32(columns["M3C"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M3D = reader.GetInt32(columns["M3D"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M3E = reader.GetInt32(columns["M3E"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M3F = reader.GetInt32(columns["M3F"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M40 = reader.GetInt32(columns["M40"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M41 = reader.GetInt32(columns["M41"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M42 = reader.GetInt32(columns["M42"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M43 = reader.GetInt32(columns["M43"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M44 = reader.GetInt32(columns["M44"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M45 = reader.GetInt32(columns["M45"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M46 = reader.GetInt32(columns["M46"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M47 = reader.GetInt32(columns["M47"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M48 = reader.GetInt32(columns["M48"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M49 = reader.GetInt32(columns["M49"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M4A = reader.GetInt32(columns["M4A"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M4B = reader.GetInt32(columns["M4B"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M4C = reader.GetInt32(columns["M4C"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M4D = reader.GetInt32(columns["M4D"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M4E = reader.GetInt32(columns["M4E"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M4F = reader.GetInt32(columns["M4F"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M50 = reader.GetInt32(columns["M50"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M51 = reader.GetInt32(columns["M51"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M52 = reader.GetInt32(columns["M52"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M53 = reader.GetInt32(columns["M53"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M54 = reader.GetInt32(columns["M54"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M55 = reader.GetInt32(columns["M55"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M56 = reader.GetInt32(columns["M56"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M57 = reader.GetInt32(columns["M57"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M58 = reader.GetInt32(columns["M58"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M59 = reader.GetInt32(columns["M59"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M5A = reader.GetInt32(columns["M5A"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M5B = reader.GetInt32(columns["M5B"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M5C = reader.GetInt32(columns["M5C"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M5D = reader.GetInt32(columns["M5D"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M5E = reader.GetInt32(columns["M5E"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M5F = reader.GetInt32(columns["M5F"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M60 = reader.GetInt32(columns["M60"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M61 = reader.GetInt32(columns["M61"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M62 = reader.GetInt32(columns["M62"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M63 = reader.GetInt32(columns["M63"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M64 = reader.GetInt32(columns["M64"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M65 = reader.GetInt32(columns["M65"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M66 = reader.GetInt32(columns["M66"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M67 = reader.GetInt32(columns["M67"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M68 = reader.GetInt32(columns["M68"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M69 = reader.GetInt32(columns["M69"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M6A = reader.GetInt32(columns["M6A"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M6B = reader.GetInt32(columns["M6B"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M6C = reader.GetInt32(columns["M6C"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M6D = reader.GetInt32(columns["M6D"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M6E = reader.GetInt32(columns["M6E"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M6F = reader.GetInt32(columns["M6F"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M70 = reader.GetInt32(columns["M70"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M71 = reader.GetInt32(columns["M71"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M72 = reader.GetInt32(columns["M72"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M73 = reader.GetInt32(columns["M73"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M74 = reader.GetInt32(columns["M74"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M75 = reader.GetInt32(columns["M75"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M76 = reader.GetInt32(columns["M76"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M77 = reader.GetInt32(columns["M77"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M78 = reader.GetInt32(columns["M78"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M79 = reader.GetInt32(columns["M79"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M7A = reader.GetInt32(columns["M7A"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M7B = reader.GetInt32(columns["M7B"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M7C = reader.GetInt32(columns["M7C"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M7D = reader.GetInt32(columns["M7D"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M7E = reader.GetInt32(columns["M7E"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M7F = reader.GetInt32(columns["M7F"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M80 = reader.GetInt32(columns["M80"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M81 = reader.GetInt32(columns["M81"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M82 = reader.GetInt32(columns["M82"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M83 = reader.GetInt32(columns["M83"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M84 = reader.GetInt32(columns["M84"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M85 = reader.GetInt32(columns["M85"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M86 = reader.GetInt32(columns["M86"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M87 = reader.GetInt32(columns["M87"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M88 = reader.GetInt32(columns["M88"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M89 = reader.GetInt32(columns["M89"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M8A = reader.GetInt32(columns["M8A"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M8B = reader.GetInt32(columns["M8B"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M8C = reader.GetInt32(columns["M8C"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M8D = reader.GetInt32(columns["M8D"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M8E = reader.GetInt32(columns["M8E"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M8F = reader.GetInt32(columns["M8F"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M90 = reader.GetInt32(columns["M90"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M91 = reader.GetInt32(columns["M91"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M92 = reader.GetInt32(columns["M92"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.M93 = reader.GetInt32(columns["M93"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MA0 = reader.GetInt32(columns["MA0"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MA1 = reader.GetInt32(columns["MA1"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MA2 = reader.GetInt32(columns["MA2"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MA3 = reader.GetInt32(columns["MA3"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MA4 = reader.GetInt32(columns["MA4"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MA5 = reader.GetInt32(columns["MA5"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MA6 = reader.GetInt32(columns["MA6"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MA7 = reader.GetInt32(columns["MA7"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MA8 = reader.GetInt32(columns["MA8"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MA9 = reader.GetInt32(columns["MA9"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MAA = reader.GetInt32(columns["MAA"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MAB = reader.GetInt32(columns["MAB"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MAC = reader.GetInt32(columns["MAC"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MAD = reader.GetInt32(columns["MAD"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MAE = reader.GetInt32(columns["MAE"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MAF = reader.GetInt32(columns["MAF"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MB0 = reader.GetInt32(columns["MB0"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MB1 = reader.GetInt32(columns["MB1"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MB8 = reader.GetInt32(columns["MB8"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MB9 = reader.GetInt32(columns["MB9"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MBA = reader.GetInt32(columns["MBA"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MBB = reader.GetInt32(columns["MBB"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MBC = reader.GetInt32(columns["MBC"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MBD = reader.GetInt32(columns["MBD"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MFA = reader.GetInt32(columns["MFA"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MFB = reader.GetInt32(columns["MFB"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MFC = reader.GetInt32(columns["MFC"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MFD = reader.GetInt32(columns["MFD"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MFE = reader.GetInt32(columns["MFE"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MFF = reader.GetInt32(columns["MFF"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MTotalBillMeterInDollars = reader.GetInt32(columns["MTotalBillMeterInDollars"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MTrueCoinIn = reader.GetInt32(columns["MTrueCoinIn"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MTrueCoinOutMeter = reader.GetInt32(columns["MTrueCoinOut"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MBonusingDeductible = reader.GetInt32(columns["MBonusingDeductible"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MBonusingNoDeductible = reader.GetInt32(columns["MBonusingNoDeductibl"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MBonusingWagerMatch = reader.GetInt32(columns["MBonusingWagerMatch"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MBasicTotalCoinIn = reader.GetInt32(columns["MBasicTotalConIn"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MBasicTotalCoinOut = reader.GetInt32(columns["MBasicTotalCoinOut"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MBasicTotalDrop = reader.GetInt32(columns["MBasicTotalDrop"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MBasicTotalJackPot = reader.GetInt32(columns["MBasicTotalJackPot"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MBasicGamesPlayed = reader.GetInt32(columns["MBasicGamesPlayed"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MBasicGamesWon = reader.GetInt32(columns["MBasicGamesWon"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MBasicSlotDoorOpen = reader.GetInt32(columns["MBasicSlotDoorOpen"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MBasicSlotDoorClose = reader.GetInt32(columns["MBasicSlotDoorClose"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MBasicPowerReset = reader.GetInt32(columns["MBasicPowerReset"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MBasicLogicDoorOpen = reader.GetInt32(columns["MBasicLogicDoorOpen"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MBasicLogicDoorClose = reader.GetInt32(columns["MBasicLogicDoorClose"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MBasicCashboxDoorOpen = reader.GetInt32(columns["MBasicCashboxDoorOpen"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MBasicCashboxDoorClose = reader.GetInt32(columns["MBasicCashboxDoorClose"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MBasicDropDoorOpen = reader.GetInt32(columns["MBasicDropDoorOpen"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MBasicDropDoorClose = reader.GetInt32(columns["MBasicDropDoorClose"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MBasicStackerOpen = reader.GetInt32(columns["MBasicStackerOpen"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MBasicStackerClose = reader.GetInt32(columns["MBasicStackerClose"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MBillsJammed = reader.GetInt32(columns["MBillsJammed"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MSASInterfaceError = reader.GetInt32(columns["MSASInterfaceError"]); } catch { }
                                    try { EGMAccounting.GetInstance().meters.MTotalTilts = reader.GetInt32(columns["MTotalTilts"]); } catch { }

                                    IntegrityController.GetInstance().CheckEGMAccountingMeters(EGMAccounting.GetInstance().meters);

                                }
                                catch
                                {

                                }
                            }
                        }
                    }

                    #endregion

                    #region EGMAccountingRamClears

                    using (SqliteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                                SELECT *
                                FROM EGMAccountingRamClears";

                        using (var reader = command.ExecuteReader())
                        {
                            Dictionary<string, int> columns = new Dictionary<string, int>();

                            for (var i = 0; i < reader.FieldCount; i++)
                            {
                                columns.Add(reader.GetName(i), i);
                            }

                            while (reader.Read())
                            {
                                try
                                {

                                    RamClearLog ramClear = new RamClearLog(
                                                reader.GetString(columns["User"]),
                                                FromDateTimeString(reader.GetString(columns["TimeStamp"])),
                                                reader.GetDecimal(columns["CoinIn"]),
                                                reader.GetDecimal(columns["CoinOut"]),
                                                reader.GetInt32(columns["GamePlays"]));

                                    EGMAccountingModule.EGMAccounting.GetInstance().ramclears.AddNewRamClear(ramClear, false);
                                }
                                catch
                                {

                                }
                            }
                        }
                    }

                    #endregion

                    #region EGMAccountingSystemLogs

                    using (SqliteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                                SELECT *
                                FROM EGMAccountingSystemLogs";

                        using (var reader = command.ExecuteReader())
                        {
                            Dictionary<string, int> columns = new Dictionary<string, int>();

                            for (var i = 0; i < reader.FieldCount; i++)
                            {
                                columns.Add(reader.GetName(i), i);
                            }

                            while (reader.Read())
                            {
                                try
                                {

                                    SystemLog log = new SystemLog(
                                                               FromDateTimeString(reader.GetString(columns["TimeStamp"])),
                                                               reader.GetString(columns["Detail"]),
                                                               reader.GetString(columns["User"]));

                                    EGMAccountingModule.EGMAccounting.GetInstance().systemlogs.AddNewSystemLog(log, false);
                                }
                                catch
                                {

                                }
                            }
                        }
                    }

                    #endregion

                    #region EGMAccountingHandpay

                    using (SqliteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                                SELECT *
                                FROM EGMAccountingHandpay";

                        using (var reader = command.ExecuteReader())
                        {
                            Dictionary<string, int> columns = new Dictionary<string, int>();

                            for (var i = 0; i < reader.FieldCount; i++)
                            {
                                columns.Add(reader.GetName(i), i);
                            }

                            while (reader.Read())
                            {
                                try
                                {

                                    HandpayTransaction handpay = new HandpayTransaction(
                                                               reader.GetString(columns["Type"]),
                                                               FromDateTimeString(reader.GetString(columns["DateTime"])),
                                                               reader.GetDecimal(columns["Amount"]));

                                    EGMAccountingModule.EGMAccounting.GetInstance().handpays.AddNewHandpay(handpay, false);
                                }
                                catch
                                {

                                }
                            }
                        }
                    }

                    #endregion

                }
                catch
                {

                }
                finally
                {
                    connection.Close();
                }

            }
        }

        #endregion


        #region "AUX"


        private string ToDateTimeString(DateTime dt)
        {
            return dt.ToString(new CultureInfo("en-US"));
        }

        private DateTime FromDateTimeString(string dt)
        {
            return DateTime.Parse(dt, new CultureInfo("en-US"));
        }


        #endregion

        internal static EGMDataPersisterCTL GetInstance()
        {
            if (egmdataPersister_ == null)
            {
                egmdataPersister_ = new EGMDataPersisterCTL();

                localaccountingmeter = new EGMAccountingMeters();

            }
            return egmdataPersister_;
        }

    }
}
