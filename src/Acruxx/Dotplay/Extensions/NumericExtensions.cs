namespace Dotplay.Extensions;

/// <summary>
/// The numeric extensions.
/// </summary>
public static class NumericExtensions
{
    /// <summary>
    /// Safes the division.
    /// </summary>
    /// <param name="numerator">The numerator.</param>
    /// <param name="denominator">The denominator.</param>
    /// <returns>A float.</returns>
    public static float SafeDivision(this float numerator, float denominator)
    {
        return (denominator == 0) ? 0 : numerator / denominator;
    }
}