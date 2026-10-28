extends SceneTree
const R=preload("res://core/original_unity_random.gd")
const C=preload("res://core/original_timekeeper.gd")
var differences:Array=[]
var comparisons:=0
var calls:=0
func compare_words(actual:Array,expected:Array,path:String)->void:
	for i in 4:
		comparisons+=1
		if actual[i]!=int(expected[i]) and differences.size()<24:differences.append({"path":path+"/"+str(i),"actual":actual[i],"expected":int(expected[i])})
func compare_value(actual:float,expected:float,path:String)->void:
	comparisons+=1
	if actual!=C.f32(expected) and differences.size()<24:differences.append({"path":path,"actual":actual,"expected":expected})
func _initialize()->void:
	var fixture:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/decoy-native-random.json"))
	var transitions:=0
	for test in fixture.cases:
		var rng:=R.new(int(test.seed))
		compare_words(rng.get_state(),test.initial,"seed/"+str(test.seed))
		for row in test.rows:
			var prefix:=str(test.seed)+"/"+str(calls)
			compare_words(rng.get_state(),row.before,prefix+"/before")
			var value:=0.0
			match int(row.kind):
				0:value=rng.value()
				1:value=rng.range_float(-3,7)
				2:value=rng.range_int(0,17)
				3:
					var point:=rng.inside_unit_circle()
					compare_value(point.x,row.vector[0],prefix+"/x")
					compare_value(point.y,row.vector[1],prefix+"/y")
				4:value=rng.range_float(0,360)
				5:value=rng.range_int(-5,12)
				6:value=rng.range_float(3,3)
				7:value=rng.range_int(8,8)
				8:value=rng.range_float(7,-3)
				9:value=rng.range_int(12,-5)
				10:value=rng.range_float(-19.5,19.5)
				11:value=rng.range_float(40,70)
				12:value=rng.range_float(3,6)
				13:value=rng.range_int(0,3)
				14:
					var point:=OriginalActorMotion.normalized(rng.inside_unit_circle())
					compare_value(point.x,row.vector[0],prefix+"/x")
					compare_value(point.y,row.vector[1],prefix+"/y")
			if int(row.kind) not in [3,14]:compare_value(value,row.value,prefix+"/value")
			compare_words(rng.get_state(),row.after,prefix+"/after")
			calls+=1
		transitions+=rng.draws
	var report:Dictionary={"passed":differences.is_empty(),"strict_equal":differences.is_empty(),"calls":calls,"state_transitions":transitions,"comparisons":comparisons,"seeds":fixture.cases.size(),"differences":differences,"scope":"Actual UnityPlayer RNG initialization, xorshift state, scalar values, float/integer ranges including equal/reversed limits and insideUnitCircle. Whole-game random-call interleaving is not yet reproduced."}
	FileAccess.open("res://../evidence/godot-unity-random-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  "))
	print(JSON.stringify(report))
	quit(0 if differences.is_empty() else 1)
