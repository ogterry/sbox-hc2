
using System;
using Sandbox.Events;
using Voxel.Modifications;

namespace Voxel;

[Icon( "healing" )]
public sealed class VoxelDamage : Component,
	IGameEventHandler<DamageWorldEvent>
{
	[RequireComponent] public VoxelRenderer Renderer { get; private set; }
	[RequireComponent] public VoxelNetworking VoxelNetworking { get; private set; }

	void IGameEventHandler<DamageWorldEvent>.OnGameEvent( DamageWorldEvent eventArgs )
	{
		if ( IsProxy ) return;

		var coords = Renderer.WorldToVoxelCoords( eventArgs.Damage.Position );

		if ( Renderer.Model.OutOfBounds( coords ) ) return;

		VoxelNetworking.Modify( new CarveModification( coords, 1, Random.Shared.Next() ) );
	}
}
