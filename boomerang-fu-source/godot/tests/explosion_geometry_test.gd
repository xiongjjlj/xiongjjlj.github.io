extends SceneTree
const C = preload("res://core/original_timekeeper.gd")
var differences: Array = []
var mismatch_count := 0
var max_radius_error := 0.0
var max_vertex_error := 0.0
var max_curve_error := 0.0
var max_color_error := 0.0
func fail(path: String, actual: Variant, expected: Variant) -> void:
	mismatch_count += 1
	if differences.size()<20: differences.append({"path":path,"actual":actual.duplicate(true) if actual is Array or actual is Dictionary else actual,"expected":expected})
func compare_state(blast: OriginalExplosion, calls: Array[int], expected: Dictionary, path: String) -> void:
	var actual := {"target":blast.target_radius,"exploding":blast.exploding,"active":blast.active,"segments":blast.segments,"calls":calls,"fire_cells":blast.fire_points.size()}
	for key in actual:
		if key == "calls":
			for index in calls.size():
				if calls[index] != int(expected.calls[index]): fail(path+"/calls/"+str(index),calls[index],expected.calls[index])
			continue
		var reference: Variant = C.f32(expected[key]) if actual[key] is float else expected[key]
		if actual[key] != reference: fail(path+"/"+key,actual[key],reference)
	var error := absf(blast.radius-C.f32(expected.radius))
	max_radius_error = maxf(max_radius_error,error)
	if error>0.000005:fail(path+"/radius",blast.radius,expected.radius)
	if blast.vertices.size()!=expected.vertices.size(): fail(path+"/vertices/count",blast.vertices.size(),expected.vertices.size())
	else:
		for i in blast.vertices.size():
			var v:=blast.vertices[i]
			var refv:=Vector3(expected.vertices[i][0],expected.vertices[i][1],expected.vertices[i][2])
			var distance:=v.distance_to(refv)
			max_vertex_error=maxf(max_vertex_error,distance)
			if distance>0.00003:fail(path+"/vertices/"+str(i),str(v),str(refv))
	if blast.triangles != PackedInt32Array(expected.triangles): fail(path+"/triangles",blast.triangles.size(),expected.triangles.size())
func _initialize() -> void:
	var fixture: Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/explosive-native-lifecycle.json"))
	var prefabs: Array=JSON.parse_string(FileAccess.get_file_as_string("res://assets/original/explosion-inputs.json"))
	var parameters: Dictionary={}
	for p in prefabs:
		if p.script=="Explosion":parameters[p.name]=p.data
	for group in fixture.curves:
		var data:Dictionary=parameters[group.name]
		for sample in group.samples:
			var radius:=OriginalExplosion.curve_value(data.radiusCurve.m_Curve,C.f32(sample.t))
			var error:=absf(radius-C.f32(sample.value))
			max_curve_error=maxf(max_curve_error,error)
			if error>0.0000005:fail(group.name+"/curve",radius,sample.value)
			var color:=OriginalExplosion.gradient_value(data.gradient,C.f32(sample.t))
			for i in 4:
				error=absf(color[i]-C.f32(sample.color[i]))
				max_color_error=maxf(max_color_error,error)
				if error>0.0000003:fail(group.name+"/gradient",color[i],sample.color[i])
	var frames:=0
	for item in fixture.blasts:
		var world:=OriginalCollisionWorld.new()
		var corners:=PackedVector2Array()
		for p in item.corners:corners.append(Vector2(p[0],p[1]))
		if not corners.is_empty():
			var vertices:=PackedVector2Array([corners[0],corners[2],corners[3],corners[1]])
			world.shapes.append({"id":1,"kind":"polygon","vertices":vertices,"bounds":Rect2(vertices[0],Vector2(0.8,6)),"layer":256,"actor":false,"trigger":false,"edge_radius":0.0})
		var blast:=OriginalExplosion.new()
		blast.initialize(parameters[item.kind],Vector2(item.position[0],item.position[1]),item.position[2],item.mini,10.0,world,corners,"switch" in item.name)
		var calls:Array[int]=[0,0,0,0]
		compare_state(blast,calls,item.initial,item.name+"/initial")
		var index:=0
		for frame in item.frames:
			if item.name=="moving_wall" and index==12:
				for i in world.shapes[0].vertices.size():world.shapes[0].vertices[i].x-=4.0
				world.shapes[0].bounds.position.x-=4.0
			blast.frame_step(C.f32(frame.time),C.f32(frame.time))
			for i in item.targets.size():
				var p:Array=item.targets[i]
				if blast.reaches(Vector2(p[0],p[1]),p[2],0.5):calls[i]+=1
			compare_state(blast,calls,frame.state,item.name+"/"+str(index))
			index+=1
			frames+=1
	var report:Dictionary={"passed":mismatch_count==0,"cases":fixture.blasts.size(),"frames":frames,"curve_samples":3003,"mismatch_count":mismatch_count,"differences":differences,"max_curve_error":max_curve_error,"max_color_error":max_color_error,"max_radius_error":max_radius_error,"max_vertex_error":max_vertex_error,"tolerances":{"curve":0.0000005,"color":0.0000003,"radius":0.000005,"vertex":0.00003},"scope":"Original Explosion source in Unity: timer state, damage callback counts, triangle indices exact; radius/gradient and ray-fan vertices have explicit measured tolerances; not full game parity"}
	FileAccess.open("res://../evidence/godot-native-explosion-geometry-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print("BF_EXPLOSION_GEOMETRY "+JSON.stringify(report))
	quit(0 if mismatch_count==0 else 1)
