using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Oficina.Contracts.Events;
using Oficina.Execucao.Application.DTOs;
using Oficina.Execucao.Application.UseCases;
using Oficina.Execucao.Domain.Entities;
using Oficina.Execucao.Domain.Enums;
using Oficina.Execucao.Domain.Repositories;
using Reqnroll;

namespace Oficina.Execucao.Api.IntegrationTests.Steps;

[Binding]
public class FluxoExecucaoSteps
{
    private readonly Mock<IExecucaoRepository> _repo = new();
    private readonly Mock<IEventoRepository> _eventoRepo = new();
    private readonly Mock<IPublishEndpoint> _publish = new();
    private readonly List<object> _eventos = new();

    private ExecucaoOS _execucao = null!;
    private ExecucaoUseCase _useCase = null!;

    public FluxoExecucaoSteps()
    {
        _publish
            .Setup(p => p.Publish(It.IsAny<It.IsAnyType>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(new InvocationAction(inv => _eventos.Add(inv.Arguments[0])));

        _useCase = new ExecucaoUseCase(
            _repo.Object, _eventoRepo.Object, _publish.Object, NullLogger<ExecucaoUseCase>.Instance);
    }

    [Given(@"que existe uma OS ""(.*)"" na fila aguardando diagnóstico")]
    public void DadoOSAguardandoDiagnostico(string placa)
    {
        _execucao = NovaExecucao(placa, StatusExecucao.AguardandoDiagnostico);
        _repo.Setup(r => r.ObterPorOSAsync(_execucao.OrdemDeServicoId, It.IsAny<CancellationToken>())).ReturnsAsync(_execucao);
    }

    [Given(@"que existe uma OS ""(.*)"" em diagnóstico")]
    public void DadoOSEmDiagnostico(string placa)
    {
        _execucao = NovaExecucao(placa, StatusExecucao.AguardandoDiagnostico);
        _execucao.IniciarDiagnostico("M");
        _repo.Setup(r => r.ObterPorOSAsync(_execucao.OrdemDeServicoId, It.IsAny<CancellationToken>())).ReturnsAsync(_execucao);
    }

    [Given(@"que existe uma OS ""(.*)"" em execução")]
    public void DadoOSEmExecucao(string placa)
    {
        _execucao = NovaExecucao(placa, StatusExecucao.AguardandoDiagnostico);
        _execucao.IniciarDiagnostico("M");
        _execucao.ProporOrcamento(100m, new List<ItemPropostoSnapshot>());
        _execucao.Status = StatusExecucao.AguardandoPagamento;
        _execucao.IniciarExecucao("M");
        _repo.Setup(r => r.ObterPorOSAsync(_execucao.OrdemDeServicoId, It.IsAny<CancellationToken>())).ReturnsAsync(_execucao);
    }

    [When(@"o mecânico ""(.*)"" inicia o diagnóstico")]
    public async Task QuandoMecanicoIniciaDiagnostico(string mecanico)
    {
        await _useCase.IniciarDiagnosticoAsync(_execucao.OrdemDeServicoId, new IniciarDiagnosticoInput(mecanico));
    }

    [When(@"o mecânico propõe um orçamento de (.*) com (\d+) item")]
    public async Task QuandoMecanicoPropoeOrcamento(decimal valor, int qtdItens)
    {
        var itens = Enumerable.Range(0, qtdItens)
            .Select(_ => new ItemOrcamentoInput("Servico", Guid.NewGuid(), "X", 1, valor))
            .ToList();
        await _useCase.ProporOrcamentoAsync(_execucao.OrdemDeServicoId, new ProporOrcamentoInput(valor, itens));
    }

    [When(@"o mecânico finaliza a execução")]
    public async Task QuandoMecanicoFinaliza()
    {
        await _useCase.FinalizarExecucaoAsync(_execucao.OrdemDeServicoId);
    }

    [Then(@"o evento ""(.*)"" é publicado com o mecânico ""(.*)""")]
    public void EntaoEventoComMecanico(string nomeEvento, string mecanico)
    {
        nomeEvento.Should().Be(nameof(DiagnosticoIniciado));
        var evt = _eventos.OfType<DiagnosticoIniciado>().Should().ContainSingle().Subject;
        evt.MecanicoResponsavel.Should().Be(mecanico);
    }

    [Then(@"a execução fica com status ""(.*)""")]
    public void EntaoExecucaoStatus(string statusEsperado)
    {
        _execucao.Status.ToString().Should().Be(statusEsperado);
    }

    [Then(@"o evento ""(.*)"" é publicado com valor (.*)")]
    public void EntaoEventoComValor(string nomeEvento, decimal valor)
    {
        nomeEvento.Should().Be(nameof(OrcamentoPropostoPelaOficina));
        var evt = _eventos.OfType<OrcamentoPropostoPelaOficina>().Should().ContainSingle().Subject;
        evt.ValorTotal.Should().Be(valor);
    }

    [Then(@"o evento ""(.*)"" é publicado no barramento")]
    public void EntaoEventoPublicado(string nomeEvento)
    {
        _eventos.Should().Contain(e => e.GetType().Name == nomeEvento);
    }

    private static ExecucaoOS NovaExecucao(string placa, StatusExecucao status) => new()
    {
        CorrelationId = Guid.NewGuid(),
        OrdemDeServicoId = Guid.NewGuid(),
        ClienteId = Guid.NewGuid(),
        VeiculoId = Guid.NewGuid(),
        Dados = new DadosSnapshot("João", "52998224725", "j@e.com", "11", placa, "Toyota", "Corolla", 2020),
        Status = status
    };
}
