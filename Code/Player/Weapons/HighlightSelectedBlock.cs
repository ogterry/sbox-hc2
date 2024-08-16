using HC2;
using Voxel;

public sealed class HighlightSelectedBlock : Component
{
    protected override void OnUpdate()
    {
        var tr = Scene.Trace.Ray( Scene.Camera.Transform.Position, Scene.Camera.Transform.Position + Scene.Camera.Transform.Rotation.Forward * 500 )
            .WithoutTags( "player", "trigger", "worlditem", "bodypart" )
            .Run();

        if ( tr.Hit )
        {
            var blockSize = 16f;
            var pos = tr.HitPosition - tr.Normal * (blockSize / 2f);
            pos -= blockSize / 2f;
            pos = pos.SnapToGrid( blockSize );

            var world = Scene.GetAllComponents<VoxelNetworking>().FirstOrDefault();
            if ( world == null ) return;
            if ( world?.Renderer?.Model is not VoxelModel ) return;
            if ( !world.Renderer.IsReady ) return;

            var voxelPos = world.Renderer.WorldToVoxelCoords( pos );
            var voxel = world.Renderer.Model.GetVoxel( voxelPos.x, voxelPos.y, voxelPos.z );
            if ( voxel == 0 ) return;

            using ( Gizmo.Scope( "block_ghost" ) )
            {
                var bbox = new BBox( pos, pos + blockSize ).Grow( 1 );
                Gizmo.Draw.Color = Color.Cyan;
                Gizmo.Draw.LineThickness = 2f;
                Gizmo.Draw.LineBBox( bbox );
            }
        }
    }
}