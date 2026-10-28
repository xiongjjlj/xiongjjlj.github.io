class_name OriginalDecoyAI
extends RefCounted
## AI + DecoyAI. Engine queries and asynchronous path delivery are explicit.
const C=preload("res://core/original_timekeeper.gd")
const A=preload("res://core/original_actor_motion.gd")
const B=preload("res://core/original_level_boundary.gd")
const R=preload("res://core/original_unity_random.gd")
const N=preload("res://core/original_navigation.gd")
enum AIState { WANDERING,COLLECTING,ATTACKING,FLEEING,DIVERTING,RETRIEVING,FLICKING_SWITCH,IDLE,DEAD,SEEKING }
var actor:OriginalActorMotion
var decoy_input:RefCounted
var boundary:OriginalLevelBoundary
var rng:RefCounted
var path_provider:Callable
var point_hazard:Callable
var touching_hazard:Callable
var dash_safety:Callable
var same_moving_floor:Callable
var avoidance_query:Callable
var fire_query:Callable
var forward_override:=Vector2.ZERO
var has_parent:=false
var state:=AIState.DEAD # Original AI.Awake initializes StateMachine in Dead.
var last_state:=AIState.WANDERING
var target:=Vector3.ZERO
var move_dir:=Vector2.ZERO
var path:Array[Vector3]=[]
var path_floor:Dictionary={}
var has_path:=false
var current_waypoint:=0
var path_length:=0.0
var reached_end:=true
var recalculating:=false
var pending_result:Dictionary={}
var create_after_dash:=false
var create_timer:=0.0
var scan_timer:=0.0
var scan_delay:=C.f32(0.3)
var action_timer:=0.0
var action_delay:=C.f32(0.2)
var input_timer:=0.0
var reluctance:=0.0
var reluctance_default:=C.f32(0.7)
var reluctance_cooldown:=0.0
var move_input_speed:=20.0
var diverting_distance:=3.0
var dash_distance:=C.f32(11.06)
var next_waypoint_distance:=C.f32(1.5)
var hard_difficulty:=false
var unsafe_attempts:=0
var frame_number:=0
var release_frame:=-1
var playing:=true
var events:Array[Dictionary]=[]
var snaps:=0

func position3()->Vector3:return Vector3(actor.position.x,actor.position.y,actor.source_z)
func waypoint(index:int)->Vector3:return OriginalMovingFloors.path_point(path_floor,path[index])
func forward()->Vector2:
	if forward_override!=Vector2.ZERO:return forward_override
	return Vector2(C.f32(-sin(actor.rotation_z)),C.f32(cos(actor.rotation_z)))
static func normalized3(p:Vector3)->Vector3:
	var length:=N.magnitude(p)
	return p/length if length>0.00001 else Vector3.ZERO
static func lerp2(a:Vector2,b:Vector2,t:float)->Vector2:
	return Vector2(C.lerp32(a.x,b.x,t),C.lerp32(a.y,b.y,t))
static func angle_from_vector(p:Vector2)->float:
	# VectorExt intentionally passes x before y; preserve that source convention.
	return C.f32(C.f32(atan2(p.x,p.y))*C.f32(57.29578))
static func vector_from_angle(degrees:float)->Vector2:
	var angle:=C.f32(degrees*C.f32(C.f32(PI)/180.0))
	return Vector2(C.f32(cos(angle)),C.f32(sin(angle)))

func initialize()->void:
	release_frame=-1
	reached_end=true
	has_path=false
	move_dir=Vector2.ZERO
	create_path()
	change_state(AIState.WANDERING)

func set_random_target()->void:
	var best:=-1.0
	for i in 3:
		var candidate:=Vector2(rng.range_float(C.f32(-boundary.size.x*0.5+2.0),C.f32(boundary.size.x*0.5-2.0)),rng.range_float(C.f32(-boundary.size.y*0.5+2.0),C.f32(boundary.size.y*0.5-2.0)))
		candidate=B.rotate(candidate,boundary.angle)
		var direction:=A.normalized(candidate-actor.position)
		var up:=forward()
		var dot:=C.f32(direction.x*up.x+direction.y*up.y)
		if dot>best:
			target=Vector3(candidate.x,candidate.y,0)
			best=dot

func change_state(next:int)->void:
	if state==next:return
	if state>=0:last_state=state
	state=next
	match state:
		AIState.WANDERING:
			set_random_target()
			create_path()
			reluctance=C.f32(1.25)
			reluctance_cooldown=C.f32(1.5)
		AIState.DIVERTING:
			create_path()
			reluctance=2.0
			reluctance_cooldown=1.0
	events.append({"type":"state","state":state,"last":last_state})

func create_path()->void:
	if not playing or create_timer<C.f32(0.01666):return
	create_timer=0.0
	if N.magnitude(position3()-target)<next_waypoint_distance:
		reached_end=true
		return
	if state not in [AIState.WANDERING,AIState.COLLECTING,AIState.ATTACKING,AIState.DIVERTING,AIState.FLICKING_SWITCH,AIState.SEEKING] or not path_provider.is_valid():return
	var result:Dictionary=path_provider.call(position3(),target)
	pending_result=result
	recalculating=true
	reached_end=false
	events.append({"type":"path_requested","from":position3(),"to":target})

func deliver_path_result()->void:
	# Called in the AstarPath phase (order 0), after the AI/FSM updates.
	if not recalculating:return
	recalculating=false
	if pending_result.get("error","")=="":
		path.assign(pending_result.smoothed)
		path_floor=pending_result.get("moving_floor",{})
		has_path=true
		current_waypoint=0
		path_length=pending_result.total_length
	pending_result={}

func random_reaction()->float:
	return C.f32([0.1,0.2,0.3][rng.range_int(0,3)])

func ai_update(delta:float,is_playing:bool)->void:
	frame_number+=1
	playing=is_playing
	if not playing:return
	if actor.state==A.State.DEAD:
		has_path=false
		change_state(AIState.DEAD)
		return
	delta=C.f32(delta)
	# DecoyAI.UpdateBehaviour executes at -5; StateMachineRunner is -4.
	dash_distance=C.f32(11.06)
	diverting_distance=3.0
	move_input_speed=20.0
	if scan_timer>scan_delay and state!=AIState.ATTACKING:
		scan_timer=0.0
		scan_delay=random_reaction()
		if hard_difficulty:scan_delay=C.f32(scan_delay*3.0)
	scan_timer=C.f32(scan_timer+delta)
	action_timer=C.f32(action_timer+delta)
	create_timer=C.f32(create_timer+delta)
	if input_timer<=C.f32(0.15):input_timer=C.f32(input_timer+delta)
	if reluctance_cooldown>0.0:reluctance_cooldown=C.f32(reluctance_cooldown-delta)
	else:reluctance=reluctance_default
	if actor.is_dashing:create_after_dash=true
	elif create_after_dash:
		create_path()
		create_after_dash=false
	if action_timer>action_delay:
		action_timer=0.0
		action_delay=random_reaction()
	# Decoy's inherited interest/action handlers do not choose threats or attack.
	decoy_input.input_move=move_dir

func state_update(delta:float)->void:
	if not playing:return
	match state:
		AIState.WANDERING:
			if reached_end:
				set_random_target()
				create_path()
			move_on_path(delta,true)
		AIState.DIVERTING:
			move_on_path(delta,false)
			if reached_end:change_state(last_state)

func move_on_path(delta:float,wandering:=true)->void:
	if actor.is_aiming:return
	move_dir=lerp2(actor.input_move,Vector2.ZERO,C.f32(move_input_speed*(1.0 if state==AIState.FLEEING else 2.0)*delta))
	if not has_path:
		if not recalculating:reached_end=true
		return
	var threshold:=C.f32(next_waypoint_distance*(2.0 if actor.on_wet_floor else 1.0))
	while current_waypoint<path.size() and N.magnitude(position3()-waypoint(current_waypoint))<threshold:
		current_waypoint+=1
		if current_waypoint==path.size()-1 and is_point_hazard(waypoint(current_waypoint)):
			# Chance.PercentageTrue uses integer Random.Range(0,100) <= threshold.
			if rng.range_int(0,100)<=25 and has_parent:change_state(AIState.IDLE)
			else:current_waypoint=path.size()
	if current_waypoint>=path.size():
		has_path=false
		reached_end=true
		move_dir=Vector2.ZERO
		if wandering:dash_across_hazards()
		elif not actor.is_dashing and touches_hazard(position3()+Vector3(forward().x,forward().y,0)) and N.magnitude(target-position3())>5.0:
			dash(forward(),true,true,false)
	else:
		var direction:=normalized3(waypoint(current_waypoint)-position3())
		move_dir=lerp2(actor.input_move,Vector2(direction.x,direction.y),C.f32(move_input_speed*delta))
		if not actor.is_dashing and avoidance_query.is_valid():
			var point:Variant=avoidance_query.call(move_dir,diverting_distance)
			if point is Vector3:divert_path(point)
		if not actor.is_dashing and not actor.is_invulnerable and fire_query.is_valid():
			var fire_point:=position3()+Vector3(forward().x,forward().y,0)*C.f32(3.0*C.f32(1.33))
			if fire_query.call(fire_point):divert_path(fire_point)

func divert_path(avoid_position:Vector3)->void:
	if last_state==AIState.DIVERTING:return
	var to_avoid:=avoid_position-position3()
	var up:=forward()
	var cross_z:=C.f32(up.x*to_avoid.y-up.y*to_avoid.x)
	var angle:float=rng.range_float(40.0,70.0)
	if cross_z<0.0:angle=-angle
	var rotated:=B.rotate(Vector2(to_avoid.x,to_avoid.y),angle)+actor.position
	target=Vector3(rotated.x,rotated.y,avoid_position.z)
	target+=normalized3(target-position3())*rng.range_float(3.0,6.0)
	change_state(AIState.DIVERTING)

func is_point_hazard(point:Vector3)->bool:
	return bool(point_hazard.call(point)) if point_hazard.is_valid() else false
func touches_hazard(point:Vector3)->bool:
	return bool(touching_hazard.call(point)) if touching_hazard.is_valid() else false

func dash(direction:Vector2,avoid_obstacles:=true,allow_outside:=true,allow_hazard_end:=true,track_failures:=true)->bool:
	if actor.can_dash and (not dash_safety.is_valid() or dash_safety.call(direction,avoid_obstacles,allow_outside,allow_hazard_end)):
		actor.last_rotation=direction
		snaps+=1
		actor.last_input_move=direction
		var degrees:=C.f32(C.f32(atan2(direction.y,direction.x))*C.f32(57.29578)-90.0)
		actor.rotation_z=degrees*PI/180.0
		move_dir=direction
		actor.input_move=direction
		has_path=false
		decoy_input.input_move=direction
		input_timer=0.0
		decoy_input.input_dash=true
		release_frame=frame_number+1
		return true
	unsafe_attempts+=1
	if track_failures and unsafe_attempts>7:find_safe_dash(angle_from_vector(forward()))
	return false

func dash_across_hazards()->bool:
	if actor.is_dashing:return false
	var direction:=A.normalized(forward())
	if not touches_hazard(position3()+Vector3(direction.x,direction.y,0)):return false
	if rng.range_int(0,100)<=90:return false
	if N.magnitude(target-position3())<=5.0:return false
	return dash(direction,false,false,false)

func force_dash(direction:Vector2)->void:
	find_safe_dash(angle_from_vector(direction))

func find_safe_dash(starting_angle:float)->void:
	if actor.is_dashing:return
	unsafe_attempts=0
	rng.range_float(0.0,360.0) # Source consumes this value without using it.
	for i in 16:
		var direction:=vector_from_angle(C.f32(22.5*i+starting_angle))*C.f32(9.45)
		var endpoint:=position3()+Vector3(direction.x,direction.y,0)
		if is_point_hazard(endpoint):continue
		if has_parent and same_moving_floor.is_valid() and same_moving_floor.call(endpoint):continue
		if dash(direction,false,true,true,false):break

func coroutine_phase()->void:
	if release_frame>=0 and frame_number>=release_frame:
		decoy_input.input_dash=false
		release_frame=-1
