using System.Collections.Generic;
using System.Threading.Tasks;

namespace Attendance_System.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string recipientEmail, string subject, string body);
        Task<bool> SendEmailAsync(List<string> recipientEmails, string subject, string body);
        Task<bool> SendForgotPasswordOtpAsync(string recipientEmail, string otp, string userName, int expirationMinutes);
    }
}
