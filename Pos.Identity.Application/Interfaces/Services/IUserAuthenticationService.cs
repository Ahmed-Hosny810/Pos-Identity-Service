using Pos.Identity.Application.Wrappers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Interfaces.Services
{
    public interface IUserAuthenticationService
    {
        Task<Response<LoginResult>> LoginAsync(string email, string password);
        Task<Response<string>> RegisterAsync(string userName,string email, string password, string fullName);
        Task<Response<LoginResult>> SocialLoginAsync(string provider,string providerKey,string email,string fullName);
        Task<Response<bool>> ConfirmEmailAsync(string userId, string token);
        Task<bool> GetUserStatus(string userId);
        Task<Response<string>> ForgotPasswordAsync(string email);
        Task<Response<string>> ResetPasswordAsync(string userId, string token, string newPassword);
        Task<Response<string>> DeactivateUserAsync(string userId);
    }
}
