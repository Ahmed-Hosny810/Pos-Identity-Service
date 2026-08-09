using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos.Identity.Application.Dtos;
using Pos.Identity.Application.Features.TenantOnboarding.Commands;
using Pos.Identity.Application.Wrappers;

namespace Pos.Auth.WebApi.Controllers.V1
{
    [Route("api/v{version:apiVersion}/tenant-onboarding")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    public class TenantOnboardingController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TenantOnboardingController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<ActionResult<Response<CreateTenantResult>>> Create([FromBody] CreateTenantOnboardingCommand command)
        {
          var result = await _mediator.Send(command);
          return Ok(result);
        }
    }
}
