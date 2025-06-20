namespace Harmonie.UI;

public sealed class WorldPanelLookAtPlayer : Component
{
	[Property] public GameObject PanelObject { get; set; } = null;
	[Property] float _maxRotationX = 45.0f; // Maximum rotation around the X-axis in degrees
	[Property] float _minRotationX = -45.0f; // Minimum rotation around the X-axis in degrees

	private CameraComponent _target_camera = null;

	private Rotation GetTargetRotationFromCamera( Rotation cameraRotation )
	{
		if ( PanelObject is null ) return Rotation.Identity;

		// Get the camera's forward direction
		var cameraForward = cameraRotation.Forward;
		var lookRotation = Rotation.LookAt( cameraForward, Vector3.Up );
		var euler = lookRotation.Angles();

		// Clamp the pitch (X axis)
		euler.pitch = euler.pitch.Clamp( _minRotationX, _maxRotationX );

		// Rotate 180 degrees around Y axis to face the correct direction
		return Rotation.From( euler ) * Rotation.FromYaw( 180 );
	}

	protected override void OnUpdate()
	{
		if ( PanelObject is null ) return;

		if ( _target_camera is null )
		{
			_target_camera = Game.ActiveScene.GetAllComponents<CameraComponent>()
				.FirstOrDefault( c => !c.IsProxy );
			if ( _target_camera is null )
			{
				Log.Error( "WorldPanelLookAtPlayer: No local camera found in the scene." );
				return;
			}
		}
		// Set PanelObject to match the camera's rotation (with clamped pitch)
		var cameraRotation = _target_camera.WorldRotation;
		var targetLookRotation = GetTargetRotationFromCamera( cameraRotation );
		PanelObject.WorldRotation = targetLookRotation;
	}
}
