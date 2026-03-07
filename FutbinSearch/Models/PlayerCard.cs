using System.Collections.Generic;

namespace FutbinSearch.Models
{
    /// <summary>
    /// Representa uma carta de jogador do Ultimate Team com todos os seus atributos,
    /// incluindo estatísticas, preços e informações visuais.
    /// </summary>
    public class PlayerCard
    {
        // ── Identificadores ────────────────────────────────────────────────────────
        public int Id { get; set; }

        /// <summary>ID de recurso usado para compor URLs de imagens de cartas.</summary>
        public long ResourceId { get; set; }

        // ── Informações básicas ────────────────────────────────────────────────────
        /// <summary>Nome completo do jogador (ex: "Kylian Mbappé Lottin").</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Nome comum exibido na carta (ex: "Mbappé").</summary>
        public string CommonName { get; set; } = string.Empty;

        /// <summary>Overall / Rating geral da carta (0-99).</summary>
        public int Rating { get; set; }

        /// <summary>Posição na carta (ST, CAM, CB, GK, etc.).</summary>
        public string Position { get; set; } = string.Empty;

        /// <summary>Versão/tipo da carta (gold_rare, totw, icon, hero, toty, etc.).</summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>Label de exibição do tipo de carta (ex: "TOTW", "Icon", "Hero").</summary>
        public string CardType { get; set; } = string.Empty;

        // ── Clube, nação e liga ────────────────────────────────────────────────────
        public int ClubId { get; set; }
        public string Club { get; set; } = string.Empty;
        public string ClubImageUrl { get; set; } = string.Empty;

        public int NationId { get; set; }
        public string Nation { get; set; } = string.Empty;
        public string NationImageUrl { get; set; } = string.Empty;

        public int LeagueId { get; set; }
        public string League { get; set; } = string.Empty;

        // ── URLs das imagens ──────────────────────────────────────────────────────
        /// <summary>URL da imagem completa da carta (com moldura/design).</summary>
        public string CardImageUrl { get; set; } = string.Empty;

        /// <summary>URL da foto do jogador.</summary>
        public string PlayerImageUrl { get; set; } = string.Empty;

        // ── Estatísticas principais ───────────────────────────────────────────────
        public PlayerStats Stats { get; set; } = new PlayerStats();

        // ── Preços por plataforma ─────────────────────────────────────────────────
        public long PricePS { get; set; }
        public long PriceXbox { get; set; }
        public long PricePC { get; set; }

        // ── Informações físicas e técnicas ────────────────────────────────────────
        public int Age { get; set; }
        public string Height { get; set; } = string.Empty;
        public string Weight { get; set; } = string.Empty;
        public string PreferredFoot { get; set; } = string.Empty;

        /// <summary>Pé fraco (1-5 estrelas).</summary>
        public int WeakFoot { get; set; }

        /// <summary>Estrelas de habilidade (1-5 estrelas).</summary>
        public int SkillMoves { get; set; }

        public string WorkRate { get; set; } = string.Empty;
        public string AttackWorkRate { get; set; } = string.Empty;
        public string DefensiveWorkRate { get; set; } = string.Empty;

        // ── Playstyles ────────────────────────────────────────────────────────────
        /// <summary>Lista de playstyles normais associados à carta.</summary>
        public List<string> Playstyles { get; set; } = new List<string>();

        /// <summary>Lista de playstyles "+" (elite) associados à carta.</summary>
        public List<string> PlaystylePlus { get; set; } = new List<string>();

        // ── Histórico de preços ───────────────────────────────────────────────────
        public List<PriceHistory> PriceHistory { get; set; } = new List<PriceHistory>();

        /// <summary>URL da página do jogador no FUTBIN.</summary>
        public string FutbinUrl { get; set; } = string.Empty;

        // ── Propriedades calculadas ───────────────────────────────────────────────

        /// <summary>
        /// Nome de exibição: usa CommonName se disponível, caso contrário Name.
        /// </summary>
        public string DisplayName =>
            !string.IsNullOrEmpty(CommonName) ? CommonName : Name;

        /// <summary>
        /// Preço PS formatado para exibição (ex: "1.5M", "850K").
        /// </summary>
        public string PriceFormatted =>
            PricePS > 0 ? FormatPrice(PricePS) : "N/D";

        /// <summary>
        /// Cor de fundo da carta baseada no tipo/versão.
        /// Retorna um código de cor hexadecimal (#RRGGBB).
        /// </summary>
        public string CardColor => Version?.ToLower() switch
        {
            "gold_rare" or "gold rare"                         => "#C8A850",
            "gold_nonrare" or "gold" or "common"              => "#A08530",
            "silver_rare" or "silver rare"                    => "#B0B0B0",
            "silver_nonrare" or "silver"                      => "#909090",
            "bronze_rare" or "bronze rare"                    => "#CD7F32",
            "bronze_nonrare" or "bronze"                      => "#A05A20",
            "totw" or "totw_gold"                             => "#1E1E1E",
            "icon" or "icon_moments" or "base_icon"           => "#5C4033",
            "hero" or "fut_hero"                              => "#1A237E",
            "toty" or "toty_honorable"                        => "#0D47A1",
            "potm"                                            => "#4A148C",
            "futties"                                         => "#E65100",
            "tots" or "tots_moments"                          => "#880E4F",
            _ => "#C8A850"
        };

        /// <summary>
        /// Formata um valor numérico de preço para exibição amigável.
        /// Ex: 1.250.000 → "1.25M" | 850.000 → "850K"
        /// </summary>
        public static string FormatPrice(long price)
        {
            if (price >= 1_000_000)
                return $"{price / 1_000_000.0:0.##}M";
            if (price >= 1_000)
                return $"{price / 1_000.0:0.##}K";
            return price.ToString();
        }
    }
}
