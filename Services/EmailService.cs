using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using USPTOQueryBuilder.Models;

namespace USPTOQueryBuilder.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendQueryCompletionEmail(PatentQuery query, string downloadUrl)
        {
            try
            {
                var smtpClient = new SmtpClient(_configuration["Email:SmtpServer"])
                {
                    Port = int.Parse(_configuration["Email:Port"]),
                    Credentials = new NetworkCredential(
                        _configuration["Email:Username"],
                        _configuration["Email:Password"]
                    ),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_configuration["Email:FromAddress"], "USPTO Query Builder"),
                    Subject = $"Your USPTO Patent Query Results - {query.QueryId}",
                    IsBodyHtml = true,
                    Body = GetEmailBody(query, downloadUrl)
                };

                mailMessage.To.Add(query.UserEmail);

                await smtpClient.SendMailAsync(mailMessage);

                // Update query status
                query.Status = QueryStatus.EmailSent;

                _logger.LogInformation($"Email sent successfully to {query.UserEmail} for query {query.QueryId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email for query {query.QueryId}");
                throw;
            }
        }

        private string GetEmailBody(PatentQuery query, string downloadUrl)
        {
            var fileSizeMB = query.ResultFileSize.HasValue
                ? $"{query.ResultFileSize.Value / (1024.0 * 1024.0):F2} MB"
                : "Unknown";

            return $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>USPTO Patent Query Results</h2>
                    <p>Your query has been completed successfully!</p>
                    
                    <h3>Query Details:</h3>
                    <ul>
                        <li><strong>Query ID:</strong> {query.QueryId}</li>
                        <li><strong>Category:</strong> {query.PrimaryCategory}</li>
                        <li><strong>Query Type:</strong> {query.QueryType ?? "Custom"}</li>
                        <li><strong>Submitted:</strong> {query.CreatedAt:yyyy-MM-dd HH:mm} UTC</li>
                        <li><strong>File Size:</strong> {fileSizeMB}</li>
                    </ul>
                    
                    <h3>Download Your Results:</h3>
                    <p>
                        <a href='{downloadUrl}' 
                           style='background-color: #007bff; color: white; padding: 10px 20px; 
                                  text-decoration: none; border-radius: 5px; display: inline-block;'>
                            Download CSV File
                        </a>
                    </p>
                    
                    <p><em>Note: This download link will expire in 7 days.</em></p>
                    
                    <hr/>
                    <p style='font-size: 12px; color: #666;'>
                        This is an automated email from the USPTO Query Builder. 
                        Please do not reply to this email.
                    </p>
                </body>
                </html>";
        }

        public async Task SendQueryFailureEmail(PatentQuery query, string errorMessage)
        {
            try
            {
                var smtpClient = new SmtpClient(_configuration["Email:SmtpServer"])
                {
                    Port = int.Parse(_configuration["Email:Port"]),
                    Credentials = new NetworkCredential(
                        _configuration["Email:Username"],
                        _configuration["Email:Password"]
                    ),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_configuration["Email:FromAddress"], "USPTO Query Builder"),
                    Subject = $"USPTO Query Failed - {query.QueryId}",
                    IsBodyHtml = true,
                    Body = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <h2>USPTO Patent Query Failed</h2>
                            <p>Unfortunately, your query could not be completed.</p>
                            
                            <h3>Query Details:</h3>
                            <ul>
                                <li><strong>Query ID:</strong> {query.QueryId}</li>
                                <li><strong>Error:</strong> {errorMessage}</li>
                            </ul>
                            
                            <p>Common reasons for query failure:</p>
                            <ul>
                                <li>Result size exceeds 1GB limit</li>
                                <li>Invalid search criteria</li>
                                <li>API timeout due to complex query</li>
                            </ul>
                            
                            <p>Please try refining your search criteria and submitting again.</p>
                        </body>
                        </html>"
                };

                mailMessage.To.Add(query.UserEmail);
                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation($"Failure email sent to {query.UserEmail} for query {query.QueryId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send failure email for query {query.QueryId}");
            }
        }
    }
}