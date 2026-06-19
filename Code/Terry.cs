namespace TerryHunt;

/// <summary>
/// A "Terry" — a citizen standing on the floor waiting to be clicked. Builds its own
/// model + capsule collider on start and tags itself "terry" so <see cref="TerryHunter"/>
/// can identify a crosshair hit. Click it and it's deleted.
/// </summary>
public sealed class Terry : Component
{
	public const string Tag = "terry";

	[Property] public Model BodyModel { get; set; }

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
	}
}
