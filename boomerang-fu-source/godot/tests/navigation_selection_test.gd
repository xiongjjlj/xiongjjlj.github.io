extends SceneTree
var differences: Array = []
var queries := 0
var transform_points := 0
var maximum_error := 0.0
func vector(row: Array) -> Vector3: return Vector3(row[0],row[1],row[2])
func transform(row: Array) -> Transform3D:
	return Transform3D(Basis(Vector3(row[0],row[4],-row[8]),Vector3(row[1],row[5],-row[9]),Vector3(-row[2],-row[6],row[10])),Vector3(row[3],row[7],-row[11]))
func compare_vector(actual: Vector3, expected: Array, label: String) -> void:
	transform_points+=1
	var error: float = (actual-vector(expected)).length()
	maximum_error=maxf(maximum_error,error)
	if error>0.00005 and differences.size()<24:differences.append({"path":label,"actual":[actual.x,actual.y,actual.z],"expected":expected,"error":error})
func _initialize() -> void: call_deferred("run")
func run() -> void:
	var fixture: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/navigation-selection-native.json"))
	for map: Dictionary in fixture.maps:
		var nav := OriginalNavigationWorld.new()
		nav.initialize(JSON.parse_string(FileAccess.get_file_as_string("res://assets/original/navigation/"+str(map.key)+".json")))
		var floors := OriginalMovingFloors.new(); nav.bind_moving_floors(floors)
		for phase: Dictionary in map.phases:
			for definition: Dictionary in phase.floors:
				var node := Node3D.new(); root.add_child(node);node.global_transform=transform(definition.current)
				var floor := {"node":node,"initial_global":transform(definition.initial),"graph_mask":int(definition.mask),"reactive":definition.reactive,"use_rotations":definition.rotating}
				floors.floors[int(definition.id)]=floor
				for sample: Dictionary in definition.points:
					var label: String = "%s/phase%d/floor%d"%[map.key,phase.phase,definition.id]
					compare_vector(OriginalMovingFloors.path_point(floor,vector(sample.point)),sample.forward,label+"/forward")
					compare_vector(OriginalMovingFloors.path_point(floor,vector(sample.point),true),sample.inverse,label+"/inverse")
			for sample: Dictionary in phase.queries:
				queries+=1
				var selected := nav.best_graph(vector(sample.point),vector(sample.target))
				var mask: int = 0 if selected.is_empty() else 1<<int(selected.index)
				if mask!=int(sample.mask) and differences.size()<24:differences.append({"map":map.key,"phase":phase.phase,"point":sample.point,"target":sample.target,"actual":mask,"expected":sample.mask})
			for floor: Dictionary in floors.floors.values():floor.node.free()
			floors.floors.clear()
	var report := {"passed":differences.is_empty(),"maps":fixture.maps.size(),"queries":queries,"transform_points":transform_points,"maximum_transform_error":maximum_error,"tolerance":0.00005,"differences":differences,"scope":"Original native platform transform methods and unchanged graph selection, sampled initial/translated/rotated poses; runtime obstacle rescan is separate."}
	FileAccess.open("res://../evidence/godot-navigation-selection-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  "))
	print("BF_NAVIGATION_SELECTION "+JSON.stringify(report));quit(0 if report.passed else 1)
