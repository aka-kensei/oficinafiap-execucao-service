using Oficina.Execucao.Domain.Entities;

namespace Oficina.Execucao.Domain.Repositories;

public interface IEventoRepository
{
    Task RegistrarAsync(EventoExecucao evento, CancellationToken ct = default);
    Task<IReadOnlyList<EventoExecucao>> HistoricoPorOSAsync(Guid osId, CancellationToken ct = default);
}
