using System.Text;

namespace HttpRequest.Spy;

[Serializable]
public class HttpRequestSpyException : Exception
{
    public HttpRequestSpyException(string message) : base(message) { }
        
    public HttpRequestSpyException(StringBuilder messageBuilder) : base(messageBuilder.ToString()) { }
}