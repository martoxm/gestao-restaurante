using GestaoRestaurante.Core.Application.Abstractions;
using MediatR;
using GestaoRestaurante.Application.Abstractions.Repositories;
using GestaoRestaurante.Application.Abstractions.Services;
using GestaoRestaurante.Core.Application.Events;
using GestaoRestaurante.Core.Application.Exceptions;
using GestaoRestaurante.Core.Application.Models;
using GestaoRestaurante.Contracts.Produtos;
using GestaoRestaurante.Contracts.Common;
using GestaoRestaurante.Domain.Entities;
using GestaoRestaurante.Domain.Events;
using GestaoRestaurante.Core.Domain.ValueObjects;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Core.Mensageria.Contratos;
using Microsoft.Extensions.Logging;
using System.Net;

namespace GestaoRestaurante.Application.UseCases.Produtos;

public class CriarProdutoHandler : IRequestHandler<CriarProdutoCommand, Guid>
{
    private readonly IProdutoRepository _produtoRepository;
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IApplicationDbContext _context;
    private readonly IUsuarioAtualService _usuarioAtualService;
    private readonly IRelogio _relogio;
    private readonly IOutboxMensagemRepository _outbox;

    public CriarProdutoHandler(
        IProdutoRepository produtoRepository,
        ICategoriaRepository categoriaRepository,
        IApplicationDbContext context,
        IUsuarioAtualService usuarioAtualService,
        IRelogio relogio,
        IOutboxMensagemRepository outbox)
    {
        _produtoRepository = produtoRepository;
        _categoriaRepository = categoriaRepository;
        _context = context;
        _usuarioAtualService = usuarioAtualService;
        _relogio = relogio;
        _outbox = outbox;
    }

    public async Task<Guid> Handle(CriarProdutoCommand request, CancellationToken cancellationToken)
    {
        var categoria = await _categoriaRepository.ObterPorIdAsync(request.CategoriaId, cancellationToken);

        if (categoria is null)
            throw new NaoEncontradoException($"Categoria com Id '{request.CategoriaId}' não encontrada.");

        var produto = Produto.Criar(
            _usuarioAtualService.ObterRestauranteIdObrigatorio(),
            request.Nome,
            request.Descricao,
            Dinheiro.Criar(request.Preco),
            categoria,
            _relogio.AgoraUtc,
            Quantidade.Criar(request.QuantidadeInicial));

        _produtoRepository.Adicionar(produto);

        _outbox.Adicionar(CatalogoProdutosMensagens.Alterado(produto, _relogio.AgoraUtc));
        await _context.SaveChangesAsync(cancellationToken);

        return produto.Id;
    }
}

public class ExcluirProdutoHandler : IRequestHandler<ExcluirProdutoCommand>
{
    private readonly IProdutoRepository _produtoRepository;
    private readonly IApplicationDbContext _context;
    private readonly IOutboxMensagemRepository _outbox;
    private readonly IRelogio _relogio;

    public ExcluirProdutoHandler(
        IProdutoRepository produtoRepository,
        IApplicationDbContext context,
        IOutboxMensagemRepository outbox,
        IRelogio relogio)
    {
        _produtoRepository = produtoRepository;
        _context = context;
        _outbox = outbox;
        _relogio = relogio;
    }

    public async Task Handle(ExcluirProdutoCommand request, CancellationToken cancellationToken)
    {
        var produto = await _produtoRepository.ObterPorIdAsync(request.Id, cancellationToken)
            ?? throw new NaoEncontradoException($"Produto com Id '{request.Id}' não encontrado.");

        produto.Excluir(_relogio.AgoraUtc);

        _produtoRepository.Atualizar(produto);
        _outbox.Adicionar(CatalogoProdutosMensagens.Excluido(produto, _relogio.AgoraUtc));
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class ReativarProdutoHandler : IRequestHandler<ReativarProdutoCommand>
{
    private readonly IProdutoRepository _produtoRepository;
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IApplicationDbContext _context;
    private readonly IOutboxMensagemRepository _outbox;
    private readonly IRelogio _relogio;

    public ReativarProdutoHandler(
        IProdutoRepository produtoRepository,
        ICategoriaRepository categoriaRepository,
        IApplicationDbContext context,
        IOutboxMensagemRepository outbox,
        IRelogio relogio)
    {
        _produtoRepository = produtoRepository;
        _categoriaRepository = categoriaRepository;
        _context = context;
        _outbox = outbox;
        _relogio = relogio;
    }

    public async Task Handle(ReativarProdutoCommand request, CancellationToken cancellationToken)
    {
        var produto = await _produtoRepository.ObterExcluidoPorIdAsync(request.Id, cancellationToken)
            ?? throw new NaoEncontradoException($"Produto excluído com Id '{request.Id}' não encontrado.");

        var categoria = await _categoriaRepository.ObterPorIdAsync(produto.CategoriaId, cancellationToken);
        if (categoria is null)
            throw new GestaoRestaurante.Core.Domain.DomainException("A categoria desse produto está excluída. Reative a categoria antes.");

        produto.Reativar();

        _produtoRepository.Atualizar(produto);
        _outbox.Adicionar(CatalogoProdutosMensagens.Alterado(produto, _relogio.AgoraUtc));
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class ListarProdutosExcluidosHandler : IRequestHandler<ListarProdutosExcluidosQuery, IReadOnlyList<ItemExcluidoDto>>
{
    private readonly IProdutoRepository _produtoRepository;

    public ListarProdutosExcluidosHandler(IProdutoRepository produtoRepository) => _produtoRepository = produtoRepository;

    public async Task<IReadOnlyList<ItemExcluidoDto>> Handle(ListarProdutosExcluidosQuery request, CancellationToken cancellationToken)
    {
        var produtos = await _produtoRepository.ListarExcluidosAsync(cancellationToken);
        return produtos.Select(produto => new ItemExcluidoDto(produto.Id, produto.Nome, produto.ExcluidoEm!.Value)).ToList();
    }
}

public class AtualizarProdutoHandler : IRequestHandler<AtualizarProdutoCommand>
{
    private readonly IProdutoRepository _produtoRepository;
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IApplicationDbContext _context;
    private readonly IOutboxMensagemRepository _outbox;
    private readonly IRelogio _relogio;

    public AtualizarProdutoHandler(
        IProdutoRepository produtoRepository,
        ICategoriaRepository categoriaRepository,
        IApplicationDbContext context,
        IOutboxMensagemRepository outbox,
        IRelogio relogio)
    {
        _produtoRepository = produtoRepository;
        _categoriaRepository = categoriaRepository;
        _context = context;
        _outbox = outbox;
        _relogio = relogio;
    }

    public async Task Handle(AtualizarProdutoCommand request, CancellationToken cancellationToken)
    {
        var produto = await _produtoRepository.ObterPorIdAsync(request.Id, cancellationToken);

        if (produto is null)
            throw new NaoEncontradoException($"Produto com Id '{request.Id}' não encontrado.");

        var categoria = await _categoriaRepository.ObterPorIdAsync(request.CategoriaId, cancellationToken);

        if (categoria is null)
            throw new NaoEncontradoException($"Categoria com Id '{request.CategoriaId}' não encontrada.");

        produto.Atualizar(request.Nome, request.Descricao, Dinheiro.Criar(request.Preco), categoria);

        _produtoRepository.Atualizar(produto);
        _outbox.Adicionar(CatalogoProdutosMensagens.Alterado(produto, _relogio.AgoraUtc));

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class ObterProdutoPorIdHandler : IRequestHandler<ObterProdutoPorIdQuery, ProdutoDetalhesDto?>
{
    private readonly IProdutoRepository _produtoRepository;

    public ObterProdutoPorIdHandler(IProdutoRepository produtoRepository)
    {
        _produtoRepository = produtoRepository;
    }

    public async Task<ProdutoDetalhesDto?> Handle(ObterProdutoPorIdQuery request, CancellationToken cancellationToken)
    {
        var produto = await _produtoRepository.ObterPorIdAsync(request.Id, cancellationToken);

        if (produto is null)
            return null;

        return produto.ParaDetalhesDto();
    }
}

public class ListarProdutosHandler : IRequestHandler<ListarProdutosQuery, ResultadoPaginado<ProdutoItemListaDto>>
{
    private readonly IProdutoRepository _produtoRepository;

    public ListarProdutosHandler(IProdutoRepository produtoRepository)
    {
        _produtoRepository = produtoRepository;
    }

    public async Task<ResultadoPaginado<ProdutoItemListaDto>> Handle(ListarProdutosQuery request, CancellationToken cancellationToken)
    {
        var (produtos, totalItens) = await _produtoRepository.ListarPaginadoAsync(
            request.Pagina,
            request.TamanhoPagina,
            request.Nome,
            request.CategoriaId,
            request.OrdenarPor,
            request.DirecaoOrdenacao,
            cancellationToken);

        var itens = produtos
            .Select(produto => produto.ParaItemListaDto())
            .ToList();

        return new ResultadoPaginado<ProdutoItemListaDto>(itens, request.Pagina, request.TamanhoPagina, totalItens);
    }
}

public class AvisarEstoqueBaixoHandler : INotificationHandler<DomainEventNotification<EstoqueFicouBaixoEvent>>
{
    private readonly IOutboxMensagemRepository _outboxMensagemRepository;
    private readonly IRelogio _relogio;

    public AvisarEstoqueBaixoHandler(IOutboxMensagemRepository outboxMensagemRepository, IRelogio relogio)
    {
        _outboxMensagemRepository = outboxMensagemRepository;
        _relogio = relogio;
    }

    public Task Handle(DomainEventNotification<EstoqueFicouBaixoEvent> notification, CancellationToken cancellationToken)
    {
        var evento = notification.Evento;

        var nomeProduto = WebUtility.HtmlEncode(evento.NomeProduto);
        var limite = Produto.LimiteEstoqueBaixo.Valor;

        var assunto = $"Estoque baixo: {evento.NomeProduto}";
        var corpoHtml = $"""
            <p>Olá,</p>
            <p>O produto <b>{nomeProduto}</b> está com o estoque baixo:
            restam <b>{evento.QuantidadeAtual}</b> unidade(s) (o limite é {limite}).</p>
            <p>Considere fazer uma nova compra.</p>
            <p style="color:#888">Aviso automático do Controle de Estoque.</p>
            """;

        var mensagem = new AvisarAdminsRestauranteMensagem(evento.RestauranteId, assunto, corpoHtml);
        _outboxMensagemRepository.Adicionar(OutboxMensagem.Criar(Filas.AvisarAdmins, mensagem, _relogio.AgoraUtc));

        return Task.CompletedTask;
    }
}

public class EncaminharAvisoAdminsHandler : IRequestHandler<EncaminharAvisoAdminsCommand>
{
    private readonly IIdentidadeClient _identidade;
    private readonly IOutboxMensagemRepository _outbox;
    private readonly IMensagemProcessadaRepository _mensagensProcessadas;
    private readonly IApplicationDbContext _context;
    private readonly IRelogio _relogio;
    private readonly ILogger<EncaminharAvisoAdminsHandler> _logger;

    public EncaminharAvisoAdminsHandler(
        IIdentidadeClient identidade,
        IOutboxMensagemRepository outbox,
        IMensagemProcessadaRepository mensagensProcessadas,
        IApplicationDbContext context,
        IRelogio relogio,
        ILogger<EncaminharAvisoAdminsHandler> logger)
    {
        _identidade = identidade;
        _outbox = outbox;
        _mensagensProcessadas = mensagensProcessadas;
        _context = context;
        _relogio = relogio;
        _logger = logger;
    }

    public async Task Handle(EncaminharAvisoAdminsCommand request, CancellationToken cancellationToken)
    {
        if (await _mensagensProcessadas.ExisteAsync(request.MensagemId, cancellationToken))
            return;

        var destinatarios = await _identidade.ListarEmailsAdminsAsync(request.RestauranteId, cancellationToken);
        var agora = _relogio.AgoraUtc;

        if (destinatarios.Count == 0)
        {
            _logger.LogWarning("Restaurante {RestauranteId} não tem Admin com e-mail: aviso '{Assunto}' descartado.", request.RestauranteId, request.Assunto);
        }
        else
        {
            var email = new EnviarEmailMensagem(destinatarios, request.Assunto, request.CorpoHtml);
            _outbox.Adicionar(OutboxMensagem.Criar(Filas.Email, email, agora));
        }

        _mensagensProcessadas.Adicionar(MensagemProcessada.Criar(request.MensagemId, Filas.AvisarAdmins, agora));
        await _context.SaveChangesAsync(cancellationToken);
    }
}

file static class ProdutoMapeamentos
{
    public static ProdutoDetalhesDto ParaDetalhesDto(this Produto produto) => new(
        produto.Id,
        produto.Nome,
        produto.Descricao,
        produto.Preco.Valor,
        produto.QuantidadeEmEstoque.Valor,
        produto.CategoriaId,
        produto.Categoria!.Nome,
        produto.CriadoEm);

    public static ProdutoItemListaDto ParaItemListaDto(this Produto produto) => new(
        produto.Id,
        produto.Nome,
        produto.Preco.Valor,
        produto.QuantidadeEmEstoque.Valor,
        produto.CategoriaId,
        produto.Categoria!.Nome,
        produto.CriadoEm);
}

file static class CatalogoProdutosMensagens
{
    public static OutboxMensagem Alterado(Produto produto, DateTime agora) => Montar(produto, excluido: false, agora);

    public static OutboxMensagem Excluido(Produto produto, DateTime agora) => Montar(produto, excluido: true, agora);

    private static OutboxMensagem Montar(Produto produto, bool excluido, DateTime agora)
    {
        var mensagem = new ProdutoCatalogoMensagem(produto.Id, produto.RestauranteId, produto.Nome, produto.Preco.Valor, excluido, agora);
        return OutboxMensagem.Criar(Filas.CatalogoProdutos, mensagem, agora);
    }
}
