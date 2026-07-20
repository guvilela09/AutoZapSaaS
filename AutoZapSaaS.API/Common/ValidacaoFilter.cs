using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AutoZapSaaS.API.Common;

/// <summary>
/// Executa os validators do FluentValidation antes da action.
///
/// AddValidatorsFromAssembly apenas registra os validators no container — nao os
/// executa. Sem este filtro eles eram codigo morto: a API aceitava cliente com
/// nome vazio, e-mail invalido e telefone de 500 caracteres (que so estourava
/// depois, como erro 500 do banco).
/// </summary>
public class ValidacaoFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _provedor;

    public ValidacaoFilter(IServiceProvider provedor) => _provedor = provedor;

    public async Task OnActionExecutionAsync(ActionExecutingContext contexto, ActionExecutionDelegate proxima)
    {
        foreach (var argumento in contexto.ActionArguments.Values)
        {
            if (argumento is null) continue;

            var tipoDoValidador = typeof(IValidator<>).MakeGenericType(argumento.GetType());

            if (_provedor.GetService(tipoDoValidador) is not IValidator validador)
                continue;

            var resultado = await validador.ValidateAsync(
                new ValidationContext<object>(argumento));

            if (resultado.IsValid) continue;

            var erros = resultado.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            contexto.Result = new BadRequestObjectResult(new ValidationProblemDetails(erros));
            return;
        }

        await proxima();
    }
}
