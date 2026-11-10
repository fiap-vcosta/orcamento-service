using Domain.Entities;

namespace Application.Abstractions.Services;

public sealed record CriarOrderPixRequest(
    string IdempotencyKey,
    int OrdemServicoId,
    decimal ValorTotal,
    ClienteOrcamento Cliente);
