using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FutbinSearch.ViewModels;

namespace FutbinSearch.Views
{
    /// <summary>
    /// Code-behind da janela principal.
    /// Mantido ao mínimo (MVVM): apenas inicialização de controles que não
    /// suportam binding diretamente e tratamento de eventos de UI simples.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            // Cria e vincula o ViewModel à janela
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // Assina o evento para abrir a janela de detalhes ao clicar numa carta
            _viewModel.CardDetailRequested += OpenPlayerDetail;

            // Popula os ComboBoxes de filtro com dados estáticos
            InitializeFilterCombos();
        }

        // ── Inicialização dos filtros ──────────────────────────────────────────────

        /// <summary>
        /// Popula os ComboBoxes de filtro com as opções disponíveis.
        /// Feito no code-behind pois os itens são definidos no ViewModel como arrays estáticos.
        /// </summary>
        private void InitializeFilterCombos()
        {
            // Tipo de carta
            foreach (var (label, _) in MainViewModel.CardVersions)
                VersionCombo.Items.Add(label);
            VersionCombo.SelectedIndex = 0;

            // Posições
            foreach (var pos in MainViewModel.Positions)
                PositionCombo.Items.Add(pos);
            PositionCombo.SelectedIndex = 0;

            // Playstyles
            foreach (var ps in MainViewModel.Playstyles)
                PlaystyleCombo.Items.Add(ps);
            PlaystyleCombo.SelectedIndex = 0;
        }

        // ── Eventos de controles de UI ─────────────────────────────────────────────

        /// <summary>
        /// Permite buscar pressionando Enter no campo de nome do jogador.
        /// </summary>
        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _viewModel.SearchCommand.CanExecute(null))
                _viewModel.SearchCommand.Execute(null);
        }

        /// <summary>
        /// Atualiza o ViewModel quando o tipo de carta muda no ComboBox.
        /// </summary>
        private void VersionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VersionCombo.SelectedIndex >= 0 &&
                VersionCombo.SelectedIndex < MainViewModel.CardVersions.Length)
            {
                _viewModel.SelectedVersion = MainViewModel.CardVersions[VersionCombo.SelectedIndex].Value;
            }
        }

        /// <summary>
        /// Atualiza o ViewModel quando a posição muda.
        /// </summary>
        private void PositionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PositionCombo.SelectedItem is string pos)
                _viewModel.SelectedPosition = pos == "Todas" ? string.Empty : pos;
        }

        /// <summary>
        /// Atualiza o ViewModel quando o playstyle muda.
        /// </summary>
        private void PlaystyleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlaystyleCombo.SelectedItem is string ps)
                _viewModel.SelectedPlaystyle = ps == "Todos" ? string.Empty : ps;
        }

        /// <summary>
        /// Atualiza o filtro de liga quando o usuário digita um ID numérico.
        /// </summary>
        private void LeagueIdBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = LeagueIdBox.Text.Trim();
            if (int.TryParse(text, out var id))
                _viewModel.SetLeague(id, $"Liga ID {id}");
            else if (string.IsNullOrEmpty(text))
                _viewModel.SetLeague(null, string.Empty);
        }

        /// <summary>
        /// Atualiza o filtro de time quando o usuário digita um ID numérico.
        /// </summary>
        private void TeamIdBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = TeamIdBox.Text.Trim();
            if (int.TryParse(text, out var id))
                _viewModel.SetTeam(id, $"Time ID {id}");
            else if (string.IsNullOrEmpty(text))
                _viewModel.SetTeam(null, string.Empty);
        }

        /// <summary>
        /// Atualiza o filtro de nação quando o usuário digita um ID numérico.
        /// </summary>
        private void NationIdBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = NationIdBox.Text.Trim();
            if (int.TryParse(text, out var id))
                _viewModel.SetNation(id, $"Nação ID {id}");
            else if (string.IsNullOrEmpty(text))
                _viewModel.SetNation(null, string.Empty);
        }

        /// <summary>
        /// Fecha o banner de erro ao clicar no botão "✕".
        /// </summary>
        private void DismissError_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ErrorMessage = string.Empty;
        }

        // ── Navegação para detalhes ────────────────────────────────────────────────

        /// <summary>
        /// Abre a janela de detalhes da carta selecionada.
        /// Este método é chamado pelo evento CardDetailRequested do ViewModel.
        /// </summary>
        private void OpenPlayerDetail(Models.PlayerCard card)
        {
            var detailWindow = new PlayerDetailWindow(card)
            {
                Owner = this // a janela de detalhes é filha da principal
            };
            detailWindow.ShowDialog(); // exibe como modal
        }
    }
}
