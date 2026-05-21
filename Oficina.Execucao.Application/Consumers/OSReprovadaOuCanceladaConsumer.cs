using MassTransit;
using Microsoft.Extensions.Logging;
using Oficina.Contracts.Events;
using Oficina.Execucao.Domain.Entities;
using Oficina.Execucao.Domain.Enums;
using Oficina.Execucao.Domain.Repositories;

namespace Oficina.Execucao.Application.Consumers;

/// <summary>
/// Remove a OS da fila quando reprovada pelo cliente ou cancelada por timeout.
/// </summary>
public class OSReprovadaPeloClienteConsumer : IConsumer<OSReprovadaPeloCliente>
{
    private readonly IExecucaoRepository _execucaoRepo;
    private readonly IEventoRepository _eventoRepo;
    private readonly ILogger<OSReprovadaPeloClienteConsumer> _logger;

    public OSReprovadaPeloClienteConsumer(IExecucaoRepository execucaoRepo, IEventoRepository eventoRepo, ILogger<OSReprovadaPeloClienteConsumer> logger)
    {
        _execucaoRepo = execucaoRepo;
        _eventoRepo = eventoRepo;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OSReprovadaPeloCliente> context)
    {
        var msg = context.Message;
        var execucao = await _execucaoRepo.ObterPorOSAsync(msg.OrdemDeServicoId, context.CancellationToken);
        if (execucao == null) return;

        execucao.Remover();
        await _execucaoRepo.SalvarAsync(execucao, context.CancellationToken);
        await _eventoRepo.RegistrarAsync(new EventoExecucao(
            msg.CorrelationId, msg.OrdemDeServicoId, TipoEvento.Removida,
            $"Removida da fila (cliente reprovou): {msg.Motivo ?? "sem motivo"}"), context.CancellationToken);

        _logger.LogInformation("Execução da OS {OSId} removida (reprovada).", msg.OrdemDeServicoId);
    }
}

public class OSCanceladaConsumer : IConsumer<OSCancelada>
{
    private readonly IExecucaoRepository _execucaoRepo;
    private readonly IEventoRepository _eventoRepo;

    public OSCanceladaConsumer(IExecucaoRepository execucaoRepo, IEventoRepository eventoRepo)
    {
        _execucaoRepo = execucaoRepo;
        _eventoRepo = eventoRepo;
    }

    public async Task Consume(ConsumeContext<OSCancelada> context)
    {
        var msg = context.Message;
        var execucao = await _execucaoRepo.ObterPorOSAsync(msg.OrdemDeServicoId, context.CancellationToken);
        if (execucao == null) return;

        execucao.Remover();
        await _execucaoRepo.SalvarAsync(execucao, context.CancellationToken);
        await _eventoRepo.RegistrarAsync(new EventoExecucao(
            msg.CorrelationId, msg.OrdemDeServicoId, TipoEvento.Removida,
            $"Cancelada: {msg.Motivo}"), context.CancellationToken);
    }
}
