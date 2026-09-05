namespace FileBrowser.Application.Search.Semantic;

public interface ICosineSimilarity
{
    /// <summary>Returns the cosine of the angle between two vectors.</summary>
    /// <exception cref="ArgumentNullException">Either vector is null.</exception>
    /// <exception cref="ArgumentException">Vectors must have equal, nonzero lengths, finite components, and nonzero magnitudes.</exception>
    double Calculate(float[] left, float[] right);
}
