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
using ScheduleOne.GameTime;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Quests;
using ScheduleOne.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace ScheduleOne.DealSummary.Mono
{
    internal class DealSummaryQuest : Quest
    {
        private static MelonLogger.Instance Logger => new MelonLogger.Instance(nameof(DealSummaryQuest));

        public override bool ShouldSave() => false;
        protected override bool ShouldShowJournalEntry() => false;

        internal DealSummaryQuest() : base()
        {

        }

        public override void InitializeQuest(string title, string description, QuestEntryData[] entries, string guid)
        {
            var iconGameObject = new GameObject(name: "QuestIcon");
            iconGameObject.transform.SetParent(transform, false);
            IconPrefab = iconGameObject.AddComponent<RectTransform>();

            onQuestBegin = new UnityEvent();
            onQuestEnd = new UnityEvent<EQuestState>();
            onTrackChange = new UnityEvent<bool>();
            onActiveState = new UnityEvent();
            onComplete = new UnityEvent();
            onInitialComplete = new UnityEvent();

            base.InitializeQuest(title, description, entries, guid);
            gameObject.name = "Deal Summary Quest";
        }

        public void Initialize(List<DealProduct> products)
        {
            InitializeQuest("Scheduled deals:", "A summary of the deal products.", Array.Empty<QuestEntryData>(), Guid.NewGuid().ToString());

            foreach (var entry in Entries)
                entry.SetState(EQuestState.Cancelled);

            Entries.Clear();

            foreach (var product in products)
            {
                var questEntry = CreateQuestEntry(product);
                Entries.Add(questEntry);
            }

            SetIsTracked(true);
            SetupHudUI();

            hudUI.transform.SetAsFirstSibling();
        }

        public bool TryUpdateProducts(List<DealProduct> products, bool force = false)
        {
            var productsToAdd = products.Count(p => !Entries.Any(e => e.gameObject.GetComponent<DealProduct>().ProductID == p.ProductID));
            var entriesToRemove = Entries.Count(e => !products.Any(p => p.ProductID == e.gameObject.GetComponent<DealProduct>().ProductID));

            if (force)
            {
                Logger.Msg("Disabling Hud UI layout");
                hudUI.hudUILayout.enabled = false;
                foreach (var entry in Entries)
                {
                    Logger.Msg($"Forcing quest entry state to Cancelled: {entry.Title}");
                    typeof(QuestEntry).GetField("state", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(entry, EQuestState.Cancelled);

                    var entryUI = typeof(QuestEntry).GetField("entryUI", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(entry) as QuestEntryHUDUI;
                    if (entryUI != null)
                    {
                        Logger.Msg($"Setting quest entry HUD UI to inactive for {entry.Title}");
                        entryUI.gameObject.SetActive(false);
                    }
                }
                Entries.Clear();
            }

            if (!force && (productsToAdd > 0 || entriesToRemove > 0))
            {
                return false;
            }
            else
            {
                foreach (var product in products)
                {
                    var entry = Entries.SingleOrDefault(e => e.gameObject.GetComponent<DealProduct>().ProductID == product.ProductID);
                    if (entry == null && force)
                    {
                        Logger.Msg($"Adding new quest entry for product: {product.ProductName}");
                        entry = CreateQuestEntry(product);
                        Entries.Add(entry);
                    }
                    else if (entry == null)
                    {
                        Logger.Error($"No existing quest entry found for product: {product.ProductName}");
                    }
                    else if (entry.Title != product.QuestTitle)
                    {
                        Logger.Msg($"Updating quest entry title from '{entry.Title}' to '{product.QuestTitle}'");
                        UpdateQuestEntry(entry, product.QuestTitle);
                    }
                }

                if (QuestState != EQuestState.Active)
                {
                    Logger.Msg($"Forcing quest state to Active");
                    SetQuestState(EQuestState.Active);
                }

                if (force)
                {
                    Logger.Msg("Re-enabling Hud UI layout");
                    hudUI.hudUILayout.enabled = true;
                }

                UpdateHUDUI();

                return true;
            }
        }

        private void UpdateQuestEntry(QuestEntry questEntry, string newTitle)
        {
            var oldEntryUI = hudUI.EntryContainer.GetComponentsInChildren<QuestEntryHUDUI>(true).FirstOrDefault(e => e.QuestEntry == questEntry);
            if (oldEntryUI == null)
            {
                Logger.Error($"No HUD UI found for quest entry: {questEntry.Title}");
                return;
            }
            CoroutineService.Instance.StartCoroutine(Routine(questEntry, oldEntryUI, newTitle));
        
            static IEnumerator Routine(QuestEntry questEntry, QuestEntryHUDUI entryHudUI, string newTitle)
            {
                Logger.Msg("Playing exit animation for quest entry: " + questEntry.Title);
                entryHudUI.Animation.Play("Quest entry exit");
                yield return new WaitForEndOfFrame();
                while (entryHudUI.Animation.isPlaying)
                {
                    yield return new WaitForEndOfFrame();
                }
                Logger.Msg("Setting quest entry title: " + questEntry.Title);
                questEntry.SetEntryTitle(newTitle);
                Logger.Msg("Playing enter animation for quest entry: " + questEntry.Title);
                entryHudUI.Animation.Play("Quest entry enter");
            }
        }

        private QuestEntry CreateQuestEntry(DealProduct product)
        {
            var questEntryData = new QuestEntryData(product.QuestTitle, EQuestState.Active);

            GameObject gameObject = new GameObject(questEntryData.Name);
            gameObject.transform.SetParent(transform);

            QuestEntry questEntry = gameObject.AddComponent<QuestEntry>();
            questEntry.SetData(questEntryData);
            questEntry.CompleteParentQuest = false;

            var dealProduct = gameObject.AddComponent<DealProduct>();
            dealProduct.CopyFrom(product);

            return questEntry;
        }
    }
}
