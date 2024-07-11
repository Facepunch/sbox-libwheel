using Sandbox;
using System;

[Category( "Vehicles" )]
[Title( "Wheel" )]
[Icon( "sync" )]
public sealed class Wheel : Component
{
	[Property] public float MinSuspensionLength { get; set; } = 0f;
	[Property] public float MaxSuspensionLength { get; set; } = 8f;
	[Property] public float SuspensionStiffness { get; set; } = 3000.0f;
	[Property] public float SuspensionDamping { get; set; } = 140.0f;
	[Property] public float WheelRadius { get; set; } = 14.0f;

	[Property] public WheelFrictionInfo ForwardFriction { get; set; }
	[Property] public WheelFrictionInfo SideFriction { get; set; }

	public bool IsGrounded => _groundTrace.Hit;

	private const float _lowSpeedThreshold = 20.0f;

	private SceneTraceResult _groundTrace;
	private Rigidbody _rigidbody;
	private float _motorTorque;

	protected override void OnEnabled()
	{
		_rigidbody = Components.GetInAncestorsOrSelf<Rigidbody>();
	}

	protected override void OnFixedUpdate()
	{
		if ( Scene.IsEditor )
			return;

		DoTrace();

		UpdateSuspension();
		UpdateWheelForces();
	}

	public void ApplyMotorTorque( float value )
	{
		_motorTorque = _motorTorque.LerpTo( value, 1f * Time.Delta );
	}

	private void UpdateWheelForces()
	{
		if ( IsGrounded )
		{
			var forwardDir = Transform.Rotation.Forward;
			var sideDir = Transform.Rotation.Right;
			var wheelVelocity = _rigidbody.GetVelocityAtPoint( Transform.Position );

			var side = Vector3.Zero;
			var forward = Vector3.Zero;

			//
			// We use a low speed threshold to avoid jittering when the car is stopped.
			//
			if ( wheelVelocity.Length > _lowSpeedThreshold )
			{
				// Simple friction model
				var sideSlip = Vector3.Dot( wheelVelocity, sideDir ) / (wheelVelocity.Length + 0.01f);
				var forwardSlip = Vector3.Dot( wheelVelocity, forwardDir ) / (wheelVelocity.Length + 0.01f);

				side = -SideFriction.Evaluate( MathF.Abs( sideSlip ) ) * MathF.Sign( sideSlip ) * sideDir;
				forward = -ForwardFriction.Evaluate( MathF.Abs( forwardSlip ) ) * MathF.Sign( forwardSlip ) * forwardDir;
			}

			var targetAccel = (side + forward);
			targetAccel += _motorTorque * Transform.Rotation.Forward;
			_rigidbody.ApplyForceAt( GameObject.Transform.Position, targetAccel / Time.Delta );
		}
	}

	private void UpdateSuspension()
	{
		var tx = Transform;

		var suspensionLength = _groundTrace.Distance;

		//
		// If the point is touching any surface then we want it to
		// 'push' the car up at the point based on Hooke's law.
		//
		if ( IsGrounded )
		{
			var worldVelocity = _rigidbody.GetVelocityAtPoint( Transform.Position );

			var suspensionTotalLength = (MaxSuspensionLength + WheelRadius) - MinSuspensionLength;
			var magnitude = -SuspensionDamping * worldVelocity.z - SuspensionStiffness * (suspensionLength - suspensionTotalLength);
			var force = new Vector3( 0, 0, magnitude ) / Time.Delta;

			_rigidbody.ApplyForceAt( tx.Position, force );
		}
	}

	private void DoTrace()
	{
		var startPos = Transform.Position + Transform.Rotation.Down * MinSuspensionLength;
		var endPos = startPos + Transform.Rotation.Down * (MaxSuspensionLength + WheelRadius);

		_groundTrace = Scene.Trace
			.Radius( 1f )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "car" )
			.FromTo( startPos, endPos )
			.Run();
	}
}
