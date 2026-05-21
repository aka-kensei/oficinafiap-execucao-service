namespace Oficina.Execucao.Domain.Entities;

public record ItemPropostoSnapshot(
    string Tipo,
    Guid ItemId,
    string Descricao,
    int Quantidade,
    decimal PrecoUnitario);
