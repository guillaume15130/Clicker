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
        private int _relationLevel;
        private double _bribeCost = 1000;

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

        public int RelationLevel
        {
            get => _relationLevel;
            set { _relationLevel = value; OnPropertyChanged(); OnPropertyChanged(nameof(RelationDisplay)); }
        }

        public string RelationDisplay => $"Nv.{RelationLevel}";

        public double BribeCost
        {
            get => _bribeCost;
            set { _bribeCost = Math.Round(value); OnPropertyChanged(); }
        }

        public Dictionary<OreType, int> Inventory { get; } = new();

        public Mine CurrentMine
        {
            get => _currentMine;
            set { _currentMine = value; OnPropertyChanged(); }
        }

        public List<Mine> AllMines { get; set; }
        public List<Mine> VisibleShopMines { get; set; }
        public List<Mine> OwnedMines { get; set; } = new();
        public List<City> Cities { get; set; }
        public MiningTool Tool { get; } = new();
        public AutoMiner AutoMiners { get; } = new();
        public Carriage Carriage { get; } = new();

        private readonly Dictionary<int, double> _autoMineAccumulators = new();

        public int UnassignedMiners
        {
            get
            {
                int assigned = 0;
                foreach (var mine in OwnedMines)
                    assigned += mine.AssignedMiners;
                return AutoMiners.TotalCount - assigned;
            }
        }

        public GameState(bool skipInit = false)
        {
            foreach (OreType ore in Enum.GetValues<OreType>())
                Inventory[ore] = 0;

            _gameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _gameTimer.Tick += GameTick;

            if (!skipInit)
            {
                _currentMine = Mine.CreateStarterMine();
                OwnedMines.Add(_currentMine);
                AllMines = Mine.GenerateAllMines(_rng);
                VisibleShopMines = Mine.PickVisibleMines(AllMines, RelationLevel, _rng);
                Cities = City.CreateCities(_rng);
                _gameTimer.Start();
            }
            else
            {
                _currentMine = null!;
                AllMines = new List<Mine>();
                VisibleShopMines = new List<Mine>();
                Cities = new List<City>();
            }
        }

        public void StartTimer()
        {
            if (!_gameTimer.IsEnabled)
                _gameTimer.Start();
        }

        public void InitFromSave()
        {
            // Rebuild visible mines and owned mines after loading
            OwnedMines = new List<Mine> { _currentMine };
            foreach (var mine in AllMines.Where(m => m.IsOwned))
            {
                if (!OwnedMines.Contains(mine))
                    OwnedMines.Add(mine);
            }
            RefreshVisibleMines();
        }

        public void RefreshVisibleMines()
        {
            VisibleShopMines = Mine.PickVisibleMines(AllMines, RelationLevel, _rng);
            OnPropertyChanged(nameof(VisibleShopMines));
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

            // Hit the current meter (try to go deeper)
            bool descended = CurrentMine.HitMeter(Tool.DepthPower);

            // Mine ores at current depth
            var availableOres = CurrentMine.GetOresAtCurrentDepth();
            if (availableOres.Count == 0)
            {
                result.Message = "Vous frappez la roche... continuez pour descendre !";
                LastEvent = result.Message;
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

                if (descended)
                    result.Message += $" (nouveau mètre : {CurrentMine.CurrentDepth}m !)";

                if (CurrentMine.IsExhausted)
                    result.Message += " - LA MINE EST ÉPUISÉE !";
            }
            else
            {
                result.Message = descended
                    ? $"Nouveau mètre atteint : {CurrentMine.CurrentDepth}m !"
                    : "Coup dans le vide... Essayez encore !";
            }

            OnPropertyChanged(nameof(Inventory));
            LastEvent = result.Message;
            return result;
        }

        private void GameTick(object? sender, EventArgs e)
        {
            double deltaSeconds = 0.1;

            // Auto miners per mine
            foreach (var mine in OwnedMines)
            {
                if (mine.AssignedMiners <= 0) continue;
                if (mine.IsExhausted) continue;

                double output = mine.AssignedMiners * AutoMiners.MiningSpeed * deltaSeconds;

                if (!_autoMineAccumulators.ContainsKey(mine.MineId))
                    _autoMineAccumulators[mine.MineId] = 0;

                _autoMineAccumulators[mine.MineId] += output;

                while (_autoMineAccumulators[mine.MineId] >= 1.0)
                {
                    _autoMineAccumulators[mine.MineId] -= 1.0;
                    AutoMineFor(mine);
                }
            }

            // Carriage travel
            UpdateCarriage(deltaSeconds);
        }

        private void AutoMineFor(Mine mine)
        {
            if (mine.IsExhausted) return;

            mine.HitMeter(1);

            var availableOres = mine.GetOresAtCurrentDepth();
            if (availableOres.Count == 0) return;

            foreach (var slot in mine.AvailableOres)
            {
                if (slot.MinDepth > mine.CurrentDepth) continue;
                if (_rng.NextDouble() < slot.DropRate && mine.RemainingResources > 0)
                {
                    Inventory[slot.OreType]++;
                    mine.RemainingResources--;
                }
            }

            if (mine.IsExhausted)
                LastEvent = $"{mine.Name} est épuisée !";

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

        public double SellToBlacksmith(City city, Blacksmith blacksmith, double negotiationMultiplier = 1.0)
        {
            if (Carriage.State != CarriageState.InCity) return 0;
            if (Carriage.DestinationCityName != city.Name) return 0;
            if (Carriage.TotalCargoCount == 0) return 0;

            int itemCount = Carriage.TotalCargoCount;
            var cargo = Carriage.UnloadAll();
            double totalGold = 0;

            foreach (var kvp in cargo)
            {
                double price = blacksmith.GetPrice(kvp.Key) * negotiationMultiplier;
                totalGold += price * kvp.Value;
            }

            totalGold = Math.Round(totalGold, 1);
            Gold += totalGold;

            bool leveledUp = blacksmith.AddSale(itemCount);
            string msg = $"Vendu à {blacksmith.Name} pour {totalGold:N1} golds !";
            if (leveledUp)
                msg += $" Relation améliorée ! (Nv.{blacksmith.RelationLevel}, +{blacksmith.RelationBonus * 100:F0}% prix)";
            LastEvent = msg;
            return totalGold;
        }

        public (double multiplier, bool success, string message) NegotiateWithBlacksmith(Blacksmith blacksmith)
        {
            return blacksmith.TryNegotiate(_rng);
        }

        /// <summary>
        /// Calculate estimated sale value for display purposes
        /// </summary>
        public double EstimateSaleValue(Blacksmith blacksmith)
        {
            double total = 0;
            foreach (var kvp in Carriage.Cargo)
            {
                total += blacksmith.GetPrice(kvp.Key) * kvp.Value;
            }
            return Math.Round(total, 1);
        }

        public void LoadAllToCarriage()
        {
            if (Carriage.State != CarriageState.AtMine) return;

            foreach (OreType ore in Enum.GetValues<OreType>())
            {
                int available = Inventory[ore];
                if (available <= 0) continue;
                int toLoad = Math.Min(available, Carriage.RemainingSpace);
                if (toLoad <= 0) break;

                Inventory[ore] -= toLoad;
                Carriage.LoadOre(ore, toLoad);
            }
            OnPropertyChanged(nameof(Inventory));
            LastEvent = $"Tout chargé ! Cargo: {Carriage.TotalCargoCount}/{Carriage.Capacity}";
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
            LastEvent = $"Nouveau mineur embauché ! ({AutoMiners.TotalCount} mineurs)";
            OnPropertyChanged(nameof(UnassignedMiners));
            return true;
        }

        public bool AssignMinerToMine(Mine mine, int count)
        {
            if (!mine.IsOwned) return false;
            int available = UnassignedMiners;
            int toAssign = Math.Min(count, available);
            if (toAssign <= 0) return false;

            mine.AssignedMiners += toAssign;
            OnPropertyChanged(nameof(UnassignedMiners));
            LastEvent = $"{toAssign} mineur(s) assigné(s) à {mine.Name} ({mine.AssignedMiners} total)";
            return true;
        }

        public bool UnassignMinerFromMine(Mine mine, int count)
        {
            if (mine.AssignedMiners <= 0) return false;
            int toRemove = Math.Min(count, mine.AssignedMiners);

            mine.AssignedMiners -= toRemove;
            OnPropertyChanged(nameof(UnassignedMiners));
            LastEvent = $"{toRemove} mineur(s) retiré(s) de {mine.Name} ({mine.AssignedMiners} restants)";
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
            OwnedMines.Add(mine);
            RefreshVisibleMines();
            LastEvent = $"Mine achetée : {mine.Name} !";
            return true;
        }

        public void SwitchMine(Mine mine)
        {
            if (!mine.IsOwned) return;
            CurrentMine = mine;
            LastEvent = $"Vous travaillez maintenant dans : {mine.Name}";
        }

        public bool PayBribe()
        {
            if (Gold < BribeCost) return false;

            Gold -= BribeCost;
            RelationLevel++;
            BribeCost = 1000 * Math.Pow(2.5, RelationLevel);
            RefreshVisibleMines();
            LastEvent = $"Pot-de-vin payé ! Relation Nv.{RelationLevel} - Nouvelles mines débloquées !";
            return true;
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
