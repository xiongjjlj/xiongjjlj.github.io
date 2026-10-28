extends SceneTree
func _initialize() -> void:
	call_deferred("run")
func face(scene: Node, index: int, direction: Vector2) -> void:
	scene.actors[index].last_input_move = direction
	scene.actors[index].last_rotation = direction
	scene.actors[index].rotation_z = atan2(direction.y, direction.x) - PI / 2.0
	scene.previous_rotations[index] = scene.actors[index].rotation_z
func run() -> void:
	var scene = load("res://main.tscn").instantiate()
	root.add_child(scene)
	scene.set_process(false)
	var baseline := get_node_count()
	face(scene, 0, Vector2.RIGHT)
	scene.actors[1].position.x = 0.5
	scene.previous_positions[1] = scene.actors[1].position
	scene.weapons[0].held_discs = 0
	scene.weapons[0].attacks_left = 0
	scene.pending_attack[0] = true
	var stunned := false
	var dropped := false
	var attack_clip := false
	for frame in 240:
		scene._process(1.0 / 60.0)
		await process_frame
		stunned = stunned or scene.actors[1].is_stunned
		for item in scene.flying_discs: dropped = dropped or item.motion.started_dropped
		attack_clip = attack_clip or scene.characters[0].clip.name == "Disarm"
	var disarm_alive: bool = scene.actors[0].state == OriginalActorMotion.State.ALIVE and scene.actors[1].state == OriginalActorMotion.State.ALIVE
	var disarm_inventory := [scene.weapons[0].held_discs, scene.weapons[1].held_discs]
	scene.reset_motions(true)
	await process_frame
	face(scene, 0, Vector2.RIGHT)
	face(scene, 1, Vector2.LEFT)
	scene.pending_throw_release = [true, true]
	var clash := false
	var original_movement := false
	var caught := false
	for frame in 240:
		scene._process(1.0 / 60.0)
		await process_frame
		for item in scene.flying_discs:
			if item.motion.was_just_deflected:
				clash = true
				original_movement = original_movement or item.motion.movement == OriginalDiscMotion.Movement.THROWING
		if clash and scene.weapons[0].held_discs == 1 and scene.weapons[1].held_discs == 1: caught = true
	var clash_alive: bool = scene.actors[0].state == OriginalActorMotion.State.ALIVE and scene.actors[1].state == OriginalActorMotion.State.ALIVE
	scene.reset_motions(true)
	await process_frame
	var final_nodes := get_node_count()
	var report := {"passed": stunned and dropped and attack_clip and disarm_alive and clash and original_movement and caught and clash_alive and final_nodes == baseline,
		"disarm_stun": stunned, "dropped_disc": dropped, "original_disarm_clip": attack_clip, "both_alive_after_disarm": disarm_alive,
		"inventory_after_disarm_sequence": disarm_inventory, "disc_clash": clash, "clash_keeps_throwing_state": original_movement,
		"both_discs_caught_after_clash": caught, "both_alive_after_clash": clash_alive, "nodes_before": baseline, "nodes_after": final_nodes,
		"native_frame_parity_verified": false, "scope": "Actual original map scene, empty-hand strike/drop and simultaneous enemy-disc collision, source animation binding and round reset"}
	var file := FileAccess.open("res://../evidence/godot-disarm-clash-scene-check.json", FileAccess.WRITE)
	file.store_string(JSON.stringify(report, "\t") + "\n")
	file.close()
	print(JSON.stringify(report))
	scene.free()
	quit(0 if report.passed else 1)
