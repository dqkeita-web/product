using System.Threading.Tasks;

namespace FindAncestor.Voice.LLM
{
    public class PythonLlmClient
    {
        public Task<string> GenerateAsync(string text)
        {
            return Task.FromResult(text);
        }
    }
}