namespace TerryHunt;

/// <summary>
/// A "Terry" — a citizen launched across the arena waiting to be clicked. Builds its
/// own model + capsule collider on start and tags itself "terry" so <see cref="TerryHunter"/>
/// can identify a crosshair hit. The spawner gives him a sideways <see cref="Velocity"/>
/// so he slides across the screen; click him and he bursts into his gibs.
/// </summary>
public sealed class Terry : Component
{
	public const string Tag = "terry";

	[Property] public Model BodyModel { get; set; }

	/// <summary>Constant velocity Terry travels at, in units/second. Set by the spawner.</summary>
	[Property] public Vector3 Velocity { get; set; }

	/// <summary>Seconds Terry lives before despawning if he's never clicked (once he's off-screen).</summary>
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
		// Cruise across the screen at the velocity the spawner handed us.
		WorldPosition += Velocity * Time.Delta;

		// Once he's slid off-screen (or just hung around too long), clean him up.
		if ( _despawn )
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
			// over Terry's travel so the explosion drifts the way he was moving.
			var dir = Rotation.FromYaw( Random.Shared.Float( 0, 360 ) ).Forward;
			body.Velocity = Velocity + dir * GibForce + Vector3.Up * Random.Shared.Float( 0.4f, 0.9f ) * GibForce;
			body.AngularVelocity = Vector3.Random * 20f;
		}

		GameObject.Destroy();
	}
}
