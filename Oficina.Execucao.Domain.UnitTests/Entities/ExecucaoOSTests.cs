using FluentAssertions;
using Oficina.Execucao.Domain.Entities;
using Oficina.Execucao.Domain.Enums;
using Oficina.Execucao.Domain.Exceptions;
using Xunit;

namespace Oficina.Execucao.Domain.UnitTests.Entities;

public class ExecucaoOSTests
{
    private static ExecucaoOS Criar() => new()
    {
        CorrelationId = Guid.NewGuid(),
        OrdemDeServicoId = Guid.NewGuid(),
        ClienteId = Guid.NewGuid(),
        VeiculoId = Guid.NewGuid(),
        Dados = new DadosSnapshot("João", "52998224725", "j@e.com", "11", "ABC1234", "Toyota", "Corolla", 2020),
        Status = StatusExecucao.AguardandoDiagnostico
    };

    [Fact]
    public void IniciarDiagnostico_QuandoAguardando_TransicionaParaEmDiagnostico()
    {
        var e = Criar();
        e.IniciarDiagnostico("Mecanico A");

        e.Status.Should().Be(StatusExecucao.EmDiagnostico);
        e.MecanicoResponsavel.Should().Be("Mecanico A");
        e.DiagnosticoIniciadoEm.Should().NotBeNull();
    }

    [Fact]
    public void IniciarDiagnostico_QuandoEmExecucao_LancaDomainException()
    {
        var e = Criar();
        e.IniciarDiagnostico("M");
        e.ProporOrcamento(100m, new List<ItemPropostoSnapshot>());
        e.Status = StatusExecucao.AguardandoPagamento;
        e.IniciarExecucao("M");

        var act = () => e.IniciarDiagnostico("X");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ProporOrcamento_QuandoEmDiagnostico_AtualizaValorEItens()
    {
        var e = Criar();
        e.IniciarDiagnostico("M");

        var itens = new List<ItemPropostoSnapshot>
        {
            new("Peca", Guid.NewGuid(), "Filtro", 2, 40m)
        };
        e.ProporOrcamento(80m, itens);

        e.Status.Should().Be(StatusExecucao.AguardandoOrcamento);
        e.ValorOrcamentoProposto.Should().Be(80m);
        e.ItensPropostos.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ProporOrcamento_ComValorNaoPositivo_LancaDomainException(decimal valor)
    {
        var e = Criar();
        e.IniciarDiagnostico("M");

        var act = () => e.ProporOrcamento(valor, new List<ItemPropostoSnapshot>());
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void IniciarExecucao_QuandoAguardandoPagamento_TransicionaParaEmExecucao()
    {
        var e = Criar();
        e.IniciarDiagnostico("M");
        e.ProporOrcamento(100m, new List<ItemPropostoSnapshot>());
        e.Status = StatusExecucao.AguardandoPagamento;

        e.IniciarExecucao("M");

        e.Status.Should().Be(StatusExecucao.EmExecucao);
        e.ExecucaoIniciadaEm.Should().NotBeNull();
    }

    [Fact]
    public void IniciarExecucao_QuandoEmDiagnostico_LancaDomainException()
    {
        var e = Criar();
        e.IniciarDiagnostico("M");

        var act = () => e.IniciarExecucao("M");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Finalizar_PreencheDataEfimECalculaTempo()
    {
        var e = Criar();
        e.IniciarDiagnostico("M");
        e.ProporOrcamento(100m, new List<ItemPropostoSnapshot>());
        e.Status = StatusExecucao.AguardandoPagamento;
        e.IniciarExecucao("M");

        Thread.Sleep(10);
        e.Finalizar();

        e.Status.Should().Be(StatusExecucao.Finalizada);
        e.FinalizadaEm.Should().NotBeNull();
        e.TempoTotalExecucaoHoras.Should().NotBeNull();
    }

    [Fact]
    public void Remover_AlteraStatusParaRemovida()
    {
        var e = Criar();
        e.Remover();
        e.Status.Should().Be(StatusExecucao.Removida);
    }
}
