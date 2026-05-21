using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using Oficina.Execucao.Domain.Entities;

namespace Oficina.Execucao.Infrastructure.Database;

/// <summary>
/// Wrapper sobre IMongoDatabase para expor as collections do Execução Service.
/// Não há "DbContext" em MongoDB — é só um agrupador conveniente de IMongoCollection.
/// </summary>
public class MongoContext
{
    public IMongoDatabase Database { get; }
    public IMongoCollection<ExecucaoOS> Execucoes { get; }
    public IMongoCollection<EventoExecucao> Eventos { get; }

    public MongoContext(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ExecucaoDb")
            ?? throw new InvalidOperationException("ConnectionString 'ExecucaoDb' não configurada.");
        var databaseName = configuration["MongoDb:Database"] ?? "execucao_db";

        var settings = MongoClientSettings.FromConnectionString(connectionString);
        // Forca todas as Guids a serem persistidas como BSON UUID Standard (subtype 4).
        // Sem isso, o driver usa CSharpLegacy (subtype 3) por default em alguns paths e
        // gera mismatch entre o _id (mapeado via ClassMap com Standard) e as demais Guids,
        // resultando em "After applying the update, the (immutable) field '_id' was found to have been altered".
#pragma warning disable CS0618
        settings.GuidRepresentation = MongoDB.Bson.GuidRepresentation.Standard;
#pragma warning restore CS0618
        var client = new MongoClient(settings);
        Database = client.GetDatabase(databaseName);
        Execucoes = Database.GetCollection<ExecucaoOS>("execucoes");
        Eventos = Database.GetCollection<EventoExecucao>("eventos_execucao");
    }

    /// <summary>Cria os índices necessários (chamado uma vez no startup).</summary>
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await Execucoes.Indexes.CreateOneAsync(
            new CreateIndexModel<ExecucaoOS>(
                Builders<ExecucaoOS>.IndexKeys.Ascending(e => e.OrdemDeServicoId),
                new CreateIndexOptions { Unique = true, Name = "ix_execucoes_osid" }),
            cancellationToken: ct);

        await Execucoes.Indexes.CreateOneAsync(
            new CreateIndexModel<ExecucaoOS>(
                Builders<ExecucaoOS>.IndexKeys.Ascending(e => e.Status),
                new CreateIndexOptions { Name = "ix_execucoes_status" }),
            cancellationToken: ct);

        await Execucoes.Indexes.CreateOneAsync(
            new CreateIndexModel<ExecucaoOS>(
                Builders<ExecucaoOS>.IndexKeys.Ascending(e => e.CorrelationId),
                new CreateIndexOptions { Unique = true, Name = "ix_execucoes_correlation" }),
            cancellationToken: ct);

        await Eventos.Indexes.CreateOneAsync(
            new CreateIndexModel<EventoExecucao>(
                Builders<EventoExecucao>.IndexKeys.Ascending(e => e.OrdemDeServicoId).Ascending(e => e.OcorreuEm),
                new CreateIndexOptions { Name = "ix_eventos_os_tempo" }),
            cancellationToken: ct);
    }
}
