using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FutbinSearch.Models;
using FutbinSearch.Services;

namespace FutbinSearch.Views.Controls
{
    /// <summary>
    /// Code-behind do controle visual de carta de jogador.
    /// Responsável por carregar a imagem do jogador de forma assíncrona
    /// e expor o evento de clique para a tela principal.
    /// </summary>
    public partial class PlayerCardControl : UserControl
    {
        // ── Dependency Properties ──────────────────────────────────────────────────

        /// <summary>
        /// Carta exibida neste controle. Ao ser definida, dispara o carregamento da imagem.
        /// </summary>
        public static readonly DependencyProperty CardProperty =
            DependencyProperty.Register(
                nameof(Card),
                typeof(PlayerCard),
                typeof(PlayerCardControl),
                new PropertyMetadata(null, OnCardChanged));

        public PlayerCard? Card
        {
            get => (PlayerCard?)GetValue(CardProperty);
            set => SetValue(CardProperty, value);
        }

        /// <summary>
        /// Comando executado quando o usuário clica na carta.
        /// Usado pela MainWindow para abrir a tela de detalhes.
        /// </summary>
        public static readonly DependencyProperty CardClickCommandProperty =
            DependencyProperty.Register(
                nameof(CardClickCommand),
                typeof(ICommand),
                typeof(PlayerCardControl),
                new PropertyMetadata(null));

        public ICommand? CardClickCommand
        {
            get => (ICommand?)GetValue(CardClickCommandProperty);
            set => SetValue(CardClickCommandProperty, value);
        }

        // ── Construtor ─────────────────────────────────────────────────────────────

        public PlayerCardControl()
        {
            InitializeComponent();

            // Ao clicar no cartão, executa o comando passado pela MainWindow
            MouseLeftButtonUp += (_, _) =>
            {
                if (Card != null && CardClickCommand?.CanExecute(Card) == true)
                    CardClickCommand.Execute(Card);
            };
        }

        // ── Callback ao mudar a carta exibida ──────────────────────────────────────

        private static void OnCardChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlayerCardControl control && e.NewValue is PlayerCard card)
            {
                // Define o DataContext para o binding do XAML
                control.DataContext = card;
                // Inicia o carregamento da imagem em segundo plano
                _ = control.LoadPlayerImageAsync(card);
            }
        }

        // ── Carregamento lazy de imagem ────────────────────────────────────────────

        /// <summary>
        /// Carrega a imagem do jogador de forma assíncrona usando o cache de imagens.
        /// Evita que o carregamento trave a interface gráfica.
        /// </summary>
        private async Task LoadPlayerImageAsync(PlayerCard card)
        {
            // Tenta carregar primeiro a imagem da carta (design completo)
            var image = await ImageCacheService.Instance.LoadImageAsync(card.CardImageUrl);

            // Fallback: foto do jogador (sem design da carta)
            if (image == null && !string.IsNullOrEmpty(card.PlayerImageUrl))
                image = await ImageCacheService.Instance.LoadImageAsync(card.PlayerImageUrl);

            // Atualiza o controle Image na thread de UI
            if (image != null)
                Dispatcher.Invoke(() => PlayerImage.Source = image);
        }
    }
}
