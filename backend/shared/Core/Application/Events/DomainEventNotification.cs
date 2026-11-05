using MediatR;
using GestaoRestaurante.Core.Domain;

namespace GestaoRestaurante.Core.Application.Events;

public sealed record DomainEventNotification<TEvento>(TEvento Evento) : INotification
    where TEvento : IDomainEvent;
