#nullable enable
using System.Text.RegularExpressions;

namespace HttpRequest.Spy.Comparison;
    
public class ComparisonOptions
{
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once NotAccessedPositionalProperty.Local
    private record ExcludedPath(string _pattern)
    {
        private readonly Regex _regex = new(Clean(_pattern));

        private static string Clean(string pattern)
        {
            return pattern
                    .Replace(".", @"\.")
                    .Replace("[", @"\[")
                    .Replace("]", @"\]")
                    .Replace("*", ".*")
                ;
        }

        public bool Matches(string text)
        {   
            return _regex.IsMatch(text);
        }
    }
        
    private readonly List<ExcludedPath> _excludedPaths = new();
    
    public string? RootPath { get; private set; } 
            
    public ComparisonOptions Exclude(params string[] patterns)
    {
        _excludedPaths.AddRange(patterns.Select(pattern => new ExcludedPath(pattern)));
        return this;
    }

    public ComparisonOptions WithRootPath(string rootPath)
    {
        RootPath = rootPath;
        return this;
    }

    public bool IsExcluded(string path)
    {
        return _excludedPaths.Exists(excludedPath => excludedPath.Matches(path));
    }
}