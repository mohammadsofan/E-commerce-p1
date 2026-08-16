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
                       ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return Guid.TryParse(sub, out var id) ? id : null;
            }
        }

        public string? UserName => _accessor.HttpContext?.User?.Identity?.Name;
    }
}
