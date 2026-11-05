using GestaoRestaurante.Identidade.Contracts.Convites;

namespace GestaoRestaurante.Identidade.Contracts.Plataforma;



public record ResumoPlataformaDto(
    RestaurantesResumoDto Restaurantes,
    UsuariosResumoDto Usuarios,
    ConvitesResumoDto Convites,
    FunilConversaoDto Funil,
    decimal MrrEstimado,
    IReadOnlyList<CadastrosDiaDto> CadastrosPorDia);

public record RestaurantesResumoDto(
    int Total,
    int Ativos,
    int Inativos,
    int PlanoTeste,
    int PlanoBasico,
    int PlanoPro,
    int EmTeste,
    int TesteVencido,
    int VencendoEmBreve);

public record UsuariosResumoDto(int Total, int Admins, int Funcionarios, int Desativados);

public record ConvitesResumoDto(int Pendentes, int Aceitos);

public record FunilConversaoDto(int Cadastrados, int EmTeste, int Pagantes, int VencidosSemConverter);

public record CadastrosDiaDto(DateTime Dia, int Quantidade);


public record RestaurantePlataformaItemDto(
    Guid Id,
    string Nome,
    string Plano,
    bool Ativo,
    string Situacao,
    DateTime? TesteTerminaEm,
    DateTime CriadoEm,
    string? EmailDono,
    int QuantidadeUsuarios,
    DateTime? PlanoValidoAte,
    DateTime? AssinadoEm,
    DateTime? UltimoPagamentoEm);

public record ListarRestaurantesPlataformaRequest(
    string? Busca = null,
    string? Plano = null,
    string? Status = null,
    int Pagina = 1,
    int TamanhoPagina = 20);

public record UsuarioPlataformaDto(Guid Id, string Nome, string Email, string Role, bool Ativo, bool EmailConfirmado);

public record RestaurantePlataformaDetalhesDto(
    Guid Id,
    string Nome,
    string Plano,
    bool Ativo,
    string Situacao,
    DateTime? TesteTerminaEm,
    DateTime CriadoEm,
    int? LimiteUsuarios,
    DateTime? PlanoValidoAte,
    DateTime? AssinadoEm,
    DateTime? UltimoPagamentoEm,
    IReadOnlyList<UsuarioPlataformaDto> Usuarios,
    IReadOnlyList<ConviteItemListaDto> Convites);


public record AuditoriaItemDto(
    Guid Id,
    DateTime OcorridoEm,
    string EmailSuperAdmin,
    string Acao,
    Guid? RestauranteId,
    string? NomeRestaurante,
    Guid? UsuarioAlvoId,
    string? ValorAntes,
    string? ValorDepois);

public record ListarAuditoriaRequest(Guid? RestauranteId = null, int Pagina = 1, int TamanhoPagina = 20);


public record MudarPlanoRequest(string Plano);

public record EstenderTesteRequest(int Dias);

public record AlterarAtivoRequest(bool Ativo);


public record NotificacaoPlataformaDto(
    Guid Id,
    string Tipo,
    string Titulo,
    string Mensagem,
    Guid? RestauranteId,
    DateTime OcorridoEm,
    bool Lida);

public record ListarNotificacoesRequest(bool SoNaoLidas = false, int Pagina = 1, int TamanhoPagina = 20);

public record NotificacoesNaoLidasDto(int Quantidade);
