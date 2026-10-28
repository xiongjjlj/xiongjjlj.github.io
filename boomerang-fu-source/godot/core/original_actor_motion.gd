class_name OriginalActorMotion
extends RefCounted
var explosion_dodges := 0
var is_decoy:=false
var is_burnt := false
## Actor/Player locomotion arithmetic. Collision queries and gameplay effects are
## injected separately. This component does not replace the complete Actor class.
const Clock = preload("res://core/original_timekeeper.gd")
enum State { ALIVE, DEAD, FALLING }
var fixed_delta := Clock.f32(0.016666)
var position := Vector2.ZERO
var source_z := 0.0 # Original Unity transform Z; physics queries remain in XY.
var velocity := Vector2.ZERO
var velocity_stored := Vector2.ZERO
var input_move := Vector2.ZERO
var raw_input_move := Vector2.ZERO # PlayerInput.inputMove, before Player.CalculateWalk reversal.
var time_controlling_disc := 0.0
var last_input_move := Vector2(0, -1) # Simulation retains Unity's Y-up plane.
var last_rotation := Vector2(0, -1)
var rotation_z := PI
var time_scaler := 1.0 # Personal SlowTime modifier, distinct from global time.
var time_immune := false
var speed_limit := 40.0
var radius := Clock.f32(Clock.f32(0.66) * Clock.f32(1.33))
var collision_layer := 256 | 512
var destructables_layer := 32768
var state := State.ALIVE
var can_walk := true
var can_rotate := true
var can_dash := true
var is_dashing := false
var dash_through_walls := false
var insides_enabled := false
var stop_dash_on_hazards := false
var stop_lunge_on_hazards := false
var is_stunned := false
var is_frozen := false
var is_invulnerable := false
var is_shielded := false
var is_attacking := false
var is_disarming := false
var just_became_vulnerable := false
var deflected_discs: Dictionary = {}
var is_lunging := false
var is_showing_off := false
var is_spawning_in := false
var is_wading := false
var is_slimed := false
var is_aiming := false
var is_disarmed := false
var move_faster := false
var controlled_disc := false
var reverse_inputs := false
var on_wet_floor := false
var slide_factor := 1.0
var drag := 11.0
var aim_move_lag := 0.0
var retrieve_charge := 0.0
var dash_duration := Clock.f32(0.2)
var dash_cooldown := Clock.f32(0.55)
var dash_cooldown_move_threshold := 0.0
var dash_timer := Clock.f32(0.2)
var dash_cooldown_timer := 0.0
var dash_direction := Vector2.ZERO
var dash_force := 0.0
var current_dash_amount := 0.0
var move_speed_parameter := 0.0
var standing_still_timer := 0.0
var fall_timer := 0.0
var fall_duration := Clock.f32(0.66)
var is_falling := false
var is_sliced := false
var physics_enabled := true
var appearance_visible := true
var environment_hook: WeakRef
var falls_saved := 0
var push_duration := 0.0
var push_timer := 0.0
var push_strength := 0.0
var push_direction := Vector2.ZERO
var push_stun_duration := 0.0
var stun_timer := 0.0
var stunned_direction := Vector2.ZERO
var lunge_timer := 0.0
var lunge_duration := Clock.f32(0.1125)
var lunge_direction := Vector2.ZERO
var lunge_cooldown_timer := 0.0
var melee_timer := 0.0
var deflected_melees: Dictionary = {}
var world: RefCounted
var query_id := -1
var collision_events: Array = []
var aim_cancel_hook: WeakRef
var elements_hook: WeakRef
var disguise_hook: WeakRef
var element_move_amount := Vector2.ZERO

func break_disguise() -> void:
	if disguise_hook != null and disguise_hook.get_ref() != null: disguise_hook.get_ref().break_disguise()

func stop_aiming() -> void:
	if is_aiming and magnitude(input_move) > 0.0: aim_move_lag = 1.0
	is_aiming = false
	if aim_cancel_hook != null and aim_cancel_hook.get_ref() != null:
		aim_cancel_hook.get_ref().on_actor_stop_aiming()

static func normalized(value: Vector2) -> Vector2:
	var length := magnitude(value)
	return value / length if length > 0.00001 else Vector2.ZERO

static func magnitude(value: Vector2) -> float:
	return Clock.f32(sqrt(value.x * value.x + value.y * value.y))

static func magnitude_xyz(value: Vector2, z: float) -> float:
	z = Clock.f32(z)
	return Clock.f32(sqrt(value.x * value.x + value.y * value.y + z * z))

static func normalized_xyz_xy(value: Vector2, z: float) -> Vector2:
	# Unity normalizes a Vector3 before implicitly dropping its Z component.
	# The resulting XY vector may have length < 1, including in Reflect calls.
	var length := magnitude_xyz(value, z)
	if length <= Clock.f32(0.00001): return Vector2.ZERO
	return Vector2(Clock.f32(value.x / length), Clock.f32(value.y / length))

static func clamp_magnitude(value: Vector2, maximum: float) -> Vector2:
	var squared := Clock.f32(value.x * value.x + value.y * value.y)
	if squared > maximum * maximum:
		return value / Clock.f32(sqrt(squared)) * maximum
	return value

static func reflect(value: Vector2, normal: Vector2) -> Vector2:
	var dot := Clock.f32(normal.x * value.x + normal.y * value.y)
	var scale := Clock.f32(-2.0 * dot)
	# Original Mono retains expression precision until each returned component.
	return Vector2(Clock.f32(scale * normal.x + value.x), Clock.f32(scale * normal.y + value.y))

func start_pushback(direction: Vector2, duration: float, stun_duration := 0.0) -> void:
	if state != State.ALIVE: return
	velocity += direction * 2.5 * time_scaler * fixed_delta * slide_factor
	push_duration = Clock.f32(duration)
	push_timer = push_duration
	push_direction = normalized(direction)
	push_strength = Clock.f32(500.0 * magnitude(direction))
	if stun_duration > 0.0:
		stunned_direction = -direction
		move_speed_parameter = 0.0
		push_stun_duration = Clock.f32(stun_duration)
		is_stunned = true
		stun_timer = 0.0
		is_showing_off = false

func calculate_pushback() -> Vector2:
	if push_timer > 0.0 and not is_falling:
		var amount := Clock.lerp32(0.0, push_strength, Clock.f32(push_timer / push_duration))
		push_timer = Clock.f32(push_timer - fixed_delta * time_scaler)
		return push_direction * amount * fixed_delta
	push_timer = 0.0
	push_strength = 0.0
	return Vector2.ZERO

func get_stunned(direction: Vector2, duration := 0.66) -> void:
	is_attacking = false
	is_disarming = false
	stop_aiming()
	velocity = Vector2.ZERO
	start_pushback(direction, 0.5, 0.0 if is_stunned else duration)
	break_disguise()

func frame_step(scaled_delta: float) -> void:
	if is_stunned and stun_timer < push_stun_duration:
		stun_timer = Clock.f32(stun_timer + Clock.f32(scaled_delta) * time_scaler)
	elif is_stunned:
		is_stunned = false

func begin_attack() -> void:
	# Actor.Attack's movement branch; Player owns inventory and cooldown guards.
	if is_dashing: stop_dash()
	melee_timer = Clock.f32(0.2)
	is_lunging = true
	lunge_cooldown_timer = Clock.f32(0.5)
	lunge_timer = lunge_duration
	lunge_direction = normalized(last_input_move if can_walk else last_rotation)
	if environment_hook != null and environment_hook.get_ref() != null:
		stop_lunge_on_hazards = not environment_hook.get_ref().is_lunge_safe(lunge_direction)
	rotation_z = atan2(lunge_direction.y, lunge_direction.x) - PI / 2.0
	velocity += Clock.f32(speed_limit * Clock.f32(0.66)) * lunge_direction * time_scaler * slide_factor
	velocity_stored = velocity * clampf(Clock.f32(1.0 - drag * fixed_delta), 0.0, 1.0)

func cooldown_attack_motion(delta: float) -> void:
	var elapsed := Clock.f32(delta) * time_scaler
	if melee_timer <= 0.0:
		melee_timer = 0.0
		is_attacking = false
	else: melee_timer = Clock.f32(melee_timer - elapsed)
	if lunge_cooldown_timer > 0.0:
		lunge_cooldown_timer = Clock.f32(lunge_cooldown_timer - elapsed)
		can_walk = false
		can_rotate = false
		return
	lunge_cooldown_timer = 0.0
	if is_lunging:
		can_walk = true
		can_rotate = true
	is_lunging = false

func calculate_lunge() -> Vector2:
	if lunge_timer <= 0.0: return Vector2.ZERO
	var t := Clock.f32(lunge_timer / lunge_duration)
	var amount := Clock.lerp32(0.0, 800.0, Clock.f32(t * t * t))
	if is_slimed: amount = Clock.f32(amount * Clock.f32(0.33))
	lunge_timer = Clock.f32(lunge_timer - fixed_delta * time_scaler)
	return lunge_direction * amount

func stop_lunge(reduce_velocity := true) -> void:
	if lunge_timer <= 0.0: return
	lunge_timer = 0.0
	if reduce_velocity:
		velocity = clamp_magnitude(velocity, Clock.f32(speed_limit * time_scaler)) * 0.5
		velocity *= Clock.f32(0.3)

func stop_dash(recharge_dash := false, reduce_velocity := false) -> void:
	# Source stops the timer now; IsDashing clears in the next CalculateDash.
	if dash_timer < dash_duration:
		dash_cooldown_timer = Clock.f32(dash_cooldown_timer - (dash_duration - dash_timer))
		dash_timer = dash_duration
	if reduce_velocity: velocity = -0.5 * clamp_magnitude(velocity, Clock.f32(speed_limit * 0.5))
	if recharge_dash:
		dash_cooldown_timer = 0.0
		if not is_lunging:
			can_walk = true
			can_rotate = true
			can_dash = true

func respond_to_actor(normal: Vector2, other: OriginalActorMotion) -> void:
	# Actor.CollisionEnter2D locomotion branch. Inventory exchange, fire transfer,
	# kill attribution and Player dash-disarm effects require the combat layer.
	if state == State.DEAD: return
	if other.is_decoy:return
	if not is_lunging: velocity = reflect(velocity_stored, normal) * slide_factor
	var reflected := reflect(velocity_stored - other.velocity_stored, normal)
	var ratio := maxf(Clock.f32(Clock.f32(1.0 / time_scaler) * other.time_scaler),
		Clock.f32(Clock.f32(1.0 / other.time_scaler) * time_scaler))
	if not is_dashing:
		if other.is_dashing:
			stop_aiming()
			stop_lunge()
			start_pushback(normalized(other.velocity_stored) * ratio * 660.0 * 0.5, 0.19, 0.4)
		else:
			velocity += other.velocity_stored * ratio * slide_factor
			start_pushback(reflected, 0.2)
	elif other.is_dashing:
		start_pushback(reflected * 0.75, 0.2, 0.66)
	else:
		if dash_timer < Clock.f32(dash_duration * Clock.f32(0.66)):
			dash_timer = Clock.f32(dash_duration * Clock.f32(0.66))
		velocity = Vector2.ZERO

func get_move_speed() -> float:
	var amount := 230.0 if is_disarmed else 200.0
	if move_faster: amount = Clock.f32(amount * Clock.f32(1.4))
	if is_aiming:
		amount = 0.0 # Actor.aimSpeed has no serialized value or assignment.
	elif aim_move_lag > 0.0:
		aim_move_lag = Clock.f32(aim_move_lag - Clock.f32(fixed_delta * time_scaler * 2.0))
		amount = Clock.lerp32(amount, 0.0, Clock.ease_out(aim_move_lag))
	if retrieve_charge > 0.0:
		amount = Clock.f32(0.1) if controlled_disc else 0.0
	if is_slimed: amount = Clock.f32(amount * Clock.f32(0.33))
	return Clock.f32(amount * clampf(time_scaler, 1.0, 1.75))

func calculate_walk() -> Vector2:
	# Player.CalculateWalk reverses the sampled input before Actor's guards.
	if reverse_inputs: input_move *= -1.0
	if can_rotate:
		if magnitude(input_move) > 0.0: last_input_move = input_move
		else: aim_move_lag = 0.0
	if not can_walk or is_stunned or is_showing_off or is_frozen or is_lunging or state != State.ALIVE or is_spawning_in:
		return Vector2.ZERO
	return input_move * get_move_speed()

func dash() -> bool:
	if state == State.DEAD or (state == State.FALLING and fall_timer > Clock.f32(0.33)):
		return false
	if is_frozen:
		if elements_hook != null and elements_hook.get_ref() != null: elements_hook.get_ref().chip()
		return false
	if not can_dash or is_stunned or is_lunging or is_showing_off:
		return false
	if state == State.FALLING:
		if falls_saved != 0: return false
		if environment_hook != null and environment_hook.get_ref() != null:
			environment_hook.get_ref().fall_saved()
		elif not is_sliced:
			falls_saved += 1
			state = State.ALIVE
			is_falling = false
	dash_direction = normalized(input_move) if magnitude(input_move) > 0.0 else normalized(last_rotation)
	can_walk = false
	can_rotate = false
	can_dash = false
	stop_aiming()
	dash_timer = 0.0
	dash_cooldown_timer = dash_cooldown
	dash_force = 660.0
	if is_slimed: dash_cooldown_timer = Clock.f32(dash_cooldown_timer * 1.5)
	if environment_hook != null and environment_hook.get_ref() != null:
		stop_dash_on_hazards = not environment_hook.get_ref().is_dash_safe(dash_direction)
	is_dashing = true
	velocity += Clock.f32(speed_limit * 1.5) * dash_direction * time_scaler
	velocity_stored = velocity * clampf(Clock.f32(1.0 - drag * fixed_delta), 0.0, 1.0)
	last_rotation = dash_direction
	last_input_move = dash_direction
	rotation_z = atan2(dash_direction.y, dash_direction.x) - PI / 2.0
	if move_faster:
		dash_force = Clock.f32(dash_force * 1.25)
		dash_timer = Clock.f32(dash_duration * Clock.f32(0.2))
		dash_cooldown_timer = Clock.f32(dash_cooldown * Clock.f32(0.6))
	if dash_through_walls:
		insides_enabled = true
		collision_layer ^= 256
	return true

func calculate_dash() -> Vector2:
	if is_dashing and dash_timer >= dash_duration:
		if dash_through_walls:
			if world != null and world.overlap_circle(position,Clock.f32(0.3),256):
				dash_timer = Clock.f32(dash_duration * Clock.f32(0.4))
				if dash_cooldown_timer < dash_timer: dash_cooldown_timer = dash_duration
				return dash_direction * current_dash_amount
			collision_layer |= 256
		insides_enabled = false
	if dash_timer < dash_duration:
		current_dash_amount = Clock.lerp32(dash_force, 0.0, Clock.ease_in(Clock.f32(dash_timer / dash_duration)))
		dash_timer = Clock.f32(dash_timer + fixed_delta * time_scaler)
		return dash_direction * current_dash_amount
	if is_dashing:
		is_dashing = false
		dash_timer = dash_duration
		if environment_hook != null and environment_hook.get_ref() != null:
			environment_hook.get_ref().on_dash_end()
	return Vector2.ZERO

func cooldown_dash() -> void:
	if dash_cooldown_timer > 0.0:
		dash_cooldown_timer = Clock.f32(dash_cooldown_timer - fixed_delta * time_scaler)
		if dash_cooldown_timer > Clock.f32(dash_cooldown_move_threshold * dash_cooldown):
			can_walk = false
			can_rotate = false
		elif dash_cooldown_timer < dash_duration:
			can_walk = true
			can_rotate = true
			can_dash = true
	else:
		dash_cooldown_timer = 0.0

func fixed_step() -> void:
	collision_events.clear()
	if not physics_enabled:
		move_speed_parameter = 0.0
		return
	for protections in [deflected_discs, deflected_melees]:
		for id in protections.keys():
			protections[id] -= 1
			if protections[id] <= 0: protections.erase(id)
	if (not is_dashing or dash_timer > Clock.f32(dash_duration * Clock.f32(0.8))) and on_wet_floor:
		slide_factor = Clock.f32(0.25)
		drag = 1.0
	elif Clock.f32(1.0 - slide_factor) < Clock.f32(0.1):
		slide_factor = 1.0
		drag = 11.0
	else:
		var t := Clock.f32(fixed_delta * 7.0)
		slide_factor = Clock.lerp32(slide_factor, 1.0, t)
		drag = Clock.lerp32(drag, 11.0, t)
	if is_frozen: drag = 2.0
	if state != State.DEAD:
		# The source intentionally applies fixedDeltaTime twice to this force.
		velocity += calculate_pushback() * fixed_delta * slide_factor
		if elements_hook != null and elements_hook.get_ref() != null: elements_hook.get_ref().contacts()
		velocity += calculate_walk() * fixed_delta * time_scaler * slide_factor
		velocity += calculate_dash() * fixed_delta * time_scaler * slide_factor
		if environment_hook != null and environment_hook.get_ref() != null:
			environment_hook.get_ref().stop_dash_on_hazards()
		cooldown_dash()
		velocity += calculate_lunge() * fixed_delta * time_scaler * slide_factor
		if environment_hook != null and environment_hook.get_ref() != null:
			environment_hook.get_ref().stop_lunge_on_hazards()
		if can_rotate and not is_lunging and not is_frozen and state == State.ALIVE:
			last_rotation = stunned_direction if is_stunned else last_input_move
			var desired := atan2(last_rotation.y, last_rotation.x) - PI / 2.0
			rotation_z = lerp_angle(rotation_z, desired, clampf(fixed_delta * time_scaler * 12.0, 0.0, 1.0))
		if not is_stunned and not is_showing_off and not is_frozen and not is_dashing:
			move_speed_parameter = minf(Clock.f32(magnitude(velocity) / 11.0), 2.0)
			if slide_factor < 1.0: move_speed_parameter = Clock.f32(move_speed_parameter * clampf(Clock.f32(slide_factor * 2.0), 0.0, 1.0))
			elif is_lunging: move_speed_parameter = 0.0
			if move_speed_parameter <= 0.25: move_speed_parameter = 0.0
	move_actor()

func move_actor() -> void:
	velocity *= time_scaler
	if magnitude(velocity) > 0.0:
		var loss := drag * fixed_delta * clampf(time_scaler, 1.0, 4.0)
		velocity *= clampf(Clock.f32(1.0 - loss), 0.0, 1.0)
		if elements_hook != null and elements_hook.get_ref() != null: elements_hook.get_ref().note_previous_move(element_move_amount)
	else: velocity = Vector2.ZERO
	if world != null and not (is_dashing and dash_through_walls): position = world.separate_from_obstacles(position, radius, 256)
	velocity = clamp_magnitude(velocity, Clock.f32(Clock.f32(speed_limit * (2.0 if is_dashing else 1.0)) * time_scaler))
	var amount := collide_with_obstacles(velocity) if world != null and magnitude(velocity) > 0.0 else velocity
	element_move_amount = amount
	position += amount * fixed_delta
	velocity_stored = velocity
	velocity /= time_scaler

func collide_with_obstacles(amount: Vector2, depth := 0) -> Vector2:
	depth += 1
	var direction := normalized(amount)
	var hit: Dictionary = world.circle_cast(position, radius, direction, Clock.f32(magnitude(amount) * fixed_delta), collision_layer | destructables_layer, query_id)
	if hit.is_empty(): return amount
	collision_events.append(hit)
	if hit.get("actor", false):
		var other: OriginalActorMotion = hit.shape.actor_ref.get_ref()
		respond_to_actor(hit.normal, other)
		other.respond_to_actor(normalized(other.position - position), self)
		if other.is_decoy:return amount
		# Original player branch returns a displacement here; MoveActor still
		# multiplies it by fixedDeltaTime. Keep this original behavior explicit.
		return hit.centroid - position - direction * Clock.f32(0.01)
	var result := Vector2.ZERO
	if depth < 2:
		var projection := Clock.f32(Clock.f32(amount.x * hit.normal.x) + Clock.f32(amount.y * hit.normal.y))
		result = collide_with_obstacles(amount - hit.normal * projection - direction * Clock.f32(0.01), depth)
	velocity = result
	return result

func snapshot() -> Dictionary:
	return {"position": [position.x, position.y], "velocity": [velocity.x, velocity.y],
		"stored": [velocity_stored.x, velocity_stored.y], "dash_timer": dash_timer,
		"cooldown": dash_cooldown_timer, "dashing": is_dashing, "can_walk": can_walk,
		"can_dash": can_dash, "slide": slide_factor, "drag": drag,
		"push_timer": push_timer, "push_strength": push_strength, "stun_timer": stun_timer, "stunned": is_stunned}
