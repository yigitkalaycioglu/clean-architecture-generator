using System.Reflection;
using System.Transactions;
using __Name__.Core.Utilities.Interceptors;
using __Name__.Core.Utilities.Results;
using Castle.DynamicProxy;

namespace __Name__.Core.Aspects.Autofac.Transaction;

/// <summary>
/// Metodu tek bir veritabanı işlemi (transaction) içinde çalıştırır. Hata fırlatılırsa ya da metot
/// başarısız bir <see cref="IResult"/> döndürürse tüm değişiklikler geri alınır.
/// <para>Kullanım: <c>[TransactionScopeAspect]</c></para>
/// <para>Not: Ortam (ambient) transaction desteği sağlayıcıya bağlıdır; SQL Server ve PostgreSQL destekler.</para>
/// </summary>
public sealed class TransactionScopeAspect : MethodInterceptionBaseAttribute
{
    private static readonly MethodInfo RunWithResultMethod =
        typeof(TransactionScopeAspect).GetMethod(nameof(RunWithResultAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    public override void Intercept(IInvocation invocation)
    {
        var resultType = invocation.GetAsyncResultType();
        if (resultType is not null)
        {
            invocation.ReturnValue = RunWithResultMethod.MakeGenericMethod(resultType).Invoke(null, [invocation]);
        }
        else if (invocation.Method.ReturnType == typeof(Task))
        {
            invocation.ReturnValue = RunAsync(invocation);
        }
        else
        {
            using var scope = CreateScope();
            invocation.Proceed();
            if (invocation.ReturnValue is not IResult { Success: false })
            {
                scope.Complete();
            }
        }
    }

    // Scope async metodun içinde açılır; böylece ortam transaction'ı çağıran koda sızmaz.
    private static async Task RunAsync(IInvocation invocation)
    {
        using var scope = CreateScope();
        invocation.Proceed();
        await ((Task)invocation.ReturnValue!).ConfigureAwait(false);
        scope.Complete();
    }

    private static async Task<T> RunWithResultAsync<T>(IInvocation invocation)
    {
        using var scope = CreateScope();
        invocation.Proceed();
        var result = await ((Task<T>)invocation.ReturnValue!).ConfigureAwait(false);
        if (result is not IResult { Success: false })
        {
            scope.Complete();
        }

        return result;
    }

    private static TransactionScope CreateScope() =>
        new(TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            TransactionScopeAsyncFlowOption.Enabled);
}
