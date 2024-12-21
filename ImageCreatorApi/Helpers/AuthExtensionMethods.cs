using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Sakur.WebApiUtilities.Helpers;
using System.Security.Claims;
using System.Text;

namespace ImageCreatorApi.Helpers
{
    public static class AuthExtensionMethods
    {
        /// <summary>
        /// Will setup the authentication for the service collection
        /// </summary>
        /// <param name="services">The service collection to use</param>
        /// <param name="authDomain">The domain for the auth</param>
        /// <param name="authAudience">The audience for the auth</param>
        /// <param name="permissions">The permissions to have in the auth</param>
        /// <param name="jwtSecretKey">The secret key used for signing and validating JWT tokens</param>
        /// <param name="authenticationScheme">The scheme to use, default is "Bearer"</param>
        /// <returns>The service collection again so that calls can be chained</returns>
        public static IServiceCollection SetupAuth(
            this IServiceCollection services,
            string authDomain,
            string authAudience,
            List<string> permissions,
            string jwtSecretKey,
            string authenticationScheme = "Bearer")
        {
            byte[] signingKey = Encoding.UTF8.GetBytes(jwtSecretKey);

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = authenticationScheme;
                    options.DefaultChallengeScheme = authenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    // Configure token validation parameters
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = authDomain,
                        ValidAudience = authAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(signingKey), // Use the provided secret key
                        NameClaimType = ClaimTypes.NameIdentifier
                    };
                });

            services.AddAuthorization(options =>
            {
                foreach (string permission in permissions)
                    options.AddPolicy(permission, policy => policy.Requirements.Add(new HasScopeRequirement(permission, authDomain)));
            });

            return services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();
        }
    }
}
