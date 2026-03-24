using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MedievalClicker.Models
{
    public class City
    {
        public string Name { get; set; } = "";
        public double TravelTimeSeconds { get; set; }
        public List<Blacksmith> Blacksmiths { get; set; } = new();
        public double BrigandDanger { get; set; }

        public static List<City> CreateCities(Random rng)
        {
            return new List<City>
            {
                new City
                {
                    Name = "Havrefer",
                    TravelTimeSeconds = 5,
                    BrigandDanger = 0.1,
                    Blacksmiths = new List<Blacksmith>
                    {
                        new Blacksmith("Grondar le Rude", GeneratePrices(rng, 0.8, 1.2)),
                        new Blacksmith("Elise la Fine", GeneratePrices(rng, 0.9, 1.3)),
                    }
                },
                new City
                {
                    Name = "Luméclair",
                    TravelTimeSeconds = 12,
                    BrigandDanger = 0.25,
                    Blacksmiths = new List<Blacksmith>
                    {
                        new Blacksmith("Thorn l'Ancien", GeneratePrices(rng, 1.0, 1.5)),
                        new Blacksmith("Mira la Mystique", GeneratePrices(rng, 0.7, 1.8)),
                    }
                },
                new City
                {
                    Name = "Crépuscaille",
                    TravelTimeSeconds = 20,
                    BrigandDanger = 0.4,
                    Blacksmiths = new List<Blacksmith>
                    {
                        new Blacksmith("Vulkan le Noir", GeneratePrices(rng, 1.2, 2.0)),
                        new Blacksmith("Aelindra l'Éternelle", GeneratePrices(rng, 0.6, 2.5)),
                    }
                }
            };
        }

        private static Dictionary<OreType, double> GeneratePrices(Random rng, double minMul, double maxMul)
        {
            var prices = new Dictionary<OreType, double>();
            foreach (OreType ore in Enum.GetValues<OreType>())
            {
                double mul = minMul + rng.NextDouble() * (maxMul - minMul);
                prices[ore] = Math.Round(OreInfo.GetBaseValue(ore) * mul, 1);
            }
            return prices;
        }
    }

    public class Blacksmith : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public Dictionary<OreType, double> Prices { get; set; }

        private int _relationLevel;
        public int RelationLevel
        {
            get => _relationLevel;
            set { _relationLevel = value; OnPropertyChanged(); OnPropertyChanged(nameof(RelationBonus)); OnPropertyChanged(nameof(DisplayName)); }
        }

        private int _totalSales;
        public int TotalSales
        {
            get => _totalSales;
            set { _totalSales = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Relation bonus: +2% per relation level, up to +30%
        /// </summary>
        public double RelationBonus => Math.Min(0.30, RelationLevel * 0.02);

        public string DisplayName => RelationLevel > 0
            ? $"{Name} (Rel. {RelationLevel})"
            : Name;

        public Blacksmith(string name, Dictionary<OreType, double> prices)
        {
            Name = name;
            Prices = prices;
        }

        public double GetPrice(OreType ore)
        {
            double basePrice = Prices.TryGetValue(ore, out var price) ? price : OreInfo.GetBaseValue(ore);
            return Math.Round(basePrice * (1.0 + RelationBonus), 1);
        }

        /// <summary>
        /// Try to negotiate. Success chance depends on relation level.
        /// On success, prices for this sale increase by 5-15%.
        /// On failure, prices decrease by 5-10%.
        /// Returns the negotiation multiplier (>1 = success, less than 1 = failure).
        /// </summary>
        public (double multiplier, bool success, string message) TryNegotiate(Random rng)
        {
            // Base success chance: 30% + 3% per relation level, capped at 70%
            double successChance = Math.Min(0.70, 0.30 + RelationLevel * 0.03);
            bool success = rng.NextDouble() < successChance;

            if (success)
            {
                double bonus = 1.05 + rng.NextDouble() * 0.10; // +5% to +15%
                return (bonus, true, $"{Name} accepte ! Prix +{(bonus - 1) * 100:F0}% pour cette vente !");
            }
            else
            {
                double penalty = 0.90 + rng.NextDouble() * 0.05; // -5% to -10%
                return (penalty, false, $"{Name} refuse et se vexe... Prix -{(1 - penalty) * 100:F0}% pour cette vente.");
            }
        }

        /// <summary>
        /// Build relation after a sale. Gains 1 level every 5 sales.
        /// </summary>
        public bool AddSale(int itemCount)
        {
            TotalSales += itemCount;
            int newLevel = TotalSales / 5;
            if (newLevel > RelationLevel)
            {
                RelationLevel = newLevel;
                return true; // level up!
            }
            return false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
