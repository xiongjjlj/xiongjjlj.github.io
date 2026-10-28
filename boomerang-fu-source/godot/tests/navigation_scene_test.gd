extends SceneTree
var checks: Array = []
func check(label: String, passed: bool, details = null) -> void: checks.append({"name":label,"passed":passed,"details":details})
func _initialize() -> void: call_deferred("run")
func run() -> void:
	var game = load("res://main.tscn").instantiate();root.add_child(game);game.set_process(false)
	var nav: OriginalNavigationWorld = game.decoys.navigation
	var key := str(game.active_scene.file).get_file().get_basename()
	var fixture: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/navigation-selection-native.json"))
	var differences: Array = [];var maximum_error := 0.0;var queries := 0
	for map: Dictionary in fixture.maps:
		if str(map.key)!=key:continue
		for native: Dictionary in map.phases[0].floors:
			var floor: Dictionary = game.level_behaviors.moving_floors.floors[int(native.id)]
			var matrix := OriginalMovingFloors.source_matrix(floor.initial_global)
			for i in 16: maximum_error=maxf(maximum_error,absf(matrix[i]-OriginalTimekeeper.f32(native.initial[i])))
		for sample: Dictionary in map.phases[0].queries:
			queries+=1
			var start := Vector3(sample.point[0],sample.point[1],sample.point[2]);var finish := Vector3(sample.target[0],sample.target[1],sample.target[2])
			var selected := nav.best_graph(start,finish)
			var mask: int = 0 if selected.is_empty() else 1<<int(selected.index)
			if mask!=int(sample.mask) and differences.size()<12:differences.append({"actual":mask,"expected":sample.mask,"start":sample.point,"finish":sample.target})
	check("imported source platform transforms",maximum_error<0.00001,maximum_error)
	check("original graph selection in imported scene",queries>0 and differences.is_empty(),{"queries":queries,"differences":differences})
	var report := {"passed":checks.all(func(c):return c.passed),"map":key,"checks":checks}
	FileAccess.open("res://../evidence/godot-navigation-scene-"+key+"-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  "))
	print("BF_NAVIGATION_SCENE "+JSON.stringify(report));game.queue_free();await process_frame;quit(0 if report.passed else 1)
