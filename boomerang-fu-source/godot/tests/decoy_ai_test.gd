extends SceneTree
const AI=preload("res://core/original_decoy_ai.gd")
const M=preload("res://core/original_decoy_motion.gd")
const I=preload("res://core/original_decoy_input.gd")
const R=preload("res://core/original_unity_random.gd")
const B=preload("res://core/original_level_boundary.gd")
const C=preload("res://core/original_timekeeper.gd")
var differences:Array=[]
var maximum_error:Dictionary={}
var comparisons:=0
var samples:=0
func v(value:Array)->Vector3:return Vector3(value[0],value[1],value[2])
func create(seed:int,position:Vector3,up:Vector3)->RefCounted:
	var ai:=AI.new()
	ai.actor=M.new()
	ai.actor.position=Vector2(position.x,position.y)
	ai.actor.source_z=position.z
	ai.actor.last_rotation=Vector2.ZERO
	ai.actor.last_input_move=Vector2.ZERO
	ai.decoy_input=I.new()
	ai.decoy_input.actor=ai.actor
	ai.rng=R.new(seed)
	ai.boundary=B.new()
	ai.forward_override=Vector2(up.x,up.y)
	ai.state=AI.AIState.WANDERING
	ai.reached_end=false
	return ai
func snapshot(ai:RefCounted)->Dictionary:
	return {"target":ai.target,"move":Vector3(ai.move_dir.x,ai.move_dir.y,0),"input":Vector3(ai.decoy_input.input_move.x,ai.decoy_input.input_move.y,0),"input_dash":ai.decoy_input.input_dash,"state":ai.state,"last":ai.last_state,"waypoint":ai.current_waypoint,"has_path":ai.has_path,"reached":ai.reached_end,"recalculating":ai.recalculating,"reluctance":ai.reluctance,"reluctance_cooldown":ai.reluctance_cooldown,"scan_timer":ai.scan_timer,"scan_delay":ai.scan_delay,"action_timer":ai.action_timer,"action_delay":ai.action_delay,"input_timer":ai.input_timer,"create_timer":ai.create_timer,"create_after_dash":ai.create_after_dash,"unsafe_attempts":ai.unsafe_attempts,"rng":ai.rng.get_state()}
func compare(actual:Variant,expected:Variant,path:String,tolerance:=0.0)->void:
	comparisons+=1
	if path.contains("/rng/"):
		if int(actual)!=int(expected) and differences.size()<32:differences.append({"path":path,"actual":actual,"expected":expected})
	elif expected is float or expected is int:
		var error:=absf(float(actual)-C.f32(expected))
		maximum_error[path.get_slice("/",0)]=maxf(maximum_error.get(path.get_slice("/",0),0.0),error)
		if error>tolerance and differences.size()<32:differences.append({"path":path,"actual":actual,"expected":expected,"error":error})
	elif expected is Array:
		for i in expected.size():compare(actual[i],expected[i],path+"/"+str(i),tolerance)
	elif expected is Dictionary:
		for key in expected:compare(actual[key],expected[key],path+"/"+key,0.00002 if key in ["target","move","input"] else 0.0)
	elif actual!=expected and differences.size()<32:differences.append({"path":path,"actual":actual,"expected":expected})
func _initialize()->void:
	var fixture:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/decoy-ai-native-decisions.json"))
	for test in fixture.targets:
		var ai:=create(int(test.seed),v(test.position),v(test.up))
		ai.boundary.size=Vector2(test.size[0],test.size[1])
		ai.boundary.angle=test.level_angle
		for row in test.rows:
			ai.set_random_target()
			compare(snapshot(ai),row,"target/"+str(samples))
			samples+=1
	for test in fixture.diverts:
		var ai:=create(int(test.seed),v(test.position),v(test.up))
		ai.state=int(test.initial)
		ai.last_state=int(test.last)
		ai.divert_path(v(test.avoid))
		compare(snapshot(ai),test.result,"divert/"+str(samples))
		samples+=1
	for test in fixture.moves:
		var ai:=create(int(test.seed),v(test.position),Vector3.UP)
		ai.target=Vector3(20,-2,0)
		ai.actor.input_move=Vector2(test.input[0],test.input[1])
		ai.actor.is_aiming=test.aiming
		ai.actor.on_wet_floor=test.wet
		ai.actor.can_dash=false
		ai.has_parent=test.parent
		ai.has_path=not test.no_path
		ai.path.assign([Vector3(0,-2,0),Vector3(4,-2,0),Vector3(8,-2,0)])
		ai.current_waypoint=int(test.initial_waypoint)
		ai.recalculating=test.recalculating
		var hazard:bool=test.hazard
		var touching:bool=test.touching
		ai.point_hazard=func(p:Vector3)->bool:return hazard and p.x>=8.0
		ai.touching_hazard=func(_p:Vector3)->bool:return touching
		ai.move_on_path(C.f32(0.016666),true)
		compare(snapshot(ai),test.result,"move/"+str(samples))
		samples+=1
	for test in fixture.timers:
		var ai:=create(int(test.seed),Vector3.ZERO,Vector3.UP)
		ai.target=Vector3(12,12,0)
		ai.move_dir=Vector2(0.3,0.7)
		ai.reluctance_cooldown=C.f32(1.5)
		ai.reluctance=C.f32(1.25)
		ai.hard_difficulty=test.hard
		for row in test.rows:
			ai.actor.is_dashing=row.dashing
			ai.actor.state=M.State.DEAD if row.dead else M.State.ALIVE
			ai.ai_update(C.f32(0.016666),row.playing)
			compare(snapshot(ai),row.result,"timers/"+str(samples))
			ai.coroutine_phase()
			samples+=1
	for test in fixture.forces:
		var ai:=create(int(test.seed),Vector3(2,-3,0),Vector3.UP)
		ai.actor.can_dash=test.can_dash
		ai.actor.is_dashing=test.dashing
		var safety:bool=test.safety
		var hazard:bool=test.hazard
		ai.dash_safety=func(direction:Vector2,_avoid:bool,_outside:bool,_end:bool)->bool:return not safety or direction.y<0.0
		ai.point_hazard=func(p:Vector3)->bool:return hazard and p.x>2.0
		ai.force_dash(Vector2(test.direction[0],test.direction[1]))
		compare(snapshot(ai),test.result,"force/"+str(samples))
		compare(Vector3(ai.actor.input_move.x,ai.actor.input_move.y,0),test.actor_input,"force/actor_input/"+str(samples),0.00002)
		compare(Vector3(ai.actor.last_rotation.x,ai.actor.last_rotation.y,0),test.actor_rotation,"force/actor_rotation/"+str(samples),0.00002)
		samples+=1
	var report:Dictionary={"passed":differences.is_empty(),"samples":samples,"comparisons":comparisons,"maximum_error":maximum_error,"differences":differences,"geometry_tolerance":0.00002,"scope":"Original random targets, diversion, waypoint following, timers and forced dash decisions; exact discrete/RNG state, explicit vector tolerance. Uses injected facing, spatial predicates and path-response endpoints; native scene/scheduler remains outside scope."}
	FileAccess.open("res://../evidence/godot-decoy-ai-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  "))
	print(JSON.stringify(report))
	quit(0 if differences.is_empty() else 1)
