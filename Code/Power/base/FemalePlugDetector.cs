namespace Harmonie.PowerSpace;

public sealed class FemalePlugDetector : Component, Component.ITriggerListener
{
	[Property] public GameObject TargetSnapObject { get; set; }

	[Property] public DeviceBase ConnectedDevice { get; set; } // The device that this plug is connected to, if any
	[Property] public Battery ConnectedBattery { get; set; } // The battery that this plug is connected to, if any

	private DeviceBase _connectedDevice { get; set; } // The device that this plug is connected to, if any
	private Battery _connectedBattery { get; set; } // The battery that this plug is connected to, if any

	[Property] public bool IsInput { get; set; } = true; // true: device input, false: output (e.g. charger)

	[Property] public bool IsPluggedIn { get; set; } = false;
	[Property] public SoundEvent PlugSound { get; set; }

	private readonly List<String> _validTags = new()
	{
		"unplugged-plug"
	};

	private GameObject _pluggedObject = null; // The object that is currently plugged in, if any

	/// <summary>
	/// Disconnects the plug from the target snap object and clears the connected device or battery.
	/// </summary>
	public void DisconnectPlug()
	{
		Log.Info( $"Disconnecting plug: {_pluggedObject?.Name}" );
		if ( _pluggedObject is null || TargetSnapObject is null ) return; // Prerequisites: must have a plugged object and a target snap object
		Plug plug = _pluggedObject.GetComponent<Plug>();
		if ( plug is null ) return;

		// Clear the connected device or battery using cache references
		if ( _connectedDevice is not null )
		{
			ConnectedDevice = null; // Clear the connected device
			_connectedDevice = null; // Clear the connected device Reference
			plug.ConnectedBattery = null; // Clear the plug's connected battery
		}
		ConnectedBattery.RemoveConnection( plug ); // Remove this plug from the battery's connections
		if ( _connectedBattery is not null )
		{
			ConnectedBattery = null; // Clear the connected battery
			_connectedBattery = null; // Clear the connected battery reference
			plug.ConnectedDevice = null; // Clear the plug's connected device
		}

		// Detach the object
		_pluggedObject.SetParent( null, true );
		// Reset the plug's tags
		_pluggedObject.Tags.Remove( "plugged-plug" );
		_pluggedObject.Tags.Add( "unplugged-plug" );
		_pluggedObject.Tags.Add( "solid" );
		// Move the plug away to avoid a reattachment loop
		_pluggedObject.WorldPosition = TargetSnapObject.WorldPosition + Vector3.Forward * 5f; // Move the plug slightly forward to avoid reattachment

		// Re-enable the plug's physics & collider
		Rigidbody rb = _pluggedObject.GetComponent<Rigidbody>( true );
		if ( rb is not null )
			rb.Enabled = true; // Re-enable physics for the plug
		BoxCollider boxCollider = _pluggedObject.GetComponent<BoxCollider>( true );
		if ( boxCollider is not null )
			boxCollider.Enabled = true; // Make sure the plug's collider is enabled

		// Clear cache references
		_connectedBattery = null;
		_connectedDevice = null;

		_pluggedObject = null; // Clear the plugged object reference
		IsPluggedIn = false; // Mark as unplugged

		return;
	}

	private void ConnectPlug( GameObject go )
	{
		_connectedBattery = null;
		_connectedDevice = null;
		Log.Info( $"Connecting plug: {go.Name}" );
		if ( go.Tags.Any( tag => _validTags.Contains( tag ) ) == false ) return; // Verify tags

		// Verify that the object has a Plug component
		Plug plug = go.GetComponent<Plug>();
		if ( plug is null ) return;

		// Make sure the plug is connected to the target type for what it is plugging into
		if ( ConnectedDevice is not null && plug.ConnectedDevice is not null ) return; // Device to device plugging is not allowed
		if ( ConnectedBattery is not null && plug.ConnectedBattery is not null ) return; // Battery to battery plugging is not allowed

		// Snap the plug to the target snap object
		go.WorldPosition = TargetSnapObject.WorldPosition;
		go.WorldRotation = TargetSnapObject.WorldRotation;

		// Set tags
		go.Tags.Remove( "unplugged-plug" );
		go.Tags.Remove( "solid" );
		go.Tags.Remove( "grabbed" );
		go.Tags.Add( "plugged-plug" );

		// Disable the plug's physics while it's plugged in
		Rigidbody rb = go.GetComponent<Rigidbody>();
		if ( rb is not null )
		{
			rb.Enabled = false; // Disable physics for the plug while it's plugged in
		}
		BoxCollider boxCollider = go.GetComponent<BoxCollider>();
		if ( boxCollider is not null )
		{
			boxCollider.Enabled = false; // Make sure the plug's collider is not enabled
		}

		// Link physics to the target snap object
		go.SetParent( TargetSnapObject, true );

		// Get the device or battery from the plug
		if ( plug.ConnectedDevice.IsValid() && ConnectedDevice is null )
		{
			ConnectedDevice = plug.ConnectedDevice;
			_connectedDevice = plug.ConnectedDevice; // Set the connected device reference
			plug.ConnectedBattery = ConnectedBattery; // Link the plug to the connected battery
		}
		if ( plug.ConnectedBattery.IsValid() && ConnectedBattery is null )
		{
			ConnectedBattery = plug.ConnectedBattery;
			_connectedBattery = plug.ConnectedBattery; // Set the connected battery reference
			plug.ConnectedDevice = ConnectedDevice; // Link the plug to the connected device
		}
		ConnectedBattery.Connections.Add( plug ); // Add this plug detector to the battery's connections

		_pluggedObject = go; // Set the plugged object reference
		IsPluggedIn = true; // Mark as plugged in
		Sound.Play( PlugSound );

		return;
	}

	public void OnTriggerEnter( Collider other )
	{
		if ( IsPluggedIn || TargetSnapObject is null ) return; // Must not be plugged in already and must have a target snap object
		if ( ConnectedBattery is null && ConnectedDevice is null ) return; // Must have a battery or device to connect to
		ConnectPlug( other.GameObject ); // Connect the plug
	}
}
