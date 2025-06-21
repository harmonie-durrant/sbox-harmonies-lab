using Harmonie.PowerSpace;

namespace Harmonie.PlayerSpace;

public sealed class PlayerUnPlug : Component
{
    [Property] public CameraComponent TargetCamera { get; set; }

    private GameObject GetLookingAtObject( string tag )
    {
        if (TargetCamera is null) return null;

        var rayOrigin = TargetCamera.WorldPosition;
        var rayDirection = TargetCamera.WorldRotation.Forward;
        var trace = Scene.Trace.Ray( rayOrigin, rayOrigin + rayDirection * 1000f )
            .IgnoreGameObject( TargetCamera.GameObject )
            .HitTriggersOnly()
            .WithTag(tag)
            .Run();

        return trace.Hit ? trace.GameObject : null;
    }

    protected override void OnFixedUpdate()
    {
        // When player hits use button
        if (Input.Pressed("Use"))
        {
            // Get the object the player is looking at (now only with tag "femaleplug")
            GameObject lookingAtObject = GetLookingAtObject("femaleplug");
            if (lookingAtObject is not null)
            {
                FemalePlugDetector detector = lookingAtObject.GetComponent<FemalePlugDetector>();
                if (detector is not null && detector.IsPluggedIn)
                {
                    detector.DisconnectPlug();
                    return;
                }
            }
        }
    }
}
