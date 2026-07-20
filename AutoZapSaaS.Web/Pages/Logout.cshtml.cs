using AutoZapSaaS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutoZapSaaS.Web.Pages;

public class LogoutModel : PageModel
{
    /// <summary>Só via POST: um GET permitiria deslogar o lojista por um link ou img.</summary>
    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.FecharSessaoAsync();
        return RedirectToPage("/Login");
    }

    public IActionResult OnGet() => RedirectToPage("/Index");
}
