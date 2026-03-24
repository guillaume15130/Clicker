using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MedievalClicker.Engine;
using MedievalClicker.Models;

namespace MedievalClicker.Views
{
    public partial class MainWindow : Window
    {
        private GameState _game;
        private readonly DispatcherTimer _uiTimer;
        private List<InventoryItem> _inventoryItems = new();

        public MainWindow()
        {
            InitializeComponent();

            // Try to load save, otherwise new game
            _game = SaveSystem.Load() ?? new GameState();

            // UI refresh timer
            _uiTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _uiTimer.Tick += (_, _) => RefreshUI();
            _uiTimer.Start();

            RefreshUI();
        }

        private void MineButton_Click(object sender, RoutedEventArgs e)
        {
            _game.DoClick();
            RefreshUI();
        }

        private void UpgradeTool_Click(object sender, RoutedEventArgs e)
        {
            _game.UpgradeTool();
            RefreshUI();
        }

        private void HireMiner_Click(object sender, RoutedEventArgs e)
        {
            _game.HireMiner();
            RefreshUI();
        }

        private void UpgradeMiners_Click(object sender, RoutedEventArgs e)
        {
            _game.UpgradeMiners();
            RefreshUI();
        }

        private void UpgradeCarriage_Click(object sender, RoutedEventArgs e)
        {
            _game.UpgradeCarriage();
            RefreshUI();
        }

        private void HireSoldier_Click(object sender, RoutedEventArgs e)
        {
            _game.HireSoldier();
            RefreshUI();
        }

        private void Bribe_Click(object sender, RoutedEventArgs e)
        {
            _game.PayBribe();
            RefreshUI();
        }

        private void LoadOre_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is InventoryItem item)
            {
                int amount = item.LoadAmount;
                if (amount <= 0) return;
                _game.LoadOreToCarriage(item.OreType, amount);
                RefreshUI();
            }
        }

        private void LoadAllOres_Click(object sender, RoutedEventArgs e)
        {
            _game.LoadAllToCarriage();
            RefreshUI();
        }

        private void SendCarriage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is City city)
            {
                _game.SendCarriage(city);
                RefreshUI();
            }
        }

        private void ReturnCarriage_Click(object sender, RoutedEventArgs e)
        {
            _game.ReturnCarriage();
            RefreshUI();
        }

        private void SellToBlacksmith_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Blacksmith smith)
            {
                var city = _game.Cities.FirstOrDefault(c => c.Blacksmiths.Contains(smith));
                if (city == null) return;

                if (_game.Carriage.State != CarriageState.InCity)
                {
                    _game.LastEvent = "La calèche n'est dans aucune ville !";
                }
                else if (_game.Carriage.DestinationCityName != city.Name)
                {
                    _game.LastEvent = $"La calèche est à {_game.Carriage.DestinationCityName}, pas à {city.Name} !";
                }
                else
                {
                    _game.SellToBlacksmith(city, smith);
                }
                RefreshUI();
            }
        }

        private void BuyMine_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Mine mine)
            {
                _game.BuyMine(mine);
                RefreshUI();
            }
        }

        private void SwitchMine_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Mine mine)
            {
                _game.SwitchMine(mine);
                RefreshUI();
            }
        }

        private void AssignMiner_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Mine mine)
            { _game.AssignMinerToMine(mine, 1); RefreshUI(); }
        }

        private void AssignMiner5_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Mine mine)
            { _game.AssignMinerToMine(mine, 5); RefreshUI(); }
        }

        private void UnassignMiner_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Mine mine)
            { _game.UnassignMinerFromMine(mine, 1); RefreshUI(); }
        }

        private void UnassignMiner5_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Mine mine)
            { _game.UnassignMinerFromMine(mine, 5); RefreshUI(); }
        }

        private void RefreshMines_Click(object sender, RoutedEventArgs e)
        {
            _game.RefreshVisibleMines();
            RefreshUI();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveSystem.Save(_game);
                _game.LastEvent = "Partie sauvegardée !";
            }
            catch (Exception ex)
            {
                _game.LastEvent = $"Erreur de sauvegarde : {ex.Message}";
            }
            RefreshUI();
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            var loaded = SaveSystem.Load();
            if (loaded != null)
            {
                _game = loaded;
                _inventoryItems = new();
                _game.LastEvent = "Partie chargée !";
            }
            else
            {
                _game.LastEvent = "Aucune sauvegarde trouvée.";
            }
            RefreshUI();
        }

        private void RefreshUI()
        {
            // Gold
            GoldDisplay.Text = _game.GoldDisplay;

            // Event
            EventDisplay.Text = _game.LastEvent;

            // Mine info
            MineNameText.Text = _game.CurrentMine.IsExhausted
                ? $"{_game.CurrentMine.Name} [ÉPUISÉE]"
                : _game.CurrentMine.Name;
            MineDepthText.Text = $"Profondeur: {_game.CurrentMine.DepthDisplay}";
            MineDepthBar.Maximum = _game.CurrentMine.MaxDepth;
            MineDepthBar.Value = _game.CurrentMine.CurrentDepth;

            // Resources
            MineResourcesText.Text = $"Ressources: {_game.CurrentMine.ResourcesDisplay}";
            MineResourcesBar.Maximum = _game.CurrentMine.TotalResources;
            MineResourcesBar.Value = _game.CurrentMine.RemainingResources;

            // Meter progress
            MeterProgressText.Text = _game.CurrentMine.MeterDisplay;
            MeterProgressBar.Maximum = _game.CurrentMine.HitsPerMeter;
            MeterProgressBar.Value = _game.CurrentMine.CurrentMeterHits;

            // Distance
            MineDistanceText.Text = $"Distance des villes: x{_game.CurrentMine.DistanceMultiplier:F1}";

            // Tool
            ToolInfoText.Text = $"{_game.Tool.Name} (Nv.{_game.Tool.Level}) - Puissance: {_game.Tool.MiningPower}";

            // Miners
            MinersInfoText.Text = $"Mineurs ici: {_game.CurrentMine.AssignedMiners} | Total: {_game.AutoMiners.TotalCount} (Nv.{_game.AutoMiners.Level})";

            // Available ores at depth
            var ores = _game.CurrentMine.GetOresAtCurrentDepth();
            AvailableOresText.Text = ores.Count > 0
                ? string.Join(", ", ores.Select(o => $"{OreInfo.GetEmoji(o)} {OreInfo.GetName(o)}"))
                : "Aucun minerai (creusez plus profond !)";

            // Inventory with quantity selector - update in place to preserve user input
            if (_inventoryItems.Count == 0)
            {
                _inventoryItems = _game.Inventory
                    .Select(kvp => new InventoryItem
                    {
                        OreType = kvp.Key,
                        Name = $"{OreInfo.GetEmoji(kvp.Key)} {OreInfo.GetName(kvp.Key)}",
                        Count = kvp.Value,
                        LoadAmount = kvp.Value
                    })
                    .ToList();
                InventoryList.ItemsSource = _inventoryItems;
            }
            else
            {
                foreach (var item in _inventoryItems)
                    item.Count = _game.Inventory[item.OreType];
            }

            // Carriage
            CarriageStateText.Text = _game.Carriage.StateDisplay;
            CarriageCargoText.Text = $"Cargo: {_game.Carriage.TotalCargoCount} / {_game.Carriage.Capacity}";
            CarriageSoldiersText.Text = $"Soldats: {_game.Carriage.Soldiers}";

            bool isTraveling = _game.Carriage.State == CarriageState.TravelingToCity
                            || _game.Carriage.State == CarriageState.TravelingToMine;
            CarriageTravelBar.Visibility = isTraveling ? Visibility.Visible : Visibility.Collapsed;
            if (isTraveling)
            {
                CarriageTravelBar.Value = _game.Carriage.TravelProgressPercent;
            }

            // Cities state
            bool carriageAtMine = _game.Carriage.State == CarriageState.AtMine;
            bool carriageInCity = _game.Carriage.State == CarriageState.InCity;
            CitiesList.ItemsSource = _game.Cities.Select(c => new CityViewModel
            {
                City = c,
                IsCarriageHere = carriageInCity && _game.Carriage.DestinationCityName == c.Name,
                CanSendCarriage = carriageAtMine && _game.Carriage.TotalCargoCount > 0,
                StatusText = (carriageInCity && _game.Carriage.DestinationCityName == c.Name) ? "[CALÈCHE ICI]" : ""
            }).ToList();

            ReturnCarriageBtn.IsEnabled = carriageInCity;

            // Relation / Bribe
            RelationText.Text = $"Niveau de relation: {_game.RelationLevel}";
            BribeBtn.Content = $"Pot-de-vin ({_game.BribeCost:N0}g)";

            // Owned mines
            UnassignedMinersText.Text = $"Mineurs disponibles: {_game.UnassignedMiners}";
            OwnedMinesList.ItemsSource = _game.OwnedMines;

            // Shop mines
            MinesShopList.ItemsSource = _game.VisibleShopMines;

            // Upgrade button texts with costs
            UpgradeToolBtn.Content = $"Améliorer Pioche ({_game.Tool.UpgradeCost:N0}g)";
            HireMinerBtn.Content = $"Embaucher Mineur ({_game.AutoMiners.HireCost:N0}g)";
            UpgradeMinersBtn.Content = $"Améliorer Mineurs ({_game.AutoMiners.UpgradeCost:N0}g)";
            UpgradeCarriageBtn.Content = $"Améliorer Calèche ({_game.Carriage.UpgradeCost:N0}g)";
            HireSoldierBtn.Content = $"Engager Soldat ({_game.Carriage.SoldierHireCost:N0}g)";

            // Mine depth warning
            if (_game.CurrentMine.IsMaxDepthReached)
            {
                MineDepthText.Text += " - PROFONDEUR MAX !";
            }
        }
    }

    public class InventoryItem : INotifyPropertyChanged
    {
        public OreType OreType { get; set; }
        public string Name { get; set; } = "";

        private int _count;
        public int Count
        {
            get => _count;
            set { _count = value; OnPropertyChanged(); }
        }

        private int _loadAmount;
        public int LoadAmount
        {
            get => _loadAmount;
            set { _loadAmount = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class CityViewModel
    {
        public City City { get; set; } = null!;
        public bool IsCarriageHere { get; set; }
        public bool CanSendCarriage { get; set; }
        public string StatusText { get; set; } = "";
    }
}
