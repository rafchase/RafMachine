using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace FutbinSearch.Services
{
    /// <summary>
    /// Serviço de cache de imagens em memória para evitar downloads repetidos.
    /// Carrega imagens de URLs de forma assíncrona e as mantém em um dicionário
    /// thread-safe durante a sessão da aplicação.
    ///
    /// Uso:
    ///   var bitmap = await ImageCacheService.Instance.LoadImageAsync(url);
    /// </summary>
    public class ImageCacheService : IDisposable
    {
        // ── Singleton ──────────────────────────────────────────────────────────────
        private static readonly Lazy<ImageCacheService> _instance =
            new(() => new ImageCacheService());

        /// <summary>Instância global do serviço de cache de imagens.</summary>
        public static ImageCacheService Instance => _instance.Value;

        // ── Estado interno ─────────────────────────────────────────────────────────
        private readonly HttpClient _httpClient;

        /// <summary>Cache em memória: URL → BitmapImage (já carregado e pronto para uso no WPF).</summary>
        private readonly ConcurrentDictionary<string, BitmapImage?> _cache =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Semáforo por URL para evitar downloads duplicados simultâneos.</summary>
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks =
            new(StringComparer.OrdinalIgnoreCase);

        // ── Construtor privado (Singleton) ─────────────────────────────────────────
        private ImageCacheService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(20)
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        // ── Métodos públicos ───────────────────────────────────────────────────────

        /// <summary>
        /// Carrega uma imagem de forma assíncrona, retornando do cache se já disponível.
        /// Retorna null em caso de falha de download ou URL inválida.
        /// </summary>
        /// <param name="url">URL completa da imagem a ser carregada.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>BitmapImage pronto para uso em controles WPF, ou null.</returns>
        public async Task<BitmapImage?> LoadImageAsync(
            string url,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            // Retorna imediatamente do cache se já foi baixado
            if (_cache.TryGetValue(url, out var cached))
                return cached;

            // Garante que apenas um download por URL ocorra simultaneamente
            var semaphore = _locks.GetOrAdd(url, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync(cancellationToken);

            try
            {
                // Verifica novamente após adquirir o lock (double-check)
                if (_cache.TryGetValue(url, out cached))
                    return cached;

                var image = await DownloadImageAsync(url, cancellationToken);
                _cache[url] = image;
                return image;
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Verifica se uma imagem já está disponível no cache sem disparar download.
        /// </summary>
        public bool IsCached(string url) => _cache.ContainsKey(url);

        /// <summary>
        /// Remove todas as imagens do cache, liberando memória.
        /// </summary>
        public void ClearCache() => _cache.Clear();

        /// <summary>
        /// Quantidade de imagens atualmente em cache.
        /// </summary>
        public int CachedCount => _cache.Count;

        // ── Internos ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Realiza o download real da imagem e converte para BitmapImage.
        /// BitmapImage deve ser criado na thread de UI (via Dispatcher) ou
        /// com Freeze() para ser usado em threads secundárias.
        /// </summary>
        private async Task<BitmapImage?> DownloadImageAsync(
            string url,
            CancellationToken cancellationToken)
        {
            try
            {
                var bytes = await _httpClient.GetByteArrayAsync(url, cancellationToken);
                if (bytes == null || bytes.Length == 0) return null;

                // Cria o BitmapImage em memória e congela para uso cross-thread
                using var stream = new MemoryStream(bytes);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption  = BitmapCacheOption.OnLoad; // lê todo o stream aqui
                bitmap.EndInit();
                bitmap.Freeze(); // torna o objeto imutável e seguro para threads

                return bitmap;
            }
            catch (Exception)
            {
                // Qualquer falha de download retorna null (imagem padrão será usada na UI)
                return null;
            }
        }

        // ── IDisposable ────────────────────────────────────────────────────────────

        public void Dispose()
        {
            _httpClient?.Dispose();
            foreach (var sem in _locks.Values)
                sem?.Dispose();
        }
    }
}
