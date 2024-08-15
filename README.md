# Microsoft.Extensions.Resilience Package
O pacote Microsoft.Extensions.Resilience é destinado para criar e gerenciar políticas de tratativas de exceção que podem ocorrer durante o processamento de algum fluxo.
Internamente, ela utiliza o pacote Polly, que é bastante conhecido e consolidado para realizar essas configurações.

## Funcionamento
O pacote Microsoft.Extensions.Resilience permite que sejam criadas pipelines de políticas e que a mesma seja adicionada via Dependency Injection, facilitando a sua utilização dentro da aplicação. 
Além disso, é mantido a independência do gerenciamento de estado de cada política/pipeline.

## Exemplo
Neste exemplo, foram adicionadas 3 políticas dentro da pipeline:
- Retry
- Fallback
- CircuitBreaker

### Pipeline
Inicialmente, é necessário definir a pipeline, informando o nome e o tipo de retorno:

```csharp
builder.Services.AddResiliencePipeline<string, string>("retry")
```

### Retry
A tratativa de retry é adicionada dentro de uma pipeline. 
Essa tratativa é gerenciada a nível de execução, ou seja, cada execução de processamento terá o seu estado armazenado.

```csharp
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
    });
});
```

### Fallback
A tratativa de fallback é adicionada dentro de uma pipeline. 
Essa tratativa é gerenciada a nível de execução, ou seja, cada execução de processamento terá o seu estado armazenado.
Nesse exemplo, caso tratativas falhem, o valor default será retornado como resultado.

```csharp
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
   });
});
```

### Circuit Breaker
A tratativa de circuit breaker é adicionada dentro de uma pipeline. 
Essa tratativa é gerenciada a nível de pipeline, ou seja, caso haja falhas consecutivas que ativem o circuit breaker, qual execução que vier posteriormente será interrompida.

```csharp
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
    });
});
```

O pacote fornece outras opções de tratativas para serem adiconadas, cada uma com sua respectiva finalidade.

### Utilizando no service
Para utilizar a política adicionada, basta injetar a dependência no serviço e informar o nome de qual a pipeline desejada para utilização:

```csharp
public class RetryService
{
    private static int COUNT01 = 0;
    private static int COUNT02 = 0;
    private readonly ResiliencePipelineProvider<string> _pipelineResilience;

    public RetryService(ResiliencePipelineProvider<string> pipelineResilience)
    {
        _pipelineResilience = pipelineResilience;
    }

    public void Execute()
    {
        var pipeline = _pipelineResilience.GetPipeline<string>("retry");
        Parallel.Invoke
        (
            () => pipeline.Execute(() =>
            {
                COUNT01++;
                if (COUNT01 == 9) return "true"; // FINALIZA O PROCESSO COM SUCESSO
                Console.WriteLine("Execução 01 - COUNTER: " + COUNT01); 
                return "Execução 01"; // RETORNA STRING INVALIDA PARA REPROCESSAR NOVAMENTE
            })
            ,
            () => pipeline.Execute(() =>
            {
                COUNT02++;
                if (COUNT02 == 3) return "true"; // FINALIZA O PROCESSO COM SUCESSO
                Console.WriteLine("Execução 02 - COUNTER: " + COUNT02);
                return "Execução 02"; // RETORNA STRING INVALIDA PARA REPROCESSAR NOVAMENTE
            })
        );
    }
}
```
## Caso de uso
Adicionar políticas de tratativas é útil para inúmeros casos, como por exemplo:
- Realizar uma ação ou retornar um valor default de fallback caso dê algum erro no processamento
- Realizar novas tentativas de comunicação com outro serviço (por exemplo uma API) e não retornar um erro diretamente na primeira execução.
- Adicionar regras para validar se o token de autenticação ainda se mantém válido e caso retornar inválido, inserir na retentativa o processamento de obter um novo token. 
