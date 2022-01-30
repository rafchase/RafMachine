using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controle
{
    class Venda
    {
        public int Id { get; private set; }
        public double Valor { get; private set; }
        public string TipoPagamento { get; set; }
        public int numeroNota { get; set; }
        public int quantidadeProdutos { get; set; }
        public string statusEntrega { get; set; }
    }
}
