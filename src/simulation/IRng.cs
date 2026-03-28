namespace ElevenLegends.Simulation;

/// <summary>
/// Abstraction for random number generation. Always inject — never use global RNG.
/// Implementations must be seeded for deterministic/reproducible results.
/// </summary>
public interface IRng
{
    /// <summary>Returns a random integer in [minInclusive, maxInclusive].</summary>
    int NextInt(int minInclusive, int maxInclusive);

    /// <summary>Returns a random float in [min, max].</summary>
    float NextFloat(float min, float max);
}

/// <summary>
/// Seeded RNG implementation using System.Random for deterministic results.
/// </summary>
public sealed class SeededRng : IRng
{
    private readonly Random _random;

    public SeededRng(int seed)
    {
        _random = new Random(seed);
    }

    public int NextInt(int minInclusive, int maxInclusive)
    {
        return _random.Next(minInclusive, maxInclusive + 1);
    }

    public float NextFloat(float min, float max)
    {
        return (float)(_random.NextDouble() * (max - min) + min);
    }
}
