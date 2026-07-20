using AutoZapSaaS.Web.Models;
using AutoZapSaaS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoZapSaaS.Web.Controllers;

public class ContaController : Controller
{
    private readonly AutoZapApiClient _api;

    public ContaController(AutoZapApiClient api) => _api = api;

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Painel");

        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel modelo)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        try
        {
            var login = await _api.LoginAsync(modelo.Email, modelo.Senha);
            await HttpContext.AbrirSessaoAsync(login);

            return RedirectToAction("Index", "Painel");
        }
        catch (ApiException ex)
        {
            // Credencial errada e indisponibilidade sao coisas diferentes para o lojista.
            ModelState.AddModelError(string.Empty,
                ex.StatusCode == 401 ? "E-mail ou senha invalidos." : ex.Message);

            return View(modelo);
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Cadastro()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Painel");

        return View(new CadastroViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cadastro(CadastroViewModel modelo)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        try
        {
            var login = await _api.RegistrarAsync(
                modelo.NomeEmpresa, modelo.EmailEmpresa, modelo.Documento,
                modelo.EmailAdmin, modelo.SenhaAdmin);

            await HttpContext.AbrirSessaoAsync(login);
            return RedirectToAction("Index", "Instancias", new { primeiroAcesso = true });
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }
    }

    /// <summary>Só via POST: um GET permitiria deslogar o lojista por um link ou img.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sair()
    {
        await HttpContext.FecharSessaoAsync();
        return RedirectToAction(nameof(Login));
    }
}
