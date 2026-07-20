using AutoZapSaaS.Web.Models;
using AutoZapSaaS.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoZapSaaS.Web.Controllers;

public class ClientesController : Controller
{
    private readonly AutoZapApiClient _api;

    public ClientesController(AutoZapApiClient api) => _api = api;

    public async Task<IActionResult> Index(string? busca)
    {
        var modelo = new ClientesViewModel { Busca = busca };

        try
        {
            var todos = await _api.ListarClientesAsync();

            modelo.Clientes = string.IsNullOrWhiteSpace(busca)
                ? todos
                : todos.Where(c =>
                        c.Name.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                        c.PhoneNumber.Contains(busca, StringComparison.OrdinalIgnoreCase))
                    .ToList();
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(string nome, string telefone, string email)
    {
        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(telefone))
        {
            TempData["Erro"] = "Nome e telefone são obrigatórios.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _api.CriarClienteAsync(nome.Trim(), SomenteDigitos(telefone), email?.Trim() ?? "", "Manual");
            TempData["Sucesso"] = "Cliente cadastrado.";
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remover(Guid id)
    {
        try
        {
            await _api.RemoverClienteAsync(id);
            TempData["Sucesso"] = "Cliente removido.";
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>A API espera o telefone só com dígitos.</summary>
    private static string SomenteDigitos(string valor) =>
        new(valor.Where(char.IsDigit).ToArray());
}
