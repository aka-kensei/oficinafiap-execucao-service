using Oficina.Execucao.Domain.Entities;
using Oficina.Execucao.Domain.Enums;

namespace Oficina.Execucao.Domain.Repositories;

public interface IExecucaoRepository
{
    Task AdicionarAsync(ExecucaoOS execucao, CancellationToken ct = default);
    Task<ExecucaoOS?> ObterPorOSAsync(Guid osId, CancellationToken ct = default);
    Task<ExecucaoOS?> ObterPorCorrelationIdAsync(Guid correlationId, CancellationToken ct = default);
    Task<IReadOnlyList<ExecucaoOS>> ListarFilaAsync(StatusExecucao? status = null, CancellationToken ct = default);
    Task SalvarAsync(ExecucaoOS execucao, CancellationToken ct = default);
}
