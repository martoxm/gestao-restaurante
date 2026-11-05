using GestaoRestaurante.Identidade.Domain.Entities;

namespace GestaoRestaurante.Identidade.Application.Abstractions.Repositories;

public interface IAuditoriaPlataformaRepository
{
    void Adicionar(AuditoriaPlataforma auditoria);
}
