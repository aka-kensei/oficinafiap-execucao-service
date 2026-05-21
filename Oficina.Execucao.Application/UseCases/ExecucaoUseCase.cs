using MassTransit;
using Microsoft.Extensions.Logging;
using Oficina.Contracts.Events;
using Oficina.Execucao.Application.DTOs;
using Oficina.Execucao.Domain.Entities;
using Oficina.Execucao.Domain.Enums;
using Oficina.Execucao.Domain.Exceptions;
using Oficina.Execucao.Domain.Repositories;

namespace Oficina.Execucao.Application.UseCases;

public class ExecucaoUseCase : IExecucaoUseCase
{
    private readonly IExecucaoRepository _repo;
    private readonly IEventoRepository _eventoRepo;
    private readonly IPublishEndpoint _publish;
    private readonly ILogger<ExecucaoUseCase> _logger;

    public ExecucaoUseCase(IExecucaoRepository repo, IEventoRepository eventoRepo, IPublishEndpoint publish, ILogger<ExecucaoUseCase> logger)
    {
        _repo = repo;
        _eventoRepo = eventoRepo;
        _publish = publish;
        _logger = logger;
    }

    public async Task<IReadOnlyList<FilaItemViewModel>> ListarFilaAsync(StatusExecucao? status, CancellationToken ct = default)
    {
        var lista = await _repo.ListarFilaAsync(status, ct);
        return lista.Select(e => new FilaItemViewModel(
            e.Id, e.OrdemDeServicoId,
            e.Dados.ClienteNome, e.Dados.VeiculoPlaca, e.Dados.VeiculoMarca, e.Dados.VeiculoModelo,
            e.Status, e.MecanicoResponsavel, e.EntrouNaFilaEm, e.AtualizadaEm)).ToList();
    }

    public async Task IniciarDiagnosticoAsync(Guid osId, IniciarDiagnosticoInput input, CancellationToken ct = default)
    {
        var execucao = await _repo.ObterPorOSAsync(osId, ct) ?? throw new DomainException($"OS {osId} não está na fila.");
        execucao.IniciarDiagnostico(input.MecanicoResponsavel);
        await _repo.SalvarAsync(execucao, ct);

        await _eventoRepo.RegistrarAsync(new EventoExecucao(
            execucao.CorrelationId, osId, TipoEvento.DiagnosticoIniciado,
            $"Diagnóstico iniciado por {input.MecanicoResponsavel ?? "(não informado)"}",
            mecanico: input.MecanicoResponsavel), ct);

        await _publish.Publish(new DiagnosticoIniciado(
            CorrelationId: execucao.CorrelationId,
            OrdemDeServicoId: osId,
            MecanicoResponsavel: input.MecanicoResponsavel,
            IniciadoEm: DateTime.UtcNow), ct);
    }

    public async Task ProporOrcamentoAsync(Guid osId, ProporOrcamentoInput input, CancellationToken ct = default)
    {
        var execucao = await _repo.ObterPorOSAsync(osId, ct) ?? throw new DomainException($"OS {osId} não encontrada.");

        var itensSnapshot = input.Itens
            .Select(i => new ItemPropostoSnapshot(i.Tipo, i.ItemId, i.Descricao, i.Quantidade, i.PrecoUnitario))
            .ToList();

        execucao.ProporOrcamento(input.ValorTotal, itensSnapshot);
        await _repo.SalvarAsync(execucao, ct);

        await _eventoRepo.RegistrarAsync(new EventoExecucao(
            execucao.CorrelationId, osId, TipoEvento.OrcamentoProposto,
            $"Orçamento proposto: R$ {input.ValorTotal:F2}",
            payload: new Dictionary<string, object?> { ["valor"] = input.ValorTotal, ["qtdItens"] = input.Itens.Count }), ct);

        await _publish.Publish(new OrcamentoPropostoPelaOficina(
            CorrelationId: execucao.CorrelationId,
            OrdemDeServicoId: osId,
            ValorTotal: input.ValorTotal,
            Itens: input.Itens.Select(i => new ItemOrcamento(i.Tipo, i.ItemId, i.Descricao, i.Quantidade, i.PrecoUnitario)).ToList(),
            PropostoEm: DateTime.UtcNow), ct);
    }

    public async Task IniciarExecucaoAsync(Guid osId, IniciarExecucaoInput input, CancellationToken ct = default)
    {
        var execucao = await _repo.ObterPorOSAsync(osId, ct) ?? throw new DomainException($"OS {osId} não encontrada.");
        execucao.IniciarExecucao(input.MecanicoResponsavel);
        await _repo.SalvarAsync(execucao, ct);

        await _eventoRepo.RegistrarAsync(new EventoExecucao(
            execucao.CorrelationId, osId, TipoEvento.ExecucaoIniciada,
            $"Execução iniciada por {execucao.MecanicoResponsavel ?? "(não informado)"}",
            mecanico: execucao.MecanicoResponsavel), ct);

        await _publish.Publish(new ExecucaoIniciada(
            CorrelationId: execucao.CorrelationId,
            OrdemDeServicoId: osId,
            MecanicoResponsavel: execucao.MecanicoResponsavel,
            IniciadaEm: DateTime.UtcNow), ct);
    }

    public async Task FinalizarExecucaoAsync(Guid osId, CancellationToken ct = default)
    {
        var execucao = await _repo.ObterPorOSAsync(osId, ct) ?? throw new DomainException($"OS {osId} não encontrada.");
        execucao.Finalizar();
        await _repo.SalvarAsync(execucao, ct);

        await _eventoRepo.RegistrarAsync(new EventoExecucao(
            execucao.CorrelationId, osId, TipoEvento.ExecucaoFinalizada,
            $"Execução finalizada — duração {execucao.TempoTotalExecucaoHoras:F2}h"), ct);

        await _publish.Publish(new ExecucaoFinalizada(
            CorrelationId: execucao.CorrelationId,
            OrdemDeServicoId: osId,
            TempoTotalHoras: execucao.TempoTotalExecucaoHoras,
            FinalizadaEm: DateTime.UtcNow), ct);
    }

    public async Task<IReadOnlyList<EventoHistoricoViewModel>> ObterHistoricoAsync(Guid osId, CancellationToken ct = default)
    {
        var eventos = await _eventoRepo.HistoricoPorOSAsync(osId, ct);
        return eventos
            .OrderBy(e => e.OcorreuEm)
            .Select(e => new EventoHistoricoViewModel(e.Id, e.Tipo.ToString(), e.Descricao, e.MecanicoResponsavel, e.Payload, e.OcorreuEm))
            .ToList();
    }
}
