#nullable enable

namespace HttpRequest.Spy.Assertions.HttpRequestAssertions;

internal abstract record UrlAssertion : IAssertHttpRequest
{
    private record AbsoluteUrlAssertion(string ExpectedUrl) : UrlAssertion
    {
        public override AssertionResult Matches(Uri? actualUri)
        {
            if (actualUri is null)
            {
                return AssertionResult.Failure("URL is null");
            }
                
            return actualUri.AbsoluteUri == ExpectedUrl ?
                AssertionResult.Success() : 
                AssertionResult.Failure("URL does not match", ExpectedUrl, actualUri.AbsoluteUri);
        }

        public override string ToExpectation()
        {
            return $"Expected URL : {ExpectedUrl}";
        }
    }
        
    public static UrlAssertion Absolute(string url) => new AbsoluteUrlAssertion(url); 

    private record RelativeUrlAssertion(string ExpectedUrl) : UrlAssertion
    {
        public override AssertionResult Matches(Uri? actualUri)
        {
            if (actualUri is null)
            {
                return AssertionResult.Failure("URL is null");
            }

            if (actualUri.LocalPath == ExpectedUrl)
            {
                return AssertionResult.Success();
            }

            var matchesUrlWithHeadingSlash = actualUri.LocalPath == $"/{ExpectedUrl}";
            if (matchesUrlWithHeadingSlash)
            {
                return AssertionResult.Success();
            }

            return AssertionResult.Failure("URL does not match", ExpectedUrl, actualUri.LocalPath);
        }
            
        public override string ToExpectation()
        {
            return $"Expected URL : {ExpectedUrl}";
        }
    }
        
    public static UrlAssertion Relative(string url) => new RelativeUrlAssertion(url);

    public static UrlAssertion From(string url)
    {
        if (Uri.TryCreate(url, UriKind.Relative, out _))
        {
            return Relative(url);
        }
        
        if (Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return Absolute(url);
        }

        throw new InvalidOperationException($"{url} is neither a relative or an absolute URL");
    } 

    public abstract AssertionResult Matches(Uri? actualUri);

    public AssertionResult Matches(AssertableHttpRequestMessage request)
    {
        return Matches(request.InnerRequest.RequestUri);
    }

    public abstract string ToExpectation();
}