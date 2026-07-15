using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Attendance_System.Middleware
{
    public class RoleMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public RoleMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader != null && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                AttachUserToContext(context, token);
            }
            else
            {
                // Fallback to check cookie if token is stored in cookies (useful for MVC views)
                var tokenFromCookie = context.Request.Cookies["jwt"];
                if (!string.IsNullOrEmpty(tokenFromCookie))
                {
                    AttachUserToContext(context, tokenFromCookie);
                }
            }

            await _next(context);
        }

        private void AttachUserToContext(HttpContext context, string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var keyStr = _configuration["JwtSettings:Key"];
                if (string.IsNullOrEmpty(keyStr)) return;

                var key = Encoding.UTF8.GetBytes(keyStr);
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["JwtSettings:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["JwtSettings:Audience"],
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var identity = new ClaimsIdentity(jwtToken.Claims, "Jwt");
                context.User = new ClaimsPrincipal(identity);
            }
            catch
            {
                // Ignore validation errors (user remains unauthenticated)
            }
        }
    }
}
