class_name OriginalPlayerWeapons
extends RefCounted
## Normal-disc inventory, aiming and retrieval from Player/PlayerInput. Melee,
## damage, special powerups and match transitions are still integrated separately.
const C = preload("res://core/original_timekeeper.gd")
const V = preload("res://core/original_actor_motion.gd")
var actor: OriginalActorMotion
var world: OriginalCollisionWorld
var held_discs := 1
var attacks_left := 1
var disarms_left := 1
var attack_cooldown := 0.0
var cooling_attack := false
var aim_timer := 0.0
var aim_power := 0.0
var input_held := false
var input_blocked := false
var rotation_queue: Array[Vector2] = []
var owned_discs: Array[OriginalDiscMotion] = []
var spawned_discs: Array[OriginalDiscMotion] = []
var buffered_throw_time := -1.0
var display_position := Vector2.ZERO
var display_rotation := PI
var action_serial := 0
var action_hand := "right"
var powers := OriginalPlayerPowers.new()
var collision_receiver: WeakRef
var mini_discs_collected := 0
var mini_discs_required := 0
var ghost_discs := 0
var normal_catches := 0
var respawn_requests: Array[float] = []
var respawn_jobs: Array[Dictionary] = []
var replacement_pickups := 0
var teleport := OriginalTeleport.new()

func initialize(motion: OriginalActorMotion, queries: OriginalCollisionWorld) -> void:
	actor = motion
	world = queries
	actor.aim_cancel_hook = weakref(self)
	powers.initialize(actor, self)
	teleport.initialize(self)

func on_actor_stop_aiming() -> void:
	aim_timer = 0.0
	aim_power = 0.0
	rotation_queue.clear()
	if input_held:
		input_blocked = true
		input_held = false
	actor.retrieve_charge = 0.0

func fixed_step() -> void:
	if aim_timer > C.f32(C.f32(0.1) * 0.5):
		if rotation_queue.size() >= 10: rotation_queue.pop_front()
		rotation_queue.append(V.normalized(actor.last_input_move))

func process_input(held: bool, released: bool, dash_pressed: bool, delta: float, current_time: float, view_position: Vector2, view_rotation: float) -> void:
	display_position = view_position
	display_rotation = view_rotation
	input_held = held
	if actor.state != V.State.ALIVE: return
	if released and not input_blocked:
		if try_throw(): buffered_throw_time = -1.0
		elif owned_discs.is_empty(): buffered_throw_time = current_time
	elif released:
		input_blocked = false
	if input_held and not input_blocked: aim(delta)
	if released: actor.stop_aiming()
	if not released and not dash_pressed and buffered_throw_time >= 0.0:
		if C.f32(current_time - buffered_throw_time) > C.f32(0.35): buffered_throw_time = -1.0
		elif not input_blocked and owned_discs.is_empty() and try_throw(): buffered_throw_time = -1.0

func aim(delta: float) -> void:
	actor.break_disguise()
	delta = C.f32(delta)
	if held_discs > 0 and actor.retrieve_charge == 0.0 and not actor.is_frozen:
		aim_timer = C.f32(aim_timer + C.f32(delta * actor.time_scaler))
	if aim_timer >= C.f32(0.1):
		actor.is_aiming = true
		var t := clampf(C.f32(C.f32(aim_timer - C.f32(0.1)) / C.f32(0.9)), 0.0, 1.0)
		aim_power = clampf(C.lerp32(0.0, 1.0, C.ease_in(t)), 0.0, 1.0)
	if aim_timer == 0.0 and not actor.is_dashing and not actor.is_lunging and not owned_discs.is_empty() and held_discs < 2:
		if actor.retrieve_charge < 1.4:
			actor.retrieve_charge = C.f32(actor.retrieve_charge + C.f32(C.f32(delta / 4.0) * actor.time_scaler))
	elif (owned_discs.is_empty() or held_discs > 1) and actor.retrieve_charge > 0.0:
		actor.retrieve_charge = 0.0
		if input_held:
			input_blocked = true
			input_held = false

func try_throw() -> bool:
	actor.break_disguise()
	if attacks_left <= 0 or actor.is_stunned or actor.is_showing_off or actor.is_frozen or held_discs <= 0 or actor.retrieve_charge != 0.0: return false
	attacks_left -= 1
	var up := Vector2(-sin(display_rotation), cos(display_rotation))
	var spawn_position := display_position + up * C.f32(0.75)
	var hit := world.circle_cast(display_position - up * C.f32(0.1), C.f32(0.7125), up, 1.5, 256)
	if not hit.is_empty(): spawn_position = hit.centroid - V.normalized(hit.point - display_position) * C.f32(0.1)
	var disc := OriginalDiscMotion.new()
	disc.position = spawn_position
	disc.source_z = -0.75 # ThrowDisc casts spawn position to Vector2 first.
	disc.owner_actor = actor
	disc.world = world
	disc.throw_direction = V.normalized(actor.last_input_move) if actor.last_input_move.length() > 0.0 else up
	if not actor.can_walk: disc.throw_direction = up
	disc.aim_power = aim_power
	disc.disc_powerups = powers.active
	disc.time_scaler = actor.time_scaler
	if not rotation_queue.is_empty():
		var current := V.normalized(actor.last_input_move)
		var old: Vector2 = rotation_queue[0]
		var denominator := C.f32(sqrt(C.f32(current.length_squared() * old.length_squared())))
		var angle := 0.0
		if denominator >= 1e-15:
			angle = C.f32(C.f32(acos(clampf(C.f32(OriginalDiscMotion.dot32(current, old) / denominator), -1.0, 1.0))) * C.f32(57.29578))
		if C.f32(C.f32(current.x * old.y) - C.f32(current.y * old.x)) < 0.0: angle = -angle
		disc.spin_angle = clampf(angle, -90.0, 90.0)
	disc.owner_inventory = weakref(self)
	if collision_receiver != null and collision_receiver.get_ref() != null:
		collision_receiver.get_ref().bind_disc(disc)
	disc.initialize()
	if disc.is_an_explosive(): disc.grenade_active = true
	spawned_discs.append(disc)
	held_discs -= 1
	return true

func try_attack() -> bool:
	if actor.state != V.State.ALIVE or actor.is_stunned or actor.is_showing_off or actor.is_aiming: return false
	if attacks_left <= 0 and (held_discs > 0 or disarms_left <= 0): return false
	actor.break_disguise()
	if actor.is_frozen:
		if actor.elements_hook != null and actor.elements_hook.get_ref() != null: actor.elements_hook.get_ref().chip()
		return true
	if actor.dash_through_walls and world.overlap_circle(actor.position,C.f32(0.3),256): return false
	if held_discs > 0: actor.retrieve_charge = 0.0
	cooling_attack = true
	attack_cooldown = C.f32(0.66)
	actor.begin_attack()
	if held_discs > 0:
		disarms_left = 0
		actor.is_attacking = true
		action_hand = "right" if attacks_left == 1 else "left"
		attacks_left -= 1
	else:
		actor.lunge_cooldown_timer = C.f32(actor.lunge_cooldown_timer * C.f32(0.85))
		actor.is_disarming = true
		disarms_left -= 1
		attacks_left = 0
		action_hand = "disarm"
	action_serial += 1
	return true

func drop_disc(direction: Vector2) -> OriginalDiscMotion:
	if held_discs == 0: return null
	var distance := C.f32(0.75)
	var normal := V.normalized(direction)
	var hit := world.circle_cast(actor.position - normal * C.f32(0.1), C.f32(0.7125), normal, distance, 256)
	if not hit.is_empty(): distance = C.f32((hit.centroid - actor.position).length() - C.f32(0.25))
	var disc := OriginalDiscMotion.new()
	disc.position = actor.position + normal * distance
	disc.source_z = C.f32(actor.source_z - 0.75)
	disc.owner_actor = actor
	disc.throw_direction = direction
	disc.world = world
	disc.time_scaler = actor.time_scaler
	disc.owner_inventory = weakref(self)
	disc.disc_powerups = powers.active
	disc.initialize(true)
	spawned_discs.append(disc)
	held_discs -= 1
	return disc

func frame_step(delta: float, advance_power_coroutines := true) -> void:
	if advance_power_coroutines: powers.frame_step(delta)
	if attack_cooldown > 0.0: attack_cooldown = C.f32(attack_cooldown - C.f32(C.f32(delta) * actor.time_scaler))
	else:
		attack_cooldown = 0.0
		cooling_attack = false
	actor.cooldown_attack_motion(delta)
	if actor.melee_timer <= 0.0: actor.is_disarming = false
	if held_discs > 0: actor.is_disarmed = false
	if not cooling_attack:
		attacks_left = held_discs
		disarms_left = 1

func collect(disc: OriginalDiscMotion) -> bool:
	if actor.state == V.State.DEAD or (actor.state == V.State.FALLING and actor.fall_timer > C.f32(0.33)): return false
	if held_discs >= 2 or not disc.can_be_picked_up: return false
	if disc.owner_actor == actor:
		if held_discs + powers.pending_discs > 1 or not disc.can_collide_with_owner: return false
	elif disc.owner_actor != null or disc.movement != OriginalDiscMotion.Movement.DROPPED: return false
	if disc.is_mini_disc and disc.owner_actor == actor: return collect_mini(disc)
	return pick_up_disc(disc)

func pick_up_disc(disc: OriginalDiscMotion) -> bool:
	if actor.disguise_hook != null and actor.disguise_hook.get_ref() != null:
		var disguise = actor.disguise_hook.get_ref()
		if disguise.disguised: disguise.events.append({"type":"jiggle"})
	if actor.state == V.State.DEAD or (actor.state == V.State.FALLING and actor.fall_timer > C.f32(0.33)) or not disc.can_be_picked_up: return false
	cooling_attack = true
	attack_cooldown = C.f32(0.05) if actor.move_faster or held_discs > 0 else C.f32(0.1)
	if disc.owner_actor == actor and (disc.was_just_deflected or disc.aim_power > C.f32(0.15)):
		var ratio := maxf(C.f32(C.f32(1.0 / actor.time_scaler) * disc.time_scaler), C.f32(C.f32(1.0 / disc.time_scaler) * actor.time_scaler))
		var direction := -(actor.velocity_stored - disc.stored)
		if disc.was_just_deflected: direction *= C.f32(1.2)
		direction *= ratio
		if direction.length() < 1.0: direction = V.normalized_xyz_xy(actor.position - disc.position, actor.source_z - disc.source_z) * 50.0
		if disc.was_just_deflected: actor.start_pushback(direction, C.f32(0.1), C.f32(0.15))
		else: actor.start_pushback(direction, C.f32(0.02))
	if held_discs < 2:
		held_discs += 1
		if powers.pending_discs > 0: powers.pending_discs = mini(powers.pending_discs,2-held_discs)
	normal_catches += 1
	check_num_mini_discs()
	owned_discs.erase(disc)
	disc.recycle()
	return true

func check_num_mini_discs() -> void:
	ghost_discs = 0 if mini_discs_collected >= mini_discs_required or mini_discs_collected == 0 else held_discs+1

func collect_mini(disc: OriginalDiscMotion) -> bool:
	if not disc.can_be_picked_up: return false
	if mini_discs_collected >= mini_discs_required:
		ghost_discs = 0
		return false
	if mini_discs_collected == mini_discs_required-1:
		mini_discs_collected = 0
		return pick_up_disc(disc)
	ghost_discs = held_discs+1
	mini_discs_collected += 1
	disc.recycle()
	return true

func multidisc_burst(parent: OriginalDiscMotion) -> void:
	if (parent.disc_powerups & 64) == 0 or parent.owner_actor == null: return
	var count := 4 if (parent.disc_powerups & (32|256)) != 0 else 5
	mini_discs_required = count
	var direction := V.normalized(parent.velocity)
	var base_degrees := C.f32(C.f32(atan2(direction.y,direction.x))*C.f32(57.29578)-90.0)
	for index in count:
		var angle := C.f32(C.f32(C.f32(float(index)/float(count-1))*180.0)-90.0)
		var rotation := C.f32(base_degrees+angle)
		var child := OriginalDiscMotion.new()
		child.position = parent.position
		child.source_z = parent.source_z
		child.world = world
		child.owner_actor = parent.owner_actor
		child.owner_inventory = weakref(self)
		child.keyboard_owner = parent.keyboard_owner
		child.disc_powerups = parent.disc_powerups & ~64
		child.throw_direction = OriginalDiscMotion.euler_up_z(rotation)
		child.spawn_rotation_z = C.f32(rotation*C.f32(C.f32(PI)/180.0))
		child.is_mini_disc = true
		child.initialize()
		if (parent.disc_powerups & 32) != 0:
			child.grenade_active = true
			child.grenade_timer = C.f32(C.f32(0.931)+index*C.f32(0.1))
		if collision_receiver != null and collision_receiver.get_ref() != null: collision_receiver.get_ref().bind_disc(child)
		spawned_discs.append(child)
	parent.is_mini_disc = true
	parent.recycle()
	if (parent.disc_powerups & 32) != 0: respawn_requests.append(C.f32(3.25))
	powers.events.append({"type":"multidisc_burst","count":count})

func request_respawn(wait_time: float, count := 1) -> void:
	if actor.state == V.State.DEAD or held_discs + powers.pending_discs >= 2: return
	for _index in count:
		if held_discs + powers.pending_discs < 2: powers.pending_discs += 1
	respawn_jobs.append({"remaining": C.f32(wait_time), "count": count, "wait_frame": true})

func finish_respawn(count: int) -> void:
	if actor.state == V.State.DEAD or (actor.state == V.State.FALLING and actor.fall_timer > C.f32(0.33)): return
	for _index in count:
		if powers.pending_discs > 0 and held_discs < 2:
			powers.pending_discs -= 1
			grant_replacement_disc()

func grant_replacement_disc() -> void:
	# Player.PickUpDisc(null): no collision/catch path or catch count.
	cooling_attack = true
	attack_cooldown = C.f32(0.05) if actor.move_faster or held_discs > 0 else C.f32(0.1)
	if held_discs < 2:
		held_discs += 1
		if powers.pending_discs > 0: powers.pending_discs = mini(powers.pending_discs, 2-held_discs)
	replacement_pickups += 1
	check_num_mini_discs()

func advance_respawns(delta: float) -> void:
	for job in respawn_jobs.duplicate():
		if job.wait_frame:
			job.wait_frame = false
			continue
		job.remaining = C.f32(job.remaining - C.f32(delta))
		if job.remaining <= 0.0:
			finish_respawn(job.count)
			respawn_jobs.erase(job)
