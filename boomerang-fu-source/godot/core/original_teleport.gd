class_name OriginalTeleport
extends RefCounted
## Player.Teleport / Player.Dash teleport branch. Cosmetic service calls are
## recorded separately; RNG stream parity and the full Animator remain open.
const C = preload("res://core/original_timekeeper.gd")
var inventory: WeakRef
var count := 0
var last_timestamp := 0.0
var events: Array[Dictionary] = []
var selected_index := -1
var forced_index := -1 # Native-oracle replay only; never set by the game.

func initialize(weapon: OriginalPlayerWeapons) -> void:
	inventory = weakref(weapon)

func over_hazard(point: Vector2) -> bool:
	var weapon: OriginalPlayerWeapons = inventory.get_ref()
	var hook := weapon.actor.environment_hook
	return hook == null or hook.get_ref() == null or hook.get_ref().over_hazard(point)

func is_safe() -> bool:
	var weapon: OriginalPlayerWeapons = inventory.get_ref()
	for disc in weapon.owned_discs:
		if not over_hazard(disc.position): return true
	return false

func attempt(current_time: float) -> bool:
	var weapon: OriginalPlayerWeapons = inventory.get_ref()
	var actor := weapon.actor
	selected_index = -1
	if weapon.owned_discs.is_empty() or (weapon.powers.active & 128) == 0: return false
	# Unity's integer Random.Range excludes its maximum: the source passes N-1.
	selected_index = forced_index if forced_index >= 0 else randi_range(0,maxi(0,weapon.owned_discs.size()-2))
	var disc: OriginalDiscMotion = weapon.owned_discs[selected_index]
	var offset := Vector3(disc.position.x-actor.position.x,disc.position.y-actor.position.y,disc.source_z-actor.source_z)
	var distance := C.f32(sqrt(C.f32(C.f32(C.f32(offset.x*offset.x)+C.f32(offset.y*offset.y))+C.f32(offset.z*offset.z))))
	if distance < C.f32(actor.radius*3.0):
		if weapon.world.circle_cast(actor.position,0.0,Vector2(offset.x,offset.y),distance,256).is_empty(): return true
	if over_hazard(disc.position):
		if weapon.owned_discs.size() == 1: return false
		var found := false
		for candidate in weapon.owned_discs:
			if not over_hazard(candidate.position) and candidate.movement != OriginalDiscMotion.Movement.SQUISHED:
				disc = candidate
				found = true
				break
		if not found: return false
	var destination := disc.position
	if weapon.collision_receiver != null and weapon.collision_receiver.get_ref() != null:
		weapon.collision_receiver.get_ref().telefrag(actor,destination,C.f32(actor.radius+C.f32(0.01)))
	if (disc.disc_powerups & 32) == 0:
		disc.can_collide_with_owner = true
		disc.can_be_picked_up = true
	else:
		# Disc.Explode removes the projectile and requests a replacement. Grenade
		# damage is a separate source method, not invoked here by Teleport.
		if not disc.is_mini_disc: weapon.respawn_requests.append(C.f32(0.25))
		disc.recycle()
		if disc.is_mini_disc and weapon.powers.pending_discs > 0:
			weapon.powers.pending_discs -= 1
			weapon.respawn_requests.append(C.f32(1.5))
	var origin := actor.position
	if actor.environment_hook != null and actor.environment_hook.get_ref() != null:
		var environment = actor.environment_hook.get_ref()
		environment.parent_kind = 0
		environment.parent_shape_id = -1
		environment.parent_velocity = Vector2.ZERO
	actor.position = destination
	if actor.elements_hook != null and actor.elements_hook.get_ref() != null:
		var grid = actor.elements_hook.get_ref().grid
		if (weapon.powers.active&256)!=0:grid.extinguish(destination,OriginalCellGrid.CellType.FIRE,10)
		if (weapon.powers.active&1024)!=0:grid.extinguish(destination,OriginalCellGrid.CellType.ICE,10)
	var picked := false
	if disc.can_be_picked_up:
		picked = weapon.collect_mini(disc) if disc.is_mini_disc else weapon.pick_up_disc(disc)
	actor.break_disguise()
	actor.retrieve_charge = 0.0
	if actor.is_dashing: actor.stop_dash(true)
	weapon.attack_cooldown = 0.0
	count += 1
	last_timestamp = C.f32(current_time)
	events.append({"from":origin,"to":destination,"picked":picked,"fire":(weapon.powers.active&256)!=0,"ice":(weapon.powers.active&1024)!=0})
	return true

func dash(current_time: float) -> bool:
	var weapon: OriginalPlayerWeapons = inventory.get_ref()
	var actor := weapon.actor
	if actor.state == OriginalActorMotion.State.FALLING and actor.fall_timer > C.f32(0.33): return false
	if not actor.is_stunned and not actor.is_showing_off and not actor.is_lunging and attempt(current_time):
		if actor.state == OriginalActorMotion.State.FALLING and actor.environment_hook != null:
			actor.environment_hook.get_ref().fall_saved()
		return true
	return actor.dash()
