using System;
using Godot;

namespace Dotplay.Utils;

/// <summary>
/// The vector extension.
/// </summary>
public static class VectorExtension
{
    /// <summary>
    /// Gradually changes a vector towards a desired goal over time.
    /// </summary>
    /// <param name="current"></param>
    /// <param name="target"></param>
    /// <param name="currentVelocity"></param>
    /// <param name="smoothTime"></param>
    /// <param name="deltaTime"></param>
    public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime, float deltaTime)
    {
        const float maxSpeed = Mathf.Inf;
        return SmoothDamp(current, target, ref currentVelocity, smoothTime, deltaTime, maxSpeed);
    }

    /// <summary>
    /// Gradually changes a vector towards a desired goal over time.
    /// </summary>
    /// <param name="current"></param>
    /// <param name="target"></param>
    /// <param name="currentVelocity"></param>
    /// <param name="smoothTime"></param>
    /// <param name="deltaTime"></param>
    /// <param name="maxSpeed"></param>
    public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime, float deltaTime, float maxSpeed = Mathf.Inf)
    {
        // Based on Game Programming Gems 4 Chapter 1.10
        smoothTime = Mathf.Max(0.0001F, smoothTime);
        float omega = 2F / smoothTime;

        float x = omega * (float)deltaTime;
        float exp = 1F / (1F + x + (0.48F * x * x) + (0.235F * x * x * x));

        float change_x = current.x - target.x;
        float change_y = current.y - target.y;
        float change_z = current.z - target.z;
        Vector3 originalTo = target;

        // Clamp maximum speed
        float maxChange = maxSpeed * smoothTime;

        float maxChangeSq = maxChange * maxChange;
        float sqrmag = (change_x * change_x) + (change_y * change_y) + (change_z * change_z);
        if (sqrmag > maxChangeSq)
        {
            var mag = (float)Math.Sqrt(sqrmag);
            change_x = change_x / mag * maxChange;
            change_y = change_y / mag * maxChange;
            change_z = change_z / mag * maxChange;
        }

        target.x = current.x - change_x;
        target.y = current.y - change_y;
        target.z = current.z - change_z;

        float temp_x = (currentVelocity.x + (omega * change_x)) * (float)deltaTime;
        float temp_y = (currentVelocity.y + (omega * change_y)) * (float)deltaTime;
        float temp_z = (currentVelocity.z + (omega * change_z)) * (float)deltaTime;

        currentVelocity.x = (currentVelocity.x - (omega * temp_x)) * exp;
        currentVelocity.y = (currentVelocity.y - (omega * temp_y)) * exp;
        currentVelocity.z = (currentVelocity.z - (omega * temp_z)) * exp;

        float output_x = target.x + ((change_x + temp_x) * exp);
        float output_y = target.y + ((change_y + temp_y) * exp);
        float output_z = target.z + ((change_z + temp_z) * exp);

        // Prevent overshooting
        float origMinusCurrent_x = originalTo.x - current.x;
        float origMinusCurrent_y = originalTo.y - current.y;
        float origMinusCurrent_z = originalTo.z - current.z;
        float outMinusOrig_x = output_x - originalTo.x;
        float outMinusOrig_y = output_y - originalTo.y;
        float outMinusOrig_z = output_z - originalTo.z;

        if ((origMinusCurrent_x * outMinusOrig_x) + (origMinusCurrent_y * outMinusOrig_y) + (origMinusCurrent_z * outMinusOrig_z) > 0)
        {
            output_x = originalTo.x;
            output_y = originalTo.y;
            output_z = originalTo.z;

            currentVelocity.x = (output_x - originalTo.x) / (float)deltaTime;
            currentVelocity.y = (output_y - originalTo.y) / (float)deltaTime;
            currentVelocity.z = (output_z - originalTo.z) / (float)deltaTime;
        }

        return new Vector3(output_x, output_y, output_z);
    }
}