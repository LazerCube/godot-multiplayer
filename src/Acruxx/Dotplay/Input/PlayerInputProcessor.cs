using System.Collections.Generic;
using Dotplay.Utils;
using PriorityQueue;

namespace Dotplay.Input;

/// <summary>
/// Processing inputs from an given queue
/// </summary>
public class PlayerInputProcessor
{
    private readonly MovingAverage _averageInputQueueSize = new(10);
    private readonly Dictionary<short, TickInput> _latestPlayerInput = new();
    private readonly SimplePriorityQueue<TickInput> _queue = new();

    /// <summary>
    /// Get an collection of the last inputs since an given tick and dequeue them
    /// </summary>
    /// <param name="worldTick"></param>
    public List<TickInput> DequeueInputsForTick(uint worldTick)
    {
        var ret = new List<TickInput>();
        while (this._queue.TryDequeue(out TickInput entry))
        {
            if (entry.WorldTick < worldTick)
            {
            }
            else if (entry.WorldTick == worldTick)
            {
                ret.Add(entry);
            }
            else
            {
                // We dequeued a future input, put it back in.
                this._queue.Enqueue(entry, entry.WorldTick);
                break;
            }
        }
        return ret;
    }

    /// <summary>
    /// Add new input to input queue
    /// </summary>
    /// <param name="command"></param>
    /// <param name="playerId"></param>
    /// <param name="lastAckedInputTick"></param>
    public void EnqueueInput(PlayerInput command, short playerId, uint lastAckedInputTick)
    {
        uint startIndex = lastAckedInputTick >= command.StartWorldTick
           ? lastAckedInputTick - command.StartWorldTick + 1
           : 0;

        // Scan for inputs which haven't been handled yet.
        for (int i = (int)startIndex; i < command.Inputs.Length; ++i)
        {
            // Apply inputs to the associated player controller and simulate the world.
            var worldTick = command.StartWorldTick + i;
            var tickInput = new TickInput
            {
                WorldTick = (uint)worldTick,
                RemoteViewTick = (uint)(worldTick - command.ClientWorldTickDeltas[i]),
                PlayerId = playerId,
                Inputs = command.Inputs[i],
            };
            this._queue.Enqueue(tickInput, worldTick);

            // Store the latest input in case the simulation needs to repeat missed frames.
            this._latestPlayerInput[playerId] = tickInput;
        }
    }

    /// <summary>
    /// Get the last player input tick
    /// </summary>
    /// <param name="playerId"></param>
    public uint GetLatestPlayerInputTick(short playerId)
    {
        return !this.TryGetLatestInput(playerId, out TickInput input) ? 0 : input.WorldTick;
    }

    /// <summary>
    /// Log stats for queue input
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="worldTick"></param>
    public void LogQueueStatsForPlayer(int playerId, uint worldTick)
    {
        int count = 0;
        foreach (var entry in this._queue)
        {
            if (entry.PlayerId == playerId && entry.WorldTick >= worldTick)
            {
                count++;
                worldTick++;
            }
        }

        this._averageInputQueueSize.Push(count);
        Logger.SetDebugUI("sv_avg_input_queue", this._averageInputQueueSize.ToString());
    }

    /// <summary>
    /// Try to get the last input
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="ret"></param>
    public bool TryGetLatestInput(short playerId, out TickInput ret)
    {
        return this._latestPlayerInput.TryGetValue(playerId, out ret);
    }
}