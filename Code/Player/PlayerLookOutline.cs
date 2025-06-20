namespace Harmonie.PlayerSpace;

public sealed class PlayerLookOutline : Component
{
    [Property] public float OutlineThickness { get; set; } = 0.5f;
    [Property] public Color OutlineColor { get; set; } = Color.White;

    [Property] public CameraComponent TargetCamera { get; set; }
    
    private List<HighlightOutline> _highlightedOutlines = new();

    private void ClearHighlightedOutlines()
    {
        foreach ( var outline in _highlightedOutlines )
        {
            outline.Width = 0f; // Reset outline thickness
        }
        _highlightedOutlines.Clear();
    }

    private GameObject GetLookingAtObject()
    {
        if ( TargetCamera is null ) return null;

        // Raycast from the camera's world position in its forward direction, only hitting objects with the 'highlightable' tag
        var rayOrigin = TargetCamera.WorldPosition;
        var rayDirection = TargetCamera.WorldRotation.Forward;
        var trace = Scene.Trace.Ray( rayOrigin, rayOrigin + rayDirection * 1000f )
            .IgnoreGameObject( TargetCamera.GameObject )
            .WithTag( "highlightable" )
            .Run();

        return trace.Hit ? trace.GameObject : null;
    }

    protected override void OnFixedUpdate()
    {
        // Clear previous outlines
        ClearHighlightedOutlines();

        // Get the object the player is looking at
        var lookingAtObject = GetLookingAtObject();
        if ( lookingAtObject is null ) return;

        // Apply outline effect to the object being looked at
        HighlightOutline comp = lookingAtObject.GetComponent<HighlightOutline>();
        comp.Width = OutlineThickness;
        _highlightedOutlines.Add( comp );
    }
}
