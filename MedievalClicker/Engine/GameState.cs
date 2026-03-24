using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using MedievalClicker.Models;

namespace MedievalClicker.Engine
{
    public class GameState : INotifyPropertyChanged
    {
        private readonly Random _rng = new();
        private readonly DispatcherTimer _gameTimer;
        private double _gold;
        private Mine _currentMine;
        private string _lastEvent = "Bienvenue, jeune mineur !";

        public double Gold
        {
            get => _gold;
            set { _gold = Math.Round(value, 1); OnPropertyChanged(); OnPropertyChanged(nameof(GoldDisplay)); }
        }

        public string GoldDisplay => $"{Gold:N1} golds";

        public string LastEvent
        {
            get => _lastEvent;
            set { _lastEvent = value; OnPropertyChanged(); }
        }

        public Dictionary<OreType, int> Inventory { get; } = new();

        public Mine CurrentMine
        {
            get => _currentMine;
            set { _currentMine = value; OnPropertyChanged(); }
        }

        public List<Mine> ShopMines { get; set; }
        public List<City> Cities { get; set; }
        public MiningTool Tool { get; } = new();
        public AutoMiner AutoMiners { get; } = new();
        public Carriage Carriage { get; } = new();

        private double _autoMineAccumulator;

        public GameState()
        {
            foreach (OreType ore in Enum.GetValues<OreType>())
                Inventory[ore] = 0;

            _currentMine = Mine.CreateStarterMine();
            ShopMines = Mine.GenerateShopMines(_rng);
            Cities = City.CreateCities(_rng);

            _gameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _gameTimer.Tick += GameTick;
            _gameTimer.Start();
        }

        public ClickResult DoClick()
        {
            var result = new ClickResult();

            // Check if mine is exhausted
            if (CurrentMine.IsExhausted)
            {
                result.Message = "Cette mine est épuisée ! Changez de mine.";
                LastEvent = result.Message;
                return result;
            }

            // Dig deeper
            CurrentMine.DigDeeper(Tool.DepthPower);

            // Get available ores at current depth
            var availableOres = CurrentMine.GetOresAtCurrentDepth();
            if (availableOres.Count == 0)
            {
                result.Message = "Vous frappez la roche... rien ne se passe.";
                return result;
            }

            // Mine ores based on drop rates
            foreach (var slot in CurrentMine.AvailableOres)
            {
                if (slot.MinDepth > CurrentMine.CurrentDepth) continue;

                for (int i = 0; i < Tool.MiningPower; i++)
                {
                    if (_rng.NextDouble() < slot.DropRate && CurrentMine.RemainingResources > 0)
                    {
                        Inventory[slot.OreType]++;
                        CurrentMine.RemainingResources--;
                        result.MinedOres[slot.OreType] = result.MinedOres.GetValueOrDefault(slot.OreType) + 1;
                    }
                }
            }

            if (result.MinedOres.Count > 0)
            {
                var oreNames = result.MinedOres.Select(kvp =>
                    $"{kvp.Value}x {OreInfo.GetName(kvp.Key)}");
                result.Message = $"Vous avez miné : {string.Join(", ", oreNames)}";

                if (CurrentMine.IsExhausted)
                    result.Message += " - LA MINE EST ÉPUISÉE !";
            }
            else
            {
                result.Message = "Coup dans le vide... Essayez encore !";
            }

            OnPropertyChanged(nameof(Inventory));
            LastEvent = result.Message;
            return result;
        }

        private void GameTick(object? sender, EventArgs e)
        {
            double deltaSeconds = 0.1;

            // Auto miners
            if (AutoMiners.Count > 0)
            {
                _autoMineAccumulator += AutoMiners.TotalOutput * deltaSeconds;
                while (_autoMineAccumulator >= 1.0)
                {
                    _autoMineAccumulator -= 1.0;
                    AutoMine();
                }
            }

            // Carriage travel
            UpdateCarriage(deltaSeconds);
        }

        private void AutoMine()
        {
            if (CurrentMine.IsExhausted) return;

            var availableOres = CurrentMine.GetOresAtCurrentDepth();
            if (availableOres.Count == 0) return;

            CurrentMine.DigDeeper(1);

            foreach (var slot in CurrentMine.AvailableOres)
            {
                if (slot.MinDepth > CurrentMine.CurrentDepth) continue;
                if (_rng.NextDouble() < slot.DropRate && CurrentMine.RemainingResources > 0)
                {
                    Inventory[slot.OreType]++;
                    CurrentMine.RemainingResources--;
                }
            }

            if (CurrentMine.IsExhausted)
                LastEvent = $"{CurrentMine.Name} est épuisée ! Changez de mine.";

            OnPropertyChanged(nameof(Inventory));
        }

        private void UpdateCarriage(double deltaSeconds)
        {
            if (Carriage.State == CarriageState.TravelingToCity || Carriage.State == CarriageState.TravelingToMine)
            {
                Carriage.TravelProgress += deltaSeconds * Carriage.SpeedMultiplier;

                if (Carriage.TravelProgress >= Carriage.TravelDuration)
                {
                    Carriage.TravelProgress = 0;

                    if (Carriage.State == CarriageState.TravelingToCity)
                    {
                        // Check for brigand attack
                        var city = Cities.First(c => c.Name == Carriage.DestinationCityName);
                        if (BrigandAttack(city))
                        {
                            LastEvent = "Des brigands ont attaqué la calèche !";
                        }
                        else
                        {
                            Carriage.State = CarriageState.InCity;
                            LastEvent = $"La calèche est arrivée à {city.Name} !";
                        }
                    }
                    else
                    {
                        Carriage.State = CarriageState.AtMine;
                        LastEvent = "La calèche est de retour à la mine !";
                    }
                }
            }
        }

        private bool BrigandAttack(City city)
        {
            double attackChance = Math.Max(0, city.BrigandDanger - Carriage.Soldiers * 0.1);
            if (_rng.NextDouble() < attackChance)
            {
                // Brigands steal some cargo
                double stealPercent = 0.3 + _rng.NextDouble() * 0.4;
                var newCargo = new Dictionary<OreType, int>();
                foreach (var kvp in Carriage.Cargo)
                {
                    int stolen = (int)(kvp.Value * stealPercent);
                    int remaining = kvp.Value - stolen;
                    if (remaining > 0) newCargo[kvp.Key] = remaining;
                }
                Carriage.Cargo = newCargo;
                Carriage.State = CarriageState.InCity;
                LastEvent += $" Ils ont volé {stealPercent * 100:F0}% du chargement !";
                return true;
            }
            return false;
        }

        public void SendCarriage(City city)
        {
            if (Carriage.State != CarriageState.AtMine) return;
            if (Carriage.TotalCargoCount == 0) return;

            double adjustedTime = city.TravelTimeSeconds * CurrentMine.DistanceMultiplier;
            Carriage.DestinationCityName = city.Name;
            Carriage.TravelDuration = adjustedTime;
            Carriage.TravelProgress = 0;
            Carriage.State = CarriageState.TravelingToCity;
            LastEvent = $"La calèche part vers {city.Name} ! (trajet: {adjustedTime:F0}s)";
        }

        public void ReturnCarriage()
        {
            if (Carriage.State != CarriageState.InCity) return;

            var city = Cities.First(c => c.Name == Carriage.DestinationCityName);
            double adjustedTime = city.TravelTimeSeconds * CurrentMine.DistanceMultiplier;
            Carriage.TravelDuration = adjustedTime;
            Carriage.TravelProgress = 0;
            Carriage.State = CarriageState.TravelingToMine;
            LastEvent = $"La calèche repart vers la mine ! (trajet: {adjustedTime:F0}s)";
        }

        public double SellToBlacksmith(Blacksmith blacksmith)
        {
            if (Carriage.State != CarriageState.InCity) return 0;

            var cargo = Carriage.UnloadAll();
            double totalGold = 0;

            foreach (var kvp in cargo)
            {
                double price = blacksmith.GetPrice(kvp.Key);
                totalGold += price * kvp.Value;
            }

            Gold += totalGold;
            LastEvent = $"Vendu au forgeron {blacksmith.Name} pour {totalGold:N1} golds !";
            return totalGold;
        }

        public void LoadOreToCarriage(OreType ore, int amount)
        {
            if (Carriage.State != CarriageState.AtMine) return;

            int available = Inventory[ore];
            int toLoad = Math.Min(amount, available);
            toLoad = Math.Min(toLoad, Carriage.RemainingSpace);
            if (toLoad <= 0) return;

            Inventory[ore] -= toLoad;
            Carriage.LoadOre(ore, toLoad);
            OnPropertyChanged(nameof(Inventory));
        }

        public bool UpgradeTool()
        {
            if (Gold < Tool.UpgradeCost) return false;
            Gold -= Tool.UpgradeCost;
            Tool.Upgrade();
            LastEvent = $"Pioche améliorée ! Niveau {Tool.Level} - {Tool.Name}";
            return true;
        }

        public bool HireMiner()
        {
            if (Gold < AutoMiners.HireCost) return false;
            Gold -= AutoMiners.HireCost;
            AutoMiners.Hire();
            LastEvent = $"Nouveau mineur embauché ! ({AutoMiners.Count} mineurs)";
            return true;
        }

        public bool UpgradeMiners()
        {
            if (Gold < AutoMiners.UpgradeCost) return false;
            Gold -= AutoMiners.UpgradeCost;
            AutoMiners.UpgradeAll();
            LastEvent = $"Mineurs améliorés ! Niveau {AutoMiners.Level}";
            return true;
        }

        public bool UpgradeCarriage()
        {
            if (Gold < Carriage.UpgradeCost) return false;
            Gold -= Carriage.UpgradeCost;
            Carriage.Upgrade();
            LastEvent = $"Calèche améliorée ! Niveau {Carriage.Level}";
            return true;
        }

        public bool HireSoldier()
        {
            if (Gold < Carriage.SoldierHireCost) return false;
            Gold -= Carriage.SoldierHireCost;
            Carriage.HireSoldier();
            LastEvent = $"Soldat engagé ! ({Carriage.Soldiers} soldats)";
            return true;
        }

        public bool BuyMine(Mine mine)
        {
            if (Gold < mine.PurchasePrice) return false;
            if (mine.IsOwned) return false;

            Gold -= mine.PurchasePrice;
            mine.IsOwned = true;
            LastEvent = $"Mine achetée : {mine.Name} !";
            return true;
        }

        public void SwitchMine(Mine mine)
        {
            if (!mine.IsOwned) return;
            CurrentMine = mine;
            LastEvent = $"Vous travaillez maintenant dans : {mine.Name}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ClickResult
    {
        public Dictionary<OreType, int> MinedOres { get; } = new();
        public string Message { get; set; } = "";
    }
}
