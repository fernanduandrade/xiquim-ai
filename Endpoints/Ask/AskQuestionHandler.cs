using System.Runtime.CompilerServices;
using xiquim_api.Services;

namespace xiquim_api.Endpoints.Ask;

public class AskQuestionHandler(IAgentService agentService)
{
    public async IAsyncEnumerable<string> Generate(
        QuestionRequestDto request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var trecho in agentService.AskQuestion(request, cancellationToken))
        {
            if (!string.IsNullOrWhiteSpace(trecho))
                yield return trecho;
        }
    }
}