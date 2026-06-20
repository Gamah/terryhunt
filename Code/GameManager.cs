namespace TerryHunt;

/// <summary>
/// Bootstraps the dynamic part of the game in code. The scene file owns the static
/// stuff (lighting, skybox, floor, camera); on start this drops the local player into
/// the world in first person and then keeps launching Terrys across the view, alternating
/// left-to-right and right-to-left every <see cref="SpawnInterval"/> seconds. It also owns
/// the score and paints the counter at the top of the screen.
///
/// Spawned objects are flagged NotSaved so an accidental editor save never bakes
/// runtime objects into the .scene.
/// </summary>
public sealed class GameManager : Component
{
	/// <summary>Seconds between Terry launches.</summary>
	[Property] public float SpawnInterval { get; set; } = 1.5f;

	/// <summary>How far in front of the player the Terrys cross.</summary>
	[Property] public float SpawnDistance { get; set; } = 200f;

	/// <summary>How far to each side of centre a Terry travels before despawning.</summary>
	[Property] public float CrossRange { get; set; } = 280f;

	/// <summary>How fast a Terry slides across the view, in units/second.</summary>
	[Property] public float CrossSpeed { get; set; } = 220f;

	/// <summary>Height above the floor the Terrys are launched at.</summary>
	[Property] public float SpawnHeight { get; set; } = 48f;

	/// <summary>How many Terrys the player has clicked.</summary>
	public int Score { get; private set; }

	GameObject _player;
	TimeUntil _nextSpawn;
	bool _fromLeft = true;

	protected override void OnStart()
	{
		_player = SpawnPlayer();
		_nextSpawn = 0f; // launch one right away
	}

	protected override void OnUpdate()
	{
		if ( _nextSpawn )
		{
			LaunchTerry();
			_nextSpawn = SpawnInterval;
		}

		DrawScore();
	}

	/// <summary>Bump the score; called by <see cref="TerryHunter"/> when a Terry is hit.</summary>
	public void AddScore( int amount = 1 ) => Score += amount;

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

	void LaunchTerry()
	{
		var rot = _player.WorldRotation;
		var forward = rot.Forward.WithZ( 0 ).Normal;
		var right = rot.Right.WithZ( 0 ).Normal;

		var centre = _player.WorldPosition.WithZ( 0 ) + forward * SpawnDistance + Vector3.Up * SpawnHeight;

		// Alternate which side we launch from each time.
		var sign = _fromLeft ? -1f : 1f;
		_fromLeft = !_fromLeft;

		var start = centre + right * CrossRange * sign;
		var velocity = right * CrossSpeed * -sign; // head toward the far side

		var go = new GameObject( true, "Terry" );
		go.Flags |= GameObjectFlags.NotSaved;
		go.WorldPosition = start;
		// Face back toward the player so Terry is staring you down as he slides past.
		go.WorldRotation = Rotation.LookAt( -forward, Vector3.Up );

		var terry = go.AddComponent<Terry>();
		terry.Velocity = velocity;
		// Live just long enough to cross the full span, plus a small margin.
		terry.Lifetime = (CrossRange * 2f) / CrossSpeed + 1f;
	}

	void DrawScore()
	{
		if ( Scene.Camera is not CameraComponent cam )
			return;

		var hud = cam.Hud;
		var scope = new TextRendering.Scope( $"Score: {Score}", Color.White, 40 );
		var rect = new Rect( 0, 24, Screen.Width, 48 );
		hud.DrawText( scope, rect, TextFlag.Center );
	}
}
