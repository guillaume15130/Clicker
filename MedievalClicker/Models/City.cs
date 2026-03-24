using System;
using System.Collections.Generic;

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
                    TravelTimeSeconds = 30,
                    BrigandDanger = 0.2,
                    Blacksmiths = new List<Blacksmith>
                    {
                        new Blacksmith("Grondar le Rude", GeneratePrices(rng, 0.8, 1.2)),
                        new Blacksmith("Elise la Fine", GeneratePrices(rng, 0.9, 1.3)),
                    }
                },
                new City
                {
                    Name = "Luméclair",
                    TravelTimeSeconds = 60,
                    BrigandDanger = 0.35,
                    Blacksmiths = new List<Blacksmith>
                    {
                        new Blacksmith("Thorn l'Ancien", GeneratePrices(rng, 1.0, 1.5)),
                        new Blacksmith("Mira la Mystique", GeneratePrices(rng, 0.7, 1.8)),
                    }
                },
                new City
                {
                    Name = "Crépuscaille",
                    TravelTimeSeconds = 90,
                    BrigandDanger = 0.5,
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

    public class Blacksmith
    {
        public string Name { get; set; }
        public Dictionary<OreType, double> Prices { get; set; }

        public Blacksmith(string name, Dictionary<OreType, double> prices)
        {
            Name = name;
            Prices = prices;
        }

        public double GetPrice(OreType ore)
        {
            return Prices.TryGetValue(ore, out var price) ? price : OreInfo.GetBaseValue(ore);
        }
    }
}
