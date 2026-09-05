using FileBrowser.Application.Search.Semantic;

namespace FileBrowser.Application.Tests.Search;

public sealed class CosineSimilarityTests
{
    private readonly CosineSimilarity _sut = new();

    public static TheoryData<float[], float[], double> KnownVectors => new()
    {
        { [3, 4], [3, 4], 1 },
        { [3, 4], [-3, -4], -1 },
        { [1, 0], [0, 1], 0 },
        { [1, 1], [1, -1], 0 },
        { [1, 0], [1, 1], Math.Sqrt(0.5) },
        { [1, 0], [-1, 1], -Math.Sqrt(0.5) },
        // Dot product = 32; squared magnitudes = 14 and 77.
        { [1, 2, 3], [4, 5, 6], 32 / Math.Sqrt(14 * 77) },
        { [2], [7], 1 },
        { [2], [-7], -1 },
        { [float.MaxValue, float.MaxValue], [float.MaxValue, 0], Math.Sqrt(0.5) },
        { [float.Epsilon, float.Epsilon], [float.Epsilon, 0], Math.Sqrt(0.5) }
    };

    [Theory]
    [MemberData(nameof(KnownVectors))]
    public void Calculate_KnownVectors_ReturnsExpectedCosine(float[] left, float[] right, double expected)
    {
        var result = _sut.Calculate(left, right);

        Assert.Equal(expected, result, precision: 12);
        Assert.InRange(result, -1, 1);
        Assert.Equal(result, _sut.Calculate(right, left), precision: 12);
    }

    [Fact]
    public void Calculate_PositiveScaling_DoesNotChangeSimilarity()
    {
        var original = _sut.Calculate([1, -2, 3], [4, 5, -6]);
        var scaled = _sut.Calculate([10, -20, 30], [8, 10, -12]);

        Assert.Equal(original, scaled, precision: 12);
    }

    [Fact]
    public void Calculate_NegatingOneVector_ReversesSign()
    {
        var original = _sut.Calculate([1, -2, 3], [4, 5, -6]);
        var negated = _sut.Calculate([-1, 2, -3], [4, 5, -6]);

        Assert.Equal(-original, negated, precision: 12);
    }

    [Fact]
    public void Calculate_NullVector_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("left", () => _sut.Calculate(null!, [1]));
        Assert.Throws<ArgumentNullException>("right", () => _sut.Calculate([1], null!));
    }

    [Fact]
    public void Calculate_EmptyVectors_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _sut.Calculate([], []));
    }

    [Fact]
    public void Calculate_UnequalLengths_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _sut.Calculate([1], [1, 2]));
        Assert.Throws<ArgumentException>(() => _sut.Calculate([1, 2], [1]));
        Assert.Throws<ArgumentException>(() => _sut.Calculate([1], []));
        Assert.Throws<ArgumentException>(() => _sut.Calculate([], [1]));
    }

    [Fact]
    public void Calculate_ZeroMagnitude_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("left", () => _sut.Calculate([0, 0], [1, 2]));
        Assert.Throws<ArgumentException>("right", () => _sut.Calculate([1, 2], [0, 0]));
        Assert.Throws<ArgumentException>(() => _sut.Calculate([0], [0]));
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void Calculate_NonFiniteComponent_ThrowsArgumentException(float value)
    {
        Assert.Throws<ArgumentException>("left", () => _sut.Calculate([1, value], [1, 2]));
        Assert.Throws<ArgumentException>("right", () => _sut.Calculate([1, 2], [1, value]));
    }
}
