using Polly.Registry;

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