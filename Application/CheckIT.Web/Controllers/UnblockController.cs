using CheckIT.Web.Models;
using CheckIT.Web.Services;
using CheckIT.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CheckIT.Web.Controllers;

public class UnblockController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UnblockRequestService _requests;

    public UnblockController(UserManager<ApplicationUser> userManager, UnblockRequestService requests)
    {
        _userManager = userManager;
        _requests = requests;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (!user.IsBlocked)
        {
            TempData["Info"] = "Ваш акаунт не заблоковано.";
            return RedirectToAction("Index", "Home");
        }

        ViewBag.HasOpen = await _requests.HasOpenRequestAsync(user.Id, ct);
        return View(new UnblockRequestCreateVm());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UnblockRequestCreateVm vm, CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (!user.IsBlocked)
        {
            TempData["Info"] = "Ваш акаунт не заблоковано.";
            return RedirectToAction("Index", "Home");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.HasOpen = await _requests.HasOpenRequestAsync(user.Id, ct);
            return View("Index", vm);
        }

        if (await _requests.HasOpenRequestAsync(user.Id, ct))
        {
            TempData["Info"] = "У вас вже є активний запит на розблокування.";
            return RedirectToAction(nameof(Index));
        }

        await _requests.CreateAsync(user.Id, vm.Message!, ct);
        TempData["Success"] = "Запит на розблокування відправлено адміну.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Public(string? email = null)
    {
        return View(new PublicUnblockRequestVm { Email = email });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Public(PublicUnblockRequestVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var email = (vm.Email ?? string.Empty).Trim();
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Користувача з таким email не знайдено");
            return View(vm);
        }

        if (!user.IsBlocked)
        {
            ModelState.AddModelError(string.Empty, "Цей акаунт не заблоковано");
            return View(vm);
        }

        if (await _requests.HasOpenRequestAsync(user.Id, ct))
        {
            TempData["Info"] = "Для цього акаунта вже є активний запит. Очікуйте відповідь адміністратора.";
            return RedirectToAction(nameof(Public), new { email });
        }

        await _requests.CreateAsync(user.Id, vm.Message!, ct);
        TempData["Success"] = "Запит на розблокування відправлено адміну.";
        return RedirectToAction("Login", "Account");
    }
}
