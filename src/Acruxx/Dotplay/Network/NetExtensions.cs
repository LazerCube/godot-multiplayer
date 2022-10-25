using System;
using System.Collections.Generic;
using Godot;
using LiteNetLib.Utils;

namespace Dotplay;

/// <summary>
/// The NetDataReader extensions.
/// </summary>
public static class NetExtensions
{
    /// <summary>
    ///  Quaternion float precision.
    /// </summary>
    private const float QUAT_FLOAT_PRECISION_MULT = 32767f;

    /// <summary>
    /// Deserializes the quaternion.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <returns>A Quaternion.</returns>
    public static Quaternion DeserializeQuaternion(NetDataReader reader)
    {
        // Read values from the wire and map back to normal float precision.
        byte maxIndex = reader.GetByte();
        float a = reader.GetShort() / QUAT_FLOAT_PRECISION_MULT;
        float b = reader.GetShort() / QUAT_FLOAT_PRECISION_MULT;
        float c = reader.GetShort() / QUAT_FLOAT_PRECISION_MULT;

        // Reconstruct the fourth value.
        float d = Mathf.Sqrt(1f - ((a * a) + (b * b) + (c * c)));
        return maxIndex switch
        {
            0 => new Quaternion(d, a, b, c),
            1 => new Quaternion(a, d, b, c),
            2 => new Quaternion(a, b, d, c),
            3 => new Quaternion(a, b, c, d),
            _ => throw new InvalidProgramException("Unexpected quaternion index."),
        };
    }

    /// <summary>
    /// Deserializes the string dictonary.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <returns>A Dictionary.</returns>
    public static Dictionary<string, string> DeserializeStringDictonary(NetDataReader reader)
    {
        var dictonary = new Dictionary<string, string>();
        var count = reader.GetInt();
        for (int i = 0; i < count; i++)
        {
            dictonary.Add(reader.GetString(), reader.GetString());
        }

        return dictonary;
    }

    /// <summary>
    /// Deserializes the vector2.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <returns>A Vector2.</returns>
    public static Vector2 DeserializeVector2(NetDataReader reader)
    {
        Vector2 v;
        v.x = reader.GetFloat();
        v.y = reader.GetFloat();
        return v;
    }

    /// <summary>
    /// Deserializes the vector3.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <returns>A Vector3.</returns>
    public static Vector3 DeserializeVector3(NetDataReader reader)
    {
        Vector3 v;
        v.x = reader.GetFloat();
        v.y = reader.GetFloat();
        v.z = reader.GetFloat();
        return v;
    }

    /// <summary>
    /// Gets the array.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <returns>An array of TS.</returns>
    public static T[] GetArray<T>(this NetDataReader reader) where T : INetSerializable, new()
    {
        var len = reader.GetUShort();
        var array = new T[len];
        for (int i = 0; i < len; i++)
        {
            array[i] = reader.Get<T>();
        }
        return array;
    }

    /// <summary>
    /// Gets the dictonary string.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <returns>A Dictionary.</returns>
    public static Dictionary<string, string> GetDictonaryString(this NetDataReader reader)
    {
        return DeserializeStringDictonary(reader);
    }

    /// <summary>
    /// Gets the quaternion.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <returns>A Quaternion.</returns>
    public static Quaternion GetQuaternion(this NetDataReader reader)
    {
        return DeserializeQuaternion(reader);
    }

    /// <summary>
    /// Gets the vector2.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <returns>A Vector2.</returns>
    public static Vector2 GetVector2(this NetDataReader reader)
    {
        return DeserializeVector2(reader);
    }

    /// <summary>
    /// Gets the vector3.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <returns>A Vector3.</returns>
    public static Vector3 GetVector3(this NetDataReader reader)
    {
        return DeserializeVector3(reader);
    }

    /// <summary>
    /// Puts the.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="quaternion">The quaternion.</param>
    public static void Put(this NetDataWriter writer, Quaternion quaternion)
    {
        SerializeQuaternion(writer, quaternion);
    }

    /// <summary>
    /// Puts the.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="vector">The vector.</param>
    public static void Put(this NetDataWriter writer, Vector3 vector)
    {
        SerializeVector3(writer, vector);
    }

    /// <summary>
    /// Puts the.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="vector">The vector.</param>
    public static void Put(this NetDataWriter writer, Vector2 vector)
    {
        SerializeVector2(writer, vector);
    }

    /// <summary>
    /// Puts the.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="dic">The dic.</param>
    public static void Put(this NetDataWriter writer, Dictionary<string, string> dic)
    {
        SerializeStringDictonary(writer, dic);
    }

    /// <summary>
    /// Puts the array.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="array">The array.</param>
    public static void PutArray<T>(this NetDataWriter writer, T[] array) where T : INetSerializable
    {
        writer.Put((ushort)array.Length);
        foreach (var obj in array)
        {
            writer.Put(obj);
        }
    }

    /// <summary>
    /// Serializes the quaternion.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="quaternion">The quaternion.</param>
    public static void SerializeQuaternion(NetDataWriter writer, Quaternion quaternion)
    {
        // Utilize "Smallest three" strategy.
        // Reference: https://gafferongames.com/post/snapshot_compression/
        byte maxIndex = 0;
        float maxValue = float.MinValue;
        float maxValueSign = 1;

        // Find the largest value in the quaternion and save its index.
        for (byte i = 0; i < 4; i++)
        {
            var value = quaternion[i];
            var absValue = Mathf.Abs(value);
            if (absValue > maxValue)
            {
                maxIndex = i;
                maxValue = absValue;

                // Note the sign of the maxValue for later.
                maxValueSign = Mathf.Sign(value);
            }
        }

        // Encode the smallest three components.
        short a, b, c;
        switch (maxIndex)
        {
            case 0:
                a = (short)(quaternion.y * maxValueSign * QUAT_FLOAT_PRECISION_MULT);
                b = (short)(quaternion.z * maxValueSign * QUAT_FLOAT_PRECISION_MULT);
                c = (short)(quaternion.w * maxValueSign * QUAT_FLOAT_PRECISION_MULT);
                break;

            case 1:
                a = (short)(quaternion.x * maxValueSign * QUAT_FLOAT_PRECISION_MULT);
                b = (short)(quaternion.z * maxValueSign * QUAT_FLOAT_PRECISION_MULT);
                c = (short)(quaternion.w * maxValueSign * QUAT_FLOAT_PRECISION_MULT);
                break;

            case 2:
                a = (short)(quaternion.x * maxValueSign * QUAT_FLOAT_PRECISION_MULT);
                b = (short)(quaternion.y * maxValueSign * QUAT_FLOAT_PRECISION_MULT);
                c = (short)(quaternion.w * maxValueSign * QUAT_FLOAT_PRECISION_MULT);
                break;

            case 3:
                a = (short)(quaternion.x * maxValueSign * QUAT_FLOAT_PRECISION_MULT);
                b = (short)(quaternion.y * maxValueSign * QUAT_FLOAT_PRECISION_MULT);
                c = (short)(quaternion.z * maxValueSign * QUAT_FLOAT_PRECISION_MULT);
                break;

            default:
                throw new InvalidProgramException("Unexpected quaternion index.");
        }

        writer.Put(maxIndex);
        writer.Put(a);
        writer.Put(b);
        writer.Put(c);
    }

    /// <summary>
    /// Serializes the string dictonary.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="dic">The dic.</param>
    public static void SerializeStringDictonary(NetDataWriter writer, Dictionary<string, string> dic)
    {
        if (dic != null)
        {
            writer.Put(dic.Count);

            foreach (var va in dic)
            {
                writer.Put(va.Key);
                writer.Put(va.Value);
            }
        }
        else
        {
            writer.Put(0);
        }
    }

    /// <summary>
    /// Serializes the vector2.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="vector">The vector.</param>
    public static void SerializeVector2(NetDataWriter writer, Vector2 vector)
    {
        writer.Put(vector.x);
        writer.Put(vector.y);
    }

    /// <summary>
    /// Serializes the vector3.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="vector">The vector.</param>
    public static void SerializeVector3(NetDataWriter writer, Vector3 vector)
    {
        writer.Put(vector.x);
        writer.Put(vector.y);
        writer.Put(vector.z);
    }

    /// <summary>
    /// Tos the enum.
    /// </summary>
    /// <param name="enumString">The enum string.</param>
    /// <returns>A T.</returns>
    public static T ToEnum<T>(this string enumString)
    {
        return (T)Enum.Parse(typeof(T), enumString);
    }
}