extends SceneTree
const C = preload("res://core/original_timekeeper.gd")
func _initialize() -> void:
	var fixture: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/ground-native-queries.json"))
	var queries = load("res://core/original_ground_queries.gd").new()
	queries.world = OriginalCollisionWorld.new()
	queries.floor_mask = int(fixture.floor_mask)
	queries.water_mask = int(fixture.water_mask)
	queries.cloud_level = true
	for item in fixture.shapes:
		var shape: Dictionary = item.duplicate()
		shape.layer = int(item.layer)
		if item.kind == "circle": shape.center = Vector2(item.center[0],item.center[1])
		else:
			shape.vertices = PackedVector2Array()
			for point in item.vertices: shape.vertices.append(Vector2(point[0],point[1]))
		queries.world.shapes.append(shape)
	var differences: Array = []
	var different_samples := 0
	for item in fixture.samples:
		var point := Vector2(item.point[0],item.point[1])
		var radius := C.f32(item.radius)
		var actual := {"contacts":queries.hazard_contacts(point,radius),"without_obstacles":queries.hazard_contacts(point,radius,false),"solid":queries.solid_ground(point,radius),"point_hazard":not queries.point_on_layer(point,queries.floor_mask),"over_hazard":queries.over_hazard(point,radius),"off_cliff":queries.off_cliff(point)}
		var any_difference := false
		for key in actual:
			if actual[key] != item[key]:
				any_difference = true
				if differences.size()<20: differences.append({"point":point,"radius":radius,"field":key,"actual":actual[key],"expected":item[key]})
		if any_difference: different_samples += 1
	var report := {"passed":different_samples==0,"samples":fixture.samples.size(),"different_samples":different_samples,"first_differences":differences,"actual_unity_physics2d":true,"scope":"Supplied box/circle floors, wall and water; source nine-point hazard predicates including exact boundaries; no full level collision parity","full_game_parity_verified":false}
	FileAccess.open("res://../evidence/godot-native-ground-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print(JSON.stringify(report))
	quit(0 if different_samples==0 else 1)
