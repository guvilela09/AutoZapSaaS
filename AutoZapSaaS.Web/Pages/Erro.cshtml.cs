using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutoZapSaaS.Web.Pages;

/// <summary>
/// Pagina generica de erro. Nao expoe detalhe da excecao: o stack trace fica no
/// log do servidor, nao na tela do lojista.
/// </summary>
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ErroModel : PageModel
{
    public void OnGet() { }
}
