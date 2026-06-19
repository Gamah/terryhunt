namespace TerryHunt;

/// <summary>
/// Bootstraps the whole game in code so the scene file can stay tiny (just lighting +
/// a camera + this manager). On start it lays down a big floor, drops the local player
/// into it in first person, and spawns the first Terry to hunt.
///
/// Everything generated here is flagged NotSaved so an accidental editor save never
/// bakes runtime geometry into the .scene. Modelled on rotaliate's LobbyRoom approach:
/// non-uniform scale in scene JSON is unreliable, so geometry is built from C#.
/// </summary>
public sealed class GameManager : Component
{
	/// <summary>Side length of the (square) floor in units. 1 unit ≈ 1 inch.</summary>
	[Property] public float FloorSize { get; set; } = 2000f;

	[Property] public float FloorThickness { get; set; } = 20f;

	[Property] public Color FloorColor { get; set; } = new Color( 0.22f, 0.24f, 0.28f );

	/// <summary>How far in front of the player the Terry spawns.</summary>
	[Property] public float SpawnDistance { get; set; } = 160f;

	protected override void OnStart()
	{
		BuildFloor();
		var player = SpawnPlayer();
		SpawnTerry( player );
	}

	void BuildFloor()
	{
		// Floor top sits at Z=0 so everything else can spawn relative to ground level.
		var floor = new GameObject( true, "Floor" );
		floor.Flags |= GameObjectFlags.NotSaved;
		floor.Parent = GameObject;
		floor.WorldPosition = new Vector3( 0, 0, -FloorThickness * 0.5f );

		var size = new Vector3( FloorSize, FloorSize, FloorThickness );

		var collider = floor.AddComponent<BoxCollider>();
		collider.Scale = size;

		// Visual lives on a separately scaled child so the collider GO keeps uniform
		// scale (a BoxCollider under non-uniform scale can wedge the physics engine).
		var box = Model.Load( "models/dev/box.vmdl" );
		if ( box == null )
		{
			Log.Warning( "[TerryHunt] models/dev/box.vmdl failed to load — floor has no visual" );
			return;
		}

		var visual = new GameObject( true, "Floor_Visual" );
		visual.Flags |= GameObjectFlags.NotSaved;
		visual.Parent = floor;
		var modelSize = box.Bounds.Size;
		visual.LocalScale = new Vector3(
			size.x / modelSize.x,
			size.y / modelSize.y,
			size.z / modelSize.z );

		var renderer = visual.AddComponent<ModelRenderer>();
		renderer.Model = box;
		renderer.Tint = FloorColor;
	}

	GameObject SpawnPlayer()
	{
		var go = new GameObject( true, "Player" );
		go.Flags |= GameObjectFlags.NotSaved;
		// Drop in slightly above the floor; the controller settles onto it.
		go.WorldPosition = new Vector3( 0, 0, 10f );

		var controller = go.AddComponent<PlayerController>();
		controller.ThirdPerson = false;          // first person, as asked
		controller.UseCameraControls = true;     // drives the scene Camera
		controller.CreateBodyRenderer();         // citizen body (hidden in first person)

		go.AddComponent<TerryHunter>();
		return go;
	}

	void SpawnTerry( GameObject player )
	{
		var forward = player.WorldRotation.Forward.WithZ( 0 ).Normal;
		var pos = player.WorldPosition.WithZ( 0 ) + forward * SpawnDistance;

		var go = new GameObject( true, "Terry" );
		go.Flags |= GameObjectFlags.NotSaved;
		go.WorldPosition = pos;
		// Face back toward the player so Terry is staring you down.
		go.WorldRotation = Rotation.LookAt( -forward, Vector3.Up );

		go.AddComponent<Terry>();
	}
}
