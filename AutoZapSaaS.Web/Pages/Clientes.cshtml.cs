using AutoZapSaaS.Web.Models;
using AutoZapSaaS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutoZapSaaS.Web.Pages;

public class ClientesModel : PageModel
{
    private readonly AutoZapApiClient _api;

    public ClientesModel(AutoZapApiClient api) => _api = api;

    public List<CustomerResponse> Clientes { get; private set; } = new();

    [BindProperty] public string Nome { get; set; } = string.Empty;
    [BindProperty] public string Telefone { get; set; } = string.Empty;
    [BindProperty] public string Email { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)] public string? Busca { get; set; }

    public string? Erro { get; set; }
    public string? Sucesso { get; set; }

    public async Task OnGetAsync() => await CarregarAsync();

    public async Task<IActionResult> OnPostCriarAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome) || string.IsNullOrWhiteSpace(Telefone))
        {
            Erro = "Nome e telefone são obrigatórios.";
            await CarregarAsync();
            return Page();
        }

        try
        {
            await _api.CriarClienteAsync(Nome.Trim(), SomenteDigitos(Telefone), Email.Trim(), "Manual");
            Sucesso = "Cliente cadastrado.";
            Nome = Telefone = Email = string.Empty;
        }
        catch (ApiException ex)
        {
            Erro = ex.Message;
        }

        await CarregarAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRemoverAsync(Guid id)
    {
        try
        {
            await _api.RemoverClienteAsync(id);
            Sucesso = "Cliente removido.";
        }
        catch (ApiException ex)
        {
            Erro = ex.Message;
        }

        await CarregarAsync();
        return Page();
    }

    private async Task CarregarAsync()
    {
        try
        {
            var todos = await _api.ListarClientesAsync();

            Clientes = string.IsNullOrWhiteSpace(Busca)
                ? todos
                : todos.Where(c =>
                        c.Name.Contains(Busca, StringComparison.OrdinalIgnoreCase) ||
                        c.PhoneNumber.Contains(Busca, StringComparison.OrdinalIgnoreCase))
                    .ToList();
        }
        catch (ApiException ex)
        {
            Erro ??= ex.Message;
        }
    }

    /// <summary>A API espera o telefone só com dígitos.</summary>
    private static string SomenteDigitos(string valor) =>
        new(valor.Where(char.IsDigit).ToArray());

    public static string FormatarTelefone(string telefone)
    {
        var d = new string(telefone.Where(char.IsDigit).ToArray());

        // 55 + DDD + 9 dígitos
        if (d.Length == 13 && d.StartsWith("55"))
            return $"+55 ({d[2..4]}) {d[4..9]}-{d[9..]}";

        if (d.Length == 11)
            return $"({d[..2]}) {d[2..7]}-{d[7..]}";

        return telefone;
    }
}
