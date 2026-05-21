using MongoDB.Driver;
using Oficina.Execucao.Domain.Entities;
using Oficina.Execucao.Domain.Repositories;
using Oficina.Execucao.Infrastructure.Database;

namespace Oficina.Execucao.Infrastructure.Repositories;

public class EventoRepository : IEventoRepository
{
    private readonly MongoContext _ctx;
    public EventoRepository(MongoContext ctx) => _ctx = ctx;

    public Task RegistrarAsync(EventoExecucao evento, CancellationToken ct = default) =>
        _ctx.Eventos.InsertOneAsync(evento, cancellationToken: ct);

    public async Task<IReadOnlyList<EventoExecucao>> HistoricoPorOSAsync(Guid osId, CancellationToken ct = default) =>
        await _ctx.Eventos.Find(e => e.OrdemDeServicoId == osId)
            .SortBy(e => e.OcorreuEm)
            .ToListAsync(ct);
}
