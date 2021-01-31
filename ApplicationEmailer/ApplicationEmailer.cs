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
using System.Collections;

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
            string recipientName = json.recipientName;
            string fromName = json.fromName;
            string fromEmail = json.fromEmail;

            Console.WriteLine("Finding attached files");

            string containerName = "files";

            ArrayList attachments = new ArrayList();

            foreach (var attachedFileName in json.attachments)
            {
               Console.WriteLine(attachedFileName);

               string attachedFileNameValue = attachedFileName.Value;

               string attachedFileData = GetAttachment(containerName, attachedFileNameValue);

               string[] attachedFile = { attachedFileData, attachedFileName };
               attachments.Add(attachedFile);
            }

            Execute(subject, content, recipient, recipientName, fromName, fromEmail, attachments).Wait();
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


        
        static async Task Execute(String subject, String content, String recipient, String recipientName, String fromName, String fromEmail, ArrayList attachments)
        {

            try
            {
                var apiKey = System.Environment.GetEnvironmentVariable("SENDGRID_APIKEY");
                var client = new SendGridClient(apiKey);

                var msg = new SendGridMessage()
                {
                    From = new EmailAddress(fromEmail, fromName),
                    Subject = subject,
                    PlainTextContent = content,
                };
                msg.AddTo(new EmailAddress(recipient, recipientName));

                foreach (string[] attachedFile in attachments)
                {
                    Attachment attachment = new Attachment();
                    attachment.Content = attachedFile[0];
                    attachment.Type = "application/pdf";
                    attachment.Filename = attachedFile[1];
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
