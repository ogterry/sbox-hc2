namespace HC2;

internal class ItemEditor : BaseResourceEditor<ItemAsset>
{
    Sandbox.SerializedObject Object;
	public static int IconSize => 256;

	public ItemEditor()
	{
		Layout = Layout.Column();
	}

	protected override void Initialize( Asset asset, ItemAsset resource )
	{
		Layout.Clear( true );

		Object = resource.GetSerialized();

		var sheet = new ControlSheet();
		sheet.AddObject( Object );
		Layout.Add( sheet );

		var ip = Layout.Add( new IconProperty( resource ) );
		ip.Asset = asset;
		ip.ContentMargins = 16;
		Object.OnPropertyChanged += NoteChanged;
	}

	private static void RenderIcon( Asset asset, ItemAsset resource )
	{
		var path = $"/item_icons/{asset.Name}.png";

		var Scene = new ItemScene( resource );
		Scene.UpdateLighting();
		Scene.UpdateCameraPosition();

		var pixelMap = new Pixmap( IconSize * 2, IconSize * 2 );
		Scene.Camera.RenderToPixmap( pixelMap );

		var root = asset.AbsolutePath[0..^(asset.RelativePath.Length)];
		var success = pixelMap.SavePng( root + path );
	}

	public static void RenderAllIcons()
	{
		using var progress = Progress.Start( "Rendering Icons" );
		var token = Progress.GetCancel();
		var all = AssetSystem.All.Where( x => x.AssetType.FileExtension == "item" ).ToArray();

		int i = 0;
		foreach ( var asset in all )
		{
			Progress.Update( asset.Name, ++i, all.Length );

			var resource = asset.LoadResource<ItemAsset>();
			RenderIcon( asset, resource );

			if ( token.IsCancellationRequested )
				return;
		}
	}

	class IconProperty : Widget
	{
		public ItemScene Scene;

		public ItemAsset ItemResource;
		public Asset Asset { get; set; }

		NativeRenderingWidget CanvasWidget;

		public IconProperty( ItemAsset resource ) : base( null )
		{
			Layout = Layout.Row();
			ItemResource = resource;

			Scene = new ItemScene( resource );
			Scene.UpdateLighting();

			CanvasWidget = new NativeRenderingWidget( this );
			CanvasWidget.FixedHeight = IconSize;
			CanvasWidget.FixedWidth = IconSize;

			Layout.Add( CanvasWidget );
			Layout.AddStretchCell();
		}

		public string IconPath { get; internal set; }

		protected override void OnPaint()
		{
			Scene.UpdateCameraPosition();

			CanvasWidget.Camera = Scene.Camera;
			//	CanvasWidget.RenderScene();

			Update();
		}

		protected override void OnMousePress( MouseEvent e )
		{
			base.OnMousePress( e );

			if ( e.RightMouseButton )
			{
				var all = AssetSystem.All.Where( x => x.AssetType.FileExtension == "item" );

				var menu = new Menu( this );
				menu.AddOption( $"Render Icon", null, () => RenderIcon( Asset, ItemResource ) );
				menu.AddOption( $"Render All Icons ({all.Count():n0})", null, () => ItemEditor.RenderAllIcons() );
				menu.OpenAt(Editor.Application.CursorPosition );
			}
		}
	}

	public class ItemScene
	{
		public SceneWorld World;
		public SceneCamera Camera;

		public SceneModel Model;

		public SceneModel TargetModel;

		List<SceneObject> LightingObjects = new();

		public float Pitch = 45f;
		public float Yaw = 135f;
		public SlotMode Target = SlotMode.ModelBounds;

		public enum SlotMode
		{
			ModelBounds,
			Face
		}

		public ItemScene( ItemAsset resource )
		{
			World = new SceneWorld();
			Camera = new SceneCamera( "ItemEditor" );

			Model = new SceneModel( World, resource.WorldModel, Transform.Zero );
			Model.Rotation = Rotation.From( 0, 0, 0 );
			Model.Position = 0;
			Model.Update( 1 );

			Camera.World = World;
			Camera.BackgroundColor = new Color( 0.1f, 0.1f, 0.1f, 0.0f );
			Camera.AmbientLightColor = Color.Gray * 0.1f;

			TargetModel = Model;
		}

		public void UpdateLighting()
		{
			foreach ( var light in LightingObjects )
			{
				light.Delete();
			}
			LightingObjects.Clear();

			var sun = new SceneDirectionalLight( World, Rotation.From( 60, 90, 0 ), Color.White * 1.5f + Color.Cyan * 0.05f + new Color( 1f, 0.4f, 0.4f ) * 0.05f )
			{
				ShadowsEnabled = true
			};

			LightingObjects.Add( sun );

			LightingObjects.Add( new SceneLight( World, Vector3.Left * 50f + Vector3.Up * 150.0f, 512, new Color( 1f, 0.4f, 0.4f ) * 4.0f ) );
			LightingObjects.Add( new SceneLight( World, Vector3.Right * 50f + Vector3.Up * 150.0f, 512, new Color( 0.4f, 0.4f, 1f ) * 4.0f ) );
			LightingObjects.Add( new SceneLight( World, Vector3.Up * 150.0f + Vector3.Backward * 100.0f, 512, new Color( 0.7f, 0.8f, 1 ) * 3.0f ) );
		}

		public void UpdateCameraPosition()
		{
			if ( TargetModel == null )
				return;

			Camera.FieldOfView = 5;
			Camera.ZFar = 10000;
			Camera.ZNear = 10;

			Model.Update( RealTime.Delta );
			TargetModel.Update( RealTime.Delta );
			TargetModel.Transform = TargetModel.Transform.WithScale( 1f );

			var bounds = TargetModel.Bounds;

			if ( Target == SlotMode.ModelBounds )
			{
				bounds = TargetModel.Model.RenderBounds;
			}
			else if ( Target == SlotMode.Face )
			{
				var headBone = TargetModel.GetBoneWorldTransform( "head" );
				headBone.Position += Vector3.Up * 6;
				bounds = new BBox( headBone.Position - 7, headBone.Position + 7 );
			}

			var lookAngle = new Angles( Pitch, Yaw, 0 );
			var forward = lookAngle.Forward;
			var distance = MathX.SphereCameraDistance( bounds.Size.Length * 0.45f, Camera.FieldOfView );

			Camera.Position = bounds.Center - forward * distance;
			Camera.Rotation = Rotation.From( lookAngle );
		}
	}
}
