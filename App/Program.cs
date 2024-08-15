using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Fallback;


var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddResiliencePipeline<string, string>("retry", opt =>
{
    opt.AddRetry(new RetryStrategyOptions<string>
    {
        Name = "Retry",
        BackoffType = DelayBackoffType.Constant,
        Delay = TimeSpan.FromSeconds(5),
        MaxRetryAttempts = 10,
        ShouldHandle = new PredicateBuilder<string>().HandleResult(result => result is not "true"),
        OnRetry = result =>
        {
            Console.WriteLine(result.Outcome.Result);
            return new ValueTask();
        },
    })
   .AddFallback(new FallbackStrategyOptions<string>
   {
       FallbackAction = result => new ValueTask<Outcome<string>>(Outcome.FromResult("true")),
       Name = "Fallback"
   })
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions<string>
    {
        FailureRatio = 0, // PERCENTUAL DE FALHAS PARA SER CONSIDERADO NO CIRCUITO
        BreakDuration = TimeSpan.FromSeconds(1),
        Name = "Circuit Breaker Retry",
        ShouldHandle = new PredicateBuilder<string>().HandleResult(result => result is not "true"),
        MinimumThroughput = 9, // QUANTIDADE DE ERRO DE EXECUCAO PARA ATIVAR O CIRCUITO
    })

    ;

});
builder.Services.AddSingleton<RetryService>();

var host = builder.Build();
((RetryService)host.Services.GetRequiredService(typeof(RetryService))).Execute();
