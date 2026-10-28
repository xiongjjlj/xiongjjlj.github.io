class_name OriginalPowerupPreview
extends OriginalCharacterPreview
var collected := false
var spinning_book: Node3D
func initialize() -> void:
	configure("res://assets/powers/Powerup.glb","res://assets/powers/Powerup-spawn.json")
	model.position = Vector3.ZERO
	targets["Powerup__2066"].node.position = Vector3.ZERO
	spinning_book = targets["book__2207"].node
func collect() -> void:
	collected = true
	set_clip("res://assets/powers/Powerup-collect.json")
func advance_book(delta: float) -> void:
	advance(delta)
	if not collected and clip.name == "PowerupSpawn" and clip_time >= clip.duration:
		set_clip("res://assets/powers/Powerup-idle.json")
	spinning_book.rotate_z(deg_to_rad(OriginalTimekeeper.f32(OriginalTimekeeper.f32(delta) * 60.0)))
