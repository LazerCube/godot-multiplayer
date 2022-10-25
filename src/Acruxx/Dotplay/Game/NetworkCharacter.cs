using System;
using System.Collections.Generic;
using System.Linq;
using Dotplay.Extensions;
using Dotplay.Game.Client;
using Dotplay.Game.Server;
using Dotplay.Input;
using Dotplay.Network;
using Dotplay.Network.Commands;
using Dotplay.Physics;
using Godot;

namespace Dotplay.Game;

/// <summary>
/// The general player class
/// </summary>
public partial class NetworkCharacter : CharacterBody3D, INetworkCharacter
{
    /// <summary>
    /// Node path to collider
    /// </summary>
    [Export]
    public NodePath ColliderPath;

    /// <summary>
    /// The height on crouching
    /// </summary>
    [Export]
    public float CrouchHeight = 1.3f;

    /// <summary>
    /// The last incoming local player state
    /// </summary>
    public PlayerState IncomingLocalPlayerState = new PlayerState();

    [NetworkVar(NetworkSyncFrom.FromServer, NetworkSyncTo.ToClient | NetworkSyncTo.ToPuppet)]
    public float NetworkCrouchingLevel = 1.0f;

    [NetworkVar(NetworkSyncFrom.FromServer, NetworkSyncTo.ToClient | NetworkSyncTo.ToPuppet)]
    public Vector3 NetworkPosition = Vector3.Zero;

    [NetworkVar(NetworkSyncFrom.FromServer, NetworkSyncTo.ToClient | NetworkSyncTo.ToPuppet)]
    public Quaternion NetworkRotation = Quaternion.Identity;

    [NetworkVar(NetworkSyncFrom.FromServer, NetworkSyncTo.ToClient | NetworkSyncTo.ToPuppet)]
    public Vector3 NetworkVelocity = Vector3.Zero;

    /// <summary>
    /// Archived player states
    /// </summary>
    public PlayerState[] States = new PlayerState[NetworkWorld.MaxTicks];

    internal MeshInstance3D DebugMesh;
    internal MeshInstance3D DebugMeshLocal;
    internal PlayerState? LastState;
    internal float OriginalHeight;
    internal float OriginalYPosition;
    internal float PreviousCrouchLevel;
    internal CollisionShape3D Shape;
    internal float ShapeHeight;
    internal Queue<PlayerState> StateQueue = new();
    internal float StateTimer;

    private readonly ComponentRegistry<NetworkCharacter> _components;
    private readonly Queue<Vector3> _teleportQueue = new();

    private CharacterCamera _detectCamera;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkCharacter"/> class.
    /// </summary>
    public NetworkCharacter()
    {
        this.NetworkSyncVars = this.GetNetworkAttributes();
        this._components = new ComponentRegistry<NetworkCharacter>(this);
    }

    /// <inheritdoc />
    public ComponentRegistry<NetworkCharacter> Components => this._components;

    /// <summary>
    /// The active tick based input
    /// </summary>
    // Current input struct for each player.
    // This is only needed because the ProcessAttack delegate flow is a bit too complicated.
    // TODO: Simplify this.
    public TickInput CurrentPlayerInput { get; set; }

    /// <summary>
    /// Time since last disconnect
    /// </summary>
    public float DisconnectTime { get; set; } = 0;

    /// <inheritdoc />
    public INetworkWorld GameWorld { get; set; }

    /// <summary>
    /// Return if the player is syncronized with server.
    /// </summary>
    public bool IsSynchronized { get; set; } = false;

    /// <inheritdoc />
    public int Latency { get; set; }

    /// <summary>
    /// Get the last tick of the last input
    /// </summary>
    public uint LatestInputTick { get; set; } = 0;

    /// <summary>
    /// The network mode of the plaer
    /// </summary>
    [Export(PropertyHint.Enum)]
    public NetworkMode Mode { get; set; }

    /// <summary>
    /// Movement processor
    /// </summary>
    public IMovementProcessor MovementProcessor { get; set; } = new DefaultMovementProcessor();

    /// <inheritdoc />
    public short NetworkId { get; set; }

    /// <summary>
    /// Gets the network sync vars.
    /// </summary>
    public Dictionary<string, NetworkAttribute> NetworkSyncVars { get; private set; } = new Dictionary<string, NetworkAttribute>();

    /// <inheritdoc />
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>
    /// Previous state since last tick
    /// </summary>
    public PlayerConnectionState PreviousState { get; set; }

    /// <inheritdoc />
    public short[] RequiredComponents { get; set; } = Array.Empty<short>();

    /// <inheritdoc />
    public short[] RequiredPuppetComponents { get; set; } = Array.Empty<short>();

    /// <summary>
    /// The resource path of the player component
    /// </summary>
    public string ResourcePath { get; set; } = string.Empty;

    /// <summary>
    /// The mono script path of the player component
    /// </summary>
    public string[] ScriptPaths { get; set; } = Array.Empty<string>();

    /// <inheritdoc />
    public PlayerConnectionState State { get; set; }

    /// <inheritdoc />
    public override void _EnterTree()
    {
        base._EnterTree();

        this.ChildEnteredTree += this.SetNewComponent;

        this.Shape = this.GetShape();
        this.Shape.Shape.ResourceLocalToScene = true;

        float shapeHeight = 0;

        if (this.Shape != null)
        {
            this.OriginalHeight = this.Shape.Scale.y;
            this.OriginalYPosition = this.Shape.Transform.origin.y;
            if (this.Shape.Shape is CapsuleShape3D)
            {
                shapeHeight = (this.Shape.Shape as CapsuleShape3D).Height;
            }
            else if (this.Shape.Shape is BoxShape3D)
            {
                shapeHeight = (this.Shape.Shape as BoxShape3D).Size.y;
            }
            else
            {
                throw new Exception("Shape type not implemented yet");
            }
        }

        this.ShapeHeight = shapeHeight;
        this.NetworkCrouchingLevel = shapeHeight;
        this.PreviousCrouchLevel = shapeHeight;

        //if (this.IsLocal())
        //{
        //    this.CreateDebugMesh();
        //}
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        base._ExitTree();

        this.ChildEnteredTree -= this.SetNewComponent;
    }

    /// <inheritdoc />
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (this.PreviousCrouchLevel != this.NetworkCrouchingLevel && this.Shape?.Shape != null)
        {
            var yPos = (this.ShapeHeight - this.NetworkCrouchingLevel) / 2;
            yPos *= -1;

            var transform = this.Shape.Transform;
            transform.origin.y = this.OriginalYPosition + yPos;
            transform.origin.y = Mathf.Clamp(transform.origin.y, (this.ShapeHeight / 2) * -1f, this.OriginalYPosition);
            this.Shape.Transform = transform;

            var shape = this.Shape.Shape as CapsuleShape3D;
            shape.Height = this.NetworkCrouchingLevel;
            this.Shape.Shape = shape;

            this.PreviousCrouchLevel = this.NetworkCrouchingLevel;
        }

        if (this.IsPuppet())
        {
            this.ProcessPuppetInput((float)delta);
        }
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        base._Process(delta);

        if (this._detectCamera == null)
        {
            this._detectCamera = this.Components.Get<CharacterCamera>();
        }

        if (this.IsLocal() && this.DebugMesh != null && this.DebugMeshLocal != null && this._detectCamera != null)
        {
            var activated = ClientSettings.Variables.Get("cl_debug_server", false);
            if (activated && this._detectCamera.Mode == CameraMode.FPS)
            {
                activated = false;
            }

            if (!activated)
            {
                this.DebugMeshLocal.Visible = false;
                this.DebugMesh.Visible = false;
            }
            else
            {
                var state = this.IncomingLocalPlayerState;
                if (!default(PlayerState).Equals(state))
                {
                    this.DebugMeshLocal.Visible = activated;
                    this.DebugMesh.Visible = activated;

                    this.DebugMesh.GlobalTransform = new Transform3D(
                        state.GetVar<Quaternion>(this, "NetworkRotation"),
                        state.GetVar<Vector3>(this, "NetworkPosition"));
                }
            }
        }
    }

    /// <summary>
    /// Activate an component by given id
    /// </summary>
    /// <param name="index"></param>
    /// <param name="enable"></param>
    public void ActivateComponent(int index, bool enable)
    {
        var node = this.Components.All.Where(df => df is IPlayerComponent && df is Node).Select(df => df as IPlayerComponent)
        .FirstOrDefault(df => df.NetworkId == index);

        if (node == null || !node.IsInsideTree())
            return;

        var child = node as Node;
        child.ProcessMode = enable ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;

        if (child is IPlayerComponent playerComponent)
        {
            playerComponent.IsEnabled = enable;
        }

        if (child is Node3D node3D)
        {
            (node3D).Visible = enable;
        }

        child.SetPhysicsProcess(enable);
        child.SetProcessInternal(enable);
        child.SetProcess(enable);
        child.SetProcessInput(enable);
        child.SetPhysicsProcessInternal(enable);
    }

    /// <summary>
    /// Apply the network state for the body component
    /// </summary>
    /// <param name="state"></param>
    public void ApplyBodyState(PlayerState state)
    {
        this.GlobalTransform = new Transform3D(
            state.GetVar<Quaternion>(this, "NetworkRotation"),
            state.GetVar<Vector3>(this, "NetworkPosition"));

        // this.MovingPlatformApplyVelocityOnLeave = MovingPlatformApplyVelocityOnLeaveEnum.Never;

        this.Velocity = state.GetVar<Vector3>(this, "NetworkVelocity");
        this.MovementProcessor.Velocity = state.GetVar<Vector3>(this, "NetworkVelocity");
    }

    /// <summary>
    /// Apply an network state
    /// </summary>
    /// <param name="state">The network state to applied</param>
    public virtual void ApplyNetworkState(PlayerState state)
    {
        if (!this.IsPuppet() || !this.GameWorld.ServerVars.Get("sv_interpolate", true))
        {
            this.ApplyVars(state);
            this.ApplyBodyState(state);
        }
        // only for puppet
        else
        {
            while (this.StateQueue.Count >= 2)
            {
                this.StateQueue.Dequeue();
            }
            this.StateQueue.Enqueue(state);
        }
    }

    /// <summary>
    /// Applies the vars.
    /// </summary>
    /// <param name="state">The state.</param>
    public void ApplyVars(PlayerState state)
    {
        if (this.IsPuppet())
        {
            // check if the marked for local
            foreach (var networkVar in this.NetworkSyncVars.Where(df => df.Value.From.HasFlag(NetworkSyncFrom.FromServer)
            && df.Value.To.HasFlag(NetworkSyncTo.ToPuppet)))
            {
                var value = state.GetVar(this, networkVar.Key);
                this.Set(networkVar.Key, VariantExtensions.CreateFromObject(value));
            }
        }

        if (this.IsLocal())
        {
            // check if the marked for local
            foreach (var networkVar in this.NetworkSyncVars.Where(df => df.Value.From.HasFlag(NetworkSyncFrom.FromServer)
            && df.Value.To.HasFlag(NetworkSyncTo.ToClient)))
            {
                var value = state.GetVar(this, networkVar.Key);
                this.Set(networkVar.Key, VariantExtensions.CreateFromObject(value));
            }
        }
    }

    /// <summary>
    /// Detect an hit by given camera view
    /// </summary>
    /// <param name="range"></param>
    public virtual RayCastHit DetechtHit(float range)
    {
        var input = this.Components.Get<NetworkInput>();
        if (input is null)
        {
            return null;
        }

        if (this._detectCamera is null)
        {
            return null;
        }

        var headViewRotation = input.LastInput.ViewDirection;
        var basis = new Basis(headViewRotation);

        var command = this.ToNetworkState();
        //var currentTransform = new Godot.Transform3D(basis, command.GetVar<Vector3>(this, "NetworkPosition"));

        var attackPosition = this._detectCamera.GlobalTransform.origin;
        var attackTransform = new Transform3D(basis, attackPosition);
        var attackTransformFrom = new Transform3D(command.GetVar<Quaternion>(this, "NetworkRotation"), attackPosition);

        var raycast = new PhysicsRayQueryParameters3D
        {
            From = attackTransformFrom.origin,
            To = attackTransform.origin + (-attackTransform.basis.z * range)
        };

        var result = this.GetWorld3d().DirectSpaceState.IntersectRay(raycast);

        if (result?.ContainsKey("position") == true)
        {
            var rayResult = new RayCastHit
            {
                PlayerSource = this,
                To = (Vector3)result["position"],
                Collider = (Node)result["collider"],
                From = attackTransform.origin
            };

            if (rayResult.Collider is HitBox hitbox)
            {
                var enemy = hitbox.GetPlayer();
                if (enemy is not null)
                {
                    rayResult.PlayerDestination = enemy;
                }
            }

            return rayResult;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Trigger an attack on server side
    /// </summary>
    /// <param name="range">Range for detection</param>
    public void DoAttack(float range = 1000)
    {
        if (this.GameWorld is NetworkServerWorld)
        {
            (this.GameWorld as NetworkServerWorld)?.ProcessPlayerAttack(this, range);
        }
    }

    /// <summary>
    /// Teleport player to an given position
    /// </summary>
    /// <param name="origin">New position of the player</param>
    public void DoTeleport(Vector3 origin)
    {
        Logger.LogDebug(this, "Enquene teleport to " + origin);
        this._teleportQueue.Enqueue(origin);
    }

    /// <summary>
    /// Get the shape of the player body
    /// </summary>
    /// <returns></returns>
    public CollisionShape3D GetShape()
    {
        return this.GetNodeOrNull<CollisionShape3D>(this.ColliderPath);
    }

    /// <summary>
    /// Get the current shape height
    /// </summary>
    public float GetShapeHeight()
    {
        return this.Shape != null ? this.Shape.Transform.origin.y : 0;
    }

    /// <summary>
    /// Interpolates the.
    /// </summary>
    /// <param name="theta">The theta.</param>
    /// <param name="lastState">The last state.</param>
    /// <param name="nextState">The next state.</param>
    /// <returns>A PlayerState.</returns>
    public virtual PlayerState Interpolate(float theta, PlayerState lastState, PlayerState nextState)
    {
        // only for movement component (to interpolate)
        var a = lastState.GetVar<Quaternion>(this, "NetworkRotation");
        var b = nextState.GetVar<Quaternion>(this, "NetworkRotation");

        var aPos = lastState.GetVar<Vector3>(this, "NetworkPosition");
        var bPos = nextState.GetVar<Vector3>(this, "NetworkPosition");

        var aCrouch = lastState.GetVar<float>(this, "NetworkCrouchingLevel");
        var bCrouch = nextState.GetVar<float>(this, "NetworkCrouchingLevel");

        var newState = this.ToNetworkState();

        newState.SetVar(this, "NetworkPosition", aPos.Lerp(bPos, theta));
        newState.SetVar(this, "NetworkRotation", a.Slerp(b, theta));
        newState.SetVar(this, "NetworkCrouchingLevel", Mathf.Lerp(aCrouch, bCrouch, theta));

        return newState;
    }

    /// <summary>
    /// Is player on ground
    /// </summary>
    /// <returns>true or false</returns>
    public virtual bool IsOnGround()
    {
        return this.IsOnFloor();
    }

    /// <summary>
    /// Moving the character
    /// </summary>
    /// <param name="delta">float</param>
    /// <param name="velocity">Vector3</param>
    public virtual void Move(Vector3 velocity)
    {
        this.Velocity = velocity;
        this.MoveAndSlide();
    }

    /// <summary>
    /// Teleport player to an given position
    /// </summary>
    /// <param name="origin">New position of the player</param>
    public void MoveToPosition(Vector3 origin)
    {
        var data = this.ToNetworkState();
        data.SetVar(this, "NetworkPosition", origin);
        this.ApplyBodyState(data);
    }

    /// <summary>
    /// Trigger when an player got an hit
    /// </summary>
    public virtual void OnHit(RayCastHit hit)
    { }

    /// <summary>
    /// Set the shape height by a given process value
    /// </summary>
    /// <param name="crouchInProcess">can be between 0 (crouching) and 1 (non crouching)</param>
    public virtual void SetCrouchingLevel(float crouchInProcess)
    {
        // this.currentCouchLevel
        this.NetworkCrouchingLevel += MathF.Round(crouchInProcess, 2);
        this.NetworkCrouchingLevel = Mathf.Clamp(this.NetworkCrouchingLevel, this.CrouchHeight, this.ShapeHeight);
    }

    /// <summary>
    /// Ticks the.
    /// </summary>
    /// <param name="delta">The delta.</param>
    public virtual void Tick(float delta)
    { }

    /// <summary>
    /// /// Get the current network state
    /// </summary>
    public virtual PlayerState ToNetworkState()
    {
        var state = new PlayerState
        {
            NetworkId = this.NetworkId,
            Latency = (short)this.Latency,
            NetworkSyncedVars = new List<PlayerNetworkVarState>()
        };

        foreach (var element in this.NetworkSyncVars)
        {
            state.SetVar(this, element.Key, this.Get(element.Key).Obj);
        }

        state.SetVar(this, "NetworkRotation", this.GlobalTransform.basis.GetRotationQuaternion());
        state.SetVar(this, "NetworkPosition", this.GlobalTransform.origin);
        state.SetVar(this, "NetworkVelocity", this.MovementProcessor.Velocity);
        state.SetVar(this, "NetworkCrouchingLevel", this.NetworkCrouchingLevel);

        return state;
    }

    /// <summary>
    /// Internals the tick.
    /// </summary>
    /// <param name="delta">The delta.</param>
    internal virtual void InternalTick(float delta)
    {
        if (this._teleportQueue.Count > 0)
        {
            var nextTeleport = this._teleportQueue.Dequeue();
            Logger.LogDebug(this, "Execute teleport to " + nextTeleport + " -> " + this.IsServer());
            this.MoveToPosition(nextTeleport);

            Logger.LogDebug(this, "Finish teleport to " + nextTeleport + " -> " + this.IsServer());
        }

        // process server and client
        if (!this.IsPuppet())
        {
            var input = this.Components.Get<NetworkInput>();
            if (input != null)
            {
                // this.MovementProcessor.Velocity = this.Velocity;
                this.MovementProcessor.SetServerVars(this.GameWorld.ServerVars);
                this.MovementProcessor.Simulate(this, input.LastInput, delta);
            }
        }

        //process components
        foreach (var component in this.Components.All)
        {
            if (component is IPlayerComponent)
            {
                (component as IPlayerComponent)?.Tick(delta);
            }
        }

        this.Tick(delta);
    }

    /// <summary>
    /// Processes the puppet input.
    /// </summary>
    /// <param name="delta">The delta.</param>
    internal void ProcessPuppetInput(float delta)
    {
        if (!this.GameWorld.ServerVars.Get("sv_interpolate", true))
        {
            return;
        }

        this.StateTimer += delta;

        float serverSendRate = 1f / (float)this.GetPhysicsProcessDeltaTime();
        float serverSendInterval = 1f / serverSendRate;

        if (this.StateTimer > serverSendInterval)
        {
            this.StateTimer -= serverSendInterval;
            if (this.StateQueue.Count > 1)
            {
                this.LastState = this.StateQueue.Dequeue();
            }
        }

        // We can only interpolate if we have a previous and next world state.
        if (!this.LastState.HasValue || this.StateQueue.Count < 1)
        {
            Logger.LogDebug(this, "RemotePlayer: not enough states to interp");
            return;
        }

        var nextState = this.StateQueue.Peek();
        float theta = this.StateTimer / serverSendInterval;

        if (this.GameWorld.ServerVars.Get("sv_interpolate", true))
        {
            var newState = this.Interpolate(theta, this.LastState.Value, nextState);
            this.ApplyVars(newState);
            this.ApplyBodyState(newState);
        }
        else
        {
            this.ApplyVars(nextState);
            this.ApplyBodyState(nextState);
        }
    }

    /// <summary>
    /// Sets the new component.
    /// </summary>
    /// <param name="child">The child.</param>
    internal void SetNewComponent(Node child)
    {
        if (child is IPlayerComponent playerComponent)
        {
            this.ActivateComponent(playerComponent.NetworkId, false);

            Logger.LogDebug(this, "Found component: " + playerComponent.GetType().Name);
            playerComponent.BaseComponent = this;
        }
    }

    /// <summary>
    /// Creates the debug mesh.
    /// </summary>
    private void CreateDebugMesh()
    {
        var shape = this.GetShape();
        if (shape?.Shape != null && shape.Shape is CapsuleShape3D radShape)
        {
            var cpsule = new CapsuleMesh
            {
                Radius = radShape.Radius,
                Height = radShape.Height
            };

            var color = Colors.Red;
            color.a = 0.3f;

            var mesh = new MeshInstance3D
            {
                Mesh = cpsule,
                MaterialOverride = new StandardMaterial3D
                {
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    AlbedoColor = color,
                },
                Name = "DebugMesh",
                Visible = false,
                TopLevel = true
            };

            var colorLocal = Colors.Green;
            colorLocal.a = 0.3f;

            var meshLocal = new MeshInstance3D
            {
                Mesh = cpsule,
                MaterialOverride = new StandardMaterial3D
                {
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    AlbedoColor = colorLocal
                },
                Name = "DebugMeshLocal",
                Visible = false
            };

            this.AddChild(mesh);
            this.AddChild(meshLocal);

            this.DebugMesh = mesh;
            this.DebugMeshLocal = meshLocal;
        }
    }
}