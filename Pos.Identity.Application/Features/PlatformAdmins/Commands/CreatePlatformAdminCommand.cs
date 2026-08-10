using MediatR;
using Pos.Identity.Application.Features.PlatformAdmins.DTOS;
using Pos.Identity.Application.Interfaces.Services;
using Pos.Identity.Application.Wrappers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Features.PlatformAdmins.Commands
{
    public class CreatePlatformAdminCommand : IRequest<Response<CreatePlatformAdminResult>>
    {
        public string FullName { get; set; } = null!;

        public string UserName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string Role { get; set; } = null!;
    }

    public class CreatePlatformAdminCommandHandler: IRequestHandler<CreatePlatformAdminCommand, Response<CreatePlatformAdminResult>>
    {
        private readonly IPlatformAdminService _platformAdminService;

        public CreatePlatformAdminCommandHandler(
            IPlatformAdminService platformAdminService)
        {
            _platformAdminService = platformAdminService;
        }

        public async Task<Response<CreatePlatformAdminResult>> Handle(
            CreatePlatformAdminCommand request,
            CancellationToken cancellationToken)
        {
            var result = await _platformAdminService.CreatePlatformAdminAsync(
                request,
                cancellationToken);

            return new Response<CreatePlatformAdminResult>(
                result,
                "Platform admin created successfully.");
        }
    }
}
