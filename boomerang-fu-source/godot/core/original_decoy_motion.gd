class_name OriginalDecoyMotion
extends "res://core/original_actor_motion.gd"
## Decoy uses Actor's dash, with its own simplified walking-speed override.
var wade_factor:=1.0
var successful_dashes:=0
var owner_actor:OriginalActorMotion
var lifecycle_hook:WeakRef
var serial:=0

func _init()->void:
	is_decoy=true
	collision_layer=256

func respond_to_actor(normal:Vector2,other:OriginalActorMotion)->void:
	if state==State.DEAD or other.state==State.DEAD or other==owner_actor:return
	if not is_dashing:
		if lifecycle_hook!=null and lifecycle_hook.get_ref()!=null:lifecycle_hook.get_ref().die(serial,"contact")
		return
	super.respond_to_actor(normal,other)

func get_move_speed()->float:
	var amount:=200.0
	if move_faster:amount=Clock.f32(amount*Clock.f32(1.4))
	if is_wading:amount=Clock.f32(amount*wade_factor)
	return Clock.f32(amount*clampf(time_scaler,1.0,1.75))

func dash()->bool:
	# Only Player.Dash applies the haste boost. Decoy delegates to Actor.Dash.
	var faster:=move_faster
	move_faster=false
	var accepted:=super.dash()
	move_faster=faster
	if accepted:successful_dashes+=1
	return accepted
