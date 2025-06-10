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
using UnityEngine;
using UnityEngine.Events;

namespace ScheduleOne.DealSummary.Mono
{
    internal class DealSummaryQuest : Quest
    {
        private static MelonLogger.Instance Logger => new MelonLogger.Instance(nameof(DealSummaryQuest));

        private static List<DealProduct> EnumerateCurrentDealProducts()
        {
            Dictionary<string, int> productCounts = new Dictionary<string, int>();

            var contracts = Contract.Contracts ?? new List<Contract>();

            foreach (var contract in contracts)
            {
                if (contract.Dealer != null)
                    continue; // Only consider contracts where the player is the dealer

                foreach (var product in contract.ProductList.entries)
                {
                    productCounts.TryAdd(product.ProductID, 0);
                    productCounts[product.ProductID] += product.Quantity;
                }
            }

            return productCounts
                .Select(kvp => new DealProduct(Registry.GetItem(kvp.Key).Name, kvp.Value))
                .ToList();
        }

        public override bool ShouldSave() => false;
        protected override bool ShouldShowJournalEntry() => false;

        internal DealSummaryQuest() : base()
        {

        }

        public void Initialize()
        {
            GUID = Guid.NewGuid();
            GUIDManager.RegisterObject(this);

            title = "Scheduled deal totals:";
            gameObject.name = title;

            Description = "This quest is used to display the product totals in the UI.";
            QuestState = EQuestState.Inactive;

            var iconGameObject = new GameObject(name: "QuestIcon");
            iconGameObject.transform.SetParent(transform, false);
            IconPrefab = iconGameObject.AddComponent<RectTransform>();

            onQuestBegin = new UnityEvent();
            onQuestEnd = new UnityEvent<EQuestState>();
            onTrackChange = new UnityEvent<bool>();
            onActiveState = new UnityEvent();
            onComplete = new UnityEvent();
            onInitialComplete = new UnityEvent();

            IsTracked = true;

            NetworkSingleton<TimeManager>.Instance.onTick += UpdateProducts;
            SetupHudUI();
            gameObject.SetActive(true);
        }

        public void UpdateProducts()
        {
            var products = EnumerateCurrentDealProducts();
            var processedEntries = new List<QuestEntry>(Entries.Count);

            for (int i = 0; i < products.Count; i++)
            {
                var newTitle = $"{products[i].Quantity}x {products[i].ProductName}";
                var entry = Entries.FirstOrDefault(e => e.Title.Contains(products[i].ProductName));
                if (entry == null)
                {
                    Logger.Msg($"Creating quest entry: {newTitle}");
                    var newEntry = CreateQuestEntry(newTitle);
                    Entries.Add(newEntry);
                    processedEntries.Add(newEntry);
                }
                else if (entry.Title != newTitle || entry.State == EQuestState.Inactive)
                {
                    Logger.Msg($"Updating {entry.State} quest entry: {entry.Title} to {newTitle}");
                    UpdateQuestEntry(entry, newTitle);
                    processedEntries.Add(entry);
                }
                else
                {
                    Logger.Msg($"Quest entry unchanged: {entry.Title}");
                    processedEntries.Add(entry);
                }
            }

            foreach (var entry in Entries)
            {
                if (!processedEntries.Contains(entry) && entry.State != EQuestState.Inactive)
                {
                    Logger.Msg($"Setting quest entry inactive: {entry.Title}");
                    entry.SetState(EQuestState.Inactive);
                }
            }

            SetQuestState(ActiveEntryCount > 0 ? EQuestState.Active : EQuestState.Inactive);
            UpdateHUDUI();
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
                entryHudUI.Animation.Play("Quest entry exit");
                yield return new WaitForEndOfFrame();
                while (entryHudUI.Animation.isPlaying)
                {
                    yield return new WaitForEndOfFrame();
                }
                questEntry.SetEntryTitle(newTitle);
                entryHudUI.Animation.Play("Quest entry enter");
            }
        }

        private QuestEntry CreateQuestEntry(string title)
        {
            var questEntryData = new QuestEntryData(title, EQuestState.Active);

            GameObject gameObject = new GameObject(questEntryData.Name);
            gameObject.transform.SetParent(transform);
            QuestEntry questEntry = gameObject.AddComponent<QuestEntry>();
            questEntry.SetData(questEntryData);
            questEntry.CompleteParentQuest = false;

            return questEntry;
        }

        private class DealProduct
        {
            internal DealProduct(string productName, int quantity)
            {
                ProductName = productName;
                Quantity = quantity;
            }

            public string ProductName { get; private set; }
            public int Quantity { get; private set; }
        }
    }
}
