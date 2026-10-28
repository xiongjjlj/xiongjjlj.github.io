extends SceneTree
func _initialize() -> void:
	var native := OS.get_cmdline_user_args().has("--native")
	var reference: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/actor-native-melee.json" if native else "res://../../../work/melee-motion-oracle.json"))
	var differences: Array = []
	var count := 0
	var failed_frames := 0
	var maximum_error := 0.0
	for scenario in reference.cases:
		var actor := OriginalActorMotion.new()
		var weapon := OriginalPlayerWeapons.new()
		weapon.initialize(actor, OriginalCollisionWorld.new())
		weapon.held_discs = scenario.held_discs
		weapon.attacks_left = scenario.held_discs
		for index in scenario.frames.size():
			var frame: Dictionary = scenario.frames[index]
			actor.input_move = Vector2(frame.input[0], frame.input[1])
			actor.time_scaler = OriginalTimekeeper.f32(frame.scale)
			actor.is_slimed = frame.slimed
			actor.on_wet_floor = frame.wet
			actor.is_aiming = frame.aim
			if frame.push: actor.start_pushback(Vector2(10, -4), 0.2, 0.4)
			var accepted: bool = weapon.try_attack() if frame.attack else false
			var dash_accepted: bool = actor.dash() if frame.dash else false
			if frame.stopped: actor.stop_lunge()
			actor.fixed_step()
			actor.frame_step(actor.fixed_delta)
			weapon.frame_step(actor.fixed_delta)
			var actual := actor.snapshot()
			actual.merge({"lunge_timer": actor.lunge_timer, "lunge_cooldown": actor.lunge_cooldown_timer,
				"melee_timer": actor.melee_timer, "attack_cooldown": weapon.attack_cooldown, "lunging": actor.is_lunging,
				"attacking": actor.is_attacking, "cooling_attack": weapon.cooling_attack, "attacks_left": weapon.attacks_left,
				"can_rotate": actor.can_rotate})
			var expected: Dictionary = frame.state.duplicate()
			expected.merge({"accepted": frame.accepted, "dash_accepted": frame.dash_accepted})
			actual.merge({"accepted": accepted, "dash_accepted": dash_accepted})
			var changed := false
			for field in expected:
				var want: Variant = expected[field]
				var got: Variant = actual[field]
				var error := 0.0
				if want is Array:
					for component in want.size(): error = maxf(error, absf(got[component] - OriginalTimekeeper.f32(want[component])))
				elif want is bool: error = 0.0 if want == got else 1.0
				else: error = absf(got - OriginalTimekeeper.f32(want))
				if error != 0.0:
					changed = true
					maximum_error = maxf(maximum_error, error)
					if differences.size() < 24: differences.append({"scenario": scenario.name, "frame": index, "field": field, "expected": want, "actual": got, "error": error})
			if changed: failed_frames += 1
			count += 1
	var report := {"source_runtime": reference.get("runtime", "Legacy desktop CoreCLR fixture"), "original_player_execution": native, "strict_equal": failed_frames == 0, "scenarios": reference.cases.size(), "frames": count, "different_frames": failed_frames,
		"maximum_absolute_difference": maximum_error, "first_differences": differences, "native_unity_parity_verified": false,
		"scope": "Armed melee/lunge arithmetic against extracted original C# methods; engine queries and Animator replaced"}
	var file := FileAccess.open("res://../evidence/godot-native-melee-motion-check.json" if native else "res://../evidence/godot-melee-motion-check.json", FileAccess.WRITE)
	file.store_string(JSON.stringify(report, "\t") + "\n")
	file.close()
	print(JSON.stringify(report))
	quit(0 if failed_frames == 0 else 1)
