using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using Oficina.Execucao.Domain.Entities;
using Oficina.Execucao.Domain.Enums;

namespace Oficina.Execucao.Infrastructure.Database;

/// <summary>
/// Registra convenções e mapeamentos do BSON uma única vez no startup.
/// </summary>
public static class MongoBsonRegistration
{
    private static int _registered;

    public static void RegisterOnce()
    {
        if (Interlocked.Exchange(ref _registered, 1) == 1) return;

        var pack = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new IgnoreExtraElementsConvention(true),
            new EnumRepresentationConvention(BsonType.String)
        };
        ConventionRegistry.Register("OficinaExecucaoConventions", pack, _ => true);

        var stdGuid = new GuidSerializer(GuidRepresentation.Standard);

        BsonClassMap.TryRegisterClassMap<ExecucaoOS>(cm =>
        {
            cm.AutoMap();
            cm.MapIdProperty(e => e.Id).SetSerializer(stdGuid);
            // Forca todos os campos Guid a serem persistidos como UUID Standard (subtype 4)
            // para evitar mismatch com o _id no ReplaceOne (que dispara
            // "the (immutable) field '_id' was found to have been altered").
            cm.MapProperty(e => e.CorrelationId).SetSerializer(stdGuid);
            cm.MapProperty(e => e.OrdemDeServicoId).SetSerializer(stdGuid);
            cm.MapProperty(e => e.ClienteId).SetSerializer(stdGuid);
            cm.MapProperty(e => e.VeiculoId).SetSerializer(stdGuid);
        });

        BsonClassMap.TryRegisterClassMap<EventoExecucao>(cm =>
        {
            cm.AutoMap();
            cm.MapIdProperty(e => e.Id).SetSerializer(stdGuid);
            cm.MapProperty(e => e.CorrelationId).SetSerializer(stdGuid);
            cm.MapProperty(e => e.OrdemDeServicoId).SetSerializer(stdGuid);
        });

        // MongoDB Driver 2.27+ ja registra um GuidSerializer default; nao redefinir aqui
        // (ja temos class maps especificos para todos os campos Guid usados).
    }
}
