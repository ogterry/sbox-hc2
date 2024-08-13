using System;
using Sandbox;

namespace HC2.Mobs;

#nullable enable

public sealed class Leg : Component
{
	[Property] public GameObject Upper { get; set; } = null!;
	[Property] public GameObject Lower { get; set; } = null!;
	[Property] public GameObject Foot { get; set; } = null!;

	[Property] public float StepPeriod { get; set; } = 1f;
	[Property, Range( 0f, 1f )] public float StepPhase { get; set; }

	private float _upperLength;
	private float _lowerLength;

	private Vector3 _lastStaticFootTarget;
	private Vector3 _velocity;
	private TimeSince _sinceLastStepCheck;
	private TimeSince _sinceLastStep;

	private Vector3 _stepStartPosition;

	public Vector3 FootTarget { get; set; }
	public float TotalLength => _upperLength + _lowerLength;
	public float MaxStepLength => TotalLength * 0.5f;

	private Vector3 StaticFootTarget => Transform.Position + Transform.Rotation.Forward * TotalLength / 2f - Vector3.Up * TotalLength / 3f;
	private Vector3 IdealFootTarget => StaticFootTarget + (_velocity.Length < MaxStepLength ? _velocity : _velocity.Normal * MaxStepLength);

	protected override void OnStart()
	{
		_upperLength = (Lower.Transform.LocalPosition - Upper.Transform.LocalPosition).Length;
		_lowerLength = (Foot.Transform.LocalPosition - Lower.Transform.LocalPosition).Length;

		_lastStaticFootTarget = StaticFootTarget;

		Step();

		_sinceLastStepCheck = StepPeriod * StepPhase;
	}

	protected override void OnFixedUpdate()
	{
		UpdateVelocity();

		if ( _sinceLastStepCheck > StepPeriod )
		{
			_sinceLastStepCheck = 0f;

			if ( (FootTarget - IdealFootTarget).Length > MaxStepLength )
			{
				Step();
			}
		}
		else if ( (FootTarget - IdealFootTarget).Length > MaxStepLength * 2f || Vector3.Dot( FootTarget - Transform.Position, Transform.Rotation.Forward ) < 0f )
		{
			Step();
		}

		var minRange = Math.Abs( _upperLength - _lowerLength ) * 1.25f;
		var maxRange = _upperLength + _lowerLength;
		var targetPos = Transform.World.PointToLocal( FootTarget );
		var range = targetPos.Length;

		if ( range < minRange || range > maxRange )
		{
			range = Math.Clamp( range, minRange, maxRange );
			targetPos = (targetPos.IsNearZeroLength ? Vector3.Forward : targetPos.Normal) * range;
		}

		var t = Math.Clamp( _sinceLastStep * 2f / StepPeriod, 0f, 1f );

		targetPos = Vector3.Lerp( _stepStartPosition, targetPos, t )
			+ Vector3.Up * MathF.Sin( t * MathF.PI ) * TotalLength / 3f;

		var diff = targetPos - Foot.Transform.LocalPosition;

		Foot.Transform.LocalPosition += diff * 0.1f;

		var r = Foot.Transform.LocalPosition.Length;
		var x = (r * r - _upperLength * _upperLength + _lowerLength * _lowerLength) / (2f * r);
		var h = x >= _upperLength ? 0f : MathF.Sqrt( _upperLength * _upperLength - x * x );

		var along = Foot.Transform.LocalPosition.Normal;
		var up = Vector3.Cross( along, Vector3.Cross( Vector3.Up, along ) ).Normal;

		Lower.Transform.LocalPosition = along * x + up * h;
		Upper.Transform.LocalRotation = Rotation.LookAt( Lower.Transform.LocalPosition );
		Lower.Transform.LocalRotation = Rotation.LookAt( Foot.Transform.LocalPosition - Lower.Transform.LocalPosition );
	}

	private void UpdateVelocity()
	{
		if ( Time.Delta <= 0.001f ) return;

		var staticFootTarget = StaticFootTarget;

		_velocity = (staticFootTarget - _lastStaticFootTarget) / Time.Delta;
		_lastStaticFootTarget = staticFootTarget;
	}

	private void Step()
	{
		FootTarget = IdealFootTarget;

		_sinceLastStep = 0f;
		_stepStartPosition = Foot.Transform.LocalPosition;
	}
}
