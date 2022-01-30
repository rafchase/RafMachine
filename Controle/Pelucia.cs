using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controle
{
    class Pelucia
    {
        public int Id { get; private set; }
        public string Nome { get; set; }
        public string Tipo { get; set; }
        public double ValorCaptura { get; private set; }
        public double ValorVenda { get; private set; }
        public int Quantidade { get; set; }

        public Pelucia(int id, string nome, string tipo)
        {
            Id = id;
            Nome = nome;
            Tipo = tipo;
        }

        public Pelucia(int id, string nome, string tipo, double valorCaptura) : this(id, nome, tipo)
        {
            ValorCaptura = valorCaptura;
        }

        public Pelucia(int id, string nome, string tipo, double valorCaptura, double valorVenda) : this(id, nome, tipo, valorCaptura)
        {
            ValorVenda = valorVenda;
        }

        public Pelucia(int id, string nome, string tipo, double valorCaptura, int quantidade) : this(id, nome, tipo, valorCaptura)
        {
            Quantidade = quantidade;
        }

        public Pelucia(int id, string nome, string tipo, double valorCaptura, double valorVenda, int quantidade) : this(id, nome, tipo, valorCaptura, valorVenda)
        {
            Quantidade = quantidade;
        }

        public void InserirValorVenda(double valor)
        {
            ValorVenda = valor;
        }

        public void InserirQuantidade(int quantidade)
        {
            Quantidade = quantidade;
        }

        public override string ToString()
        {
            return $"{Nome}, {Tipo}, Estoque: {Quantidade}, Valor: {ValorVenda}";
        }
    }
}
