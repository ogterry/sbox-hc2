
using System;
using Sandbox.Events;
using Voxel.Modifications;

namespace Voxel;

public record DamageWorldEvent( Vector3 Position, Vector3 Direction, float Damage ) : IGameEvent;

[Icon( "healing" )]
public sealed class VoxelDamage : Component,
	IGameEventHandler<DamageWorldEvent>
{
	[RequireComponent] public VoxelRenderer Renderer { get; private set; }
	[RequireComponent] public VoxelNetworking VoxelNetworking { get; private set; }

	void IGameEventHandler<DamageWorldEvent>.OnGameEvent( DamageWorldEvent eventArgs )
	{
		if ( IsProxy ) return;

		var coords = Renderer.WorldToVoxelCoords( eventArgs.Position );

		if ( Renderer.Model.OutOfBounds( coords ) ) return;

		VoxelNetworking.Modify( new CarveModification( coords, 1, Random.Shared.Next() ) );
	}
}
