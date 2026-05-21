namespace Oficina.Execucao.Domain.Enums;

public enum StatusExecucao
{
    NaFila,
    AguardandoDiagnostico,
    EmDiagnostico,
    AguardandoOrcamento,
    AguardandoPagamento,
    EmExecucao,
    Finalizada,
    Removida
}
