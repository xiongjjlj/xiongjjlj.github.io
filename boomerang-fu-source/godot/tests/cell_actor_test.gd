extends SceneTree
const C=preload("res://core/original_timekeeper.gd")
var differences:Array=[]
var mismatch_count:=0
func vector(value:Array)->Vector2:return Vector2(value[0],value[1])
func compare(a:Variant,b:Variant,path:String)->void:
	if a is Dictionary:
		for key in a:compare(a[key],b[key],path+"."+key)
		return
	if a is Array and b is Array and a.size()==b.size():
		for i in a.size():compare(a[i],b[i],path+"[%d]"%i)
		return
	if a is float:b=C.f32(b)
	if a!=b:
		mismatch_count+=1
		if differences.size()<25:differences.append({"field":path,"actual":a,"expected":b})
func _initialize()->void:
	var fixture:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/cell-native-grid.json"))
	var frames:=0
	for item in fixture.actors:
		var actor:=OriginalActorMotion.new()
		var weapon:=OriginalPlayerWeapons.new()
		weapon.initialize(actor,OriginalCollisionWorld.new())
		var state:=OriginalActorElements.new()
		state.actor=actor
		state.inventory=weapon
		state.grid=OriginalCellGrid.new()
		state.can_use_teleportal=item.initial.portal
		actor.time_scaler=C.f32(item.initial.scale)
		actor.is_shielded=item.initial.shield
		weapon.powers.shield_hits=1
		state.grid.register_cell(int(item.initial.type),Vector2.ZERO,30.0,int(item.initial.owner))
		for i in item.frames.size():
			var frame:Dictionary=item.frames[i]
			var input:Dictionary=frame.input
			state.grid.current_time=C.f32(input.time)
			if input.register_ice:state.grid.register_cell(OriginalCellGrid.CellType.ICE,actor.position,30.0,int(item.initial.owner))
			actor.position=vector(input.position)
			actor.is_dashing=input.dash
			actor.is_invulnerable=input.invulnerable
			actor.on_wet_floor=input.wet
			actor.fixed_delta=C.f32(input.delta)
			state.fire_distance=C.f32(input.fire_distance)
			state.last_teleport=C.f32(input.teleport)
			state.contacts()
			state.timers(input.delta)
			state.player_frame(input.delta)
			state.movement_frame(input.delta,input.time)
			state.grid.frame_step(input.time)
			var actual:Dictionary={"dead":actor.state==OriginalActorMotion.State.DEAD,"deaths":state.deaths,"burning":state.burning,"burn_timer":state.burn_timer,"fire_distance":state.fire_distance,"fire_owner":state.fire_owner,"slime_timing":state.slime_timing,"slime_timer":state.slime_timer,"slimed":actor.is_slimed,"frozen":actor.is_frozen,"freeze_timer":state.freeze_timer,"chip_timer":state.chip_timer,"burnt":actor.is_burnt,"shield":actor.is_shielded,"shield_hits":weapon.powers.shield_hits,"invulnerable":actor.is_invulnerable,"extinguish":state.extinguish_amount,"fires_extinguished":state.fires_extinguished,"time_on_fire":state.time_on_fire,"move_distance":state.move_distance,"killer":state.killer,"killer_type":state.killer_type,"killer_duration":state.killer_duration,"push":[state.push.x,state.push.y],"push_duration":state.push_duration,"push_stun":state.push_stun}
			compare(actual,frame.state,item.name+"[%d]"%i)
			state.events.clear()
			state.grid.events.clear()
			frames+=1
	var report:Dictionary={"passed":mismatch_count==0,"cases":fixture.actors.size(),"frames":frames,"mismatch_count":mismatch_count,"differences":differences,"scope":"Unchanged Actor/Player fire/ice blocks and methods; explicit fixed/frame sequence, injected movement/death/audio/visuals and invulnerability inputs; freeze random rotation/chipping and full-match order separate","full_game_parity_verified":false}
	FileAccess.open("res://../evidence/godot-native-cell-actor-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print("BF_CELL_ACTOR_TEST "+JSON.stringify(report))
	quit(0 if report.passed else 1)
