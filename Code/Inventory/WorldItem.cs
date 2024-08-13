namespace HC2;

public partial class WorldItem : Component, Component.ITriggerListener
{
	[Property]
	public ItemAsset Resource { get; set; }

	[RequireComponent]
	public Rigidbody Rigidbody { get; set; }

	[RequireComponent]
	public SphereCollider SphereCollider { get; set; }

	[RequireComponent]
	public BoxCollider Trigger { get; set; }

	[RequireComponent]
	public SkinnedModelRenderer ModelRenderer { get; set; }

	protected override void OnStart()
	{
		Tags.Add( "pickup" );

		Trigger.Scale = new( 16, 16, 16 );
		Trigger.IsTrigger = true;
	}

	protected override void OnUpdate()
	{
		Transform.Rotation = Rotation.Identity;
	}

	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		if ( !Sandbox.Networking.IsHost )
			return;

		if ( other.GameObject.Root.Components.Get<Player>() is { IsValid: true } player )
		{
			if ( player.Inventory.GetFreeSlotIndex() >= 0 )
			{
				player.Inventory.GiveItem( HC2.Item.Create( Resource ) );
				GameObject.Destroy();
			}
		}
	}

	/// <summary>
	/// Quick and nasty accessor for creating a world item. This could be loads better
	/// </summary>
	/// <param name="itemAsset"></param>
	/// <param name="worldPosition"></param>
	/// <returns></returns>
	public static WorldItem CreateInstance( ItemAsset itemAsset, Vector3 worldPosition )
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
		worldItem.ModelRenderer.Model = itemAsset.WorldModel;
		worldItem.Rigidbody.LinearDamping = 1f;

		return worldItem;
	}
}
