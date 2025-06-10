/*
    ScheduleOne Deal Summary Mod
    Copyright (C) 2025 fatboyfat_uk (email github@fatboyfat.uk)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using MelonLoader;
using ScheduleOne.DevUtilities;
using ScheduleOne.Quests;
using ScheduleOne.UI;
using ScheduleOne.UI.Phone;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScheduleOne.DealSummary.Mono
{
    public class Core : MelonMod
    {
        private const string MainSceneName = "Main";

        private DealSummaryQuest dealSummaryQuest;

        private void CreateDealSummaryQuest()
        {
            if (dealSummaryQuest != null)
                return;

            var questManager = QuestManager.Instance;

            var gameObject = new GameObject("DealSummaryQuest");
            gameObject.transform.SetParent(questManager.QuestContainer, false);
            dealSummaryQuest = gameObject.AddComponent<DealSummaryQuest>();
            dealSummaryQuest.Initialize();
        }

        public override void OnUpdate()
        {
            bool gameStarted = SceneManager.GetActiveScene().name == MainSceneName;
            if (gameStarted && dealSummaryQuest == null)
            {
                if (PlayerSingleton<JournalApp>.Instance?.QuestHUDUIPrefab == null)
                    LoggerInstance.Warning($"QuestHUDUIPrefab is null, deferring creation of {nameof(DealSummaryQuest)}.");
                else if (HUD.Instance == null)
                    LoggerInstance.Warning($"HUD instance is null, deferring creation of {nameof(DealSummaryQuest)}.");
                else if (HUD.Instance.QuestEntryContainer == null)
                    LoggerInstance.Warning($"QuestEntryContainer is null, deferring creation of {nameof(DealSummaryQuest)}.");
                else if (QuestManager.Instance == null)
                    LoggerInstance.Warning($"QuestManager instance is null, deferring creation of {nameof(DealSummaryQuest)}.");
                else if (QuestManager.Instance.QuestContainer == null)
                    LoggerInstance.Warning($"QuestContainer is null, deferring creation of {nameof(DealSummaryQuest)}.");
                else
                {
                    LoggerInstance.Msg($"Calling {nameof(CreateDealSummaryQuest)}");
                    CreateDealSummaryQuest();
                }
            }
        }
    }
}