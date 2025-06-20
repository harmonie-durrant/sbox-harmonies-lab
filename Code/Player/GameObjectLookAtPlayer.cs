namespace Harmonie.UI;

public sealed class GameObjectLookAtPlayer : Component
{
	private GameObject _targetObject { get; set; }
	[Property] float MaxRotationX = 45.0f; // Maximum rotation around the X-axis in degrees
	[Property] float MinRotationX = -45.0f; // Minimum rotation around the X-axis in degrees
	[Property] bool Invert = false; // Invert the rotation around the Y-axis (If text is backwards for example)

	private CameraComponent _target_camera = null;

	protected override void OnStart()
	{
		base.OnStart();

		_targetObject = GameObject;
	}

	private Rotation GetTargetRotationFromCamera( Rotation cameraRotation )
	{
		if ( _targetObject is null ) return Rotation.Identity;

		// Get the camera's forward direction
		var cameraForward = cameraRotation.Forward;
		var lookRotation = Rotation.LookAt( cameraForward, Vector3.Up );
		var euler = lookRotation.Angles();

		// Clamp the pitch (X axis)
		euler.pitch = euler.pitch.Clamp( MinRotationX, MaxRotationX );

		var targetRotation = Rotation.From( euler );
		if ( Invert )
			targetRotation *= Rotation.FromYaw( 180 );
		return targetRotation;
	}

	protected override void OnUpdate()
	{
		if ( _targetObject is null ) return;

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
		// Set _targetObject to match the camera's rotation (with clamped pitch)
		var cameraRotation = _target_camera.WorldRotation;
		var targetLookRotation = GetTargetRotationFromCamera( cameraRotation );
		_targetObject.WorldRotation = targetLookRotation;
	}
}
