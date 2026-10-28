extends SceneTree
const A=preload("res://core/original_player_ai_actions.gd")
const C=preload("res://core/original_timekeeper.gd")
var differences:Array=[]
var compared:=0
var max_error:=0.0
var exact:=0
var non_exact:Array=[]
const ROTATION_TOLERANCE=0.00000012
func compare(got:Variant,want:Variant,path:String)->void:
	if want is Dictionary:
		for key in want:compare(got[key],want[key],path+"."+key)
	elif want is Array:
		if got.size()!=want.size():differences.append({"path":path,"reason":"array size","got":got,"expected":want});return
		for i in want.size():compare(got[i],want[i],path+"["+str(i)+"]")
	else:
		compared+=1
		var passed:bool
		if want is bool or want is String:passed=got==want
		else:
			var expected:float=float(want) if path.contains(".rng[") else C.f32(want)
			var error:=absf(float(got)-expected);max_error=maxf(max_error,error)
			var rotation_component:=path.contains(".vector[") or path.contains(".move[") or path.contains(".input_move[") or path.contains(".last_rotation[")
			passed=error<=(ROTATION_TOLERANCE if rotation_component else 0.0)
			if error!=0.0 and non_exact.size()<60:non_exact.append({"path":path,"got":got,"expected":expected,"absolute_error":error})
			if error==0.0:exact+=1
		if (want is bool or want is String) and passed:exact+=1
		if not passed and differences.size()<60:differences.append({"path":path,"got":got,"expected":want})
func vec(raw:Array)->Vector3:return Vector3(raw[0],raw[1],raw[2])
func snapshot_actor(raw:Dictionary)->Dictionary:
	var snapshot:=raw.duplicate(true)
	for key in ["position","forward","velocity"]:
		if snapshot.has(key):snapshot[key]=vec(snapshot[key])
	for key in ["since_teleport","speed","disguise_time","aim","aim_max"]:
		if snapshot.has(key):snapshot[key]=C.f32(snapshot[key])
	return snapshot
func array2(v:Vector2)->Array:return [v.x,v.y]
func _initialize()->void:
	var fixture:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/player-ai-native-policy.json"))
	var index:=0;var counts:Dictionary={}
	for row:Dictionary in fixture.actions:
		var actions:=A.new();actions.targeting.policy.difficulty=int(row.difficulty);actions.targeting.policy.rng.initialize(int(row.seed));actions.move_dir=Vector2(row.move[0],row.move[1]);actions.unsafe_attempts=int(row.attempts)
		var own:=snapshot_actor(row.self);var target:=snapshot_actor(row.target)
		for key in ["retrieve_charge","dashing","can_dash","dash_safe","lunge_safe","teleport_safe","fire"]:own[key]=row[key]
		own.retrieve_charge=C.f32(own.retrieve_charge);own.last_rotation=Vector2(row.last_rotation[0],row.last_rotation[1]);own.input_move=Vector2.ZERO;own.thrown_positions=[]
		for position:Array in row.thrown_positions:own.thrown_positions.append(vec(position))
		var returned:=false
		match row.operation:
			"block":actions.block(own,target)
			"attack":actions.attack(own,target if row.victim else {})
			"throw":actions.throw_at(own,target)
			"dash":returned=actions.dash(own,Vector2(row.dash_direction[0],row.dash_direction[1]),row.avoid,row.outside,row.hazard,row.track)
		var snapshot:={"events":actions.events,"returned":returned,"move":array2(actions.move_dir),"input_move":array2(own.input_move),"last_rotation":array2(own.last_rotation),"unsafe_attempts":actions.unsafe_attempts,"path_present":actions.has_path,"rng":actions.targeting.policy.rng.get_state()}
		compare(snapshot,row.result,"action["+str(index)+"]."+row.operation);index+=1
		for event:Dictionary in actions.events:counts[event.kind]=counts.get(event.kind,0)+1
	var report:={"passed":differences.is_empty(),"comparisons":compared,"exact_comparisons":exact,"all_comparisons_exact":exact==compared,"max_absolute_error":max_error,"rotation_component_tolerance":ROTATION_TOLERANCE,"other_values_tolerance":0.0,"differences":differences,"non_exact":non_exact,"scenarios":fixture.actions.size(),"commands":counts,"full_player_ai_verified":false,"scope":"Immediate original action branches with injected safety predicates and recorded world/button/coroutine endpoints. Command order, RNG and discrete states require exact equality. Native Quaternion.AngleAxis cosine has a known 1-ULP component gap; full angular/frame parity is not established."}
	FileAccess.open("res://../evidence/godot-player-ai-actions-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  ")+"\n")
	print("BF_PLAYER_AI_ACTIONS "+JSON.stringify(report));quit(0 if differences.is_empty() else 1)
