using System.Collections;
using System.Collections.Immutable;
using System.Text;

namespace HttpRequestSpy.Comparison;

public class ComparisonResult : IEnumerable<ComparisonResult.Difference>
{
    public record Difference(string Message, string? Expected = null, string? Actual = null)
    {
        public override string ToString()
        {
            if (Actual == null && Expected == null)
            {
                return Message;
            }

            return $"{Message}:\n - Expected : {Expected}\n - Actual : {Actual}";
        }
    }

    private ImmutableList<Difference> _differences = ImmutableList<Difference>.Empty;

    public static readonly ComparisonResult Empty = new();

    private ComparisonResult()
    {
    }

    public static ComparisonResult operator +(ComparisonResult result, Difference difference)
    {
        return new ComparisonResult
        {
            _differences = result._differences.Add(difference)
        };
    }

    public static ComparisonResult operator +(ComparisonResult result, ComparisonResult otherResult)
    {
        return new ComparisonResult
        {
            _differences = result._differences.AddRange(otherResult._differences)
        };
    }

    public static implicit operator ComparisonResult(Difference difference)
    {
        return new ComparisonResult
        {
            _differences = ImmutableList<Difference>.Empty.Add(difference)
        };
    }

    public static implicit operator ComparisonResult(List<Difference> differences)
    {
        return new ComparisonResult
        {
            _differences = ImmutableList<Difference>.Empty.AddRange(differences)
        };
    }

    public bool IsEmpty => _differences.IsEmpty;

    public IEnumerator<Difference> GetEnumerator()
    {
        return _differences.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();

        foreach (var difference in _differences)
        {
            stringBuilder.AppendLine($"- {difference}");
        }

        return stringBuilder.ToString();
    }
}