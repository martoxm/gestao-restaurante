using GestaoRestaurante.Core.Domain;
using GestaoRestaurante.Identidade.Domain.Enums;

namespace GestaoRestaurante.Identidade.Domain.Entities;

public class Restaurante : Entity
{
    public string Nome { get; private set; } = string.Empty;

    public DateTime CriadoEm { get; private set; }

    public const int DiasTeste = 14;

    public PlanoRestaurante Plano { get; private set; }

    public DateTime? TesteTerminaEm { get; private set; }

    public bool Ativo { get; private set; }

    public DateTime? PlanoValidoAte { get; private set; }

    public DateTime? AssinadoEm { get; private set; }
    public DateTime? UltimoPagamentoEm { get; private set; }

    public int? UltimoAvisoFimTesteDias { get; private set; }

    private Restaurante()
    {
    }

    public static Restaurante Criar(string nome, DateTime criadoEm)
    {
        ValidarNome(nome);

        return new Restaurante
        {
            Nome = nome.Trim(),
            CriadoEm = criadoEm,
            Plano = PlanoRestaurante.Teste,
            TesteTerminaEm = criadoEm.AddDays(DiasTeste),
            Ativo = true
        };
    }

    public void Atualizar(string nome)
    {
        ValidarNome(nome);

        Nome = nome.Trim();
    }

    public bool EstaLiberado(DateTime agora) =>
        Ativo && (Plano == PlanoRestaurante.Teste
            ? agora < TesteTerminaEm
            : PlanoValidoAte is null || agora < PlanoValidoAte);

    public const int LimiteUsuariosTeste = 3;
    public const int LimiteUsuariosBasico = 5;

    public int? LimiteUsuarios => Plano switch
    {
        PlanoRestaurante.Teste => LimiteUsuariosTeste,
        PlanoRestaurante.Basico => LimiteUsuariosBasico,
        _ => null
    };

    public void GarantirQueCabeMaisUmUsuario(int vagasOcupadas)
    {
        if (LimiteUsuarios is int limite && vagasOcupadas >= limite)
            throw new DomainException(
                $"O plano {Plano} permite até {limite} usuários, contando os convites pendentes. " +
                "Cancele um convite pendente ou mude de plano.");
    }


    public const int MaximoDiasExtensao = 90;

    public void MudarPlano(PlanoRestaurante novoPlano, DateTime agora)
    {
        if (novoPlano == Plano)
            throw new DomainException($"O restaurante já está no plano {Plano}.");

        Plano = novoPlano;
        TesteTerminaEm = novoPlano == PlanoRestaurante.Teste ? agora.AddDays(DiasTeste) : null;
        PlanoValidoAte = null;
        UltimoAvisoFimTesteDias = null;
    }

    public void EstenderTeste(int dias, DateTime agora)
    {
        if (Plano != PlanoRestaurante.Teste)
            throw new DomainException("Só dá pra estender o teste de restaurante que está no plano Teste.");

        if (dias < 1 || dias > MaximoDiasExtensao)
            throw new DomainException($"A extensão tem que ser de 1 a {MaximoDiasExtensao} dias.");

        var base_ = TesteTerminaEm is DateTime fim && fim > agora ? fim : agora;
        TesteTerminaEm = base_.AddDays(dias);
        UltimoAvisoFimTesteDias = null;
    }

    public const int DiasAvisoVencimento = 3;

    public string ObterSituacao(DateTime agora) => this switch
    {
        { Ativo: false } => "Inativo",
        { Plano: not PlanoRestaurante.Teste } when PlanoValidoAte <= agora => "PlanoVencido",
        { Plano: not PlanoRestaurante.Teste } => "Pago",
        _ when TesteTerminaEm <= agora => "TesteVencido",
        _ when TesteTerminaEm <= agora.AddDays(DiasAvisoVencimento) => "VencendoEmBreve",
        _ => "EmTeste"
    };

    public void DefinirAtivo(bool ativo)
    {
        if (Ativo == ativo)
            throw new DomainException(ativo ? "O restaurante já está ativo." : "O restaurante já está desativado.");

        Ativo = ativo;
    }


    public static readonly int[] DiasAvisoFimTeste = [3, 1];

    public int? DiasParaAvisarFimDoTeste(DateTime agora)
    {
        if (!Ativo || Plano != PlanoRestaurante.Teste || TesteTerminaEm is not DateTime fim || fim <= agora)
            return null;

        var diasRestantes = (int)Math.Ceiling((fim - agora).TotalDays);

        var marcos = DiasAvisoFimTeste.Where(marco => diasRestantes <= marco).ToList();
        if (marcos.Count == 0)
            return null;

        var marco = marcos.Min();

        if (UltimoAvisoFimTesteDias is int ultimo && ultimo <= marco)
            return null;

        return marco;
    }

    public void RegistrarAvisoFimDoTeste(int dias)
    {
        if (!DiasAvisoFimTeste.Contains(dias))
            throw new DomainException($"Aviso de fim do teste inválido: {dias} dias.");

        UltimoAvisoFimTesteDias = dias;
    }


    public void AplicarPagamentoDoPlano(PlanoRestaurante plano, int dias, DateTime agora)
    {
        if (plano == PlanoRestaurante.Teste)
            throw new DomainException("O plano Teste não é pago.");

        if (dias < 1)
            throw new DomainException("O pagamento precisa valer pelo menos 1 dia.");

        var inicio = Plano != PlanoRestaurante.Teste && PlanoValidoAte is DateTime fim && fim > agora ? fim : agora;

        Plano = plano;
        TesteTerminaEm = null;
        PlanoValidoAte = inicio.AddDays(dias);
        UltimoAvisoFimTesteDias = null;
        AssinadoEm ??= agora;
        UltimoPagamentoEm = agora;
    }

    private static void ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome do restaurante é obrigatório.");

        if (nome.Trim().Length > 150)
            throw new DomainException("O nome do restaurante deve ter no máximo 150 caracteres.");
    }
}
