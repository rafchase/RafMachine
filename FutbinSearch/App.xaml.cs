using System;
using System.Windows;

namespace FutbinSearch
{
    /// <summary>
    /// Ponto de entrada da aplicação WPF FUTBIN Card Search.
    /// Gerencia o ciclo de vida da aplicação e tratamento global de exceções.
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ── Tratamento global de exceções não capturadas ──────────────────────

            // Exceções em threads de UI (dispatcher)
            DispatcherUnhandledException += (_, args) =>
            {
                ShowFatalError(args.Exception);
                args.Handled = true; // evita crash — mantém a aplicação ativa
            };

            // Exceções em tasks assíncronas sem await (Task não observadas)
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                    ShowFatalError(ex);
            };

            // Exceções em Tasks esquecidas (sem .Wait() ou await)
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                // Marca como observada para não derrubar o processo
                args.SetObserved();
            };
        }

        /// <summary>
        /// Exibe uma caixa de diálogo de erro fatal com informações úteis para o usuário.
        /// </summary>
        private static void ShowFatalError(Exception ex)
        {
            MessageBox.Show(
                $"Ocorreu um erro inesperado na aplicação:\n\n" +
                $"{ex.Message}\n\n" +
                $"Se o problema persistir, verifique sua conexão com a internet\n" +
                $"ou tente reiniciar a aplicação.",
                "Erro — FUTBIN Card Search",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
