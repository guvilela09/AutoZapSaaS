using System.ComponentModel.DataAnnotations;
using AutoZapSaaS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutoZapSaaS.Web.Pages;

public class CadastroModel : PageModel
{
    private readonly AutoZapApiClient _api;

    public CadastroModel(AutoZapApiClient api) => _api = api;

    [BindProperty]
    [Required(ErrorMessage = "Informe o nome da empresa")]
    public string NomeEmpresa { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Informe o e-mail da empresa")]
    [EmailAddress(ErrorMessage = "E-mail invalido")]
    public string EmailEmpresa { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Informe o CPF ou CNPJ")]
    public string Documento { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Informe o e-mail de acesso")]
    [EmailAddress(ErrorMessage = "E-mail invalido")]
    public string EmailAdmin { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Informe a senha")]
    [MinLength(8, ErrorMessage = "A senha precisa ter ao menos 8 caracteres")]
    public string SenhaAdmin { get; set; } = string.Empty;

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
            var login = await _api.RegistrarAsync(
                NomeEmpresa, EmailEmpresa, Documento, EmailAdmin, SenhaAdmin);

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
