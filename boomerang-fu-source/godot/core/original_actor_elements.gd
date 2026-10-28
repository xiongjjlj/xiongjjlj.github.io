class_name OriginalActorElements
extends RefCounted
## Original Actor/Player fire, ice contact and timer blocks. Visual outputs
## and full engine callback order are separate from this state machine.
const C=preload("res://core/original_timekeeper.gd")
const A=preload("res://core/original_actor_motion.gd")
var actor:OriginalActorMotion
var inventory:OriginalPlayerWeapons
var grid:OriginalCellGrid
var combat:OriginalCombatWorld
var index:=0
var burning:=false
var burn_timer:=0.0
var burn_duration:=2.5
var fire_distance:=0.0
var fire_owner:=-1
var slime_timing:=false
var slime_timer:=0.0
var slime_duration:=C.f32(0.5)
var slime_buffer:=C.f32(0.1)
var freeze_timer:=0.0
var freeze_duration:=7.0
var chip_timer:=0.0
var extinguish_amount:=0.0
var fires_extinguished:=0
var time_on_fire:=0.0
var move_distance:=0.0
var previous_position:=Vector2.ZERO
var previous_z:=0.0
var last_teleport:=-100.0
var can_use_teleportal:=true
var deaths:=0
var killer:=-1
var killer_type:=""
var killer_duration:=0.0
var push:=Vector2.ZERO
var push_duration:=0.0
var push_stun:=0.0
var events:Array[Dictionary]=[]
var rng:=RandomNumberGenerator.new()

func set_killer(owner:int,type:String,duration:=0.0)->void:
	killer=owner
	killer_type=type
	killer_duration=C.f32(duration)
	if combat!=null:
		combat.killer_ids[index]=owner
		combat.killer_timers[index]=killer_duration

func toggle_fire(value:bool,steam:=false)->void:
	if burning and not value:extinguish_amount=0.0
	if not burning and value:
		actor.is_showing_off=false
		actor.break_disguise()
		extinguish_amount=0.0
	if steam and burning and not value:events.append({"type":"steam"})
	if value and actor.on_wet_floor:return
	burning=value
	burn_timer=0.0
	events.append({"type":"fire_changed","value":value,"steam":steam})

func freeze()->void:
	var up:=OriginalDiscMotion.euler_up_z(C.f32(actor.rotation_z*C.f32(57.29578)))
	if OriginalDiscMotion.dot32(up,Vector2(0,-1))<C.f32(0.2):
		var direction:=A.normalized(Vector2(rng.randf_range(-0.4,0.4),-1))
		actor.rotation_z=C.f32(atan2(-direction.x,direction.y))
	actor.is_frozen=true
	actor.move_speed_parameter=0.0
	freeze_timer=0.0
	chip_timer=0.0
	toggle_fire(false,true)
	# Source Player override checks isOnFire after base already extinguished it.
	if burning:fires_extinguished+=1
	if actor.is_shielded:inventory.powers.stop_shield()
	events.append({"type":"frozen"})

func unfreeze()->void:
	actor.is_frozen=false
	freeze_timer=0.0
	actor.is_slimed=false
	slime_timing=false
	slime_timer=0.0
	events.append({"type":"unfrozen"})

func contacts()->void:
	if actor.state==A.State.DEAD:return
	var fire:=grid.touching(actor.position,OriginalCellGrid.CellType.FIRE)
	if not burning and not slime_timing and fire.touch:
		if actor.is_dashing or actor.is_invulnerable or actor.on_wet_floor:
			grid.extinguish(actor.position,OriginalCellGrid.CellType.FIRE)
		elif actor.is_shielded:
			actor.stop_aiming()
			actor.velocity=Vector2.ZERO
			inventory.powers.damage_shield()
			actor.start_pushback(Vector2.ZERO,C.f32(0.15),0.0)
			push=Vector2.ZERO
			push_duration=C.f32(0.15)
			push_stun=0.0
			var owner:=grid.touching(actor.position,OriginalCellGrid.CellType.FIRE)
			if owner.touch:
				grid.extinguish(actor.position,OriginalCellGrid.CellType.FIRE)
				if owner.owner!=-1:set_killer(owner.owner,"Fire",1.5)
		else:
			toggle_fire(true)
			if fire.owner!=-1:fire_owner=fire.owner
	if burning and fire_distance>0.5:
		fire_distance=0.0
		grid.register_cell(OriginalCellGrid.CellType.FIRE,actor.position,C.f32(0.66),index)
	var ice:=grid.touching(actor.position,OriginalCellGrid.CellType.ICE)
	if not actor.is_dashing and not actor.is_invulnerable and not actor.is_frozen and ice.touch:
		if not slime_timing:
			slime_timing=true
			slime_timer=slime_duration
			if burning:
				toggle_fire(false,true)
				fires_extinguished+=1
		elif slime_timer>0.0:
			slime_timer=C.f32(slime_timer-actor.fixed_delta*(C.f32(0.2) if ice.owner!=-1 and ice.owner==index else 1.0))
		else:
			grid.extinguish(actor.position,OriginalCellGrid.CellType.ICE)
			freeze()
			set_killer(ice.owner,"Freeze",1.0)
	elif slime_timing:
		if slime_timer<slime_duration:slime_timer=C.f32(slime_timer+C.f32(actor.fixed_delta*0.5))
		else:
			slime_timing=false
			slime_timer=0.0
			if actor.is_slimed:actor.is_slimed=false
	if slime_timing and not actor.is_slimed and slime_timer<C.f32(slime_duration-slime_buffer):
		actor.is_slimed=true
		actor.break_disguise()

func timers(delta:float)->void:
	delta=C.f32(delta)
	if burning and burn_timer<burn_duration:
		burn_timer=C.f32(burn_timer+delta*actor.time_scaler)
		if actor.on_wet_floor or slime_timing:toggle_fire(false,true)
	elif burning:
		if actor.state!=A.State.DEAD:
			if fire_owner!=-1:set_killer(fire_owner,"Fire")
			if combat!=null:combat.kill(index,"fire")
			else:
				actor.state=A.State.DEAD
				actor.velocity=Vector2.ZERO
				actor.is_dashing=false
			deaths+=1
		actor.is_burnt=true
		burn_timer=0.0
		toggle_fire(false)
		events.append({"type":"burnt"})
	if actor.is_frozen and freeze_timer<freeze_duration:freeze_timer=C.f32(freeze_timer+delta)
	elif actor.is_frozen:unfreeze()
	if actor.is_frozen and chip_timer>0.0:chip_timer=C.f32(chip_timer-delta)

func player_frame(delta:float)->void:
	delta=C.f32(delta)
	if burning and burn_timer<burn_duration:
		time_on_fire=C.f32(time_on_fire+delta)
		if extinguish_amount>=1.0 or actor.on_wet_floor:
			toggle_fire(false,true)
			fires_extinguished+=1

func movement_frame(delta:float,time:float)->void:
	var distance:=A.magnitude_xyz(actor.position-previous_position,C.f32(actor.source_z-previous_z))
	move_distance=C.f32(move_distance+distance)
	if burning and C.f32(C.f32(time)-C.f32(last_teleport))>C.f32(delta) and can_use_teleportal:
		extinguish_amount=C.f32(extinguish_amount+distance/C.f32(11.06)*C.f32(0.24))
	previous_position=actor.position
	previous_z=actor.source_z

func note_previous_move(amount:Vector2)->void:
	if burning:fire_distance=C.f32(fire_distance+A.magnitude(amount*actor.fixed_delta))

func chip(random_direction:=Vector2.INF)->void:
	if chip_timer>0.0:return
	if random_direction==Vector2.INF:random_direction=Vector2(rng.randf_range(-1,1),rng.randf_range(-1,1))
	actor.velocity+=A.normalized(random_direction)*actor.slide_factor
	chip_timer=C.f32(0.2)
	freeze_timer=C.f32(freeze_timer+freeze_duration/12.0)
	events.append({"type":"chip"})
