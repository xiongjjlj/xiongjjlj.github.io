extends SceneTree
func _initialize() -> void:
	var native := OS.get_cmdline_user_args().has("--native")
	var reference: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/disc-native-clash.json" if native else "res://../../../work/disc-clash-oracle.json"))
	var differences: Array = []
	var different := 0
	var count := 0
	for scenario in reference.cases:
		var owner := OriginalActorMotion.new()
		owner.position = Vector2(-2, 0)
		owner.retrieve_charge = scenario.retrieve
		var other := OriginalDiscMotion.new()
		other.owner_actor = owner if scenario.same_owner else OriginalActorMotion.new()
		if not scenario.same_owner: other.owner_actor.position = Vector2(6, 2)
		other.position = Vector2(2, 1)
		other.stored = Vector2(-25, 4)
		var disc := OriginalDiscMotion.new()
		disc.owner_actor = owner
		disc.throw_direction = Vector2.RIGHT
		disc.time_scaler = OriginalTimekeeper.f32(scenario.scale)
		disc.can_home = false
		disc.initialize(scenario.dropped)
		if scenario.public_disc: disc.owner_actor = null
		for index in scenario.frames.size():
			var frame: Dictionary = scenario.frames[index]
			if scenario.public_disc: disc.can_return = false
			disc.fixed_step()
			if frame.contact:
				disc.collide_with_disc(other, Vector2(frame.normal[0], frame.normal[1]))
				if frame.duplicate: disc.collide_with_disc(other, Vector2(frame.normal[0], frame.normal[1]))
			disc.frame_step(disc.fixed_delta)
			var actual := disc.snapshot()
			actual.merge({"deflected": disc.was_just_deflected, "deflected_timer": disc.deflected_timer,
				"can_home": disc.can_home, "can_control": disc.can_be_controlled, "owner_protected": owner.deflected_discs.has(other.get_instance_id())})
			var changed := false
			for field in frame.state:
				var expected: Variant = frame.state[field]
				var value: Variant = actual[field]
				var error := 0.0
				if expected is Array:
					for component in expected.size(): error = maxf(error, absf(value[component] - OriginalTimekeeper.f32(expected[component])))
				elif expected is bool: error = 0.0 if expected == value else 1.0
				else: error = absf(value - OriginalTimekeeper.f32(expected))
				if error > 0.0:
					changed = true
					if differences.size() < 20: differences.append({"scenario": scenario.name, "frame": index, "field": field, "expected": expected, "actual": value, "error": error})
			if changed: different += 1
			count += 1
	var report := {"source_runtime": reference.get("runtime", "Legacy desktop CoreCLR fixture"), "original_player_execution": native, "strict_equal": different == 0, "scenarios": reference.cases.size(), "steps": count, "different_steps": different, "first_differences": differences,
		"native_parity_verified": false, "scope": "Original collision-response and flight arithmetic with injected contacts; excludes native spatial queries and protection coroutine scheduling"}
	var file := FileAccess.open("res://../evidence/godot-native-disc-clash-motion-check.json" if native else "res://../evidence/godot-disc-clash-motion-check.json", FileAccess.WRITE)
	file.store_string(JSON.stringify(report, "\t") + "\n")
	file.close()
	print(JSON.stringify(report))
	quit(0 if different == 0 else 1)
