using Sakur.WebApiUtilities.BaseClasses;
using Sakur.WebApiUtilities.Models;
using System.Text.Json.Serialization;

namespace ImageCreatorApi.Helpers.Users
{
    /// <summary>
    /// Represents the request body for generating a magic link.
    /// </summary>
    public class UserGenerateLinkRequestBody : RequestBody
    {
        [JsonPropertyName("email")]
        [Required]
        public string Email { get; set; }

        [JsonPropertyName("productName")]
        [Required]
        public string ProductName { get; set; }

        [JsonPropertyName("baseUrl")]
        [Required]
        public string BaseUrl { get; set; }

        [JsonPropertyName("dateAndTime")]
        [Required]
        public string DateAndTime { get; set; }

        /// <summary>
        /// Validates the request body based on required attributes.
        /// </summary>
        public override bool Valid => ValidateByRequiredAttributes();

        /// <summary>
        /// Initializes a new instance of the <see cref="UserGenerateLinkRequestBody"/> class.
        /// </summary>
        /// <param name="email">The email address.</param>
        /// <param name="productName">The product name.</param>
        /// <param name="baseUrl">The base URL.</param>
        /// <param name="dateAndTime">The date and time string.</param>
        public UserGenerateLinkRequestBody(string email, string productName, string baseUrl, string dateAndTime)
        {
            Email = email;
            ProductName = productName;
            BaseUrl = baseUrl;
            DateAndTime = dateAndTime;
        }
    }
}
