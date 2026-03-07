using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FutbinSearch.Models;
using FutbinSearch.ViewModels;

namespace FutbinSearch.Views
{
    /// <summary>
    /// Code-behind da janela de detalhes de uma carta.
    /// Responsável por:
    /// 1. Iniciar o carregamento dos dados ao abrir a janela.
    /// 2. Desenhar o gráfico de histórico de preços no Canvas.
    /// 3. Popular o ItemsControl de stats principais.
    /// </summary>
    public partial class PlayerDetailWindow : Window
    {
        private readonly PlayerDetailViewModel _viewModel;

        /// <summary>
        /// Cria a janela de detalhes para a carta especificada.
        /// </summary>
        /// <param name="card">Carta com dados básicos (vinda dos resultados de busca).</param>
        public PlayerDetailWindow(PlayerCard card)
        {
            InitializeComponent();

            _viewModel  = new PlayerDetailViewModel();
            DataContext = _viewModel;

            // Título da janela com o nome do jogador
            Title = $"{card.DisplayName} — Detalhes da Carta";

            // Quando o histórico de preços for carregado, redesenha o gráfico
            _viewModel.PriceHistory.CollectionChanged += (_, _) => DrawPriceChart();

            // Inicia o carregamento assíncrono ao abrir a janela
            Loaded += async (_, _) => await LoadCardAsync(card);
        }

        // ── Carregamento de dados ──────────────────────────────────────────────────

        /// <summary>
        /// Inicia o carregamento completo dos detalhes da carta e atualiza a UI.
        /// </summary>
        private async Task LoadCardAsync(PlayerCard card)
        {
            await _viewModel.LoadCardDetailsAsync(card);

            // Após carregar, popula o ItemsControl de stats principais
            PopulateMainStats();
        }

        // ── Stats principais ───────────────────────────────────────────────────────

        /// <summary>
        /// Popula o controle de stats principais (PAC, SHO, PAS, DRI, DEF, PHY)
        /// com objetos StatItem para reutilizar o template XAML.
        /// </summary>
        private void PopulateMainStats()
        {
            if (_viewModel.Card == null) return;

            var stats = _viewModel.Card.Stats;

            // Goleiros têm stats diferentes
            List<StatItem> mainStats;
            if (stats.IsGoalkeeper)
            {
                mainStats = new List<StatItem>
                {
                    new("DIV", stats.GKDiving),
                    new("HAN", stats.GKHandling),
                    new("KIC", stats.GKKicking),
                    new("REF", stats.GKReflexes),
                    new("SPD", stats.Pace),
                    new("POS", stats.GKPositioning),
                };
            }
            else
            {
                mainStats = new List<StatItem>
                {
                    new("PAC", stats.Pace),
                    new("SHO", stats.Shooting),
                    new("PAS", stats.Passing),
                    new("DRI", stats.Dribbling),
                    new("DEF", stats.Defending),
                    new("PHY", stats.Physical),
                };
            }

            MainStatsControl.ItemsSource = mainStats;
        }

        // ── Gráfico de histórico de preços ────────────────────────────────────────

        /// <summary>
        /// Desenha o gráfico de linha do histórico de preços no Canvas.
        /// Usa WPF Shapes (Polyline, Ellipse) para renderização nativa — sem dependências externas.
        /// </summary>
        private void DrawPriceChart()
        {
            // O Canvas precisa ser acessado na thread de UI
            Dispatcher.Invoke(() =>
            {
                PriceChart.Children.Clear();

                var data = _viewModel.PriceHistory
                    .Where(h => h.PricePS > 0)
                    .ToList();

                if (data.Count < 2) return;

                // ── Dimensões do canvas ─────────────────────────────────────────────
                const double paddingLeft   = 10;
                const double paddingRight  = 10;
                const double paddingTop    = 8;
                const double paddingBottom = 8;

                double canvasWidth  = PriceChart.ActualWidth  > 0 ? PriceChart.ActualWidth  : 240;
                double canvasHeight = PriceChart.ActualHeight > 0 ? PriceChart.ActualHeight : 110;

                double chartWidth  = canvasWidth  - paddingLeft - paddingRight;
                double chartHeight = canvasHeight - paddingTop  - paddingBottom;

                // ── Escala: min/max de preço → coordenadas Y ────────────────────────
                long minPrice = data.Min(h => h.PricePS);
                long maxPrice = data.Max(h => h.PricePS);
                long priceRange = maxPrice - minPrice;
                if (priceRange == 0) priceRange = 1; // evita divisão por zero

                // Função de mapeamento: preço → Y no canvas (Y cresce para baixo)
                double PriceToY(long price) =>
                    paddingTop + chartHeight - ((price - minPrice) / (double)priceRange * chartHeight);

                // Função de mapeamento: índice → X no canvas
                double IndexToX(int i) =>
                    paddingLeft + (i / (double)(data.Count - 1)) * chartWidth;

                // ── Linhas de grade horizontais ─────────────────────────────────────
                for (int g = 0; g <= 3; g++)
                {
                    double y = paddingTop + (g / 3.0) * chartHeight;
                    PriceChart.Children.Add(new Line
                    {
                        X1              = paddingLeft,
                        Y1              = y,
                        X2              = canvasWidth - paddingRight,
                        Y2              = y,
                        Stroke          = new SolidColorBrush(Color.FromArgb(60, 100, 100, 180)),
                        StrokeThickness = 1,
                        StrokeDashArray = new DoubleCollection { 4, 4 },
                    });
                }

                // ── Área sob a linha (gradiente semi-transparente) ─────────────────
                var areaPoints = new PointCollection();
                areaPoints.Add(new Point(IndexToX(0), canvasHeight - paddingBottom)); // início embaixo

                for (int i = 0; i < data.Count; i++)
                    areaPoints.Add(new Point(IndexToX(i), PriceToY(data[i].PricePS)));

                areaPoints.Add(new Point(IndexToX(data.Count - 1), canvasHeight - paddingBottom)); // final embaixo

                PriceChart.Children.Add(new Polygon
                {
                    Points = areaPoints,
                    Fill   = new LinearGradientBrush(
                        Color.FromArgb(120, 61, 107, 204),  // #3D6BCC semi-transparente
                        Color.FromArgb(10,  61, 107, 204),  // quase transparente
                        new Point(0, 0), new Point(0, 1)),
                    StrokeThickness = 0,
                });

                // ── Linha principal do gráfico ─────────────────────────────────────
                var linePoints = new PointCollection(
                    data.Select((h, i) => new Point(IndexToX(i), PriceToY(h.PricePS))));

                PriceChart.Children.Add(new Polyline
                {
                    Points          = linePoints,
                    Stroke          = new SolidColorBrush(Color.FromRgb(61, 107, 204)), // #3D6BCC
                    StrokeThickness = 2,
                    StrokeLineJoin  = PenLineJoin.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap   = PenLineCap.Round,
                });

                // ── Pontos de dados (círculos) — apenas para conjuntos pequenos ─────
                if (data.Count <= 30)
                {
                    for (int i = 0; i < data.Count; i++)
                    {
                        double x = IndexToX(i);
                        double y = PriceToY(data[i].PricePS);

                        var dot = new Ellipse
                        {
                            Width  = 5,
                            Height = 5,
                            Fill   = new SolidColorBrush(Color.FromRgb(100, 160, 255)),
                        };

                        Canvas.SetLeft(dot, x - 2.5);
                        Canvas.SetTop(dot,  y - 2.5);
                        PriceChart.Children.Add(dot);
                    }
                }

                // ── Label de preço no ponto mais recente ───────────────────────────
                var lastPoint = data[data.Count - 1];
                double lastX  = IndexToX(data.Count - 1);
                double lastY  = PriceToY(lastPoint.PricePS);

                var priceLabel = new TextBlock
                {
                    Text       = lastPoint.PricePSFormatted,
                    FontSize   = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)), // #FFD700
                    FontWeight = FontWeights.Bold,
                };

                Canvas.SetLeft(priceLabel, Math.Max(0, lastX - 20));
                Canvas.SetTop(priceLabel,  Math.Max(0, lastY - 16));
                PriceChart.Children.Add(priceLabel);
            });
        }
    }
}
