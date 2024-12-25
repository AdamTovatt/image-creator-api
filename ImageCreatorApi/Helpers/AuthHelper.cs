using Sakur.WebApiUtilities.Helpers;

namespace ImageCreatorApi.Helpers
{
    namespace ImageCreatorApi.Helpers
    {
        /// <summary>
        /// Provides authentication and authorization functionality for the ImageCreator API.
        /// </summary>
        public sealed class AuthHelper : AuthHelperBase<AuthHelper>
        {
            /// <summary>
            /// Configures the authentication helper with the required settings.
            /// </summary>
            protected override void Initialize()
            {
                Configure(
                    magicLinkSecretKey: EnvironmentHelper.GetEnvironmentVariable(StringConstants.MagicLinkSecret),
                    jwtSecretKey: EnvironmentHelper.GetEnvironmentVariable(StringConstants.JwtSecret),
                    issuer: EnvironmentHelper.GetEnvironmentVariable(StringConstants.JwtIssuer),
                    audience: EnvironmentHelper.GetEnvironmentVariable(StringConstants.JwtAudience),
                    magicLinkExpirationMinutes: 15,
                    jwtExpirationMinutes: 60 * 2
                );
            }
        }
    }
}
