using AutoZapSaaS.Application.DTOs;
using FluentValidation;

namespace AutoZapSaaS.Application.Validators;

public class SendMessageValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.InstanceId).NotEmpty();
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(4096);
    }
}

public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class CreateMessageTemplateValidator : AbstractValidator<CreateMessageTemplateRequest>
{
    public CreateMessageTemplateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EventType).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4096);
    }
}
