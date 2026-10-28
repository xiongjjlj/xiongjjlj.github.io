extends SceneTree
const P=preload("res://core/original_player_ai_policy.gd")
const C=preload("res://core/original_timekeeper.gd")
var differences:Array=[]
var compared:=0
var exact:=0
var max_error:=0.0
var non_exact:Array=[]
func compare(got:Variant,want:Variant,path:String)->void:
	if want is Dictionary:
		for key in want:compare(got[key],want[key],path+"."+key)
	elif want is Array:
		if got.size()!=want.size():differences.append({"path":path,"reason":"array size"});return
		for i in want.size():compare(got[i],want[i],path+"["+str(i)+"]")
	else:
		compared+=1
		var passed:=false
		if want is bool or path.contains(".rng["):
			passed=got==want
			if passed:exact+=1
		else:
			var error:=absf(float(got)-C.f32(want))
			max_error=maxf(max_error,error)
			passed=error==0.0
			if error==0.0:exact+=1
			elif non_exact.size()<50:non_exact.append({"path":path,"got":got,"expected":C.f32(want)})
		if not passed and differences.size()<50:differences.append({"path":path,"got":got,"expected":want})
func vec(a:Array)->Vector3:return Vector3(a[0],a[1],a[2])
func player(raw:Dictionary)->Dictionary:
	var snapshot:=raw.duplicate(true)
	snapshot.position=vec(raw.position);snapshot.forward=vec(raw.forward)
	return snapshot
func mods(p:RefCounted)->Dictionary:return {"ambient":p.ambient,"aggression":p.aggression,"nervous":p.nervous,"greedy":p.greedy}
func params(p:RefCounted)->Dictionary:return {"reactions":p.reaction_times,"delay":p.input_delay,"bearings_duration":p.bearings_duration,"bearings_speed":p.bearings_speed,"bearings_timer":p.bearings_timer,"teleport_threshold":p.teleport_threshold,"rng":p.rng.get_state()}
func _initialize() -> void:
	var fixture:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/player-ai-native-policy.json"))
	var index:=0
	for row in fixture.difficulty:
		var p:=P.new();p.rng.initialize(int(row.seed));p.set_difficulty(int(row.difficulty))
		var path:="difficulty["+str(index)+"]";index+=1
		compare(params(p),row.initial,path+".initial")
		for reaction in row.reactions:
			p.bearings_timer=C.f32(p.bearings_duration*reaction.fraction)
			var values:Array=[]
			for i in 3:values.append(p.random_reaction())
			compare(values,reaction.values,path+".reaction"+str(reaction.fraction))
			compare(p.rng.get_state(),reaction.rng,path+".rng")
		for j in row.mods.size():
			var sample:Dictionary=row.mods[j]
			p.randomize_modifiers(sample.reverse,sample.retrieving)
			compare(mods(p),sample.result,path+".mods["+str(j)+"]")
			compare(p.rng.get_state(),sample.rng,path+".rng")
	index=0
	for row in fixture.interests:
		var p:=P.new();p.difficulty=int(row.difficulty);p.aggression=C.f32(row.mods.aggression);p.nervous=C.f32(row.mods.nervous);p.greedy=C.f32(row.mods.greedy)
		var own:=player(row.self);var other:=player(row.other);var size:=Vector2(row.size[0],row.size[1]);var path:="interest["+str(index)+"]";index+=1
		compare(p.can_see_target(other),row.visible,path+".visible")
		compare(p.victim_interest(own,other,size),row.victim,path+".victim")
		compare(p.threat_interest(own,other,size),row.threat,path+".threat")
		compare(p.threat_interest(own,other,size,1),row.explosive,path+".explosive")
		compare(p.threat_interest(own,other,size,2),row.fire,path+".fire")
	index=0
	for row in fixture.desires:
		var p:=P.new();p.greedy=C.f32(row.greedy)
		var target:Dictionary=row.duplicate(true);target.position=vec(row.position)
		var own:=player(row.self);var size:=Vector2(row.size[0],row.size[1]);var path:="desire["+str(index)+"]";index+=1
		compare(p.desire_interest(own,target,size),row.powerup,path+".powerup")
		compare(p.desire_interest(own,target,size,1),row.disc,path+".disc")
	index=0
	for row in fixture.visibility:
		var p:=P.new();p.difficulty=int(row.difficulty)
		compare(p.can_see_target(player(row.target)),row.visible,"visibility["+str(index)+"]");index+=1
	var report:={"passed":differences.is_empty(),"comparisons":compared,"exact_float32_or_discrete":exact,"max_absolute_error":max_error,"tolerance":0.0,"differences":differences,"non_exact":non_exact,"groups":{"difficulty":fixture.difficulty.size(),"interests":fixture.interests.size(),"desires":fixture.desires.size(),"visibility":fixture.visibility.size()},"full_player_ai_verified":false}
	FileAccess.open("res://../evidence/godot-player-ai-policy-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  ")+"\n")
	print("BF_PLAYER_AI_POLICY "+JSON.stringify(report))
	quit(0 if differences.is_empty() else 1)
