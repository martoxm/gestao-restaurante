using GestaoRestaurante.Core.Application.Abstractions;
using MediatR;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Email.Application.Abstractions.Services;

namespace GestaoRestaurante.Email.Application.UseCases.Emails;

public class EnviarEmailHandler : IRequestHandler<EnviarEmailCommand, bool>
{
    private readonly IEmailService _emailService;
    private readonly IMensagemProcessadaRepository _mensagensProcessadas;
    private readonly IApplicationDbContext _context;
    private readonly IRelogio _relogio;

    public EnviarEmailHandler(
        IEmailService emailService,
        IMensagemProcessadaRepository mensagensProcessadas,
        IApplicationDbContext context,
        IRelogio relogio)
    {
        _emailService = emailService;
        _mensagensProcessadas = mensagensProcessadas;
        _context = context;
        _relogio = relogio;
    }

    public async Task<bool> Handle(EnviarEmailCommand request, CancellationToken cancellationToken)
    {
        if (await _mensagensProcessadas.ExisteAsync(request.MensagemId, cancellationToken))
            return true;

        var enviado = await _emailService.EnviarAsync(request.Destinatarios, request.Assunto, request.CorpoHtml, cancellationToken);
        if (!enviado)
            return false;

        _mensagensProcessadas.Adicionar(MensagemProcessada.Criar(request.MensagemId, Filas.Email, _relogio.AgoraUtc));
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
