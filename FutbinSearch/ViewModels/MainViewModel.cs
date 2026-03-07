using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using FutbinSearch.Models;
using FutbinSearch.Services;

namespace FutbinSearch.ViewModels
{
    /// <summary>
    /// ViewModel da tela principal (MainWindow).
    /// Gerencia o estado da busca, filtros, lista de resultados e carregamento de mais páginas.
    /// Segue o padrão MVVM: a View se liga às propriedades via binding e
    /// dispara comandos sem precisar de lógica no code-behind.
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        // ── Dependências ───────────────────────────────────────────────────────────
        private readonly FutbinService _futbinService;

        // ── Estado de controle ─────────────────────────────────────────────────────
        private CancellationTokenSource? _searchCts; // cancela buscas anteriores

        // ── Propriedades de estado ─────────────────────────────────────────────────

        private bool _isLoading;
        /// <summary>Indica se há uma operação de busca em andamento (mostra spinner).</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _statusMessage = "Pronto. Digite um nome ou aplique filtros para buscar.";
        /// <summary>Mensagem exibida na barra de status inferior da janela.</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private string _errorMessage = string.Empty;
        /// <summary>Mensagem de erro visível ao usuário (vazia = sem erro).</summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                SetProperty(ref _errorMessage, value);
                OnPropertyChanged(nameof(HasError));
            }
        }

        /// <summary>Indica se há uma mensagem de erro para exibir.</summary>
        public bool HasError => !string.IsNullOrEmpty(_errorMessage);

        private bool _hasResults;
        /// <summary>True quando há pelo menos uma carta na lista de resultados.</summary>
        public bool HasResults
        {
            get => _hasResults;
            set => SetProperty(ref _hasResults, value);
        }

        private bool _canLoadMore;
        /// <summary>Indica se há mais páginas de resultado para carregar.</summary>
        public bool CanLoadMore
        {
            get => _canLoadMore;
            set => SetProperty(ref _canLoadMore, value);
        }

        // ── Filtros de busca ───────────────────────────────────────────────────────

        private string _searchTerm = string.Empty;
        /// <summary>Texto digitado no campo de busca pelo nome do jogador.</summary>
        public string SearchTerm
        {
            get => _searchTerm;
            set => SetProperty(ref _searchTerm, value);
        }

        private string _selectedVersion = string.Empty;
        /// <summary>Versão/tipo de carta selecionado no ComboBox.</summary>
        public string SelectedVersion
        {
            get => _selectedVersion;
            set => SetProperty(ref _selectedVersion, value);
        }

        private int _minRating = 60;
        /// <summary>Rating mínimo do slider de filtro.</summary>
        public int MinRating
        {
            get => _minRating;
            set => SetProperty(ref _minRating, value);
        }

        private int _maxRating = 99;
        /// <summary>Rating máximo do slider de filtro.</summary>
        public int MaxRating
        {
            get => _maxRating;
            set => SetProperty(ref _maxRating, value);
        }

        private string _selectedPosition = string.Empty;
        /// <summary>Posição selecionada (ST, CAM, CB, etc.).</summary>
        public string SelectedPosition
        {
            get => _selectedPosition;
            set => SetProperty(ref _selectedPosition, value);
        }

        private string _selectedPlaystyle = string.Empty;
        /// <summary>Playstyle selecionado para filtro.</summary>
        public string SelectedPlaystyle
        {
            get => _selectedPlaystyle;
            set => SetProperty(ref _selectedPlaystyle, value);
        }

        // IDs e nomes das entidades selecionadas (Liga, Time, País)
        public int? SelectedLeagueId { get; private set; }
        public string SelectedLeagueName { get; private set; } = string.Empty;
        public int? SelectedTeamId { get; private set; }
        public string SelectedTeamName { get; private set; } = string.Empty;
        public int? SelectedNationId { get; private set; }
        public string SelectedNationName { get; private set; } = string.Empty;

        private string _leagueDisplay = "Todas as Ligas";
        public string LeagueDisplay
        {
            get => _leagueDisplay;
            set => SetProperty(ref _leagueDisplay, value);
        }

        private string _teamDisplay = "Todos os Times";
        public string TeamDisplay
        {
            get => _teamDisplay;
            set => SetProperty(ref _teamDisplay, value);
        }

        private string _nationDisplay = "Todos os Países";
        public string NationDisplay
        {
            get => _nationDisplay;
            set => SetProperty(ref _nationDisplay, value);
        }

        // ── Dados exibidos ─────────────────────────────────────────────────────────

        /// <summary>
        /// Coleção de cartas exibidas na grade de resultados.
        /// ObservableCollection notifica a UI automaticamente ao adicionar/remover itens.
        /// </summary>
        public ObservableCollection<PlayerCard> PlayerCards { get; } =
            new ObservableCollection<PlayerCard>();

        private int _currentPage = 1;

        // ── Listas estáticas para ComboBoxes ───────────────────────────────────────

        /// <summary>Versões/tipos de carta disponíveis para filtro.</summary>
        public static readonly (string Label, string Value)[] CardVersions =
        {
            ("Todos os Tipos",     ""),
            ("Ouro Raro",          "gold_rare"),
            ("Ouro",               "gold_nonrare"),
            ("Prata Raro",         "silver_rare"),
            ("Prata",              "silver_nonrare"),
            ("Bronze Raro",        "bronze_rare"),
            ("Bronze",             "bronze_nonrare"),
            ("TOTW",               "totw"),
            ("Icon",               "icon"),
            ("Icon Base",          "base_icon"),
            ("Hero",               "hero"),
            ("TOTY",               "toty"),
            ("POTM",               "potm"),
            ("TOTS",               "tots"),
            ("FUTTIES",            "futties"),
        };

        /// <summary>Posições disponíveis para filtro.</summary>
        public static readonly string[] Positions =
        {
            "Todas", "GK", "RB", "CB", "LB", "RWB", "LWB",
            "CDM", "CM", "CAM", "RM", "LM", "RW", "LW", "ST", "CF"
        };

        /// <summary>Playstyles disponíveis para filtro.</summary>
        public static readonly string[] Playstyles =
        {
            "Todos",
            "Finesse Shot", "Power Header", "Trivela", "Tiki Taka",
            "Press Proven", "Rapid", "Aerial", "Long Ball Pass",
            "Chip Shot", "Acrobatic", "Incisive Pass", "Power Shot",
            "Technical", "Dead Ball", "Bruiser"
        };

        // ── Comandos ───────────────────────────────────────────────────────────────

        public RelayCommand SearchCommand { get; }
        public RelayCommand LoadMoreCommand { get; }
        public RelayCommand ClearFiltersCommand { get; }
        public RelayCommand<PlayerCard> OpenDetailCommand { get; }

        // ── Evento para abrir a tela de detalhes ──────────────────────────────────
        /// <summary>
        /// Disparado quando o usuário clica em uma carta.
        /// A View assina este evento para abrir a janela de detalhes.
        /// </summary>
        public event Action<PlayerCard>? CardDetailRequested;

        // ── Construtor ─────────────────────────────────────────────────────────────

        public MainViewModel()
        {
            _futbinService = new FutbinService();

            // Inicializa os comandos vinculando métodos assíncronos
            SearchCommand       = new RelayCommand(async () => await ExecuteSearchAsync(resetPage: true), () => !IsLoading);
            LoadMoreCommand      = new RelayCommand(async () => await ExecuteSearchAsync(resetPage: false), () => !IsLoading && CanLoadMore);
            ClearFiltersCommand = new RelayCommand(ExecuteClearFilters, () => !IsLoading);
            OpenDetailCommand   = new RelayCommand<PlayerCard>(card => { if (card != null) CardDetailRequested?.Invoke(card); });
        }

        // ── Lógica de busca ────────────────────────────────────────────────────────

        /// <summary>
        /// Executa a busca com os filtros atuais.
        /// Cancela qualquer busca anterior em andamento antes de iniciar uma nova.
        /// </summary>
        /// <param name="resetPage">True = nova busca (limpa resultados); False = "carregar mais".</param>
        private async Task ExecuteSearchAsync(bool resetPage)
        {
            // Cancela busca anterior se ainda estiver em andamento
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            IsLoading    = true;
            ErrorMessage = string.Empty;

            if (resetPage)
            {
                _currentPage = 1;
                PlayerCards.Clear();
                HasResults = false;
            }

            try
            {
                StatusMessage = "Buscando cartas no FUTBIN...";

                var filters = BuildFilters();
                var results = await _futbinService.SearchWithFiltersAsync(filters, token);

                // Se foi cancelado entre o início e o retorno, não atualiza a UI
                if (token.IsCancellationRequested) return;

                foreach (var card in results)
                    PlayerCards.Add(card);

                HasResults  = PlayerCards.Count > 0;
                CanLoadMore = results.Count >= 28; // FUTBIN retorna ~30 por página

                StatusMessage = HasResults
                    ? $"{PlayerCards.Count} carta(s) encontrada(s). Página {_currentPage}."
                    : "Nenhuma carta encontrada com os filtros informados.";

                _currentPage++;
            }
            catch (FutbinException ex)
            {
                if (!token.IsCancellationRequested)
                {
                    ErrorMessage  = ex.Message;
                    StatusMessage = "Erro ao buscar cartas.";
                }
            }
            catch (OperationCanceledException)
            {
                // Busca foi cancelada — normal, ignora
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    ErrorMessage  = $"Erro inesperado: {ex.Message}";
                    StatusMessage = "Erro ao buscar cartas.";
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Monta o objeto SearchFilters com os valores atuais da UI.
        /// </summary>
        private SearchFilters BuildFilters()
        {
            return new SearchFilters
            {
                SearchTerm   = SearchTerm.Trim(),
                CardVersion  = SelectedVersion,
                MinRating    = MinRating > 60 ? MinRating : null,
                MaxRating    = MaxRating < 99 ? MaxRating : null,
                Position     = SelectedPosition == "Todas" ? string.Empty : SelectedPosition,
                Playstyle    = SelectedPlaystyle == "Todos" ? string.Empty : SelectedPlaystyle,
                LeagueId     = SelectedLeagueId,
                LeagueName   = SelectedLeagueName,
                TeamId       = SelectedTeamId,
                TeamName     = SelectedTeamName,
                NationId     = SelectedNationId,
                NationName   = SelectedNationName,
                Page         = _currentPage,
            };
        }

        /// <summary>
        /// Limpa todos os filtros e reseta a busca.
        /// </summary>
        private void ExecuteClearFilters()
        {
            SearchTerm        = string.Empty;
            SelectedVersion   = string.Empty;
            MinRating         = 60;
            MaxRating         = 99;
            SelectedPosition  = string.Empty;
            SelectedPlaystyle = string.Empty;
            SelectedLeagueId  = null;
            SelectedLeagueName = string.Empty;
            SelectedTeamId    = null;
            SelectedTeamName  = string.Empty;
            SelectedNationId  = null;
            SelectedNationName = string.Empty;
            LeagueDisplay     = "Todas as Ligas";
            TeamDisplay       = "Todos os Times";
            NationDisplay     = "Todos os Países";

            PlayerCards.Clear();
            HasResults    = false;
            CanLoadMore   = false;
            ErrorMessage  = string.Empty;
            StatusMessage = "Filtros limpos. Pronto para uma nova busca.";
        }

        // ── Métodos auxiliares para filtros de entidade ────────────────────────────

        public void SetLeague(int? id, string name)
        {
            SelectedLeagueId   = id;
            SelectedLeagueName = name;
            LeagueDisplay      = id.HasValue ? name : "Todas as Ligas";
            OnPropertyChanged(nameof(SelectedLeagueId));
        }

        public void SetTeam(int? id, string name)
        {
            SelectedTeamId   = id;
            SelectedTeamName = name;
            TeamDisplay      = id.HasValue ? name : "Todos os Times";
            OnPropertyChanged(nameof(SelectedTeamId));
        }

        public void SetNation(int? id, string name)
        {
            SelectedNationId   = id;
            SelectedNationName = name;
            NationDisplay      = id.HasValue ? name : "Todos os Países";
            OnPropertyChanged(nameof(SelectedNationId));
        }
    }
}
