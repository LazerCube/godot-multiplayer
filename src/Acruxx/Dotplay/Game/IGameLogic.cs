using Dotplay.Extensions;

namespace Dotplay.Game;

/// <summary>
/// Required interface for game logic
/// </summary>
public interface IGameLogic : IBaseComponent
{
    /// <summary>
    /// Service registry (which contains the service of the game logic)
    /// </summary>
    public TypeDictonary<IService> Services { get; }
}