
using System;
using HC2;
using Voxel;
using Voxel.Modifications;

public sealed class BlockPlacer : Carriable
{
    public static int BrushSize = 1;

    /// <summary>
    /// The block type that this places
    /// </summary>
    [Property] public Block BlockType { get; set; }

    /// <summary>
	/// Gets the player
	/// </summary>
	public Player Player => GameObject.Root.Components.Get<Player>();


    RealTimeSince timeSinceLastPlace = 0f;
    Transform lastCamTransform;

    protected override void OnUpdate()
    {
        base.OnUpdate();

        var tr = Scene.Trace.Ray( Scene.Camera.Transform.Position, Scene.Camera.Transform.Position + Scene.Camera.Transform.Rotation.Forward * 325 )
            .WithoutTags( "player", "trigger", "mob", "worlditem" )
            .Run();

        Vector2 checkingDir = Vector2.Zero;
        if ( !tr.Hit )
        {
            for ( var i = 0; i < 4; i++ )
            {
                var angle = i * 90;
                var angles = new Angles( 3 * (angle == 0 ? 1 : -1), 0, 0 );
                if ( i % 2 == 1 )
                    angles = new Angles( 0, 3 * (angle == 90 ? 1 : -1), 0 );
                var newRot = Scene.Camera.Transform.Rotation * angles;
                tr = Scene.Trace.Ray( Scene.Camera.Transform.Position, Scene.Camera.Transform.Position + newRot.Forward * 250 )
                    .WithoutTags( "player", "trigger", "mob", "worlditem" )
                    .Size( 2f )
                    .Run();

                if ( tr.Hit )
                {
                    checkingDir = new Vector2( MathF.Sign( angles.yaw ), MathF.Sign( angles.pitch ) );
                    break;
                }
            }
        }

        if ( tr.Hit )
        {
            var blockSize = 16f;
            var pos = tr.HitPosition + tr.Normal * (blockSize / 2f);
            var normal = tr.Normal;
            if ( checkingDir != 0 )
            {
                normal = new Vector3( checkingDir.x, 0, checkingDir.y );
                pos = tr.HitPosition - tr.Normal * (blockSize / 2f) + normal * (blockSize / 2f);
            }
            pos -= blockSize / 2f;
            pos = pos.SnapToGrid( blockSize );

            if ( BrushSize > 1 )
            {
                pos += normal * blockSize * (BrushSize - 1);
            }

            var startPos = pos;
            var size = Vector3.One * blockSize;
            var totalBlocks = BrushSize;
            if ( BrushSize > 1 )
            {
                var brushSize = BrushSize - 1;
                startPos -= Vector3.One * blockSize * brushSize;
                size += Vector3.One * blockSize * brushSize * 2;
                totalBlocks = (int)MathF.Pow( 1 + (BrushSize * 2), 3 );
            }

            var canPlace = Player.Local.Inventory.HasItem( Item.Create( BlockType.ItemResource, totalBlocks ) );

            using ( Gizmo.Scope( "block_ghost" ) )
            {
                var bbox = new BBox( startPos, startPos + size );
                Gizmo.Draw.Color = Color.Cyan;
                if ( !canPlace )
                    Gizmo.Draw.Color = Color.Red;
                Gizmo.Draw.LineThickness = 2f;
                Gizmo.Draw.LineBBox( bbox );
                Gizmo.Draw.Color = Gizmo.Draw.Color.WithAlpha( 0.5f );
                Gizmo.Draw.SolidBox( bbox );
            }


            if ( canPlace && Input.Pressed( "attack1" ) )
            {
                PlaceBlock( pos + blockSize / 2f );
            }
        }

        if ( Input.Down( "Run" ) )
        {
            if ( Input.MouseWheel.y < 0 )
            {
                BrushSize = Math.Clamp( BrushSize - 1, 1, 5 );
            }
            else if ( Input.MouseWheel.y > 0 )
            {
                BrushSize = Math.Clamp( BrushSize + 1, 1, 5 );
            }
        }
    }

    void PlaceBlock( Vector3 pos )
    {
        if ( timeSinceLastPlace < 0.05f )
            return;

        lastCamTransform = Scene.Camera.Transform.World.WithPosition( Scene.Camera.Transform.Position.SnapToGrid( 1 ) );

        if ( Player.Hotbar.GetItemInSlot( Player.Hotbar.SelectedSlot ) is Item item )
        {
            if ( item.Amount <= 0 ) return;

            using ( Rpc.FilterInclude( Connection.Host ) )
            {
                PlaceBlockHost( pos, BlockType, Player.Hotbar.SelectedSlot, BrushSize );
            }
        }

        timeSinceLastPlace = 0;
    }

    [Broadcast]
    void PlaceBlockHost( Vector3 pos, Block block, int slot, int brushSize )
    {
        if ( !Sandbox.Networking.IsHost )
            return;

        var world = Scene.GetAllComponents<VoxelNetworking>().FirstOrDefault();
        if ( world is null )
            return;

        var voxelPos = world.Renderer.WorldToVoxelCoords( pos );
        var voxel = world.Renderer.Model.GetVoxel( voxelPos.x, voxelPos.y, voxelPos.z );
        if ( voxel != 0 ) return;

        world.Modify( new BuildModification( block, voxelPos - (brushSize - 1), voxelPos + (brushSize - 1) ) );

        var totalBlocks = brushSize;
        if ( brushSize > 1 )
        {
            totalBlocks = (int)MathF.Pow( 1 + (brushSize * 2), 3 );
        }

        var caller = Rpc.Caller;
        using ( Rpc.FilterInclude( caller ) )
        {
            UseBlock( slot, totalBlocks );
        }
    }

    [Broadcast( NetPermission.HostOnly )]
    void UseBlock( int slot, int amount )
    {
        if ( Player.Hotbar.GetItemInSlot( slot ) is Item item )
        {
            if ( item.Amount <= 0 ) return;

            if ( item.Amount < amount )
            {
                Player.Hotbar.TakeItem( Item.Create( item.Resource, amount ) );
                return;
            }

            item.Amount -= amount;

            if ( item.Amount <= 0 )
            {
                Player.Hotbar.TakeItemSlot( slot );
            }
        }
    }
}