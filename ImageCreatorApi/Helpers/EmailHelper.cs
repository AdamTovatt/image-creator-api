using PostmarkDotNet;
using Sakur.WebApiUtilities;
using Sakur.WebApiUtilities.Helpers;

namespace ImageCreatorApi.Helpers
{
    public class EmailHelper
    {
        public static EmailHelper Instance { get { if (instance == null) instance = CreateFromEnvironmentVariables(); return instance; } }
        private static EmailHelper? instance;

        private string apiKey;
        private string sender;

        private string? cachedMagicLinkPlainText;
        private string? cachedMagicLinkHtml;

        private EmailHelper(string apiKey, string sender)
        {
            this.apiKey = apiKey;
            this.sender = sender;
        }

        public static EmailHelper CreateFromEnvironmentVariables()
        {
            string apiKey = EnvironmentHelper.GetEnvironmentVariable(StringConstants.PostmarkApiKey);
            string sender = EnvironmentHelper.GetEnvironmentVariable(StringConstants.EmailSender);

            return new EmailHelper(apiKey, sender);
        }

        private async Task<PostmarkResponse> SendEmail(string recipient, string subject, string plainTextContent, string htmlContent, string tag)
        {
            PostmarkMessage message = new PostmarkMessage()
            {
                To = recipient,
                From = sender,
                TrackOpens = false,
                Subject = subject,
                TextBody = plainTextContent,
                HtmlBody = htmlContent,
                Tag = tag,
            };

            PostmarkClient client = new PostmarkClient(apiKey);
            return await client.SendMessageAsync(message);
        }

        private async Task<string> GetMagicLinkHtml()
        {
            if (cachedMagicLinkHtml == null)
            {
                using Stream stream = typeof(EmailHelper).Assembly.GetManifestResourceStream("ImageCreatorApi.Resources.MagicLinkEmail.html")!;
                using StreamReader reader = new StreamReader(stream);
                cachedMagicLinkHtml = await reader.ReadToEndAsync();
            }

            return cachedMagicLinkHtml;
        }

        private async Task<string> GetMagicLinkPlainText()
        {
            if (cachedMagicLinkPlainText == null)
            {
                using Stream stream = typeof(EmailHelper).Assembly.GetManifestResourceStream("ImageCreatorApi.Resources.MagicLinkPlainText.txt")!;
                using StreamReader reader = new StreamReader(stream);
                cachedMagicLinkPlainText = await reader.ReadToEndAsync();
            }

            return cachedMagicLinkPlainText;
        }

        public async Task SendMagicLinkEmail(string recipient, string productName, string baseUrl, string magicLinkToken, string dateAndTime)
        {
            string rawHtml = await GetMagicLinkHtml();
            string rawPlainText = await GetMagicLinkPlainText();

            var parameters = new
            {
                productName,
                baseUrl,
                magicLinkToken,
                dateAndTime
            };

            string replacedHtml = rawHtml.ApplyParameters(parameters);
            string replacedPlainText = rawPlainText.ApplyParameters(parameters);

            string subject = $"Login link for {productName} ({dateAndTime})";

            await SendEmail(recipient, subject, replacedPlainText, replacedHtml, "image-creator-login");
        }
    }
}
