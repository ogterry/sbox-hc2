
using HC2;
using Voxel;
using Voxel.Modifications;

public sealed class BlockPlacer : Carriable
{
    /// <summary>
    /// The block type that this places
    /// </summary>
    [Property] public Block BlockType { get; set; }

    /// <summary>
	/// Gets the player
	/// </summary>
	public Player Player => GameObject.Root.Components.Get<Player>();


    RealTimeSince timeSinceLastPlace = 0f;

    protected override void OnUpdate()
    {
        base.OnUpdate();

        var tr = Scene.Trace.Ray( Scene.Camera.Transform.Position, Scene.Camera.Transform.Position + Scene.Camera.Transform.Rotation.Forward * 500 )
            .WithoutTags( "player", "trigger" )
            .Run();

        if ( tr.Hit )
        {
            var blockSize = 16f;
            var pos = tr.HitPosition + tr.Normal * (blockSize / 2f);
            pos -= blockSize / 2f;
            pos = pos.SnapToGrid( blockSize );
            using ( Gizmo.Scope( "block_ghost" ) )
            {
                var bbox = new BBox( pos, pos + Vector3.One * blockSize );
                Gizmo.Draw.Color = Color.Cyan;
                Gizmo.Draw.LineThickness = 2f;
                Gizmo.Draw.LineBBox( bbox );
                Gizmo.Draw.Color = Color.Cyan.WithAlpha( 0.5f );
                Gizmo.Draw.SolidBox( bbox );
            }


            if ( Input.Down( "attack1" ) )
            {
                PlaceBlock( pos + blockSize / 2f );
            }
        }
    }

    void PlaceBlock( Vector3 pos )
    {
        if ( timeSinceLastPlace < 0.05f )
            return;

        if ( Player.Hotbar.GetItemInSlot( Player.Hotbar.SelectedSlot ) is Item item )
        {
            if ( item.Amount <= 0 ) return;

            using ( Rpc.FilterInclude( Connection.Host ) )
            {
                PlaceBlockHost( pos, BlockType, Player.Hotbar.SelectedSlot );
            }
        }

        timeSinceLastPlace = 0;
    }

    [Broadcast]
    void PlaceBlockHost( Vector3 pos, Block block, int slot )
    {
        if ( !Sandbox.Networking.IsHost )
            return;

        var world = Scene.GetAllComponents<VoxelNetworking>().First();
        if ( world is null )
            return;

        var voxelPos = world.Renderer.WorldToVoxelCoords( pos );
        var voxel = world.Renderer.Model.GetVoxel( voxelPos.x, voxelPos.y, voxelPos.z );
        if ( voxel != 0 ) return;

        world.Modify( new BuildModification( block, voxelPos, voxelPos ) );

        var caller = Rpc.Caller;
        using ( Rpc.FilterInclude( caller ) )
        {
            UseBlock( slot );
        }
    }

    [Broadcast( NetPermission.HostOnly )]
    void UseBlock( int slot )
    {
        if ( Player.Hotbar.GetItemInSlot( slot ) is Item item )
        {
            if ( item.Amount <= 0 ) return;
            item.Amount--;
            if ( item.Amount <= 0 )
            {
                Player.Hotbar.TakeItemSlot( slot );
            }
        }
    }
}