
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

    bool drawingRect = false;
    Vector3 startingRectPosition = Vector3.Zero;

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
            if ( BrushSize > 1 )
            {
                var brushSize = BrushSize - 1;
                startPos -= Vector3.One * blockSize * brushSize;
                size += Vector3.One * blockSize * brushSize * 2;
            }

            var boxStart = startPos;
            var boxEnd = startPos + size;
            if ( drawingRect )
            {
                boxStart = startingRectPosition;
                boxEnd = startPos + blockSize / 2f;
            }
            var mins = Vector3.Min( boxStart, boxEnd );
            var maxs = Vector3.Max( boxStart, boxEnd );
            if ( drawingRect )
            {
                mins -= blockSize * (BrushSize - 1);
                maxs += blockSize * (BrushSize - 1);
            }

            var xblocks = (int)Math.Round( Math.Abs( maxs.x - mins.x ) / blockSize );
            var yblocks = (int)Math.Round( Math.Abs( maxs.y - mins.y ) / blockSize );
            var zblocks = (int)Math.Round( Math.Abs( maxs.z - mins.z ) / blockSize );
            if ( drawingRect )
            {
                xblocks++;
                yblocks++;
                zblocks++;
            }
            var totalBlocks = xblocks * yblocks * zblocks;
            var canPlace = Player.Local.Inventory.HasItem( Item.Create( BlockType.ItemResource, totalBlocks ) );

            using ( Gizmo.Scope( "block_ghost" ) )
            {
                var bbox = new BBox( mins, maxs );
                if ( drawingRect ) bbox = bbox.Grow( blockSize / 2f );
                Gizmo.Draw.Color = Color.Cyan;
                if ( !canPlace )
                    Gizmo.Draw.Color = Color.Red;
                Gizmo.Draw.LineThickness = 2f;
                Gizmo.Draw.LineBBox( bbox );
                Gizmo.Draw.Color = Gizmo.Draw.Color.WithAlpha( 0.5f );
                Gizmo.Draw.SolidBox( bbox );
            }


            if ( canPlace )
            {
                if ( drawingRect && !Input.Down( "attack2" ) )
                {
                    drawingRect = false;
                    PlaceBlock( mins, maxs - (blockSize / 2f) );
                }
                else if ( !drawingRect && Input.Pressed( "attack1" ) )
                {
                    PlaceBlock( mins, maxs - (blockSize / 2f) );
                }
                else if ( Input.Pressed( "attack2" ) )
                {
                    drawingRect = true;
                    startingRectPosition = pos + blockSize / 2f;
                }
            }
        }

        if ( Input.Down( "Walk" ) )
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

    void PlaceBlock( Vector3 mins, Vector3 maxs )
    {
        if ( timeSinceLastPlace < 0.05f )
            return;

        lastCamTransform = Scene.Camera.Transform.World.WithPosition( Scene.Camera.Transform.Position.SnapToGrid( 1 ) );

        if ( Player.Hotbar.GetItemInSlot( Player.Hotbar.SelectedSlot ) is Item item )
        {
            if ( item.Amount <= 0 ) return;

            using ( Rpc.FilterInclude( Connection.Host ) )
            {
                PlaceBlockHost( mins, maxs, BlockType, Player.Hotbar.SelectedSlot );
            }
        }

        timeSinceLastPlace = 0;
    }

    [Broadcast]
    void PlaceBlockHost( Vector3 mins, Vector3 maxs, Block block, int slot )
    {
        if ( !Sandbox.Networking.IsHost )
            return;

        var world = Scene.GetAllComponents<VoxelNetworking>().FirstOrDefault();
        if ( world is null )
            return;

        var voxelMins = world.Renderer.WorldToVoxelCoords( mins );
        var voxelMaxs = world.Renderer.WorldToVoxelCoords( maxs );

        world.Modify( new BuildModification( block, voxelMins, voxelMaxs ) );

        int xblocks = (int)Math.Abs( voxelMaxs.x - voxelMins.x ) + 1;
        int yblocks = (int)Math.Abs( voxelMaxs.y - voxelMins.y ) + 1;
        int zblocks = (int)Math.Abs( voxelMaxs.z - voxelMins.z ) + 1;
        var totalBlocks = xblocks * yblocks * zblocks;

        var caller = Rpc.Caller;
        using ( Rpc.FilterInclude( caller ) )
        {
            UseBlock( slot, totalBlocks );
        }
        PlaceBlockEffects( mins, maxs );
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

    [Broadcast]
    void PlaceBlockEffects( Vector3 mins, Vector3 maxs )
    {
        var position = (mins + maxs) / 2f;
        var diff = maxs - mins;
        var size = Math.Max( diff.x, Math.Max( diff.y, diff.z ) );
        var sound = Sound.Play( "block.place", position );
        sound.Pitch = Random.Shared.Float( 0.9f, 1f ) - ((size - 1f) / 16f) * 0.1f;
    }
}