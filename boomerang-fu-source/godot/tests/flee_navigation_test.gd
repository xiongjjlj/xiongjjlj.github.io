extends SceneTree
const N=preload("res://core/original_navigation.gd")
const R=preload("res://core/original_system_random.gd")
const C=preload("res://core/original_timekeeper.gd")
var differences:Array=[]
var maximum_error:Dictionary={}
var comparisons:=0
func vector(value:Array)->Vector3:return Vector3(value[0],value[1],value[2])
func compare(actual:Variant,expected:Variant,path:String)->void:
	comparisons+=1
	if expected is float or expected is int:
		var wanted:float=expected if path.begins_with("system_random/") else C.f32(expected)
		var error:=absf(float(actual)-wanted)
		var category:=path.get_slice("/",0)
		maximum_error[category]=maxf(maximum_error.get(category,0.0),error)
		if error>0.0 and differences.size()<40:differences.append({"path":path,"actual":actual,"expected":expected,"error":error})
	elif expected is Array:
		var count:int=3 if actual is Vector3 else actual.size()
		if count!=expected.size() and differences.size()<40:differences.append({"path":path,"actual_size":count,"expected_size":expected.size()})
		for i in mini(count,expected.size()):compare(actual[i],expected[i],path+"/"+str(i))
	elif actual!=expected and differences.size()<40:differences.append({"path":path,"actual":actual,"expected":expected})
func _initialize() -> void:
	var nav:=N.new();nav.initialize(JSON.parse_string(FileAccess.get_file_as_string("res://assets/original/navigation-enclosed.json")))
	var fixture:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/flee-navigation-native-paths.json"))
	for row in fixture.randoms:
		var random:=R.new(int(row.seed));var samples:Array=[];var expected:Array=[]
		for i in row.values.size():
			samples.append(random.next_double())
			var bytes:=PackedByteArray();bytes.resize(8);bytes.encode_u64(0,str(row.bits[i]).hex_to_int());expected.append(bytes.decode_double(0))
		compare(samples,expected,"system_random/"+str(row.seed))
	for i in fixture.paths.size():
		var row:Dictionary=fixture.paths[i]
		var actual:=nav.find_flee_path(vector(row.start),vector(row.avoid),R.new(int(row.seed)),int(row.length),int(row.spread),float(row.strength))
		compare(actual.error,row.error,"path_error/"+str(i))
		if actual.error!="":continue
		for key in ["node_ids","start_node","end_node","edge","aim","searched","clamped_start","clamped_end","raw","snapped","trimmed","smoothed","total_length"]:compare(actual[key],row[key],key+"/"+str(i))
	var report:Dictionary={"passed":differences.is_empty(),"strict_equal":differences.is_empty(),"comparisons":comparisons,"paths":fixture.paths.size(),"system_random_seeds":fixture.randoms.size(),"maximum_error":maximum_error,"differences":differences,"scope":"Original FleePath and RandomPath on Enclosed graph, seeded Mono System.Random and source modifiers. Live pooled seed scheduling and complete PlayerAI are separate."}
	FileAccess.open("res://../evidence/godot-flee-navigation-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  ")+"\n")
	print("BF_FLEE_NAVIGATION "+JSON.stringify(report))
	quit(0 if differences.is_empty() else 1)
