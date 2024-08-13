using HC2;
using Sandbox.Events;

public partial class Player
{
	/// <summary>
	/// The player's main weapon, the one we're using right now
	/// </summary>
	[Property, Group( "Equipment" ), Sync]
	public Carriable MainHand { get; private set; }

	/// <summary>
	/// Shorthand accessor to get main hand as a weapon
	/// </summary>
	public WeaponComponent MainHandWeapon => MainHand.IsValid() ? MainHand as WeaponComponent : null;

	/// <summary>
	/// We might implement this soon
	/// </summary>
	[Property, Group( "Equipment" ), Sync]
	public Carriable OffHand { get; private set; }

	/// <summary>
	/// Shorthand accessor to get off hand as a weapon
	/// </summary>
	public WeaponComponent OffHandWeapon => OffHand.IsValid() ? OffHand as WeaponComponent : null;

	/// <summary>
	/// The bone we'll put the main hand carriable on
	/// </summary>
	[Property, Group( "Equipment" )]
	public GameObject MainHandBone { get; set; }

	/// <summary>
	/// The bone we'll put the main hand carriable on
	/// </summary>
	[Property, Group( "Equipment" )]
	public GameObject OffHandBone { get; set; }

	/// <summary>
	/// Something to spawn automatically..
	/// </summary>
	[Property, Group( "Equipment" )]
	public GameObject DefaultMainHandPrefab { get; set; }

	/// <summary>
	/// Something to spawn automatically..
	/// </summary>
	[Property, Group( "Equipment" )]
	public GameObject DefaultOffHandPrefab { get; set; }

	private void InitEquipment()
	{
		if ( DefaultMainHandPrefab.IsValid() )
		{
			SetMainHand( DefaultMainHandPrefab );
		}

		if ( DefaultOffHandPrefab.IsValid() )
		{
			SetOffHand( DefaultOffHandPrefab );
		}
	}

	/// <summary>
	/// Sets the main hand carriable to something, handles destruction of the old carriable.
	/// </summary>
	/// <param name="carriable"></param>
	public void SetMainHand( Carriable carriable )
	{
		// Only the owner should be doing this
		if ( IsProxy )
			return;

		if ( MainHand.IsValid() )
		{
			MainHand.GameObject.Destroy();
		}

		// TODO: Events
		MainHand = null;
		MainHand = carriable;
	}

	/// <summary>
	/// Sets the main hand carriable to something, handles destruction of the old carriable.
	/// </summary>
	/// <param name="carriable"></param>
	public void SetOffHand( Carriable carriable )
	{
		// Only the owner should be doing this
		if ( IsProxy )
			return;

		if ( OffHand.IsValid() )
		{
			OffHand.GameObject.Destroy();
		}

		// TODO: Events
		OffHand = null;
		OffHand = carriable;
	}

	/// <summary>
	/// Removes the main hand carriable, destroying it.
	/// </summary>
	public void RemoveMainHand()
	{
		if ( MainHand.IsValid() )
		{
			MainHand.GameObject.Destroy();
		}

		MainHand = null;
	}

	/// <summary>
	/// Removes the offhand carriable, destroying it.
	/// </summary>
	public void RemoveOffHand()
	{
		if ( OffHand.IsValid() )
		{
			OffHand.GameObject.Destroy();
		}

		OffHand = null;
	}

	/// <summary>
	/// Sets the current main hand from a GameObject.
	/// </summary>
	/// <param name="go"></param>
	public void SetMainHand( GameObject go )
	{
		// Only the owner should be doing this
		if ( IsProxy )
			return;

		if ( !go.IsValid() )
			return;

		var inst = go.Clone( new CloneConfig()
		{
			Parent = MainHandBone,
			StartEnabled = true,
			Transform = new(),
		} );

		var carriable = inst.Components.Get<Carriable>();
		if ( !carriable.IsValid() )
		{
			inst.Destroy();
			return;
		}

		inst.NetworkSpawn( Network.OwnerConnection );

		SetMainHand( carriable );
	}

	/// <summary>
	/// Sets the current offhand from a GameObject.
	/// </summary>
	/// <param name="go"></param>
	public void SetOffHand( GameObject go )
	{
		// Only the owner should be doing this
		if ( IsProxy )
			return;

		if ( !go.IsValid() )
			return;

		var inst = go.Clone( new CloneConfig()
		{
			Parent = OffHandBone,
			StartEnabled = true,
			Transform = new(),
		} );

		var carriable = inst.Components.Get<Carriable>();
		if ( !carriable.IsValid() )
		{
			inst.Destroy();
			return;
		}

		inst.NetworkSpawn( Network.OwnerConnection );

		SetOffHand( carriable );
	}
}
