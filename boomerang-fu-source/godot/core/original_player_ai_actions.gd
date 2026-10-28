class_name OriginalPlayerAiActions
extends RefCounted
## Immediate PlayerAI action branches. Button/coroutine, teleport and safe-dash
## endpoints are commands for the world adapter, not a complete bot scheduler.
const C=preload("res://core/original_timekeeper.gd")
const P=preload("res://core/original_player_ai_policy.gd")
const T=preload("res://core/original_player_ai_targeting.gd")
const B=preload("res://core/original_level_boundary.gd")
var targeting:=T.new()
var move_dir:=Vector2.ZERO
var unsafe_attempts:=0
var has_path:=true
var events:Array[Dictionary]=[]
var dash_safety:Callable
var lunge_safety:Callable
var teleport_safety:Callable
var command_handler:Callable

static func rotate_direction(direction:Vector2,angle:float)->Vector2:
	var half:=C.f32(C.f32(angle)*C.f32(0.008726646259971648))
	var z:=C.f32(sin(half));var w:=C.f32(cos(half))
	var twice:=C.f32(z*2.0)
	# Unity's managed Quaternion * Vector3 rounds its intermediate locals, but
	# keeps the diagonal subtraction and output expression wide until storage.
	var diagonal:=1.0-C.f32(z*twice);var cross_term:=C.f32(w*twice)
	return Vector2(C.f32(diagonal*direction.x-cross_term*direction.y),C.f32(cross_term*direction.x+diagonal*direction.y))

func emit(kind:String,vector:=Vector2.ZERO)->void:
	events.append({"kind":kind,"vector":[vector.x,vector.y]})
	if command_handler.is_valid():command_handler.call(kind,vector)

func safe_dash(own:Dictionary,direction:Vector2,avoid:=true,outside:=true,hazard:=true)->bool:
	return dash_safety.call(direction,avoid,outside,hazard) if dash_safety.is_valid() else own.dash_safe
func safe_lunge(own:Dictionary,direction:Vector2)->bool:
	return lunge_safety.call(direction) if lunge_safety.is_valid() else own.lunge_safe
func safe_teleport(own:Dictionary)->bool:
	return teleport_safety.call() if teleport_safety.is_valid() else own.teleport_safe
func rotate(own:Dictionary,direction:Vector2)->void:
	own.last_rotation=direction
	var angle:=C.f32(C.f32(atan2(direction.y,direction.x))*C.f32(57.29578)-90.0)
	var up:=B.rotate(Vector2(0,1),angle)
	own.forward=Vector3(up.x,up.y,0.0)
	emit("rotate",direction)
func teleport()->void:
	emit("teleport");emit("create_path")

func block(own:Dictionary,target:Dictionary)->void:
	var difficulty:int=targeting.policy.difficulty
	if own.aiming or own.retrieve_charge>0.0:
		if difficulty>P.Difficulty.MEDIUM and P.dot2(P.direction(own.position,target.position),own.forward)>C.f32(0.4):
			move_dir=P.direction(own.position,target.position);rotate(own,move_dir)
		emit("stop_hold")
	if targeting.chance(float(difficulty)*40.0) and own.teleport and not own.thrown_positions.is_empty() and safe_teleport(own) and T.magnitude(target.position-own.thrown_positions[0])>5.0:teleport()
	if P.dot2(P.direction(own.position,target.position),own.forward)>C.f32(-0.1):
		if own.discs>0 and (targeting.chance(80.0) or not safe_dash(own,own.last_rotation)):
			if safe_lunge(own,move_dir):emit("button_attack")
		elif own.teleport and own.thrown_positions.is_empty() and safe_teleport(own):teleport()
		elif not own.dashing and dash(own,Vector2(own.forward.x,own.forward.y)):pass
		else:emit("find_safe_dash")
	elif targeting.chance(95.0):
		if own.teleport and own.thrown_positions.is_empty() and safe_teleport(own):teleport()
		elif safe_dash(own,Vector2(own.forward.x,own.forward.y)):emit("button_dash")

func attack(own:Dictionary,victim:Dictionary)->void:
	var probability:=100.0
	if targeting.policy.difficulty==P.Difficulty.EASY:
		probability=90.0
		if own.discs==0:probability=C.f32(probability*C.f32(0.1))
	if own.discs==2 and targeting.chance(70.0):emit("button_throwdisc")
	elif not safe_lunge(own,move_dir) and not victim.is_empty():throw_at(own,victim)
	elif targeting.chance(probability):emit("button_attack")

func throw_at(own:Dictionary,target:Dictionary)->void:
	var difficulty:int=targeting.policy.difficulty
	var probability:float=[80.0,90.0,95.0,100.0][difficulty]
	if targeting.chance(probability) or own.discs>1:
		var direction:=P.direction(own.position,targeting.target_after_throw(own,target))
		if difficulty==P.Difficulty.EASY:
			if targeting.chance(66.0):direction=rotate_direction(direction,targeting.policy.rng.range_float(-30.0,30.0))
			else:direction=Vector2(own.forward.x,own.forward.y)
		if difficulty==P.Difficulty.MEDIUM:direction=Vector2(C.f32((own.forward.x+direction.x)*0.5),C.f32((own.forward.y+direction.y)*0.5))
		rotate(own,direction)
		if own.aiming:emit("stop_hold")
		else:emit("button_throwdisc")
		if own.fire:emit("divert_after_throw")
	elif safe_dash(own,own.last_rotation,true,false,false) and targeting.chance(75.0 if own.get("has_parent",false) else 25.0):emit("button_dash")

func dash(own:Dictionary,direction:Vector2,avoid:=true,outside:=true,hazard:=true,track:=true)->bool:
	if own.teleport and safe_teleport(own):
		teleport();unsafe_attempts=0;return true
	if own.can_dash and safe_dash(own,direction,avoid,outside,hazard):
		rotate(own,direction);move_dir=direction;own.input_move=direction;has_path=false
		emit("button_dash");unsafe_attempts=0;return true
	unsafe_attempts+=1
	if track and unsafe_attempts>7:emit("find_safe_dash")
	return false
