class_name OriginalPlayerAiTargeting
extends RefCounted
## Original FFA target selection. Array order, short-circuit random calls and
## the distinct current/predicted line-of-sight positions are intentional.
const C=preload("res://core/original_timekeeper.gd")
const P=preload("res://core/original_player_ai_policy.gd")
var policy:=P.new()
var line_of_sight:Callable
var block_radius_players:=2.0
var block_radius_discs:=10.0
var attack_radius:=5.0
var throw_radius:=34.0
var throw_radius_aimed:=41.0
var throw_min_dot:=C.f32(0.85)
const FLICKING_SWITCH=6

static func magnitude(value:Vector3)->float:
	return C.f32(sqrt(value.x*value.x+value.y*value.y+value.z*value.z))

static func normalized2(value:Vector2)->Vector2:
	var length:=P.magnitude2(value)
	return Vector2(C.f32(value.x/length),C.f32(value.y/length)) if length>0.00001 else Vector2.ZERO

func sight(own:Dictionary,point:Vector3)->bool:
	return line_of_sight.call(point) if line_of_sight.is_valid() else own.get("los",true)

func chance(percent:float)->bool:
	return policy.rng.range_int(0,100)<=percent

func best_block(own:Dictionary,players:Array,discs:Array)->Dictionary:
	var selected:Dictionary={}
	var distance:=block_radius_players
	for target:Dictionary in players:
		if target.id!=own.id and magnitude(target.position-own.position)<=distance and target.alive and target.opponent and sight(own,target.position) and policy.can_see_target(target):
			distance=magnitude(target.position-own.position);selected=target
	distance=block_radius_discs
	for disc:Dictionary in discs:
		if magnitude(disc.position-own.position)<=distance and disc.owner_exists and not disc.owner_self and disc.movement!="Dropped" and disc.movement!="Squished" and disc.opponent and sight(own,disc.position) and (P.dot2(normalized2(Vector2(disc.velocity.x,disc.velocity.y)),own.position-disc.position)>0.75 or disc.movement=="Controlled"):
			distance=magnitude(disc.position-own.position);selected=disc
	return selected

func best_attack(own:Dictionary,players:Array,decoys:Array,switches:Array,state:int)->Dictionary:
	var selected:Dictionary={}
	var distance:=attack_radius
	for target:Dictionary in players:
		if target.id!=own.id and not target.since_teleport<policy.teleport_threshold and magnitude(target.position-own.position)<=distance and P.dot2(P.direction(own.position,target.position),own.forward)>C.f32(0.2) and target.alive and target.opponent and policy.can_see_target(target) and (sight(own,target.position) or chance(40.0)):
			distance=magnitude(target.position-own.position);selected=target
	if policy.difficulty<P.Difficulty.HARD:
		for target:Dictionary in decoys:
			if target.owner_exists and not target.owner_self and magnitude(target.position-own.position)<=distance and P.dot2(P.direction(own.position,target.position),own.forward)>C.f32(0.2) and target.alive and target.opponent and (sight(own,target.position) or chance(40.0)):
				distance=magnitude(target.position-own.position);selected=target
	if not selected.is_empty():return selected
	# Chance is consumed even while flicking a switch: preserve operand order.
	if own.completed_tutorial and chance(60.0) and state!=FLICKING_SWITCH:return {}
	distance=attack_radius*0.75
	for target:Dictionary in switches:
		if magnitude(target.position-own.position)<=distance and P.dot2(P.direction(own.position,target.position),own.forward)>0.5 and sight(own,target.position) and target.active:
			distance=magnitude(target.position-own.position);selected=target
	return selected

func best_throw(own:Dictionary,players:Array,decoys:Array)->Dictionary:
	if own.discs==0:return {}
	var selected:Dictionary={}
	var dot_threshold:=throw_min_dot
	if policy.difficulty==P.Difficulty.EASY:dot_threshold=C.f32(dot_threshold-C.f32(0.3))
	elif policy.difficulty==P.Difficulty.MEDIUM:dot_threshold=C.f32(dot_threshold-C.f32(0.15))
	if own.explosive or own.multi or own.teleport or own.discs>1:dot_threshold=C.f32(dot_threshold*0.75)
	var distance:=throw_radius
	if policy.difficulty>P.Difficulty.MEDIUM and own.aim==own.aim_max:distance=throw_radius_aimed
	for target:Dictionary in players:
		if target.id!=own.id and not target.since_teleport<policy.teleport_threshold and magnitude(target.position-own.position)<=distance and P.dot2(P.direction(own.position,target.position),own.forward)>dot_threshold and target.alive and target.opponent and policy.can_see_target(target) and sight(own,target_after_throw(own,target)):
			dot_threshold=P.dot2(P.direction(own.position,target.position),own.forward);selected=target
	if policy.difficulty<P.Difficulty.HARD:
		for target:Dictionary in decoys:
			if target.owner_exists and not target.owner_self and magnitude(target.position-own.position)<=distance and P.dot2(P.direction(own.position,target.position),own.forward)>dot_threshold and target.alive and target.opponent and sight(own,target_after_throw(own,target)):
				dot_threshold=P.dot2(P.direction(own.position,target.position),own.forward);selected=target
	return selected

func target_after_throw(own:Dictionary,target:Dictionary)->Vector3:
	if not target.body:return target.position
	var lead:=C.f32(0.74)
	if policy.difficulty==P.Difficulty.EASY:lead*=0.0
	elif policy.difficulty==P.Difficulty.MEDIUM:lead*=C.f32(0.47)
	elif policy.difficulty==P.Difficulty.HARD:lead*=C.f32(0.9)
	lead=C.f32(lead)
	var eased:=C.ease_out(C.f32(magnitude(target.position-own.position)/C.f32(27.32)))
	var offset:=Vector2(C.f32(target.velocity.x*lead),C.f32(target.velocity.y*lead))
	offset=Vector2(C.f32(offset.x*eased),C.f32(offset.y*eased))
	return Vector3(C.f32(target.position.x+offset.x),C.f32(target.position.y+offset.y),0.0)
