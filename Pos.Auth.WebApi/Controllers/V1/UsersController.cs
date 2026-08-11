using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos.Identity.Application.Features.Users.Commands.ResendInvitation;
using Pos.Identity.Application.Wrappers;

namespace Pos.Auth.WebApi.Controllers.V1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Policy = "CanResendInvitations")]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("resend-invitation")]
        public async Task<ActionResult<Response<string>>> ResendInvitation(string userId,CancellationToken cancellationToken)
        {
            await _mediator.Send(
                new ResendInvitationCommand
                {
                    UserId = userId
                },
                cancellationToken);

            return Ok(new Response<string>(
                data: "Invitation has been resent successfully."));
        }
    }
}
