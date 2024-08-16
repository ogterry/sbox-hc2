
using System.Reflection.Metadata;

namespace Voxel.Modifications;

/// <summary>
/// When used in a prefab spawned by <see cref="VoxelWorldGen.SpawnPrefab"/>,
/// optionally moves the attached object up / down to match ground level, and
/// flattens the ground around it.
/// </summary>
[Icon( "vertical_align_bottom" )]
public sealed class FlattenGroundComponent : Component
{
	/// <summary>
	/// How far up or down in voxels can we move this object, to match ground level.
	/// </summary>
	[Property]
	public RangedFloat OffsetRange { get; set; }

	/// <summary>
	/// When flattening, how much variance in voxels is okay. Use 0 for perfectly flat.
	/// </summary>
	[Property]
	public RangedFloat FlattenRange { get; set; }

	/// <summary>
	/// Region where the ground should be flat.
	/// </summary>
	[Property]
	public Rect Area { get; set; } = new Rect( -64f, -64f, 128f, 128f );

	private static (float Min, float Max) GetRange( RangedFloat rangedFloat )
	{
		return rangedFloat.Range switch
		{
			RangedFloat.RangeType.Fixed => (rangedFloat.x, rangedFloat.x),
			RangedFloat.RangeType.Between => (rangedFloat.x, rangedFloat.y),
			_ => default
		};
	}

	protected override void DrawGizmos()
	{
		Gizmo.Draw.Color = Color.White;
		Gizmo.Draw.LineBBox( new BBox( Area.Position, Area.Position + Area.Size ) );

		Gizmo.Draw.Color = Color.Yellow;

		if ( OffsetRange.x != 0 || OffsetRange.y != 0 )
		{
			var (min, max) = GetRange( OffsetRange );

			var lower = new Vector3( Area.Center, min * Constants.VoxelSize );
			var upper = new Vector3( Area.Center, max * Constants.VoxelSize );

			var edge = (Vector3) Area.Size * 0.5f;

			Gizmo.Draw.LineBBox( new BBox( lower - edge, lower + edge ) );
			Gizmo.Draw.LineBBox( new BBox( upper - edge, upper + edge ) );

			Gizmo.Draw.Arrow( Area.Center, lower );
			Gizmo.Draw.Arrow( Area.Center, upper );
		}

		{
			var (min, max) = GetRange( FlattenRange );

			Gizmo.Draw.Color = Gizmo.Draw.Color.WithAlpha( 0.25f );
			Gizmo.Draw.SolidBox( new BBox( new Vector3( Area.Position, min * Constants.VoxelSize ), new Vector3( Area.Position + Area.Size, max * Constants.VoxelSize ) ) );
		}
	}
}

