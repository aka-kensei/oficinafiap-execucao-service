using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Execucao.Application.DTOs;
using Oficina.Execucao.Application.UseCases;
using Oficina.Execucao.Domain.Enums;

namespace Oficina.Execucao.Api.Controllers;

[ApiController]
[Route("api/execucao")]
[Authorize]
public class ExecucaoController : ControllerBase
{
    private readonly IExecucaoUseCase _useCase;

    public ExecucaoController(IExecucaoUseCase useCase) => _useCase = useCase;

    [HttpGet("fila")]
    public async Task<ActionResult<IReadOnlyList<FilaItemViewModel>>> Fila([FromQuery] StatusExecucao? status, CancellationToken ct)
        => Ok(await _useCase.ListarFilaAsync(status, ct));

    [HttpPatch("{osId:guid}/iniciar-diagnostico")]
    public async Task<IActionResult> IniciarDiagnostico(Guid osId, [FromBody] IniciarDiagnosticoInput input, CancellationToken ct)
    {
        await _useCase.IniciarDiagnosticoAsync(osId, input, ct);
        return NoContent();
    }

    [HttpPost("{osId:guid}/propor-orcamento")]
    public async Task<IActionResult> ProporOrcamento(Guid osId, [FromBody] ProporOrcamentoInput input, CancellationToken ct)
    {
        await _useCase.ProporOrcamentoAsync(osId, input, ct);
        return NoContent();
    }

    [HttpPatch("{osId:guid}/iniciar-execucao")]
    public async Task<IActionResult> IniciarExecucao(Guid osId, [FromBody] IniciarExecucaoInput input, CancellationToken ct)
    {
        await _useCase.IniciarExecucaoAsync(osId, input, ct);
        return NoContent();
    }

    [HttpPatch("{osId:guid}/finalizar")]
    public async Task<IActionResult> Finalizar(Guid osId, CancellationToken ct)
    {
        await _useCase.FinalizarExecucaoAsync(osId, ct);
        return NoContent();
    }

    [HttpGet("{osId:guid}/historico")]
    public async Task<ActionResult<IReadOnlyList<EventoHistoricoViewModel>>> Historico(Guid osId, CancellationToken ct)
        => Ok(await _useCase.ObterHistoricoAsync(osId, ct));
}
