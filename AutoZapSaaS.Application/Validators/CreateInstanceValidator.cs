using AutoZapSaaS.Application.DTOs;
using FluentValidation;

namespace AutoZapSaaS.Application.Validators;

public class CreateInstanceValidator : AbstractValidator<CreateInstanceRequest>
{
    public CreateInstanceValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.SessionName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Token).NotEmpty().MaximumLength(500);
    }
}
