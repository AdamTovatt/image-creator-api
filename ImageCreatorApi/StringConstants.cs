namespace ImageCreatorApi
{
    /// <summary>
    /// Provides a centralized location for all string constants used in the application.
    /// </summary>
    public static class StringConstants
    {
        // Cloudinary-related constants
        public const string CloudinaryCloud = "CLOUDINARY_CLOUD";
        public const string CloudinaryKey = "CLOUDINARY_KEY";
        public const string CloudinarySecret = "CLOUDINARY_SECRET";

        // Auth-related constants
        public const string MagicLinkSecret = "MAGIC_LINK_SECRET";
        public const string JwtSecret = "JWT_SECRET";
        public const string JwtIssuer = "JWT_ISSUER";
        public const string JwtAudience = "JWT_AUDIENCE";

        // Email-related constants
        public const string PostmarkApiKey = "POSTMARK_API_KEY";
        public const string EmailSender = "EMAIL_SENDER";
    }
}
