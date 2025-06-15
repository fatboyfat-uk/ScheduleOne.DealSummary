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

using ScheduleOne.Product;
using UnityEngine;

namespace ScheduleOne.DealSummary.Mono
{
    public class DealProduct : MonoBehaviour
    {
        public DealProduct(ProductDefinition productDefinition, int playerQuantity, int dealerQuantity)
        {
            ProductDefinition = productDefinition;
            PlayerQuantity = playerQuantity;
            DealerQuantity = dealerQuantity;
        }

        public ProductDefinition ProductDefinition { get; private set; }
        public string ProductID => ProductDefinition?.ID ?? string.Empty;
        public string ProductName => ProductDefinition?.Name ?? string.Empty;
        public int PlayerQuantity { get; private set; }
        public int DealerQuantity { get; private set; }

        public string QuestTitle => $"{OpeningColorTag}{PlayerQuantity}x {ProductName}{ClosingColorTag}{DealerTitlePart}";
        private string OpeningColorTag => PlayerQuantity == 0 ? "<color=#888888ff>" : "";
        private string ClosingColorTag => PlayerQuantity == 0 ? "</color>" : "";
        private string DealerTitlePart => DealerQuantity > 0 ? $" <color=#888888ff>(+ {DealerQuantity})</color>" : string.Empty;

        internal void CopyFrom(DealProduct other)
        {
            if (other is null) 
                throw new System.ArgumentNullException(nameof(other));

            ProductDefinition = other.ProductDefinition;
            PlayerQuantity = other.PlayerQuantity;
            DealerQuantity = other.DealerQuantity;
        }
    }
}
