using System;

namespace Dotplay.Utils;

/// <summary>
/// The fixed timer.
/// </summary>
public class FixedTimer
{
    private readonly Action<float> _callback;
    private readonly float _fixedDelta;
    private readonly float _tickRate;
    private float _accumulator;
    private bool _running;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedTimer"/> class.
    /// </summary>
    /// <param name="tickRate">The tick rate.</param>
    /// <param name="callback">The callback.</param>
    public FixedTimer(float tickRate, Action<float> callback)
    {
        this._tickRate = tickRate;
        this._callback = callback;
        this._fixedDelta = 1.0f / this._tickRate;
    }

    /// <summary>
    /// Gets the lerp alpha.
    /// </summary>
    public float LerpAlpha => this._accumulator / this._fixedDelta;

    /// <summary>
    /// Starts the.
    /// </summary>
    public void Start()
    {
        this._accumulator = 0.0f;
        this._running = true;
    }

    /// <summary>
    /// Stops the.
    /// </summary>
    public void Stop()
    {
        this._running = false;
    }

    /// <summary>
    /// Updates the.
    /// </summary>
    /// <param name="dt">The dt.</param>
    public void Update(float dt)
    {
        this._accumulator += dt;
        while (this._accumulator >= this._fixedDelta)
        {
            if (!this._running)
            {
                return;
            }
            this._callback(this._fixedDelta);
            this._accumulator -= this._fixedDelta;
        }
    }
}