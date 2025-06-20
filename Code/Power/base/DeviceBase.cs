namespace Harmonie.PowerSpace;

public abstract class DeviceBase : Component
{
    [Property] public string Name { get; set; } = "Device";
    [Property] public float PowerConsumption { get; set; } = 10.0f; // in W

    // Indicates whether the device is on or off
    private bool _isActive = false;
    [Property]
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if ( _isActive == value )
                return;

            _isActive = value;

            if ( ActiveSoundPoint is not null && ActiveSound is not null )
                ActiveSoundPoint.Enabled = _isActive;

            OnPowerChange();
        }
    }

    // Indicates whether the device is receiving power
	private bool _isPowered = false;
    [Property]
    public bool IsPowered
    {
        get => _isPowered;
        set
        {
            if ( _isPowered == value )
                return;

            _isPowered = value;
            OnPowerChange();
        }
    }

    // Indicates whether the device is currently running (active and powered)
    [Property]
    public bool IsRunning
    {
        get => IsActive && IsPowered;
    }

    // Shounds for the device when it is active
    [Property] public SoundEvent ActiveSound { get; set; } = null;
	[Property] public SoundPointComponent ActiveSoundPoint { get; set; } = null;

    // Called when a change in the device's state occurs
    public virtual void OnPowerChange()
    {
        // This method can be overridden by derived classes to handle power state changes
        // For example, you might want to toggle device state or update UI elements
        Log.Info( $"Power state changed: {(IsRunning ? "Running" : "Not running")}" );
    }
}