using Sandbox.UI;
using Sandbox.UI.Construct;

public class Compass : Panel
{
	private List<CompassItem> CompassItems = new();

	public Compass()
	{
		StyleSheet.Load( "/UI/HUD/Compass.scss" ); // [StyleSheet] doesn't work here?

		//
		// North/south/east/west
		//
		for ( int i = 0; i < 8; i++ )
		{
			var label = new CompassLabel( this, i );
			label.Angle = i * 45f;
			CompassItems.Add( label );
		}

		//
		// Angle numbers
		//
		float angleIncrement = 15f;
		for ( int i = 0; i < 360.0f / angleIncrement; i++ )
		{
			if ( i % (360 / (8 * angleIncrement)) == 0 )
				continue;

			var label = new CompassLabel( this, i, "small" );
			label.Label.Text = (360 - (i * angleIncrement)).CeilToInt().ToString();
			label.Angle = i * angleIncrement;
			CompassItems.Add( label );
		}
	}
}

class CompassItem : Panel
{
	public float Angle { get; set; }

	public CompassItem( Panel parent )
	{
		AddClass( "compass-item" );
		Parent = parent;
	}

	public override void Tick()
	{
		const float maxAngle = 110;

		var relativeAngle = Player.Local.CameraController.CalcRelativeYaw( Angle );
		var halfMaxAngle = maxAngle / 2.0f;
		var position = relativeAngle.LerpInverse( -halfMaxAngle, halfMaxAngle );
		Style.Left = Length.Fraction( position );
	}
}

class CompassLabel : CompassItem
{
	public Label Label { get; set; }

	public CompassLabel( Panel parent, int index, string className = null ) : base( parent )
	{
		AddClass( className );

		Label = Add.Label( GetDirectionString( index ) );
		Angle = 0;
	}

	public string GetDirectionString( int index ) => index switch
	{
		0 => "N",
		1 => "NW",
		2 => "W",
		3 => "SW",
		4 => "S",
		5 => "SE",
		6 => "E",
		7 => "NE",
		_ => "?",
	};
}
