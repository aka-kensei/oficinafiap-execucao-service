namespace Oficina.Contracts.Events;

// Os contratos abaixo são cópias dos definidos no OS Service e no Billing Service.
// Compartilham o mesmo namespace `Oficina.Contracts.Events` para que o MassTransit
// roteie cada mensagem pela mesma exchange RabbitMQ entre os 3 micros.

public record ItemOrcamento(
    string Tipo,
    Guid Id,
    string Descricao,
    int Quantidade,
    decimal PrecoUnitario);

// ── Consumidos pelo Execução (vindos do OS Service / Billing Service) ──

public record OSCriada(
    Guid CorrelationId,
    Guid OrdemDeServicoId,
    Guid ClienteId,
    string ClienteNome,
    string ClienteCpf,
    string ClienteEmail,
    string ClienteTelefone,
    Guid VeiculoId,
    string VeiculoPlaca,
    string VeiculoMarca,
    string VeiculoModelo,
    int VeiculoAno,
    DateTime DataCriacao);

public record PagamentoAprovado(
    Guid CorrelationId,
    Guid OrdemDeServicoId,
    Guid OrcamentoId,
    Guid PagamentoId,
    string MercadoPagoPaymentId,
    decimal ValorPago,
    DateTime AprovadoEm);

public record OSReprovadaPeloCliente(
    Guid CorrelationId,
    Guid OrdemDeServicoId,
    string? Motivo,
    DateTime ReprovadaEm);

public record OSCancelada(
    Guid CorrelationId,
    Guid OrdemDeServicoId,
    string Motivo,
    DateTime CanceladaEm);

// ── Publicados pelo Execução ───────────────────────────────────────────

public record DiagnosticoIniciado(
    Guid CorrelationId,
    Guid OrdemDeServicoId,
    string? MecanicoResponsavel,
    DateTime IniciadoEm);

public record OrcamentoPropostoPelaOficina(
    Guid CorrelationId,
    Guid OrdemDeServicoId,
    decimal ValorTotal,
    IReadOnlyList<ItemOrcamento> Itens,
    DateTime PropostoEm);

public record ExecucaoIniciada(
    Guid CorrelationId,
    Guid OrdemDeServicoId,
    string? MecanicoResponsavel,
    DateTime IniciadaEm);

public record ExecucaoFinalizada(
    Guid CorrelationId,
    Guid OrdemDeServicoId,
    decimal? TempoTotalHoras,
    DateTime FinalizadaEm);
