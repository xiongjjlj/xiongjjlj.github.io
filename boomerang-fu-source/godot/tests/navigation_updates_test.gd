extends SceneTree
const U=preload("res://core/original_navigation_updates.gd")
var differences:Array=[]
var comparisons:=0
var path_count:=0
var maximum_error:=0.0
func mismatch(label:String,actual:Variant,expected:Variant)->void:
	if differences.size()<30:differences.append({"label":label,"actual":actual,"expected":expected})
func compare(actual:Variant,expected:Variant,label:String)->void:
	if expected is Array:
		var count:int=3 if actual is Vector3 else actual.size()
		if count!=expected.size():mismatch(label+"/length",count,expected.size())
		for i in mini(count,expected.size()):compare(actual[i],expected[i],label+"/"+str(i))
	else:
		comparisons+=1
		if actual is float:
			var error:=absf(actual-OriginalTimekeeper.f32(expected));maximum_error=maxf(maximum_error,error)
			if error>0:mismatch(label,actual,expected)
		elif actual!=expected:mismatch(label,actual,expected)
func check_nodes(grid:OriginalNavigation,rows:Array,label:String)->void:
	for row:Array in rows:
		var i:=int(row[0]);var p:Vector3=grid.integer_positions[i]
		var actual:=[i,int(p.x),int(p.y),int(p.z),grid.walkable[i],grid.base_penalties[i],grid.tags[i],grid.connections[i]]
		compare(actual,row,label+"/node"+str(i))
func shapes(data:Array)->OriginalCollisionWorld:
	var world:=OriginalCollisionWorld.new()
	for i in data.size():
		var source:Dictionary=data[i]
		if not source.active:continue
		var pos:=Vector2(source.position[0],source.position[1])
		var shape:={"id":i,"layer":1<<int(source.layer),"actor":false,"kind":source.kind,"edge_radius":0.0}
		if source.kind=="circle":shape.center=pos;shape.radius=OriginalTimekeeper.f32(source.radius)
		else:
			shape.kind="polygon"
			var vertices:=PackedVector2Array();var half:=Vector2(source.size[0],source.size[1])*0.5
			for point:Vector2 in [Vector2(-half.x,-half.y),Vector2(half.x,-half.y),half,Vector2(-half.x,half.y)]:vertices.append(pos+point.rotated(deg_to_rad(source.rotation)))
			shape.vertices=vertices
		world.shapes.append(shape)
	return world
func _initialize()->void:
	var suffix:=""
	for argument in OS.get_cmdline_user_args():
		if argument.begins_with("--fixture-suffix="):suffix=argument.trim_prefix("--fixture-suffix=")
		elif argument=="--map=level48":suffix="-level48"
	var fixture:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/navigation-updates-native"+suffix+".json"))
	var world:=OriginalNavigationWorld.new();world.initialize(JSON.parse_string(FileAccess.get_file_as_string("res://assets/original/navigation/"+str(fixture.map)+".json")))
	var grid:OriginalNavigation=world.grids[0]
	check_nodes(grid,fixture.initial,"initial")
	var phases:Array=[]
	for phase:Dictionary in fixture.phases:
		var result:=U.apply(grid,shapes(phase.shapes),phase)
		check_nodes(grid,phase.nodes,phase.name)
		for i in phase.paths.size():
			var expected:Dictionary=phase.paths[i];path_count+=1
			var actual:=grid.find_path(U.vector(expected.start),U.vector(expected.end))
			var label:String=phase.name+"/path"+str(i)
			compare(actual.error=="",expected.error=="",label+"/success")
			if actual.error!="" or expected.error!="":continue
			for field in ["node_ids","edge","smoothed"]:compare(actual[field],expected[field],label+"/"+field)
		phases.append({"name":phase.name,"updated":result.updated})
	var report:={"passed":differences.is_empty(),"comparisons":comparisons,"paths":path_count,"maximum_error":maximum_error,"differences":differences,"phases":phases,"scope":"Original Physics2D and GridGraph.UpdateArea with synthetic changing geometry. Scheduling and real scene callbacks are separate."}
	FileAccess.open("res://../evidence/godot-navigation-updates"+suffix+"-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  "))
	print("BF_NAVIGATION_UPDATES "+JSON.stringify(report));quit(0 if report.passed else 1)
