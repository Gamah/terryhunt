using Sandbox.Citizen;

namespace TerryHunt;

/// <summary>
/// A "Terry" — a citizen launched into the air to be shot down. Builds his own model +
/// capsule collider on start, plays a running animation, and tags himself "terry" so
/// <see cref="TerryHunter"/> can identify a crosshair hit. The spawner gives him a launch
/// <see cref="Velocity"/> and he arcs through the air under <see cref="Gravity"/>; click
/// him and he bursts into his gibs.
/// </summary>
public sealed class Terry : Component
{
	public const string Tag = "terry";

	[Property] public Model BodyModel { get; set; }

	/// <summary>Current velocity in units/second. Set by the spawner as the launch velocity.</summary>
	[Property] public Vector3 Velocity { get; set; }

	/// <summary>Downward acceleration applied each frame, in units/second².</summary>
	[Property] public float Gravity { get; set; } = 800f;

	/// <summary>Height Terry was launched from; he despawns once he falls back to it.</summary>
	[Property] public float GroundZ { get; set; }

	/// <summary>Ground speed fed to the run animation, in units/second.</summary>
	[Property] public float RunSpeed { get; set; } = 160f;

	/// <summary>Backstop lifetime in case Terry never lands (e.g. launched oddly).</summary>
	[Property] public float Lifetime { get; set; } = 6f;

	/// <summary>How hard the gibs are flung apart when Terry is killed.</summary>
	[Property] public float GibForce { get; set; } = 250f;

	/// <summary>The chunks Terry comes apart into. Shipped by the "Facepunch Citizen Gibs" packages.</summary>
	static readonly string[] GibModels =
	{
		"models/citizen_gibs/models/torn_torso_gib.vmdl",
		"models/citizen_gibs/models/pelvis_gib_cap.vmdl",
		"models/citizen_gibs/models/torn_arm_gib.vmdl",
		"models/citizen_gibs/models/torn_hand_gib.vmdl",
		"models/citizen_gibs/models/left_arm_gib_cap.vmdl",
		"models/citizen_gibs/models/right_leg_gib_cap.vmdl",
		"models/citizen_gibs/models/left_foot_gib_cap.vmdl",
		"models/citizen_gibs/models/right_foot_gib_cap.vmdl",
		"models/citizen_gibs/models/organ_gib.vmdl",
		"models/citizen_gibs/models/intestine_gib.vmdl",
	};

	CitizenAnimationHelper _anim;
	TimeUntil _despawn;

	protected override void OnStart()
	{
		GameObject.Tags.Add( Tag );

		var model = BodyModel ?? Model.Load( "models/citizen/citizen.vmdl" );

		var visual = new GameObject( true, "Terry_Body" );
		visual.Flags |= GameObjectFlags.NotSaved;
		visual.Parent = GameObject;

		var renderer = visual.AddComponent<SkinnedModelRenderer>();
		renderer.Model = model;

		// Drive the citizen animgraph so Terry runs his legs while sailing through the air.
		_anim = visual.AddComponent<CitizenAnimationHelper>();
		_anim.Target = renderer;
		_anim.IsGrounded = true; // play the grounded run cycle rather than a falling pose

		// A capsule roughly the size of a citizen so the crosshair trace has something
		// to hit. The collider's GameObject inherits the "terry" tag from the trace's
		// point of view via GetComponentInParent, so it can live on a child.
		var col = visual.AddComponent<CapsuleCollider>();
		col.Start = new Vector3( 0, 0, 8 );
		col.End = new Vector3( 0, 0, 64 );
		col.Radius = 16f;

		_despawn = Lifetime;
	}

	protected override void OnUpdate()
	{
		// Ballistic flight: gravity pulls the velocity down, then we integrate position.
		Velocity += Vector3.Down * Gravity * Time.Delta;
		WorldPosition += Velocity * Time.Delta;

		// Face the way he's flying and run his legs at that pace.
		var horizontal = Velocity.WithZ( 0 );
		if ( horizontal.Length > 1f )
			WorldRotation = Rotation.LookAt( horizontal.Normal, Vector3.Up );

		_anim?.WithVelocity( WorldRotation.Forward * RunSpeed );

		// Despawn once he's arced back down to where he launched (or the backstop fires).
		if ( (Velocity.z < 0f && WorldPosition.z <= GroundZ) || _despawn )
			GameObject.Destroy();
	}

	/// <summary>
	/// Blow Terry apart into his gibs, hurling each chunk outward with a bit of force,
	/// then remove the (now invisible) Terry himself.
	/// </summary>
	public void Explode()
	{
		var origin = WorldPosition + Vector3.Up * 40f;

		foreach ( var path in GibModels )
		{
			var model = Model.Load( path );
			if ( model is null || model.IsError )
				continue;

			var go = new GameObject( true, "Terry_Gib" );
			go.Flags |= GameObjectFlags.NotSaved;
			go.WorldPosition = origin + Vector3.Random * 12f;
			go.WorldRotation = Rotation.Random;

			// Gib is a Prop that builds its own renderer + collider + rigidbody from the
			// model and fades itself out after FadeTime, so we don't have to clean up.
			var gib = go.Components.Create<Gib>();
			gib.Model = model;
			gib.FadeTime = 5f;

			var body = go.Components.Get<Rigidbody>();
			if ( body is null )
				continue;

			// Fling each chunk out in a random direction with an upward kick, carrying
			// over Terry's flight so the explosion drifts the way he was moving.
			var dir = Rotation.FromYaw( Random.Shared.Float( 0, 360 ) ).Forward;
			body.Velocity = Velocity + dir * GibForce + Vector3.Up * Random.Shared.Float( 0.4f, 0.9f ) * GibForce;
			body.AngularVelocity = Vector3.Random * 20f;
		}

		GameObject.Destroy();
	}
}
