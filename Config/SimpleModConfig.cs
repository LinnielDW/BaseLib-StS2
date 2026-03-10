using System.Reflection;
using Godot;

namespace BaseLib.Config;

public class SimpleModConfig : ModConfig
{
    public override void SetupConfigUI(Control optionContainer)
    {
        VBoxContainer options = new();

        MainFile.Logger.Info($"Setting up SimpleModConfig {GetType().FullName}");
        
        options.Size = optionContainer.Size;
        options.AddThemeConstantOverride("separation", 8);
        optionContainer.AddChild(options);

        Type? t = null;
        Control? current = null;
        try
        {
            foreach (var property in ConfigProperties)
            {
                t = property.PropertyType;
                var previous = current;
                //Special case
                if (t.IsEnum)
                {
                    current = Generators[typeof(Enum)](this, options, property);
                }
                else
                {
                    current = Generators[t](this, options, property);
                }
                
                if (previous == null) continue;
                
                if (current.FocusNeighborBottom == null) MainFile.Logger.Info("NEIGHBOR DEFAULT NULL");
                else MainFile.Logger.Info($"NEIGHBOR DEFAULT: {current.FocusNeighborBottom}");
                NodePath path = current.GetPathTo(previous);
                current.FocusNeighborLeft ??= path;
                current.FocusNeighborTop ??= path;
                path = previous.GetPathTo(current);
                previous.FocusNeighborRight ??= path;
                previous.FocusNeighborBottom ??= path;
            }
        }
        catch (KeyNotFoundException)
        {
            MainFile.Logger.Error($"Attempted to construct SimpleModConfig with unsupported type {t?.FullName}");
        }
    }

    private static readonly Dictionary<Type, Func<ModConfig, Control, PropertyInfo, Control>> Generators = new()
    {
        { 
            typeof(bool),
            (cfg, control, property) => cfg.MakeToggleOption(control, property)
        },
        { 
            typeof(Enum),
            (cfg, control, property) => cfg.MakeDropdownOption(control, property)
        }
    };
}