using static Sandbox.Citizen.CitizenAnimationHelper;

namespace Harmonie.PlayerSpace;

public class Hands : Component
{
	public bool IsActive { get; set; } = true;
	[Property] public GameObject Head { get; set; }
	[Property] public SoundEvent FreezeSound { get; set; }
	[Property] public float MaxPullDistance => 2000.0f;
	[Property] public float MaxPushDistance => 500.0f;
	[Property] public float LinearFrequency => 10.0f;
	[Property] public float LinearDampingRatio => 1.0f;
	[Property] public float AngularFrequency => 10.0f;
	[Property] public float AngularDampingRatio => 1.0f;
	[Property] public float PullForce => 20.0f;
	[Property] public float PushForce => 1000.0f;
	[Property] public float HoldDistance => 50.0f;
	[Property] public float AttachDistance => 150.0f;
	[Property] public float DropCooldown => 0.5f;
	[Property] public float BreakLinearForce => 2000.0f;

	public HoldTypes HoldType { get; set; } = HoldTypes.None;
	public Hand Handedness { get; set; } = Hand.Both;

	[Sync] public Vector3 HoldPos { get; set; }
	[Sync] public Rotation HoldRot { get; set; }
	[Sync, Property] public GameObject GrabbedObject { get; set; }
	[Sync] public Vector3 GrabbedPos { get; set; }
	[Sync] public int GrabbedBone { get; set; } = -1;

	private GameObject lastGrabbed;
	private PhysicsBody _heldBody;

	private Vector3 heldPos;
	private Rotation heldRot;
	private float currentHoldDistance;

	private TimeSince timeSinceImpulse;
	private TimeSince timeSinceDrop;

	/// <summary>
	/// Get the correct body from GrabbedObject/Bone. Similar to PhysGun's approach.
	/// </summary>
	private PhysicsBody HeldBody
	{
		get
		{
			if ( GrabbedObject != lastGrabbed && GrabbedObject != null )
			{
				_heldBody = GetBody( GrabbedObject, GrabbedBone );
				lastGrabbed = GrabbedObject;
			}
			return _heldBody;
		}
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();
		GrabbedObject = null;
	}

	public void SetActive()
	{
		IsActive = true;
	}

	public void SetInactive()
	{
		IsActive = false;

		GrabEnd();
	}

	[Rpc.Broadcast]
	private void SetHoldType( HoldTypes holdType )
	{
		HoldType = holdType;
	}

	[Rpc.Broadcast]
	private void SetHandedness( Hand handedness )
	{
		Handedness = handedness;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( !IsActive )
			return;

		OnControl();

		if ( !_heldBody.IsValid() )
			SetHoldType( HoldTypes.None );
		else
		{
			SetHoldType( HoldTypes.HoldItem );
			SetHandedness( Handedness );
		}

		MoveHeldObject();
	}

	private Angles GetEyeAngles()
	{
		return Head.IsValid() ? Head.WorldRotation.Angles() : Angles.Zero;
	}
	
	public void OnControl()
	{
		//Owner.Controller.EnablePressing = !GrabbedObject.IsValid();

		var eyePos = Head.WorldPosition;
		var eyeDir = GetEyeAngles().Forward;
		var eyeRot = GetEyeAngles();

		if ( HeldBody.IsValid() )
		{
			// If we are holding an object and it no longer has the tag "solid"
			if ( !GrabbedObject.Tags.Has( "solid" ) )
			{
				GrabEnd();
				return;
			}
			if ( Input.Pressed( "Attack1" ) )
			{
				GrabEnd();
			}
			else if ( Input.Pressed( "Attack2" ) )
			{
				Freeze( GrabbedObject, GrabbedBone );
				// Play freeze sound
				Sound.Play( FreezeSound );
				GrabEnd();
			}
			else
			{
				GrabMove( eyePos, eyeRot );
			}
			return;
		}

		if ( timeSinceDrop < DropCooldown )
			return;

		//if ( Input.Pressed( "Attack2" ) )
		//{
		//	TryPush( eyePos, eyeDir );
		//	return;
		//}

		if ( Input.Down( "Attack1" ) )
		{
			var tr = DoPickupTrace( eyePos, eyeDir );

			if ( !tr.Hit || !tr.GameObject.IsValid() || !tr.Body.IsValid() || tr.Component is MapCollider )
				return;

			// Make sure it's not already "grabbed"
			if ( tr.GameObject.Tags.Has( "grabbed" ) )
				return;

			// If the object is too far to attach, try pulling
			var attachPos = tr.Body.FindClosestPoint( eyePos );
			if ( eyePos.Distance( attachPos ) <= AttachDistance )
			{
				// Close enough => pick it up
				bool isRagdoll = tr.GameObject.Components.Get<ModelPhysics>().IsValid();
				var boneIndex = isRagdoll ? tr.Body.GroupIndex : -1;

				// If it's frozen, unfreeze it so we can pick up in one go
				if ( tr.Body.BodyType == PhysicsBodyType.Static )
				{
					// "Immediate" unfreeze so we don't wait for an RPC
					UnFreezeImmediate( tr.GameObject, boneIndex );
				}

				GrabStart( tr.GameObject, tr.Body, eyePos, eyeDir, attachPos );
			}
			else
			{
				// Not close => apply "pull" impulse to bring it nearer
				TryPull( tr, eyeDir );
			}
		}
	}

	/// <summary>
	/// Smoothly moves the held object to our current HoldPos/HoldRot each frame.
	/// </summary>
	[Rpc.Broadcast]
	private void MoveHeldObject()
	{
		// If no grabbed object or we've just impulse'd
		if ( !GrabbedObject.IsValid() || !HeldBody.IsValid() )
			return;

		if ( timeSinceImpulse < Time.Delta * 5 )
			return;

		var velocity = HeldBody.Velocity;
		Vector3.SmoothDamp( HeldBody.Position, HoldPos, ref velocity, 0.1f, Time.Delta );
		HeldBody.Velocity = velocity;

		var angularVelocity = HeldBody.AngularVelocity;
		Rotation.SmoothDamp( HeldBody.Rotation, HoldRot, ref angularVelocity, 0.1f, Time.Delta );
		HeldBody.AngularVelocity = angularVelocity;
	}

	/// <summary>
	/// Actually starts holding the object in front of us.
	/// </summary>
	private void GrabStart( GameObject gameObject, PhysicsBody body, Vector3 eyePos, Vector3 eyeDir, Vector3 attachPos )
	{
		if ( !body.IsValid() )
			return;

		GrabEnd(); // drop anything else we were holding

		GrabbedObject = gameObject;

		bool isRagdoll = gameObject.Components.Get<ModelPhysics>().IsValid();
		GrabbedBone = isRagdoll ? body.GroupIndex : -1;

		if ( !HeldBody.IsValid() )
			return;

		heldRot = GetEyeAngles().ToRotation().Inverse * HeldBody.Rotation;
		heldPos = HeldBody.LocalMassCenter;

		currentHoldDistance = HoldDistance + attachPos.Distance( body.MassCenter );

		HoldPos = body.Position;
		HoldRot = body.Rotation;

		// Tag so others can't pick it up
		gameObject.Tags.Add( "grabbed" );
	}

	/// <summary>
	/// Updates hold position based on eye position/direction each tick.
	/// </summary>
	private void GrabMove( Vector3 eyePos, Angles eyeAngles )
	{
		if ( !HeldBody.IsValid() )
			return;

		var attachPos = HeldBody.FindClosestPoint( eyePos );
		// We keep the current hold distance plus local offset
		HoldPos = eyePos - heldPos * HeldBody.Rotation + (eyeAngles.Forward * currentHoldDistance);
		HoldRot = eyeAngles.ToRotation() * heldRot;
	}

	/// <summary>
	/// Stop holding the object (drop it).
	/// </summary>
	[Rpc.Broadcast]
	private void GrabEnd()
	{
		timeSinceDrop = 0;
		heldRot = Rotation.Identity;

		if ( GrabbedObject.IsValid() )
		{
			GrabbedObject.Tags.Remove( "grabbed" );
		}

		GrabbedObject = null;
		lastGrabbed = null;
		_heldBody = null;
	}

	private SceneTraceResult DoPickupTrace( Vector3 start, Vector3 dir )
	{
		return Scene.Trace.Ray( start, start + dir * MaxPullDistance )
			.UseHitboxes()
			.WithAnyTags( "solid", "debris", "nocollide" )
			.IgnoreGameObjectHierarchy( GameObject )
			.Radius( 2.0f )
			.Run();
	}

	/// <summary>
	/// Freeze the current object => make its body static, show effects.
	/// Same logic as PhysGun does.
	/// </summary>
	[Rpc.Broadcast]
	public void Freeze( GameObject gameObject, int bone )
	{
		if ( !gameObject.IsValid() )
			return;

		var body = GetBody( gameObject, bone );
		if ( body.IsValid() )
		{
			body.BodyType = PhysicsBodyType.Static;
			FreezeEffects( body.FindClosestPoint( WorldPosition ) );
		}
	}

	/// <summary>
	/// Immediately unfreeze (turn dynamic) in the same tick.
	/// Not an RPC, so we don't wait for network round-trip.
	/// </summary>
	private void UnFreezeImmediate( GameObject gameObject, int bone )
	{
		if ( !gameObject.IsValid() )
			return;

		var body = GetBody( gameObject, bone );
		if ( body.IsValid() )
		{
			body.BodyType = PhysicsBodyType.Dynamic;
		}
	}

	private void FreezeEffects( Vector3 position )
	{
		//TODO
		//Particles.MakeParticleSystem( "particles/physgun_freeze.vpcf", new Transform( position ), 4 );
	}

	/// <summary>
	/// Try to pull the object towards us.
	/// </summary>
	private void TryPull( SceneTraceResult tr, Vector3 eyeDir )
	{
		var body = tr.Body;
		if ( !body.IsValid() )
			return;

		// If ragdoll => apply impulses to all bodies
		if ( body.PhysicsGroup.IsValid() )
		{
			foreach ( var b in body.PhysicsGroup.Bodies )
			{
				if ( !b.IsValid() ) continue;
				ApplyImpulse( tr.GameObject, -1, eyeDir * -PullForce * b.Mass );
			}
		}
		else
		{
			ApplyImpulse( tr.GameObject, -1, eyeDir * -PullForce * body.Mass );
		}
	}

	/// <summary>
	/// Return the bone index from a trace if it's a ragdoll body; otherwise -1.
	/// </summary>
	private int GetBoneIndex( SceneTraceResult tr )
	{
		var modelPhysics = tr.GameObject.Components.Get<ModelPhysics>();
		return modelPhysics.IsValid() ? tr.Body.GroupIndex : -1;
	}

	[Rpc.Broadcast]
	private void ApplyImpulseAt( GameObject gameObject, int boneIndex, Vector3 position, Vector3 velocity )
	{
		if ( !gameObject.IsValid() )
			return;

		timeSinceImpulse = 0;

		var body = GetBody( gameObject, boneIndex );
		if ( body.IsValid() )
		{
			body.ApplyImpulseAt( position, velocity );
		}
	}

	[Rpc.Broadcast]
	private void ApplyImpulse( GameObject gameObject, int bodyIndex, Vector3 velocity )
	{
		if ( !gameObject.IsValid() )
			return;

		timeSinceImpulse = 0;

		var body = GetBody( gameObject, bodyIndex );
		if ( body.IsValid() )
		{
			body.ApplyImpulse( velocity );
		}
	}

	/// <summary>
	/// Just like PhysGun's approach: if bone >= 0, we return that ragdoll body;
	/// otherwise we return the single rigidbody.
	/// </summary>
	private PhysicsBody GetBody( GameObject go, int boneIndex )
	{
		if ( !go.IsValid() )
			return null;

		if ( boneIndex >= 0 )
		{
			var modelPhysics = go.Components.Get<ModelPhysics>();
			if ( modelPhysics.IsValid() && modelPhysics.PhysicsGroup.IsValid() )
			{
				return modelPhysics.PhysicsGroup.GetBody( boneIndex );
			}
		}
		else
		{
			var rigidbody = go.Components.Get<Rigidbody>();
			if ( rigidbody.IsValid() )
			{
				return rigidbody.PhysicsBody;
			}
		}
		return null;
	}

	protected override void OnDisabled()
	{
		base.OnDisabled();

		GrabEnd();
	}
}