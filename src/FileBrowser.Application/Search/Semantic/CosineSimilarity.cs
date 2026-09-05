namespace FileBrowser.Application.Search.Semantic;

public sealed class CosineSimilarity : ICosineSimilarity
{
    public double Calculate(float[] left, float[] right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left.Length == 0)
        {
            throw new ArgumentException("Vectors must not be empty.", nameof(left));
        }

        if (left.Length != right.Length)
        {
            throw new ArgumentException("Vectors must have equal lengths.", nameof(right));
        }

        double dot = 0, leftNorm = 0, rightNorm = 0;
        for (var i = 0; i < left.Length; i++)
        {
            if (!float.IsFinite(left[i]))
            {
                throw new ArgumentException("Vector components must be finite.", nameof(left));
            }

            if (!float.IsFinite(right[i]))
            {
                throw new ArgumentException("Vector components must be finite.", nameof(right));
            }

            // Promote before multiplication to avoid float overflow and underflow.
            dot += (double)left[i] * right[i];
            leftNorm += (double)left[i] * left[i];
            rightNorm += (double)right[i] * right[i];
        }

        if (leftNorm == 0)
        {
            throw new ArgumentException("Vector magnitude must be nonzero.", nameof(left));
        }

        if (rightNorm == 0)
        {
            throw new ArgumentException("Vector magnitude must be nonzero.", nameof(right));
        }

        return Math.Clamp(dot / (Math.Sqrt(leftNorm) * Math.Sqrt(rightNorm)), -1, 1);
    }
}
