namespace Harmonie.PowerSpace;

public sealed class CustomLamp : DeviceBase
{
	[Property] public ModelRenderer LampModel { get; set; } = null;
	[Property] public PointLight Light { get; set; } = null;

	public override void OnPowerChange()
	{
		if ( LampModel is not null )
			LampModel.MaterialGroup = IsRunning ? "lit" : "default";
		if ( Light is not null )
			Light.Enabled = IsRunning;
	}
}
