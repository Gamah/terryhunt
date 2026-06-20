namespace TerryHunt;

/// <summary>
/// Bootstraps the dynamic part of the game in code. The scene file owns the static
/// stuff (lighting, skybox, floor, camera); on start this drops the local player into
/// the world in first person and then keeps launching Terrys into the air every
/// <see cref="SpawnInterval"/> seconds, lobbing them ballistically from alternating sides
/// so they arc across the view. It also owns the score and paints the counter at the top
/// of the screen.
///
/// Spawned objects are flagged NotSaved so an accidental editor save never bakes
/// runtime objects into the .scene.
/// </summary>
public sealed class GameManager : Component
{
	/// <summary>Seconds between Terry launches.</summary>
	[Property] public float SpawnInterval { get; set; } = 1.5f;

	/// <summary>How far in front of the player the Terrys are lobbed across.</summary>
	[Property] public float SpawnDistance { get; set; } = 1200f;

	/// <summary>How far out to each side a Terry is launched from.</summary>
	[Property] public float CrossRange { get; set; } = 260f;

	/// <summary>Sideways launch speed carrying a Terry across the view, in units/second.</summary>
	[Property] public float CrossSpeed { get; set; } = 1200f;

	/// <summary>Upward launch speed that lobs a Terry into the air, in units/second.</summary>
	[Property] public float LaunchUpSpeed { get; set; } = 2000f;

	/// <summary>How much to randomize each launch vector, skeet-style (0 = identical lobs).</summary>
	[Property, Range( 0f, 1f )] public float LaunchSpread { get; set; } = 0.3f;

	/// <summary>Downward acceleration applied to airborne Terrys, in units/second².</summary>
	[Property] public float Gravity { get; set; } = 800f;

	/// <summary>Seconds a Terry keeps running along the ground after landing before despawning.</summary>
	[Property] public float RunDuration { get; set; } = 4f;

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

		// Launch from roughly the player's feet height so Terry rises up into view.
		var floorZ = _player.WorldPosition.z;
		var centre = _player.WorldPosition.WithZ( floorZ ) + forward * SpawnDistance;

		// Alternate which side we launch from each time.
		var sign = _fromLeft ? -1f : 1f;
		_fromLeft = !_fromLeft;

		var start = centre + right * CrossRange * sign;

		// Skeet-style randomness: jitter each launch's speeds and add a little depth so no
		// two Terrys fly quite the same arc.
		float Vary() => 1f + Random.Shared.Float( -LaunchSpread, LaunchSpread );
		var upSpeed = LaunchUpSpeed * Vary();
		var crossSpeed = CrossSpeed * Vary();
		var depthSpeed = CrossSpeed * Random.Shared.Float( -LaunchSpread, LaunchSpread );

		// Lob across toward the far side, with an upward kick so he arcs through the air.
		var velocity = (right * crossSpeed * -sign) + (Vector3.Up * upSpeed) + (forward * depthSpeed);

		var go = new GameObject( true, "Terry" );
		go.Flags |= GameObjectFlags.NotSaved;
		go.WorldPosition = start;
		// Face the direction of travel; Terry re-orients himself as he flies.
		go.WorldRotation = Rotation.LookAt( velocity.WithZ( 0 ).Normal, Vector3.Up );

		var terry = go.AddComponent<Terry>();
		terry.Velocity = velocity;
		terry.Gravity = Gravity;
		terry.GroundZ = start.z;
		// Live through the full arc (up and back down) plus a stretch of ground running.
		terry.Lifetime = (2f * upSpeed / Gravity) + RunDuration;
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
