namespace CodeFamily.Core.Enums
{
    public enum RepoStatus
    {
        Queued,
        Cloning,
        AnalyzingDependencies,
        CalculatingOwnership,
        Ready,
        Error
    }
}