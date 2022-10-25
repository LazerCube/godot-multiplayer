using System.Collections.Generic;
using Godot;

namespace Dotplay.Utils;

/// <summary>
/// The line drawer3d.
/// </summary>
public partial class LineDrawer3d : MeshInstance3D
{
    private readonly List<RayCastLine> _lines = new();

    /// <summary>
    /// Gets or sets the draw time.
    /// </summary>
    public float DrawTime { get; set; } = 1.2f;

    /// <inheritdoc />
    public override void _EnterTree()
    {
        base._EnterTree();
        this.Mesh = new ImmediateMesh();
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = Colors.Red;
        mat.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        this.MaterialOverride = mat;
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        base._Process(delta);

        var mesh = this.Mesh as ImmediateMesh;
        mesh.ClearSurfaces();

        foreach (var line in this._lines.ToArray())
        {
            if (line.Time <= 0f)
            {
                this._lines.Remove(line);
            }
            else
            {
                line.Time -= delta;
            }
        }

        if (this._lines.Count > 0)
        {
            mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
            foreach (var line in this._lines.ToArray())
            {
                mesh.SurfaceAddVertex(line.P1);
                mesh.SurfaceAddVertex(line.P2);
            }

            mesh.SurfaceEnd();
        }
    }

    /// <summary>
    /// Adds the line.
    /// </summary>
    /// <param name="p1">The p1.</param>
    /// <param name="p2">The p2.</param>
    public void AddLine(Vector3 p1, Vector3 p2)
    {
        this._lines.Add(new RayCastLine { P1 = p1, P2 = p2, Time = DrawTime });
    }
}

/// <summary>
/// The ray cast line.
/// </summary>
public class RayCastLine
{
    /// <summary>
    /// Gets or sets the p1.
    /// </summary>
    public Vector3 P1 { get; set; }

    /// <summary>
    /// Gets or sets the p2.
    /// </summary>
    public Vector3 P2 { get; set; }

    /// <summary>
    /// Gets or sets the time.
    /// </summary>
    public double Time { get; set; }
}