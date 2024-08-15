
using HC2;
using Voxel;

public sealed class BlockPlacer : Carriable
{
    [Property] public Block BlockType { get; set; }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        var tr = Scene.Trace.Ray( Scene.Camera.Transform.Position, Scene.Camera.Transform.Position + Scene.Camera.Transform.Rotation.Forward * 500 )
            .WithoutTags( "player", "trigger" )
            .Run();

        if ( tr.Hit )
        {
            var blockSize = 16f;
            var pos = tr.HitPosition + tr.Normal * (blockSize / 4f);
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
        using ( Rpc.FilterInclude( Connection.Host ) )
        {
            PlaceBlockHost( pos );
        }
    }

    [Broadcast]
    void PlaceBlockHost( Vector3 pos )
    {
        if ( !Sandbox.Networking.IsHost )
            return;

        var world = Scene.GetAllComponents<VoxelNetworking>().First();
        if ( world is null )
            return;

        var voxelPos = world.Renderer.WorldToVoxelCoords( pos );
        // world.Modify(new BuildModifi)
    }
}