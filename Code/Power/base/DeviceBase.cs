namespace Harmonie.PowerSpace;

public abstract class DeviceBase : Component
{
    [Property] public string Name { get; set; } = "Device";
    [Property] public float PowerConsumption { get; set; } = 10.0f; // in W
    private float _powerIn = 0.0f;
    [Property]
    public float PowerIn
    {
        get => _powerIn;
        set
        {
            if ( _powerIn == value )
                return;

            _powerIn = value;
            OnPowerChange();
        }
    }

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

            if (!_isActive)
                PowerIn = 0.0f;

            if ( ActiveSoundPoint is not null && ActiveSound is not null )
                    ActiveSoundPoint.Enabled = _isActive;

            OnPowerChange();
        }
    }
    public bool IsPowered
    {
        get => _powerIn >= PowerConsumption;
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

    protected override void OnStart()
    {
        if (ActiveSoundPoint is not null)
        {
            ActiveSoundPoint.Enabled = IsActive;
            ActiveSoundPoint.SoundEvent = ActiveSound;
        }
    }

    // Called when a change in the device's state occurs
    public virtual void OnPowerChange()
    {
        ActiveSoundPoint.Enabled = IsActive;
        // This method can be overridden by derived classes to handle power state changes
        // For example, you might want to toggle device state or update UI elements
    }
}