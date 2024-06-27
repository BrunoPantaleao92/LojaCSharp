using loja.data;
using loja.models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace loja.services
{
    public class VendaService
    {
        private readonly LojaDbContext _context;
        private readonly ProdutoService _produtoService;
        private readonly ClienteService _clienteService;

        public VendaService(LojaDbContext context, ProdutoService produtoService, ClienteService clienteService)
        {
            _context = context;
            _produtoService = produtoService;
            _clienteService = clienteService;
        }

        public async Task<Venda> GravarVendaAsync(Venda venda)
        {
            // Verificar se o cliente existe
            var cliente = await _clienteService.GetClienteByIdAsync(venda.ClienteId);
            if (cliente == null)
                throw new InvalidOperationException("Cliente não encontrado.");

            // Verificar se o produto existe
            var produto = await _produtoService.GetProductByIdAsync(venda.ProdutoId);
            if (produto == null)
                throw new InvalidOperationException("Produto não encontrado.");

            // Validar e gravar a venda
            venda.DataVenda = DateTime.Now; // Ou use a data fornecida, se houver
            _context.Vendas.Add(venda);
            await _context.SaveChangesAsync();

            return venda;
        }

        public async Task<List<object>> ConsultarVendasPorProdutoDetalhadaAsync(int produtoId)
        {
            var vendasDetalhadas = await _context.Vendas
                .Where(v => v.ProdutoId == produtoId)
                .Select(v => new
                {
                    ProdutoNome = v.Produto.Nome,
                    DataVenda = v.DataVenda,
                    VendaId = v.Id,
                    ClienteNome = v.Cliente.Nome,
                    QuantidadeVendida = v.QuantidadeVendida,
                    PrecoVendaUnitario = v.PrecoUnitario
                })
                .ToListAsync();

            return vendasDetalhadas.Select(v => new
            {
                ProdutoNome = v.ProdutoNome,
                DataVenda = v.DataVenda,
                VendaId = v.VendaId,
                ClienteNome = v.ClienteNome,
                QuantidadeVendida = v.QuantidadeVendida,
                PrecoVendaUnitario = v.PrecoVendaUnitario
            }).ToList<object>();
        }

        public async Task<List<object>> ConsultarVendasPorClienteDetalhadaAsync(int clienteId)
        {
            var vendasDetalhadas = await _context.Vendas
                .Where(v => v.ClienteId == clienteId)
                .Select(v => new
                {
                    ProdutoNome = v.Produto.Nome,
                    DataVenda = v.DataVenda,
                    VendaId = v.Id,
                    QuantidadeVendida = v.QuantidadeVendida,
                    PrecoVendaUnitario = v.PrecoUnitario
                })
                .ToListAsync();

            return vendasDetalhadas.Select(v => new
            {
                ProdutoNome = v.ProdutoNome,
                DataVenda = v.DataVenda,
                VendaId = v.VendaId,
                QuantidadeVendida = v.QuantidadeVendida,
                PrecoVendaUnitario = v.PrecoVendaUnitario
            }).ToList<object>();
        }

        public async Task<List<object>> ConsultarVendasPorProdutoSumarizadaAsync(int produtoId)
        {
            var vendasSumarizadas = await _context.Vendas
                .Where(v => v.ProdutoId == produtoId)
                .GroupBy(v => v.ProdutoId)
                .Select(g => new
                {
                    ProdutoNome = g.FirstOrDefault().Produto.Nome,
                    TotalQuantidadeVendida = g.Sum(v => v.QuantidadeVendida),
                    TotalPrecoCobrado = g.Sum(v => v.QuantidadeVendida * v.PrecoUnitario)
                })
                .ToListAsync();

            return vendasSumarizadas.Select(v => new
            {
                ProdutoNome = v.ProdutoNome,
                TotalQuantidadeVendida = v.TotalQuantidadeVendida,
                TotalPrecoCobrado = v.TotalPrecoCobrado
            }).ToList<object>();
        }

        public async Task<List<object>> ConsultarVendasPorClienteSumarizadaAsync(int clienteId)
        {
            var vendasSumarizadas = await _context.Vendas
                .Where(v => v.ClienteId == clienteId)
                .GroupBy(v => v.ProdutoId)
                .Select(g => new
                {
                    ProdutoNome = g.FirstOrDefault().Produto.Nome,
                    TotalQuantidadeVendida = g.Sum(v => v.QuantidadeVendida),
                    TotalPrecoCobrado = g.Sum(v => v.QuantidadeVendida * v.PrecoUnitario)
                })
                .ToListAsync();

            return vendasSumarizadas.Select(v => new
            {
                ProdutoNome = v.ProdutoNome,
                TotalQuantidadeVendida = v.TotalQuantidadeVendida,
                TotalPrecoCobrado = v.TotalPrecoCobrado
            }).ToList<object>();
        }
    }
}
