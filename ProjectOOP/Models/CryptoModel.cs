using System.ComponentModel;
using System.Windows.Media;

namespace ProjectOOP
{
    // INotifyPropertyChanged zorgt dat de UI automatisch update als een property verandert (nice)
    public class CryptoModel : INotifyPropertyChanged
    {
        // private backing fields voor properties met change-notificatie
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

        // lijst van historische prijspunten grafiek (7 dagen)
        public List<double> SparklineData { get; set; } = new();

        public double Price
        {
            get => _price;
            set
            {
                _price = value;
                // Notify voor zichzelf én de afgeleide formatted property
                OnPropertyChanged(nameof(Price));
                OnPropertyChanged(nameof(PriceFormatted));
            }
        }

        public double Change24h
        {
            get => _change24h;
            set
            {
                _change24h = value;
                OnPropertyChanged(nameof(Change24h));
                OnPropertyChanged(nameof(ChangeFormatted));
                OnPropertyChanged(nameof(ChangeColor)); // kleur moet ook herladen worden
            }
        }

        // Computed properties zonder setter, berekend op basis van Price/Change24h
        public string PriceFormatted => Price >= 1000
            ? $"${Price:N0}"        // geen decimalen boven 1000 (bv. $45,000)
            : Price >= 1
                ? $"${Price:F2}"   // 2 decimalen (bv. $2.45)
                : $"${Price:F6}";  // 6 decimalen voor kleine altcoins (bv. $0.000123)

        public string ChangeFormatted => Change24h >= 0
            ? $"▲ {Change24h:F2}%"
            : $"▼ {Math.Abs(Change24h):F2}%"; // Math.Abs zodat het minteken niet dubbel staat

        // Geeft groen of rood terug als Brush, rechtstreeks bruikbaar in XAML binding
        public Brush ChangeColor => Change24h >= 0
            ? new SolidColorBrush(Color.FromRgb(16, 185, 129))  // groen
            : new SolidColorBrush(Color.FromRgb(239, 68, 68));  // rood

        public string MarketCapFormatted => FormatLargeNumber(MarketCap);
        public string VolumeFormatted => FormatLargeNumber(Volume24h);
        public string SupplyFormatted => FormatLargeNumber(CirculatingSupply, Symbol);

        // Hulpfunctie om grote getallen leesbaar te maken (B/M/K)
        private static string FormatLargeNumber(double value, string suffix = "")
        {
            if (value >= 1_000_000_000) return $"${value / 1_000_000_000:F2}B";
            if (value >= 1_000_000) return $"${value / 1_000_000:F2}M";
            if (value >= 1_000) return $"${value / 1_000:F2}K";

            // Voor circulating supply: toon als "21,000,000 BTC" i.p.v. dollar
            return suffix.Length > 0
                ? $"{value:N0} {suffix.ToUpper()}"
                : $"${value:N0}";
        }

        // Standaard INotifyPropertyChanged implementatie
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}