using System.Text.Json.Serialization;

public partial class Projectile : Component, Component.ICollisionListener
{
	/// <summary>
	/// The projectile's weapon
	/// </summary>
	[Property, Sync, JsonIgnore] public WeaponComponent Weapon { get; set; }

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

	/// <summary>
	/// Passed in by a weapon, how much damage do we inflict to a damageable?
	/// </summary>
	public float Damage { get; set; }

	/// <summary>
	/// Passed in by a weapon, what's the damage type?
	/// </summary>
	public DamageType DamageType { get; set; }

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
		// Log.Info( $"Collided with {other.Other.GameObject}" );

		if ( IsProxy )
		{
			// Disable it locally, wait for an authoritative figure to tell you what to do.
			Renderer.Enabled = false;
			Rigidbody.Enabled = false;
			return;
		}

		SpawnEffects( other.Contact.Point );

		var healthComponent = other.Other.GameObject.Root.Components.Get<HealthComponent>();
		if ( healthComponent.IsValid() )
		{
			healthComponent.TakeDamage( new DamageInstance()
			{
				Attacker = Weapon.Player,
				Inflictor = this,
				Damage = Damage,
				Type = DamageType,
				Force = Rigidbody.Velocity,
				Position = other.Contact.Point,
				Victim = healthComponent
			} );
		}

		GameObject.Destroy();
	}
}
