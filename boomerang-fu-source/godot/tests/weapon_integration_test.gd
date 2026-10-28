extends SceneTree
var checks: Array[Dictionary] = []

func _initialize() -> void:
	call_deferred("run")

func check(label: String, condition: bool, details: Dictionary = {}) -> void:
	checks.append({"name": label, "passed": condition, "details": details})

func run() -> void:
	var world := OriginalCollisionWorld.new()
	var actor := OriginalActorMotion.new()
	actor.world = world
	var weapon := OriginalPlayerWeapons.new()
	weapon.initialize(actor, world)
	weapon.process_input(false, true, false, actor.fixed_delta, 0.0, actor.position, actor.rotation_z)
	check("tap creates one disc and consumes inventory", weapon.held_discs == 0 and weapon.owned_discs.size() == 1)
	var disc := weapon.owned_discs[0]
	check("owner collision guarded at spawn", not disc.can_collide_with_owner and not weapon.collect(disc))
	var steps := 0
	var returned := false
	for step in 300:
		actor.fixed_step()
		disc.fixed_step()
		steps += 1
		if disc.position.distance_to(actor.position) <= 1.463 + disc.collider_radius and weapon.collect(disc):
			returned = true
			break
	check("outbound disc returns to owner and is reclaimed", returned and not disc.active and weapon.owned_discs.is_empty() and weapon.held_discs == 1, {"steps": steps})
	weapon.frame_step(0.2)
	weapon.frame_step(0.0)
	for step in 66:
		weapon.process_input(true, false, false, actor.fixed_delta, float(step) * actor.fixed_delta, actor.position, actor.rotation_z)
		weapon.fixed_step()
	check("held throw charges and keeps ten direction samples", actor.is_aiming and weapon.aim_power == 1.0 and weapon.rotation_queue.size() == 10)
	weapon.process_input(false, true, false, actor.fixed_delta, 2.0, actor.position, actor.rotation_z)
	var charged := weapon.owned_discs[0]
	check("charged release retains power and clears aiming", charged.aim_power == 1.0 and charged.speed_limit == 101.25 and not actor.is_aiming and weapon.aim_timer == 0.0)
	weapon.process_input(true, false, false, actor.fixed_delta, 2.1, actor.position, actor.rotation_z)
	check("empty hands retrieve an existing disc", actor.retrieve_charge > 0.0 and weapon.aim_timer == 0.0)
	var dashed := actor.dash()
	check("dash clears retrieval and blocks held input", dashed and actor.retrieve_charge == 0.0 and weapon.input_blocked)
	weapon.process_input(false, true, false, actor.fixed_delta, 2.2, actor.position, actor.rotation_z)
	check("release clears held-input block without spawning", not weapon.input_blocked and weapon.owned_discs.size() == 1)
	var bindings: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://assets/characters/weapon-bindings.json"))
	for character_name in ["Avocado", "Watermelon"]:
		var preview := OriginalCharacterPreview.new()
		preview.name = character_name
		root.add_child(preview)
		preview.configure("res://assets/characters/" + character_name + ".glb", "res://assets/characters/" + character_name + "-idle.json")
		preview.set_held_discs(bindings[character_name], 1)
		var right: Node3D = preview.targets[bindings[character_name].rightDisc].node
		var left: Node3D = preview.targets[bindings[character_name].leftDisc].node
		check(character_name + " one-disc visibility", right.visible and not left.visible)
		preview.set_held_discs(bindings[character_name], 0)
		check(character_name + " empty-hand visibility", not right.visible and not left.visible)
		preview.free()
	var disc_view := OriginalDiscPreview.new()
	root.add_child(disc_view)
	disc_view.initialize(Color.GREEN)
	disc_view.advance_disc(0.1, charged)
	check("original disc model and three clips bind", disc_view.missing_targets.is_empty() and disc_view.targets.has("Appearance__2079") and disc_view.model.position == Vector3.ZERO)
	disc_view.free()
	var passed := checks.all(func(item: Dictionary) -> bool: return item.passed)
	var report := {"passed": passed, "checks": checks, "native_unity_query_parity_verified": false, "frame_visual_parity_verified": false}
	var output := FileAccess.open("res://../evidence/godot-weapon-integration-check.json", FileAccess.WRITE)
	output.store_string(JSON.stringify(report, "\t") + "\n")
	output.close()
	print(JSON.stringify(report))
	quit(0 if passed else 1)
