extends SceneTree

func _initialize() -> void:
	var native := OS.get_cmdline_user_args().has("--native")
	var path := "res://../evidence/actor-native-actor.json" if native else "res://../../../work/actor-motion-oracle.json"
	var reference: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(path))
	var mismatches: Array = []
	var frame_count := 0
	var different_frames := 0
	var max_error := 0.0
	for scenario in reference.cases:
		var actor := OriginalActorMotion.new()
		var frame_index := 0
		for frame in scenario.frames:
			actor.input_move = Vector2(frame.input[0], frame.input[1])
			actor.on_wet_floor = frame.wet
			actor.is_frozen = frame.frozen
			actor.is_aiming = frame.aim
			actor.is_disarmed = frame.disarmed
			actor.move_faster = frame.faster
			actor.time_scaler = OriginalTimekeeper.f32(frame.personal_scale)
			var accepted: bool = actor.dash() if frame.dash else false
			if frame.push: actor.start_pushback(Vector2(20,-6), 0.2, 0.4)
			if frame.contact != null:
				var other := OriginalActorMotion.new()
				other.velocity_stored = Vector2(frame.contact.velocity[0], frame.contact.velocity[1])
				other.is_dashing = frame.contact.dashing
				other.time_scaler = frame.contact.personal_scale
				actor.respond_to_actor(Vector2(frame.contact.normal[0], frame.contact.normal[1]), other)
			actor.fixed_step()
			actor.frame_step(actor.fixed_delta)
			var actual := actor.snapshot()
			var changed := false
			if accepted != frame.dash_accepted:
				mismatches.append({"scenario": scenario.name, "frame": frame_index, "field": "dash_accepted"})
				changed = true
			for field in frame.state:
				var expected: Variant = frame.state[field]
				var value: Variant = actual[field]
				var error := 0.0
				if expected is Array:
					for i in expected.size(): error = maxf(error, absf(value[i] - OriginalTimekeeper.f32(expected[i])))
				elif expected is bool:
					error = 0.0 if expected == value else 1.0
				else:
					error = absf(value - OriginalTimekeeper.f32(expected))
				if error > 0.0:
					changed = true
					max_error = maxf(max_error, error)
					if mismatches.size() < 20:
						mismatches.append({"scenario": scenario.name, "frame": frame_index, "field": field,
							"expected": expected, "actual": value, "error": error})
			if changed: different_frames += 1
			frame_index += 1
			frame_count += 1
	var report := {"source_runtime": reference.get("runtime", "Legacy desktop CoreCLR fixture"), "original_player_execution": native, "strict_equal": different_frames == 0, "frames": frame_count,
		"scenarios": reference.cases.size(), "compared_fields": reference.cases[0].frames[0].state.keys(),
		"different_frames": different_frames, "maximum_absolute_difference": max_error,
		"first_differences": mismatches, "native_unity_parity_verified": false,
		"scope": "Movement/contact arithmetic against extracted original C# methods; engine substitutes and injected contacts"}
	var file := FileAccess.open("res://../evidence/godot-native-actor-motion-check.json" if native else "res://../evidence/godot-actor-motion-check.json", FileAccess.WRITE)
	file.store_string(JSON.stringify(report, "\t") + "\n")
	file.close()
	print(JSON.stringify(report))
	quit(0 if different_frames == 0 else 1)
