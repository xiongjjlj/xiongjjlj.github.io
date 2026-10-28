extends SceneTree
var differences: Array = []
var path_cases := 0
var maximum_error := 0.0
func vector(row: Array) -> Vector3: return Vector3(row[0],row[1],row[2])
func mismatch(path: String, actual: Variant, expected: Variant) -> void:
	if differences.size()<20: differences.append({"path":path,"actual":actual,"expected":expected})
func compare(actual: Variant, expected: Variant, path: String) -> void:
	if expected is Array:
		var count: int = 3 if actual is Vector3 else actual.size()
		if count != expected.size(): mismatch(path+"/length",count,expected.size())
		for i in mini(count,expected.size()): compare(actual[i],expected[i],path+"/"+str(i))
	elif expected is float or expected is int:
		var error := absf(float(actual)-OriginalTimekeeper.f32(expected))
		maximum_error=maxf(maximum_error,error)
		if error>0: mismatch(path,actual,expected)
	elif actual!=expected: mismatch(path,actual,expected)
func _initialize() -> void:
	var batch := 0
	for argument in OS.get_cmdline_user_args():
		if argument.begins_with("--batch="):batch=int(argument.trim_prefix("--batch="))
	var fixture: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/navigation-maps-native-batch%02d.json"%batch))
	for map: Dictionary in fixture.maps:
		var nav := OriginalNavigationWorld.new()
		nav.initialize(JSON.parse_string(FileAccess.get_file_as_string("res://assets/original/navigation/"+str(map.key)+".json")))
		compare(nav.grids.size(),map.graphs.size(),map.key+"/graphs")
		for graph: Dictionary in map.graphs:
			var grid: OriginalNavigation = nav.grids[int(graph.index)]
			for i in graph.paths.size():
				path_cases+=1
				var expected: Dictionary = graph.paths[i]
				var actual := grid.find_path(vector(expected.start),vector(expected.end))
				var label: String = "%s/graph%d/path%d"%[map.key,graph.index,i]
				compare(actual.error=="",expected.error=="",label+"/success")
				if actual.error!="" or expected.error!="":continue
				for field in ["node_ids","edge","smoothed"]:compare(actual[field],expected[field],label+"/"+field)
	var report := {"passed":differences.is_empty(),"batch":batch,"maps":fixture.maps.size(),"paths":path_cases,"maximum_error":maximum_error,"differences":differences,"scope":"Native Astar paths on per-map grids including platform graphs; no runtime collider rescan or callback-order claim."}
	FileAccess.open("res://../evidence/godot-navigation-maps-batch%02d-check.json"%batch,FileAccess.WRITE).store_string(JSON.stringify(report,"  "))
	print("BF_NAVIGATION_MAPS "+JSON.stringify(report));quit(0 if report.passed else 1)
