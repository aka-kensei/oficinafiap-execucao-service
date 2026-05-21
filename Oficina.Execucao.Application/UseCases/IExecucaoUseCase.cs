using Oficina.Execucao.Application.DTOs;
using Oficina.Execucao.Domain.Enums;

namespace Oficina.Execucao.Application.UseCases;

public interface IExecucaoUseCase
{
    Task<IReadOnlyList<FilaItemViewModel>> ListarFilaAsync(StatusExecucao? status, CancellationToken ct = default);
    Task IniciarDiagnosticoAsync(Guid osId, IniciarDiagnosticoInput input, CancellationToken ct = default);
    Task ProporOrcamentoAsync(Guid osId, ProporOrcamentoInput input, CancellationToken ct = default);
    Task IniciarExecucaoAsync(Guid osId, IniciarExecucaoInput input, CancellationToken ct = default);
    Task FinalizarExecucaoAsync(Guid osId, CancellationToken ct = default);
    Task<IReadOnlyList<EventoHistoricoViewModel>> ObterHistoricoAsync(Guid osId, CancellationToken ct = default);
}
