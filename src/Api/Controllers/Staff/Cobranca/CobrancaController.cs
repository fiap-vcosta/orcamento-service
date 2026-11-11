using Api.Presenters.Cobranca;
using Application.UseCases.Cobranca.Commands.GerarNovaCobranca;
using BuildingBlocks.Http.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Staff.Cobranca;

[ApiController]
[Route("orcamento")]
[Authorize(Policy = AuthPolicies.FuncionarioJwt)]
public sealed class CobrancaController(IMediator mediator, CobrancaPresenter presenter) : ControllerBase
{
    [HttpPost("ordens/{ordemServicoId:int}/cobrancas")]
    public async Task<IActionResult> GerarNova(int ordemServicoId)
    {
        var response = await mediator.Send(new GerarNovaCobrancaCommand { OrdemServicoId = ordemServicoId });
        return Created(
            $"/orcamento/ordens/{ordemServicoId}/cobrancas/{response.CobrancaId}",
            presenter.Present(response));
    }
}
