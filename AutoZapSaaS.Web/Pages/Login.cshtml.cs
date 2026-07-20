using AutoZapSaaS.Web.Models;
using AutoZapSaaS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutoZapSaaS.Web.Pages;

public class LoginModel : PageModel
{
    private readonly AutoZapApiClient _api;

    public LoginModel(AutoZapApiClient api) => _api = api;

    [BindProperty]
    public LoginViewModel Entrada { get; set; } = new();

    public string? Erro { get; private set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var login = await _api.LoginAsync(Entrada.Email, Entrada.Senha);
            await HttpContext.AbrirSessaoAsync(login);

            return RedirectToPage("/Index");
        }
        catch (ApiException ex)
        {
            // Credencial errada e indisponibilidade sao coisas diferentes para o lojista.
            Erro = ex.StatusCode == 401 ? "E-mail ou senha inválidos." : ex.Message;
            return Page();
        }
    }
}
