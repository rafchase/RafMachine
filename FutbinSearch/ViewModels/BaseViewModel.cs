using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FutbinSearch.ViewModels
{
    /// <summary>
    /// Classe base para todos os ViewModels da aplicação.
    /// Implementa INotifyPropertyChanged para habilitar o data binding do WPF —
    /// sempre que uma propriedade muda, a interface gráfica é notificada
    /// e atualizada automaticamente.
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Evento disparado quando o valor de uma propriedade é alterado.
        /// O WPF assina este evento automaticamente via binding.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Notifica a interface gráfica que o valor de uma propriedade mudou.
        /// O parâmetro [CallerMemberName] preenche automaticamente o nome
        /// da propriedade chamante — não é necessário passá-lo manualmente.
        /// </summary>
        /// <param name="propertyName">Nome da propriedade (preenchido automaticamente).</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Atualiza o valor de um campo e notifica a interface caso o valor tenha mudado.
        /// Padrão recomendado para propriedades em ViewModels:
        ///
        ///   private string _nome = string.Empty;
        ///   public string Nome
        ///   {
        ///       get => _nome;
        ///       set => SetProperty(ref _nome, value);
        ///   }
        /// </summary>
        /// <typeparam name="T">Tipo da propriedade.</typeparam>
        /// <param name="field">Referência ao campo privado de armazenamento.</param>
        /// <param name="value">Novo valor a ser atribuído.</param>
        /// <param name="propertyName">Nome da propriedade (preenchido automaticamente).</param>
        /// <returns>True se o valor mudou; False se era igual ao anterior.</returns>
        protected bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
