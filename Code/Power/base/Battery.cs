namespace Harmonie.PowerSpace;

public sealed class Battery : Component
{
	[Property] public string Name { get; set; } = "Battery";

	[Property] public bool IsInstantRelease { get; set; } = false;

	[Property] public float CurrentCharge { get; set; } = 50.0f; // in Ah
	[Property] public float MaxCharge { get; set; } = 100.0f; // in Ah
	[Property] public float Voltage { get; set; } = 12.0f; // in V
	[Property] public float MaxDischargeRate { get; set; } = 10.0f; // in A
	[Property] public float MaxChargeRate { get; set; } = 5.0f; // in A
	[Property] public float InternalResistance { get; set; } = 0.05f; // in Ohms
	[Property] public int ChargeStatus { get; set; } = 0; // 1: charging, 0: idle, -1: discharging
	[Property] public float Temperature { get; set; } = 25.0f; // in °C
	[Property] public float MinVoltage { get; set; } = 10.0f; // in V
	[Property] public float MaxVoltage { get; set; } = 14.4f; // in V

	[Property] public List<Plug> Connections { get; set; } = new();

	// Returns the current state of charge as a percentage
	public float GetChangePercentage() => MathF.Round((CurrentCharge / MaxCharge) * 100.0f, 2);
	public float GetChangeToDecimals(int dp) => MathF.Round(CurrentCharge, dp);

	// Returns true if the battery is depleted
	public bool IsDepleted() => CurrentCharge <= 0.0f;

	// Returns true if the battery is fully charged
	public bool IsFull() => CurrentCharge >= MaxCharge;

	// Simulate charging the battery
	public bool Charge( float amount )
	{
		if ( IsFull() ) return false;
		if ( amount < 0.0f ) return false;
		CurrentCharge = MathF.Min( CurrentCharge + amount, MaxCharge );
		return true;
	}

	// Simulate discharging the battery
	public bool Discharge( float amount )
	{
		if ( IsDepleted() ) return false;
		if ( amount < 0.0f ) return false;
		CurrentCharge = MathF.Max( CurrentCharge - amount, 0.0f );
		return true;
	}

	// Calculate output voltage under load (simple model)
	public float GetOutputVoltage(float loadCurrent)
	{
		return Voltage - (loadCurrent * InternalResistance);
	}
	
	public void RemoveConnection(Plug plug)
	{
		if ( plug is null ) return;
		if ( Connections.Contains( plug ) )
		{
			Connections.Remove( plug );
			plug.ConnectedDevice.PowerIn = 0; // Ensure the device is no longer powere by this battery
			plug.ConnectedBattery = null; // Clear the connected battery reference if not already done
		}
	}

	public void DistributeInstantRelease()
	{
		if ( !IsInstantRelease ) return;
		// Get power to distribute
		float power = 0.0f; // in w
		foreach ( Plug plug in Connections )
		{
			if ( plug is null || plug.ConnectedDevice is null ) continue;
			DeviceBase device = plug.ConnectedDevice;
			if ( !device.IsActive )
				continue;
			if ( device.PowerConsumption <= 0 )
				power += -device.PowerConsumption;
		}
		foreach ( Plug plug in Connections )
		{
			if ( plug is null || plug.ConnectedDevice is null ) continue;
			DeviceBase device = plug.ConnectedDevice;
			if ( device.PowerConsumption < 0 )
				continue;
			if ( power <= 0.0f )
			{
				device.PowerIn = 0; // No power
				continue;
			}
			if ( !device.IsActive )
				continue;
			device.PowerIn = MathF.Min( device.PowerConsumption, power );
			power -= device.PowerConsumption;
		}
	}

	protected override void OnUpdate()
	{
		if ( IsInstantRelease )
		{
			DistributeInstantRelease();
			return;
		}
		float status = 0;
		// Simulate charging/discharging via plugs
		foreach ( Plug plug in Connections )
		{
			if ( plug is null || plug.ConnectedDevice is null ) continue;
			DeviceBase device = plug.ConnectedDevice;

			float power = device.PowerConsumption; // Watts
			if ( power <= 0 )
			{
				device.PowerIn = 0; // No power
				continue;
			}

			float dt = Time.Delta;
			float energy = power * dt / 3600.0f; // Wh to Ah (assuming Voltage is constant)
			float chargeAmount = energy / Voltage; // Ah
			if ( !device.IsActive )
			{
				device.PowerIn = (CurrentCharge > 0) ? device.PowerConsumption : 0;
				continue;
			}
			bool powered = false;
			if ( power > 0 )
				powered = Discharge( chargeAmount );
			else
				powered = Charge( -chargeAmount );
			device.PowerIn = powered ? device.PowerConsumption : 0;; // Device is powered
			if ( powered )
				status += -chargeAmount;
		}
		ChargeStatus = status > 0 ? 1 : (status < 0 ? -1 : 0);
	}
}
