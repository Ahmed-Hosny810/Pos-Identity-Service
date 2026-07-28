using Asp.Versioning;
using Pos.Identity.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos.Identity.Pos.Identity.Application.Features.Roles.Commands;

namespace Pos.Identity.WebApi.Controllers.V1
{
    [Route("api/{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize(Roles = PlatformRoles.Admin)]
    [ApiVersion("1.0")]
    public class RolesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RolesController(IMediator mediator)
            => _mediator = mediator;

        [HttpPost("assign-admin/{userId}")]
        public async Task<IActionResult> AssignAdmin(string userId)
        {
            var result = await _mediator.Send(
                new AssignAdminRoleCommand { UserId = userId });
            return Ok(result);
        }

    }
}
