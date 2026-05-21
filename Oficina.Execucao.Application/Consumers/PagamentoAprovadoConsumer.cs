using MassTransit;
using Microsoft.Extensions.Logging;
using Oficina.Contracts.Events;
using Oficina.Execucao.Domain.Entities;
using Oficina.Execucao.Domain.Enums;
using Oficina.Execucao.Domain.Repositories;

namespace Oficina.Execucao.Application.Consumers;

/// <summary>
/// Pagamento aprovado pelo Billing — libera a execução para o mecânico iniciar.
/// O ExecucaoIniciada não é publicado automaticamente; é disparado quando o mecânico
/// chama PATCH /api/execucao/{osId}/iniciar-execucao.
/// </summary>
public class PagamentoAprovadoConsumer : IConsumer<PagamentoAprovado>
{
    private readonly IExecucaoRepository _execucaoRepo;
    private readonly IEventoRepository _eventoRepo;
    private readonly ILogger<PagamentoAprovadoConsumer> _logger;

    public PagamentoAprovadoConsumer(IExecucaoRepository execucaoRepo, IEventoRepository eventoRepo, ILogger<PagamentoAprovadoConsumer> logger)
    {
        _execucaoRepo = execucaoRepo;
        _eventoRepo = eventoRepo;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PagamentoAprovado> context)
    {
        var msg = context.Message;
        var execucao = await _execucaoRepo.ObterPorOSAsync(msg.OrdemDeServicoId, context.CancellationToken);
        if (execucao == null)
        {
            _logger.LogWarning("Execução não encontrada para OS {OSId} — pagamento aprovado ignorado.", msg.OrdemDeServicoId);
            return;
        }

        if (execucao.Status != StatusExecucao.AguardandoOrcamento)
        {
            execucao.MarcarAguardandoPagamento();
        }

        execucao.Status = StatusExecucao.AguardandoPagamento;
        await _execucaoRepo.SalvarAsync(execucao, context.CancellationToken);

        await _eventoRepo.RegistrarAsync(new EventoExecucao(
            msg.CorrelationId, msg.OrdemDeServicoId, TipoEvento.LiberadaParaExecucao,
            $"Pagamento aprovado (R$ {msg.ValorPago:F2}) — liberada para execução",
            payload: new Dictionary<string, object?>
            {
                ["pagamentoId"] = msg.PagamentoId,
                ["mpPaymentId"] = msg.MercadoPagoPaymentId,
                ["valorPago"] = msg.ValorPago
            }), context.CancellationToken);
    }
}
