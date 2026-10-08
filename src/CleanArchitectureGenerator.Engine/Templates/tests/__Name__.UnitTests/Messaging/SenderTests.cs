using __Name__.Application;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Domain.Common;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace __Name__.UnitTests.Messaging;

/// <summary>İstek hattı: geçerli istek işleyiciye ulaşır, geçersiz istek doğrulamada durur.</summary>
public class SenderTests
{
    [Fact]
    public async Task Send_ValidRequest_ReachesHandler()
    {
        var handler = new EchoCommandHandler();
        await using var provider = CreateServices(handler);
        using var scope = provider.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new EchoCommand("merhaba"));

        Assert.True(result.IsSuccess);
        Assert.Equal("merhaba", result.Value);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task Send_InvalidRequest_StopsAtValidation()
    {
        var handler = new EchoCommandHandler();
        await using var provider = CreateServices(handler);
        using var scope = provider.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new EchoCommand(string.Empty));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.NotNull(result.Error.Details);
        Assert.Contains("text", result.Error.Details);
        Assert.Equal(0, handler.CallCount);
    }

    private static ServiceProvider CreateServices(EchoCommandHandler handler)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<IRequestHandler<EchoCommand, Result<string>>>(handler);
        services.AddSingleton<IValidator<EchoCommand>, EchoCommandValidator>();
        return services.BuildServiceProvider(validateScopes: true);
    }

    public sealed record EchoCommand(string Text) : ICommand<string>;

    public sealed class EchoCommandValidator : AbstractValidator<EchoCommand>
    {
        public EchoCommandValidator()
        {
            RuleFor(command => command.Text).NotEmpty();
        }
    }

    public sealed class EchoCommandHandler : ICommandHandler<EchoCommand, string>
    {
        public int CallCount { get; private set; }

        public Task<Result<string>> Handle(EchoCommand command, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult<Result<string>>(command.Text);
        }
    }
}
