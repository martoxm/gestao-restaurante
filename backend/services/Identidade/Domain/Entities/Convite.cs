using GestaoRestaurante.Core.Domain;
using GestaoRestaurante.Core.Domain.ValueObjects;
using GestaoRestaurante.Core.Seguranca;

namespace GestaoRestaurante.Identidade.Domain.Entities;

public class Convite : Entity
{
    public const int DiasValidade = 7;

    public Guid RestauranteId { get; private set; }

    public string NomeRestaurante { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string Role { get; private set; } = string.Empty;

    public string TokenHash { get; private set; } = string.Empty;

    public DateTime CriadoEm { get; private set; }
    public DateTime ExpiraEm { get; private set; }
    public DateTime? AceitoEm { get; private set; }
    public DateTime? CanceladoEm { get; private set; }

    private Convite()
    {
    }

    public static Convite Criar(Guid restauranteId, string nomeRestaurante, string email, string role, string tokenHash, DateTime criadoEm)
    {
        if (restauranteId == Guid.Empty)
            throw new DomainException("O convite precisa de um restaurante.");

        if (role is not (RoleNames.Admin or RoleNames.Funcionario))
            throw new DomainException($"A role tem que ser '{RoleNames.Admin}' ou '{RoleNames.Funcionario}'.");

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("O convite precisa do hash do token.");

        return new Convite
        {
            RestauranteId = restauranteId,
            NomeRestaurante = nomeRestaurante,
            Email = Core.Domain.ValueObjects.Email.Criar(email).Valor,
            Role = role,
            TokenHash = tokenHash,
            CriadoEm = criadoEm,
            ExpiraEm = criadoEm.AddDays(DiasValidade)
        };
    }

    public bool EstaPendente(DateTime agora) =>
        AceitoEm is null && CanceladoEm is null && agora < ExpiraEm;

    public string ObterStatus(DateTime agora) => this switch
    {
        { AceitoEm: not null } => "Aceito",
        { CanceladoEm: not null } => "Cancelado",
        _ when agora >= ExpiraEm => "Expirado",
        _ => "Pendente"
    };

    public void Aceitar(DateTime agora)
    {
        if (!EstaPendente(agora))
            throw new DomainException("Este convite não é mais válido. Peça um novo convite ao administrador do restaurante.");

        AceitoEm = agora;
    }

    public void Cancelar(DateTime agora)
    {
        if (AceitoEm is not null)
            throw new DomainException("Este convite já foi aceito e não pode mais ser cancelado.");

        CanceladoEm ??= agora;
    }
}
