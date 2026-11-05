using GestaoRestaurante.Core.Domain;
using GestaoRestaurante.Core.Domain.ValueObjects;
using GestaoRestaurante.Pagamentos.Domain.Enums;

namespace GestaoRestaurante.Pagamentos.Domain.Entities;

public class CobrancaPlano : Entity
{
    public const int DiasPorPagamento = 30;

    public Guid RestauranteId { get; private set; }
    public PlanoPago Plano { get; private set; }
    public Dinheiro Valor { get; private set; } = null!;
    public int Dias { get; private set; }
    public StatusCobrancaPlano Status { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? PagaEm { get; private set; }

    public string? IdSessaoGateway { get; private set; }

    private CobrancaPlano()
    {
    }

    public static CobrancaPlano Criar(Guid restauranteId, PlanoPago plano, Dinheiro valor, DateTime criadoEm)
    {
        if (restauranteId == Guid.Empty)
            throw new DomainException("A cobrança precisa pertencer a um restaurante.");

        if (!Enum.IsDefined(plano))
            throw new DomainException("Plano inválido.");

        if (valor.Valor <= 0)
            throw new DomainException("O valor do plano precisa ser maior que zero.");

        return new CobrancaPlano
        {
            RestauranteId = restauranteId,
            Plano = plano,
            Valor = valor,
            Dias = DiasPorPagamento,
            Status = StatusCobrancaPlano.Pendente,
            CriadoEm = criadoEm
        };
    }

    public bool MarcarComoPaga(DateTime agora)
    {
        if (Status == StatusCobrancaPlano.Paga)
            return false;

        Status = StatusCobrancaPlano.Paga;
        PagaEm = agora;
        return true;
    }

    public void RegistrarSessao(string idSessaoGateway)
    {
        if (string.IsNullOrWhiteSpace(idSessaoGateway))
            throw new DomainException("O gateway não devolveu o Id da sessão.");

        if (IdSessaoGateway is not null)
            throw new DomainException("Essa cobrança já tem uma sessão de pagamento.");

        IdSessaoGateway = idSessaoGateway;
    }
}
