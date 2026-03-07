namespace FutbinSearch.Models
{
    /// <summary>
    /// Estatísticas completas de um jogador do Ultimate Team.
    /// Inclui os seis atributos principais e todas as sub-estatísticas.
    /// </summary>
    public class PlayerStats
    {
        // ── Atributos principais (médias ponderadas) ──────────────────────────────

        /// <summary>Velocidade (Pace) — média de Aceleração e Velocidade de Sprint.</summary>
        public int Pace { get; set; }

        /// <summary>Finalização (Shooting) — média de sub-atributos de chute.</summary>
        public int Shooting { get; set; }

        /// <summary>Passe (Passing) — média de sub-atributos de passe.</summary>
        public int Passing { get; set; }

        /// <summary>Drible (Dribbling) — média de sub-atributos de drible.</summary>
        public int Dribbling { get; set; }

        /// <summary>Defesa (Defending) — média de sub-atributos defensivos.</summary>
        public int Defending { get; set; }

        /// <summary>Físico (Physical) — média de sub-atributos físicos.</summary>
        public int Physical { get; set; }

        // ── Sub-estatísticas de Velocidade (Pace) ────────────────────────────────

        /// <summary>Aceleração — quão rápido o jogador chega à velocidade máxima.</summary>
        public int Acceleration { get; set; }

        /// <summary>Velocidade de Sprint — velocidade máxima do jogador.</summary>
        public int SprintSpeed { get; set; }

        // ── Sub-estatísticas de Finalização (Shooting) ───────────────────────────

        /// <summary>Posicionamento — habilidade de estar no lugar certo para finalizar.</summary>
        public int Positioning { get; set; }

        /// <summary>Finalização — precisão e potência ao finalizar.</summary>
        public int Finishing { get; set; }

        /// <summary>Potência de Chute — força aplicada nos chutes.</summary>
        public int ShotPower { get; set; }

        /// <summary>Chute de Longa Distância — eficácia em finalizações de fora da área.</summary>
        public int LongShots { get; set; }

        /// <summary>Voleio — habilidade em finalizações de voleio.</summary>
        public int Volleys { get; set; }

        /// <summary>Pênaltis — precisão nas cobranças de pênalti.</summary>
        public int Penalties { get; set; }

        // ── Sub-estatísticas de Passe (Passing) ──────────────────────────────────

        /// <summary>Visão — capacidade de enxergar e executar passes difíceis.</summary>
        public int Vision { get; set; }

        /// <summary>Cruzamento — precisão e potência em cruzamentos.</summary>
        public int Crossing { get; set; }

        /// <summary>Precisão em Falta — acurácia nas cobranças de falta.</summary>
        public int FKAccuracy { get; set; }

        /// <summary>Passe Curto — precisão e velocidade em passes próximos.</summary>
        public int ShortPassing { get; set; }

        /// <summary>Passe Longo — precisão e potência em passes longos.</summary>
        public int LongPassing { get; set; }

        /// <summary>Efeito — habilidade de dar efeito na bola.</summary>
        public int Curve { get; set; }

        // ── Sub-estatísticas de Drible (Dribbling) ───────────────────────────────

        /// <summary>Agilidade — quão rapidamente o jogador muda de direção.</summary>
        public int Agility { get; set; }

        /// <summary>Equilíbrio — capacidade de manter-se de pé sob pressão.</summary>
        public int Balance { get; set; }

        /// <summary>Reações — velocidade de resposta a situações de jogo.</summary>
        public int Reactions { get; set; }

        /// <summary>Controle de Bola — habilidade de receber e controlar a bola.</summary>
        public int BallControl { get; set; }

        /// <summary>Drible — habilidade geral de driblar adversários.</summary>
        public int DribblingSkill { get; set; }

        /// <summary>Composure — calma e controle em situações de pressão.</summary>
        public int Composure { get; set; }

        // ── Sub-estatísticas de Defesa (Defending) ───────────────────────────────

        /// <summary>Interceptação — habilidade de interceptar passes adversários.</summary>
        public int Interceptions { get; set; }

        /// <summary>Cabeceio — precisão e potência em cabeceios.</summary>
        public int HeadingAccuracy { get; set; }

        /// <summary>Consciência Defensiva — posicionamento e leitura defensiva.</summary>
        public int DefensiveAwareness { get; set; }

        /// <summary>Carrinho em Pé — eficácia ao realizar carrinhos parado.</summary>
        public int StandingTackle { get; set; }

        /// <summary>Carrinho Deslizante — eficácia ao realizar carrinhos deslizantes.</summary>
        public int SlidingTackle { get; set; }

        // ── Sub-estatísticas de Físico (Physical) ────────────────────────────────

        /// <summary>Salto — altura e potência ao saltar.</summary>
        public int Jumping { get; set; }

        /// <summary>Resistência — capacidade de manter o ritmo durante a partida.</summary>
        public int Stamina { get; set; }

        /// <summary>Força — potência física do jogador.</summary>
        public int Strength { get; set; }

        /// <summary>Agressividade — intensidade nas disputas de bola.</summary>
        public int Aggression { get; set; }

        // ── Atributo de Goleiro (quando aplicável) ────────────────────────────────

        /// <summary>Diving — habilidade de se jogar nas defesas.</summary>
        public int GKDiving { get; set; }

        /// <summary>Handling — habilidade de agarrar a bola.</summary>
        public int GKHandling { get; set; }

        /// <summary>Kicking — qualidade dos chutes do goleiro.</summary>
        public int GKKicking { get; set; }

        /// <summary>Reflexos — velocidade de reação do goleiro.</summary>
        public int GKReflexes { get; set; }

        /// <summary>Posicionamento do Goleiro — leitura de jogo e posicionamento.</summary>
        public int GKPositioning { get; set; }

        /// <summary>Indica se o jogador é goleiro (para exibir stats corretas).</summary>
        public bool IsGoalkeeper { get; set; }
    }
}
