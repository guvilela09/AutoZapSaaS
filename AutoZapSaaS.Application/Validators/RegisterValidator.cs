using AutoZapSaaS.Application.DTOs;
using FluentValidation;

namespace AutoZapSaaS.Application.Validators;

/// <summary>
/// A API e a fronteira de seguranca de verdade: o painel valida no navegador,
/// mas qualquer um pode postar direto aqui. Sem estas regras, era possivel criar
/// conta com senha de um caractere e e-mail invalido.
/// </summary>
public class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Informe o nome da loja.")
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Informe o e-mail da loja.")
            .EmailAddress().WithMessage("E-mail da loja invalido.")
            .MaximumLength(200);

        RuleFor(x => x.Document)
            .NotEmpty().WithMessage("Informe o CPF ou CNPJ.")
            .Must(TerTamanhoDeCpfOuCnpj)
            .WithMessage("CPF deve ter 11 digitos e CNPJ 14.");

        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("Informe o e-mail de acesso.")
            .EmailAddress().WithMessage("E-mail de acesso invalido.")
            .MaximumLength(200);

        RuleFor(x => x.AdminPassword)
            .NotEmpty().WithMessage("Informe a senha.")
            .MinimumLength(8).WithMessage("A senha precisa ter ao menos 8 caracteres.")
            .MaximumLength(128)
            .Must(s => s.Any(char.IsLetter) && s.Any(char.IsDigit))
            .WithMessage("A senha precisa ter ao menos uma letra e um numero.");
    }

    private static bool TerTamanhoDeCpfOuCnpj(string documento)
    {
        var digitos = new string((documento ?? string.Empty).Where(char.IsDigit).ToArray());
        return digitos.Length is 11 or 14;
    }
}
