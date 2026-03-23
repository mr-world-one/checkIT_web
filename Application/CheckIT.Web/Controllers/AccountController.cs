using CheckIT.Web.Models;
using CheckIT.Web.Services;
using CheckIT.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CheckIT.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAppLogger _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAppLogger logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User?.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var email = (model.Email ?? string.Empty).Trim();

        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Невірні дані");
                return View(model);
            }

            if (user.IsBlocked)
            {
                ModelState.AddModelError(string.Empty, "Акаунт заблоковано адміністратором");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                userName: user.UserName!,
                password: model.Password!,
                isPersistent: false,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                TempData["Success"] = $"Вхід виконано. Вітаємо, {user.FullName ?? user.Email}!";
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                _logger.Warn($"Identity lockout for '{email}'");
                ModelState.AddModelError(string.Empty, "Забагато спроб. Спробуйте пізніше.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Невірні дані");
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.Error($"Login server error for '{email}'", ex);
            ModelState.AddModelError(string.Empty, "Помилка сервера");
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User?.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var email = (model.Email ?? string.Empty).Trim();

        try
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = model.Name?.Trim(),
                IsBlocked = false,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password!);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, "User");

            await _signInManager.SignInAsync(user, isPersistent: false);

            TempData["Success"] = "Реєстрація успішна.";
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            _logger.Error($"Register server error for '{email}'", ex);
            ModelState.AddModelError(string.Empty, "Помилка реєстрації, спробуйте пізніше");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData["Success"] = "Ви вийшли з акаунту.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
