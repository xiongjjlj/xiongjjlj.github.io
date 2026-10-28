extends SceneTree

func _initialize() -> void:
	call_deferred("run")

func run() -> void:
	var scene = load("res://main.tscn").instantiate()
	root.add_child(scene)
	scene.set_process(false)
	var baseline := get_node_count()
	var rounds: Array = []
	var passed := true
	for round_index in 14:
		var actor: OriginalActorMotion = scene.actors[0]
		actor.last_input_move = Vector2.RIGHT
		actor.last_rotation = Vector2.RIGHT
		actor.rotation_z = -PI / 2.0
		scene.previous_rotations[0] = actor.rotation_z
		scene.weapons[0].process_input(false, true, false, actor.fixed_delta, 0.0, actor.position, actor.rotation_z)
		scene.spawn_discs(0, actor.rotation_z)
		var frames := 0
		while scene.round_state.simulation_running() and frames < 600:
			scene._process(1.0 / 60.0)
			frames += 1
		var ok: bool = scene.actors[1].state == OriginalActorMotion.State.DEAD and scene.round_state.scores.net_score(0) == round_index + 1 and scene.round_state.scores.net_score(1) == 0
		var clip_bound: bool = scene.characters[1].clip.name == "DeathSliced" and scene.characters[1].missing_targets.is_empty()
		passed = passed and ok and clip_bound
		rounds.append({"round": round_index + 1, "frames": frames, "passed": ok and clip_bound, "phase": scene.round_state.phase, "points": scene.round_state.scores.net_score(0)})
		if not passed: break
		if round_index < 13: scene.next_round()
		await process_frame
	var won: bool = scene.round_state.phase == OriginalFfaRound.Phase.MATCH_END and scene.round_state.scores.winners == [0]
	passed = passed and won
	scene.reset_motions(true)
	await process_frame
	var final_nodes := get_node_count()
	passed = passed and final_nodes == baseline and scene.round_state.scores.net_score(0) == 0
	var report := {"passed": passed, "rounds": rounds, "reached_default_medium_goal": won, "nodes_before": baseline, "nodes_after_restart": final_nodes, "native_frame_parity_verified": false, "scope": "Actual scene, source map queries, flight, damage, source death clip, score persistence, 14-round preview victory and restart; excludes melee/powers/complete original presentation"}
	var output := FileAccess.open("res://../evidence/godot-duel-integration-check.json", FileAccess.WRITE)
	output.store_string(JSON.stringify(report, "\t") + "\n")
	output.close()
	print(JSON.stringify(report))
	scene.free()
	quit(0 if passed else 1)
