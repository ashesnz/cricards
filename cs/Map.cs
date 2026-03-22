using Godot;
using System.Collections.Generic;
using Godot.Collections;

public partial class Map : Control
{
    [Signal] public delegate void ChosenEventHandler(Encounter encounter);

    [Export] public PackedScene dotted_line_scene;

    private Button back_button;
    private Encounter ice_cream_isaac;
    private Encounter muffin_max;
    private Encounter donut_daisy;
    private Label ascension_label;

    public override void _Ready()
    {
        back_button = GetNodeOrNull<Button>("BackButton");
        ice_cream_isaac = GetNodeOrNull<Encounter>("IceCreamIsaac");
        muffin_max = GetNodeOrNull<Encounter>("MuffinMax");
        donut_daisy = GetNodeOrNull<Encounter>("DonutDaisy");
        ascension_label = GetNodeOrNull<Label>("AscensionLabel");

        if (back_button != null)
            back_button.Pressed += _OnBackButtonPressed;

        foreach (object encObj in GetAllEncounters())
        {
            var enc = encObj as Encounter;
            if (enc != null)
            {
                // TextureButton.Pressed has no parameters; capture the encounter in a lambda.
                enc.Pressed += () => _OnEncounterPressed(enc);
            }
        }

        _CreateConnections();
    }

    public async void ReturnToMap()
    {
        Visible = true;
        GetTree().Paused = true;
        await ToSignal(GetTree().CreateTimer(0.25f), "timeout");
        GetTree().Paused = false;
    }

    public void Enable(bool is_win)
    {
        Visible = !Visible;
        if (Visible)
        {
            // play open
        }
        else
        {
            // play select
        }
    }

    public void Disable(Character character)
    {
        foreach (object encObj in GetAllEncounters())
        {
            var encounter = encObj as Encounter;
            if (encounter == null) continue;
            var cd = encounter.GetCharacterData();
            var targetCd = character.GetCharacterData();
            if (cd != null && targetCd != null && cd == targetCd)
                encounter.Disabled = true;
        }
    }

    public bool IsAllEncountersDefeated()
    {
        var defeated = 0;
        foreach (object encObj in GetAllEncounters())
        {
            var enc = encObj as Encounter;
            if (enc != null && enc.Disabled) defeated += 1;
        }
        return defeated == GetAllEncounters().Count;
    }

    public void EnableAllEncounters()
    {
        foreach (object encObj in GetAllEncounters())
        {
            var enc = encObj as Encounter;
            if (enc != null) enc.Disabled = false;
        }
    }

    private void _OnBackButtonPressed()
    {
        Visible = false;
        // map_select.play
    }

    // Public Back method so external callers can trigger the map back behavior
    public void Back()
    {
        _OnBackButtonPressed();
    }

    private void _OnEncounterPressed(Encounter encounter)
    {
        EmitSignal(SignalName.Chosen, encounter);
    }

    public Array GetAllEncounters()
    {
        var encounters = new Array();
        foreach (object childObj in GetChildren())
        {
            var node = childObj as Node;
            var e = node as Encounter;
            if (e != null)
                encounters.Add(e);
        }
        return encounters;
    }

    private void _CreateConnections()
    {
        var encounters = GetAllEncounters();
        var drawn_pairs = new Dictionary();
        foreach (object encounterObj in encounters)
        {
            var encounter = encounterObj as Encounter;
            if (encounter == null) continue;

            // Use the strongly-typed property on Encounter to get connections
            Godot.Collections.Array rawConnections = encounter.connections;
            if (rawConnections == null) continue;

            foreach (object connectionObj in rawConnections)
            {
                Encounter enc2 = null;

                // Direct Encounter reference
                enc2 = connectionObj as Encounter;

                if (enc2 == null)
                {
                    // Could be NodePath or string representation
                    NodePath np = new NodePath();
                    bool haveNp = false;
                    if (connectionObj is string)
                    {
                        np = new NodePath((string)connectionObj);
                        haveNp = true;
                    }
                    else if (connectionObj is NodePath)
                    {
                        np = (NodePath)connectionObj;
                        haveNp = true;
                    }

                    if (haveNp)
                    {
                        // Try resolving relative to the Map
                        enc2 = GetNodeOrNull<Encounter>(np);
                        // If still null, try resolving relative to the encounter node itself
                        if (enc2 == null)
                            enc2 = encounter.GetNodeOrNull<Encounter>(np);
                    }
                }

                if (enc2 == null)
                    continue;

                var pos1 = encounter.GetCenterPosition();
                var pos2 = enc2.GetCenterPosition();
                var pair = new Godot.Collections.Array { pos1, pos2 };
                pair.Sort();
                if (!drawn_pairs.ContainsKey(pair))
                {
                    var points = _GenerateTrailPoints(pos1, pos2, 10);
                    var line2DInst = dotted_line_scene.Instantiate();
                    var line2D = line2DInst as Node2D;
                    if (line2D != null)
                    {
                        AddChild(line2D);
                        foreach (object pointObj in points)
                        {
                            if (pointObj is Vector2 p)
                            {
                                var child = new Node2D();
                                child.Position = p;
                                line2D.AddChild(child);
                            }
                        }
                        drawn_pairs[pair] = true;
                    }
                }
            }
        }
    }

    private Array _GenerateTrailPoints(Vector2 start, Vector2 end, int num_points)
    {
        var points = new Array();
        var random_offset_range = 15;
        var rng = new RandomNumberGenerator();
        rng.Randomize();
        for (int i = 0; i < num_points; i++)
        {
            var t = (float)i / (num_points - 1);
            var interp_point = start + (end - start) * t;
            var offset = new Vector2(rng.RandfRange(-random_offset_range, random_offset_range), rng.RandfRange(-random_offset_range, random_offset_range));
            points.Add(interp_point + offset);
        }
        points.Add(end);
        return points;
    }
}

