using Dotplay.Utils;

namespace Dotplay.Physics;

/// <summary>
/// Helper class for interpolations by ticks
/// </summary>
public class InterpolationController
{
    private readonly DoubleBuffer<float> _timestampBuffer = new();
    private float _totalFixedTime;
    private float _totalTime;

    /// <summary>
    /// Contains the current interpolation factor
    /// </summary>
    public static float InterpolationFactor { get; private set; } = 1f;

    /// <summary>
    /// Explicits the fixed update.
    /// </summary>
    /// <param name="dt">The dt.</param>
    public void ExplicitFixedUpdate(float dt)
    {
        this._totalFixedTime += dt;
        this._timestampBuffer.Push(this._totalFixedTime);
    }

    /// <summary>
    /// Explicits the update.
    /// </summary>
    /// <param name="dt">The dt.</param>
    public void ExplicitUpdate(float dt)
    {
        this._totalTime += dt;

        float newTime = this._timestampBuffer.New();
        float oldTime = this._timestampBuffer.Old();

        InterpolationFactor = newTime != oldTime ? (this._totalTime - newTime) / (newTime - oldTime) : 1;
    }
}