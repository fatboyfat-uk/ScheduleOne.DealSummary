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
using ScheduleOne.Economy;
using ScheduleOne.GameTime;
using ScheduleOne.Product;
using ScheduleOne.Quests;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScheduleOne.DealSummary.Mono
{
    public class Core : MelonMod
    {
        private const string MainSceneName = "Main";

        private DealSummaryQuest currentQuest;
        private bool isSubscribed = false;

        private AnimationCurve fadeInCurve;
        private AnimationCurve fadeOutCurve;

        private AnimationClip fadeInClip;
        private AnimationClip fadeOutClip;

        public override void OnInitializeMelon()
        {
            fadeInClip = new AnimationClip
            {
                name = "Custom fade in",
                legacy = true
            };
            fadeOutClip = new AnimationClip
            {
                name = "Custom fade out",
                legacy = true
            };

            fadeInCurve = AnimationCurve.Linear(0.0f, 0.0f, 1f, 1.0f);
            fadeOutCurve = AnimationCurve.Linear(0.0f, 1.0f, 1f, 0.0f);

            fadeInClip.SetCurve("", typeof(Material), "color.a", fadeInCurve);
            fadeOutClip.SetCurve("", typeof(Material), "color.a", fadeOutCurve);
        }

        private DealSummaryQuest CreateDealSummaryQuest(List<DealProduct> products)
        {
            LoggerInstance.Msg($"Creating {nameof(DealSummaryQuest)} with {products.Count} products.");

            var gameObject = new GameObject("DealSummaryQuest");
            gameObject.transform.SetParent(QuestManager.Instance.QuestContainer, false);
            gameObject.transform.SetSiblingIndex(0);
            var newQuest = gameObject.AddComponent<DealSummaryQuest>();
            newQuest.Initialize(products);
            
            return newQuest;
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            isSubscribed = false;
        }

        public override void OnUpdate()
        {
            bool gameStarted = SceneManager.GetActiveScene().name == MainSceneName;
            if (gameStarted && !isSubscribed)
            {
                if (NetworkSingleton<TimeManager>.Instance == null)
                    LoggerInstance.Warning($"TimeManager is null, deferring subscription to onTick event.");
                else
                {
                    LoggerInstance.Msg($"Adding onTick for {nameof(UpdateDealSummaryQuest)}");
                    NetworkSingleton<TimeManager>.Instance.onTick += UpdateDealSummaryQuest;
                    isSubscribed = true;
                }
            }
        }

        private bool updateInProgress = false;

        private void UpdateDealSummaryQuest()
        {
            if (updateInProgress)
            {
                LoggerInstance.Msg("Update already in progress, skipping this tick.");
                return;
            }

            updateInProgress = true;
            var products = EnumerateCurrentDealProducts();

            if (currentQuest == null)
            {
                try
                {
                    currentQuest = CreateDealSummaryQuest(products);
                }
                finally
                {
                    updateInProgress = false;
                }
            }
            else if (!currentQuest.TryUpdateProducts(products))
            {
                LoggerInstance.Msg($"We have added or removed some products since last time. Updating all quest entries...");
                CoroutineService.Instance.StartCoroutine(Routine(LoggerInstance, products));
            }
            else
            {
                updateInProgress = false;
            }

            IEnumerator Routine(MelonLogger.Instance logger, List<DealProduct> products)
            {
                int i = 0;

                try
                {
                    currentQuest.hudUI.Animation.AddClip(fadeOutClip, fadeOutClip.name);
                    currentQuest.hudUI.Animation.Play(fadeOutClip.name);
                    
                    while (currentQuest.hudUI.Animation.isPlaying)
                    {
                        if (i > 1000)
                        {
                            logger.Msg("Timed out waiting for quest exit animation to finish, continuing anyway...");
                            continue;
                        }
                        else if (++i % 100 == 0)
                        {
                            logger.Msg("Waiting for quest exit animation to finish...");
                        }

                        yield return new WaitForEndOfFrame();
                    }
                    
                    logger.Msg("Forcing quest product update");
                    currentQuest.TryUpdateProducts(products, true);
                    
                    currentQuest.hudUI.Animation.AddClip(fadeInClip, fadeInClip.name);
                    currentQuest.hudUI.Animation.Play(fadeInClip.name);

                    while (currentQuest.hudUI.Animation.isPlaying)
                    {
                        if (i > 1000)
                        {
                            logger.Msg("Timed out waiting for quest enter animation to finish, continuing anyway...");
                            continue;
                        }
                        else if (++i % 100 == 0)
                        {
                            logger.Msg("Waiting for quest enter animation to finish...");
                        }
                        yield return new WaitForEndOfFrame();
                    }
                }
                finally
                {
                    updateInProgress = false;
                }
            }
        }

        private static List<DealProduct> EnumerateCurrentDealProducts()
        {
            Dictionary<ProductDefinition, (int player, int dealer)> productCounts = new Dictionary<ProductDefinition, (int player, int dealer)>();

            var contracts = Contract.Contracts ?? new List<Contract>();
            var productIDToDefinition = ProductManager.Instance.AllProducts.ToDictionary(p => p.ID, p => p);
            var player = new Dealer();

            foreach (var dealerGroup in contracts
                .GroupBy(c => c.Dealer ?? player)
                .ToDictionary(c => c.Key, c => c.SelectMany(c => c.ProductList.entries)))
            {
                foreach (var dealItem in dealerGroup.Value)
                {
                    var productDefinition = productIDToDefinition[dealItem.ProductID];

                    if (!productCounts.TryGetValue(productDefinition, out (int player, int dealer) count))
                    {
                        productCounts[productDefinition] = (0, 0);
                    }

                    if (dealerGroup.Key == player)
                        productCounts[productDefinition] = (productCounts[productDefinition].player + dealItem.Quantity, productCounts[productDefinition].dealer);
                    else
                        productCounts[productDefinition] = (productCounts[productDefinition].player, productCounts[productDefinition].dealer + dealItem.Quantity);
                }
            }

            foreach (var product in ProductManager.ListedProducts)
            {
                if (!productCounts.ContainsKey(product))
                {
                    productCounts[product] = (0, 0);
                }
            }

            return productCounts
                .OrderBy(kvp => kvp.Key.Name)
                .Select(kvp => new DealProduct(kvp.Key, kvp.Value.player, kvp.Value.dealer))
                .ToList();
        }

    }
}