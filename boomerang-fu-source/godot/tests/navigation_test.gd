extends SceneTree
const N=preload("res://core/original_navigation.gd")
const C=preload("res://core/original_timekeeper.gd")
var differences:Array=[]
var maximum_error:Dictionary={}
var comparisons:=0
func vector(value:Array)->Vector3:return Vector3(value[0],value[1],value[2])
func compare(actual:Variant,expected:Variant,path:String,tolerance:=0.0)->void:
	comparisons+=1
	if expected is float or expected is int:
		var error:=absf(float(actual)-C.f32(expected))
		var category:=path.get_slice("/",0)
		maximum_error[category]=maxf(maximum_error.get(category,0.0),error)
		if error>tolerance and differences.size()<30:differences.append({"path":path,"actual":actual,"expected":expected,"error":error})
	elif expected is Array:
		var count:int=3 if actual is Vector3 else actual.size()
		if count!=expected.size():
			if differences.size()<30:differences.append({"path":path,"actual_size":count,"expected_size":expected.size()})
		for i in mini(count,expected.size()):compare(actual[i],expected[i],path+"/"+str(i),tolerance)
	elif actual!=expected and differences.size()<30:differences.append({"path":path,"actual":actual,"expected":expected})
func _initialize()->void:
	var nav:=N.new()
	nav.initialize(JSON.parse_string(FileAccess.get_file_as_string("res://assets/original/navigation-enclosed.json")))
	var fixture:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/navigation-native-paths.json"))
	for query in fixture.queries:
		var actual:=nav.nearest(vector(query.point))
		compare(actual.node,query.node,"nearest/node/"+str(query.point))
		compare(actual.point,query.clamped,"nearest_position/"+str(query.point))
		var forced:=nav.nearest(vector(query.point),-1,true,true)
		compare(forced.node,query.force_node,"force_node/"+str(query.point))
		compare(forced.point,query.force_clamped,"force_position/"+str(query.point))
		compare(nav.squared(nav.positions[forced.node]-vector(query.point))<=7.0,query.eligible,"graph_eligible/"+str(query.point))
	var total_searched:=0
	for i in fixture.paths.size():
		var test:Dictionary=fixture.paths[i]
		var actual:=nav.find_path(vector(test.start),vector(test.end))
		compare(actual.error,test.error,"path_error/"+str(i))
		if actual.error!="":continue
		for key in ["node_ids","start_node","end_node","edge","h_target","geometric_graph","geometric_node","special_node"]:compare(actual[key],test[key],key+"/"+str(i))
		for key in ["clamped_start","clamped_end","raw","snapped","trimmed","smoothed","total_length"]:compare(actual[key],test[key],key+"/"+str(i))
		total_searched+=nav.searches
	var report:Dictionary={"passed":differences.is_empty(),"strict_equal":differences.is_empty(),"comparisons":comparisons,"queries":fixture.queries.size(),"paths":fixture.paths.size(),"searched_nodes":total_searched,"maximum_error":maximum_error,"differences":differences,"scope":"Original Enclosed static traversal and all eight graphs for unconstrained endpoints: nearest, ABPath node selection, original four-way heap ties, endpoint/edge/smoothing modifiers. Dynamic graph updates and asynchronous scheduling remain pending."}
	FileAccess.open("res://../evidence/godot-navigation-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  "))
	print(JSON.stringify(report))
	quit(0 if differences.is_empty() else 1)
