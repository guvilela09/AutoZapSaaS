using System.ComponentModel.DataAnnotations;

namespace AutoZapSaaS.Web.Models;

// ViewModels do painel. Separados dos contratos da API (ApiModels) porque
// carregam validacao e estado de tela — coisas que a API nao conhece.

public class LoginViewModel
{
    [Required(ErrorMessage = "Informe o e-mail")]
    [EmailAddress(ErrorMessage = "E-mail invalido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha")]
    public string Senha { get; set; } = string.Empty;
}

public class CadastroViewModel
{
    [Required(ErrorMessage = "Informe o nome da loja")]
    public string NomeEmpresa { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail da loja")]
    [EmailAddress(ErrorMessage = "E-mail invalido")]
    public string EmailEmpresa { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o CPF ou CNPJ")]
    public string Documento { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail de acesso")]
    [EmailAddress(ErrorMessage = "E-mail invalido")]
    public string EmailAdmin { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha")]
    [MinLength(8, ErrorMessage = "A senha precisa ter ao menos 8 caracteres")]
    public string SenhaAdmin { get; set; } = string.Empty;
}

public class PainelViewModel
{
    public AssinaturaResponse? Assinatura { get; set; }
    public List<InstanceResponse> Instancias { get; set; } = new();
    public int TotalClientes { get; set; }
    public int MensagensEnviadas { get; set; }
    public int MensagensComFalha { get; set; }

    public bool TemNumeroConectado =>
        Instancias.Any(i => i.Status.Equals("Connected", StringComparison.OrdinalIgnoreCase));

    public bool TemIntegracao { get; set; }
    public bool TemTemplate { get; set; }

    /// <summary>Percentual da cota usada, para o medidor da tela.</summary>
    public int PercentualUsado()
    {
        if (Assinatura is null || Assinatura.MaxMensagensPorMes == 0) return 0;

        var usado = Assinatura.MaxMensagensPorMes - Assinatura.MensagensRestantes;
        return (int)Math.Round(usado * 100.0 / Assinatura.MaxMensagensPorMes);
    }
}


