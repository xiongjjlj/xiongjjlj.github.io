class_name OriginalPlayerAiPolicy
extends RefCounted
## PlayerAI policy arithmetic for FFA kills. World facts and coroutine phases are
## supplied by the scene adapter; this class does not implement the full bot.
const C = preload("res://core/original_timekeeper.gd")
const R = preload("res://core/original_unity_random.gd")
enum Difficulty { EASY, MEDIUM, HARD, IMPOSSIBLE }
var difficulty := Difficulty.MEDIUM
var rng := R.new()
var reaction_times: Array[float] = []
var input_delay := 0.0
var bearings_duration := 0.0
var bearings_speed := 0.0
var bearings_timer := 0.0
var teleport_threshold := 0.0
var ambient := 1.0
var aggression := 1.0
var nervous := 1.0
var greedy := 1.0

static func product(values: Array) -> float:
	var result := 1.0
	for value in values: result *= value
	return C.f32(result)

static func magnitude2(value: Vector2) -> float:
	return C.f32(sqrt(value.x*value.x+value.y*value.y))

static func distance2(a: Vector3, b: Vector3) -> float:
	return magnitude2(Vector2(a.x-b.x,a.y-b.y))

static func direction(a: Vector3, b: Vector3) -> Vector2:
	var delta := b-a
	var squared := delta.x*delta.x+delta.y*delta.y+delta.z*delta.z
	var length := C.f32(sqrt(squared))
	return Vector2(C.f32(delta.x/length),C.f32(delta.y/length)) if length>0.00001 else Vector2.ZERO

static func dot2(a: Vector2,b: Vector3) -> float:
	return C.f32(a.x*b.x+a.y*b.y)

func set_difficulty(value: int) -> void:
	difficulty=value
	var times: Array = [[0.6,0.7,0.85],[0.41,0.52,0.63],[0.1,0.25,0.35],[0.07,0.13,0.2]][value]
	reaction_times.clear()
	for time in times: reaction_times.append(C.f32(time))
	input_delay=C.f32([0.3,0.25,0.2,0.1][value])
	bearings_duration=C.f32([8.0,4.0,2.0,0.1][value])
	teleport_threshold=C.f32([1.5,0.75,0.25,0.0][value])
	bearings_speed=1.0 if rng.range_int(0,100)<=50 else C.f32(0.05)
	bearings_timer=C.f32(bearings_duration*rng.range_float(0.75,1.0))

func random_reaction() -> float:
	var factor := C.f32(1.0+clampf(C.f32(bearings_timer/bearings_duration),0.0,1.0))
	return C.f32(reaction_times[rng.range_int(0,3)]*factor)

func randomize_modifiers(reverse: bool,retrieving: bool) -> void:
	var bounds: Array = [[1.0,2.0],[0.9,1.5],[0.8,1.2],[0.6,1.0]][difficulty]
	ambient=rng.range_float(bounds[0],bounds[1])
	aggression=rng.range_float(0.6,1.0)
	if difficulty>Difficulty.MEDIUM: aggression=C.f32(aggression+C.f32(0.3))
	nervous=rng.range_float(0.8,1.2)
	if difficulty==Difficulty.EASY: nervous=C.f32(nervous+C.f32(0.3))
	greedy=rng.range_float(0.8,1.2)
	if reverse:
		nervous=C.f32(nervous*1.5)
		ambient=C.f32(ambient*5.0)
	if retrieving: nervous=C.f32(nervous*1.25)

func can_see_target(target: Dictionary) -> bool:
	if target.is_empty(): return false
	if difficulty==Difficulty.IMPOSSIBLE: return true
	if target.get("disguised",false) or target.get("shadows",false):
		if target.get("golden_held",false): return true
		# Native Mono retains this expression's intermediate precision through
		# the comparison: at easy difficulty, a timer of exactly 5 is visible.
		var threshold := C.f32(0.2)*(5.0+float(difficulty))*5.0
		return float(target.get("speed",0.0))>6.0-float(difficulty)*2.0 or float(target.get("disguise_time",0.0))<threshold
	return not target.get("hidden",false)

func victim_interest(own: Dictionary,target: Dictionary,size: Vector2) -> float:
	if target.is_empty() or own.disarmed: return 0.0
	var disc_factor := 1.0
	if target.disarmed: disc_factor=C.f32(1.2)
	elif target.discs>own.discs: disc_factor=C.f32(0.8)
	var facing := minf(C.f32(dot2(direction(own.position,target.position),own.forward)*C.f32(0.2)+1.0),1.0)
	var sight := C.f32(1.1) if own.los else 1.0
	var visible := 1.0 if can_see_target(target) else 0.25
	var distance_factor := clampf(C.f32(1.5-distance2(own.position,target.position)/magnitude2(size)),0.0,2.0)
	var frozen := C.f32(1.3) if target.frozen else 1.0
	var lead := 1.0
	if target.leading: lead=C.f32(2.5-minf(C.f32(float(target.points_left)*C.f32(0.2)),1.25))
	return product([facing,sight,disc_factor,distance_factor,aggression,lead,visible,1.0,frozen])

func threat_interest(own: Dictionary,target: Dictionary,size: Vector2,kind:=0) -> float:
	var distance := distance2(own.position,target.position)
	if kind==1: return C.f32((6.0 if own.los and distance<17.0 else 1.0)*nervous)
	var distance_factor := maxf(C.f32(1.0-distance/magnitude2(size)*2.0),0.0)
	var facing := dot2(direction(own.position,target.position),own.forward)
	if kind==2:
		if distance_factor<0.5: return 0.0
		return product([-facing*0.5+0.5,distance_factor,nervous])
	var discs := 1.0
	if target.disarmed or target.frozen: discs=0.0
	elif own.disarmed: discs=5.25
	elif own.discs==0: discs=2.0
	elif target.discs<own.discs: discs=C.f32(0.8)
	var fire := 5.0 if target.on_fire and not own.on_fire else 1.0
	var facing_factor := -facing*C.f32(0.05)+C.f32(0.45)
	var visible := 1.0 if can_see_target(target) else 0.5
	var sight := 1.0 if own.los else C.f32(0.6)
	var aim := 2.0 if target.aiming and dot2(direction(target.position,own.position),target.forward)>0.25 else 1.0
	return product([facing_factor,sight,discs,distance_factor,nervous,aim,visible,fire])

func desire_interest(own: Dictionary,target: Dictionary,size: Vector2,kind:=0) -> float:
	var proximity := clampf(C.f32(1.25-distance2(own.position,target.position)/magnitude2(size)),0.0,1.0)
	if kind==0:
		if target.unlocked and target.reverse: return 0.0
		return product([proximity,1.5,greedy])
	if target.get("golden",false): return 100.0
	if target.explosive or target.burning or (target.owner==0 and target.hazard): return 0.0
	if own.discs+target.transit>1: return 0.0
	var factor := 1.0
	if target.owner==1: factor=4.0
	else:
		if target.owner==0 or target.can_steal:
			if own.disarmed: factor=4.0
			elif target.in_bounds: factor=3.0
			else:
				factor=0.5
				if absf(target.position.x)>40.0 or absf(target.position.y)>30.0: return 0.0
		elif target.owner!=0 and target.owner_dead: return 0.0
		if target.owner!=0 and not target.opponents: return 0.0
	return product([proximity,greedy,factor])
