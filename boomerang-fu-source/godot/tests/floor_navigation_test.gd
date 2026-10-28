extends SceneTree
var differences:Array=[]
var comparisons:=0
func _initialize()->void:
	var data:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/floor-navigation-native.json"))
	var maps:Dictionary={}
	for c:Dictionary in data.cases:
		if not maps.has(c.map):
			var nav:=OriginalNavigationWorld.new()
			nav.initialize(JSON.parse_string(FileAccess.get_file_as_string("res://assets/original/navigation/"+c.map+".json")))
			maps[c.map]=nav
		var nav:OriginalNavigationWorld=maps[c.map];nav.reset_grids()
		var center:=Vector2(c.point[0],c.point[1]);var half:=Vector2(c.extent[0],c.extent[1]);var polygon:=PackedVector2Array()
		for p:Vector2 in [Vector2(-half.x,-half.y),Vector2(-half.x,half.y),half,Vector2(half.x,-half.y)]:polygon.append(center+p.rotated(deg_to_rad(c.angle)))
		nav.tag_area(polygon)
		var update:Dictionary=nav.pending_updates[0]
		for expected:Dictionary in c.graphs:
			var grid:OriginalNavigation=nav.grids[int(expected.index)]
			preload("res://core/original_navigation_updates.gd").apply(grid,OriginalCollisionWorld.new(),update)
			var tagged:Dictionary={}
			for index in expected.tags:tagged[int(index)]=true
			for i in grid.tags.size():
				comparisons+=1
				if (grid.tags[i]==1)!=tagged.has(i) and differences.size()<30:differences.append({"map":c.map,"angle":c.angle,"graph":expected.index,"index":i,"point":[grid.positions[i].x,grid.positions[i].y,grid.positions[i].z],"actual":grid.tags[i],"expected":int(tagged.has(i))})
	var report:={"passed":differences.is_empty(),"cases":data.cases.size(),"comparisons":comparisons,"differences":differences}
	FileAccess.open("res://../evidence/godot-floor-navigation-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  "))
	print("BF_FLOOR_NAVIGATION "+JSON.stringify(report));quit(0 if report.passed else 1)
