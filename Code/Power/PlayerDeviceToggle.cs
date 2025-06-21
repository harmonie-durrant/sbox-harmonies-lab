using Harmonie.PowerSpace;

namespace Harmonie.PlayerSpace;

public sealed class PlayerDeviceToggle : Component
{
    [Property] public CameraComponent TargetCamera { get; set; }

    private GameObject GetLookingAtObject( string tag )
    {
        if ( TargetCamera is null ) return null;

        var rayOrigin = TargetCamera.WorldPosition;
        var rayDirection = TargetCamera.WorldRotation.Forward;
        var trace = Scene.Trace.Ray( rayOrigin, rayOrigin + rayDirection * 1000f )
            .IgnoreGameObject( TargetCamera.GameObject )
            .WithTag( tag )
            .Run();

        return trace.Hit ? trace.GameObject : null;
    }

    protected override void OnFixedUpdate()
    {

        // When player hits use button
        if ( Input.Pressed( "Use" ) )
        {
            // Get the object the player is looking at
            GameObject lookingAtObject = GetLookingAtObject("interactable");
            if ( lookingAtObject is not null )
            {
                DeviceBase device = lookingAtObject.GetComponent<DeviceBase>();
                if ( device is not null )
                {
                    device.IsActive = !device.IsActive; // Toggle the device's active state
                    return;
                }
            }
        }
    }
}
