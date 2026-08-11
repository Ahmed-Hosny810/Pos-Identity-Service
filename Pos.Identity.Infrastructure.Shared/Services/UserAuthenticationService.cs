using Pos.Identity.Application.Exceptions;
using Pos.Identity.Application.Interfaces.Services;
using Pos.Identity.Application.Wrappers;
using Pos.Identity.Domain.Constants;
using Pos.Identity.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Pos.Identity.Infrastructure.Shared.Services
{
    public class UserAuthenticationService : IUserAuthenticationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<UserAuthenticationService> _logger;
        private readonly ICurrentUserService _currentUserService;

        public UserAuthenticationService(UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager, IEmailService emailService, 
        ILogger<UserAuthenticationService> logger,ICurrentUserService currentUserService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _logger = logger;
            _currentUserService = currentUserService;
        }
        

        public async Task<Response<string>> RegisterAsync(string userName,string email, string password, string fullName)
        {
            _logger.LogInformation("Registration attempt for email: {Email}", email);
            var existingUserByEmail = await _userManager.FindByEmailAsync(email);
            if (existingUserByEmail != null)
            {
                throw new ApiException("Email is already registered. Please login.");
            }

            var existingUserByUsername = await _userManager.FindByNameAsync(userName);
            if (existingUserByUsername != null)
            {
                _logger.LogWarning("Registration failed — username already taken: {UserName}", userName);
                throw new ApiException("Username is already taken");
            }

            var user = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                FullName = fullName,
                UserType = UserTypes.PendingTenant,
                TenantId = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
                
            ValidateUserContext(user);

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError(
                "Registration failed for {Email}. Errors: {Errors}",
                email,errors);
                throw new ApiException($"Registration failed: {errors}");
            }
            _logger.LogInformation(
           "User registered successfully. UserId: {UserId} Email: {Email}",
           user.Id, email);

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await _emailService.SendConfirmationEmailAsync(user.Email, user.Id, token);

            return new Response<string>(user.Id, "Registration successful. Please check your email to confirm your account.");
        }
        public async Task<Response<LoginResult>> LoginAsync(string email, string password)
        {
            _logger.LogInformation(
           "Login attempt for email: {Email}", email);
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning(
                "Login failed — user not found: {Email}", email);
                throw new ApiException("Invalid email or password");
            }
            if (!user.IsActive)
            {
                _logger.LogWarning(
                "Login failed — account deactivated. UserId: {UserId}", user.Id);
                throw new ApiException("This account has been deactivated. Please contact support.");
            }

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                _logger.LogWarning(
                "Login failed — email not confirmed. UserId: {UserId}", user.Id);
                throw new ApiException("Email is not confirmed. Please check your inbox.");
            }

            ValidateUserContext(user);

            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    _logger.LogWarning(
                   "Login failed — account locked out. UserId: {UserId}", user.Id);
                    throw new ApiException("Account is locked out due to multiple failed attempts. Please try again later.");
                }
                _logger.LogWarning(
                "Login failed — invalid password. UserId: {UserId}", user.Id);
                throw new ApiException("Invalid email or password");
            }
            var now = DateTime.UtcNow;

            if (user.MustChangePassword)
            {
                if (user.TemporaryPasswordExpiresAt.HasValue &&
                    user.TemporaryPasswordExpiresAt.Value < now)
                {
                    throw new ApiException("Temporary password has expired. Please contact your tenant admin for a new invitation.");
                }
            }

            // Check for concurrent logins
            var inactivityLimit = TimeSpan.FromMinutes(20);
            if (user.IsLoggedIn &&user.LastAccessedAt.HasValue && now - user.LastAccessedAt.Value <= inactivityLimit)
            {
                throw new ApiException("This account is already logged in on another device.");
            }

            user.IsLoggedIn = true;
            user.LastAccessedAt = now;
            user.UpdatedAt = now;

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                throw new ApiException($"Failed to update login state: {errors}");
            }

            _logger.LogInformation(
            "Login successful. UserId: {UserId} Email: {Email}",
            user.Id, email);
            var roles = await _userManager.GetRolesAsync(user);

            return new Response<LoginResult>(new LoginResult
            {
                UserId = user.Id,
                Email = user.Email,
                TenantId = user.TenantId,
                IsLoggedIn = user.IsLoggedIn,
                LastAccessedAt= user.LastAccessedAt.Value,
                UserType = user.UserType,
                FullName = user.FullName,
                MustChangePassword = user.MustChangePassword,
                Roles = roles
            });
        }
        public async Task<Response<bool>> ConfirmEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new ApiException("User not found");
            }

            if (user.EmailConfirmed)
            {
                throw new ApiException("Email is already confirmed");
            }

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ApiException($"Email confirmation failed: {errors}");
            }

            return new Response<bool>(true);
        }

        public async Task<bool> GetUserStatus(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new ApiException("User not found");
            }
            return user.IsActive;
        }

        public async Task<Response<string>> ForgotPasswordAsync(string email)
        {
            _logger.LogInformation(
            "Password reset requested for email: {Email}", email);

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning(
                "Password reset — user not found or inactive: {Email}", email);

                return new Response<string>("If this email is registered you will receive a reset link.");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _emailService.SendPasswordResetEmailAsync(user.Email, user.Id, token);

            return new Response<string>(data: "If this email is registered you will receive a reset link.");
        }
        
        public async Task<Response<string>> ResetPasswordAsync(string userId, string token, string newPassword)
        {
            _logger.LogInformation(
           "Password reset attempt. UserId: {UserId}", userId);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new ApiException("Invalid request.");

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, newPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning(
                "Password reset failed. UserId: {UserId} Errors: {Errors}", userId, errors);
                throw new ApiException($"Password reset failed: {errors}");
            }

            _logger.LogInformation(
            "Password reset successful. UserId: {UserId}", userId);

            return new Response<string>(data:"Password has been reset successfully.");
        }

        public async Task<Response<string>> DeactivateUserAsync(string userId)
        {
            _logger.LogInformation(
            "Deactivation requested. UserId: {UserId}", userId);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new ApiException("User not found.");

            if (!user.IsActive)
                throw new ApiException("User is already deactivated.");

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ApiException($"Failed to deactivate user: {errors}");
            }

            _logger.LogWarning(
            "User deactivated. UserId: {UserId}", userId);

            return new Response<string>(data:"User has been deactivated.");
        }

        public async Task<Response<LoginResult>> SocialLoginAsync(string provider, string providerKey, string email, string fullName)
        {
            _logger.LogInformation(
           "Social login attempt. Provider: {Provider} Email: {Email}",
           provider, email);
            var user = await _userManager.FindByLoginAsync(provider, providerKey);
            if(user == null)
            {
                _logger.LogInformation(
               "No existing social login found. Checking email: {Email}", email);
                user = await _userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    _logger.LogInformation(
                    "Creating new user via social login. Email: {Email}", email);
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FullName = fullName,
                        EmailConfirmed = true,
                        IsActive = true,
                        UserType = UserTypes.PendingTenant,
                        TenantId = null,
                        CreatedAt = DateTime.UtcNow
                    };

                    ValidateUserContext(user);

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        var errors = string.Join(", ", createResult.Errors
                            .Select(e => e.Description));
                        throw new ApiException($"Failed to create user: {errors}");
                    }
                }
                _logger.LogInformation(
                    "Linking {Provider} to existing account. UserId: {UserId}",
                    provider, user.Id);
                await _userManager.AddLoginAsync(user, new UserLoginInfo(
                    provider,
                    providerKey,
                    provider
                    ));
            }

            if (!user.IsActive)
            {
                throw new ApiException("This account has been deactivated.");
            }
                
            _logger.LogInformation(
           "Social login successful. UserId: {UserId} Provider: {Provider}",
           user.Id, provider);

            var roles = await _userManager.GetRolesAsync(user);

            return new Response<LoginResult>(new LoginResult
            {
                UserId = user.Id,
                Email = user.Email!,
                FullName = user.FullName,
                TenantId = user.TenantId,
                UserType = user.UserType,
                Roles = roles
            });
        }

        public async Task LogoutAsync()
        {
            var userId = _currentUserService.UserId;
            _logger.LogInformation("Logout attempt for userId: {UserId}", userId);

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning(
                "Logout failed — user not found: {UserId}", userId);
                throw new ApiException("Invalid userId");
            }

            user.IsLoggedIn = false;
            user.LastAccessedAt = null;
            user.UpdatedAt = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                throw new ApiException($"Failed to update user state: {errors}");
            }
            _logger.LogInformation(
           "Logout successful. UserId: {UserId}",user.Id);

        }

        public async Task<Response<string>> ChangeTemporaryPasswordAsync(string currentPassword,string newPassword)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
                throw new ApiException("User is not authenticated.");

            _logger.LogInformation(
                "Temporary password change attempt. UserId: {UserId}",
                userId);

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                throw new ApiException("User not found.");

            if (!user.IsActive)
                throw new ApiException("This account has been deactivated.");

            if (!user.MustChangePassword)
                throw new ApiException("Password change is not required for this account.");

            var now = DateTime.UtcNow;

            if (!user.TemporaryPasswordExpiresAt.HasValue ||
                user.TemporaryPasswordExpiresAt.Value < now)
            {
                throw new ApiException(
                    "Temporary password has expired. Please contact your admin for a new invitation.");
            }

            var result = await _userManager.ChangePasswordAsync(
                user,
                currentPassword,
                newPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));

                _logger.LogWarning(
                    "Temporary password change failed. UserId: {UserId}. Errors: {Errors}",
                    user.Id,
                    errors);

                throw new ApiException($"Password change failed: {errors}");
            }

            user.MustChangePassword = false;
            user.TemporaryPasswordExpiresAt = null;
            user.UpdatedAt = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));

                throw new ApiException($"Failed to update user password state: {errors}");
            }

            _logger.LogInformation(
                "Temporary password changed successfully. UserId: {UserId}",
                user.Id);

            return new Response<string>(
                data: "Password has been changed successfully.");
        }

        //helper
        private void ValidateUserContext(ApplicationUser user)
        {
            if (string.IsNullOrWhiteSpace(user.UserType))
            {
                _logger.LogWarning(
                    "User context validation failed — missing UserType. UserId: {UserId}",
                    user.Id);

                throw new ApiException("User account is not configured correctly.");
            }

            if (user.UserType == UserTypes.Tenant && !user.TenantId.HasValue)
            {
                _logger.LogWarning(
                    "User context validation failed — tenant user without TenantId. UserId: {UserId}",
                    user.Id);

                throw new ApiException("User account is not linked to a tenant.");
            }

            if (user.UserType == UserTypes.PendingTenant && user.TenantId.HasValue)
            {
                _logger.LogWarning(
                    "User context validation failed — pending tenant user has TenantId. UserId: {UserId}",
                    user.Id);

                throw new ApiException("User account is in an invalid onboarding state.");
            }
        }

    }
}
