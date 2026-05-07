using System.ComponentModel;
using System.Windows.Media;

namespace ProjectOOP
{
    public class CryptoModel : INotifyPropertyChanged
    {
        private double _price;
        private double _change24h;

        public string Id { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public int Rank { get; set; }
        public double MarketCap { get; set; }
        public double Volume24h { get; set; }
        public double CirculatingSupply { get; set; }
        public bool IsFavorite { get; set; }

        public List<double> SparklineData { get; set; } = new();

        public double Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(nameof(Price)); OnPropertyChanged(nameof(PriceFormatted)); }
        }

        public double Change24h
        {
            get => _change24h;
            set { _change24h = value; OnPropertyChanged(nameof(Change24h)); OnPropertyChanged(nameof(ChangeFormatted)); OnPropertyChanged(nameof(ChangeColor)); }
        }

        public string PriceFormatted => Price >= 1000
            ? $"${Price:N0}"
            : Price >= 1
                ? $"${Price:F2}"
                : $"${Price:F6}";

        public string ChangeFormatted => Change24h >= 0
            ? $"▲ {Change24h:F2}%"
            : $"▼ {Math.Abs(Change24h):F2}%";

        public Brush ChangeColor => Change24h >= 0
            ? new SolidColorBrush(Color.FromRgb(16, 185, 129))
            : new SolidColorBrush(Color.FromRgb(239, 68, 68));

        public string MarketCapFormatted => FormatLargeNumber(MarketCap);
        public string VolumeFormatted => FormatLargeNumber(Volume24h);
        public string SupplyFormatted => FormatLargeNumber(CirculatingSupply, Symbol);

        private static string FormatLargeNumber(double value, string suffix = "")
        {
            if (value >= 1_000_000_000) return $"${value / 1_000_000_000:F2}B";
            if (value >= 1_000_000) return $"${value / 1_000_000:F2}M";
            if (value >= 1_000) return $"${value / 1_000:F2}K";
            return suffix.Length > 0
                ? $"{value:N0} {suffix.ToUpper()}"
                : $"${value:N0}";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
