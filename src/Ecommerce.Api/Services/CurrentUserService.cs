using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Ecommerce.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Ecommerce.Api.Services
{
    /// <summary>
    /// Resolves the current user from the authenticated HttpContext (JWT claims),
    /// matching the 'sub' claim used by JwtTokenService.
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _accessor;

        public CurrentUserService(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public Guid? UserId
        {
            get
            {
                var user = _accessor.HttpContext?.User;
                if (user?.Identity?.IsAuthenticated != true) return null;

                var sub = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                       ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? user.FindFirst("sub")?.Value
                       ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
                       ?? user.FindFirst("id")?.Value
                       ?? user.FindFirst("uid")?.Value;
                return Guid.TryParse(sub, out var id) ? id : null;
            }
        }

        public string? UserName
        {
            get
            {
                var user = _accessor.HttpContext?.User;
                if (user?.Identity?.IsAuthenticated != true) return null;

                return user.Identity?.Name
                    ?? user.FindFirst(ClaimTypes.Name)?.Value
                    ?? user.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                    ?? user.FindFirst(ClaimTypes.Email)?.Value
                    ?? user.FindFirst("email")?.Value;
            }
        }

        public bool IsAdmin
        {
            get
            {
                var user = _accessor.HttpContext?.User;
                if (user?.Identity?.IsAuthenticated != true) return false;

                return user.IsInRole("Admin")
                    || user.HasClaim(ClaimTypes.Role, "Admin")
                    || user.HasClaim("role", "Admin")
                    || string.Equals(UserName, "admin@ecommerce.local", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
