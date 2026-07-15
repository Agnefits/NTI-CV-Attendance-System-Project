using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Attendance_System.Models.Enums;

namespace Attendance_System.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class AuthorizedRolesAttribute : Attribute, IAuthorizationFilter
    {
        private readonly Roles[] _roles;

        public AuthorizedRolesAttribute(params Roles[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var userRoleClaim = user.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userRoleClaim))
            {
                context.Result = new ForbidResult();
                return;
            }

            // Check if the user's role matches any of the allowed roles
            var hasRole = _roles.Any(r => r.ToString().Equals(userRoleClaim, StringComparison.OrdinalIgnoreCase));

            if (!hasRole)
            {
                context.Result = new ForbidResult();
            }
        }
    }
}
