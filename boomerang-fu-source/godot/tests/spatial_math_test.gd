extends SceneTree
func _initialize() -> void:
	var native := OS.get_cmdline_user_args().has("--native")
	var reference: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/disc-native-math3.json" if native else "res://../../../work/spatial-math-oracle.json"))
	var differences: Array = []
	for sample in reference.samples:
		var value := Vector2(sample.input[0], sample.input[1])
		var z := OriginalTimekeeper.f32(sample.input[2])
		var length := OriginalActorMotion.magnitude_xyz(value, z)
		var normal := OriginalActorMotion.normalized_xyz_xy(value, z)
		var reflected := OriginalActorMotion.reflect(Vector2(sample.incoming[0], sample.incoming[1]), normal)
		var expected_length := OriginalTimekeeper.f32(sample.length)
		var expected_normal := Vector2(sample.normalized_xy[0], sample.normalized_xy[1])
		var expected_reflection := Vector2(sample.reflected[0], sample.reflected[1])
		if length != expected_length or normal != expected_normal or reflected != expected_reflection:
			differences.append({"input": sample.input, "actual": [length, [normal.x, normal.y], [reflected.x, reflected.y]], "expected": [sample.length, sample.normalized_xy, sample.reflected]})
	var report := {"source_runtime": reference.get("runtime", "Legacy desktop CoreCLR fixture"), "original_player_execution": native, "strict_equal": differences.is_empty(), "cases": reference.samples.size(), "different_cases": differences.size(), "first_differences": differences.slice(0, 20), "actual_original_managed_assembly": true, "native_engine_execution": native, "scope": reference.scope}
	var file := FileAccess.open("res://../evidence/godot-native-spatial-math-check.json" if native else "res://../evidence/godot-spatial-math-check.json", FileAccess.WRITE)
	file.store_string(JSON.stringify(report, "\t") + "\n")
	file.close()
	print(JSON.stringify(report))
	quit(0 if differences.is_empty() else 1)
