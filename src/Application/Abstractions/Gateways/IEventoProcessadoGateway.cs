using Domain.Entities;

namespace Application.Abstractions.Gateways;

public interface IEventoProcessadoGateway
{
    Task<bool> ExisteEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task<bool> ExisteChaveNegocioAsync(string tipo, string chaveNegocio, CancellationToken cancellationToken = default);

    Task AddAsync(EventoProcessado evento, CancellationToken cancellationToken = default);

    Task<bool> TentarRegistrarAsync(EventoProcessado evento, CancellationToken cancellationToken = default);
}
