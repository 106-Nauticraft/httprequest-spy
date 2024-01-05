using HttpRequest.Spy.Comparison;

namespace HttpRequest.Spy.Assertions;

public abstract record AssertionResult
{
    public record SuccessResult : AssertionResult
    {
        public override AssertionResult And(AssertionResult result)
        {
            if (result is SuccessResult)
            {
                return Success();
            }

            return result;
        }
    };

    public static AssertionResult Success() => new SuccessResult();

    public record FailureResult(
        ComparisonResult ComparisonResult) : AssertionResult
    {
        public override AssertionResult And(AssertionResult result)
        {
            if (result is not FailureResult otherFailure)
            {
                return this;
            }

            return new FailureResult(
                ComparisonResult + otherFailure.ComparisonResult);
        }
    }

    public static AssertionResult Failure(string error, string? expected = null, string? actual = null) => 
        new FailureResult(new ComparisonResult.Difference(error, expected, actual));

    public abstract AssertionResult And(AssertionResult result);
}