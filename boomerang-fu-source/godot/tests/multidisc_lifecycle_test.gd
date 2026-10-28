extends SceneTree
const C=preload("res://core/original_timekeeper.gd")
const D=preload("res://core/original_disc_motion.gd")
var differences:Array=[]
var mismatches:=0
var maximum_error:=0.0
func compare(actual:Variant,expected:Variant,path:String)->void:
	if actual is Dictionary:
		for key in actual:compare(actual[key],expected[key],path+"/"+str(key))
	elif actual is Array:
		if actual.size()!=expected.size():mismatches+=1;differences.append({"path":path,"actual_size":actual.size(),"expected_size":expected.size()});return
		for i in actual.size():compare(actual[i],expected[i],path+"/"+str(i))
	else:
		var reference:Variant=C.f32(expected) if actual is float else expected
		if actual!=reference:
			mismatches+=1
			if actual is float:maximum_error=maxf(maximum_error,absf(actual-reference))
			if differences.size()<30:differences.append({"path":path,"actual":actual,"expected":reference})
func vector(v:Vector2)->Array:return [v.x,v.y]
func state(d:OriginalDiscMotion)->Dictionary:
	return {"position":vector(d.position),"z":d.source_z,"direction":vector(d.throw_direction),"velocity":vector(d.velocity),"stored":vector(d.stored),"movement":d.movement,"flags":d.disc_powerups,"mini":d.is_mini_disc,"aim":d.aim_power,"scale":d.time_scaler,"speed":d.speed_limit,"duration":d.throw_duration,"timer":d.throw_timer,"strength":d.throw_strength,"return_timer":d.return_timer,"spin_timer":d.spin_timer,"spinning":d.is_spinning,"can_return":d.can_return,"pickable":d.can_be_picked_up,"can_owner":d.can_collide_with_owner,"grenade":d.grenade_active,"grenade_timer":d.grenade_timer,"recycled":not d.active}
func _initialize()->void:
	var fixture:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/multidisc-native-lifecycle.json"))
	var frame_count:=0
	var initial_count:=0
	for index in fixture.cases.size():
		var item:Dictionary=fixture.cases[index]
		var weapon:=OriginalPlayerWeapons.new()
		weapon.initialize(OriginalActorMotion.new(),OriginalCollisionWorld.new())
		weapon.actor.position=Vector2(item.owner_position[0],item.owner_position[1])
		weapon.actor.time_scaler=C.f32(item.owner_scale)
		var parent:=D.new()
		parent.owner_actor=weapon.actor
		parent.owner_inventory=weakref(weapon)
		parent.world=weapon.world
		parent.disc_powerups=int(item.flags)
		parent.position=Vector2(item.parent_position[0],item.parent_position[1])
		parent.source_z=C.f32(item.parent_position[2])
		parent.velocity=Vector2(item.velocity[0],item.velocity[1])
		weapon.owned_discs.append(parent)
		weapon.multidisc_burst(parent)
		compare({"required":weapon.mini_discs_required,"parent_recycled":not parent.active,"parent_mini":parent.is_mini_disc,"owned":weapon.owned_discs.size(),"respawns":weapon.respawn_requests},item,"burst/%d"%index)
		var children:=weapon.spawned_discs.duplicate()
		compare(children.map(state),item.initial,"initial/%d"%index)
		initial_count+=children.size()
		for frame in item.frames:
			for child in children:child.fixed_step()
			compare(children.map(state),frame.discs,"flight/%d/%d"%[index,frame.frame])
			frame_count+=children.size()
	for index in fixture.pickups.size():
		var item:Dictionary=fixture.pickups[index]
		var weapon:=OriginalPlayerWeapons.new()
		weapon.initialize(OriginalActorMotion.new(),OriginalCollisionWorld.new())
		weapon.held_discs=int(item.held)
		weapon.mini_discs_required=int(item.required)
		weapon.mini_discs_collected=int(item.start)
		for i in item.states.size():
			var expected:Dictionary=item.states[i]
			var disc:=D.new()
			disc.owner_actor=weapon.actor
			disc.owner_inventory=weakref(weapon)
			disc.is_mini_disc=true
			disc.can_be_picked_up=expected.pickable
			weapon.owned_discs.append(disc)
			weapon.collect_mini(disc)
			weapon.check_num_mini_discs()
			compare({"pickable":disc.can_be_picked_up,"held":weapon.held_discs,"collected":weapon.mini_discs_collected,"ghost":weapon.ghost_discs,"recycled":not disc.active,"normal_catches":weapon.normal_catches,"owned":weapon.owned_discs.size()},expected,"pickup/%d/%d"%[index,i])
	var rotation_gaps:Array=[]
	for sample in fixture.rotations:
		var up:=D.euler_up_z(C.f32(sample.angle))
		var expected:=Vector2(sample.up[0],sample.up[1])
		if up!=expected:rotation_gaps.append({"angle":sample.angle,"actual":vector(up),"expected":sample.up})
	var report:={"passed":mismatches==0,"cases":fixture.cases.size(),"initial_discs":initial_count,"frames":frame_count,"pickup_cases":fixture.pickups.size(),"mismatched_fields":mismatches,"maximum_absolute_error":maximum_error,"first_differences":differences,"diagnostic_rotation_cases":fixture.rotations.size(),"diagnostic_rotation_gaps":rotation_gaps,"full_game_parity_verified":false,"scope":fixture.scope}
	FileAccess.open("res://../evidence/godot-native-multidisc-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print("BF_MULTIDISC_CHECK "+JSON.stringify(report))
	quit(0 if mismatches==0 else 1)
