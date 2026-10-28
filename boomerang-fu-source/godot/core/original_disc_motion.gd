class_name OriginalDiscMotion
extends RefCounted
## Projectile/Disc flight calculations. Engine queries are supplied by the
## collision adapter. Powerup effects, damage and audiovisual events are separate.
const C = preload("res://core/original_timekeeper.gd")
const V = preload("res://core/original_actor_motion.gd")
enum Movement { THROWING, RETURNING, FREE, DROPPED, SQUISHED, CONTROLLED }
var owner_actor: OriginalActorMotion
var owner_inventory: WeakRef
var disc_powerups := 0
var spawn_rotation_z := NAN
var grenade := OriginalGrenade.new()
var cell_grid: OriginalCellGrid
var cell_trail := OriginalDiscTrail.new()
var cell_dark := false
var cell_owner := -1
var grenade_owner: OriginalActorMotion
var grenade_active: bool:
	get: return grenade.active
	set(value):
		if value and not grenade.active:
			grenade.arm()
			grenade_owner = owner_actor
		else: grenade.active = value
var grenade_timer: float:
	get: return grenade.timer
	set(value): grenade.timer = C.f32(value)
var world: OriginalCollisionWorld
var position := Vector2.ZERO
var source_z := 0.0
var velocity := Vector2.ZERO
var stored := Vector2.ZERO
var movement := Movement.THROWING
var fixed_delta := C.f32(0.016666)
var mass := C.f32(0.6)
var drag := C.f32(2.8)
var bounciness := C.f32(0.8)
var speed_limit := 45.0
var throw_speed := 190.0
var throw_duration := C.f32(0.45)
var throw_timer := 0.0
var throw_strength := 0.0
var throw_direction := Vector2(0, -1)
var aim_power := 0.0
var spin_angle := 0.0
var spin_timer := 0.0
var spin_duration := 0.0
var is_spinning := true
var return_timer := 0.0
var return_direction := Vector2.ZERO
var return_strength := 190.0
var free_direction := Vector2.ZERO
var free_strength := 0.0
var time_scaler := 1.0
var time_scaler_cooldown := 0.0
var time_scaled_contact_requests := 0
var pending_time_scaled_contacts := 0
var can_return := true
var can_home := true
var homing_radius := 11.0
var keyboard_owner := true
var can_collide_with_owner := false
var can_be_picked_up := true
var started_dropped := false
var was_just_deflected := false
var deflected_timer := 0.0
var is_mini_disc := false
var can_be_controlled := true
var controlled_factor := 0.0
var controlled_direction := Vector2.ZERO
var controlled_boundary_active := false
var orbit_timer := 0.0
var orbit_is_charged := false
var radius := 0.5 # Explicit query radius, distinct from serialized trigger size.
var collider_radius := C.f32(0.7125)
var min_distance_from_owner := 4.5 # Projectile.OnEnable overrides prefab's 3.
var events: Array[Dictionary] = []
var active := true
var collision_receiver: WeakRef
var damage_overlaps: Dictionary = {}
var melee_overlaps: Dictionary = {}
var shield_overlaps: Dictionary = {}
var shield_triggers_this_step: Dictionary = {}
var melee_triggers_this_step: Dictionary = {}
var collider_disabled_remaining := 0.0
var projectile_layer := true
var disc_contacts_this_step: Dictionary = {}

static func dot32(a: Vector2, b: Vector2) -> float:
	return C.f32(a.x * b.x + a.y * b.y)

static func lerp_vector(a: Vector2, b: Vector2, amount: float) -> Vector2:
	return Vector2(C.lerp32(a.x, b.x, amount), C.lerp32(a.y, b.y, amount))

static func cubic_out(t: float) -> float:
	# --t leaves its expression value on Mono's evaluation stack, while the
	# parameter store narrows subsequent reads to binary32.
	var decremented := t - 1.0
	t = C.f32(decremented)
	return C.f32(1.0 + decremented * t * t)

static func quart_out(t: float) -> float:
	var decremented := t - 1.0
	t = C.f32(decremented)
	return C.f32(1.0 - decremented * t * t * t)

static func quarter_turn(vector: Vector2, sign_value: float) -> Vector2:
	# Quaternion.AngleAxis(+/-90, forward) * vector, retaining binary32
	# quaternion multiplication residuals instead of swapping axes exactly.
	var q := C.f32(0.7071067811865476)
	var zz := C.f32(q * C.f32(q * 2.0))
	var cross := C.f32(zz * sign_value)
	var diagonal := C.f32(1.0 - zz)
	return Vector2(C.f32(diagonal * vector.x - cross * vector.y),
		C.f32(cross * vector.x + diagonal * vector.y))

static func quaternion_turn(vector: Vector2, angle_degrees: float) -> Vector2:
	# Original Mono rounds quaternion locals, retaining wider component
	# expressions. The game's -45 degree parry is verified in UnityPlayer.
	# Native AngleAxis divides by 180 before multiplying by pi. Euler uses a
	# different conversion. Native sincos still has rare one-ULP differences.
	var half_angle := C.f32(C.f32(C.f32(angle_degrees) / 180.0) * C.f32(PI) * 0.5)
	var z := C.f32(sin(half_angle))
	var w := C.f32(cos(half_angle))
	var z2 := C.f32(z * 2.0)
	var diagonal := 1.0 - C.f32(z * z2)
	var cross := C.f32(w * z2)
	return Vector2(C.f32(diagonal * vector.x - cross * vector.y),
		C.f32(cross * vector.x + diagonal * vector.y))

func force(value: Vector2, impulse := false) -> Vector2:
	var result := value * C.f32(1.0 / mass)
	return result if impulse else result * fixed_delta

static func euler_up_z(angle_degrees: float) -> Vector2:
	var radians := C.f32(C.f32(angle_degrees) * C.f32(C.f32(PI) / 180.0))
	var half := C.f32(radians * 0.5)
	var z := C.f32(sin(half))
	var w := C.f32(cos(half))
	# Native Transform.rotation normalizes by component division, not by a
	# rounded reciprocal. That distinction changes otherwise unit quaternions.
	var length := C.f32(sqrt(C.f32(C.f32(z*z)+C.f32(w*w))))
	z = C.f32(z / length)
	w = C.f32(w / length)
	var z2 := C.f32(z * 2.0)
	return Vector2(-C.f32(w*z2),C.f32(1.0-C.f32(z*z2)))

func is_an_explosive() -> bool:
	return (disc_powerups & 32) != 0 and (disc_powerups & 64) == 0

func recycle() -> void:
	active = false
	grenade_active = false
	if owner_inventory != null and owner_inventory.get_ref() != null: owner_inventory.get_ref().owned_discs.erase(self)
	if collision_receiver != null and collision_receiver.get_ref() != null: collision_receiver.get_ref().on_disc_recycled(self)

func initialize(dropped := false) -> void:
	velocity = Vector2.ZERO
	stored = Vector2.ZERO
	if dropped:
		start_dropped()
	else:
		# Projectile.Init dispatches virtually, then Disc.Init calls StartThrow again.
		start_throw()
	# Disc.Init registers ownership between the base and derived throw calls.
	if owner_actor != null and owner_inventory != null and owner_inventory.get_ref() != null:
		owner_inventory.get_ref().owned_discs.append(self)
	if not dropped and is_an_explosive():
		can_return = false
		can_be_picked_up = false
		can_collide_with_owner = false
	if dropped:
		start_dropped()
	else:
		start_throw(false, not is_mini_disc)

func start_throw(reset := false, inherit_velocity := false) -> void:
	movement = Movement.THROWING
	throw_timer = 0.0
	orbit_timer = 0.0
	orbit_is_charged = false
	throw_strength = C.f32(throw_speed * (1.0 + aim_power))
	homing_radius = 11.0 if keyboard_owner else 7.0
	throw_duration = C.f32(0.45)
	controlled_factor = 0.0
	if not reset:
		speed_limit = C.f32(speed_limit + speed_limit * aim_power * 0.5)
		throw_duration = C.f32(throw_duration - aim_power * C.f32(0.15))
		homing_radius = C.f32(homing_radius + homing_radius * aim_power)
	if is_mini_disc:
		throw_duration = C.f32(0.3378)
		if (disc_powerups & 32) != 0: throw_duration = C.f32(throw_duration*C.f32(0.2))
	spin_duration = C.f32(throw_duration * 2.0)
	velocity = force(throw_direction * throw_strength) * 9.0 * time_scaler
	stored = velocity
	if inherit_velocity and owner_actor != null: velocity += owner_actor.velocity
	if time_scaler != 1.0:
		time_scaler_cooldown = 1.0
		time_scaled_contact_requests += 1
		pending_time_scaled_contacts += 1
		if collision_receiver != null and collision_receiver.get_ref() != null:
			collision_receiver.get_ref().check_disc_current_location(self)

func update_time_scaler(delta: float, global_scale: float) -> void:
	if owner_actor != null and owner_actor.time_immune and time_scaler_cooldown > 0.0:
		time_scaler_cooldown = C.f32(time_scaler_cooldown - C.f32(delta))
		var target := C.f32(1.0 / global_scale) if global_scale > 0.0 else 1.0
		time_scaler = C.lerp32(1.0, target, time_scaler_cooldown)
	else:
		time_scaler = 1.0

func calculate_throw() -> Vector2:
	if throw_timer < throw_duration:
		throw_timer = C.f32(throw_timer + fixed_delta) # Source does not scale this timer.
		return throw_direction * C.lerp32(throw_strength, 0.0, C.ease_in(C.f32(throw_timer / throw_duration)))
	end_throw()
	return Vector2.ZERO

func end_throw() -> void:
	if is_an_explosive():
		start_freeform()
		return
	if (disc_powerups & 64) != 0:
		if owner_actor != null and owner_inventory != null and owner_inventory.get_ref() != null:
			owner_inventory.get_ref().multidisc_burst(self)
		return
	if can_return: start_return()
	else:
		free_direction = V.normalized(velocity)
		free_strength = C.f32(V.magnitude(velocity) * C.f32(0.1))
		start_freeform()
	throw_timer = throw_duration
	throw_strength = 0.0

func start_return() -> void:
	can_collide_with_owner = true
	movement = Movement.RETURNING
	return_timer = 0.0

func calculate_return() -> Vector2:
	return_direction = V.normalized_xyz_xy(owner_actor.position - position, owner_actor.source_z - source_z)
	return_direction = lerp_vector(V.normalized(velocity), return_direction, C.f32(fixed_delta * 60.0))
	var amount: float
	if return_timer < throw_duration:
		amount = C.lerp32(0.0, return_strength, C.ease_in(C.f32(return_timer / throw_duration)))
		return_timer = C.f32(return_timer + fixed_delta * time_scaler)
	else:
		return_timer = throw_duration
		amount = throw_speed
	return return_direction * amount

func calculate_controlled() -> Vector2:
	if owner_actor.state == V.State.DEAD or owner_actor.retrieve_charge == 0.0:
		var speed := V.magnitude(velocity)
		if speed > 1.0:
			movement = Movement.THROWING
			var fraction := C.f32(C.f32(C.f32(speed / speed_limit) * owner_actor.retrieve_charge) * 4.0)
			throw_timer = C.f32(C.ease_out(C.f32(1.0 - clampf(fraction,0.0,1.0))) * throw_duration)
			throw_direction = V.normalized(velocity)
		elif throw_timer < throw_duration:
			movement = Movement.THROWING
		elif can_return:
			start_return()
		else:
			free_direction = V.normalized(velocity)
			free_strength = C.f32(speed * C.f32(0.1))
			start_freeform()
		controlled_factor = 0.0
		return Vector2.ZERO
	if controlled_factor < 1.0:
		controlled_factor = C.f32(controlled_factor + C.f32(C.f32(fixed_delta*time_scaler)*4.0))
	else:
		controlled_factor = 1.0
	controlled_direction = owner_actor.raw_input_move
	is_spinning = false
	return (controlled_direction * 110.0) * controlled_factor

func calculate_retrieve(fast_start := false) -> Vector2:
	return_direction = V.normalized_xyz_xy(owner_actor.position - position, owner_actor.source_z - source_z)
	return_direction = lerp_vector(V.normalized(velocity), return_direction, C.f32(fixed_delta * 60.0))
	var amount := clampf(C.f32(quart_out(owner_actor.retrieve_charge) - C.f32(0.2)), 0.0, 1.0)
	if fast_start: amount = clampf(quart_out(C.f32(owner_actor.retrieve_charge * 3.0)), 0.0, 1.0)
	return return_direction * return_strength * amount

func apply_spin() -> void:
	spin_timer = C.f32(spin_timer + fixed_delta * time_scaler)
	var duration := C.f32(throw_duration * C.f32(1.2))
	var amount := C.f32(throw_speed * (absf(spin_angle) / 90.0))
	var magnitude := V.magnitude(velocity)
	var sign_value := 1.0 if spin_angle >= 0.0 else -1.0
	var vector := quarter_turn(V.normalized(throw_direction), -sign_value) * amount
	if spin_timer > duration: vector = quarter_turn(V.normalized(return_direction), sign_value) * amount
	if spin_timer < duration:
		velocity += force(vector * cubic_out(C.f32(spin_timer / duration))) * time_scaler
	else:
		velocity += force(vector) * time_scaler
	velocity = V.clamp_magnitude(velocity, magnitude)

func apply_homing() -> void:
	if not can_home or world == null: return
	var target: OriginalActorMotion
	var nearest := homing_radius
	for shape in world.actor_shapes:
		var actor: OriginalActorMotion = shape.actor_ref.get_ref()
		var offset := actor.position - position
		var distance := V.magnitude_xyz(offset, actor.source_z - source_z)
		if actor == owner_actor or actor.state == V.State.DEAD or actor.is_dashing: continue
		if dot32(V.normalized_xyz_xy(offset, actor.source_z - source_z), V.normalized(velocity)) > C.f32(0.75) and distance < nearest:
			nearest = distance
			target = actor
	if target == null: return
	var lhs := target.position - position
	if dot32(lhs, throw_direction) < C.f32(0.75):
		var factor := C.f32(0.75 + clampf(C.f32(aim_power * C.f32(0.8)), 0.0, 1.0) * 0.25)
		lhs = V.magnitude(lhs) * (V.normalized(lhs) * factor)
	if world.circle_cast(position, 0.0, V.normalized(lhs), V.magnitude(lhs), 256).is_empty():
		var magnitude := V.magnitude(velocity)
		var amount := C.f32(throw_speed * 1.25 + throw_speed * aim_power * 3.0)
		velocity += force(V.normalized(lhs) * amount) * time_scaler
		velocity = V.clamp_magnitude(velocity, magnitude)

func start_dropped() -> void:
	started_dropped = true
	projectile_layer = false
	velocity = throw_direction * throw_speed * 0.5
	movement = Movement.DROPPED
	start_freeform()
	can_be_controlled = false
	aim_power = 0.0
	spin_angle = 0.0

func start_freeform(boost := 0.0, stay_dropped := false) -> void:
	free_direction = V.normalized(velocity)
	free_strength = C.f32(V.magnitude(velocity) * bounciness)
	if not stay_dropped: movement = Movement.FREE
	if boost > 0.0: velocity += force(free_direction * boost, true) * time_scaler
	if owner_actor != null and owner_actor.aim_cancel_hook != null:
		var inventory: OriginalPlayerWeapons = owner_actor.aim_cancel_hook.get_ref()
		if inventory != null and inventory.held_discs == 0: owner_actor.is_disarmed = true
	events.append({"type": "freeform"})

func calculate_freeform() -> Vector2:
	free_strength = C.lerp32(free_strength, 0.0, C.f32(fixed_delta * 20.0))
	return V.normalized(velocity) * free_strength

func bounce(collision_velocity: Vector2, normal: Vector2) -> void:
	if V.magnitude(collision_velocity) < C.f32(speed_limit * C.f32(0.05)):
		collision_velocity = V.normalized(collision_velocity) * speed_limit * C.f32(0.05)
	velocity = V.reflect(collision_velocity, normal) * bounciness
	if movement == Movement.THROWING:
		if throw_timer > C.f32(throw_duration * C.f32(0.1)): throw_timer = C.f32(throw_timer - C.f32(0.01))
		throw_direction = V.normalized(velocity)
	if movement == Movement.RETURNING and dot32(return_direction, V.normalized(collision_velocity)) > C.f32(0.01): start_freeform()
	is_spinning = false
	controlled_factor = 0.0
	if owner_actor != null and owner_actor.retrieve_charge > 0.0:
		var factor := C.f32(0.33) if movement == Movement.DROPPED else C.f32(0.66)
		velocity -= force(return_direction * return_strength * quart_out(owner_actor.retrieve_charge) * factor) * time_scaler

func collide_with_disc(other: OriginalDiscMotion, normal: Vector2) -> bool:
	var id := other.get_instance_id()
	if disc_contacts_this_step.has(id): return false
	disc_contacts_this_step[id] = true
	if not started_dropped: can_collide_with_owner = true
	var collision_velocity := velocity - other.stored
	was_just_deflected = true
	deflected_timer = C.f32(0.15)
	if other.owner_actor != owner_actor and owner_actor != null:
		var distance := V.magnitude_xyz(position - owner_actor.position, source_z - owner_actor.source_z)
		if distance < C.f32(min_distance_from_owner * 2.0) and distance < V.magnitude_xyz(other.position - owner_actor.position, other.source_z - owner_actor.source_z):
			owner_actor.deflected_discs[id] = 10
	bounce(collision_velocity, normal)
	# Disc.CollisionEnter2D returns immediately after base adds this collider.
	# Its later StartFreeform(2000)/canHome=false branch is therefore not run.
	return true

func fixed_step() -> void:
	shield_triggers_this_step.clear()
	if not active: return
	melee_triggers_this_step.clear()
	disc_contacts_this_step.clear()
	controlled_boundary_active = false
	match movement:
		Movement.THROWING: apply_homing()
		Movement.RETURNING:
			velocity += force(calculate_return()) * time_scaler
			if not can_be_controlled and not was_just_deflected and owner_actor.retrieve_charge > C.f32(0.15):
				can_be_controlled = true
		Movement.FREE:
			velocity += force(calculate_freeform()) * time_scaler
			if V.magnitude(velocity) < C.f32(10.0 * time_scaler):
				aim_power = 0.0
				movement = Movement.DROPPED
				projectile_layer = false
				can_be_controlled = false
				can_collide_with_owner = true
		Movement.DROPPED:
			if owner_actor != null and V.magnitude(velocity) < 8.0 and not is_mini_disc and started_dropped:
				if collision_receiver != null and collision_receiver.get_ref() != null:
					collision_receiver.get_ref().on_disc_public(self)
				owner_actor = null
				started_dropped = false
			if owner_actor != null and V.magnitude(stored) > 22.5 and owner_actor.retrieve_charge > C.f32(0.1):
				movement = Movement.RETURNING
				projectile_layer = true
		Movement.CONTROLLED:
			velocity += force(calculate_controlled()) * time_scaler
			var radial := position - owner_actor.position
			var distance := V.magnitude(radial)
			if distance >= 30.0:
				var normal := V.normalized(radial)
				var padding := clampf(C.f32(C.f32(distance-30.0)/4.0),0.0,1.0)
				velocity -= normal * maxf(0.0,C.f32(dot32(velocity,normal)*padding))
				controlled_boundary_active = dot32(normal,V.normalized(velocity)) > C.f32(-0.25)
			owner_actor.time_controlling_disc = C.f32(owner_actor.time_controlling_disc+fixed_delta)
	if is_spinning: apply_spin()
	if movement != Movement.SQUISHED and owner_actor != null and owner_actor.retrieve_charge > 0.0 and movement != Movement.CONTROLLED and not was_just_deflected:
		velocity += force(calculate_retrieve()) * time_scaler
	if cell_grid != null: cell_trail.update_gate(self,cell_dark)
	# The base FixedUpdate is invoked after the Disc force calculations.
	match movement:
		Movement.THROWING: velocity += force(calculate_throw()) * time_scaler
		Movement.FREE:
			velocity += force(calculate_freeform())
			if V.magnitude(velocity) < 10.0:
				movement = Movement.DROPPED
				projectile_layer = false
				can_collide_with_owner = true
		Movement.DROPPED:
			if V.magnitude(velocity) < 1.0 and started_dropped: started_dropped = false
	move_projectile()
	if not can_collide_with_owner and owner_actor != null and V.magnitude_xyz(position - owner_actor.position, source_z - owner_actor.source_z) > min_distance_from_owner:
		can_collide_with_owner = true

func separate_from_obstacles(depth := 0) -> void:
	if depth > 5: return
	for overlap in world.overlaps(position, C.f32(radius - C.f32(0.01)), 256):
		var actual := world.penetration(position, collider_radius, overlap.shape)
		if actual.is_empty(): continue
		var push: Vector2 = actual.normal * actual.depth
		var hit := world.circle_cast(position, C.f32(radius - C.f32(0.01)), V.normalized(push), V.magnitude(push), 256)
		if not hit.is_empty(): push -= hit.normal * dot32(push, hit.normal)
		position += push
		velocity += push * fixed_delta
		separate_from_obstacles(depth + 1)
		break

func move_projectile() -> void:
	if V.magnitude(velocity) > 0.0:
		velocity *= clampf(C.f32(1.0 - drag * fixed_delta * clampf(time_scaler, 1.0, 2.0)), 0.0, 1.0)
	if V.magnitude(velocity) > speed_limit: velocity = V.clamp_magnitude(velocity, speed_limit)
	if world != null: separate_from_obstacles()
	var amount := velocity
	var incoming_velocity := velocity
	if world != null:
		var direction := V.normalized(velocity)
		var hit := world.circle_cast(position, radius, direction, C.f32(V.magnitude(velocity) * fixed_delta), 256)
		if not hit.is_empty():
			if not started_dropped: can_collide_with_owner = true
			bounce(velocity, hit.normal)
			can_home = false
			# Original returns displacement, then multiplies by fixedDeltaTime below.
			amount = hit.centroid - position - direction * C.f32(0.001)
			events.append({"type": "wall", "normal": hit.normal,"position":position,"object_id":hit.shape.get("object_id",-1),"audio_velocity":C.f32(V.magnitude(incoming_velocity)/speed_limit)})
	if collision_receiver != null and collision_receiver.get_ref() != null:
		amount = collision_receiver.get_ref().sweep_disc(self, amount, incoming_velocity)
	position += amount * fixed_delta
	stored = velocity
	if cell_grid != null: cell_trail.after_move(self,amount,cell_grid,cell_owner)

func frame_step(delta: float) -> void:
	if movement == Movement.DROPPED: can_be_picked_up = true
	if was_just_deflected and deflected_timer > 0.0: deflected_timer = C.f32(deflected_timer - C.f32(delta))
	elif was_just_deflected:
		was_just_deflected = false
		deflected_timer = 0.0
	if movement == Movement.RETURNING and owner_actor != null and owner_actor.state == V.State.DEAD:
		start_freeform(-10.0)
		can_be_controlled = false
	if movement in [Movement.THROWING,Movement.RETURNING] and can_be_controlled and owner_actor != null and (disc_powerups & 8192) != 0 and owner_actor.retrieve_charge > 0.0:
		movement = Movement.CONTROLLED
		aim_power = 0.0

func spin_speed() -> float:
	var amount := C.f32(1.0 + aim_power)
	if movement == Movement.FREE: amount = C.f32(C.f32(V.magnitude(velocity) / speed_limit) + C.f32(aim_power * 0.5))
	elif movement == Movement.DROPPED: amount = C.f32(C.f32(V.magnitude(velocity) / speed_limit) * 0.5)
	elif movement == Movement.SQUISHED: amount = 0.0
	elif movement == Movement.CONTROLLED: amount = 1.0
	return C.f32(amount * C.f32(1.5 * time_scaler))

func snapshot() -> Dictionary:
	return {"source_z": source_z, "position": [position.x, position.y], "velocity": [velocity.x, velocity.y], "stored": [stored.x, stored.y],
		"movement": movement, "throw_timer": throw_timer, "throw_duration": throw_duration, "throw_strength": throw_strength,
		"return_timer": return_timer, "spin_timer": spin_timer, "free_strength": free_strength, "speed_limit": speed_limit,
		"can_collide_with_owner": can_collide_with_owner, "spinning": is_spinning, "aim_power": aim_power, "owner_present": owner_actor != null}
