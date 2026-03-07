using System;

namespace FutbinSearch.Models
{
    /// <summary>
    /// Representa um ponto de dados no histórico de preços de uma carta do mercado FUT.
    /// Cada instância corresponde a um dia e o preço médio registrado naquele dia.
    /// </summary>
    public class PriceHistory
    {
        /// <summary>Data do registro de preço.</summary>
        public DateTime Date { get; set; }

        /// <summary>Preço registrado no mercado PS naquele dia.</summary>
        public long PricePS { get; set; }

        /// <summary>Preço registrado no mercado Xbox naquele dia.</summary>
        public long PriceXbox { get; set; }

        /// <summary>Preço registrado no mercado PC naquele dia.</summary>
        public long PricePC { get; set; }

        /// <summary>
        /// Preço PS formatado para exibição (ex: "1.5M", "850K", "N/D").
        /// </summary>
        public string PricePSFormatted =>
            PricePS > 0 ? FormatPrice(PricePS) : "N/D";

        /// <summary>
        /// Data formatada no padrão brasileiro (dd/MM/yyyy).
        /// </summary>
        public string DateFormatted => Date.ToString("dd/MM/yyyy");

        /// <summary>
        /// Formata um valor de preço para exibição amigável.
        /// </summary>
        private static string FormatPrice(long price)
        {
            if (price >= 1_000_000)
                return $"{price / 1_000_000.0:0.##}M";
            if (price >= 1_000)
                return $"{price / 1_000.0:0.##}K";
            return price.ToString();
        }
    }
}
