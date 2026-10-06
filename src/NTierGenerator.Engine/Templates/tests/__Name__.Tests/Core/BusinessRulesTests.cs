using __Name__.Core.Utilities.Business;
using __Name__.Core.Utilities.Results;

namespace __Name__.Tests.Core;

public class BusinessRulesTests
{
    [Fact]
    public void Run_AllRulesSucceed_ReturnsNull()
    {
        var result = BusinessRules.Run(new SuccessResult(), new SuccessResult());

        Assert.Null(result);
    }

    [Fact]
    public void Run_SomeRulesFail_ReturnsFirstFailure()
    {
        var firstError = new ErrorResult("ilk hata");

        var result = BusinessRules.Run(new SuccessResult(), firstError, new ErrorResult("ikinci hata"));

        Assert.Same(firstError, result);
    }

    [Fact]
    public async Task RunAsync_StopsAtFirstFailure()
    {
        var executedRuleCount = 0;

        Task<IResult> Rule(bool success)
        {
            executedRuleCount++;
            return Task.FromResult<IResult>(success ? new SuccessResult() : new ErrorResult("hata"));
        }

        var result = await BusinessRules.RunAsync(() => Rule(true), () => Rule(false), () => Rule(true));

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(2, executedRuleCount);
    }
}
