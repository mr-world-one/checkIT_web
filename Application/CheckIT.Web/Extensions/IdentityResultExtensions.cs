using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CheckIT.Web.Extensions;

public static class IdentityResultExtensions
{
    public static void AddToModelState(this IdentityResult result, ModelStateDictionary modelState)
    {
        if (result.Succeeded) return;

        foreach (var e in result.Errors)
            modelState.AddModelError(string.Empty, e.Description);
    }
}
