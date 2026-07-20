using AutoZapSaaS.Application.DTOs;
using AutoZapSaaS.Application.Validators;

namespace AutoZapSaaS.Tests;

/// <summary>
/// Os validators existiam mas nunca eram executados: AddValidatorsFromAssembly
/// so registra no container. A API aceitava cliente com nome vazio e conta com
/// senha de um caractere.
/// </summary>
public class ValidacaoTests
{
    private readonly RegisterValidator _cadastro = new();
    private readonly CreateCustomerValidator _cliente = new();
    private readonly CreateWebhookIntegrationValidator _integracao = new();

    [Theory]
    [InlineData("1")]
    [InlineData("abc")]
    [InlineData("1234567")]
    public void Senha_curta_e_rejeitada(string senha)
    {
        var r = _cadastro.Validate(Cadastro(senha: senha));
        Assert.False(r.IsValid);
    }

    [Theory]
    [InlineData("12345678")]   // so digitos
    [InlineData("abcdefgh")]   // so letras
    public void Senha_sem_letra_e_numero_e_rejeitada(string senha)
    {
        var r = _cadastro.Validate(Cadastro(senha: senha));
        Assert.False(r.IsValid);
    }

    [Fact]
    public void Senha_forte_e_aceita()
    {
        var r = _cadastro.Validate(Cadastro(senha: "SenhaForte123"));
        Assert.True(r.IsValid, string.Join("; ", r.Errors.Select(e => e.ErrorMessage)));
    }

    [Theory]
    [InlineData("nao-e-email")]
    [InlineData("")]
    public void Email_invalido_no_cadastro_e_rejeitado(string email)
    {
        Assert.False(_cadastro.Validate(Cadastro(email: email)).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]           // curto demais
    [InlineData("123456789012")]  // 12 digitos: nem CPF nem CNPJ
    public void Documento_com_tamanho_invalido_e_rejeitado(string documento)
    {
        Assert.False(_cadastro.Validate(Cadastro(documento: documento)).IsValid);
    }

    [Theory]
    [InlineData("11122233344")]      // CPF
    [InlineData("11222333000181")]   // CNPJ
    [InlineData("111.222.333-44")]   // CPF formatado
    public void Cpf_e_cnpj_validos_sao_aceitos(string documento)
    {
        var r = _cadastro.Validate(Cadastro(documento: documento));
        Assert.True(r.IsValid, string.Join("; ", r.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void Cliente_com_nome_vazio_e_rejeitado()
    {
        var r = _cliente.Validate(new CreateCustomerRequest("", "5511999999999", "", "Manual"));
        Assert.False(r.IsValid);
    }

    [Fact]
    public void Cliente_com_telefone_longo_demais_e_rejeitado()
    {
        // Antes isso passava e so estourava no banco, como erro 500.
        var r = _cliente.Validate(new CreateCustomerRequest("Teste", new string('9', 500), "", "Manual"));
        Assert.False(r.IsValid);
    }

    [Fact]
    public void Segredo_de_webhook_curto_e_rejeitado()
    {
        // Segredo curto torna a assinatura HMAC facil de forjar.
        Assert.False(_integracao.Validate(new CreateWebhookIntegrationRequest("Nuvemshop", "123")).IsValid);
        Assert.True(_integracao.Validate(new CreateWebhookIntegrationRequest("Nuvemshop", "segredo-longo-o-suficiente")).IsValid);
    }

    private static RegisterRequest Cadastro(
        string email = "loja@teste.com",
        string documento = "11122233344",
        string senha = "SenhaForte123") =>
        new("Loja", email, documento, "admin@teste.com", senha);
}
