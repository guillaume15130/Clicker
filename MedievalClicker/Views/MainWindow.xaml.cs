using System;
using System.Collections.Generic;
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
        private readonly GameState _game;
        private readonly DispatcherTimer _uiTimer;

        public MainWindow()
        {
            InitializeComponent();

            _game = new GameState();

            // Set up data sources
            MinesShopList.ItemsSource = _game.ShopMines;
            CitiesList.ItemsSource = _game.Cities;

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

        private void LoadOre_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is OreType ore)
            {
                _game.LoadOreToCarriage(ore, 10);
                RefreshUI();
            }
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
                _game.SellToBlacksmith(smith);
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

            // Distance
            MineDistanceText.Text = $"Distance des villes: x{_game.CurrentMine.DistanceMultiplier:F1}";

            // Tool
            ToolInfoText.Text = $"{_game.Tool.Name} (Nv.{_game.Tool.Level}) - Puissance: {_game.Tool.MiningPower}";

            // Miners
            MinersInfoText.Text = $"Mineurs: {_game.AutoMiners.Count} (Nv.{_game.AutoMiners.Level}) - {_game.AutoMiners.TotalOutput:F1}/s";

            // Available ores at depth
            var ores = _game.CurrentMine.GetOresAtCurrentDepth();
            AvailableOresText.Text = ores.Count > 0
                ? string.Join(", ", ores.Select(o => $"{OreInfo.GetEmoji(o)} {OreInfo.GetName(o)}"))
                : "Aucun minerai (creusez plus profond !)";

            // Inventory
            var inventoryItems = _game.Inventory
                .Select(kvp => new InventoryItem
                {
                    OreType = kvp.Key,
                    Name = $"{OreInfo.GetEmoji(kvp.Key)} {OreInfo.GetName(kvp.Key)}",
                    Count = kvp.Value
                })
                .ToList();
            InventoryList.ItemsSource = inventoryItems;

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

    public class InventoryItem
    {
        public OreType OreType { get; set; }
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }
}
