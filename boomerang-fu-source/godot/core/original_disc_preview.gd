class_name OriginalDiscPreview
extends OriginalCharacterPreview
var flying: Dictionary
var grounded: Dictionary
var overlay_time := 0.0
var on_ground := false
var base_color := Color.WHITE
var blink_white := false

func retire() -> void:
	# Detach render geometry before releasing its per-disc material overrides.
	# Otherwise a queued shader-parameter update can inspect a freed material RID.
	for mesh: MeshInstance3D in find_children("*", "MeshInstance3D", true, false):
		mesh.mesh = null
	queue_free()

func initialize(color: Color) -> void:
	name = "Disc"
	base_color = color
	configure("res://assets/characters/Disc.glb", "res://assets/characters/Disc-spin.json")
	# Pool.Spawn replaces the prefab root's saved position.
	model.position = Vector3.ZERO
	if targets.has("Disc__2058") and targets["Disc__2058"].has("node"):
		targets["Disc__2058"].node.position = Vector3.ZERO
	flying = JSON.parse_string(FileAccess.get_file_as_string("res://assets/characters/Disc-flying.json"))
	grounded = JSON.parse_string(FileAccess.get_file_as_string("res://assets/characters/Disc-grounded.json"))
	for overlay in [flying, grounded]:
		for track in overlay.tracks:
			if not targets.has(track.node): push_error("Missing disc overlay target: " + track.node)
	color_meshes(model, color, true)

static func color_meshes(node: Node, color: Color, make_unique := false) -> void:
	if node is MeshInstance3D:
		for index in node.mesh.get_surface_count():
			var instance_material: Material = node.get_surface_override_material(index)
			if instance_material is BaseMaterial3D and not make_unique:
				instance_material.albedo_color = color
			else:
				var material: Material = node.get_active_material(index)
				if material is BaseMaterial3D:
					var copy: BaseMaterial3D = material.duplicate()
					copy.albedo_color = color
					node.set_surface_override_material(index, copy)
	for child in node.get_children(): color_meshes(child, color, make_unique)

func advance_disc(delta: float, motion: OriginalDiscMotion) -> void:
	var should_blink := motion.grenade_active and motion.grenade.blink_on
	if should_blink != blink_white:
		blink_white = should_blink
		color_meshes(model,Color.WHITE if should_blink else base_color)
	advance(delta * motion.spin_speed())
	var grounded_now := motion.movement == OriginalDiscMotion.Movement.DROPPED
	if on_ground != grounded_now:
		on_ground = grounded_now
		overlay_time = 0.0
	overlay_time += delta
	var overlay := grounded if on_ground else flying
	var time := fposmod(overlay_time, overlay.duration) if overlay.loop else minf(overlay_time, overlay.duration)
	apply_tracks(overlay.tracks, time)
