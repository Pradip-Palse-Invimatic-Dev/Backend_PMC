using FluentEmail.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MyWebApp.Models;

namespace MyWebApp.Api.Services
{
    public class EmailService
    {
        private readonly IFluentEmailFactory _emailFactory;
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;

        public EmailService(
            IFluentEmailFactory emailFactory,
            ILogger<EmailService> logger,
            IConfiguration configuration)
        {
            _emailFactory = emailFactory ?? throw new ArgumentNullException(nameof(emailFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task SendEmailAsync(string recipientEmail, string subject, string body)
        {
            try
            {
                _logger.LogInformation("Attempting to send email to {RecipientEmail}", recipientEmail);
                var template = body;
                var emailSubject = subject;

                var email = _emailFactory
                    .Create()
                    .To(recipientEmail)
                    .Subject(emailSubject)
                    .Body(template, isHtml: true);


                var sendResponse = await email.SendAsync();

                if (!sendResponse.Successful)
                {
                    var errors = string.Join(", ", sendResponse.ErrorMessages);
                    _logger.LogError("Failed to send email to {RecipientEmail}. Errors: {Errors}",
                        recipientEmail, errors);
                    throw new Exception($"Failed to send email: {errors}");
                }

                _logger.LogInformation("Email sent successfully to {RecipientEmail}", recipientEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {RecipientEmail}", recipientEmail);
                throw new Exception($"Failed to send email: {ex.Message}", ex);
            }
        }

        public async Task SendEmailWithAttachmentsAsync(string recipientEmail, string subject, string body, List<EmailAttachment> attachments)
        {
            try
            {
                _logger.LogInformation("Attempting to send email with {AttachmentCount} attachments to {RecipientEmail}",
                    attachments?.Count ?? 0, recipientEmail);

                var email = _emailFactory
                    .Create()
                    .To(recipientEmail)
                    .Subject(subject)
                    .Body(body, isHtml: true);

                // Add attachments if provided
                if (attachments != null && attachments.Any())
                {
                    foreach (var attachment in attachments)
                    {
                        if (attachment.Content != null && attachment.Content.Length > 0)
                        {
                            email.Attach(new FluentEmail.Core.Models.Attachment
                            {
                                Data = new MemoryStream(attachment.Content),
                                Filename = attachment.FileName,
                                ContentType = attachment.ContentType
                            });
                            _logger.LogDebug("Added attachment: {FileName} ({Size} bytes)",
                                attachment.FileName, attachment.Content.Length);
                        }
                    }
                }

                var sendResponse = await email.SendAsync();

                if (!sendResponse.Successful)
                {
                    var errors = string.Join(", ", sendResponse.ErrorMessages);
                    _logger.LogError("Failed to send email with attachments to {RecipientEmail}. Errors: {Errors}",
                        recipientEmail, errors);
                    throw new Exception($"Failed to send email with attachments: {errors}");
                }

                _logger.LogInformation("Email with {AttachmentCount} attachments sent successfully to {RecipientEmail}",
                    attachments?.Count ?? 0, recipientEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email with attachments to {RecipientEmail}", recipientEmail);
                throw new Exception($"Failed to send email with attachments: {ex.Message}", ex);
            }
        }
    }
}