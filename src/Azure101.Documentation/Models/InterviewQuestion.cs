namespace Azure101.Documentation.Models;

/// <summary>
/// Represents a single interview question with difficulty level and related topics
/// </summary>
public class InterviewQuestion
{
    public string Question { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;

    public string? DetailedExplanation { get; set; }

    public string[] RelatedTopics { get; set; } = Array.Empty<string>();

    public string[] Tags { get; set; } = Array.Empty<string>();

    public InterviewDifficulty Difficulty { get; set; } = InterviewDifficulty.Intermediate;

    public string Category { get; set; } = string.Empty;
}

public enum InterviewDifficulty
{
    Beginner,
    Intermediate,
    Advanced
}
