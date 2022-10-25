using Dotplay.Utils;

namespace Dotplay.Network;

/// <summary>
/// Helps predict player simulation based on latency. (Only client-sided)
/// </summary>
public class ClientSimulationAdjuster : ISimulationAdjuster
{
    private readonly MovingAverage _actualTickLeadAvg;
    private int _estimatedMissedInputs;

    /// <summary>
    /// Initialize the simulation tick adjuster
    /// </summary>
    /// <param name="serverSendRate">Current server send rate</param>
    public ClientSimulationAdjuster(float serverSendRate)
    {
        this._actualTickLeadAvg = new MovingAverage((int)serverSendRate * 2);
    }

    /// <inheritdoc />
    public float AdjustedInterval { get; private set; } = 1.0f;

    /// <summary>
    /// Extrapolate based on latency what our client tick should be.
    /// </summary>
    /// <param name="physicsTime"></param>
    /// <param name="receivedServerTick"></param>
    /// <param name="serverLatencyMs"></param>
    /// <returns></returns>
    public uint GuessClientTick(float physicsTime, uint receivedServerTick, int serverLatencyMs)
    {
        float serverLatencySeconds = serverLatencyMs / 1000f;
        uint estimatedTickLead = (uint)(serverLatencySeconds * 1.5 / physicsTime) + 4;
        Logger.LogDebug(this, $"Initializing client with estimated tick lead of {estimatedTickLead}, ping: {serverLatencyMs}");
        return receivedServerTick + estimatedTickLead;
    }

    /// <summary>
    /// Just for debugging
    /// </summary>
    public void Monitoring()
    {
        Logger.SetDebugUI("cl_sim_factor", this.AdjustedInterval.ToString());
        Logger.SetDebugUI("cl_est_missed_inputs", this._estimatedMissedInputs.ToString());
    }

    /// <summary>
    /// Notify actual tick lead
    /// </summary>
    /// <param name="actualTickLead"></param>
    /// <param name="isDebug"></param>
    /// <param name="useLagReduction"></param>
    public void NotifyActualTickLead(int actualTickLead, bool isDebug, bool useLagReduction)
    {
        this._actualTickLeadAvg.Push(actualTickLead);

        // TODO: This logic needs significant tuning.

        // Negative lead means dropped inputs which is worse than buffering, so immediately move the
        // simulation forward.
        if (actualTickLead < 0)
        {
            // Logger.LogDebug(this, "Dropped an input, got an actual tick lead of " + actualTickLead);
            this._actualTickLeadAvg.ForceSet(actualTickLead);
            this._estimatedMissedInputs++;
        }

        var avg = this._actualTickLeadAvg.Average();
        if (avg <= -16)
        {
            this.AdjustedInterval = 0.875f;
        }
        else if (avg <= -8)
        {
            this.AdjustedInterval = 0.9375f;
        }
        else if (avg < 0)
        {
            this.AdjustedInterval = 0.75f;
        }
        else if (avg < 0)
        {
            this.AdjustedInterval = 0.96875f;
        }
        else if (avg >= 16)
        {
            this.AdjustedInterval = 1.125f;
        }
        else if (avg >= 8)
        {
            this.AdjustedInterval = 1.0625f;
        }
        else if (avg >= 4)
        {
            this.AdjustedInterval = 1.03125f;
        }
        else if (avg >= 2 && useLagReduction)
        {
            this.AdjustedInterval = 1.015625f;
        }
        else
        {
            this.AdjustedInterval = 1f;
        }
    }
}