using Oficina.Execucao.Domain.Enums;

namespace Oficina.Execucao.Application.DTOs;

public record FilaItemViewModel(
    Guid Id,
    Guid OrdemDeServicoId,
    string ClienteNome,
    string VeiculoPlaca,
    string VeiculoMarca,
    string VeiculoModelo,
    StatusExecucao Status,
    string? MecanicoResponsavel,
    DateTime EntrouNaFilaEm,
    DateTime AtualizadaEm);

public record IniciarDiagnosticoInput(string? MecanicoResponsavel);

public record ItemOrcamentoInput(string Tipo, Guid ItemId, string Descricao, int Quantidade, decimal PrecoUnitario);

public record ProporOrcamentoInput(decimal ValorTotal, IReadOnlyList<ItemOrcamentoInput> Itens);

public record IniciarExecucaoInput(string? MecanicoResponsavel);

public record EventoHistoricoViewModel(
    Guid Id,
    string Tipo,
    string Descricao,
    string? MecanicoResponsavel,
    Dictionary<string, object?> Payload,
    DateTime OcorreuEm);
