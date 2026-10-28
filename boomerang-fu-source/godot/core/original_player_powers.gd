class_name OriginalPlayerPowers
extends RefCounted
## Player.StartPowerup and its inventory/timer lifecycle. Effects requiring
## unported world systems are emitted separately, never silently simulated.
const C = preload("res://core/original_timekeeper.gd")
enum Power { NONE=0, MOVE_FASTER=2, DASH_THROUGH_WALLS=4, REVERSE_INPUTS=8,
	SHIELD=16, EXPLOSIVE_DISC=32, MULTI_DISC=64, TELEPORT_DISC=128,
	FIRE_DISC=256, EXTRA_DISC=512, ICE_DISC=1024, DISGUISE=2048,
	TIME_IMMUNE=4096, CONTROLLED_DISC=8192, SHRINKING_BOUNDS=16384, DECOY=32768 }
var actor: OriginalActorMotion
var inventory: WeakRef
var active := 0
var history: Array[int] = []
var maximum := 3
var pending_discs := 0
var shield_hits := 0
var reverse_timer := 0.0
var reverse_limit := 0.0
var reverse_running := false
var reverse_fill := 0.0
var time_immune_timer := 0.0
var invulnerability_time := -1.0
var invulnerability_visible := true
var events: Array[Dictionary] = []

func initialize(motion: OriginalActorMotion, weapon: RefCounted) -> void:
	actor = motion
	inventory = weakref(weapon)

func has_power(power: int) -> bool:
	return (active & power) == power

func start(power: int) -> void:
	var already := has_power(power)
	match power:
		Power.MOVE_FASTER, Power.DASH_THROUGH_WALLS, Power.EXPLOSIVE_DISC, Power.MULTI_DISC, Power.TELEPORT_DISC, Power.DISGUISE, Power.CONTROLLED_DISC, Power.DECOY:
			if not already:
				active |= power
				history.append(power)
			if power == Power.DECOY: events.append({"type": "decoy_enabled"})
		Power.FIRE_DISC, Power.ICE_DISC:
			if not already:
				active |= power
				var opposite := Power.ICE_DISC if power == Power.FIRE_DISC else Power.FIRE_DISC
				if has_power(opposite):
					history.erase(opposite)
					active &= ~opposite
				history.append(power)
		Power.REVERSE_INPUTS:
			active |= power
			reverse_limit = 6.0
			reverse_timer = 6.0
			reverse_running = true
		Power.SHIELD:
			if not already:
				history.append(power)
				start_shield()
		Power.EXTRA_DISC:
			var weapon = inventory.get_ref()
			if pending_discs + weapon.held_discs < 2:
				weapon.cooling_attack = true
				weapon.attack_cooldown = C.f32(0.05 if has_power(Power.MOVE_FASTER) or weapon.held_discs > 0 else 0.1)
				weapon.held_discs += 1
				pending_discs = mini(pending_discs, 2 - weapon.held_discs)
			if not already:
				active |= power
				history.append(power)
		Power.TIME_IMMUNE:
			active |= power
			time_immune_timer = 1.5
			events.append({"type": "time_warp", "hold": C.f32(6.3), "out": C.f32(0.2), "back": 1.0, "scale": C.f32(0.4)})
		Power.SHRINKING_BOUNDS:
			events.append({"type": "shrink_boundary", "position": actor.position})
	if not already and history.size() > maximum and not history.is_empty():
		active &= ~history.pop_front()
	# Source eviction clears the flag, not IsShielded. Preserve that distinction.
	sync_motion()
	events.append({"type": "power_collected", "power": power})

func sync_motion() -> void:
	actor.time_immune = has_power(Power.TIME_IMMUNE)
	actor.move_faster = has_power(Power.MOVE_FASTER)
	actor.dash_through_walls = has_power(Power.DASH_THROUGH_WALLS)
	actor.controlled_disc = has_power(Power.CONTROLLED_DISC)
	actor.reverse_inputs = has_power(Power.REVERSE_INPUTS)

func start_shield() -> void:
	active |= Power.SHIELD
	shield_hits = 1
	actor.is_shielded = true
	events.append({"type": "shield_started"})

func stop_shield() -> void:
	active &= ~Power.SHIELD
	history.erase(Power.SHIELD)
	shield_hits = 0
	actor.is_shielded = false

func damage_shield() -> void:
	actor.is_invulnerable = true
	invulnerability_time = 0.0
	shield_hits -= 1
	if shield_hits <= 0: stop_shield()
	events.append({"type": "shield_broken"})

func stop_temporary() -> void:
	actor.break_disguise()
	active &= ~(Power.REVERSE_INPUTS | Power.TIME_IMMUNE)
	reverse_timer = 0.0
	sync_motion()

func clear() -> void:
	stop_temporary()
	active = 0
	history.clear()
	sync_motion()

func round_snapshot() -> Dictionary:
	return {"active": active, "history": history.duplicate(), "shield_hits": shield_hits}

func restore_round(saved: Dictionary) -> void:
	active = saved.active
	history.assign(saved.history)
	shield_hits = saved.shield_hits
	stop_temporary()
	reverse_running = false
	time_immune_timer = 0.0
	actor.time_scaler = 1.0
	actor.is_shielded = has_power(Power.SHIELD)
	var weapon = inventory.get_ref()
	weapon.held_discs = 2 if has_power(Power.EXTRA_DISC) else 1
	weapon.attacks_left = weapon.held_discs

func frame_step(delta: float) -> void:
	if reverse_running:
		if reverse_timer > 0.0:
			reverse_fill = C.f32(reverse_timer / reverse_limit)
			reverse_timer = C.f32(reverse_timer - C.f32(delta) * actor.time_scaler)
		else:
			reverse_fill = 0.0
			reverse_timer = 0.0
			reverse_running = false
			active &= ~Power.REVERSE_INPUTS
	else:
		reverse_fill = 0.0
		reverse_timer = 0.0
	sync_motion()

func update_time_immune(delta: float, global_scale: float, target_scale: float, playing := true) -> void:
	# Player.Update runs after Actor.Update. The power timer consumes global
	# scaled time; personal scaling affects subsequent actor updates.
	if has_power(Power.TIME_IMMUNE) and playing:
		actor.time_scaler = C.f32(1.0 / global_scale) if global_scale > 0.0 else 1.0
		if time_immune_timer > 0.0:
			time_immune_timer = C.f32(time_immune_timer - C.f32(delta))
		elif C.f32(target_scale - global_scale) < C.f32(0.4):
			time_immune_timer = 0.0
			active &= ~Power.TIME_IMMUNE
			actor.time_scaler = 1.0
			events.append({"type":"time_immune_end"})
	else:
		actor.time_scaler = 1.0
	sync_motion()

func death_time_immune(clock: OriginalTimekeeper, others: Array) -> void:
	if not has_power(Power.TIME_IMMUNE) or not clock.slow_mo: return
	for other in others:
		# Source checks the flag even on another dead player.
		if other != self and other.has_power(Power.TIME_IMMUNE): return
	clock.reset_time_scale()

func unscaled_frame(delta: float, paused := false) -> void:
	# Player.TemporaryInvulnerability uses unscaled time and yields once even
	# after crossing duration. This covers its state, not coroutine scheduling.
	if invulnerability_time < 0.0: return
	if invulnerability_time < C.f32(0.85):
		if actor.state != OriginalActorMotion.State.ALIVE:
			invulnerability_visible = true
			invulnerability_time = -1.0
			return
		if not paused: invulnerability_time = C.f32(invulnerability_time + C.f32(delta))
		actor.break_disguise()
		var flash := C.f32(invulnerability_time * 40.0)
		var nearest := floorf(flash)
		# Mathf.Round delegates to Math.Round (ties to even).
		if flash - nearest > 0.5 or (flash - nearest == 0.5 and int(nearest) % 2 != 0): nearest += 1.0
		invulnerability_visible = int(nearest) % 2 == 1
	else:
		invulnerability_visible = true
		invulnerability_time = -1.0
		actor.is_invulnerable = false
		actor.just_became_vulnerable = true
