extends SceneTree
func _initialize() -> void:
	var source: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/unity-native-probe.json"))
	if not source.has("rays") or source.rays.size() != 12 or source.rotations.size() != 16 or not source.error.is_empty():
		push_error("Native fixture is missing measurements")
		quit(1)
		return
	var differences: Array = []
	var arbitrary_rotation_differences: Array = []
	var circle_differences: Array = []
	for sample in source.rotations:
		var actual := OriginalDiscMotion.quaternion_turn(Vector2(sample.input[0], sample.input[1]), sample.angle)
		var expected := Vector2(sample.output[0], sample.output[1])
		if actual != expected:
			var detail := {"kind": "rotation", "sample": sample, "actual": [actual.x, actual.y]}
			if sample.angle in [-90.0, -45.0, 90.0]: differences.append(detail)
			else: arbitrary_rotation_differences.append(detail)
	var world := OriginalCollisionWorld.new()
	var y := OriginalTimekeeper.f32(2.05)
	var h := OriginalTimekeeper.f32(0.01)
	var vertices := PackedVector2Array([Vector2(-0.5, y-h), Vector2(0.5, y-h), Vector2(0.5, y+h), Vector2(-0.5, y+h)])
	world.shapes.append({"id": 1, "kind": "polygon", "vertices": vertices, "edge_radius": 0.0, "layer": 256, "actor": false, "bounds": Rect2(-0.5,y-h,1.0,h*2.0)})
	for sample in source.rays:
		var radius := 0.0 if sample.kind == "ray" else 0.1
		var hit := world.circle_cast(Vector2.ZERO, radius, Vector2(sample.direction[0],sample.direction[1]), sample.requestedDistance, 256)
		var actual_hit := not hit.is_empty()
		var actual_distance: float = hit.get("distance", 0.0)
		if actual_hit != sample.hit or actual_distance != OriginalTimekeeper.f32(sample.distance):
			var detail := {"kind": sample.kind, "name": sample.name, "expected_hit": sample.hit, "actual_hit": actual_hit, "expected_distance": OriginalTimekeeper.f32(sample.distance), "actual_distance": actual_distance}
			if sample.kind == "ray": differences.append(detail)
			else: circle_differences.append(detail)
	var combat := OriginalCombatWorld.new()
	combat.world = world
	if not combat.line_blocked(Vector2.ZERO,Vector2(0,2),0.0,-0.75) or combat.line_blocked(Vector2.ZERO,Vector2(0,2)):
		differences.append({"kind": "height_ray", "message": "3D source distance must extend the XY ray to the measured native hit"})
	var report := {"unity": source.unity, "cases":22, "strict_equal":differences.is_empty() and arbitrary_rotation_differences.is_empty(), "rotation_cases": 16, "gameplay_rotation_cases": 6, "ray_cases": 6,
		"gameplay_rotation_and_ray_samples_equal": differences.is_empty(), "differences": differences,
		"arbitrary_rotation_differences": arbitrary_rotation_differences,
		"circle_cases": 6, "circle_differences": circle_differences,
		"full_native_query_parity_verified": false,
		"scope": "Actual original Windows UnityPlayer 16 rotation outputs and six axis-aligned ray distances; circle-cast differences remain. Broader 512-angle diagnostic is in multidisc_lifecycle_test."}
	FileAccess.open("res://../evidence/godot-native-probe-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print(JSON.stringify(report))
	quit(0 if differences.is_empty() and arbitrary_rotation_differences.is_empty() else 1)
