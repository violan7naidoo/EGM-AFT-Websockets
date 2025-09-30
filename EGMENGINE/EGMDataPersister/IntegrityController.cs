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
using EGMENGINE.EGMDataPersisterModule;
using EGMENGINE.BillAccCTLModule.BillAccTypes;
using EGMENGINE.GUI.GAMETYPES;
using GXGGFMAPI;

namespace EGMENGINE.IntegrityControlModule
{
    internal class IntegrityController
    {
        private static IntegrityController integritycontroller_;
        internal bool RAMERROR = false;
        // Init EGMDataPersisterCTL
        protected IntegrityController()
        {

        }

        internal string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                return BitConverter.ToString(hashBytes).Replace("-", "");

            }
        }

        internal string GetMeterCHK_EGMAccountingMeters(EGMAccountingMeters meters)
        {
            string num = (meters.M0C /*credits*/
                        + meters.M00 /*Coin In*/
                        + meters.M01 /*Coin Out*/
                        + meters.M05 /*Games played*/).ToString();
            while (num.Length < 15)
            {
                num = '0' + num;
            }
            return new String(CreateMD5("P1rul0" + num).TakeLast(15).ToArray());

        }


        internal void CheckEGMAccountingMeters(EGMAccountingMeters meters)
        {
            if (GetMeterCHK_EGMAccountingMeters(meters) != meters.MeterCHK)
            {
                RAMERROR = true;
            }
        }

        internal string GetMeterCHK_EGMStatus(EGMStatus status)
        {
            string num = /*autospin*/"0;" +
                         status.betLines.ToString() + ";" +
                         status.betxline.ToString() + ";" +
                         (status.currentCashableAmount == 0 ? "0" : status.currentCashableAmount.ToString("F2").Replace(",",".")) + ";" +
                         (status.currentAmount == 0 ? "0" : status.currentAmount.ToString("F2").Replace(",",".")) + ";" +
                         (status.currentNonRestrictedAmount == 0 ? "0" : status.currentNonRestrictedAmount.ToString("F2").Replace(",",".")) + ";" +
                         (status.currentRestrictedAmount == 0 ? "0" : status.currentRestrictedAmount.ToString("F2").Replace(",",".")) + ";" +
                         (status.currentBet == 0 ? "0" : status.currentBet.ToString("F2").Replace(",",".")) + ";" +
                         (status.selectedCreditValue == 0 ? "0" : status.selectedCreditValue.ToString("F2").Replace(",", ".")) + ";" +
                         (!status.disabledByHost ? "0" : "1") + ";" +
                         (!status.fullramclearperformed ? "0" : "1") + ";" +
                         (!status.maintenanceMode ? "0" : "1") + ";" +
                         (!status.menuActive ? "0" : "1") + ";" +
                         (!status.o_autospinbuttonlightstatus ? "0" : "1") + ";" +
                         (!status.o_betbuttonlightstatus ? "0" : "1") + ";" +
                         (!status.o_cashoutbuttonlightstatus ? "0" : "1") + ";" +
                         (!status.o_helpbuttonlightstatus ? "0" : "1") + ";" +
                         (!status.o_linesbuttonlightstatus ? "0" : "1") + ";" +
                         (!status.o_maxbetbuttonlightstatus ? "0" : "1") + ";" +
                         (!status.o_servicebuttonlightstatus ? "0" : "1") + ";" +
                         (!status.o_spinbuttonlightstatus ? "0" : "1") + ";" +
                         (!status.o_tower1lightStatus ? "0" : "1") + ";" +
                         (!status.o_tower2lightStatus ? "0" : "1") + ";" +
                         (!status.setDateTime ? "0" : "1") + ";" +
                         (!status.soundEnabled ? "0" : "1") + ";" +
                         (!status.s_bellyDoorStatus ? "0" : "1") + ";" +
                         (!status.s_cardCageDoorStatus ? "0" : "1") + ";" +
                         (!status.s_cashBoxDoorStatus ? "0" : "1") + ";" +
                         (!status.s_dropBoxDoorStatus ? "0" : "1") + ";" +
                         (!status.s_logicDoorStatus ? "0" : "1") + ";" +
                         (!status.s_mainDoorStatus ? "0" : "1") + ";";


            while (num.Length < 15)
            {
                num = '0' + num;
            }

            return new String(CreateMD5("P1rul0" + num).TakeLast(15).ToArray());

        }

        internal void CheckEGMStatus(EGMStatus status)
        {
            if (GetMeterCHK_EGMStatus(status) != status.MeterCHK)
            {
             //   RAMERROR = true;
            }
        }

        internal string GetMeterCHK_EGMSlotPlay(EGMStatus_CurrentPlay play)
        {
            string num =
                 (play.spin.slotplay.actionGameWin == 0 ? "0" : play.spin.slotplay.actionGameWin.ToString("F2").Replace(",",".")) + ";" +
                play.spin.slotplay.actiongame_segment.ToString() + ";" +
                (play.spin.slotplay.awardedPennyGames == 0 ? "0" : play.spin.slotplay.awardedPennyGames.ToString("F2").Replace(",",".")) + ";" +
                (play.spin.slotplay.baseWin == 0 ? "0" : play.spin.slotplay.baseWin.ToString("F2").Replace(",",".")) + ";" +
                (play.spin.slotplay.totalBetAmount == 0 ? "0" : play.spin.slotplay.totalBetAmount.ToString("F2").Replace(",",".")) + ";" +
                (play.spin.slotplay.bonus == 0 ? "0" : play.spin.slotplay.bonus.ToString("F2").Replace(",",".")) + ";" +
                (play.spin.slotplay.creditsAfter == 0 ? "0" : play.spin.slotplay.creditsAfter.ToString("F2").Replace(",",".")) + ";" +
                (play.spin.slotplay.creditsAfterActionGameWin == 0 ? "0" : play.spin.slotplay.creditsAfterActionGameWin.ToString("F2").Replace(",",".")) + ";" +
                (play.spin.slotplay.creditsAfterBaseWin == 0 ? "0" : play.spin.slotplay.creditsAfterBaseWin.ToString("F2").Replace(",",".")) + ";" +
                (play.spin.slotplay.creditsAfterExpandedWin == 0 ? "0" : play.spin.slotplay.creditsAfterExpandedWin.ToString("F2").Replace(",",".")) + ";" +
                (play.spin.slotplay.creditsAfterMisteryWin == 0 ? "0" : play.spin.slotplay.creditsAfterMisteryWin.ToString("F2").Replace(",",".")) + ";" +
                (play.spin.slotplay.creditsAfterScatterWin == 0 ? "0" : play.spin.slotplay.creditsAfterScatterWin.ToString("F2").Replace(",",".")) + ";" +
                (play.spin.slotplay.creditsBefore == 0 ? "0" : play.spin.slotplay.creditsBefore.ToString("F2").Replace(",",".")) + ";" +
                (play.spin.slotplay.creditsOnPlay == 0 ? "0" : play.spin.slotplay.creditsOnPlay.ToString("F2").Replace(",",".")) + ";" +
                (play.spin.slotplay.creditValue == 0 ? "0" : play.spin.slotplay.creditValue.ToString("F2").Replace(",",".")) + ";" +
                (play.spin.slotplay.exceededCredits == 0 ? "0" : play.spin.slotplay.exceededCredits.ToString("F2").Replace(",",".")) + ";" +
                (!play.spin.slotplay.Finished ? "0" : "1") + ";" +
                (play.spin.slotplay.misteryWin == 0 ? "0" : play.spin.slotplay.misteryWin.ToString("F2").Replace(",",".")) + ";" +
                (play.spin.slotplay.winCredits == 0 ? "0" : play.spin.slotplay.winCredits.ToString("F2").Replace(",",".")) + ";";

            while (num.Length < 15)
            {
                num = '0' + num;
            }

            return new String(CreateMD5("P1rul0" + num).TakeLast(15).ToArray());

        }

        internal void CheckEGMSlotPlay(EGMStatus_CurrentPlay play)
        {
            if (GetMeterCHK_EGMSlotPlay(play) != play.MeterCHK)
            {
                RAMERROR = true;
            }
        }


        internal static IntegrityController GetInstance()
        {
            if (integritycontroller_ == null)
            {
                integritycontroller_ = new IntegrityController();


            }
            return integritycontroller_;
        }
    }
}
