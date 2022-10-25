using System.Diagnostics;
using Godot;

namespace Acruxx.Camera;

/// <summary>
/// The spring arm.
/// Control the zoom of the camera with 'zoom', a value between 0 and 1
/// </summary>
public partial class SpringArm : SpringArm3D
{
    public Vector3 PositionStart;
    private Vector2 _lengthRange = new(1.0f, 4.0f);
    private float _zoom = 0.5f;

    /// <summary>
    /// Gets or sets the length range.
    /// </summary>
    [Export]
    public Vector2 LengthRange
    {
        get { return this._lengthRange; }
        set
        {
            this._lengthRange.x = value.x;
            this._lengthRange.y = value.y;

            // set zoom again
            this.Zoom = this._zoom;
        }
    }

    /// <summary>
    /// Gets or sets the zoom.
    /// </summary>
    [Export]
    public float Zoom
    {
        get { return this._zoom; }
        set
        {
            Debug.Assert((value >= 0.0) && (value <= 1.0f));
            this._zoom = value;
            this.SpringLength = this._lengthRange.y + this._lengthRange.x - Mathf.Lerp(this._lengthRange.x, this._lengthRange.y, this._zoom);
        }
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        this.PositionStart = this.Position;
    }
}