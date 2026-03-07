namespace FutbinSearch.Models
{
    /// <summary>
    /// Encapsula todos os filtros de busca disponíveis na interface principal.
    /// Enviados ao FutbinService para montar a query da requisição HTTP.
    /// </summary>
    public class SearchFilters
    {
        /// <summary>Termo de busca pelo nome do jogador.</summary>
        public string SearchTerm { get; set; } = string.Empty;

        /// <summary>
        /// Versão/tipo da carta para filtrar.
        /// Valores aceitos: gold_rare, silver_rare, bronze_rare, totw, icon, hero, toty, potm, tots, etc.
        /// Vazio = todas as versões.
        /// </summary>
        public string CardVersion { get; set; } = string.Empty;

        /// <summary>Rating mínimo do jogador (1-99). Nulo = sem limite inferior.</summary>
        public int? MinRating { get; set; }

        /// <summary>Rating máximo do jogador (1-99). Nulo = sem limite superior.</summary>
        public int? MaxRating { get; set; }

        /// <summary>ID da liga para filtrar. Nulo = todas as ligas.</summary>
        public int? LeagueId { get; set; }

        /// <summary>Nome da liga (para exibição na UI).</summary>
        public string LeagueName { get; set; } = string.Empty;

        /// <summary>ID do time/clube para filtrar. Nulo = todos os times.</summary>
        public int? TeamId { get; set; }

        /// <summary>Nome do time (para exibição na UI).</summary>
        public string TeamName { get; set; } = string.Empty;

        /// <summary>ID da nação/país para filtrar. Nulo = todos os países.</summary>
        public int? NationId { get; set; }

        /// <summary>Nome do país (para exibição na UI).</summary>
        public string NationName { get; set; } = string.Empty;

        /// <summary>
        /// Playstyle para filtrar (ex: "finesse_shot", "trivela", "power_header").
        /// Vazio = qualquer playstyle.
        /// </summary>
        public string Playstyle { get; set; } = string.Empty;

        /// <summary>
        /// Posição para filtrar (ex: "ST", "CAM", "CB", "GK").
        /// Vazio = qualquer posição.
        /// </summary>
        public string Position { get; set; } = string.Empty;

        /// <summary>Número da página de resultados (começa em 1).</summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Indica se existem filtros ativos além do nome do jogador.
        /// Útil para exibir indicador visual na interface.
        /// </summary>
        public bool HasActiveFilters =>
            !string.IsNullOrEmpty(CardVersion) ||
            MinRating.HasValue ||
            MaxRating.HasValue ||
            LeagueId.HasValue ||
            TeamId.HasValue ||
            NationId.HasValue ||
            !string.IsNullOrEmpty(Playstyle) ||
            !string.IsNullOrEmpty(Position);

        /// <summary>
        /// Reseta todos os filtros para o estado padrão (sem filtros).
        /// </summary>
        public void Reset()
        {
            SearchTerm = string.Empty;
            CardVersion = string.Empty;
            MinRating = null;
            MaxRating = null;
            LeagueId = null;
            LeagueName = string.Empty;
            TeamId = null;
            TeamName = string.Empty;
            NationId = null;
            NationName = string.Empty;
            Playstyle = string.Empty;
            Position = string.Empty;
            Page = 1;
        }
    }
}
