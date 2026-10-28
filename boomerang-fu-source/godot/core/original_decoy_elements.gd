class_name OriginalDecoyElements
extends "res://core/original_actor_elements.gd"
var lifecycle_hook:WeakRef
var serial:=0
var unity_rng:OriginalUnityRandom

func toggle_fire(value:bool,steam:=false)->void:
	if value:
		if lifecycle_hook!=null and lifecycle_hook.get_ref()!=null:lifecycle_hook.get_ref().die(serial,"fire")
	else:super.toggle_fire(false,steam)

func contacts()->void:
	# Actor's shield/fire branch is guarded by `this is Player`.
	var shield:=actor.is_shielded
	actor.is_shielded=false
	super.contacts()
	if actor.state!=A.State.DEAD:actor.is_shielded=shield

func freeze()->void:
	var up:=OriginalDiscMotion.euler_up_z(C.f32(actor.rotation_z*C.f32(57.29578)))
	if OriginalDiscMotion.dot32(up,Vector2(0,-1))<C.f32(0.2):
		var direction:=A.normalized(Vector2(unity_rng.range_float(-0.4,0.4),-1))
		actor.last_rotation=direction
		actor.last_input_move=direction
		actor.rotation_z=C.f32(atan2(-direction.x,direction.y))
	actor.is_frozen=true
	actor.move_speed_parameter=0.0
	freeze_timer=0.0
	chip_timer=0.0
	toggle_fire(false,true)
	events.append({"type":"frozen"})

func chip(random_direction:=Vector2.INF)->void:
	if chip_timer>0.0:return
	if random_direction==Vector2.INF:random_direction=Vector2(unity_rng.range_float(-1,1),unity_rng.range_float(-1,1))
	super.chip(random_direction)
