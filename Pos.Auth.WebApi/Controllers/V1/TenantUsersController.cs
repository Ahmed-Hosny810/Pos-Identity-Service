using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos.Identity.Application.Features.TenantUsers.Commands.CreateCommand;
using Pos.Identity.Application.Features.TenantUsers.DTOS;
using Pos.Identity.Application.Wrappers;

namespace Pos.Auth.WebApi.Controllers.V1
{
    [Route("api/v{version:apiVersion}/tenant-users")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Policy = "CanManageTenantUsers")]
    public class TenantUsersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TenantUsersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<ActionResult<Response<CreateTenantUserResult>>> Create([FromBody] CreateTenantUserCommand command,CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);

            return Ok(result);
        }
    }
}
