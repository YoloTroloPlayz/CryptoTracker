using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ProjectOOP
{
    public partial class MainWindow : Window
    {
        private readonly CryptoApiService _api = new();
        private readonly DispatcherTimer _refreshTimer = new();
        private List<CryptoModel> _allCoins = new();
        private HashSet<string> _favorites = new();
        private bool _showFavoritesOnly = false;
        private CryptoModel _selectedCoin;

        private const string FavoritesFile = "favorites.json";

        public MainWindow()
        {
            InitializeComponent();
            LoadFavorites();

            _refreshTimer.Interval = TimeSpan.FromMinutes(1);
            _refreshTimer.Tick += async (s, e) => await LoadDataAsync();
            _refreshTimer.Start();

            Loaded += async (s, e) => await LoadDataAsync();
        }

        // DATA

        private async Task LoadDataAsync()
        {
            StatusDot.Fill = new SolidColorBrush(Colors.Orange);
            StatusText.Text = "Laden...";

            try
            {
                _allCoins = await _api.GetTopCoinsAsync(50);

                // Restore favorite state
                foreach (var c in _allCoins)
                    c.IsFavorite = _favorites.Contains(c.Id);

                await Dispatcher.InvokeAsync(() =>
                {
                    ApplyFilter();
                    StatusDot.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                    StatusText.Text = "Live";
                    LastUpdatedText.Text = $"· bijgewerkt {DateTime.Now:HH:mm}";

                    // Refresh selected coin als et nog steeds in lijst staat
                    if (_selectedCoin != null)
                    {
                        var updated = _allCoins.FirstOrDefault(c => c.Id == _selectedCoin.Id);
                        if (updated != null) ShowDetail(updated);
                    }
                });
            }
            catch (Exception ex)
            {
                StatusDot.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                StatusText.Text = "Fout bij laden";
                MessageBox.Show(ex.Message);
            }
        }

        private void ApplyFilter()
        {
            var query = SearchBox.Text?.Trim().ToLower() ?? "";

            IEnumerable<CryptoModel> coins = _allCoins;

            if (_showFavoritesOnly)
                coins = coins.Where(c => c.IsFavorite);

            if (!string.IsNullOrWhiteSpace(query))
                coins = coins.Where(c =>
                    c.Name.ToLower().Contains(query) ||
                    c.Symbol.ToLower().Contains(query));

            CryptoListBox.ItemsSource = coins.ToList();
        }

        // grafieken

        private void DrawChart(List<double> data)
        {
            ChartCanvas.Children.Clear();
            if (data == null || data.Count < 2) return;

            double w = ChartCanvas.ActualWidth;
            double h = ChartCanvas.ActualHeight;
            if (w < 10 || h < 10) return;

            double min = data.Min();
            double max = data.Max();
            double range = max - min;
            if (range == 0) range = 1;

            double padX = 0, padY = 8;
            bool isPositive = data.Last() >= data.First();

            Color lineColor = isPositive ? Color.FromRgb(16, 185, 129) : Color.FromRgb(239, 68, 68);
            Color fillTop = isPositive
                ? Color.FromArgb(60, 16, 185, 129)
                : Color.FromArgb(60, 239, 68, 68);

            // Build points
            var points = new PointCollection();
            for (int i = 0; i < data.Count; i++)
            {
                double x = padX + (i / (double)(data.Count - 1)) * (w - padX * 2);
                double y = padY + (1 - (data[i] - min) / range) * (h - padY * 2);
                points.Add(new Point(x, y));
            }

            // Fill polygon (gradient effect via opacity)
            var fillPoints = new PointCollection(points) { new Point(points.Last().X, h), new Point(points.First().X, h) };
            var fill = new Polygon
            {
                Points = fillPoints,
                Fill = new SolidColorBrush(fillTop),
            };
            ChartCanvas.Children.Add(fill);

            // Line
            var line = new Polyline
            {
                Points = points,
                Stroke = new SolidColorBrush(lineColor),
                StrokeThickness = 2,
                StrokeLineJoin = PenLineJoin.Round
            };
            ChartCanvas.Children.Add(line);

            // End dot
            var dot = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = new SolidColorBrush(lineColor)
            };
            Canvas.SetLeft(dot, points.Last().X - 4);
            Canvas.SetTop(dot, points.Last().Y - 4);
            ChartCanvas.Children.Add(dot);
        }

        // detail view

        private void ShowDetail(CryptoModel coin)
        {
            _selectedCoin = coin;

            //info
            DetailName.Text = coin.Name;
            DetailSymbol.Text = coin.Symbol;
            DetailRank.Text = $"#{coin.Rank}";
            DetailPrice.Text = coin.PriceFormatted;
            DetailChange.Text = coin.ChangeFormatted;

            bool pos = coin.Change24h >= 0;
            ChangeBadge.Background = new SolidColorBrush(pos
                ? Color.FromArgb(30, 16, 185, 129)
                : Color.FromArgb(30, 239, 68, 68));
            DetailChange.Foreground = coin.ChangeColor;

            StatMarketCap.Text = coin.MarketCapFormatted;
            StatVolume.Text = coin.VolumeFormatted;
            StatSupply.Text = coin.SupplyFormatted;

            FavBtnText.Text = coin.IsFavorite ? "★" : "☆";
            FavBtnText.Foreground = coin.IsFavorite
                ? new SolidColorBrush(Color.FromRgb(240, 180, 41))
                : new SolidColorBrush(Color.FromRgb(107, 114, 128));

            EmptyState.Visibility = Visibility.Collapsed;
            DetailView.Visibility = Visibility.Visible;

            // Wait for render then draw chart
            Dispatcher.InvokeAsync(() => DrawChart(coin.SparklineData),
                DispatcherPriority.Loaded);
        }

        // favorieten

        private void LoadFavorites()
        {
            if (File.Exists(FavoritesFile))
            {
                try
                {
                    var json = File.ReadAllText(FavoritesFile);
                    _favorites = JsonSerializer.Deserialize<HashSet<string>>(json) ?? new();
                }
                catch { _favorites = new(); }
            }
        }

        private void SaveFavorites()
        {
            var json = JsonSerializer.Serialize(_favorites); //naar json bestand schrijven
            File.WriteAllText(FavoritesFile, json);
        }

        // event handlers

        private void CryptoListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CryptoListBox.SelectedItem is CryptoModel coin)
                ShowDetail(coin);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void TabAll_Click(object sender, RoutedEventArgs e)
        {
            _showFavoritesOnly = false;
            TabAll.Background = new SolidColorBrush(Color.FromRgb(26, 30, 42));
            TabAll.Foreground = new SolidColorBrush(Color.FromRgb(240, 180, 41));
            TabFav.Background = new SolidColorBrush(Colors.Transparent);
            TabFav.Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128));
            ApplyFilter();
        }

        private void TabFav_Click(object sender, RoutedEventArgs e)
        {
            _showFavoritesOnly = true;
            TabFav.Background = new SolidColorBrush(Color.FromRgb(26, 30, 42));
            TabFav.Foreground = new SolidColorBrush(Color.FromRgb(240, 180, 41));
            TabAll.Background = new SolidColorBrush(Colors.Transparent);
            TabAll.Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128));
            ApplyFilter();
        }

        private void FavBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCoin == null) return;

            _selectedCoin.IsFavorite = !_selectedCoin.IsFavorite;

            if (_selectedCoin.IsFavorite)
                _favorites.Add(_selectedCoin.Id);
            else
                _favorites.Remove(_selectedCoin.Id);

            SaveFavorites();
            ShowDetail(_selectedCoin);   // Refresh button state
            ApplyFilter();               // Refresh list (voor als er een favoriet aan/uit is gezet)
        }

        private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        // windows controls (eig onnodig want je hebt er al van de window zelf)

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
