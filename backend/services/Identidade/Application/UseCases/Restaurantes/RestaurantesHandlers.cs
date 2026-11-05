using MediatR;
using Microsoft.Extensions.Logging;
using GestaoRestaurante.Core.Mensageria.Contratos;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Core.Application.Abstractions;
using System.Net;
using GestaoRestaurante.Identidade.Application.Abstractions.Repositories;
using GestaoRestaurante.Identidade.Application.Abstractions.Services;
using GestaoRestaurante.Core.Application.Exceptions;
using GestaoRestaurante.Core.Application.Models;
using GestaoRestaurante.Identidade.Contracts.Restaurantes;
using GestaoRestaurante.Identidade.Domain.Entities;

namespace GestaoRestaurante.Identidade.Application.UseCases.Restaurantes;

public class AtualizarRestauranteHandler : IRequestHandler<AtualizarRestauranteCommand>
{
    private readonly IRestauranteRepository _restauranteRepository;
    private readonly IApplicationDbContext _context;

    public AtualizarRestauranteHandler(IRestauranteRepository restauranteRepository, IApplicationDbContext context)
    {
        _restauranteRepository = restauranteRepository;
        _context = context;
    }

    public async Task Handle(AtualizarRestauranteCommand request, CancellationToken cancellationToken)
    {
        var restaurante = await _restauranteRepository.ObterPorIdAsync(request.Id, cancellationToken);

        if (restaurante is null)
            throw new NaoEncontradoException($"Restaurante com Id '{request.Id}' não encontrado.");

        restaurante.Atualizar(request.Nome);

        _restauranteRepository.Atualizar(restaurante);

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class ObterRestaurantePorIdHandler : IRequestHandler<ObterRestaurantePorIdQuery, RestauranteDetalhesDto?>
{
    private readonly IRestauranteRepository _restauranteRepository;

    public ObterRestaurantePorIdHandler(IRestauranteRepository restauranteRepository)
    {
        _restauranteRepository = restauranteRepository;
    }

    public async Task<RestauranteDetalhesDto?> Handle(ObterRestaurantePorIdQuery request, CancellationToken cancellationToken)
    {
        var restaurante = await _restauranteRepository.ObterPorIdAsync(request.Id, cancellationToken);

        if (restaurante is null)
            return null;

        return restaurante.ParaDetalhesDto();
    }
}

public class ListarRestaurantesHandler : IRequestHandler<ListarRestaurantesQuery, ResultadoPaginado<RestauranteItemListaDto>>
{
    private readonly IRestauranteRepository _restauranteRepository;

    public ListarRestaurantesHandler(IRestauranteRepository restauranteRepository)
    {
        _restauranteRepository = restauranteRepository;
    }

    public async Task<ResultadoPaginado<RestauranteItemListaDto>> Handle(ListarRestaurantesQuery request, CancellationToken cancellationToken)
    {
        var (restaurantes, totalItens) = await _restauranteRepository.ListarPaginadoAsync(
            request.Pagina,
            request.TamanhoPagina,
            request.Nome,
            request.DirecaoOrdenacao,
            cancellationToken);

        var itens = restaurantes
            .Select(restaurante => restaurante.ParaItemListaDto())
            .ToList();

        return new ResultadoPaginado<RestauranteItemListaDto>(itens, request.Pagina, request.TamanhoPagina, totalItens);
    }
}

public class ListarEmailsAdminsHandler : IRequestHandler<ListarEmailsAdminsQuery, EmailsAdminsDto>
{
    private readonly IIdentityService _identityService;

    public ListarEmailsAdminsHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<EmailsAdminsDto> Handle(ListarEmailsAdminsQuery request, CancellationToken cancellationToken)
    {
        var emails = await _identityService.ListarEmailsAdminsAsync(request.RestauranteId, cancellationToken);
        return new EmailsAdminsDto(emails);
    }
}

file static class RestauranteMapeamentos
{
    public static RestauranteDetalhesDto ParaDetalhesDto(this Restaurante restaurante) =>
        new(restaurante.Id, restaurante.Nome, restaurante.CriadoEm, restaurante.Plano.ToString(), restaurante.TesteTerminaEm, restaurante.Ativo, restaurante.PlanoValidoAte);

    public static RestauranteItemListaDto ParaItemListaDto(this Restaurante restaurante) =>
        new(restaurante.Id, restaurante.Nome, restaurante.CriadoEm, restaurante.Plano.ToString(), restaurante.TesteTerminaEm, restaurante.Ativo, restaurante.PlanoValidoAte);
}

public class AvisarFimDoTesteHandler : IRequestHandler<AvisarFimDoTesteCommand, int>
{
    private readonly IRestauranteRepository _restauranteRepository;
    private readonly IIdentityService _identityService;
    private readonly IOutboxMensagemRepository _outboxRepository;
    private readonly ILinksFront _linksFront;
    private readonly IApplicationDbContext _context;
    private readonly IRelogio _relogio;
    private readonly ILogger<AvisarFimDoTesteHandler> _logger;

    public AvisarFimDoTesteHandler(
        IRestauranteRepository restauranteRepository,
        IIdentityService identityService,
        IOutboxMensagemRepository outboxRepository,
        ILinksFront linksFront,
        IApplicationDbContext context,
        IRelogio relogio,
        ILogger<AvisarFimDoTesteHandler> logger)
    {
        _restauranteRepository = restauranteRepository;
        _identityService = identityService;
        _outboxRepository = outboxRepository;
        _linksFront = linksFront;
        _context = context;
        _relogio = relogio;
        _logger = logger;
    }

    public async Task<int> Handle(AvisarFimDoTesteCommand request, CancellationToken cancellationToken)
    {
        var agora = _relogio.AgoraUtc;
        var maiorMarco = Restaurante.DiasAvisoFimTeste.Max();
        var candidatos = await _restauranteRepository.ListarTestesTerminandoAsync(agora, agora.AddDays(maiorMarco), cancellationToken);

        var avisados = 0;
        foreach (var restaurante in candidatos)
        {
            if (restaurante.DiasParaAvisarFimDoTeste(agora) is not int dias)
                continue;

            var emails = await _identityService.ListarEmailsAdminsAsync(restaurante.Id, cancellationToken);
            if (emails.Count > 0)
            {
                _outboxRepository.Adicionar(OutboxMensagem.Criar(
                    Filas.Email,
                    EmailsFimDoTeste.Montar(emails, restaurante, dias, _linksFront.Assinatura()),
                    agora));
                avisados++;
            }
            else
            {
                _logger.LogWarning("Restaurante {RestauranteId} sem Admin: aviso de fim do teste não enviado.", restaurante.Id);
            }

            restaurante.RegistrarAvisoFimDoTeste(dias);
            _restauranteRepository.Atualizar(restaurante);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return avisados;
    }
}

file static class EmailsFimDoTeste
{
    public static EnviarEmailMensagem Montar(IReadOnlyList<string> emails, Restaurante restaurante, int dias, string link)
    {
        var quando = dias == 1 ? "amanhã" : $"em {dias} dias";
        var nome = WebUtility.HtmlEncode(restaurante.Nome);
        var corpo = $"""
            <p>Olá!</p>
            <p>O teste grátis do restaurante <strong>{nome}</strong> no Gestão Restaurante termina <strong>{quando}</strong>.</p>
            <p>Depois disso, os dados continuam guardados, mas não dá mais para cadastrar nem alterar nada.</p>
            <p><a href="{link}">Clique aqui para escolher um plano</a> e continuar usando sem interrupção.</p>
            """;

        return new EnviarEmailMensagem(emails, $"Seu teste termina {quando} - Gestão Restaurante", corpo);
    }
}

public class AplicarPagamentoPlanoHandler : IRequestHandler<AplicarPagamentoPlanoCommand>
{
    private readonly IRestauranteRepository _restauranteRepository;
    private readonly IMensagemProcessadaRepository _mensagensProcessadas;
    private readonly IApplicationDbContext _context;
    private readonly IRelogio _relogio;
    private readonly ILogger<AplicarPagamentoPlanoHandler> _logger;

    public AplicarPagamentoPlanoHandler(
        IRestauranteRepository restauranteRepository,
        IMensagemProcessadaRepository mensagensProcessadas,
        IApplicationDbContext context,
        IRelogio relogio,
        ILogger<AplicarPagamentoPlanoHandler> logger)
    {
        _restauranteRepository = restauranteRepository;
        _mensagensProcessadas = mensagensProcessadas;
        _context = context;
        _relogio = relogio;
        _logger = logger;
    }

    public async Task Handle(AplicarPagamentoPlanoCommand request, CancellationToken cancellationToken)
    {
        var pagamento = request.Pagamento;

        if (await _mensagensProcessadas.ExisteAsync(pagamento.CobrancaId, cancellationToken))
            return;

        var restaurante = await _restauranteRepository.ObterPorIdSemFiltroAsync(pagamento.RestauranteId, cancellationToken);
        if (restaurante is null)
            _logger.LogWarning("Plano pago para o restaurante {RestauranteId}, que não existe.", pagamento.RestauranteId);
        else if (!Enum.TryParse<GestaoRestaurante.Identidade.Domain.Enums.PlanoRestaurante>(pagamento.Plano, out var plano))
            _logger.LogError("Plano desconhecido na mensagem de pagamento: {Plano}.", pagamento.Plano);
        else
        {
            restaurante.AplicarPagamentoDoPlano(plano, pagamento.Dias, _relogio.AgoraUtc);
            _restauranteRepository.Atualizar(restaurante);
            _logger.LogInformation("Restaurante {RestauranteId} no plano {Plano} até {ValidoAte}.", restaurante.Id, plano, restaurante.PlanoValidoAte);
        }

        _mensagensProcessadas.Adicionar(MensagemProcessada.Criar(pagamento.CobrancaId, Filas.PlanoPago, _relogio.AgoraUtc));
        await _context.SaveChangesAsync(cancellationToken);
    }
}
