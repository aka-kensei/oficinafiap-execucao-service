using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Execucao.Application.Consumers;

namespace Oficina.Execucao.Infrastructure.Messaging;

public static class MassTransitSetup
{
    /// <summary>
    /// Execução Service não usa outbox persistido (MongoDB não suporta o padrão Outbox do MassTransit
    /// nativamente — ele exige RDBMS para atomicidade). Usamos InMemoryOutbox + retry policies do RabbitMQ.
    /// </summary>
    public static IServiceCollection AddExecucaoMassTransit(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            x.AddConsumer<OSCriadaConsumer>();
            x.AddConsumer<PagamentoAprovadoConsumer>();
            x.AddConsumer<OSReprovadaPeloClienteConsumer>();
            x.AddConsumer<OSCanceladaConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(configuration["RabbitMq:Host"] ?? "rabbitmq.messaging-ns.svc.cluster.local", h =>
                {
                    h.Username(configuration["RabbitMq:Username"] ?? "oficina");
                    h.Password(configuration["RabbitMq:Password"] ?? string.Empty);
                });

                cfg.UseInMemoryOutbox(ctx);
                cfg.UseMessageRetry(r => r.Intervals(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15)));

                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
