using GestaoRestaurante.Core.Application.Abstractions;
using MediatR;
using GestaoRestaurante.Application.Abstractions.Repositories;
using GestaoRestaurante.Application.Abstractions.Services;
using GestaoRestaurante.Core.Application.Exceptions;
using GestaoRestaurante.Core.Application.Models;
using GestaoRestaurante.Contracts.Fornecedores;
using GestaoRestaurante.Contracts.Common;
using GestaoRestaurante.Domain.Entities;
using GestaoRestaurante.Core.Domain.ValueObjects;

namespace GestaoRestaurante.Application.UseCases.Fornecedores;

public class CriarFornecedorHandler : IRequestHandler<CriarFornecedorCommand, Guid>
{
    private readonly IFornecedorRepository _fornecedorRepository;
    private readonly IApplicationDbContext _context;
    private readonly IUsuarioAtualService _usuarioAtualService;
    private readonly IRelogio _relogio;

    public CriarFornecedorHandler(
        IFornecedorRepository fornecedorRepository,
        IApplicationDbContext context,
        IUsuarioAtualService usuarioAtualService,
        IRelogio relogio)
    {
        _fornecedorRepository = fornecedorRepository;
        _context = context;
        _usuarioAtualService = usuarioAtualService;
        _relogio = relogio;
    }

    public async Task<Guid> Handle(CriarFornecedorCommand request, CancellationToken cancellationToken)
    {
        var fornecedor = Fornecedor.Criar(_usuarioAtualService.ObterRestauranteIdObrigatorio(), request.Nome, _relogio.AgoraUtc, request.Telefone, Email.CriarOuNulo(request.Email));

        _fornecedorRepository.Adicionar(fornecedor);
        await _context.SaveChangesAsync(cancellationToken);

        return fornecedor.Id;
    }
}

public class ExcluirFornecedorHandler : IRequestHandler<ExcluirFornecedorCommand>
{
    private readonly IFornecedorRepository _fornecedorRepository;
    private readonly IApplicationDbContext _context;
    private readonly IRelogio _relogio;

    public ExcluirFornecedorHandler(IFornecedorRepository fornecedorRepository, IApplicationDbContext context, IRelogio relogio)
    {
        _fornecedorRepository = fornecedorRepository;
        _context = context;
        _relogio = relogio;
    }

    public async Task Handle(ExcluirFornecedorCommand request, CancellationToken cancellationToken)
    {
        var fornecedor = await _fornecedorRepository.ObterPorIdAsync(request.Id, cancellationToken)
            ?? throw new NaoEncontradoException($"Fornecedor com Id '{request.Id}' não encontrado.");

        fornecedor.Excluir(_relogio.AgoraUtc);

        _fornecedorRepository.Atualizar(fornecedor);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class ReativarFornecedorHandler : IRequestHandler<ReativarFornecedorCommand>
{
    private readonly IFornecedorRepository _fornecedorRepository;
    private readonly IApplicationDbContext _context;

    public ReativarFornecedorHandler(IFornecedorRepository fornecedorRepository, IApplicationDbContext context)
    {
        _fornecedorRepository = fornecedorRepository;
        _context = context;
    }

    public async Task Handle(ReativarFornecedorCommand request, CancellationToken cancellationToken)
    {
        var fornecedor = await _fornecedorRepository.ObterExcluidoPorIdAsync(request.Id, cancellationToken)
            ?? throw new NaoEncontradoException($"Fornecedor excluído com Id '{request.Id}' não encontrado.");

        fornecedor.Reativar();

        _fornecedorRepository.Atualizar(fornecedor);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class ListarFornecedoresExcluidosHandler : IRequestHandler<ListarFornecedoresExcluidosQuery, IReadOnlyList<ItemExcluidoDto>>
{
    private readonly IFornecedorRepository _fornecedorRepository;

    public ListarFornecedoresExcluidosHandler(IFornecedorRepository fornecedorRepository) => _fornecedorRepository = fornecedorRepository;

    public async Task<IReadOnlyList<ItemExcluidoDto>> Handle(ListarFornecedoresExcluidosQuery request, CancellationToken cancellationToken)
    {
        var fornecedores = await _fornecedorRepository.ListarExcluidosAsync(cancellationToken);
        return fornecedores.Select(fornecedor => new ItemExcluidoDto(fornecedor.Id, fornecedor.Nome, fornecedor.ExcluidoEm!.Value)).ToList();
    }
}

public class AtualizarFornecedorHandler : IRequestHandler<AtualizarFornecedorCommand>
{
    private readonly IFornecedorRepository _fornecedorRepository;
    private readonly IApplicationDbContext _context;

    public AtualizarFornecedorHandler(IFornecedorRepository fornecedorRepository, IApplicationDbContext context)
    {
        _fornecedorRepository = fornecedorRepository;
        _context = context;
    }

    public async Task Handle(AtualizarFornecedorCommand request, CancellationToken cancellationToken)
    {
        var fornecedor = await _fornecedorRepository.ObterPorIdAsync(request.Id, cancellationToken);

        if (fornecedor is null)
            throw new NaoEncontradoException($"Fornecedor com Id '{request.Id}' não encontrado.");

        fornecedor.Atualizar(request.Nome, request.Telefone, Email.CriarOuNulo(request.Email));

        _fornecedorRepository.Atualizar(fornecedor);

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class ObterFornecedorPorIdHandler : IRequestHandler<ObterFornecedorPorIdQuery, FornecedorDetalhesDto?>
{
    private readonly IFornecedorRepository _fornecedorRepository;

    public ObterFornecedorPorIdHandler(IFornecedorRepository fornecedorRepository)
    {
        _fornecedorRepository = fornecedorRepository;
    }

    public async Task<FornecedorDetalhesDto?> Handle(ObterFornecedorPorIdQuery request, CancellationToken cancellationToken)
    {
        var fornecedor = await _fornecedorRepository.ObterPorIdAsync(request.Id, cancellationToken);

        if (fornecedor is null)
            return null;

        return fornecedor.ParaDetalhesDto();
    }
}

public class ListarFornecedoresHandler : IRequestHandler<ListarFornecedoresQuery, ResultadoPaginado<FornecedorItemListaDto>>
{
    private readonly IFornecedorRepository _fornecedorRepository;

    public ListarFornecedoresHandler(IFornecedorRepository fornecedorRepository)
    {
        _fornecedorRepository = fornecedorRepository;
    }

    public async Task<ResultadoPaginado<FornecedorItemListaDto>> Handle(ListarFornecedoresQuery request, CancellationToken cancellationToken)
    {
        var (fornecedores, totalItens) = await _fornecedorRepository.ListarPaginadoAsync(
            request.Pagina,
            request.TamanhoPagina,
            request.Nome,
            request.DirecaoOrdenacao,
            cancellationToken);

        var itens = fornecedores
            .Select(fornecedor => fornecedor.ParaItemListaDto())
            .ToList();

        return new ResultadoPaginado<FornecedorItemListaDto>(itens, request.Pagina, request.TamanhoPagina, totalItens);
    }
}

file static class FornecedorMapeamentos
{
    public static FornecedorDetalhesDto ParaDetalhesDto(this Fornecedor fornecedor) => new(
        fornecedor.Id,
        fornecedor.Nome,
        fornecedor.Telefone,
        fornecedor.Email?.Valor,
        fornecedor.CriadoEm);

    public static FornecedorItemListaDto ParaItemListaDto(this Fornecedor fornecedor) => new(
        fornecedor.Id,
        fornecedor.Nome,
        fornecedor.Telefone,
        fornecedor.Email?.Valor,
        fornecedor.CriadoEm);
}
