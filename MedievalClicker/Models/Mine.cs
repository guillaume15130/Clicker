using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MedievalClicker.Models
{
    public class Mine : INotifyPropertyChanged
    {
        private string _name = "";
        private int _currentDepth;
        private int _maxDepth;
        private bool _isOwned;
        private double _purchasePrice;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public int CurrentDepth
        {
            get => _currentDepth;
            set { _currentDepth = value; OnPropertyChanged(); OnPropertyChanged(nameof(DepthDisplay)); }
        }

        public int MaxDepth
        {
            get => _maxDepth;
            set { _maxDepth = value; OnPropertyChanged(); OnPropertyChanged(nameof(DepthDisplay)); }
        }

        public string DepthDisplay => $"{CurrentDepth} / {MaxDepth}m";

        public bool IsOwned
        {
            get => _isOwned;
            set { _isOwned = value; OnPropertyChanged(); }
        }

        public double PurchasePrice
        {
            get => _purchasePrice;
            set { _purchasePrice = value; OnPropertyChanged(); }
        }

        public List<MineOreSlot> AvailableOres { get; set; } = new();

        public bool IsMaxDepthReached => CurrentDepth >= MaxDepth;

        public void DigDeeper(int amount)
        {
            CurrentDepth = Math.Min(CurrentDepth + amount, MaxDepth);
        }

        public List<OreType> GetOresAtCurrentDepth()
        {
            return AvailableOres
                .Where(slot => slot.MinDepth <= CurrentDepth)
                .Select(slot => slot.OreType)
                .ToList();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public static Mine CreateStarterMine()
        {
            return new Mine
            {
                Name = "Mine Abandonnée",
                CurrentDepth = 0,
                MaxDepth = 50,
                IsOwned = true,
                PurchasePrice = 0,
                AvailableOres = new List<MineOreSlot>
                {
                    new(OreType.Charbon, 0, 0.6),
                    new(OreType.Fer, 10, 0.3),
                    new(OreType.Or, 30, 0.1),
                }
            };
        }

        public static List<Mine> GenerateShopMines(Random rng)
        {
            var names = new[] { "Mine des Montagnes Noires", "Mine du Dragon Endormi", "Mine des Abysses" };
            var mines = new List<Mine>();

            for (int i = 0; i < 3; i++)
            {
                var maxDepth = 80 + (i * 40) + rng.Next(0, 30);
                var price = 500 * Math.Pow(3, i + 1) + rng.Next(0, 500);
                var mine = new Mine
                {
                    Name = names[i],
                    CurrentDepth = 0,
                    MaxDepth = maxDepth,
                    IsOwned = false,
                    PurchasePrice = price,
                    AvailableOres = GenerateRandomOres(rng, i + 1)
                };
                mines.Add(mine);
            }

            return mines;
        }

        private static List<MineOreSlot> GenerateRandomOres(Random rng, int tier)
        {
            var slots = new List<MineOreSlot>
            {
                new(OreType.Charbon, 0, 0.4),
                new(OreType.Fer, 5 + rng.Next(0, 10), 0.3),
            };

            if (tier >= 1)
            {
                slots.Add(new MineOreSlot(OreType.Or, 15 + rng.Next(0, 15), 0.15));
                if (rng.NextDouble() > 0.3)
                    slots.Add(new MineOreSlot(OreType.Diamant, 40 + rng.Next(0, 20), 0.08));
            }

            if (tier >= 2)
            {
                slots.Add(new MineOreSlot(OreType.MineraisMagique, 60 + rng.Next(0, 20), 0.05));
                if (rng.NextDouble() > 0.4)
                    slots.Add(new MineOreSlot(OreType.CristalDePouvoir, 100 + rng.Next(0, 30), 0.03));
            }

            if (tier >= 3)
            {
                if (rng.NextDouble() > 0.5)
                    slots.Add(new MineOreSlot(OreType.MineraisAncien, 150 + rng.Next(0, 30), 0.01));
            }

            return slots;
        }
    }

    public class MineOreSlot
    {
        public OreType OreType { get; set; }
        public int MinDepth { get; set; }
        public double DropRate { get; set; }

        public MineOreSlot(OreType oreType, int minDepth, double dropRate)
        {
            OreType = oreType;
            MinDepth = minDepth;
            DropRate = dropRate;
        }
    }
}
