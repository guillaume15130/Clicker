using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MedievalClicker.Models
{
    public enum CarriageState
    {
        AtMine,
        TravelingToCity,
        InCity,
        TravelingToMine
    }

    public class Carriage : INotifyPropertyChanged
    {
        private int _level = 1;
        private int _capacity = 20;
        private double _speedMultiplier = 1.0;
        private double _upgradeCost = 300;
        private CarriageState _state = CarriageState.AtMine;
        private double _travelProgress;
        private double _travelDuration;
        private string _destinationCityName = "";
        private int _soldiers;
        private double _soldierHireCost = 150;

        public int Level
        {
            get => _level;
            set { _level = value; OnPropertyChanged(); }
        }

        public int Capacity
        {
            get => _capacity;
            set { _capacity = value; OnPropertyChanged(); }
        }

        public double SpeedMultiplier
        {
            get => _speedMultiplier;
            set { _speedMultiplier = value; OnPropertyChanged(); }
        }

        public double UpgradeCost
        {
            get => _upgradeCost;
            set { _upgradeCost = value; OnPropertyChanged(); }
        }

        public CarriageState State
        {
            get => _state;
            set { _state = value; OnPropertyChanged(); OnPropertyChanged(nameof(StateDisplay)); OnPropertyChanged(nameof(IsAtMine)); }
        }

        public string StateDisplay => State switch
        {
            CarriageState.AtMine => "A la mine",
            CarriageState.TravelingToCity => $"En route vers {DestinationCityName}...",
            CarriageState.InCity => $"En ville ({DestinationCityName})",
            CarriageState.TravelingToMine => "Retour à la mine...",
            _ => ""
        };

        public bool IsAtMine => State == CarriageState.AtMine;

        public double TravelProgress
        {
            get => _travelProgress;
            set { _travelProgress = value; OnPropertyChanged(); OnPropertyChanged(nameof(TravelProgressPercent)); }
        }

        public double TravelProgressPercent => TravelDuration > 0 ? (TravelProgress / TravelDuration) * 100 : 0;

        public double TravelDuration
        {
            get => _travelDuration;
            set { _travelDuration = value; OnPropertyChanged(); }
        }

        public string DestinationCityName
        {
            get => _destinationCityName;
            set { _destinationCityName = value; OnPropertyChanged(); OnPropertyChanged(nameof(StateDisplay)); }
        }

        public int Soldiers
        {
            get => _soldiers;
            set { _soldiers = value; OnPropertyChanged(); }
        }

        public double SoldierHireCost
        {
            get => _soldierHireCost;
            set { _soldierHireCost = value; OnPropertyChanged(); }
        }

        public Dictionary<OreType, int> Cargo { get; set; } = new();

        public int TotalCargoCount
        {
            get
            {
                int total = 0;
                foreach (var kvp in Cargo)
                    total += kvp.Value;
                return total;
            }
        }

        public int RemainingSpace => Capacity - TotalCargoCount;

        public void Upgrade()
        {
            Level++;
            Capacity = 20 + (Level - 1) * 10;
            SpeedMultiplier = 1.0 + (Level - 1) * 0.25;
            UpgradeCost = 300 * Math.Pow(2.0, Level - 1);
        }

        public void HireSoldier()
        {
            Soldiers++;
            SoldierHireCost = 150 * Math.Pow(1.6, Soldiers);
        }

        public void LoadOre(OreType type, int amount)
        {
            int canLoad = Math.Min(amount, RemainingSpace);
            if (canLoad <= 0) return;

            if (Cargo.ContainsKey(type))
                Cargo[type] += canLoad;
            else
                Cargo[type] = canLoad;

            OnPropertyChanged(nameof(TotalCargoCount));
            OnPropertyChanged(nameof(RemainingSpace));
        }

        public Dictionary<OreType, int> UnloadAll()
        {
            var unloaded = new Dictionary<OreType, int>(Cargo);
            Cargo.Clear();
            OnPropertyChanged(nameof(TotalCargoCount));
            OnPropertyChanged(nameof(RemainingSpace));
            return unloaded;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
