using System;
using System.Linq;
using Dotplay;
using Dotplay.Game;
using Dotplay.Game.Client;
using Godot;

namespace Acruxx.Client.UI.Ingame;

/// <summary>
/// The game settings.
/// </summary>
public partial class GameSettings : CanvasLayer, IChildComponent<GameLogic>
{
    private OptionButton? _aaChanger;

    [Export]
    private NodePath? _aaChangerPath;

    private Button? _closeButton;

    [Export]
    private NodePath? _closeButtonPath;

    private TabContainer? _container;

    private CheckButton? _debanding;

    [Export]
    private NodePath? _debandingPath;

    private OptionButton? _debugChanger;

    [Export]
    private NodePath? _debugChangerPath;

    private SpinBox? _fov;

    [Export]
    private NodePath? _fovPath;

    private CheckButton? _glow;

    [Export]
    private NodePath? _glowPath;

    private KeyConfirmationDialog? _keyChangeDialog;

    [Export]
    private NodePath? _keyChangeDialogPath;

    [Export]
    private NodePath? _keyContainerPath;

    private VBoxContainer? _keyListContainer;

    private OptionButton? _msaaChanger;

    [Export]
    private NodePath? _msaaChangerPath;

    private CheckButton? _occlusionCulling;

    [Export]
    private NodePath? _occlusionPath;

    private OptionButton? _resChanger;

    [Export]
    private NodePath? _resChangerPath;

    private CheckButton? _sdfgi;

    [Export]
    private NodePath? _sdfgiPlath;

    [Export]
    private NodePath? _sensPathX;

    [Export]
    private NodePath? _sensPathY;

    private SpinBox? _sensX;

    private SpinBox? _sensY;

    private OptionButton? _shadowQuality;

    [Export]
    private NodePath? _shadowQualityPath;

    [Export]
    private NodePath? _soundVolumePath;

    private CheckButton? _ssao;

    [Export]
    private NodePath? _ssaoPath;

    private CheckButton? _ssil;

    [Export]
    private NodePath? _ssilPath;

    private HSlider? _viewportScaleChanger;

    [Export]
    private NodePath? _viewportScaleChangerPath;

    private HSlider? _volumeSlider;

    private CheckButton? _vsync;

    [Export]
    private NodePath? _vsyncPath;

    private OptionButton? _windowModeChanger;

    [Export]
    private NodePath? _windowModeChangerPath;

    public delegate void DisconnectEvent();

    public event DisconnectEvent? OnDisconnect;

    /// <inheritdoc/>
    public GameLogic BaseComponent { get; set; }

    /// <inheritdoc/>
    public override void _Ready()
    {
        this._sensX = this.GetNode(this._sensPathX) as SpinBox;
        this._sensY = this.GetNode(this._sensPathY) as SpinBox;
        this._fov = this.GetNode(this._fovPath) as SpinBox;

        this._viewportScaleChanger = this.GetNode<HSlider>(this._viewportScaleChangerPath);

        this._occlusionCulling = this.GetNode(this._occlusionPath) as CheckButton;
        this._ssao = this.GetNode(this._ssaoPath) as CheckButton;
        this._ssil = this.GetNode(this._ssilPath) as CheckButton;
        this._sdfgi = this.GetNode(this._sdfgiPlath) as CheckButton;
        this._glow = this.GetNode(this._glowPath) as CheckButton;
        this._debanding = this.GetNode(this._debandingPath) as CheckButton;
        this._vsync = this.GetNode(this._vsyncPath) as CheckButton;

        this._shadowQuality = this.GetNode(this._shadowQualityPath) as OptionButton;
        this._closeButton = this.GetNode(this._closeButtonPath) as Button;
        this._resChanger = this.GetNode(this._resChangerPath) as OptionButton;
        this._windowModeChanger = this.GetNode(this._windowModeChangerPath) as OptionButton;
        this._msaaChanger = this.GetNode(this._msaaChangerPath) as OptionButton;
        this._debugChanger = this.GetNode(this._debugChangerPath) as OptionButton;
        this._aaChanger = this.GetNode(this._aaChangerPath) as OptionButton;
        this._volumeSlider = this.GetNode(this._soundVolumePath) as HSlider;

        this._sensX!.Value = ClientSettings.Variables.Get<double>("cl_sensitivity_x");
        this._sensY!.Value = ClientSettings.Variables.Get<double>("cl_sensitivity_y");
        this._fov!.Value = ClientSettings.Variables.Get<double>("cl_fov");

        this._fov.ValueChanged += (double val) => ClientSettings.Variables.Set("cl_fov", val.ToString());
        this._sensX.ValueChanged += (double val) => ClientSettings.Variables.Set("cl_sensitivity_x", val.ToString());
        this._sensY.ValueChanged += (double val) => ClientSettings.Variables.Set("cl_sensitivity_y", val.ToString());

        this.InitResolutions();

        InitEnums<ClientSettings.WindowModes>(this._windowModeChanger!, "cl_window_mode");
        //InitEnums<Viewport.MSAA>(this._msaaChanger!, "cl_draw_msaa");
        //InitEnums<Viewport.ScreenSpaceAA>(this._aaChanger!, "cl_draw_aa");
        //InitEnums<Viewport.DebugDrawEnum>(this._debugChanger!, "cl_draw_debug");
        //InitEnums<RenderingServer.ShadowQuality>(this._shadowQuality!, "cl_draw_shadow");

        InitBoolean(this._glow!, "cl_draw_glow");
        InitBoolean(this._sdfgi!, "cl_draw_sdfgi");
        InitBoolean(this._ssao!, "cl_draw_ssao");
        InitBoolean(this._ssil!, "cl_draw_ssil");
        InitBoolean(this._occlusionCulling!, "cl_draw_occulision");
        InitBoolean(this._debanding!, "cl_draw_debanding");
        InitBoolean(this._vsync!, "cl_draw_vsync");

        this._keyListContainer = this.GetNode(this._keyContainerPath) as VBoxContainer;
        this._keyChangeDialog = this.GetNode(this._keyChangeDialogPath) as KeyConfirmationDialog;
        this.GetCurentList();

        this._closeButton!.Pressed += () =>
        {
            this.BaseComponent.Components.AddComponent<MenuComponent>("res://src/Acruxx/Game/Client/UI/Ingame/MenuComponent.tscn");
            this.BaseComponent.Components.DeleteComponent<GameSettings>();

            ClientSettings.Variables.StoreConfig("client.cfg");
        };

        this._keyChangeDialog!.Confirmed += () =>
        {
            var node = this._keyListContainer!.GetNode(this._keyChangeDialog.KeyName) as GameKeyRecord;

            node!.CurrentKey = this._keyChangeDialog.SelectedKey;
            ClientSettings.Variables.Set(this._keyChangeDialog.KeyName, node.CurrentKey);
        };

        this.SetProcess(false);

        var bus = AudioServer.GetBusIndex("Master");

        this._volumeSlider!.MinValue = 0f;
        this._volumeSlider.MaxValue = 1.0f;
        this._volumeSlider.Step = 0.05f;

        this._viewportScaleChanger.MinValue = 0.1f;
        this._viewportScaleChanger.MaxValue = 2.0f;
        this._viewportScaleChanger.Step = 0.05f;
    }

    /// <summary>
    /// Inits the boolean.
    /// </summary>
    /// <param name="button">The button.</param>
    /// <param name="storeKey">The store key.</param>
    private static void InitBoolean(CheckButton button, string storeKey)
    {
        var isChecked = ClientSettings.Variables.Get<bool>(storeKey);

        button.ButtonPressed = isChecked;
        button.Pressed += () =>
        {
            ClientSettings.Variables.Set(storeKey, button.ButtonPressed.ToString());
        };
    }

    /// <summary>
    /// Inits the enums.
    /// </summary>
    /// <param name="button">The button.</param>
    /// <param name="storeKey">The store key.</param>
    private static void InitEnums<TEnum>(OptionButton button, string storeKey) where TEnum : struct
    {
        var currentWindowMode = ClientSettings.Variables.GetValue(storeKey);

        int i = 0;
        int selectedId = 0;
        foreach (var item in Enum.GetValues(typeof(TEnum)))
        {
            button.AddItem(item.ToString(), (int)item);
            button.SetItemMetadata((int)item, item.ToString());

            if (currentWindowMode == item.ToString())
            {
                selectedId = i;
            }

            i++;
        }

        button.Selected = selectedId;
        button.ItemSelected += index =>
        {
            var meta = button.GetItemMetadata((int)index);
            ClientSettings.Variables.Set(storeKey, meta.ToString());
        };
    }

    /// <summary>
    /// Gets the curent list.
    /// </summary>
    private void GetCurentList()
    {
        foreach (var n in ClientSettings.Variables.Vars.AllVariables.Where(df => df.Key.Contains("key_")))
        {
            var scene = (PackedScene)ResourceLoader.Load("res://src/Acruxx/Game/Client/UI/Ingame/GameKeyRecord.tscn");
            var record = (GameKeyRecord)scene.Instantiate();

            record.CurrentKey = n.Value;
            record.Name = n.Key;
            record.OnKeyChangeStart += this.OnKeyChangeStart;
            this._keyListContainer!.AddChild(record);
        }
    }

    /// <summary>
    /// Inits the resolutions.
    /// </summary>
    private void InitResolutions()
    {
        var possibleResolitions = ClientSettings.Resolutions;
        int i = 0;
        int selected = 0;
        foreach (var item in possibleResolitions)
        {
            if (ClientSettings.Variables.GetValue("cl_resolution") == item)
            {
                selected = i;
            }

            this._resChanger!.AddItem(item, i++);
        }

        this._resChanger!.Selected = selected;
        this._resChanger.ItemSelected += index =>
        {
            ClientSettings.Variables.Set("cl_resolution", ClientSettings.Resolutions[index]);
        };
    }

    /// <summary>
    /// ons the key change start.
    /// </summary>
    /// <param name="keyName">The key name.</param>
    private void OnKeyChangeStart(string keyName)
    {
        this._keyChangeDialog!.OpenChanger(keyName);
    }
}