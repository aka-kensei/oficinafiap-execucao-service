using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Oficina.Contracts.Events;
using Oficina.Execucao.Application.Consumers;
using Oficina.Execucao.Domain.Entities;
using Oficina.Execucao.Domain.Repositories;
using Xunit;

namespace Oficina.Execucao.Application.UnitTests.Consumers;

public class OSCriadaConsumerTests
{
    [Fact]
    public async Task Consume_FluxoFeliz_AdicionaNaFilaERegistraEvento()
    {
        var execucaoRepo = new Mock<IExecucaoRepository>();
        execucaoRepo.Setup(r => r.ObterPorOSAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExecucaoOS?)null);

        var eventoRepo = new Mock<IEventoRepository>();

        await using var provider = new ServiceCollection()
            .AddSingleton(execucaoRepo.Object)
            .AddSingleton(eventoRepo.Object)
            .AddSingleton(NullLogger<OSCriadaConsumer>.Instance)
            .AddMassTransitTestHarness(x => x.AddConsumer<OSCriadaConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            var msg = new OSCriada(
                CorrelationId: Guid.NewGuid(),
                OrdemDeServicoId: Guid.NewGuid(),
                ClienteId: Guid.NewGuid(),
                ClienteNome: "João",
                ClienteCpf: "52998224725",
                ClienteEmail: "j@e.com",
                ClienteTelefone: "11",
                VeiculoId: Guid.NewGuid(),
                VeiculoPlaca: "ABC1234",
                VeiculoMarca: "Toyota",
                VeiculoModelo: "Corolla",
                VeiculoAno: 2020,
                DataCriacao: DateTime.UtcNow);

            await harness.Bus.Publish(msg);

            (await harness.Consumed.Any<OSCriada>()).Should().BeTrue();
            execucaoRepo.Verify(r => r.AdicionarAsync(It.IsAny<ExecucaoOS>(), It.IsAny<CancellationToken>()), Times.Once);
            eventoRepo.Verify(r => r.RegistrarAsync(It.IsAny<EventoExecucao>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task Consume_QuandoExecucaoJaExiste_NaoDuplica()
    {
        var execucaoRepo = new Mock<IExecucaoRepository>();
        execucaoRepo.Setup(r => r.ObterPorOSAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecucaoOS());

        var eventoRepo = new Mock<IEventoRepository>();

        await using var provider = new ServiceCollection()
            .AddSingleton(execucaoRepo.Object)
            .AddSingleton(eventoRepo.Object)
            .AddSingleton(NullLogger<OSCriadaConsumer>.Instance)
            .AddMassTransitTestHarness(x => x.AddConsumer<OSCriadaConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            await harness.Bus.Publish(new OSCriada(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                "J", "529", "e@e", "11", Guid.NewGuid(), "ABC1234", "T", "Y", 2020, DateTime.UtcNow));

            (await harness.Consumed.Any<OSCriada>()).Should().BeTrue();
            execucaoRepo.Verify(r => r.AdicionarAsync(It.IsAny<ExecucaoOS>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        finally
        {
            await harness.Stop();
        }
    }
}
