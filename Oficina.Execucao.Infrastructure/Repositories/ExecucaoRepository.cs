using MongoDB.Driver;
using Oficina.Execucao.Domain.Entities;
using Oficina.Execucao.Domain.Enums;
using Oficina.Execucao.Domain.Repositories;
using Oficina.Execucao.Infrastructure.Database;

namespace Oficina.Execucao.Infrastructure.Repositories;

public class ExecucaoRepository : IExecucaoRepository
{
    private readonly MongoContext _ctx;
    public ExecucaoRepository(MongoContext ctx) => _ctx = ctx;

    public Task AdicionarAsync(ExecucaoOS execucao, CancellationToken ct = default) =>
        _ctx.Execucoes.InsertOneAsync(execucao, cancellationToken: ct);

    public Task<ExecucaoOS?> ObterPorOSAsync(Guid osId, CancellationToken ct = default) =>
        _ctx.Execucoes.Find(e => e.OrdemDeServicoId == osId).FirstOrDefaultAsync(ct)!;

    public Task<ExecucaoOS?> ObterPorCorrelationIdAsync(Guid correlationId, CancellationToken ct = default) =>
        _ctx.Execucoes.Find(e => e.CorrelationId == correlationId).FirstOrDefaultAsync(ct)!;

    public async Task<IReadOnlyList<ExecucaoOS>> ListarFilaAsync(StatusExecucao? status = null, CancellationToken ct = default)
    {
        var filter = status.HasValue
            ? Builders<ExecucaoOS>.Filter.Eq(e => e.Status, status.Value)
            : Builders<ExecucaoOS>.Filter.Ne(e => e.Status, StatusExecucao.Removida);

        return await _ctx.Execucoes.Find(filter)
            .SortBy(e => e.Status).ThenBy(e => e.EntrouNaFilaEm)
            .Limit(500)
            .ToListAsync(ct);
    }

    public Task SalvarAsync(ExecucaoOS execucao, CancellationToken ct = default) =>
        _ctx.Execucoes.ReplaceOneAsync(
            e => e.Id == execucao.Id,
            execucao,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken: ct);
}
