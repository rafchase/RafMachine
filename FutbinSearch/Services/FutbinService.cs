using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FutbinSearch.Models;

namespace FutbinSearch.Services
{
    /// <summary>
    /// Serviço principal para consumir a API do FUTBIN.
    /// Realiza chamadas HTTP assíncronas e desserializa os dados retornados
    /// em objetos do modelo da aplicação.
    ///
    /// Endpoints utilizados:
    ///   - /search?year=25&amp;term={termo}        → Busca de jogadores por nome
    ///   - /25/players?page={p}&amp;...filtros...  → Listagem com filtros avançados
    ///   - /25/playerGraph?type=daily&amp;player={id} → Histórico de preços
    /// </summary>
    public class FutbinService : IDisposable
    {
        // ── Constantes ─────────────────────────────────────────────────────────────
        private const string BaseUrl = "https://www.futbin.com";
        private const string CdnUrl  = "https://cdn.futbin.com";

        /// <summary>Ano do jogo (FC 25 = 25). Altere para adaptar a edições futuras.</summary>
        private const int GameYear = 25;

        private readonly HttpClient _httpClient;

        // ── Construtor ─────────────────────────────────────────────────────────────
        public FutbinService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            // Headers necessários para o FUTBIN aceitar as requisições
            // (simula um navegador real, evitando bloqueio por parte do servidor)
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                "AppleWebKit/537.36 (KHTML, like Gecko) " +
                "Chrome/120.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept",
                "application/json, text/javascript, */*; q=0.01");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language",
                "pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7");
            _httpClient.DefaultRequestHeaders.Add("X-Requested-With",
                "XMLHttpRequest");
            _httpClient.DefaultRequestHeaders.Add("Referer", BaseUrl + "/");
        }

        // ── Métodos públicos principais ────────────────────────────────────────────

        /// <summary>
        /// Busca jogadores pelo nome usando o endpoint de pesquisa rápida do FUTBIN.
        /// Retorna uma lista parcial (sem sub-estatísticas completas).
        /// </summary>
        /// <param name="searchTerm">Nome ou apelido do jogador.</param>
        /// <param name="cancellationToken">Token para cancelamento da operação.</param>
        /// <returns>Lista de cartas correspondentes ao termo de busca.</returns>
        public async Task<List<PlayerCard>> SearchByNameAsync(
            string searchTerm,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<PlayerCard>();

            // Monta a URL do endpoint de busca com o ano do jogo
            var encodedTerm = Uri.EscapeDataString(searchTerm.Trim());
            var url = $"{BaseUrl}/search?year={GameYear}&term={encodedTerm}";

            var json = await GetJsonAsync(url, cancellationToken);
            if (json == null) return new List<PlayerCard>();

            return ParseSearchResults(json);
        }

        /// <summary>
        /// Lista jogadores aplicando filtros avançados (versão, rating, liga, time, país, playstyle).
        /// Usa paginação — cada página retorna até ~30 resultados.
        /// </summary>
        /// <param name="filters">Objeto com todos os filtros a serem aplicados.</param>
        /// <param name="cancellationToken">Token para cancelamento da operação.</param>
        /// <returns>Lista de cartas encontradas com os filtros informados.</returns>
        public async Task<List<PlayerCard>> SearchWithFiltersAsync(
            SearchFilters filters,
            CancellationToken cancellationToken = default)
        {
            // Se há um nome de busca e sem outros filtros, usa endpoint de busca rápida
            if (!string.IsNullOrWhiteSpace(filters.SearchTerm) && !filters.HasActiveFilters)
                return await SearchByNameAsync(filters.SearchTerm, cancellationToken);

            // Monta a query string com todos os filtros disponíveis
            var queryParams = BuildQueryParams(filters);
            var url = $"{BaseUrl}/{GameYear}/players?{queryParams}";

            var json = await GetJsonAsync(url, cancellationToken);
            if (json == null) return new List<PlayerCard>();

            // O endpoint de listagem pode retornar um objeto com campo "data"
            // ou diretamente um array — tratamos ambos os casos
            if (json is JArray arr)
                return ParsePlayerArray(arr);

            if (json is JObject obj && obj.ContainsKey("data"))
                return ParsePlayerArray(obj["data"] as JArray ?? new JArray());

            return new List<PlayerCard>();
        }

        /// <summary>
        /// Busca o histórico de preços diários de uma carta específica no FUTBIN.
        /// </summary>
        /// <param name="playerId">ID numérico do jogador no FUTBIN.</param>
        /// <param name="cancellationToken">Token para cancelamento da operação.</param>
        /// <returns>Lista com pontos de dados de preço ordenados por data crescente.</returns>
        public async Task<List<PriceHistory>> GetPriceHistoryAsync(
            int playerId,
            CancellationToken cancellationToken = default)
        {
            // Timestamp para evitar cache do servidor (parâmetro _=epoch)
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var url = $"{BaseUrl}/{GameYear}/playerGraph?type=daily&player={playerId}&_={timestamp}";

            var json = await GetJsonAsync(url, cancellationToken);
            if (json == null) return new List<PriceHistory>();

            return ParsePriceHistory(json);
        }

        /// <summary>
        /// Busca detalhes completos de uma carta, incluindo sub-estatísticas,
        /// playstyles e informações físicas do jogador.
        /// </summary>
        /// <param name="playerId">ID numérico do jogador.</param>
        /// <param name="cancellationToken">Token para cancelamento da operação.</param>
        /// <returns>PlayerCard enriquecido com dados completos, ou null se não encontrado.</returns>
        public async Task<PlayerCard?> GetPlayerDetailsAsync(
            int playerId,
            CancellationToken cancellationToken = default)
        {
            // Tenta o endpoint de detalhes do jogador (FUTBIN internal API)
            var url = $"{BaseUrl}/{GameYear}/playerHigh?player={playerId}";

            var json = await GetJsonAsync(url, cancellationToken);
            if (json == null) return null;

            // O endpoint pode retornar um array com um item ou um objeto direto
            JObject? playerObj = json is JArray arr && arr.Count > 0
                ? arr[0] as JObject
                : json as JObject;

            return playerObj != null ? ParseDetailedPlayer(playerObj) : null;
        }

        // ── Montagem de URLs ───────────────────────────────────────────────────────

        /// <summary>
        /// Constrói a query string para o endpoint de listagem de jogadores.
        /// </summary>
        private static string BuildQueryParams(SearchFilters filters)
        {
            var builder = new System.Text.StringBuilder();

            // Paginação
            builder.Append($"page={filters.Page}");

            // Versão/tipo da carta
            if (!string.IsNullOrEmpty(filters.CardVersion))
                builder.Append($"&version={Uri.EscapeDataString(filters.CardVersion)}");

            // Rating mínimo e máximo
            if (filters.MinRating.HasValue)
                builder.Append($"&minrating={filters.MinRating.Value}");
            if (filters.MaxRating.HasValue)
                builder.Append($"&maxrating={filters.MaxRating.Value}");

            // Liga, time e nação
            if (filters.LeagueId.HasValue)
                builder.Append($"&leagueId={filters.LeagueId.Value}");
            if (filters.TeamId.HasValue)
                builder.Append($"&teamId={filters.TeamId.Value}");
            if (filters.NationId.HasValue)
                builder.Append($"&nationId={filters.NationId.Value}");

            // Playstyle
            if (!string.IsNullOrEmpty(filters.Playstyle))
                builder.Append($"&playstyle={Uri.EscapeDataString(filters.Playstyle)}");

            // Posição
            if (!string.IsNullOrEmpty(filters.Position))
                builder.Append($"&position={Uri.EscapeDataString(filters.Position)}");

            // Termo de busca por nome (quando combinado com outros filtros)
            if (!string.IsNullOrEmpty(filters.SearchTerm))
                builder.Append($"&search={Uri.EscapeDataString(filters.SearchTerm)}");

            return builder.ToString();
        }

        // ── Comunicação HTTP ───────────────────────────────────────────────────────

        /// <summary>
        /// Executa uma requisição GET e retorna o corpo como JToken (JArray ou JObject).
        /// Retorna null em caso de erro de rede ou resposta inválida.
        /// </summary>
        private async Task<JToken?> GetJsonAsync(
            string url,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(content))
                    return null;

                // Tenta parsear como JSON; retorna null se o conteúdo for HTML/inválido
                return JToken.Parse(content);
            }
            catch (HttpRequestException ex)
            {
                // Relança como exceção personalizada para ser tratada na UI
                throw new FutbinException(
                    $"Erro de conexão com o FUTBIN: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new FutbinException(
                    "A requisição expirou (timeout). Verifique sua conexão com a internet.", ex);
            }
            catch (JsonException)
            {
                // Servidor retornou HTML ou conteúdo não-JSON — comum em rate limiting
                return null;
            }
        }

        // ── Parsers de resposta da API ─────────────────────────────────────────────

        /// <summary>
        /// Interpreta o array JSON retornado pelo endpoint de busca por nome.
        /// </summary>
        private List<PlayerCard> ParseSearchResults(JToken token)
        {
            var results = new List<PlayerCard>();

            var arr = token as JArray;
            if (arr == null) return results;

            foreach (var item in arr)
            {
                if (item is not JObject obj) continue;
                results.Add(ParseSearchItem(obj));
            }

            return results;
        }

        /// <summary>
        /// Interpreta um item individual do resultado de busca.
        /// </summary>
        private PlayerCard ParseSearchItem(JObject obj)
        {
            var id = obj.Value<int>("id");
            var resourceId = obj.Value<long?>("resource_id") ?? id;
            var version = obj.Value<string>("version") ?? string.Empty;
            var clubId = obj.Value<int?>("club_id") ?? 0;
            var nationId = obj.Value<int?>("nation_id") ?? 0;
            var leagueId = obj.Value<int?>("league_id") ?? 0;

            // Preços: podem vir em um objeto aninhado ou direto
            long pricePS = 0, priceXbox = 0, pricePC = 0;
            var priceObj = obj["player_price"] as JObject;
            if (priceObj != null)
            {
                pricePS   = ParsePrice(priceObj["ps"]);
                priceXbox = ParsePrice(priceObj["xbox"]);
                pricePC   = ParsePrice(priceObj["pc"]);
            }
            else
            {
                pricePS   = ParsePrice(obj["price_ps"]);
                priceXbox = ParsePrice(obj["price_xbox"]);
                pricePC   = ParsePrice(obj["price_pc"]);
            }

            return new PlayerCard
            {
                Id           = id,
                ResourceId   = resourceId,
                Name         = obj.Value<string>("name") ?? string.Empty,
                CommonName   = obj.Value<string>("common_name") ?? string.Empty,
                Rating       = obj.Value<int?>("rating") ?? 0,
                Position     = obj.Value<string>("position") ?? string.Empty,
                Version      = version,
                CardType     = NormalizeCardType(version),
                ClubId       = clubId,
                Club         = obj.Value<string>("club") ?? string.Empty,
                NationId     = nationId,
                Nation       = obj.Value<string>("nation") ?? string.Empty,
                LeagueId     = leagueId,
                League       = obj.Value<string>("league") ?? string.Empty,
                PricePS      = pricePS,
                PriceXbox    = priceXbox,
                PricePC      = pricePC,

                // URLs de imagens montadas a partir dos IDs
                CardImageUrl   = BuildCardImageUrl(resourceId),
                PlayerImageUrl = BuildPlayerImageUrl(id),
                ClubImageUrl   = BuildClubImageUrl(clubId),
                NationImageUrl = BuildNationImageUrl(nationId),
                FutbinUrl      = BuildPlayerPageUrl(id, obj.Value<string>("common_name") ?? string.Empty),

                // Estatísticas principais (quando disponíveis no resultado de busca)
                Stats = ParseBasicStats(obj),
            };
        }

        /// <summary>
        /// Interpreta um array JSON com múltiplos jogadores (endpoint /players).
        /// </summary>
        private List<PlayerCard> ParsePlayerArray(JArray arr)
        {
            return arr
                .OfType<JObject>()
                .Select(ParseSearchItem)
                .ToList();
        }

        /// <summary>
        /// Interpreta o objeto JSON de detalhes completos de um jogador.
        /// </summary>
        private PlayerCard ParseDetailedPlayer(JObject obj)
        {
            var card = ParseSearchItem(obj);

            // Informações físicas e técnicas
            card.Age                  = obj.Value<int?>("age") ?? 0;
            card.Height               = obj.Value<string>("height") ?? string.Empty;
            card.Weight               = obj.Value<string>("weight") ?? string.Empty;
            card.PreferredFoot        = obj.Value<string>("foot") ?? string.Empty;
            card.WeakFoot             = obj.Value<int?>("weak_foot") ?? 0;
            card.SkillMoves           = obj.Value<int?>("skill_moves") ?? 0;
            card.AttackWorkRate        = obj.Value<string>("att_workrate") ?? string.Empty;
            card.DefensiveWorkRate     = obj.Value<string>("def_workrate") ?? string.Empty;
            card.WorkRate             = $"{card.AttackWorkRate}/{card.DefensiveWorkRate}";

            // Sub-estatísticas completas
            card.Stats = ParseDetailedStats(obj);

            // Playstyles (podem vir como array ou string separada por vírgulas)
            card.Playstyles     = ParseStringList(obj["playstyle"]);
            card.PlaystylePlus  = ParseStringList(obj["playstyle_plus"]);

            return card;
        }

        /// <summary>
        /// Interpreta as estatísticas básicas disponíveis no resultado de busca.
        /// </summary>
        private static PlayerStats ParseBasicStats(JObject obj)
        {
            return new PlayerStats
            {
                Pace       = obj.Value<int?>("pace")    ?? obj.Value<int?>("pac") ?? 0,
                Shooting   = obj.Value<int?>("shooting") ?? obj.Value<int?>("sho") ?? 0,
                Passing    = obj.Value<int?>("passing")  ?? obj.Value<int?>("pas") ?? 0,
                Dribbling  = obj.Value<int?>("dribbling") ?? obj.Value<int?>("dri") ?? 0,
                Defending  = obj.Value<int?>("defending") ?? obj.Value<int?>("def") ?? 0,
                Physical   = obj.Value<int?>("physicality") ?? obj.Value<int?>("phy") ?? 0,
            };
        }

        /// <summary>
        /// Interpreta todas as sub-estatísticas do objeto de detalhes completos.
        /// </summary>
        private static PlayerStats ParseDetailedStats(JObject obj)
        {
            var stats = ParseBasicStats(obj);

            // Sub-stats de Velocidade
            stats.Acceleration = obj.Value<int?>("acceleration") ?? 0;
            stats.SprintSpeed  = obj.Value<int?>("sprint_speed") ?? obj.Value<int?>("sprintspeed") ?? 0;

            // Sub-stats de Finalização
            stats.Positioning  = obj.Value<int?>("positioning") ?? 0;
            stats.Finishing    = obj.Value<int?>("finishing") ?? 0;
            stats.ShotPower    = obj.Value<int?>("shot_power") ?? obj.Value<int?>("shotpower") ?? 0;
            stats.LongShots    = obj.Value<int?>("long_shots") ?? obj.Value<int?>("longshots") ?? 0;
            stats.Volleys      = obj.Value<int?>("volleys") ?? 0;
            stats.Penalties    = obj.Value<int?>("penalties") ?? 0;

            // Sub-stats de Passe
            stats.Vision       = obj.Value<int?>("vision") ?? 0;
            stats.Crossing     = obj.Value<int?>("crossing") ?? 0;
            stats.FKAccuracy   = obj.Value<int?>("fk_accuracy") ?? obj.Value<int?>("freekickaccuracy") ?? 0;
            stats.ShortPassing = obj.Value<int?>("short_passing") ?? obj.Value<int?>("shortpassing") ?? 0;
            stats.LongPassing  = obj.Value<int?>("long_passing") ?? obj.Value<int?>("longpassing") ?? 0;
            stats.Curve        = obj.Value<int?>("curve") ?? 0;

            // Sub-stats de Drible
            stats.Agility         = obj.Value<int?>("agility") ?? 0;
            stats.Balance         = obj.Value<int?>("balance") ?? 0;
            stats.Reactions       = obj.Value<int?>("reactions") ?? 0;
            stats.BallControl     = obj.Value<int?>("ball_control") ?? obj.Value<int?>("ballcontrol") ?? 0;
            stats.DribblingSkill  = obj.Value<int?>("dribbling_sub") ?? obj.Value<int?>("dribbling") ?? 0;
            stats.Composure       = obj.Value<int?>("composure") ?? 0;

            // Sub-stats de Defesa
            stats.Interceptions       = obj.Value<int?>("interceptions") ?? 0;
            stats.HeadingAccuracy     = obj.Value<int?>("heading_accuracy") ?? obj.Value<int?>("headingaccuracy") ?? 0;
            stats.DefensiveAwareness  = obj.Value<int?>("def_awareness") ?? obj.Value<int?>("marking") ?? 0;
            stats.StandingTackle      = obj.Value<int?>("standing_tackle") ?? obj.Value<int?>("standingtackle") ?? 0;
            stats.SlidingTackle       = obj.Value<int?>("sliding_tackle") ?? obj.Value<int?>("slidingtackle") ?? 0;

            // Sub-stats de Físico
            stats.Jumping    = obj.Value<int?>("jumping") ?? 0;
            stats.Stamina    = obj.Value<int?>("stamina") ?? 0;
            stats.Strength   = obj.Value<int?>("strength") ?? 0;
            stats.Aggression = obj.Value<int?>("aggression") ?? 0;

            // Atributos de Goleiro
            stats.GKDiving      = obj.Value<int?>("gk_diving") ?? obj.Value<int?>("gkdiving") ?? 0;
            stats.GKHandling    = obj.Value<int?>("gk_handling") ?? obj.Value<int?>("gkhandling") ?? 0;
            stats.GKKicking     = obj.Value<int?>("gk_kicking") ?? obj.Value<int?>("gkkicking") ?? 0;
            stats.GKReflexes    = obj.Value<int?>("gk_reflexes") ?? obj.Value<int?>("gkreflexes") ?? 0;
            stats.GKPositioning = obj.Value<int?>("gk_positioning") ?? obj.Value<int?>("gkpositioning") ?? 0;

            return stats;
        }

        /// <summary>
        /// Interpreta o histórico de preços retornado pelo endpoint playerGraph.
        /// O FUTBIN retorna um objeto com chaves "ps", "xbox" e "pc",
        /// cada uma contendo um array de [timestamp, preco].
        /// </summary>
        private static List<PriceHistory> ParsePriceHistory(JToken token)
        {
            var history = new Dictionary<long, PriceHistory>();

            void ParsePlatform(JToken? arr, Action<PriceHistory, long> setPlatformPrice)
            {
                if (arr is not JArray jArr) return;
                foreach (var item in jArr)
                {
                    if (item is not JArray point || point.Count < 2) continue;

                    // Ponto = [timestamp_ms, preco]
                    var timestampMs = point[0].Value<long>();
                    var price       = point[1].Value<long>();
                    var date        = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs).DateTime;

                    if (!history.TryGetValue(timestampMs, out var entry))
                    {
                        entry = new PriceHistory { Date = date };
                        history[timestampMs] = entry;
                    }

                    setPlatformPrice(entry, price);
                }
            }

            if (token is JObject obj)
            {
                // Formato: { "ps": [...], "xbox": [...], "pc": [...] }
                ParsePlatform(obj["ps"],   (e, p) => e.PricePS   = p);
                ParsePlatform(obj["xbox"], (e, p) => e.PriceXbox = p);
                ParsePlatform(obj["pc"],   (e, p) => e.PricePC   = p);
            }
            else if (token is JArray arr)
            {
                // Formato alternativo: array simples de [timestamp, preco]
                foreach (var item in arr)
                {
                    if (item is not JArray point || point.Count < 2) continue;
                    var timestampMs = point[0].Value<long>();
                    var price       = point[1].Value<long>();
                    var date        = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs).DateTime;

                    if (!history.ContainsKey(timestampMs))
                        history[timestampMs] = new PriceHistory { Date = date };

                    history[timestampMs].PricePS = price;
                }
            }

            // Ordena por data crescente
            return history.Values.OrderBy(h => h.Date).ToList();
        }

        // ── Utilitários ────────────────────────────────────────────────────────────

        /// <summary>
        /// Converte um token JSON para long (suporta string e número).
        /// </summary>
        private static long ParsePrice(JToken? token)
        {
            if (token == null) return 0;
            var str = token.ToString().Replace(",", "").Trim();
            return long.TryParse(str, out var val) ? val : 0;
        }

        /// <summary>
        /// Interpreta um token JSON como lista de strings
        /// (aceita array de strings ou string separada por vírgulas).
        /// </summary>
        private static List<string> ParseStringList(JToken? token)
        {
            if (token == null) return new List<string>();

            if (token is JArray arr)
                return arr.Select(t => t.ToString()).Where(s => !string.IsNullOrEmpty(s)).ToList();

            var str = token.ToString();
            if (string.IsNullOrEmpty(str)) return new List<string>();

            return str.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
        }

        /// <summary>
        /// Converte a string de versão do FUTBIN em um label de exibição amigável.
        /// </summary>
        private static string NormalizeCardType(string version) =>
            version?.ToLower() switch
            {
                "gold_rare"       => "Ouro Raro",
                "gold_nonrare"    => "Ouro",
                "silver_rare"     => "Prata Raro",
                "silver_nonrare"  => "Prata",
                "bronze_rare"     => "Bronze Raro",
                "bronze_nonrare"  => "Bronze",
                "totw"            => "TOTW",
                "totw_gold"       => "TOTW",
                "icon"            => "Icon",
                "base_icon"       => "Icon Base",
                "icon_moments"    => "Icon Momentos",
                "hero"            => "Hero",
                "fut_hero"        => "FUT Hero",
                "toty"            => "TOTY",
                "toty_honorable"  => "TOTY Hon.",
                "potm"            => "POTM",
                "tots"            => "TOTS",
                "tots_moments"    => "TOTS Momentos",
                "futties"         => "FUTTIES",
                _                 => version?.ToUpper() ?? "Desconhecido"
            };

        // ── Construtores de URL de imagens CDN ────────────────────────────────────

        /// <summary>Monta URL da imagem completa da carta (com moldura/design visual).</summary>
        public static string BuildCardImageUrl(long resourceId) =>
            $"{CdnUrl}/content/fifa{GameYear}/img/carditems/big/{resourceId}_large.png";

        /// <summary>Monta URL da foto do jogador (rosto).</summary>
        public static string BuildPlayerImageUrl(int playerId) =>
            $"{CdnUrl}/content/fifa{GameYear}/img/players/{playerId}.png";

        /// <summary>Monta URL do escudo/logo do clube.</summary>
        public static string BuildClubImageUrl(int clubId) =>
            $"{CdnUrl}/content/fifa{GameYear}/img/clubs/dark/{clubId}.png";

        /// <summary>Monta URL da bandeira da nação.</summary>
        public static string BuildNationImageUrl(int nationId) =>
            $"{CdnUrl}/content/fifa{GameYear}/img/nation_flags/{nationId}.png";

        /// <summary>Monta URL da página do jogador no FUTBIN.</summary>
        public static string BuildPlayerPageUrl(int id, string name)
        {
            var slug = name.ToLower()
                .Replace(" ", "-")
                .Replace("'", "")
                .Replace(".", "");
            return $"{BaseUrl}/{GameYear}/player/{id}/{slug}";
        }

        // ── IDisposable ────────────────────────────────────────────────────────────

        public void Dispose() => _httpClient?.Dispose();
    }

    /// <summary>
    /// Exceção específica do serviço FUTBIN para identificar erros de comunicação
    /// com a API e exibir mensagens amigáveis ao usuário.
    /// </summary>
    public class FutbinException : Exception
    {
        public FutbinException(string message, Exception? innerException = null)
            : base(message, innerException) { }
    }
}
