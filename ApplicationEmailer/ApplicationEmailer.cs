using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage;

namespace Hakemus
{
    public static class ApplicationEmailer
    {
        [FunctionName("ApplicationEmailer")]
        public static void Run([QueueTrigger("emailqueue", Connection = "AzureWebJobsStorage")] string messageItem,
            ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {messageItem}");
            dynamic json = JsonConvert.DeserializeObject(messageItem);

            string subject = json.subject;
            string content = json.content;
            string recipient = json.recipient;

            var filestorageConnection = Environment.GetEnvironmentVariable("FileStorage");
            var storageAccount = CloudStorageAccount.Parse(filestorageConnection);

            string containerName = "files";

            string CVFileName = "CV.pdf";
            string applicationFileName = "Hakemus.pdf";

            string[] attachmentCV = { GetAttachment(containerName, CVFileName), CVFileName };
            string[] attachmentApplication = { GetAttachment(containerName, applicationFileName), applicationFileName };

            string[][] attachments = { attachmentCV, attachmentApplication };

            Execute(subject, content, recipient, attachments).Wait();
        }

        
        
        public static string GetAttachment(string containerName, string fileName)
        {
            var FileStorageConnection = Environment.GetEnvironmentVariable("FileStorage");
            var storageAccount =  CloudStorageAccount.Parse(FileStorageConnection);
            var blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            CloudBlockBlob blob = container.GetBlockBlobReference(fileName);

            string b64String;

            using (var memoryStream = new MemoryStream())
            {
                blob.DownloadToStream(memoryStream);
                var bytes = memoryStream.ToArray();
                b64String = Convert.ToBase64String(bytes);
            }

            return b64String;
        }



        static async Task Execute(String subject, String content, String recipient, string[][] attachments)
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

                foreach (string[] i in attachments)
                {
                    Attachment attachment = new Attachment();
                    attachment.Content = i[0];
                    attachment.Type = "application/pdf";
                    attachment.Filename = i[1];
                    attachment.Disposition = "attachment";
                    msg.AddAttachment(attachment);
                }

                var response = await client.SendEmailAsync(msg).ConfigureAwait(false);
                Console.WriteLine("SendGrid status code: " + response.StatusCode);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.Read();
            }
        }

    }
}
