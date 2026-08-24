using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentAPI.Models;
using RecruitmentAPI.Services;
using RecruitmentAPI.ViewModels;
using System.Security.Claims;

namespace RecruitmentAPI.Controllers
{
    public class AccountController : Controller
    {
        private readonly AccountService _accountService;

        public AccountController(AccountService accountService)
        {
            _accountService = accountService;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ServiceResult<User> result = await _accountService.LoginAsync(model.Email, model.Password);

            if (!result.Success || result.Data == null)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Đăng nhập thất bại.");
                return View(model);
            }

            await SignInUserAsync(result.Data, model.RememberMe);

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new RegisterViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ServiceResult<User> result = await _accountService.RegisterAsync(
                model.FullName,
                model.Email,
                model.Password);

            if (!result.Success)
            {
                if (result.ErrorCode == ErrorCodes.EmailAlreadyExists)
                {
                    ModelState.AddModelError(nameof(model.Email), result.Message ?? "Email đã tồn tại.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, result.Message ?? "Đăng ký thất bại.");
                }

                return View(model);
            }

            TempData["SuccessMessage"] = "Đăng ký thành công. Hãy đăng nhập bằng tài khoản vừa tạo.";
            return RedirectToAction(nameof(Login));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        private async Task SignInUserAsync(User user, bool rememberMe)
        {
            List<Claim> claims = new()
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email)
            };

            ClaimsIdentity identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            AuthenticationProperties properties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe
                    ? DateTimeOffset.UtcNow.AddDays(14)
                    : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                properties);
        }
    }
}
