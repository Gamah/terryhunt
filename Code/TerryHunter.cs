namespace TerryHunt;

/// <summary>
/// Lives on the Player. Draws a crosshair at the screen centre and, on left click
/// (attack1), fires a ray straight out of the eye. If it lands on a <see cref="Terry"/>,
/// that Terry is destroyed.
/// </summary>
public sealed class TerryHunter : Component
{
	[Property] public float Reach { get; set; } = 4000f;

	[Property] public float CrosshairSize { get; set; } = 10f;

	PlayerController _controller;

	protected override void OnAwake()
	{
		_controller = GetComponent<PlayerController>();
	}

	protected override void OnUpdate()
	{
		DrawCrosshair();

		if ( Input.Pressed( "attack1" ) )
			TryShoot();
	}

	void TryShoot()
	{
		// Aim from the eye along where we're looking. EyeTransform is driven by the
		// PlayerController each frame, so it already matches the first-person view.
		var eye = _controller.EyeTransform;
		var from = eye.Position;
		var to = from + eye.Rotation.Forward * Reach;

		var tr = Scene.Trace.Ray( from, to )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		if ( !tr.Hit || tr.GameObject is null )
			return;

		var terry = tr.GameObject.GetComponentInParent<Terry>();
		if ( terry is null )
			return;

		// Score the hit, then burst Terry into his gibs.
		Scene.GetAllComponents<GameManager>().FirstOrDefault()?.AddScore();
		terry.Explode();
	}

	void DrawCrosshair()
	{
		if ( Scene.Camera is not CameraComponent cam )
			return;

		var hud = cam.Hud;
		var c = Screen.Size * 0.5f;
		var s = CrosshairSize;
		var col = Color.White.WithAlpha( 0.85f );

		hud.DrawLine( c + new Vector2( -s, 0 ), c + new Vector2( s, 0 ), 2f, col );
		hud.DrawLine( c + new Vector2( 0, -s ), c + new Vector2( 0, s ), 2f, col );
	}
}
