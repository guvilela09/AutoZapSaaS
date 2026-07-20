using AutoZapSaaS.Application.DTOs;
using FluentValidation;

namespace AutoZapSaaS.Application.Validators;

public class UpdateMessageTemplateValidator : AbstractValidator<UpdateMessageTemplateRequest>
{
    public UpdateMessageTemplateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EventType).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4096);
    }
}

public class CreateWebhookIntegrationValidator : AbstractValidator<CreateWebhookIntegrationRequest>
{
    public CreateWebhookIntegrationValidator()
    {
        RuleFor(x => x.Platform).NotEmpty();

        // Segredo curto demais torna a assinatura HMAC facil de forjar.
        RuleFor(x => x.Secret)
            .NotEmpty().WithMessage("Informe o segredo de assinatura da plataforma.")
            .MinimumLength(8).WithMessage("O segredo precisa ter ao menos 8 caracteres.")
            .MaximumLength(200);
    }
}

public class UpdateWebhookSecretValidator : AbstractValidator<UpdateWebhookSecretRequest>
{
    public UpdateWebhookSecretValidator()
    {
        RuleFor(x => x.Secret).NotEmpty().MinimumLength(8).MaximumLength(200);
    }
}

public class AssinarValidator : AbstractValidator<AssinarRequest>
{
    public AssinarValidator()
    {
        RuleFor(x => x.Tier).NotEmpty();
        RuleFor(x => x.FormaPagamento).NotEmpty();
    }
}

public class UpdateCustomerValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}

public class UpdateInstanceValidator : AbstractValidator<UpdateInstanceRequest>
{
    public UpdateInstanceValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Token).NotEmpty().MaximumLength(200);
    }
}
