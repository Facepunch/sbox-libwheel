using Sandbox;

public struct WheelFrictionInfo
{
	public float ExtremumSlip { get; set; } = 1f;
	public float ExtremumValue { get; set; } = 1f;
	public float AsymptoteSlip { get; set; } = 2f;
	public float AsymptoteValue { get; set; } = 0.5f;
	public float Stiffness { get; set; } = 1f;

	public WheelFrictionInfo()
	{

	}

	public float Evaluate( float slip )
	{
		float value;

		if ( slip <= ExtremumSlip )
		{
			value = (slip / ExtremumSlip) * ExtremumValue;
		}
		else
		{
			value = ExtremumValue - ((slip - ExtremumSlip) / (AsymptoteSlip - ExtremumSlip)) * (ExtremumValue - AsymptoteValue);
		}

		return (value * Stiffness).Clamp( 0, float.MaxValue );
	}
}
