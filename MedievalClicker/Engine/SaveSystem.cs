using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MedievalClicker.Models;

namespace MedievalClicker.Engine
{
    public class SaveData
    {
        public double Gold { get; set; }
        public int RelationLevel { get; set; }
        public double BribeCost { get; set; }
        public Dictionary<OreType, int> Inventory { get; set; } = new();

        // Current mine ID
        public int CurrentMineId { get; set; }

        // Starter mine state
        public MineSaveData StarterMine { get; set; } = new();

        // All mines state
        public List<MineSaveData> AllMines { get; set; } = new();

        // Tool
        public int ToolLevel { get; set; }

        // AutoMiners
        public int MinerCount { get; set; }
        public int MinerLevel { get; set; }

        // Carriage
        public int CarriageLevel { get; set; }
        public int CarriageSoldiers { get; set; }

        // Cities (for blacksmith prices consistency)
        public List<CitySaveData> Cities { get; set; } = new();

        public string SaveDate { get; set; } = "";
    }

    public class MineSaveData
    {
        public int MineId { get; set; }
        public string Name { get; set; } = "";
        public int CurrentDepth { get; set; }
        public int MaxDepth { get; set; }
        public bool IsOwned { get; set; }
        public double PurchasePrice { get; set; }
        public int RemainingResources { get; set; }
        public int TotalResources { get; set; }
        public double DistanceMultiplier { get; set; }
        public int RequiredRelationLevel { get; set; }
        public int CurrentMeterHits { get; set; }
        public int HitsPerMeter { get; set; }
        public int AssignedMiners { get; set; }
        public List<OreSlotSaveData> AvailableOres { get; set; } = new();
    }

    public class OreSlotSaveData
    {
        public OreType OreType { get; set; }
        public int MinDepth { get; set; }
        public double DropRate { get; set; }
    }

    public class CitySaveData
    {
        public string Name { get; set; } = "";
        public double TravelTimeSeconds { get; set; }
        public double BrigandDanger { get; set; }
        public List<BlacksmithSaveData> Blacksmiths { get; set; } = new();
    }

    public class BlacksmithSaveData
    {
        public string Name { get; set; } = "";
        public Dictionary<OreType, double> Prices { get; set; } = new();
    }

    public static class SaveSystem
    {
        private static readonly string SaveFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MedievalClicker");

        private static readonly string SavePath = Path.Combine(SaveFolder, "save.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public static void Save(GameState game)
        {
            var data = new SaveData
            {
                Gold = game.Gold,
                RelationLevel = game.RelationLevel,
                BribeCost = game.BribeCost,
                Inventory = new Dictionary<OreType, int>(game.Inventory),
                CurrentMineId = game.CurrentMine.MineId,
                StarterMine = MineToSave(game.OwnedMines[0]),
                ToolLevel = game.Tool.Level,
                MinerCount = game.AutoMiners.TotalCount,
                MinerLevel = game.AutoMiners.Level,
                CarriageLevel = game.Carriage.Level,
                CarriageSoldiers = game.Carriage.Soldiers,
                SaveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            foreach (var mine in game.AllMines)
                data.AllMines.Add(MineToSave(mine));

            foreach (var city in game.Cities)
            {
                var cs = new CitySaveData
                {
                    Name = city.Name,
                    TravelTimeSeconds = city.TravelTimeSeconds,
                    BrigandDanger = city.BrigandDanger
                };
                foreach (var bs in city.Blacksmiths)
                {
                    cs.Blacksmiths.Add(new BlacksmithSaveData
                    {
                        Name = bs.Name,
                        Prices = new Dictionary<OreType, double>(bs.Prices)
                    });
                }
                data.Cities.Add(cs);
            }

            Directory.CreateDirectory(SaveFolder);
            string json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(SavePath, json);
        }

        public static bool HasSave()
        {
            return File.Exists(SavePath);
        }

        public static GameState? Load()
        {
            if (!File.Exists(SavePath)) return null;

            try
            {
                string json = File.ReadAllText(SavePath);
                var data = JsonSerializer.Deserialize<SaveData>(json, JsonOptions);
                if (data == null) return null;

                var game = new GameState(skipInit: true);

                // Restore gold and relation
                game.Gold = data.Gold;
                game.RelationLevel = data.RelationLevel;
                game.BribeCost = data.BribeCost;

                // Restore inventory
                foreach (var kvp in data.Inventory)
                    game.Inventory[kvp.Key] = kvp.Value;

                // Restore starter mine
                var starter = SaveToMine(data.StarterMine);
                starter.IsOwned = true;

                // Restore all mines
                game.AllMines = new List<Mine>();
                foreach (var ms in data.AllMines)
                    game.AllMines.Add(SaveToMine(ms));

                // Rebuild owned mines list
                game.OwnedMines = new List<Mine> { starter };
                foreach (var mine in game.AllMines)
                {
                    if (mine.IsOwned)
                        game.OwnedMines.Add(mine);
                }

                // Set current mine
                if (data.CurrentMineId == 0)
                {
                    game.CurrentMine = starter;
                }
                else
                {
                    game.CurrentMine = game.AllMines.Find(m => m.MineId == data.CurrentMineId) ?? starter;
                }

                // Restore tool
                for (int i = 1; i < data.ToolLevel; i++)
                    game.Tool.Upgrade();

                // Restore miners
                for (int i = 0; i < data.MinerCount; i++)
                    game.AutoMiners.Hire();
                for (int i = 1; i < data.MinerLevel; i++)
                    game.AutoMiners.UpgradeAll();

                // Restore carriage
                for (int i = 1; i < data.CarriageLevel; i++)
                    game.Carriage.Upgrade();
                for (int i = 0; i < data.CarriageSoldiers; i++)
                    game.Carriage.HireSoldier();

                // Restore cities
                if (data.Cities.Count > 0)
                {
                    game.Cities = new List<City>();
                    foreach (var cs in data.Cities)
                    {
                        var city = new City
                        {
                            Name = cs.Name,
                            TravelTimeSeconds = cs.TravelTimeSeconds,
                            BrigandDanger = cs.BrigandDanger
                        };
                        foreach (var bs in cs.Blacksmiths)
                        {
                            city.Blacksmiths.Add(new Blacksmith(bs.Name, new Dictionary<OreType, double>(bs.Prices)));
                        }
                        game.Cities.Add(city);
                    }
                }

                game.RefreshVisibleMines();
                game.StartTimer();
                return game;
            }
            catch
            {
                return null;
            }
        }

        private static MineSaveData MineToSave(Mine mine)
        {
            var ms = new MineSaveData
            {
                MineId = mine.MineId,
                Name = mine.Name,
                CurrentDepth = mine.CurrentDepth,
                MaxDepth = mine.MaxDepth,
                IsOwned = mine.IsOwned,
                PurchasePrice = mine.PurchasePrice,
                RemainingResources = mine.RemainingResources,
                TotalResources = mine.TotalResources,
                DistanceMultiplier = mine.DistanceMultiplier,
                RequiredRelationLevel = mine.RequiredRelationLevel,
                CurrentMeterHits = mine.CurrentMeterHits,
                HitsPerMeter = mine.HitsPerMeter,
                AssignedMiners = mine.AssignedMiners
            };
            foreach (var slot in mine.AvailableOres)
            {
                ms.AvailableOres.Add(new OreSlotSaveData
                {
                    OreType = slot.OreType,
                    MinDepth = slot.MinDepth,
                    DropRate = slot.DropRate
                });
            }
            return ms;
        }

        private static Mine SaveToMine(MineSaveData data)
        {
            var mine = new Mine
            {
                MineId = data.MineId,
                Name = data.Name,
                CurrentDepth = data.CurrentDepth,
                MaxDepth = data.MaxDepth,
                IsOwned = data.IsOwned,
                PurchasePrice = data.PurchasePrice,
                RemainingResources = data.RemainingResources,
                TotalResources = data.TotalResources,
                DistanceMultiplier = data.DistanceMultiplier,
                RequiredRelationLevel = data.RequiredRelationLevel,
                CurrentMeterHits = data.CurrentMeterHits,
                HitsPerMeter = data.HitsPerMeter > 0 ? data.HitsPerMeter : 10,
                AssignedMiners = data.AssignedMiners
            };
            foreach (var slot in data.AvailableOres)
            {
                mine.AvailableOres.Add(new MineOreSlot(slot.OreType, slot.MinDepth, slot.DropRate));
            }
            return mine;
        }
    }
}
