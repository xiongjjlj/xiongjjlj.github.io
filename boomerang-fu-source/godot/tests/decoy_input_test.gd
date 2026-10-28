extends SceneTree
const M=preload("res://core/original_decoy_motion.gd")
const I=preload("res://core/original_decoy_input.gd")
const C=preload("res://core/original_timekeeper.gd")
var differences:Array=[]
var maximum_error:Dictionary={}
var comparisons:=0
var frames:=0
func compare(actual:Variant,expected:Variant,path:String)->void:
	comparisons+=1
	if expected is float or expected is int:
		var error:=absf(float(actual)-C.f32(expected))
		var category:=path.get_slice("/",0)
		maximum_error[category]=maxf(maximum_error.get(category,0.0),error)
		if error>0.0 and differences.size()<24:differences.append({"path":path,"actual":actual,"expected":expected,"error":error})
	elif expected is Array:
		if actual.size()!=expected.size() and differences.size()<24:differences.append({"path":path,"actual_size":actual.size(),"expected_size":expected.size()})
		for i in mini(actual.size(),expected.size()):compare(actual[i],expected[i],path+"/"+str(i))
	elif expected is Dictionary:
		for key in expected:compare(actual[key],expected[key],path+"/"+key)
	elif actual!=expected and differences.size()<24:differences.append({"path":path,"actual":actual,"expected":expected})
func _initialize()->void:
	var fixture:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/decoy-native-lifecycle.json"))
	for test in fixture.cases:
		var actor:=M.new()
		var input:=I.new()
		input.actor=actor
		if test.name!="fresh_spawn":input.initialize()
		var time:=0.0
		for row in test.frames:
			actor.is_frozen=row.frozen
			actor.move_faster=int(row.power)&2!=0
			actor.is_wading=row.wading
			actor.wade_factor=C.f32(row.wade_factor)
			actor.is_aiming=row.aiming
			actor.is_slimed=row.slimed
			actor.is_disarmed=row.disarmed
			actor.time_scaler=C.f32(row.scaler)
			input.input_move=Vector2(row.move[0],row.move[1])
			input.input_dash=row.press
			var before:=actor.successful_dashes
			input.frame_step(row.game_state,time)
			var prefix:String=test.name+"/"+str(row.frame)
			compare(input.snapshot(),row.before_end,"input/"+prefix+"/before")
			actor.input_move=input.input_move
			compare(actor.get_move_speed(),row.speed,"speed/"+prefix)
			if int(row.game_state)==I.GameState.PLAYING:
				actor.fixed_step()
				actor.frame_step(actor.fixed_delta)
			input.end_frame()
			compare(actor.successful_dashes-before,row.dash_success,"dash_success/"+prefix)
			compare(input.snapshot(),row.after_end,"input/"+prefix+"/after")
			var motion:Dictionary={"position":[actor.position.x,actor.position.y],"velocity":[actor.velocity.x,actor.velocity.y],"stored":[actor.velocity_stored.x,actor.velocity_stored.y],"dash_timer":actor.dash_timer,"cooldown":actor.dash_cooldown_timer,"dashing":actor.is_dashing,"can_walk":actor.can_walk,"can_dash":actor.can_dash,"slide":actor.slide_factor,"drag":actor.drag}
			compare(motion,row.motion,"motion/"+prefix)
			time=C.f32(time+actor.fixed_delta)
			frames+=1
	var report:Dictionary={"passed":differences.is_empty(),"strict_equal":differences.is_empty(),"frames":frames,"comparisons":comparisons,"maximum_error":maximum_error,"differences":differences,"scope":"Original Decoy speed/dash, original bounded input buffer and end-of-frame enabling; shared Actor locomotion. Native scene physics, full AI, pool scheduling and presentation are not covered."}
	FileAccess.open("res://../evidence/godot-decoy-input-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  "))
	print(JSON.stringify(report))
	quit(0 if differences.is_empty() else 1)
