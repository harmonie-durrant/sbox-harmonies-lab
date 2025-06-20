namespace Harmonie.PowerSpace;

public sealed class Plug : Component
{
    [Property] public Battery ConnectedBattery { get; set; }
    [Property] public DeviceBase ConnectedDevice { get; set; }
    [Property] public bool IsInput { get; set; } = true; // true: device input, false: output (e.g. charger)
}
