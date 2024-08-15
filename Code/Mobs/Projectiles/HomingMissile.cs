using HC2.Mobs;
using System.Text.Json.Serialization;
using Sandbox.Events;
using Voxel;

public partial class HomingMissile : Component, Component.ICollisionListener
{
	/// <summary>
	/// The rigidbody
	/// </summary>
	[RequireComponent] public Rigidbody Rigidbody { get; set; }

	/// <summary>
	/// The collider we're listening to events from
	/// </summary>
	[Property] public Collider Collider { get; set; }

	/// <summary>
	/// We'll disable this renderer when it hits something
	/// </summary>
	[Property] public ModelRenderer Renderer { get; set; }

	/// <summary>
	/// Should we spawn a prefab when we hit something with the projectile?
	/// </summary>
	[Property] public GameObject PrefabToSpawn { get; set; }

	[Property] public float LifeTime { get; set; } = 15f;

	/// <summary>
	/// Passed in by a weapon, how much damage do we inflict to a damageable?
	/// </summary>
	public float Damage { get; set; }

	/// <summary>
	/// Passed in by a weapon, what's the damage type?
	/// </summary>
	public DamageType DamageType { get; set; }

	public MobTarget Target { get; set; }

	[Property, Range( 0f, 2048f )]
	public float MaxRange { get; set; } = 512f;

	public TimeSince TimeSinceSpawn { get; set; }

	protected override void OnStart()
	{
		Tags.Add( "projectile" );
		DestroyAfter( LifeTime );
		Damage = 20f;

		TimeSinceSpawn = 0f;
		FindTarget();

		Transform.Rotation = Rotation.LookAt( Vector3.Up );
		Rigidbody.ApplyImpulse( Transform.Rotation.Forward * 50f );
	}

	void FindTarget()
	{
		var maxRangeSq = MaxRange * MaxRange;

		var targets = Scene.GetAllComponents<MobTarget>()
			.Select( x => (Target: x, Diff: x.Transform.Position - Transform.Position) )
			.Where( x => x.Diff.LengthSquared >= 16f && x.Diff.LengthSquared <= maxRangeSq )
			.OrderBy( x => x.Diff.LengthSquared )
			.ToList();

		Target = targets.FirstOrDefault().Target;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( !Target.IsValid() )
			return;

		//Gizmo.Draw.Color = Color.Red.WithAlpha( 0.5f );
		//Gizmo.Draw.Line( Transform.Position, Target.Transform.Position + Vector3.Up * MathX.Remap( TimeSinceSpawn, 0f, LifeTime, 60f, 0f ) );

		//Gizmo.Draw.Color = Color.White.WithAlpha( 0.5f );
		//Gizmo.Draw.Line( Transform.Position, Transform.Position + Transform.Rotation.Forward * 50f );
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;

		Rigidbody.LinearDamping = MathX.Remap( TimeSinceSpawn, 0f, 5f, 1.5f, 1.8f );

		//Rigidbody.Velocity = new Vector3( 50f, 0f, 0f );
		if ( Target.IsValid() )
		{
			var targetPos = TimeSinceSpawn < 1f ? (Transform.Position + Vector3.Up * 100f) : (Target.Transform.Position + Vector3.Up * MathX.Remap( TimeSinceSpawn, 0f, 8f, 60f, 0f ));

			var smoothTime = MathX.Remap( TimeSinceSpawn, 0f, 10f, 1f, 2f );
			Rigidbody.PhysicsBody.SmoothRotate( Rotation.LookAt( targetPos - Transform.Position ), smoothTime, Time.Delta );
		}

		float speed = MathX.Remap( TimeSinceSpawn, 0f, 5f, 800f, 500f );

		Rigidbody.ApplyForce( Transform.Rotation.Forward * speed );
	}

	[Broadcast]
	public void SpawnEffects( Vector3 position )
	{
		if ( PrefabToSpawn.IsValid() )
		{
			PrefabToSpawn.Clone( position );
		}
	}

	public void OnCollisionStart( Collision other )
	{
		Log.Info( $"{GameObject.Name} Collided with {other.Other.GameObject}" );

		if ( IsProxy )
		{
			// Disable it locally, wait for an authoritative figure to tell you what to do.
			Renderer.Enabled = false;
			Rigidbody.Enabled = false;
			return;
		}

		var contactPoint = other.Contact.Point;
		if ( contactPoint == Vector3.Zero ) contactPoint = Transform.Position;

		if ( other.Other.Shape.Tags.Has( "voxel" ) )
		{
			Scene.Dispatch( new DamageWorldEvent( contactPoint + Rigidbody.Velocity.Normal * 8f, Rigidbody.Velocity.Normal, GameObject, Damage ) );
		}

		var healthComponent = other.Other.GameObject?.Root.Components.Get<HealthComponent>();
		if ( healthComponent.IsValid() )
		{
			healthComponent.TakeDamage( new DamageInstance()
			{
				Attacker = null,
				Inflictor = this,
				Damage = Damage,
				Type = DamageType,
				Force = Rigidbody.Velocity,
				Position = contactPoint,
				Victim = healthComponent
			} );
		}

		InternalDestroy( contactPoint );
	}

	void InternalDestroy( Vector3 position = default )
	{
		if ( IsProxy ) return;

		if ( position == default )
			position = Transform.Position;

		SpawnEffects( position );
		GameObject.Destroy();
	}

	async void DestroyAfter( float time )
	{
		await GameTask.DelaySeconds( time );
		if ( !GameObject.IsValid() ) return;
		InternalDestroy();
	}
}
