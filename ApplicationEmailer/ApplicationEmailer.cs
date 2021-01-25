using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hakemus
{
    public static class ApplicationEmailer
    {
        [FunctionName("ApplicationEmailer")]
        public static void Run([QueueTrigger("outqueue", Connection = "AzureWebJobsStorage")] string messageItem,
            ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {messageItem}");
            dynamic json = JsonConvert.DeserializeObject(messageItem);

            Console.WriteLine(json.subject);
            string subject = json.subject;

            Console.WriteLine(json.content);
            string content = json.content;

            Console.WriteLine(json.recipient);
            string recipient = json.recipient;

            Execute(subject, content, recipient).Wait();
        }

        static async Task Execute(String subject, String content, String recipient)
        {

            try
            {
                var apiKey = System.Environment.GetEnvironmentVariable("SENDGRID_APIKEY");
                var client = new SendGridClient(apiKey);

                var msg = new SendGridMessage()
                {
                    From = new EmailAddress("heidi.wikman@kapsi.fi", "Heidi Wikman"),
                    Subject = subject,
                    PlainTextContent = content,
                };
                msg.AddTo(new EmailAddress(recipient, "Heidi"));

                var response = await client.SendEmailAsync(msg).ConfigureAwait(false);
                Console.WriteLine("Status code: " + response.StatusCode);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.Read();
            }
        }

    }
}
