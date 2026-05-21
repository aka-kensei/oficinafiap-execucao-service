using MassTransit;
using Microsoft.Extensions.Logging;
using Oficina.Contracts.Events;
using Oficina.Execucao.Domain.Entities;
using Oficina.Execucao.Domain.Enums;
using Oficina.Execucao.Domain.Repositories;

namespace Oficina.Execucao.Application.Consumers;

/// <summary>
/// Reage à OSCriada publicada pelo OS Service: cria documento na fila com snapshot do cliente/veículo.
/// Idempotente: se já existir Execução para o mesmo OrdemDeServicoId, ignora.
/// </summary>
public class OSCriadaConsumer : IConsumer<OSCriada>
{
    private readonly IExecucaoRepository _execucaoRepo;
    private readonly IEventoRepository _eventoRepo;
    private readonly ILogger<OSCriadaConsumer> _logger;

    public OSCriadaConsumer(IExecucaoRepository execucaoRepo, IEventoRepository eventoRepo, ILogger<OSCriadaConsumer> logger)
    {
        _execucaoRepo = execucaoRepo;
        _eventoRepo = eventoRepo;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OSCriada> context)
    {
        var msg = context.Message;

        var existente = await _execucaoRepo.ObterPorOSAsync(msg.OrdemDeServicoId, context.CancellationToken);
        if (existente != null)
        {
            _logger.LogInformation("Execução já existente para OS {OSId} — ignorando OSCriada.", msg.OrdemDeServicoId);
            return;
        }

        var execucao = new ExecucaoOS
        {
            CorrelationId = msg.CorrelationId,
            OrdemDeServicoId = msg.OrdemDeServicoId,
            ClienteId = msg.ClienteId,
            VeiculoId = msg.VeiculoId,
            Dados = new DadosSnapshot(
                msg.ClienteNome, msg.ClienteCpf, msg.ClienteEmail, msg.ClienteTelefone,
                msg.VeiculoPlaca, msg.VeiculoMarca, msg.VeiculoModelo, msg.VeiculoAno),
            Status = StatusExecucao.AguardandoDiagnostico
        };

        await _execucaoRepo.AdicionarAsync(execucao, context.CancellationToken);
        await _eventoRepo.RegistrarAsync(new EventoExecucao(
            msg.CorrelationId, msg.OrdemDeServicoId, TipoEvento.EntrouNaFila,
            $"OS entrou na fila — veículo {msg.VeiculoPlaca}",
            payload: new Dictionary<string, object?>
            {
                ["clienteNome"] = msg.ClienteNome,
                ["veiculoPlaca"] = msg.VeiculoPlaca
            }), context.CancellationToken);

        _logger.LogInformation("OS {OSId} adicionada à fila de execução.", msg.OrdemDeServicoId);
    }
}
