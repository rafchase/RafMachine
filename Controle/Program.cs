using System;
using System.Collections.Generic;
using System.Globalization;

namespace Controle
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Quantas pelucias serão cadastradas? ");
            List<Pelucia> pelucia = new();
            int quantidadeRegistros = int.Parse(Console.ReadLine());
            int quantidade = 0;
            double valorCaptura = 0.0;
            double valorVenda = 0.0;
            Console.WriteLine("Insira os dados da pelucia: ");
            for (int i = 1; i <= quantidadeRegistros; i++)
            {
                Console.Write("Nome: ");
                string nome = Console.ReadLine();
                Console.Write("Tipo: ");
                string tipo = Console.ReadLine();
                Console.WriteLine("Possui estoque? ");
                char valida = char.Parse(Console.ReadLine());
                if (valida == 's' || valida == 'S')
                {
                    Console.Write("Quantidade: ");
                    quantidade = int.Parse(Console.ReadLine());
                }
                Console.WriteLine("Possui Valor de Captura? ");
                valida = char.Parse(Console.ReadLine());
                if (valida == 's' || valida == 'S')
                {
                    Console.Write("Valor: ");
                    valorCaptura = double.Parse(Console.ReadLine(), CultureInfo.InvariantCulture);
                }
                Console.WriteLine("Possui Valor de venda?  ");
                valida = char.Parse(Console.ReadLine());
                if (valida == 's' || valida == 'S')
                {
                    Console.Write("Valor: ");
                    valorVenda = double.Parse(Console.ReadLine(), CultureInfo.InvariantCulture);
                }
                pelucia.Add(new(i, nome, tipo, valorCaptura, valorVenda, quantidade));
                Console.WriteLine();
            }

            foreach (var item in pelucia)
            {
                Console.WriteLine(item);
            }

        }
    }
}
