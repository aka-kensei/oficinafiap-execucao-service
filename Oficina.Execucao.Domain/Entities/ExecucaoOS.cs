using Oficina.Execucao.Domain.Enums;
using Oficina.Execucao.Domain.Exceptions;

namespace Oficina.Execucao.Domain.Entities;

/// <summary>
/// Documento Mongo da fila de execução. Uma OS = um documento na collection `execucoes`.
/// Lifecycle: NaFila → EmDiagnostico → AguardandoOrcamento → AguardandoPagamento → EmExecucao → Finalizada
/// </summary>
public class ExecucaoOS
{
    public Guid Id { get; set; }
    public Guid CorrelationId { get; set; }
    public Guid OrdemDeServicoId { get; set; }
    public Guid ClienteId { get; set; }
    public Guid VeiculoId { get; set; }

    public DadosSnapshot Dados { get; set; } = null!;

    public StatusExecucao Status { get; set; }

    public string? MecanicoResponsavel { get; set; }

    public DateTime EntrouNaFilaEm { get; set; }
    public DateTime? DiagnosticoIniciadoEm { get; set; }
    public DateTime? OrcamentoPropostoEm { get; set; }
    public DateTime? ExecucaoIniciadaEm { get; set; }
    public DateTime? FinalizadaEm { get; set; }

    public decimal? ValorOrcamentoProposto { get; set; }
    public List<ItemPropostoSnapshot> ItensPropostos { get; set; } = new();

    public DateTime AtualizadaEm { get; set; }

    public ExecucaoOS()
    {
        Id = Guid.NewGuid();
        EntrouNaFilaEm = DateTime.UtcNow;
        AtualizadaEm = DateTime.UtcNow;
        Status = StatusExecucao.NaFila;
    }

    public void IniciarDiagnostico(string? mecanico)
    {
        if (Status is not (StatusExecucao.NaFila or StatusExecucao.AguardandoDiagnostico))
            throw new DomainException($"Não é possível iniciar diagnóstico no estado {Status}.");

        Status = StatusExecucao.EmDiagnostico;
        MecanicoResponsavel = mecanico;
        DiagnosticoIniciadoEm = DateTime.UtcNow;
        AtualizadaEm = DateTime.UtcNow;
    }

    public void ProporOrcamento(decimal valorTotal, IReadOnlyList<ItemPropostoSnapshot> itens)
    {
        if (Status != StatusExecucao.EmDiagnostico)
            throw new DomainException("Diagnóstico precisa estar em andamento para propor orçamento.");
        if (valorTotal <= 0)
            throw new DomainException("Valor do orçamento deve ser positivo.");

        ValorOrcamentoProposto = valorTotal;
        ItensPropostos = itens.ToList();
        OrcamentoPropostoEm = DateTime.UtcNow;
        Status = StatusExecucao.AguardandoOrcamento;
        AtualizadaEm = DateTime.UtcNow;
    }

    public void MarcarAguardandoPagamento()
    {
        Status = StatusExecucao.AguardandoPagamento;
        AtualizadaEm = DateTime.UtcNow;
    }

    public void LiberarParaExecucao()
    {
        if (Status != StatusExecucao.AguardandoPagamento)
            throw new DomainException($"Execução só pode ser liberada após pagamento (estado atual: {Status}).");
        AtualizadaEm = DateTime.UtcNow;
    }

    public void IniciarExecucao(string? mecanico)
    {
        if (Status != StatusExecucao.AguardandoPagamento)
            throw new DomainException($"Não é possível iniciar execução no estado {Status}.");

        Status = StatusExecucao.EmExecucao;
        MecanicoResponsavel = mecanico ?? MecanicoResponsavel;
        ExecucaoIniciadaEm = DateTime.UtcNow;
        AtualizadaEm = DateTime.UtcNow;
    }

    public void Finalizar()
    {
        if (Status != StatusExecucao.EmExecucao)
            throw new DomainException($"Só é possível finalizar uma execução em andamento (estado: {Status}).");

        Status = StatusExecucao.Finalizada;
        FinalizadaEm = DateTime.UtcNow;
        AtualizadaEm = DateTime.UtcNow;
    }

    public void Remover()
    {
        Status = StatusExecucao.Removida;
        AtualizadaEm = DateTime.UtcNow;
    }

    public decimal? TempoTotalExecucaoHoras
        => ExecucaoIniciadaEm.HasValue && FinalizadaEm.HasValue
            ? (decimal)(FinalizadaEm.Value - ExecucaoIniciadaEm.Value).TotalHours
            : null;
}
