using Oficina.Execucao.Domain.Enums;

namespace Oficina.Execucao.Domain.Entities;

/// <summary>
/// Documento Mongo do histórico (event sourcing simplificado).
/// Uma collection `eventos_execucao` com múltiplos eventos por OS — schema flexível
/// (`Payload` é um BsonDocument arbitrário com detalhes da etapa).
/// </summary>
public class EventoExecucao
{
    public Guid Id { get; set; }
    public Guid CorrelationId { get; set; }
    public Guid OrdemDeServicoId { get; set; }
    public TipoEvento Tipo { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? MecanicoResponsavel { get; set; }

    /// <summary>Payload livre — JSON serializado (será BsonDocument no MongoDB).</summary>
    public Dictionary<string, object?> Payload { get; set; } = new();

    public DateTime OcorreuEm { get; set; }

    public EventoExecucao()
    {
        Id = Guid.NewGuid();
        OcorreuEm = DateTime.UtcNow;
    }

    public EventoExecucao(Guid correlationId, Guid osId, TipoEvento tipo, string descricao, string? mecanico = null, Dictionary<string, object?>? payload = null) : this()
    {
        CorrelationId = correlationId;
        OrdemDeServicoId = osId;
        Tipo = tipo;
        Descricao = descricao;
        MecanicoResponsavel = mecanico;
        Payload = payload ?? new();
    }
}
