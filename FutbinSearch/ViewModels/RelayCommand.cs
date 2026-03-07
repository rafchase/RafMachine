using System;
using System.Windows.Input;

namespace FutbinSearch.ViewModels
{
    /// <summary>
    /// Implementação reutilizável de ICommand para uso em ViewModels (MVVM).
    /// Permite vincular ações e condições de execução a botões e controles WPF
    /// via binding, sem código no code-behind das Views.
    ///
    /// Uso básico:
    ///   BuscarCommand = new RelayCommand(async () => await BuscarAsync(), () => !IsLoading);
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        /// <summary>
        /// Cria um novo RelayCommand.
        /// </summary>
        /// <param name="execute">Ação a ser executada quando o comando é acionado.</param>
        /// <param name="canExecute">
        /// Função que determina se o comando pode ser executado.
        /// Nulo = sempre pode executar.
        /// </param>
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute    = execute    ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Evento disparado quando o estado de CanExecute muda.
        /// O WPF atualiza automaticamente o estado (enabled/disabled) dos controles vinculados.
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add    => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        /// <summary>
        /// Verifica se o comando pode ser executado no momento atual.
        /// </summary>
        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        /// <summary>
        /// Executa a ação associada ao comando.
        /// </summary>
        public void Execute(object? parameter) => _execute();

        /// <summary>
        /// Força a reavaliação de CanExecute para todos os comandos na UI.
        /// Chame este método após alterar condições que afetam CanExecute.
        /// </summary>
        public void RaiseCanExecuteChanged() =>
            CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>
    /// RelayCommand genérico que recebe um parâmetro tipado.
    /// Útil quando o botão precisa passar um argumento para o comando.
    ///
    /// Uso:
    ///   VerDetalhesCommand = new RelayCommand&lt;PlayerCard&gt;(card => AbrirDetalhes(card));
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute    = execute    ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add    => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) =>
            _canExecute?.Invoke((T?)parameter) ?? true;

        public void Execute(object? parameter) => _execute((T?)parameter);

        public void RaiseCanExecuteChanged() =>
            CommandManager.InvalidateRequerySuggested();
    }
}
