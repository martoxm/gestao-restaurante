using GestaoRestaurante.Core.Domain;
using GestaoRestaurante.Identidade.Domain.Enums;

namespace GestaoRestaurante.Identidade.Domain.Entities;

public class NotificacaoPlataforma : Entity
{
    public const string TipoNovoCadastro = "NovoCadastro";
    public const string TipoPlanoAssinado = "PlanoAssinado";
    public const string TipoPlanoRenovado = "PlanoRenovado";
    public const string TipoPlanoTrocado = "PlanoTrocado";
    public const string TipoTesteVencido = "TesteVencido";
    public const string TipoPlanoVencido = "PlanoVencido";

    public string Tipo { get; private set; } = string.Empty;
    public string Titulo { get; private set; } = string.Empty;
    public string Mensagem { get; private set; } = string.Empty;
    public Guid? RestauranteId { get; private set; }
    public string? Chave { get; private set; }
    public DateTime OcorridoEm { get; private set; }
    public DateTime? LidaEm { get; private set; }

    private NotificacaoPlataforma()
    {
    }

    public static NotificacaoPlataforma Criar(string tipo, string titulo, string mensagem, DateTime ocorridoEm, Guid? restauranteId = null, string? chave = null)
    {
        if (string.IsNullOrWhiteSpace(tipo))
            throw new DomainException("O tipo da notificação é obrigatório.");

        if (string.IsNullOrWhiteSpace(titulo))
            throw new DomainException("O título da notificação é obrigatório.");

        return new NotificacaoPlataforma
        {
            Tipo = tipo,
            Titulo = titulo.Trim(),
            Mensagem = mensagem?.Trim() ?? string.Empty,
            RestauranteId = restauranteId,
            Chave = chave,
            OcorridoEm = ocorridoEm
        };
    }


    public static NotificacaoPlataforma NovoCadastro(Restaurante restaurante, DateTime agora) =>
        Criar(TipoNovoCadastro,
            "Novo cliente cadastrado",
            $"{restaurante.Nome} criou a conta e começou o teste grátis de {Restaurante.DiasTeste} dias.",
            agora, restaurante.Id);

    public static NotificacaoPlataforma PlanoPago(Restaurante restaurante, PlanoRestaurante planoAntes, bool jaTinhaAssinado, int dias, DateTime agora)
    {
        var validoAte = restaurante.PlanoValidoAte?.ToString("dd/MM/yyyy") ?? "-";

        if (!jaTinhaAssinado)
            return Criar(TipoPlanoAssinado, "Cliente assinou um plano",
                $"{restaurante.Nome} assinou o plano {restaurante.Plano} ({dias} dias, válido até {validoAte}).",
                agora, restaurante.Id);

        if (planoAntes != PlanoRestaurante.Teste && planoAntes != restaurante.Plano)
            return Criar(TipoPlanoTrocado, "Cliente trocou de plano",
                $"{restaurante.Nome} passou do plano {planoAntes} para o {restaurante.Plano} (válido até {validoAte}).",
                agora, restaurante.Id);

        return Criar(TipoPlanoRenovado, "Cliente renovou o plano",
            $"{restaurante.Nome} renovou o plano {restaurante.Plano} por mais {dias} dias (válido até {validoAte}).",
            agora, restaurante.Id);
    }

    public static NotificacaoPlataforma? Vencimento(Restaurante restaurante, DateTime agora)
    {
        if (!restaurante.Ativo)
            return null;

        if (restaurante.Plano == PlanoRestaurante.Teste && restaurante.TesteTerminaEm is DateTime fimTeste && fimTeste <= agora)
            return Criar(TipoTesteVencido, "Teste venceu sem assinatura",
                $"O teste grátis de {restaurante.Nome} acabou em {fimTeste:dd/MM/yyyy} e o cliente não assinou.",
                agora, restaurante.Id, ChaveVencimento(TipoTesteVencido, restaurante.Id, fimTeste));

        if (restaurante.Plano != PlanoRestaurante.Teste && restaurante.PlanoValidoAte is DateTime fimPlano && fimPlano <= agora)
            return Criar(TipoPlanoVencido, "Plano venceu sem renovação",
                $"O plano {restaurante.Plano} de {restaurante.Nome} venceu em {fimPlano:dd/MM/yyyy} e não foi renovado.",
                agora, restaurante.Id, ChaveVencimento(TipoPlanoVencido, restaurante.Id, fimPlano));

        return null;
    }

    public static string ChaveVencimento(string tipo, Guid restauranteId, DateTime vencimento) =>
        $"{tipo}:{restauranteId:N}:{vencimento:yyyyMMddHHmmss}";

    public void MarcarComoLida(DateTime agora) => LidaEm ??= agora;
}
