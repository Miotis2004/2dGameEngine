using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;
using _2dGameEngine.Animation;
using _2dGameEngine.Content;
using _2dGameEngine.Core;
using _2dGameEngine.Graphics;
using _2dGameEngine.Physics;
using _2dGameEngine.Prefabs;
using _2dGameEngine.Scripting;
using _2dGameEngine.UI;

namespace _2dGameEngine.Serialization;

/// <summary>
/// Saves and restores scenes as stable, human-readable JSON documents.
/// </summary>
public static class SceneSerializer
{
    private const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Serializes a scene to a JSON string.
    /// </summary>
    public static string Serialize(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        return JsonSerializer.Serialize(ToDocument(scene), JsonOptions);
    }

    /// <summary>
    /// Writes a serialized scene JSON document to disk.
    /// </summary>
    public static void Save(Scene scene, string path)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, Serialize(scene));
    }

    /// <summary>
    /// Deserializes a scene from a JSON string.
    /// </summary>
    public static Scene Deserialize(string json, AssetManager? assets = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        SceneDocument document = JsonSerializer.Deserialize<SceneDocument>(json, JsonOptions)
            ?? throw new InvalidDataException("Scene document is empty or invalid.");

        if (document.SchemaVersion != CurrentSchemaVersion)
        {
            throw new InvalidDataException($"Unsupported scene schema version '{document.SchemaVersion}'. Expected '{CurrentSchemaVersion}'.");
        }

        if (string.IsNullOrWhiteSpace(document.Name))
        {
            throw new InvalidDataException("Scene document does not specify a name.");
        }

        Scene scene = new(document.Name);
        foreach (EntityDocument entityDocument in document.Entities ?? [])
        {
            if (string.IsNullOrWhiteSpace(entityDocument.Name))
            {
                throw new InvalidDataException("Scene contains an entity without a name.");
            }

            Entity entity = CreateEntity(entityDocument, assets);
            scene.AddEntity(entity);
        }

        return scene;
    }

    /// <summary>
    /// Reads and deserializes a scene JSON document from disk.
    /// </summary>
    public static Scene Load(string path, AssetManager? assets = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Deserialize(File.ReadAllText(path), assets);
    }


    /// <summary>
    /// Creates a deep copy of an entity hierarchy using the same stable serialization path as scenes and prefabs.
    /// </summary>
    public static Entity CloneEntity(Entity entity, AssetManager? assets = null)
    {
        ArgumentNullException.ThrowIfNull(entity);
        SceneDocument document = new(CurrentSchemaVersion, "Clone", [ToEntityDocument(entity)]);
        string json = JsonSerializer.Serialize(document, JsonOptions);
        return Deserialize(json, assets).Entities.Single();
    }

    private static EntityDocument ToEntityDocument(Entity entity)
    {
        return new EntityDocument(
            entity.Name,
            entity.IsEnabled,
            ToTransformDocument(entity.Transform.Value),
            entity.Components.Where(component => component is not TransformComponent).Select(ToComponentDocument).ToArray(),
            entity.Children.Select(ToEntityDocument).ToArray());
    }

    private static Entity CreateEntity(EntityDocument entityDocument, AssetManager? assets)
    {
        if (string.IsNullOrWhiteSpace(entityDocument.Name))
        {
            throw new InvalidDataException("Scene contains an entity without a name.");
        }

        Entity entity = new(entityDocument.Name)
        {
            IsEnabled = entityDocument.IsEnabled,
        };
        ApplyTransform(entity.Transform.Value, entityDocument.Transform);

        foreach (ComponentDocument componentDocument in entityDocument.Components ?? [])
        {
            Component component = CreateComponent(componentDocument, assets);
            component.IsEnabled = componentDocument.IsEnabled;
            entity.AddComponent(component);
        }

        foreach (EntityDocument childDocument in entityDocument.Children ?? [])
        {
            entity.AddChild(CreateEntity(childDocument, assets));
        }

        return entity;
    }

    private static SceneDocument ToDocument(Scene scene)
    {
        return new SceneDocument(
            CurrentSchemaVersion,
            scene.Name,
            scene.Entities.Where(entity => entity.Parent is null).Select(ToEntityDocument).ToArray());
    }

    private static ComponentDocument ToComponentDocument(Component component)
    {
        ComponentDocument document = component switch
        {
            EntityMotionComponent motion => new ComponentDocument("EntityMotion", Velocity: ToVectorDocument(motion.Velocity)),
            EntityInputMovementComponent movement => new ComponentDocument("EntityInputMovement", Speed: movement.Speed),
            SpriteRenderer sprite => new ComponentDocument("SpriteRenderer", Size: ToVectorDocument(sprite.Size), Color: ToColorString(sprite.Color), OutlineColor: sprite.OutlineColor is { } outline ? ToColorString(outline) : null, SortingOrder: sprite.SortingOrder, Frame: ToFrameReference(sprite.Frame), PrimitiveType: sprite.PrimitiveType.ToString(), RenderLayer: sprite.RenderLayer.ToString(), Material: ToMaterialDocument(sprite.Material)),
            RigidBody2D body => new ComponentDocument("RigidBody2D", Velocity: ToVectorDocument(body.Velocity), GravityScale: body.GravityScale, IsKinematic: body.IsKinematic),
            BoxCollider2D box => new ComponentDocument("BoxCollider2D", Size: ToVectorDocument(box.Size), Offset: ToVectorDocument(box.Offset), IsTrigger: box.IsTrigger, Radius: box.Layer),
            CircleCollider2D circle => new ComponentDocument("CircleCollider2D", Radius: circle.Radius, Offset: ToVectorDocument(circle.Offset), IsTrigger: circle.IsTrigger),
            CapsuleCollider2D capsule => new ComponentDocument("CapsuleCollider2D", Size: ToVectorDocument(capsule.Size), Offset: ToVectorDocument(capsule.Offset), IsTrigger: capsule.IsTrigger),
            PolygonCollider2D polygon => new ComponentDocument("PolygonCollider2D", Offset: ToVectorDocument(polygon.Offset), IsTrigger: polygon.IsTrigger, Tiles: polygon.Points.Select(point => FormattableString.Invariant($"{point.X},{point.Y}")).ToArray()),
            TilemapCollider2D collider => new ComponentDocument("TilemapCollider2D", Offset: ToVectorDocument(collider.Offset), IsTrigger: collider.IsTrigger, Radius: collider.Layer),
            PhysicsJoint2D joint => new ComponentDocument("PhysicsJoint2D", Text: joint.JointType.ToString(), Position: ToVectorDocument(joint.Anchor), Velocity: ToVectorDocument(joint.ConnectedAnchor), Radius: joint.Distance, Speed: joint.Frequency, Intensity: joint.DampingRatio, MoveSpeed: joint.MotorSpeed, JumpSpeed: joint.MaxMotorForce),
            PlatformerMovementComponent platformer => new ComponentDocument("PlatformerMovement", MoveSpeed: platformer.MoveSpeed, JumpSpeed: platformer.JumpSpeed),
            AnimationPlayer animation => ToAnimationPlayerDocument(animation),
            Animator animator => ToAnimatorDocument(animator),
            Tilemap tilemap => new ComponentDocument("Tilemap", Width: tilemap.Width, Height: tilemap.Height, TileSize: ToVectorDocument(tilemap.TileSize), SortingOrder: tilemap.SortingOrder, RenderLayer: tilemap.RenderLayer.ToString(), Material: ToMaterialDocument(tilemap.Material), Definitions: tilemap.Definitions.Values.OrderBy(definition => definition.Id).Select(ToTileDefinitionDocument).ToArray(), Tiles: ToTileRows(tilemap)),
            AuthoredScriptComponent script => new ComponentDocument("AuthoredScript", ScriptClass: script.ClassName, ScriptPath: script.ScriptPath, ScriptDescription: script.Description, ScriptProperties: script.Properties.OrderBy(pair => pair.Key).Select(pair => new ScriptPropertyDocument(pair.Key, pair.Value)).ToArray()),
            PrefabInstanceComponent prefab => new ComponentDocument("PrefabInstance", PrefabPath: prefab.PrefabPath, PrefabIsConnected: prefab.IsConnected, PrefabOverrides: prefab.Overrides.Select(prefabOverride => new PrefabOverrideDocument(prefabOverride.EntityPath, prefabOverride.PropertyPath, prefabOverride.Value)).ToArray()),
            Light2D light => new ComponentDocument("Light2D", LightType: light.LightType.ToString(), Color: ToColorString(light.Color), Intensity: light.Intensity, Radius: light.Radius, SpotAngle: light.SpotAngle, RenderLayer: light.LayerMask.ToString()),
            SortingGroup2D group => new ComponentDocument("SortingGroup2D", SortingOrder: group.SortingOrderOffset, RenderLayer: group.Layer.ToString()),
            CanvasComponent canvas => new ComponentDocument("Canvas", SortingOrder: canvas.SortingOrder, Text: canvas.RenderMode.ToString(), Size: new Vector2Document(canvas.ReferenceResolution.Width, canvas.ReferenceResolution.Height), Intensity: canvas.ScaleFactor),
            RectTransformComponent rect => new ComponentDocument("RectTransform", Position: ToVectorDocument(rect.AnchoredPosition), Size: ToVectorDocument(rect.Size), Text: rect.Anchor.ToString(), Velocity: ToVectorDocument(rect.Pivot)),
            UIPanelComponent panel => new ComponentDocument("UIPanel", Color: ToColorString(panel.BackgroundColor), OutlineColor: ToColorString(panel.BorderColor), Radius: panel.BorderThickness),
            UIImageComponent image => new ComponentDocument("UIImage", Color: ToColorString(image.Tint), ScriptPath: image.SourcePath),
            UITextComponent text => new ComponentDocument("UIText", Text: text.Text, ScriptClass: text.FontFamily, FontSize: text.FontSize, Color: ToColorString(text.Color), ScriptDescription: text.Alignment.ToString()),
            UIButtonComponent button => new ComponentDocument("UIButton", Text: button.Label, Color: ToColorString(button.NormalColor), OutlineColor: ToColorString(button.HighlightedColor), ScriptDescription: button.ActionName),
            UISliderComponent slider => new ComponentDocument("UISlider", Speed: slider.Value, MoveSpeed: slider.MinValue, JumpSpeed: slider.MaxValue, Color: ToColorString(slider.FillColor)),
            UILayoutGroupComponent layout => new ComponentDocument("UILayoutGroup", Text: layout.Direction.ToString(), Speed: layout.Spacing),
            _ => throw new NotSupportedException($"Component type '{component.GetType().FullName}' is not supported by scene serialization."),
        };

        return document with { IsEnabled = component.IsEnabled };
    }

    private static ComponentDocument ToAnimationPlayerDocument(AnimationPlayer animation)
    {
        if (string.IsNullOrWhiteSpace(animation.Clip.AssetPath))
        {
            throw new NotSupportedException("Animation players can only be serialized when their clip was loaded by AssetManager.");
        }

        return new ComponentDocument("AnimationPlayer", AnimationClip: animation.Clip.AssetPath, PlaybackSpeed: animation.PlaybackSpeed, IsPlaying: animation.IsPlaying);
    }

    private static ComponentDocument ToAnimatorDocument(Animator animator)
    {
        if (string.IsNullOrWhiteSpace(animator.Controller.AssetPath))
        {
            throw new NotSupportedException("Animators can only be serialized when their controller was loaded by AssetManager.");
        }

        return new ComponentDocument("Animator", AnimatorController: animator.Controller.AssetPath, PlaybackSpeed: animator.PlaybackSpeed);
    }

    private static Component CreateComponent(ComponentDocument document, AssetManager? assets)
    {
        return document.Type switch
        {
            "EntityMotion" => new EntityMotionComponent(FromVectorDocument(document.Velocity)),
            "EntityInputMovement" => new EntityInputMovementComponent(document.Speed ?? 0.0f),
            "SpriteRenderer" => CreateSpriteRenderer(document, assets),
            "RigidBody2D" => new RigidBody2D { Velocity = FromVectorDocument(document.Velocity), GravityScale = document.GravityScale ?? 1.0f, IsKinematic = document.IsKinematic ?? false },
            "BoxCollider2D" => new BoxCollider2D(FromVectorDocument(document.Size)) { Offset = FromVectorDocument(document.Offset), IsTrigger = document.IsTrigger ?? false, Layer = (int)(document.Radius ?? 0) },
            "CircleCollider2D" => new CircleCollider2D(document.Radius ?? 32.0f) { Offset = FromVectorDocument(document.Offset), IsTrigger = document.IsTrigger ?? false },
            "CapsuleCollider2D" => new CapsuleCollider2D(FromVectorDocument(document.Size, new Vector2(48.0f, 96.0f))) { Offset = FromVectorDocument(document.Offset), IsTrigger = document.IsTrigger ?? false },
            "PolygonCollider2D" => CreatePolygonCollider(document),
            "TilemapCollider2D" => new TilemapCollider2D { Offset = FromVectorDocument(document.Offset), IsTrigger = document.IsTrigger ?? false, Layer = (int)(document.Radius ?? 0) },
            "PhysicsJoint2D" => new PhysicsJoint2D { JointType = Enum.TryParse(document.Text, true, out PhysicsJoint2DType jointType) ? jointType : PhysicsJoint2DType.Distance, Anchor = FromVectorDocument(document.Position), ConnectedAnchor = FromVectorDocument(document.Velocity), Distance = document.Radius ?? 96.0f, Frequency = document.Speed ?? 4.0f, DampingRatio = document.Intensity ?? 0.5f, MotorSpeed = document.MoveSpeed ?? 0.0f, MaxMotorForce = document.JumpSpeed ?? 1000.0f },
            "PlatformerMovement" => new PlatformerMovementComponent(document.MoveSpeed ?? 0.0f, document.JumpSpeed ?? 0.0f),
            "AnimationPlayer" => CreateAnimationPlayer(document, assets),
            "Animator" => CreateAnimator(document, assets),
            "Tilemap" => CreateTilemap(document, assets),
            "AuthoredScript" => CreateAuthoredScript(document),
            "PrefabInstance" => CreatePrefabInstance(document),
            "Light2D" => CreateLight2D(document),
            "SortingGroup2D" => new SortingGroup2D { SortingOrderOffset = document.SortingOrder ?? 0, Layer = ParseRenderLayer(document.RenderLayer) },
            "Canvas" => new CanvasComponent { SortingOrder = document.SortingOrder ?? 1000, RenderMode = Enum.TryParse(document.Text, true, out CanvasRenderMode mode) ? mode : CanvasRenderMode.ScreenSpaceOverlay, ReferenceResolution = ToSizeF(FromVectorDocument(document.Size, new Vector2(1280, 720))), ScaleFactor = document.Intensity ?? 1.0f },
            "RectTransform" => new RectTransformComponent { AnchoredPosition = FromVectorDocument(document.Position), Size = FromVectorDocument(document.Size, new Vector2(160, 48)), Anchor = Enum.TryParse(document.Text, true, out UIAnchorPreset anchor) ? anchor : UIAnchorPreset.Center, Pivot = FromVectorDocument(document.Velocity, new Vector2(0.5f, 0.5f)) },
            "UIPanel" => new UIPanelComponent { BackgroundColor = FromColorString(document.Color ?? "#BE181C24"), BorderColor = FromColorString(document.OutlineColor ?? "#DC5A6982"), BorderThickness = document.Radius ?? 1.0f },
            "UIImage" => new UIImageComponent { Tint = FromColorString(document.Color ?? "#FFFFFFFF"), SourcePath = document.ScriptPath },
            "UIText" => new UITextComponent { Text = document.Text ?? "Text", FontFamily = document.ScriptClass ?? "Segoe UI", FontSize = document.FontSize ?? 16.0f, Color = FromColorString(document.Color ?? "#FFFFFFFF"), Alignment = Enum.TryParse(document.ScriptDescription, true, out ContentAlignment alignment) ? alignment : ContentAlignment.MiddleCenter },
            "UIButton" => new UIButtonComponent { Label = document.Text ?? "Button", NormalColor = FromColorString(document.Color ?? "#E6384868"), HighlightedColor = FromColorString(document.OutlineColor ?? "#F55470A4"), ActionName = document.ScriptDescription },
            "UISlider" => new UISliderComponent { Value = document.Speed ?? 0.5f, MinValue = document.MoveSpeed ?? 0.0f, MaxValue = document.JumpSpeed ?? 1.0f, FillColor = FromColorString(document.Color ?? "#FF00BFFF") },
            "UILayoutGroup" => new UILayoutGroupComponent { Direction = Enum.TryParse(document.Text, true, out UILayoutDirection direction) ? direction : UILayoutDirection.Vertical, Spacing = document.Speed ?? 8.0f },
            _ => throw new NotSupportedException($"Scene component type '{document.Type}' is not supported."),
        };
    }


    private static PolygonCollider2D CreatePolygonCollider(ComponentDocument document)
    {
        PolygonCollider2D polygon = new() { Offset = FromVectorDocument(document.Offset), IsTrigger = document.IsTrigger ?? false };
        polygon.Points.Clear();
        foreach (string row in document.Tiles ?? [])
        {
            string[] parts = row.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && float.TryParse(parts[0], out float x) && float.TryParse(parts[1], out float y)) polygon.Points.Add(new Vector2(x, y));
        }
        if (polygon.Points.Count == 0) polygon.Points.AddRange([new Vector2(-24, -24), new Vector2(24, -24), new Vector2(24, 24), new Vector2(-24, 24)]);
        return polygon;
    }

    private static PrefabInstanceComponent CreatePrefabInstance(ComponentDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.PrefabPath))
        {
            throw new InvalidDataException("Serialized prefab instances must specify a prefabPath.");
        }

        PrefabInstanceComponent instance = new(document.PrefabPath);
        instance.SetOverrides((document.PrefabOverrides ?? []).Select(prefabOverride => new PrefabOverride(prefabOverride.EntityPath, prefabOverride.PropertyPath, prefabOverride.Value)));
        if (document.PrefabIsConnected == false)
        {
            instance.Unpack();
        }

        return instance;
    }

    private static AuthoredScriptComponent CreateAuthoredScript(ComponentDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.ScriptClass) || string.IsNullOrWhiteSpace(document.ScriptPath))
        {
            throw new InvalidDataException("Serialized authored scripts must specify scriptClass and scriptPath.");
        }

        AuthoredScriptComponent script = new(document.ScriptClass, document.ScriptPath)
        {
            Description = document.ScriptDescription ?? "Authored gameplay script",
        };

        foreach (ScriptPropertyDocument property in document.ScriptProperties ?? [])
        {
            script.Properties[property.Name] = property.Value;
        }

        return script;
    }

    private static SpriteRenderer CreateSpriteRenderer(ComponentDocument document, AssetManager? assets)
    {
        SpriteRenderer sprite = new(FromVectorDocument(document.Size), FromColorString(document.Color ?? "#FFFFFFFF"))
        {
            OutlineColor = document.OutlineColor is null ? null : FromColorString(document.OutlineColor),
            SortingOrder = document.SortingOrder ?? 0,
            Frame = FromFrameReference(document.Frame, assets),
            PrimitiveType = Enum.TryParse(document.PrimitiveType, ignoreCase: true, out SpritePrimitiveType primitiveType) ? primitiveType : SpritePrimitiveType.Rectangle,
            RenderLayer = ParseRenderLayer(document.RenderLayer),
            Material = FromMaterialDocument(document.Material),
        };

        return sprite;
    }

    private static Light2D CreateLight2D(ComponentDocument document) => new()
    {
        LightType = Enum.TryParse(document.LightType, ignoreCase: true, out Light2DType lightType) ? lightType : Light2DType.Point,
        Color = FromColorString(document.Color ?? "#FFFFFFFF"),
        Intensity = document.Intensity ?? 1.0f,
        Radius = document.Radius ?? 240.0f,
        SpotAngle = document.SpotAngle ?? 45.0f,
        LayerMask = ParseRenderLayer(document.RenderLayer),
    };

    private static RenderLayerMask ParseRenderLayer(string? value) =>
        Enum.TryParse(value, ignoreCase: true, out RenderLayerMask layer) ? layer : RenderLayerMask.Default;

    private static MaterialDocument ToMaterialDocument(Material2D material) => new(
        material.Name,
        ToColorString(material.Tint),
        material.BlendMode.ToString(),
        material.TexturePath,
        material.NormalMapPath,
        material.Shader,
        material.ReceivesLighting);

    private static Material2D FromMaterialDocument(MaterialDocument? document) => document is null
        ? new Material2D()
        : new Material2D
        {
            Name = document.Name ?? "Default 2D Material",
            Tint = FromColorString(document.Tint ?? "#FFFFFFFF"),
            BlendMode = Enum.TryParse(document.BlendMode, ignoreCase: true, out MaterialBlendMode blendMode) ? blendMode : MaterialBlendMode.AlphaBlend,
            TexturePath = document.TexturePath,
            NormalMapPath = document.NormalMapPath,
            Shader = document.Shader ?? "Sprites/Lit",
            ReceivesLighting = document.ReceivesLighting ?? true,
        };

    private static AnimationPlayer CreateAnimationPlayer(ComponentDocument document, AssetManager? assets)
    {
        if (string.IsNullOrWhiteSpace(document.AnimationClip))
        {
            throw new InvalidDataException("Serialized animation players must specify an animation clip asset path.");
        }

        if (assets is null)
        {
            throw new InvalidDataException("Scene contains animation clip references, but no AssetManager was provided.");
        }

        AnimationPlayer player = new(assets.LoadAnimationClip(document.AnimationClip))
        {
            PlaybackSpeed = document.PlaybackSpeed ?? 1.0f,
        };

        if (document.IsPlaying == false)
        {
            player.Pause();
        }

        return player;
    }

    private static Animator CreateAnimator(ComponentDocument document, AssetManager? assets)
    {
        if (string.IsNullOrWhiteSpace(document.AnimatorController))
        {
            throw new InvalidDataException("Serialized animators must specify an animator controller asset path.");
        }

        if (assets is null)
        {
            throw new InvalidDataException("Scene contains animator controller references, but no AssetManager was provided.");
        }

        return new Animator(assets.LoadAnimatorController(document.AnimatorController))
        {
            PlaybackSpeed = document.PlaybackSpeed ?? 1.0f,
        };
    }

    private static Tilemap CreateTilemap(ComponentDocument document, AssetManager? assets)
    {
        Tilemap tilemap = new(document.Width ?? 1, document.Height ?? 1, FromVectorDocument(document.TileSize))
        {
            SortingOrder = document.SortingOrder ?? 0,
            RenderLayer = ParseRenderLayer(document.RenderLayer),
            Material = FromMaterialDocument(document.Material),
        };

        foreach (TileDefinitionDocument definitionDocument in document.Definitions ?? [])
        {
            tilemap.SetDefinition(new TileDefinition(definitionDocument.Id, FromColorString(definitionDocument.Color), definitionDocument.IsSolid)
            {
                Frame = FromFrameReference(definitionDocument.Frame, assets),
            });
        }

        string[] rows = document.Tiles ?? [];
        for (int y = 0; y < Math.Min(rows.Length, tilemap.Height); y++)
        {
            int[] cells = rows[y].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
            for (int x = 0; x < Math.Min(cells.Length, tilemap.Width); x++)
            {
                tilemap.SetTile(x, y, cells[x]);
            }
        }

        return tilemap;
    }

    private static TileDefinitionDocument ToTileDefinitionDocument(TileDefinition definition)
    {
        return new TileDefinitionDocument(definition.Id, ToColorString(definition.Color), definition.IsSolid, ToFrameReference(definition.Frame));
    }

    private static string[] ToTileRows(Tilemap tilemap)
    {
        string[] rows = new string[tilemap.Height];
        for (int y = 0; y < tilemap.Height; y++)
        {
            int[] cells = new int[tilemap.Width];
            for (int x = 0; x < tilemap.Width; x++)
            {
                cells[x] = tilemap.GetTile(x, y);
            }

            rows[y] = string.Join(',', cells);
        }

        return rows;
    }

    private static FrameReferenceDocument? ToFrameReference(SpriteFrame? frame)
    {
        return frame is null || string.IsNullOrWhiteSpace(frame.SpriteSheetAssetPath)
            ? null
            : new FrameReferenceDocument(frame.SpriteSheetAssetPath, frame.Name);
    }

    private static SpriteFrame? FromFrameReference(FrameReferenceDocument? frame, AssetManager? assets)
    {
        if (frame is null)
        {
            return null;
        }

        if (assets is null)
        {
            throw new InvalidDataException("Scene contains sprite frame references, but no AssetManager was provided.");
        }

        return assets.LoadSpriteSheet(frame.SpriteSheet).GetFrame(frame.Name);
    }

    private static TransformDocument ToTransformDocument(Transform2D transform)
    {
        return new TransformDocument(ToVectorDocument(transform.Position), transform.Rotation, ToVectorDocument(transform.Scale));
    }

    private static void ApplyTransform(Transform2D transform, TransformDocument? document)
    {
        if (document is null)
        {
            return;
        }

        transform.Position = FromVectorDocument(document.Position);
        transform.Rotation = document.Rotation;
        transform.Scale = FromVectorDocument(document.Scale, Vector2.One);
    }

    private static Vector2Document ToVectorDocument(Vector2 vector) => new(vector.X, vector.Y);

    private static SizeF ToSizeF(Vector2 vector) => new(vector.X, vector.Y);

    private static Vector2 FromVectorDocument(Vector2Document? document, Vector2 defaultValue = default)
    {
        return document is null ? defaultValue : new Vector2(document.X, document.Y);
    }

    private static string ToColorString(Color color) => FormattableString.Invariant($"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}");

    private static Color FromColorString(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith('#') || value.Length != 9)
        {
            throw new InvalidDataException($"Color '{value}' must use #AARRGGBB format.");
        }

        return Color.FromArgb(Convert.ToInt32(value[1..3], 16), Convert.ToInt32(value[3..5], 16), Convert.ToInt32(value[5..7], 16), Convert.ToInt32(value[7..9], 16));
    }

    private sealed record SceneDocument(int SchemaVersion, string Name, EntityDocument[]? Entities);

    private sealed record EntityDocument(string Name, bool IsEnabled, TransformDocument? Transform, ComponentDocument[]? Components, EntityDocument[]? Children = null);

    private sealed record TransformDocument(Vector2Document Position, float Rotation, Vector2Document Scale);

    private sealed record Vector2Document(float X, float Y);

    private sealed record FrameReferenceDocument(string SpriteSheet, string Name);

    private sealed record TileDefinitionDocument(int Id, string Color, bool IsSolid, FrameReferenceDocument? Frame);

    private sealed record ScriptPropertyDocument(string Name, string Value);

    private sealed record PrefabOverrideDocument(string EntityPath, string PropertyPath, string? Value);

    private sealed record MaterialDocument(string? Name, string? Tint, string? BlendMode, string? TexturePath, string? NormalMapPath, string? Shader, bool? ReceivesLighting);

    private sealed record ComponentDocument(
        string Type,
        bool IsEnabled = true,
        Vector2Document? Velocity = null,
        float? Speed = null,
        Vector2Document? Size = null,
        string? Color = null,
        string? OutlineColor = null,
        int? SortingOrder = null,
        FrameReferenceDocument? Frame = null,
        string? PrimitiveType = null,
        float? GravityScale = null,
        bool? IsKinematic = null,
        Vector2Document? Offset = null,
        bool? IsTrigger = null,
        float? MoveSpeed = null,
        float? JumpSpeed = null,
        string? AnimationClip = null,
        string? AnimatorController = null,
        float? PlaybackSpeed = null,
        bool? IsPlaying = null,
        int? Width = null,
        int? Height = null,
        Vector2Document? TileSize = null,
        TileDefinitionDocument[]? Definitions = null,
        string[]? Tiles = null,
        string? ScriptClass = null,
        string? ScriptPath = null,
        string? ScriptDescription = null,
        ScriptPropertyDocument[]? ScriptProperties = null,
        string? PrefabPath = null,
        bool? PrefabIsConnected = null,
        PrefabOverrideDocument[]? PrefabOverrides = null,
        string? RenderLayer = null,
        MaterialDocument? Material = null,
        string? LightType = null,
        float? Intensity = null,
        float? Radius = null,
        float? SpotAngle = null,
        Vector2Document? Position = null,
        string? Text = null,
        float? FontSize = null);
}
