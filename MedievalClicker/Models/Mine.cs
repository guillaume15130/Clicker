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
        private int _remainingResources;
        private int _totalResources;
        private double _distanceMultiplier = 1.0;
        private int _currentMeterHits;
        private int _hitsPerMeter = 10;
        private int _assignedMiners;

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

        public int RemainingResources
        {
            get => _remainingResources;
            set { _remainingResources = Math.Max(0, value); OnPropertyChanged(); OnPropertyChanged(nameof(IsExhausted)); OnPropertyChanged(nameof(ResourcesDisplay)); }
        }

        public int TotalResources
        {
            get => _totalResources;
            set { _totalResources = value; OnPropertyChanged(); OnPropertyChanged(nameof(ResourcesDisplay)); }
        }

        public string ResourcesDisplay => $"{RemainingResources} / {TotalResources}";
        public bool IsExhausted => RemainingResources <= 0;

        public double DistanceMultiplier
        {
            get => _distanceMultiplier;
            set { _distanceMultiplier = value; OnPropertyChanged(); }
        }

        private int _requiredRelationLevel;

        public int RequiredRelationLevel
        {
            get => _requiredRelationLevel;
            set { _requiredRelationLevel = value; OnPropertyChanged(); }
        }

        public int CurrentMeterHits
        {
            get => _currentMeterHits;
            set { _currentMeterHits = value; OnPropertyChanged(); OnPropertyChanged(nameof(MeterDisplay)); }
        }

        public int HitsPerMeter
        {
            get => _hitsPerMeter;
            set { _hitsPerMeter = value; OnPropertyChanged(); OnPropertyChanged(nameof(MeterDisplay)); }
        }

        public string MeterDisplay => $"Mètre actuel: {CurrentMeterHits} / {HitsPerMeter} coups";

        public int AssignedMiners
        {
            get => _assignedMiners;
            set { _assignedMiners = Math.Max(0, value); OnPropertyChanged(); }
        }

        public int MineId { get; set; }

        public List<MineOreSlot> AvailableOres { get; set; } = new();

        public bool IsMaxDepthReached => CurrentDepth >= MaxDepth;

        /// <summary>
        /// Hit the current meter. Returns true if we descended to a new meter.
        /// </summary>
        public bool HitMeter(int power)
        {
            if (IsMaxDepthReached) return false;

            CurrentMeterHits += power;
            if (CurrentMeterHits >= HitsPerMeter)
            {
                CurrentMeterHits = 0;
                CurrentDepth = Math.Min(CurrentDepth + 1, MaxDepth);
                // Deeper = harder rock
                HitsPerMeter = 10 + CurrentDepth / 5;
                return true;
            }
            return false;
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
                MineId = 0,
                Name = "Mine Abandonnée",
                CurrentDepth = 0,
                MaxDepth = 50,
                IsOwned = true,
                PurchasePrice = 0,
                TotalResources = 500,
                RemainingResources = 500,
                DistanceMultiplier = 1.0,
                RequiredRelationLevel = 0,
                HitsPerMeter = 10,
                CurrentMeterHits = 0,
                AvailableOres = new List<MineOreSlot>
                {
                    new(OreType.Charbon, 0, 0.6),
                    new(OreType.Fer, 10, 0.3),
                    new(OreType.Or, 30, 0.1),
                }
            };
        }

        private static readonly string[] MineNames = new[]
        {
            "Mine des Collines Grises", "Mine du Ruisseau Sombre", "Mine de la Forêt Blanche",
            "Mine des Montagnes Noires", "Mine du Vallon Perdu", "Mine de la Rivière Rouge",
            "Mine du Dragon Endormi", "Mine de l'Aigle d'Or", "Mine du Crépuscule",
            "Mine des Abysses", "Mine du Titan Déchu", "Mine de la Lune Brisée",
            "Mine du Phénix Ancien", "Mine de l'Étoile Noire", "Mine du Néant Éternel",
            "Mine des Dieux Oubliés", "Mine du Chaos Primordial", "Mine de l'Aube Écarlate",
            "Mine du Serpent de Cristal", "Mine de la Flamme Éternelle"
        };

        public static List<Mine> GenerateAllMines(Random rng)
        {
            var mines = new List<Mine>();

            for (int i = 0; i < MineNames.Length; i++)
            {
                int tier = i / 4;
                int relationRequired = i / 3;
                int maxDepth = 40 + (i * 15) + rng.Next(0, 20);
                double price = 200 * Math.Pow(2.2, i) + rng.Next(0, 500);
                int totalRes = 400 + (i * 200) + rng.Next(0, 200);
                double distance = 1.0 + i * 0.15 + rng.NextDouble() * 0.3;

                var mine = new Mine
                {
                    MineId = i + 1,
                    Name = MineNames[i],
                    CurrentDepth = 0,
                    MaxDepth = maxDepth,
                    IsOwned = false,
                    PurchasePrice = Math.Round(price),
                    TotalResources = totalRes,
                    RemainingResources = totalRes,
                    DistanceMultiplier = Math.Round(distance, 1),
                    RequiredRelationLevel = relationRequired,
                    HitsPerMeter = 10 + i,
                    CurrentMeterHits = 0,
                    AvailableOres = GenerateRandomOres(rng, tier)
                };
                mines.Add(mine);
            }

            return mines;
        }

        public static List<Mine> PickVisibleMines(List<Mine> allMines, int relationLevel, Random rng)
        {
            var available = allMines
                .Where(m => !m.IsOwned && m.RequiredRelationLevel <= relationLevel)
                .ToList();

            if (available.Count <= 3)
                return available.ToList();

            // Shuffle and pick 3
            var shuffled = available.OrderBy(_ => rng.Next()).ToList();
            return shuffled.Take(3).ToList();
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
