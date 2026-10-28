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
	face(scene, 0, Vector2.RIGHT)
	scene.actors[1].position.x = 0.5
	scene.previous_positions[1] = scene.actors[1].position
	scene.pending_attack[0] = true
	var frames := 0
	while scene.round_state.simulation_running() and frames < 300:
		scene._process(1.0 / 60.0)
		frames += 1
	var melee_kill: bool = scene.weapons[0].action_serial == 1 and scene.actors[1].state == OriginalActorMotion.State.DEAD and scene.round_state.scores.net_score(0) == 1
	scene.reset_motions(true)
	await process_frame
	face(scene, 0, Vector2.RIGHT)
	face(scene, 1, Vector2.LEFT)
	scene.pending_attack[0] = true
	scene.pending_throw_release[1] = true
	var parry := false
	var freeze_seen := false
	var both_alive := false
	var clips_bound := true
	var parry_frames := 0
	for index in 45:
		scene._process(1.0 / 60.0)
		parry_frames += 1
		if scene.clock.freeze_frame: freeze_seen = true
		for item in scene.flying_discs:
			if item.motion.was_just_deflected: parry = true
		clips_bound = clips_bound and scene.characters[0].missing_targets.is_empty()
		if parry and freeze_seen:
			both_alive = scene.actors[0].state == OriginalActorMotion.State.ALIVE and scene.actors[1].state == OriginalActorMotion.State.ALIVE
			break
	var report := {"passed": melee_kill and parry and freeze_seen and both_alive and clips_bound,
		"melee_kill": melee_kill, "melee_round_frames": frames, "disc_deflected": parry, "freeze_seen": freeze_seen,
		"both_alive_at_parry": both_alive, "parry_frames": parry_frames, "original_attack_clips_bound": clips_bound,
		"native_frame_parity_verified": false, "scope": "Actual map scene, one-frame keyboard latch, melee score/death, original attack clips and incoming enemy disc parry"}
	var file := FileAccess.open("res://../evidence/godot-melee-scene-check.json", FileAccess.WRITE)
	file.store_string(JSON.stringify(report, "\t") + "\n")
	file.close()
	print(JSON.stringify(report))
	scene.free()
	quit(0 if report.passed else 1)
