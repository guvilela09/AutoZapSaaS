using AutoZapSaaS.Web.Models;
using AutoZapSaaS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutoZapSaaS.Web.Pages;

public class CadastroModel : PageModel
{
    private readonly AutoZapApiClient _api;

    public CadastroModel(AutoZapApiClient api) => _api = api;

    [BindProperty]
    public CadastroViewModel Entrada { get; set; } = new();

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
            var login = await _api.RegistrarAsync(
                Entrada.NomeEmpresa, Entrada.EmailEmpresa, Entrada.Documento,
                Entrada.EmailAdmin, Entrada.SenhaAdmin);

            await HttpContext.AbrirSessaoAsync(login);
            return RedirectToPage("/Instancias", new { primeiroAcesso = true });
        }
        catch (ApiException ex)
        {
            Erro = ex.Message;
            return Page();
        }
    }
}
