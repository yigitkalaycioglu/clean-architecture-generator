//#if LocalAuth
using __Name__.Application.Abstractions.Authentication;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Domain.Common;
using FluentValidation;

namespace __Name__.Application.Authentication;

public sealed record ConfirmEmailCommand(string UserId, string Code) : ICommand
{
    public override string ToString() => nameof(ConfirmEmailCommand);
}

internal sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty().MaximumLength(64);
        RuleFor(command => command.Code).ValidCode();
    }
}

internal sealed class ConfirmEmailCommandHandler(IIdentityService identityService) : ICommandHandler<ConfirmEmailCommand>
{
    public Task<Result> Handle(ConfirmEmailCommand command, CancellationToken cancellationToken) =>
        identityService.ConfirmEmailAsync(command.UserId, command.Code, cancellationToken);
}
//#endif
