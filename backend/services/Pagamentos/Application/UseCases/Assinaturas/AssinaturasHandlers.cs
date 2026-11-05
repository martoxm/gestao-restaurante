using MediatR;
using GestaoRestaurante.Core.Application.Abstractions;
using GestaoRestaurante.Core.Domain;
using GestaoRestaurante.Core.Domain.ValueObjects;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Core.Mensageria.Contratos;
using Microsoft.Extensions.Logging;
using GestaoRestaurante.Pagamentos.Application.Abstractions.Repositories;
using GestaoRestaurante.Pagamentos.Application.Abstractions.Services;
using GestaoRestaurante.Pagamentos.Contracts.Assinaturas;
using GestaoRestaurante.Pagamentos.Domain.Entities;
using GestaoRestaurante.Pagamentos.Domain.Enums;

namespace GestaoRestaurante.Pagamentos.Application.UseCases.Assinaturas;

public class ListarPlanosHandler : IRequestHandler<ListarPlanosQuery, IReadOnlyList<PlanoAVendaDto>>
{
    private readonly ITabelaPlanos _tabela;

    public ListarPlanosHandler(ITabelaPlanos tabela)
    {
        _tabela = tabela;
    }

    public Task<IReadOnlyList<PlanoAVendaDto>> Handle(ListarPlanosQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<PlanoAVendaDto> planos = _tabela.Listar()
            .Select(plano => new PlanoAVendaDto(plano.Plano.ToString(), plano.Nome, plano.Preco, CobrancaPlano.DiasPorPagamento))
            .ToList();
        return Task.FromResult(planos);
    }
}

public class IniciarAssinaturaHandler : IRequestHandler<IniciarAssinaturaCommand, CheckoutAssinaturaResponse>
{
    private readonly ICobrancaPlanoRepository _repository;
    private readonly ICheckoutPlano _checkout;
    private readonly ITabelaPlanos _tabela;
    private readonly IOutboxMensagemRepository _outbox;
    private readonly IUsuarioAtualService _usuarioAtual;
    private readonly IApplicationDbContext _context;
    private readonly IRelogio _relogio;

    public IniciarAssinaturaHandler(
        ICobrancaPlanoRepository repository,
        ICheckoutPlano checkout,
        ITabelaPlanos tabela,
        IOutboxMensagemRepository outbox,
        IUsuarioAtualService usuarioAtual,
        IApplicationDbContext context,
        IRelogio relogio)
    {
        _repository = repository;
        _checkout = checkout;
        _tabela = tabela;
        _outbox = outbox;
        _usuarioAtual = usuarioAtual;
        _context = context;
        _relogio = relogio;
    }

    public async Task<CheckoutAssinaturaResponse> Handle(IniciarAssinaturaCommand request, CancellationToken cancellationToken)
    {
        var restauranteId = _usuarioAtual.ObterRestauranteIdObrigatorio();

        var plano = Enum.Parse<PlanoPago>(request.Plano, ignoreCase: true);
        var aVenda = _tabela.Listar().FirstOrDefault(item => item.Plano == plano)
            ?? throw new DomainException($"O plano {plano} não está à venda.");

        var cobranca = CobrancaPlano.Criar(restauranteId, plano, Dinheiro.Criar(aVenda.Preco), _relogio.AgoraUtc);
        _repository.Adicionar(cobranca);
        await _context.SaveChangesAsync(cancellationToken);

        var sessao = await _checkout.CriarSessaoAsync(
            new PedidoCheckout(cobranca.Id, restauranteId, plano, aVenda.Nome, aVenda.Preco, cobranca.Dias),
            cancellationToken);

        cobranca.RegistrarSessao(sessao.IdSessao);

        if (sessao.JaPaga && cobranca.MarcarComoPaga(_relogio.AgoraUtc))
            _outbox.Adicionar(OutboxMensagem.Criar(Filas.PlanoPago, CobrancaPlanoMensagens.PlanoPago(cobranca), _relogio.AgoraUtc));

        _repository.Atualizar(cobranca);
        await _context.SaveChangesAsync(cancellationToken);

        return new CheckoutAssinaturaResponse(cobranca.Id, sessao.Url);
    }
}

public class ConfirmarPagamentoPlanoHandler : IRequestHandler<ConfirmarPagamentoPlanoCommand>
{
    private readonly ICobrancaPlanoRepository _repository;
    private readonly IOutboxMensagemRepository _outbox;
    private readonly IApplicationDbContext _context;
    private readonly IRelogio _relogio;
    private readonly ILogger<ConfirmarPagamentoPlanoHandler> _logger;

    public ConfirmarPagamentoPlanoHandler(
        ICobrancaPlanoRepository repository,
        IOutboxMensagemRepository outbox,
        IApplicationDbContext context,
        IRelogio relogio,
        ILogger<ConfirmarPagamentoPlanoHandler> logger)
    {
        _repository = repository;
        _outbox = outbox;
        _context = context;
        _relogio = relogio;
        _logger = logger;
    }

    public async Task Handle(ConfirmarPagamentoPlanoCommand request, CancellationToken cancellationToken)
    {
        var cobranca = await _repository.ObterPorSessaoSemFiltroAsync(request.IdSessao, cancellationToken);
        if (cobranca is null)
        {
            _logger.LogWarning("Webhook de plano pago para a sessão {IdSessao}, que não existe aqui.", request.IdSessao);
            return;
        }

        if (!cobranca.MarcarComoPaga(_relogio.AgoraUtc))
            return;

        _repository.Atualizar(cobranca);
        _outbox.Adicionar(OutboxMensagem.Criar(Filas.PlanoPago, CobrancaPlanoMensagens.PlanoPago(cobranca), _relogio.AgoraUtc));
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cobrança {CobrancaId} paga: plano {Plano} do restaurante {RestauranteId}.", cobranca.Id, cobranca.Plano, cobranca.RestauranteId);
    }
}

internal static class CobrancaPlanoMensagens
{
    public static PlanoPagoMensagem PlanoPago(CobrancaPlano cobranca) =>
        new(cobranca.Id, cobranca.RestauranteId, cobranca.Plano.ToString(), cobranca.Dias, cobranca.PagaEm ?? cobranca.CriadoEm);
}
