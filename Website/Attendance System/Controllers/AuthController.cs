using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Attendance_System.Helpers;
using Attendance_System.Services.Interfaces;
using Attendance_System.UnitOfWork.Interfaces;
using Attendance_System.ViewModels.Auth;

namespace Attendance_System.Controllers
{
    public class AuthController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;

        public AuthController(IUnitOfWork unitOfWork, IAuthService authService, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _unitOfWork.BaseUsers.GetByUsernameAsync(model.Username);
            if (user == null || !PasswordHelper.VerifyPassword(model.Password, user.Password))
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            // Generate JWT Token
            var token = _authService.GenerateToken(user);

            // Append JWT cookie
            Response.Cookies.Append("jwt", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = model.RememberMe ? DateTime.UtcNow.AddDays(7) : DateTime.UtcNow.AddMinutes(1440)
            });

            TempData["SuccessMessage"] = $"Welcome back, {user.Username}!";
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // In a production system, look up the user by email
            // For now, let's simulate sending OTP and redirect
            var otp = new Random().Next(100000, 999999).ToString();
            
            // Try to send email (it will check appsettings config)
            await _emailService.SendForgotPasswordOtpAsync(model.Email, otp, "User", 15);

            // Stash OTP in TempData or Token database table for verification
            TempData["ForgotEmail"] = model.Email;
            TempData["ForgotOtp"] = otp; 
            TempData["SuccessMessage"] = $"An OTP verification code was sent to {model.Email}.";

            return RedirectToAction("VerifyOtp");
        }

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            var email = TempData["ForgotEmail"]?.ToString();
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }
            
            // Keep email stashed in TempData for the postback
            TempData.Keep("ForgotEmail");
            
            return View(new VerifyOtpViewModel { Email = email });
        }

        [HttpPost]
        public IActionResult VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var correctOtp = TempData["ForgotOtp"]?.ToString();
            if (model.Otp != correctOtp)
            {
                ModelState.AddModelError(nameof(model.Otp), "Incorrect OTP code.");
                TempData.Keep("ForgotEmail");
                return View(model);
            }

            TempData["SuccessMessage"] = "OTP verified successfully. You can now login using the temporary password sent to your email (simulated).";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt");
            return RedirectToAction("Login");
        }
    }
}
