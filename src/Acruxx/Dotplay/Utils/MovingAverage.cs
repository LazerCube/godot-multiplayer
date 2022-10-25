using System.Linq;

namespace Dotplay.Utils;

/// <summary>
/// The moving average.
/// </summary>
internal class MovingAverage
{
    private readonly CircularBuffer<float> _buffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="MovingAverage"/> class.
    /// </summary>
    /// <param name="windowSize">The window size.</param>
    public MovingAverage(int windowSize)
    {
        this._buffer = new CircularBuffer<float>(windowSize);
    }

    /// <summary>
    /// Averages the.
    /// </summary>
    /// <returns>A float.</returns>
    public float Average()
    {
        return this._buffer.Sum() / this._buffer.Count();
    }

    /// <summary>
    /// Forces the set.
    /// </summary>
    /// <param name="value">The value.</param>
    public void ForceSet(float value)
    {
        for (int i = 0; i < this._buffer.Size; i++)
        {
            this.Push(value);
        }
    }

    /// <summary>
    /// Pushes the.
    /// </summary>
    /// <param name="value">The value.</param>
    public void Push(float value)
    {
        this._buffer.PushFront(value);
    }
}