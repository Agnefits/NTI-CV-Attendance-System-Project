using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Attendance_System.Services.Interfaces;

namespace Attendance_System.Services.Classes
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _baseUrl = _configuration["EmailSettings:BaseUrl"]?.TrimEnd('/') ?? "";
        }

        private string GetBaseTemplate(string title, string content, string? buttonText = null, string? buttonUrl = null)
        {
            return $@"
            <!DOCTYPE html>
            <html dir='ltr' lang='en'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <link href='https://fonts.googleapis.com/css2?family=Inter:wght@400;500;700;800&display=swap' rel='stylesheet'>
                <style>
                    body {{ 
                        font-family: 'Inter', 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; 
                        line-height: 1.6; 
                        color: #333; 
                        background-color: #f4f6f9; 
                        margin: 0; 
                        padding: 0; 
                    }}
                    .container {{ 
                        max-width: 600px; 
                        margin: 40px auto; 
                        background-color: #ffffff; 
                        border-radius: 15px; 
                        overflow: hidden; 
                        box-shadow: 0 10px 30px rgba(0,0,0,0.1); 
                    }}
                    .header {{ 
                        background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%); 
                        color: white; 
                        padding: 40px 20px; 
                        text-align: center; 
                    }}
                    .header h1 {{ 
                        margin: 0; 
                        font-size: 24px; 
                        font-weight: 800; 
                        letter-spacing: 1px; 
                    }}
                    .content {{ 
                        padding: 40px; 
                        text-align: center; 
                        background-color: white; 
                    }}
                    .welcome-text {{ 
                        color: #1e3c72; 
                        font-size: 18px; 
                        font-weight: 700; 
                        margin-bottom: 20px; 
                    }}
                    .otp-box {{ 
                        background-color: #f8f9fa; 
                        border: 2px dashed #1e3c72; 
                        border-radius: 10px; 
                        padding: 20px; 
                        margin: 30px 0; 
                        display: inline-block; 
                    }}
                    .otp-code {{ 
                        font-size: 36px; 
                        font-weight: 800; 
                        color: #1e3c72; 
                        letter-spacing: 8px; 
                        margin: 0; 
                    }}
                    .footer {{ 
                        text-align: center; 
                        padding: 20px; 
                        background-color: #f8f9fa; 
                        color: #6c757d; 
                        font-size: 13px; 
                    }}
                    .button {{ 
                        display: inline-block; 
                        padding: 12px 30px; 
                        background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%); 
                        color: white !important; 
                        text-decoration: none; 
                        border-radius: 8px; 
                        font-weight: 700; 
                        margin-top: 20px; 
                        transition: all 0.3s; 
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <img src='{_baseUrl}/img/logo.png' alt='Attendance System Logo' style='height: 50px; width: auto; margin-bottom: 10px;' />
                        <h1>Attendance System</h1>
                        <p style='margin-top: 10px; opacity: 0.9;'>{title}</p>
                    </div>
                    <div class='content'>
                        {content}
                        {(buttonText != null ? $"<a href='{buttonUrl}' class='button'>{buttonText}</a>" : "")}
                    </div>
                    <div class='footer'>
                        <p>This email was sent automatically from the Attendance System.</p>
                        <p>&copy; {DateTime.Now.Year} Attendance System. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>";
        }

        public async Task<bool> SendForgotPasswordOtpAsync(string recipientEmail, string otp, string userName, int expirationMinutes)
        {
            var subject = "Password Reset - Attendance System";
            var content = $@"
                <p class='welcome-text'>Hello {userName},</p>
                <p>We received a request to reset your password. Please use the code below to complete the process:</p>
                <div class='otp-box'>
                    <p class='otp-code'>{otp}</p>
                </div>
                <p style='font-size: 14px; color: #dc3545;'>This code is valid for <strong>{expirationMinutes} minutes</strong>. If you did not request this code, you can safely ignore this email.</p>";

            var body = GetBaseTemplate("Password Reset", content);
            return await SendEmailAsync(recipientEmail, subject, body);
        }
        public async Task<bool> SendEmailAsync(string recipientEmail, string subject, string body)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var smtpUsername = _configuration["EmailSettings:FromEmail"];
                var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
                var smtpFromName = _configuration["EmailSettings:FromName"] ?? "Attendance System";

                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(smtpUsername))
                {
                    Console.WriteLine("SmtpServer or FromEmail configuration is missing.");
                    return false;
                }

                using (var client = new SmtpClient(smtpServer))
                {
                    client.Port = smtpPort;
                    client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    client.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(smtpUsername, smtpFromName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(recipientEmail);

                    await client.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendEmailAsync(List<string> recipientEmails, string subject, string body)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var smtpUsername = _configuration["EmailSettings:FromEmail"];
                var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
                var smtpFromName = _configuration["EmailSettings:FromName"] ?? "Attendance System";

                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(smtpUsername))
                {
                    Console.WriteLine("SmtpServer or FromEmail configuration is missing.");
                    return false;
                }

                using (var client = new SmtpClient(smtpServer))
                {
                    client.Port = smtpPort;
                    client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    client.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(smtpUsername, smtpFromName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };

                    foreach (var recipientEmail in recipientEmails)
                        mailMessage.To.Add(recipientEmail);

                    await client.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                return false;
            }
        }
    }
}
