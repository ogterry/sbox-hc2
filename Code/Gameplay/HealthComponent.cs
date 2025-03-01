﻿using HC2;
using Sandbox.Events;
using System.Text.Json.Serialization;

public record DamageTakenEvent( DamageInstance Instance ) : IGameEvent;
public record KilledEvent( DamageInstance LastDamage ) : IGameEvent;

/// <summary>
/// Event dispatched on the host when something takes damage, so it can be modified.
/// </summary>
public class ModifyDamageEvent : IGameEvent
{
	public DamageInstance DamageInstance { get; set; }

	/// <summary>
	/// Clears all health and armor damage from this event.
	/// </summary>
	public void ClearDamage()
	{
		DamageInstance = DamageInstance with { Damage = 0f };
	}

	/// <summary>
	/// Scales health damage by the given multiplier.
	/// </summary>
	public void ScaleDamage( float scale )
	{
		DamageInstance = DamageInstance with { Damage = DamageInstance.Damage * scale };
	}

	/// <summary>
	/// Adds a flag to this damage event.
	/// </summary>
	/// <param name="flag"></param>
	public void AddFlag( DamageFlags flag )
	{
		DamageInstance = DamageInstance with { Flags = DamageInstance.Flags | flag };
	}

	/// <summary>
	/// Removes a flag from this damage event.
	/// </summary>
	/// <param name="flag"></param>
	public void WithoutFlag( DamageFlags flag )
	{
		DamageInstance = DamageInstance with { Flags = DamageInstance.Flags & flag };
	}

	public void SetType( DamageType type )
	{
		DamageInstance = DamageInstance with { Type = type };
	}
}

public sealed class HealthComponent : Component
{
	[HostSync, Property, JsonIgnore, ReadOnly] public float Health { get; set; } = 100f;
	[Property] public float MaxHealth { get; set; } = 100f;
	[Property] public float RegenRate { get; set; } = 0f;
	[Property] public GameObject DamageEffectPrefab { get; set; }

	public DamageInstance LastDamage { get; private set; }
	TimeSince timeSinceLastDamage = 0;

	protected override void OnStart()
	{
		Health = MaxHealth;
	}

	protected override void OnFixedUpdate()
	{
		if ( timeSinceLastDamage > 4f && RegenRate > 0 && Health < MaxHealth )
		{
			Health += RegenRate * Time.Delta;
		}
	}

	[Broadcast]
	private void BroadcastDamage( float damage, Vector3 position, Vector3 force, Component attacker, Component inflictor = null, Component victim = null, DamageFlags flags = default, DamageType type = default )
	{
		var instance = new DamageInstance()
		{
			Damage = damage,
			Force = force,
			Victim = victim,
			Inflictor = inflictor,
			Flags = flags,
			Type = type,
			Position = position,
			Attacker = attacker,
		};

		GameObject.Dispatch( new DamageTakenEvent( instance ) );

		foreach ( var damageable in Components.GetAll<IDamage>() )
		{
			damageable.OnDamage( instance );
		}

		CreateDamageEffect( instance );
	}

	private void CreateDamageEffect( DamageInstance instance )
	{
		string icon = instance.Type.GetIcon();
		Color color = instance.Type.GetColor();

		// No effects if the damage was nullified or didn't exist in the first place
		if ( instance.Damage <= 0 )
		{
			icon = "🛡️";
			color = Color.White.Darken( 0.25f );
		}

		if ( !DamageEffectPrefab.IsValid() )
			return;

		var particle = DamageEffectPrefab.Clone( position: instance.Position, rotation: new Angles( -90f, 0f, 0f ) );

		var textRenderer = particle.Components.Get<ParticleTextRenderer>();
		if ( textRenderer != null )
		{
			var text = new TextRendering.Scope( icon + instance.Damage.ToString( "0.#" ), color, 32, weight: 800 );
			text.Outline.Enabled = true;
			text.Outline.Color = Color.Black;
			text.Outline.Size = 8;

			text.FontItalic = instance.Type == DamageType.Sharp && instance.Damage > 0;

			textRenderer.Text = text;
		}

		if ( instance.Type == DamageType.Heal )
		{
			var particleEffect = particle.Components.Get<ParticleEffect>();
			if ( particleEffect != null )
			{
				particleEffect.StartVelocity = Game.Random.Float( 20f, 40f );
				particleEffect.ForceDirection = new Vector3( 0f, 0f, 50f );
				particleEffect.Damping = 0.35f;
				particleEffect.Stretch = 0f;
			}
		}
	}

	private DamageInstance ModifyDamage( DamageInstance instance )
	{
		var modifyEvent = new ModifyDamageEvent()
		{
			DamageInstance = instance
		};

		if ( GameObject.Root.Components.TryGet<StatModifier>( out var modifier ) )
		{
			modifyEvent.ScaleDamage( modifier.DamageMultiplier );
		}

		GameObject.Dispatch( modifyEvent );

		return modifyEvent.DamageInstance;
	}

	[Broadcast]
	private void AskHostToDamage( float damage, Vector3 position, Vector3 force, Component attacker, Component inflictor = null, Component victim = null, DamageFlags flags = default, DamageType type = default )
	{
		if ( !Sandbox.Networking.IsHost )
		{
			return;
		}

		var instance = new DamageInstance()
		{
			Damage = damage,
			Force = force,
			Victim = victim,
			Inflictor = inflictor,
			Flags = flags,
			Type = type,
			Position = position,
			Attacker = attacker,
		};

		// Have the host do all the damaging
		TakeDamage( instance );
	}

	public void Kill()
	{
		Health = 0;
		GameObject.Dispatch( new KilledEvent( LastDamage ) );
	}

	public void TakeDamage( DamageInstance damage )
	{
		if ( !Sandbox.Networking.IsHost )
		{
			using ( Rpc.FilterInclude( Connection.Host ) )
			{
				AskHostToDamage( damage.Damage, damage.Position, damage.Force, damage.Attacker, damage.Inflictor, damage.Victim, damage.Flags, damage.Type );
			}
			return;
		}

		damage = ModifyDamage( damage );

		Health -= damage.Damage;
		LastDamage = damage;
		timeSinceLastDamage = 0;

		if ( Health <= 0 )
		{
			Kill();
		}

		BroadcastDamage( damage.Damage, damage.Position, damage.Force, damage.Attacker, damage.Inflictor, damage.Victim, damage.Flags, damage.Type );
	}
}
