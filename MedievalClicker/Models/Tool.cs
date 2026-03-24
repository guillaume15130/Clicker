using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MedievalClicker.Models
{
    public class MiningTool : INotifyPropertyChanged
    {
        private string _name = "Pioche en Bois";
        private int _level = 1;
        private int _miningPower = 1;
        private int _depthPower = 1;
        private double _upgradeCost = 50;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public int Level
        {
            get => _level;
            set { _level = value; OnPropertyChanged(); }
        }

        public int MiningPower
        {
            get => _miningPower;
            set { _miningPower = value; OnPropertyChanged(); }
        }

        public int DepthPower
        {
            get => _depthPower;
            set { _depthPower = value; OnPropertyChanged(); }
        }

        public double UpgradeCost
        {
            get => _upgradeCost;
            set { _upgradeCost = value; OnPropertyChanged(); }
        }

        public void Upgrade()
        {
            Level++;
            MiningPower = 1 + Level / 2;
            DepthPower = 1 + Level / 3;
            UpgradeCost = 50 * System.Math.Pow(1.8, Level - 1);
            Name = Level switch
            {
                <= 3 => "Pioche en Bois",
                <= 6 => "Pioche en Pierre",
                <= 10 => "Pioche en Fer",
                <= 15 => "Pioche en Acier",
                <= 20 => "Pioche en Or",
                <= 25 => "Pioche en Diamant",
                <= 30 => "Pioche Magique",
                _ => "Pioche Ancienne"
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
