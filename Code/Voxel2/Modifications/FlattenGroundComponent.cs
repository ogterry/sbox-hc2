using System;
using System.Text.Json.Serialization;

namespace Voxel.Modifications;

#nullable enable

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

	/// <summary>
	/// When flattening, how many blocks away should be affected to make a smooth transition?
	/// </summary>
	[Property, Range( 0f, 16f )]
	public int SmoothingRadius { get; set; } = 8;

	[JsonIgnore]
	public float Impact => (Area.Width + SmoothingRadius * Constants.VoxelSize)
		* (Area.Height + SmoothingRadius * Constants.VoxelSize);

	private bool HasModel => GetModel() is not null;

	[Button( "Match Model / Prop", Icon = "align"), ShowIf( nameof(HasModel), true )]
	public void MatchModel()
	{
		// TODO: nested models

		if ( GetModel() is { } model )
		{
			var bounds = model.Bounds;

			Area = new Rect( bounds.Mins - 8f, bounds.Size + 16f );
		}
	}

	private Model? GetModel()
	{
		return Components.Get<Prop>()?.Model
			?? Components.Get<ModelRenderer>()?.Model;
	}

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
		if ( !Gizmo.IsSelected ) return;

		Gizmo.Draw.Color = Color.White;
		Gizmo.Draw.LineBBox( new BBox( Area.Position, Area.Position + Area.Size ) );

		if ( SmoothingRadius > 0 )
		{
			Gizmo.Draw.Color = Color.Blue;
			Gizmo.Draw.LineBBox( new BBox( Area.Position - SmoothingRadius * Constants.VoxelSize, Area.Position + Area.Size + SmoothingRadius * Constants.VoxelSize ) );
		}

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

	internal static void Apply( Heightmap heightmap, GameObject obj )
	{
		var components = obj.Components
			.GetAll<FlattenGroundComponent>()
			.OrderByDescending( x => x.Impact )
			.ToArray();

		// TODO: these can affect each other! how do we deal with that?

		foreach ( var component in components )
		{
			component.Apply( heightmap );
		}
	}

	private float Ease( float dist )
	{
		return 0.5f - MathF.Cos( dist * MathF.PI ) * 0.5f;
	}

	private void Apply( Heightmap heightmap )
	{
		// TODO: optimize!
		// TODO: Take into account scale

		var zBias = Constants.VoxelSize * 0.5f;

		var tl = Transform.World.PointToWorld( new Vector3( Area.TopLeft, zBias ) );
		var tr = Transform.World.PointToWorld( new Vector3( Area.TopRight, zBias ) );
		var bl = Transform.World.PointToWorld( new Vector3( Area.BottomLeft, zBias ) );
		var br = Transform.World.PointToWorld( new Vector3( Area.BottomRight, zBias ) );

		var min = Vector3.Min( Vector3.Min( tl, tr ), Vector3.Min( bl, br ) );
		var max = Vector3.Max( Vector3.Max( tl, tr ), Vector3.Max( bl, br ) );

		// TODO: vary based on size
		// TODO: clean up when I'm not writing this on a tiny laptop on a train

		var maxDist = SmoothingRadius;

		var localMin = heightmap.WorldToVoxelCoord( min ) - new Vector3Int( maxDist, 0, maxDist );
		var localMax = heightmap.WorldToVoxelCoord( max ) + new Vector3Int( maxDist, 0, maxDist );

		var distScale = 1f / (Constants.VoxelSize * maxDist);
		var targetHeight = localMin.y;

		var flattenMin = (int)MathF.Round( FlattenRange.x );
		var flattenMax = (int)MathF.Round( (FlattenRange.Range == RangedFloat.RangeType.Between ? FlattenRange.y : flattenMin) );

		// first pass: find average height of affected ground

		var totalTexels = 0f;
		var totalOffset = 0f;

		for ( var z = localMin.z; z <= localMax.z; ++z )
		for ( var x = localMin.x; x <= localMax.x; ++x )
		{
			var sample = heightmap[x, z];
			var worldPos = heightmap.VoxelCoordToWorld( new Vector3Int( x, sample.Height, z ) );
			var localPos = Transform.World.PointToLocal( worldPos );

			var xDist = Math.Max( Math.Max( Area.Left - localPos.x, localPos.x - Area.Right ) * distScale, 0 );
			var yDist = Math.Max( Math.Max( Area.Top - localPos.y, localPos.y - Area.Bottom ) * distScale, 0 );

			if ( xDist > 1f || yDist > 1f ) continue;

			var dist = MathF.Sqrt( xDist * xDist + yDist * yDist );
			var goal = Math.Clamp( sample.Height, targetHeight + flattenMin, targetHeight + flattenMax );

			if ( dist > 1f ) continue;

			totalTexels += 1f - dist;
			totalOffset += (1f - dist) * (sample.Height - goal);
		}

		var avgOffset = totalTexels > 0f ? totalOffset / totalTexels : 0f;

		var offsetMin = OffsetRange.x;
		var offsetMax = OffsetRange.Range == RangedFloat.RangeType.Between ? OffsetRange.y : offsetMin;

		var offset = (int)MathF.Round( Math.Clamp( avgOffset, offsetMin, offsetMax ) );

		if ( offset != 0 )
		{
			targetHeight += offset;
			Transform.Position += Vector3.Up * offset * Constants.VoxelSize;
		}

		// second pass: write to heightmap

		for ( var z = localMin.z; z <= localMax.z; ++z )
		for ( var x = localMin.x; x <= localMax.x; ++x )
		{
			var sample = heightmap[x, z];
			var worldPos = heightmap.VoxelCoordToWorld( new Vector3Int( x, sample.Height, z ) );
			var localPos = Transform.World.PointToLocal( worldPos );

			var xDist = Math.Max( Math.Max( Area.Left - localPos.x, localPos.x - Area.Right ) * distScale, 0 );
			var yDist = Math.Max( Math.Max( Area.Top - localPos.y, localPos.y - Area.Bottom ) * distScale, 0 );

			if ( xDist > 1f || yDist > 1f ) continue;

			var dist = MathF.Sqrt( xDist * xDist + yDist * yDist );
			var goal = Math.Clamp( sample.Height, targetHeight + flattenMin, targetHeight + flattenMax );

			if ( dist > 1f ) continue;

			if ( dist <= 0f )
			{
				heightmap[x, z] = sample with { Height = goal };
			}
			else
			{
				heightmap[x, z] = sample with
				{
					Height = (int)MathF.Round( goal + (sample.Height - goal) * Ease( dist ) )
				};
			}
		}
	}
}

