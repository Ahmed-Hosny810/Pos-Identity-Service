using MediatR;
using Pos.Identity.Application.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Features.Users.Commands.ResendInvitation
{
    public class ResendInvitationCommand : IRequest
    {
        public string UserId { get; set; } = null!;
    }

    public class ResendInvitationCommandHandler
        : IRequestHandler<ResendInvitationCommand>
    {
        private readonly IUserInvitationService _userInvitationService;

        public ResendInvitationCommandHandler(
            IUserInvitationService userInvitationService)
        {
            _userInvitationService = userInvitationService;
        }

        public async Task Handle(
            ResendInvitationCommand request,
            CancellationToken cancellationToken)
        {
            await _userInvitationService.ResendInvitationAsync(
                request.UserId,
                cancellationToken);
        }
    }
}
