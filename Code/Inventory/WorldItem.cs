using System;

namespace HC2;

public partial class WorldItem : Component, Component.ITriggerListener
{
	[Property]
	public ItemAsset Resource { get; set; }

	[Sync]
	public int Amount { get; set; } = 1;

	[RequireComponent]
	public Rigidbody Rigidbody { get; set; }

	[RequireComponent]
	public SphereCollider SphereCollider { get; set; }

	[RequireComponent]
	public BoxCollider Trigger { get; set; }

	[Property]
	public SkinnedModelRenderer ModelRenderer { get; set; }

	[Property]
	public GameObject SpinningItem { get; set; }

	protected override void OnStart()
	{
		Tags.Add( "pickup" );
		Trigger.Scale = new( 16, 16, 16 );
		Trigger.IsTrigger = true;
	}

	protected override void OnUpdate()
	{
		Transform.Rotation = Rotation.Identity;
		Spin();
	}

	void Spin()
	{
		if ( !SpinningItem.IsValid() )
			return;

		var newYaw = SpinningItem.Transform.Rotation.Yaw() + 50f * Time.Delta;
		SpinningItem.Transform.Rotation = Rotation.From( 0, newYaw, 0 );
		SpinningItem.Transform.LocalPosition = Vector3.Up * MathF.Sin( Time.Now * 3f ) * 2;
	}

	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		if ( !Sandbox.Networking.IsHost )
			return;

		if ( other.GameObject.Root.Components.Get<Player>() is { IsValid: true } player )
		{
			using ( Rpc.FilterInclude( player.Network.OwnerConnection ) )
			{
				TryPickup( player );
			}
		}
	}

	[Broadcast]
	void TryPickup( Player player )
	{
		if ( player.Hotbar.TryGiveItem( HC2.Item.Create( Resource, Amount ) ) )
		{
			Pickup();
		}
	}

	[Authority]
	void Pickup()
	{
		GameObject.Destroy();
	}

	/// <summary>
	/// Quick and nasty accessor for creating a world item. This could be loads better
	/// </summary>
	/// <param name="itemAsset"></param>
	/// <param name="worldPosition"></param>
	/// <param name="amount"></param>
	/// <returns></returns>
	public static WorldItem CreateInstance( ItemAsset itemAsset, Vector3 worldPosition, int amount = 1 )
	{
		// Push the active scene
		using var _ = Game.ActiveScene.Push();

		var go = new GameObject();
		go.Name = $"World Item {itemAsset}";
		go.Transform.Position = worldPosition;

		var sphereCollider = go.Components.Create<SphereCollider>();
		sphereCollider.Radius = 16;

		var worldItem = go.Components.Create<WorldItem>();
		worldItem.Resource = itemAsset;
		worldItem.Amount = amount;
		worldItem.Rigidbody.LinearDamping = 1f;

		var spinningItem = new GameObject();
		spinningItem.Parent = go;

		var mdl = spinningItem.Components.Create<SkinnedModelRenderer>();
		mdl.Model = itemAsset.WorldModel;
		worldItem.ModelRenderer = mdl;
		worldItem.SpinningItem = spinningItem;

		go.NetworkSpawn();

		return worldItem;
	}
}
