using System.Text;

namespace HttpRequestSpy;

[Serializable]
public class HttpRequestSpyException : Exception
{
    public HttpRequestSpyException(string message) : base(message) { }
        
    public HttpRequestSpyException(StringBuilder messageBuilder) : base(messageBuilder.ToString()) { }
}