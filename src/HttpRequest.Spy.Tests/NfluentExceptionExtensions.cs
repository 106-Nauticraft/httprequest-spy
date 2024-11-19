using NFluent.Extensibility;
using NFluent.Kernel;

namespace HttpRequest.Spy.Tests;

public static class NfluentExceptionExtensions
{
    public static ILambdaExceptionCheck<T> WithMessage<T>(this ILambdaExceptionCheck<T> check, Predicate<string> predicate) where T : Exception
    {
        ExtensibilityHelper.BeginCheck(check as FluentSut<T>)
            .CantBeNegated("WithMessage")
            .SetSutName("exception")
            .CheckSutAttributes(sut => sut.Message, "message")
            .FailWhen(message => !predicate(message), "The {0} is not as expected.")
            .EndCheck();

        return check;
    }
}