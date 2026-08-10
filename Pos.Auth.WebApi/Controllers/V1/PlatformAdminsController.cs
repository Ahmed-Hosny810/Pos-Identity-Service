using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos.Identity.Application.Features.Authentication.Commands.DeactivateUser;
using Pos.Identity.Application.Features.PlatformAdmins.Commands;
using Pos.Identity.Application.Features.PlatformAdmins.DTOS;
using Pos.Identity.Application.Wrappers;
using Pos.Identity.Domain.Constants;

namespace Pos.Auth.WebApi.Controllers.V1
{
    [Route("api/v{version:apiVersion}/platform-admins")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Policy = "CanManagePlatformAdmins")]
    public class PlatformAdminsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PlatformAdminsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<ActionResult<Response<CreatePlatformAdminResult>>> Create(
            [FromBody] CreatePlatformAdminCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);

            return Ok(result);
        }

        [HttpPost("deactivate-users")]
        public async Task<ActionResult<Response<string>>> Deactivate([FromBody] DeactivateUserCommand command)
        {
            return Ok(await _mediator.Send(command));
        }
    }
}
