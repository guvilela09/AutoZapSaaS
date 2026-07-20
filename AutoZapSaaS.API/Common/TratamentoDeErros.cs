using System.Text.Json;
using AutoZapSaaS.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace AutoZapSaaS.API.Common;

/// <summary>
/// Ultima barreira de erro da API. Garante que nenhuma excecao escape como stack
/// trace na resposta — um DbUpdateException chegava ao cliente expondo nome de
/// tabela, coluna e constraint — e que cada falha tenha o status correto em vez
/// de virar 500 generico.
/// </summary>
public class TratamentoDeErros
{
    private readonly RequestDelegate _proxima;
    private readonly ILogger<TratamentoDeErros> _logger;

    public TratamentoDeErros(RequestDelegate proxima, ILogger<TratamentoDeErros> logger)
    {
        _proxima = proxima;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await _proxima(contexto);
        }
        catch (Exception ex)
        {
            if (contexto.Response.HasStarted)
            {
                // Resposta ja em transito: nao da para trocar o status.
                _logger.LogError(ex, "Erro apos o inicio da resposta em {Rota}", contexto.Request.Path);
                throw;
            }

            var (status, mensagem) = Traduzir(ex);

            if (status >= 500)
                _logger.LogError(ex, "Erro nao tratado em {Rota}", contexto.Request.Path);
            else
                _logger.LogWarning(ex, "Requisicao rejeitada em {Rota}: {Mensagem}", contexto.Request.Path, mensagem);

            contexto.Response.Clear();
            contexto.Response.StatusCode = status;
            contexto.Response.ContentType = "application/json";

            await contexto.Response.WriteAsync(JsonSerializer.Serialize(new { error = mensagem }));
        }
    }

    private static (int Status, string Mensagem) Traduzir(Exception ex) => ex switch
    {
        PlanLimitException e => (StatusCodes.Status402PaymentRequired, e.Message),

        UnauthorizedAccessException e => (StatusCodes.Status401Unauthorized, e.Message),

        // Falha de servico externo e nossa, nao do pedido do cliente. A mensagem
        // crua fica no log: pode conter detalhe interno do fornecedor.
        EvolutionApiException => (StatusCodes.Status502BadGateway,
            "Nao foi possivel falar com o servico de WhatsApp. Tente novamente."),

        AsaasException => (StatusCodes.Status502BadGateway,
            "Nao foi possivel falar com o servico de cobranca. Tente novamente."),

        // Violacao de constraint quase sempre e dado invalido que passou pela
        // validacao. Nome de tabela e coluna nao vao para a resposta.
        DbUpdateException => (StatusCodes.Status400BadRequest,
            "Os dados enviados nao puderam ser gravados. Verifique os campos e tente novamente."),

        ArgumentException e => (StatusCodes.Status400BadRequest, e.Message),
        InvalidOperationException e => (StatusCodes.Status400BadRequest, e.Message),

        _ => (StatusCodes.Status500InternalServerError,
            "Ocorreu um erro inesperado. Tente novamente.")
    };
}
