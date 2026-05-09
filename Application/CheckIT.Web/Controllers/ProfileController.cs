using CheckIT.Web.Extensions;
using CheckIT.Web.Models;
using CheckIT.Web.Utils;
using CheckIT.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CheckIT.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return Challenge();

        return View(new ProfileViewModel
        {
            Email = user.Email,
            FullName = user.FullName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await GetCurrentUserAsync();
        if (user is null)
            return Challenge();

        user.FullName = UserInput.NormalizeOptionalText(model.FullName);

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            updateResult.AddToModelState(ModelState);

            model.Email = user.Email;
            return View(model);
        }

        TempData["Success"] = "Профіль оновлено.";
        return RedirectToAction(nameof(Index));
    }

    private Task<ApplicationUser?> GetCurrentUserAsync()
        => _userManager.GetUserAsync(User);
}
