using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using FutbinSearch.Models;
using FutbinSearch.Services;

namespace FutbinSearch.ViewModels
{
    /// <summary>
    /// ViewModel da janela de detalhes de uma carta (PlayerDetailWindow).
    /// Carrega dados completos do jogador — sub-estatísticas, preços por plataforma,
    /// histórico de preços e imagens — de forma assíncrona.
    /// </summary>
    public class PlayerDetailViewModel : BaseViewModel
    {
        // ── Dependências ───────────────────────────────────────────────────────────
        private readonly FutbinService _futbinService;

        // ── Estado de carregamento ─────────────────────────────────────────────────

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private bool _isLoadingHistory;
        /// <summary>Indica carregamento específico do histórico de preços.</summary>
        public bool IsLoadingHistory
        {
            get => _isLoadingHistory;
            set => SetProperty(ref _isLoadingHistory, value);
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                SetProperty(ref _errorMessage, value);
                OnPropertyChanged(nameof(HasError));
            }
        }
        public bool HasError => !string.IsNullOrEmpty(_errorMessage);

        // ── Dados da carta ─────────────────────────────────────────────────────────

        private PlayerCard? _card;
        /// <summary>Carta atualmente exibida com todos os dados detalhados.</summary>
        public PlayerCard? Card
        {
            get => _card;
            set
            {
                SetProperty(ref _card, value);
                // Notifica todas as propriedades derivadas da carta
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(CardType));
                OnPropertyChanged(nameof(Rating));
                OnPropertyChanged(nameof(IsGoalkeeper));
            }
        }

        // Propriedades de conveniência para binding direto na View
        public string DisplayName  => Card?.DisplayName ?? string.Empty;
        public string CardType     => Card?.CardType ?? string.Empty;
        public int    Rating       => Card?.Rating ?? 0;
        public bool   IsGoalkeeper => Card?.Stats.IsGoalkeeper ?? false;

        // ── Imagens ────────────────────────────────────────────────────────────────

        private BitmapImage? _cardImage;
        /// <summary>Imagem completa da carta (design + fundo).</summary>
        public BitmapImage? CardImage
        {
            get => _cardImage;
            set => SetProperty(ref _cardImage, value);
        }

        private BitmapImage? _playerPhoto;
        /// <summary>Foto do rosto do jogador.</summary>
        public BitmapImage? PlayerPhoto
        {
            get => _playerPhoto;
            set => SetProperty(ref _playerPhoto, value);
        }

        private BitmapImage? _clubImage;
        public BitmapImage? ClubImage
        {
            get => _clubImage;
            set => SetProperty(ref _clubImage, value);
        }

        private BitmapImage? _nationImage;
        public BitmapImage? NationImage
        {
            get => _nationImage;
            set => SetProperty(ref _nationImage, value);
        }

        // ── Histórico de preços ────────────────────────────────────────────────────

        /// <summary>Lista de pontos de preço para o gráfico de histórico.</summary>
        public ObservableCollection<PriceHistory> PriceHistory { get; } =
            new ObservableCollection<PriceHistory>();

        private bool _hasPriceHistory;
        public bool HasPriceHistory
        {
            get => _hasPriceHistory;
            set => SetProperty(ref _hasPriceHistory, value);
        }

        // ── Estatísticas para exibição em barras ──────────────────────────────────

        /// <summary>
        /// Sub-estatísticas formatadas para exibição em grupos (Velocidade, Chute, etc.).
        /// Cada item: (Label, Valor, MaxValor=99).
        /// </summary>
        public List<StatGroup> StatGroups { get; private set; } = new();

        // ── Plataforma selecionada para preço ─────────────────────────────────────

        private string _selectedPlatform = "PS";
        /// <summary>Plataforma atual para exibição de preço (PS, Xbox, PC).</summary>
        public string SelectedPlatform
        {
            get => _selectedPlatform;
            set
            {
                SetProperty(ref _selectedPlatform, value);
                OnPropertyChanged(nameof(DisplayedPrice));
                OnPropertyChanged(nameof(PriceHistoryForPlatform));
            }
        }

        /// <summary>Preço da plataforma selecionada formatado.</summary>
        public string DisplayedPrice
        {
            get
            {
                if (Card == null) return "N/D";
                var price = SelectedPlatform switch
                {
                    "Xbox" => Card.PriceXbox,
                    "PC"   => Card.PricePC,
                    _      => Card.PricePS,
                };
                return price > 0 ? PlayerCard.FormatPrice(price) : "N/D";
            }
        }

        /// <summary>Pontos do histórico filtrados pela plataforma selecionada.</summary>
        public IEnumerable<PriceHistory> PriceHistoryForPlatform =>
            PriceHistory.Where(h => SelectedPlatform switch
            {
                "Xbox" => h.PriceXbox > 0,
                "PC"   => h.PricePC > 0,
                _      => h.PricePS > 0,
            });

        // ── Comandos ───────────────────────────────────────────────────────────────

        public RelayCommand OpenInBrowserCommand { get; }
        public RelayCommand<string> ChangePlatformCommand { get; }

        // ── Construtor ─────────────────────────────────────────────────────────────

        public PlayerDetailViewModel()
        {
            _futbinService = new FutbinService();

            OpenInBrowserCommand = new RelayCommand(OpenInBrowser, () => !string.IsNullOrEmpty(Card?.FutbinUrl));
            ChangePlatformCommand = new RelayCommand<string>(platform =>
            {
                if (!string.IsNullOrEmpty(platform))
                    SelectedPlatform = platform;
            });
        }

        // ── Carregamento de dados ──────────────────────────────────────────────────

        /// <summary>
        /// Carrega os detalhes completos de uma carta, incluindo:
        /// sub-estatísticas, preços, histórico de preços e imagens.
        /// </summary>
        /// <param name="card">Carta parcial (vinda dos resultados de busca).</param>
        public async Task LoadCardDetailsAsync(PlayerCard card)
        {
            IsLoading    = true;
            ErrorMessage = string.Empty;
            Card         = card; // exibe imediatamente os dados básicos disponíveis

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                // Carrega dados detalhados e imagens em paralelo para máxima performance
                var detailTask  = LoadDetailedPlayerAsync(card.Id, cts.Token);
                var imagesTask  = LoadImagesAsync(card, cts.Token);
                var historyTask = LoadPriceHistoryAsync(card.Id, cts.Token);

                await Task.WhenAll(detailTask, imagesTask, historyTask);
            }
            catch (FutbinException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erro ao carregar detalhes: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Busca e aplica os dados detalhados do jogador (sub-estatísticas, playstyles, etc.).
        /// Se a API não retornar dados completos, mantém os dados básicos já exibidos.
        /// </summary>
        private async Task LoadDetailedPlayerAsync(int playerId, CancellationToken token)
        {
            try
            {
                var detailed = await _futbinService.GetPlayerDetailsAsync(playerId, token);
                if (detailed != null)
                {
                    // Preserva as imagens e preços que podem já ter sido carregados
                    detailed.CardImageUrl   = Card?.CardImageUrl ?? detailed.CardImageUrl;
                    detailed.PlayerImageUrl = Card?.PlayerImageUrl ?? detailed.PlayerImageUrl;
                    detailed.ClubImageUrl   = Card?.ClubImageUrl ?? detailed.ClubImageUrl;
                    detailed.NationImageUrl = Card?.NationImageUrl ?? detailed.NationImageUrl;

                    // Mantém preços se a resposta detalhada veio sem eles
                    if (detailed.PricePS == 0 && Card != null)
                    {
                        detailed.PricePS   = Card.PricePS;
                        detailed.PriceXbox = Card.PriceXbox;
                        detailed.PricePC   = Card.PricePC;
                    }

                    Card = detailed;
                    StatGroups = BuildStatGroups(detailed);
                    OnPropertyChanged(nameof(StatGroups));
                    OnPropertyChanged(nameof(DisplayedPrice));
                }
                else
                {
                    // Se não obteve detalhes, ao menos monta grupos com stats básicas
                    StatGroups = BuildStatGroups(Card!);
                    OnPropertyChanged(nameof(StatGroups));
                }
            }
            catch (Exception)
            {
                // Falha silenciosa — mantém dados parciais já exibidos
                if (Card != null)
                {
                    StatGroups = BuildStatGroups(Card);
                    OnPropertyChanged(nameof(StatGroups));
                }
            }
        }

        /// <summary>
        /// Carrega todas as imagens da carta em paralelo usando o cache de imagens.
        /// </summary>
        private async Task LoadImagesAsync(PlayerCard card, CancellationToken token)
        {
            var imgService = ImageCacheService.Instance;

            var cardImgTask   = imgService.LoadImageAsync(card.CardImageUrl, token);
            var photoTask     = imgService.LoadImageAsync(card.PlayerImageUrl, token);
            var clubImgTask   = imgService.LoadImageAsync(card.ClubImageUrl, token);
            var nationImgTask = imgService.LoadImageAsync(card.NationImageUrl, token);

            await Task.WhenAll(cardImgTask, photoTask, clubImgTask, nationImgTask);

            // Atualiza a UI na thread principal
            CardImage   = cardImgTask.Result;
            PlayerPhoto = photoTask.Result;
            ClubImage   = clubImgTask.Result;
            NationImage = nationImgTask.Result;
        }

        /// <summary>
        /// Busca e popula o histórico de preços do jogador.
        /// </summary>
        private async Task LoadPriceHistoryAsync(int playerId, CancellationToken token)
        {
            IsLoadingHistory = true;
            try
            {
                var history = await _futbinService.GetPriceHistoryAsync(playerId, token);

                PriceHistory.Clear();
                foreach (var point in history)
                    PriceHistory.Add(point);

                HasPriceHistory = PriceHistory.Count > 0;
                OnPropertyChanged(nameof(PriceHistoryForPlatform));
            }
            catch (Exception)
            {
                HasPriceHistory = false;
            }
            finally
            {
                IsLoadingHistory = false;
            }
        }

        // ── Montagem dos grupos de estatísticas ────────────────────────────────────

        /// <summary>
        /// Organiza as estatísticas em grupos para exibição na UI.
        /// Cada grupo tem um nome (ex: "VELOCIDADE") e uma lista de sub-stats.
        /// </summary>
        private static List<StatGroup> BuildStatGroups(PlayerCard card)
        {
            var s = card.Stats;

            if (s.IsGoalkeeper)
            {
                return new List<StatGroup>
                {
                    new("GOLEIRO", new[]
                    {
                        new StatItem("Diving",       s.GKDiving),
                        new StatItem("Handling",     s.GKHandling),
                        new StatItem("Kicking",      s.GKKicking),
                        new StatItem("Reflexos",     s.GKReflexes),
                        new StatItem("Posicionamento", s.GKPositioning),
                        new StatItem("Velocidade",   s.Pace),
                    })
                };
            }

            return new List<StatGroup>
            {
                new("VELOCIDADE", new[]
                {
                    new StatItem("Aceleração",      s.Acceleration),
                    new StatItem("Vel. Sprint",     s.SprintSpeed),
                }),
                new("FINALIZAÇÃO", new[]
                {
                    new StatItem("Posicionamento",  s.Positioning),
                    new StatItem("Finalização",     s.Finishing),
                    new StatItem("Pot. Chute",      s.ShotPower),
                    new StatItem("Chute Longo",     s.LongShots),
                    new StatItem("Voleio",          s.Volleys),
                    new StatItem("Pênaltis",        s.Penalties),
                }),
                new("PASSE", new[]
                {
                    new StatItem("Visão",           s.Vision),
                    new StatItem("Cruzamento",      s.Crossing),
                    new StatItem("Prec. Falta",     s.FKAccuracy),
                    new StatItem("Passe Curto",     s.ShortPassing),
                    new StatItem("Passe Longo",     s.LongPassing),
                    new StatItem("Efeito",          s.Curve),
                }),
                new("DRIBLE", new[]
                {
                    new StatItem("Agilidade",       s.Agility),
                    new StatItem("Equilíbrio",      s.Balance),
                    new StatItem("Reações",         s.Reactions),
                    new StatItem("Ctrl. Bola",      s.BallControl),
                    new StatItem("Drible",          s.DribblingSkill),
                    new StatItem("Composure",       s.Composure),
                }),
                new("DEFESA", new[]
                {
                    new StatItem("Interceptação",   s.Interceptions),
                    new StatItem("Cabeceio",        s.HeadingAccuracy),
                    new StatItem("Consci. Def.",    s.DefensiveAwareness),
                    new StatItem("Carrinho (Pé)",   s.StandingTackle),
                    new StatItem("Carrinho (Dsl.)", s.SlidingTackle),
                }),
                new("FÍSICO", new[]
                {
                    new StatItem("Salto",           s.Jumping),
                    new StatItem("Resistência",     s.Stamina),
                    new StatItem("Força",           s.Strength),
                    new StatItem("Agressividade",   s.Aggression),
                }),
            };
        }

        // ── Ação de abrir no navegador ─────────────────────────────────────────────

        private void OpenInBrowser()
        {
            var url = Card?.FutbinUrl;
            if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName        = url,
                        UseShellExecute = true // abre no navegador padrão do sistema
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Não foi possível abrir o navegador:\n{ex.Message}",
                        "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }

    // ── DTOs internos para grupos de estatísticas ──────────────────────────────────

    /// <summary>Grupo de estatísticas relacionadas (ex: "VELOCIDADE").</summary>
    public record StatGroup(string Name, IEnumerable<StatItem> Stats);

    /// <summary>Item individual de estatística com label e valor (0-99).</summary>
    public record StatItem(string Label, int Value)
    {
        /// <summary>Largura da barra de progresso em pixels (escala 0-99 para 0-180px).</summary>
        public double BarWidth => Math.Round((Value / 99.0) * 180, 1);

        /// <summary>Cor da barra baseada no valor: verde (80+), amarelo (60+), vermelho (abaixo).</summary>
        public string BarColor => Value >= 80 ? "#4CAF50" : Value >= 60 ? "#FFC107" : "#F44336";
    }
}
