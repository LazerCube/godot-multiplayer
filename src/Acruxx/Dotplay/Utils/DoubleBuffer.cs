namespace Dotplay.Utils;

/// <summary>
/// The double buffer.
/// </summary>
public class DoubleBuffer<T>
{
    private readonly T[] _values = new T[2];
    private int _swapIndex = 0;

    /// <summary>
    /// News the.
    /// </summary>
    /// <returns>A T.</returns>
    public T New()
    {
        return this._values[this._swapIndex];
    }

    /// <summary>
    /// Olds the.
    /// </summary>
    /// <returns>A T.</returns>
    public T Old()
    {
        return this._values[this.NextSwapIndex()];
    }

    /// <summary>
    /// Pushes the.
    /// </summary>
    /// <param name="value">The value.</param>
    public void Push(T value)
    {
        this._swapIndex = this.NextSwapIndex();
        this._values[this._swapIndex] = value;
    }

    /// <summary>
    /// Nexts the swap index.
    /// </summary>
    /// <returns>An int.</returns>
    private int NextSwapIndex()
    {
        return this._swapIndex == 0 ? 1 : 0;
    }
}