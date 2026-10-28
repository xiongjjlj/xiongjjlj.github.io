extends SceneTree

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	var native := args.has("--native")
	var path := "res://../evidence/timekeeper-native-clock.json" if native else (args[0] if not args.is_empty() else "res://../../../work/timekeeper-oracle.json")
	var trace: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(path))
	var clock: OriginalTimekeeper
	var scenario := ""
	var differences: Array = []
	var max_error := 0.0
	for frame in trace.frames:
		if frame.scenario != scenario:
			scenario = frame.scenario
			clock = OriginalTimekeeper.new()
		for action in frame.actions:
			var v: Array = action.values
			match action.op:
				"slow": clock.slow_mo_effect(v[0], v[1], v[2])
				"freeze": clock.freeze(v[0])
				"pause": clock.pause_time()
				"unpause": clock.unpause_time()
				"warp": clock.slow_mo_time_warp(v[0], v[1], v[2], v[3])
				"disable":
					clock.enable_slow_mo = false
					clock.freeze_frames_enabled = false
				"end_match": clock.end_match()
				"set": clock.set_time_scale(v[0])
				"reset": clock.reset_time_scale()
		clock.update(frame.dt, int(frame.state))
		var actual := clock.snapshot()
		for key in frame.expected:
			var expected: Variant = frame.expected[key]
			if expected is float:
				# Reconstitute the original binary32 value from shortest JSON decimal.
				expected = OriginalTimekeeper.f32(expected)
				var error: float = absf(actual[key] - expected)
				max_error = maxf(max_error, error)
			if actual[key] != expected:
				differences.append({"scenario": scenario, "frame": frame.frame,
					"field": key, "expected": expected, "actual": actual[key]})
	var report := {"source_runtime": trace.get("runtime", "Legacy desktop CoreCLR fixture"), "original_player_execution": native, "scope": trace.scope, "frames": trace.frames.size(),
		"strict_equal": differences.is_empty(), "differences": differences.size(),
		"maximum_absolute_error": max_error, "first_differences": differences.slice(0, 20),
		"native_game_or_browser_parity_proven": false}
	var report_path := "res://../evidence/godot-native-timekeeper-check.json" if native else (args[1] if args.size() > 1 else "res://../evidence/godot-timekeeper-check.json")
	var file := FileAccess.open(report_path, FileAccess.WRITE)
	file.store_string(JSON.stringify(report, "  ") + "\n")
	print(JSON.stringify(report))
	quit(0 if differences.is_empty() else 1)
