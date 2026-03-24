using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MedievalClicker.Models
{
    public class AutoMiner : INotifyPropertyChanged
    {
        private int _count;
        private double _miningSpeed = 1.0;
        private int _level = 1;
        private double _hireCost = 100;
        private double _upgradeCost = 200;

        public int Count
        {
            get => _count;
            set { _count = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalOutput)); }
        }

        public double MiningSpeed
        {
            get => _miningSpeed;
            set { _miningSpeed = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalOutput)); }
        }

        public int Level
        {
            get => _level;
            set { _level = value; OnPropertyChanged(); }
        }

        public double HireCost
        {
            get => _hireCost;
            set { _hireCost = value; OnPropertyChanged(); }
        }

        public double UpgradeCost
        {
            get => _upgradeCost;
            set { _upgradeCost = value; OnPropertyChanged(); }
        }

        public double TotalOutput => Count * MiningSpeed;

        public void Hire()
        {
            Count++;
            HireCost = 100 * System.Math.Pow(1.5, Count);
        }

        public void UpgradeAll()
        {
            Level++;
            MiningSpeed = 1.0 + (Level - 1) * 0.5;
            UpgradeCost = 200 * System.Math.Pow(2.0, Level - 1);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
