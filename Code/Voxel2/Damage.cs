
using System;
using HC2;
using Sandbox.Events;
using Voxel.Modifications;

namespace Voxel;

public record DamageWorldEvent( Vector3 Position, Vector3 Direction, GameObject Inflictor, float Damage, int Radius ) : IGameEvent;

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

		var effectiveness = eventArgs.Inflictor.Components
			.GetAll<ResourceGatherer>( FindMode.EverythingInSelf )
			.Select( x => new GatherEffectiveness( x.SourceKind, x.Effectiveness ) )
			.ToArray();

		var radius = (byte) Math.Clamp( eventArgs.Radius, 1, 255 );

		VoxelNetworking.Modify( new CarveModification( eventArgs.Damage, coords, radius, Random.Shared.Next(), effectiveness ) );
	}
}
