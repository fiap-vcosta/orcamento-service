using Domain.Entities;

namespace Application.Abstractions.Services;

public sealed record CriarOrderPixRequest(
    string IdempotencyKey,
    Guid OrdemServicoId,
    decimal ValorTotal,
    ClienteOrcamento Cliente);
