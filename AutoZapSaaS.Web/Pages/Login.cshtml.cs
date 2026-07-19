using System.ComponentModel.DataAnnotations;
using AutoZapSaaS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutoZapSaaS.Web.Pages;

public class LoginModel : PageModel
{
    private readonly AutoZapApiClient _api;

    public LoginModel(AutoZapApiClient api) => _api = api;

    [BindProperty]
    [Required(ErrorMessage = "Informe o e-mail")]
    [EmailAddress(ErrorMessage = "E-mail invalido")]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Informe a senha")]
    public string Senha { get; set; } = string.Empty;

    public string? Erro { get; set; }

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
            var login = await _api.LoginAsync(Email, Senha);
            await HttpContext.AbrirSessaoAsync(login);

            return RedirectToPage("/Index");
        }
        catch (ApiException ex)
        {
            // Credencial errada e indisponibilidade sao coisas diferentes para o lojista.
            Erro = ex.StatusCode == 401
                ? "E-mail ou senha invalidos."
                : ex.Message;

            return Page();
        }
    }
}
