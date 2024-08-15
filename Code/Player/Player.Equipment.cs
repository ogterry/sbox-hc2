using HC2;
using Sandbox.Events;

public partial class Player
{
	/// <summary>
	/// What should be our carriable if we aren't holding anything? Think hands..
	/// </summary>
	[Property, Group( "Equipment" ), Sync]
	public Carriable FallbackCarriable { get; set; }

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

		FallbackCarriable.GameObject.Enabled = false;
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

		HoldType = Sandbox.Citizen.CitizenAnimationHelper.HoldTypes.None;
		MainHand = null;

		FallbackCarriable.GameObject.Enabled = true;
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
	public GameObject SetMainHand( GameObject go )
	{
		// Only the owner should be doing this
		if ( IsProxy )
			return null;

		if ( !go.IsValid() )
			return null;

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
			return inst;
		}

		HoldType = carriable.HoldType;
		inst.NetworkSpawn( Network.OwnerConnection );

		SetMainHand( carriable );

		return inst;
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
