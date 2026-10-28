extends SceneTree
const C=preload("res://core/original_timekeeper.gd")
const D=preload("res://core/original_disc_motion.gd")
const V=preload("res://core/original_actor_motion.gd")
var differences:Array=[]
var mismatches:=0
var maximum_error:=0.0
func compare(actual:Variant,expected:Variant,path:String)->void:
	if actual is Dictionary:
		for key in actual:compare(actual[key],expected[key],path+"/"+str(key))
	elif actual is Array:
		for i in actual.size():compare(actual[i],expected[i],path+"/"+str(i))
	else:
		var reference:Variant=C.f32(expected) if actual is float else expected
		if actual!=reference:
			mismatches+=1
			if actual is float:maximum_error=maxf(maximum_error,absf(actual-reference))
			if differences.size()<30:differences.append({"path":path,"actual":actual,"expected":reference})
func vector(v:Vector2)->Array:return [v.x,v.y]
func state(d:OriginalDiscMotion,p:OriginalActorMotion)->Dictionary:
	return {"position":vector(d.position),"velocity":vector(d.velocity),"stored":vector(d.stored),"movement":d.movement,"aim":d.aim_power,"scale":d.time_scaler,"speed":d.speed_limit,"duration":d.throw_duration,"timer":d.throw_timer,"strength":d.throw_strength,"return_timer":d.return_timer,"spin_timer":d.spin_timer,"spinning":d.is_spinning,"can_return":d.can_return,"can_control":d.can_be_controlled,"can_owner":d.can_collide_with_owner,"pickable":d.can_be_picked_up,"controlled_direction":vector(d.controlled_direction),"controlled_factor":d.controlled_factor,"boundary":d.controlled_boundary_active,"control_time":p.time_controlling_disc}
func _initialize()->void:
	var fixture:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/controlled-native-lifecycle.json"))
	var frames:=0
	for item in fixture.cases:
		var name:String=item.name
		var owner:=V.new()
		var disc:=D.new()
		disc.owner_actor=owner
		disc.world=OriginalCollisionWorld.new()
		disc.throw_direction=Vector2.RIGHT
		disc.disc_powerups=0 if name=="missing_flag" else 8192
		disc.position=Vector2(5,-3)
		disc.source_z=-0.75
		disc.aim_power=C.f32(0.6) if name=="charged" else 0.0
		disc.is_mini_disc=name=="mini"
		disc.initialize()
		disc.can_home=false
		if name in ["returning","regain_control"]:disc.start_return()
		if name in ["disabled","regain_control"]:disc.can_be_controlled=false
		if name=="dropped_reacquire":disc.movement=D.Movement.DROPPED;disc.velocity=Vector2(30,0);disc.can_be_controlled=false
		if name=="free_reacquire":disc.start_freeform();disc.velocity=Vector2(5,0);disc.can_be_controlled=false
		if name.begins_with("release_"):
			disc.movement=D.Movement.CONTROLLED
			disc.controlled_factor=C.f32(0.9)
			disc.velocity=Vector2(18,4) if name=="release_fast" else Vector2(0.5,0)
			disc.throw_timer=C.f32(0.1) if name=="release_stopped_early" else disc.throw_duration
			disc.can_return=name!="release_stopped_no_return"
		if name.begins_with("boundary_"):
			disc.position=Vector2(30 if name=="boundary_30" else 34 if name=="boundary_34" else 35,0)
			disc.movement=D.Movement.CONTROLLED
			disc.velocity=Vector2(-30,0) if name=="boundary_inward" else Vector2(0,30) if name=="boundary_tangent" else Vector2(30,0)
		disc.time_scaler=0.25 if name=="scale_quarter" else 2.5 if name=="scale_two_half" else 1.0
		compare(state(disc,owner),item.initial,name+"/initial")
		for frame in item.frames:
			owner.retrieve_charge=C.f32(frame.charge)
			owner.raw_input_move=Vector2(frame.input[0],frame.input[1])
			owner.state=V.State.DEAD if frame.dead else V.State.ALIVE
			disc.fixed_step()
			disc.frame_step(C.f32(0.016666))
			compare(state(disc,owner),frame.state,name+"/"+str(int(frame.frame)))
			frames+=1
	var report:={"passed":mismatches==0,"cases":fixture.cases.size(),"frames":frames,"mismatched_fields":mismatches,"maximum_absolute_error":maximum_error,"first_differences":differences,"full_game_parity_verified":false,"scope":fixture.scope}
	FileAccess.open("res://../evidence/godot-native-controlled-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print("BF_CONTROLLED_CHECK "+JSON.stringify(report))
	quit(0 if mismatches==0 else 1)
