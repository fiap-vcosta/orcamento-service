using Application.Abstractions.Gateways;
using Domain.Entities;

namespace UnitTests.Fakes;

public sealed class InMemoryOrcamentoGateway : IOrcamentoGateway
{
    public Dictionary<string, Orcamento> Items { get; } = new();

    public Exception? SalvarException { get; set; }

    public Task<Orcamento?> GetByIdAsync(string orcamentoId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.GetValueOrDefault(orcamentoId));

    public Task SalvarAsync(Orcamento orcamento, CancellationToken cancellationToken = default)
    {
        if (SalvarException is not null)
        {
            return Task.FromException(SalvarException);
        }

        Items[orcamento.Id] = orcamento;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryOrdemOrcamentoGateway : IOrdemOrcamentoGateway
{
    public Dictionary<Guid, OrdemOrcamento> Items { get; } = new();

    public Task<OrdemOrcamento?> GetByOrdemServicoIdAsync(
        Guid ordemServicoId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.GetValueOrDefault(ordemServicoId));

    public Task SalvarAsync(OrdemOrcamento ordem, CancellationToken cancellationToken = default)
    {
        Items[ordem.OrdemServicoId] = ordem;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryCobrancaGateway : ICobrancaGateway
{
    public Dictionary<string, Cobranca> Items { get; } = new();

    public Task<Cobranca?> GetByIdAsync(string cobrancaId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.GetValueOrDefault(cobrancaId));

    public Task<IReadOnlyList<Cobranca>> ListarPorOrdemServicoAsync(
        Guid ordemServicoId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Cobranca>>(Items.Values.Where(c => c.OrdemServicoId == ordemServicoId).ToList());

    public Task SalvarAsync(Cobranca cobranca, CancellationToken cancellationToken = default)
    {
        Items[cobranca.Id] = cobranca;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryEventoProcessadoGateway : IEventoProcessadoGateway
{
    public List<EventoProcessado> Items { get; } = [];

    public Task<bool> ExisteEventIdAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(e => e.EventId == eventId));

    public Task<bool> ExisteChaveNegocioAsync(
        string tipo,
        string chaveNegocio,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(e => e.Tipo == tipo && e.ChaveNegocio == chaveNegocio));

    public Task AddAsync(EventoProcessado evento, CancellationToken cancellationToken = default)
    {
        Items.Add(evento);
        return Task.CompletedTask;
    }

    public Task<bool> TentarRegistrarAsync(EventoProcessado evento, CancellationToken cancellationToken = default)
    {
        if (Items.Any(e => e.Tipo == evento.Tipo && e.ChaveNegocio == evento.ChaveNegocio))
        {
            return Task.FromResult(false);
        }

        Items.Add(evento);
        return Task.FromResult(true);
    }
}
