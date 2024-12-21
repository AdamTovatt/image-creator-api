using ImageCreatorApi.Helpers;
using ImageCreatorApi.Helpers.ImageCreatorApi.Helpers;
using ImageCreatorApi.Helpers.Users;
using ImageCreatorApi.Models;
using Microsoft.AspNetCore.Mvc;
using Sakur.WebApiUtilities.Models;
using System.Net;

namespace ImageCreatorApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserProvider userProvider;

        public AuthController()
        {
            userProvider = UserProviderFactory.GetInstance();
        }

        /// <summary>
        /// Generates a magic link for the given email and sends it to the user.
        /// </summary>
        /// <param name="requestBody">The request body containing necessary data for generating the link.</param>
        /// <returns>Action result indicating success or failure.</returns>
        [HttpPost("generate-magic-link")]
        public async Task<IActionResult> GenerateMagicLink([FromBody] UserGenerateLinkRequestBody requestBody)
        {
            if (!requestBody.Valid)
                return new ApiResponse(requestBody.GetInvalidBodyMessage(), HttpStatusCode.BadRequest);

            try
            {
                User user = await userProvider.GetUserByEmailAsync(requestBody.Email);
                if (user == null)
                    return new ApiResponse("User not found.", HttpStatusCode.NotFound);

                string token = AuthHelper.Instance.GenerateMagicToken(user.Id.ToString());

                await EmailHelper.Instance.SendMagicLinkEmail(
                    recipient: requestBody.Email,
                    productName: requestBody.ProductName,
                    baseUrl: requestBody.BaseUrl,
                    magicLinkToken: token,
                    dateAndTime: requestBody.DateAndTime
                );

                return new ApiResponse("Magic link sent successfully.");
            }
            catch (KeyNotFoundException ex)
            {
                return new ApiResponse(ex.Message, HttpStatusCode.NotFound);
            }
            catch (Exception ex)
            {
                return new ApiResponse($"An error occurred: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Validates a magic link token and issues a JWT if valid.
        /// </summary>
        /// <param name="token">The magic link token.</param>
        /// <returns>A JWT if the token is valid, or an error response otherwise.</returns>
        [HttpGet("validate-magic-link")]
        public IActionResult ValidateMagicLink([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return new ApiResponse("Token is required.", HttpStatusCode.BadRequest);

            try
            {
                if (AuthHelper.Instance.ValidateMagicToken(token, out string? userId))
                {
                    if (userId == null)
                        return new ApiResponse("An unknown error during token validation made userId be null.", HttpStatusCode.InternalServerError);

                    string jwt = AuthHelper.Instance.GenerateJwtToken(userId, new[] { "user" });
                    return new ApiResponse(new { Token = jwt });
                }

                return new ApiResponse("Invalid or expired token.", HttpStatusCode.Unauthorized);
            }
            catch (Exception ex)
            {
                return new ApiResponse($"An error occurred: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }
    }
}
