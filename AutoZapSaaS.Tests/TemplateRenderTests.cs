using AutoZapSaaS.Domain.Entities;
using AutoZapSaaS.Domain.Enums;

namespace AutoZapSaaS.Tests;

/// <summary>
/// A substituicao de {{nome}} e {{detalhe}} e o que o lojista compra. Se falhar,
/// o consumidor final recebe a mensagem com os marcadores crus.
/// </summary>
public class TemplateRenderTests
{
    private static MessageTemplate Template(string corpo) =>
        new(Guid.NewGuid(), "Teste", EventType.OrderCreated, corpo);

    [Fact]
    public void Substitui_nome_e_detalhe()
    {
        var t = Template("Olá {{nome}}! Seu pedido foi confirmado. {{detalhe}}");

        var texto = t.Render("Joana", "Pedido #123");

        Assert.Equal("Olá Joana! Seu pedido foi confirmado. Pedido #123", texto);
    }

    [Fact]
    public void Substitui_o_mesmo_marcador_mais_de_uma_vez()
    {
        var t = Template("{{nome}}, confirmamos seu pedido. Obrigado, {{nome}}!");

        Assert.Equal("Ana, confirmamos seu pedido. Obrigado, Ana!", t.Render("Ana", ""));
    }

    [Fact]
    public void Nome_nulo_vira_tratamento_generico_em_vez_de_texto_quebrado()
    {
        // Webhook pode chegar sem o nome do comprador.
        var t = Template("Olá {{nome}}!");

        Assert.Equal("Olá Cliente!", t.Render(null!, null!));
    }

    [Fact]
    public void Detalhe_nulo_some_em_vez_de_imprimir_o_marcador()
    {
        var t = Template("Pedido confirmado. {{detalhe}}");

        Assert.Equal("Pedido confirmado. ", t.Render("Joana", null!));
    }

    [Fact]
    public void Texto_sem_marcador_e_enviado_como_esta()
    {
        var t = Template("Recebemos seu pedido!");

        Assert.Equal("Recebemos seu pedido!", t.Render("Joana", "x"));
    }

    [Fact]
    public void Nome_com_caracteres_especiais_nao_quebra_o_texto()
    {
        // Nome vindo de e-commerce pode ter acento, aspas e emoji.
        var t = Template("Olá {{nome}}!");

        Assert.Equal("Olá José \"Zé\" D'Ávila 🎉!", t.Render("José \"Zé\" D'Ávila 🎉", ""));
    }

    [Fact]
    public void Marcador_desconhecido_permanece_no_texto()
    {
        // Documenta o comportamento atual: so {{nome}} e {{detalhe}} sao trocados,
        // entao um marcador inventado pelo lojista chega cru ao consumidor.
        var t = Template("Olá {{nome}}, seu rastreio e {{codigo}}");

        Assert.Equal("Olá Ana, seu rastreio e {{codigo}}", t.Render("Ana", ""));
    }
}
