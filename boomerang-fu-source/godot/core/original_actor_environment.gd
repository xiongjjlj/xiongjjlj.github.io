class_name OriginalActorEnvironment
extends RefCounted
## Actor's floor/fall state machine. Moving-platform attachment, effects and
## animation blending remain separate from these source arithmetic rules.
const C = preload("res://core/original_timekeeper.gd")
const A = preload("res://core/original_actor_motion.gd")
var actor: OriginalActorMotion
var ground: OriginalGroundQueries
var parent_kind := 0 # 0: none; 1: MovingFloor; 2: other parent.
var parent_velocity := Vector2.ZERO
var parent_shape_id := -1
var fall_animation := false
var frozen_animation := false
var hop_count := 0
var footstep_count := 0
var hazard_count := 0
var events: Array[Dictionary] = []
var boundary:OriginalLevelBoundary

func initialize(motion: OriginalActorMotion, queries: OriginalGroundQueries) -> void:
	actor = motion
	ground = queries
	actor.environment_hook = weakref(self)

func over_hazard(point: Vector2) -> bool:
	return ground.over_hazard(point,C.f32(actor.radius * C.f32(1.15)))

func before_fixed_step() -> void:
	if not actor.physics_enabled: return
	if not actor.is_dashing and not actor.is_falling and actor.state != A.State.FALLING:
		if actor.state == A.State.ALIVE and over_hazard(actor.position) and A.magnitude(actor.velocity) < C.f32(actor.speed_limit * C.f32(0.8)):
			start_fall()
		elif actor.state == A.State.DEAD and over_hazard(actor.position):
			fall_in_hazard()

func start_fall() -> void:
	fall_animation = true
	frozen_animation = false
	if parent_kind == 1:
		var floor_direction := A.normalized(parent_velocity)
		var direction := A.normalized(actor.velocity)
		var dot := C.f32(floor_direction.x * direction.x + floor_direction.y * direction.y)
		actor.velocity += parent_velocity * 12.0 * clampf(dot,0.0,1.0) * actor.slide_factor
	elif parent_kind == 2:
		actor.velocity = A.clamp_magnitude(actor.velocity,C.f32(actor.speed_limit * C.f32(0.33)))
	elif not over_hazard(actor.position - A.normalized(actor.velocity_stored)) and over_hazard(actor.position + A.normalized(actor.velocity_stored) * 2.0):
		actor.velocity = A.normalized(actor.velocity) * actor.speed_limit * C.f32(0.6)
	else:
		actor.velocity = A.clamp_magnitude(actor.velocity,C.f32(actor.speed_limit * C.f32(0.33)))
	actor.fall_timer = 0.0
	actor.state = A.State.FALLING
	actor.slide_factor = 1.0
	actor.is_falling = true
	actor.fall_duration = C.f32(0.74 if ground.off_cliff(actor.position) else 0.66)
	actor.last_rotation = Vector2(0,-1)
	actor.last_input_move = Vector2(0,-1)
	actor.rotation_z = PI
	# Player.StartFall adds these two state changes to Actor.StartFall.
	actor.stop_aiming()
	actor.appearance_visible = true
	events.append({"type":"fall_start"})

func fall_saved() -> void:
	if not actor.is_sliced and actor.falls_saved <= 0:
		fall_animation = false
		if actor.is_frozen: frozen_animation = true
		actor.state = A.State.ALIVE
		actor.is_falling = false
		actor.falls_saved += 1
		events.append({"type":"fall_saved"})

func frame_step(delta: float) -> void:
	if not actor.is_falling: return
	if actor.fall_timer < actor.fall_duration:
		if actor.time_scaler == 1.0 and actor.fall_timer < C.f32(0.33) and not over_hazard(actor.position) and actor.state == A.State.FALLING:
			fall_saved()
			footstep_count += 1
		var hop_at := C.f32(actor.fall_duration * C.f32(0.15))
		if actor.fall_timer < hop_at and actor.fall_timer + C.f32(delta) * actor.time_scaler > hop_at:
			hop_count += 1
		# Source finishes this entered branch even if FallSaved cleared isFalling.
		actor.fall_timer = C.f32(actor.fall_timer + C.f32(delta) * actor.time_scaler)
	else:
		actor.is_falling = false
		if actor.state == A.State.FALLING:
			events.append({"type":"fall_death"})
		else: fall_in_hazard()

func fall_in_hazard() -> void:
	actor.is_frozen = false
	if actor.elements_hook != null and actor.elements_hook.get_ref() != null: actor.elements_hook.get_ref().toggle_fire(false,true)
	fall_animation = false
	actor.appearance_visible = false
	actor.physics_enabled = false
	hazard_count += 1
	events.append({"type":"fall_hidden","cliff":ground.off_cliff(actor.position)})

func on_dash_end() -> void:
	if actor.falls_saved > 0 and not over_hazard(actor.position): actor.falls_saved = 0

func inside_obstacle() -> bool:
	return ground.world.overlap_circle(actor.position,C.f32(0.3),256)

func will_move_into_hazard() -> bool:
	var radius := C.f32(actor.radius * C.f32(0.95))
	return ground.hazard_contacts(actor.position + actor.velocity * actor.fixed_delta,radius,false)>0 and ground.hazard_contacts(actor.position,radius)<8 and over_hazard(actor.position + A.normalized(actor.velocity) * actor.radius * 2.0)

func stop_dash_on_hazards() -> void:
	if actor.stop_dash_on_hazards and not actor.can_walk and not actor.is_stunned and actor.dash_through_walls and inside_obstacle(): return
	if actor.stop_dash_on_hazards and not actor.can_walk and not actor.is_stunned and actor.dash_timer > C.f32(actor.dash_duration * 0.5) and will_move_into_hazard(): actor.stop_dash(false,true)

func stop_lunge_on_hazards() -> void:
	if actor.is_lunging and actor.stop_lunge_on_hazards and will_move_into_hazard(): actor.stop_lunge()

func is_lunge_safe(direction: Vector2) -> bool:
	var endpoint := actor.position + direction * C.f32(4.4)
	var hit := ground.world.circle_cast(actor.position,0.0,direction,C.f32(4.4),256)
	if not hit.is_empty(): endpoint = hit.point
	return not over_hazard(endpoint)

func moving_floor_at(point: Vector2) -> Dictionary:
	var hits := ground.world.overlaps(point,actor.radius,ground.floor_mask)
	return hits[0].shape.get("moving_floor",{}) if not hits.is_empty() else {}

func is_dash_safe(direction: Vector2,avoid_obstacles:=false,allow_outside:=true,allow_hazard_end:=true) -> bool:
	# Default Actor.IsDashSafe flags used by FFA Dash: allow out-of-bounds,
	# allow a hazard beyond the endpoint, and permit an obstacle on the path.
	direction = A.normalized(direction)
	if actor.slide_factor < 1.0: return false
	var residual := actor.velocity * C.f32(pow(clampf(C.f32(1.0 - actor.drag * actor.fixed_delta),0.0,1.0),C.f32(actor.dash_duration / actor.fixed_delta))) * C.f32(0.8)
	var invulnerable_end := actor.position + direction * C.f32(9.45) + residual
	var endpoint := actor.position + direction * C.f32(11.06) + residual
	var moving := moving_floor_at(invulnerable_end)
	if moving.is_empty(): moving = moving_floor_at(endpoint)
	if not moving.is_empty() and int(moving.id) != parent_shape_id:
		invulnerable_end -= moving.velocity * actor.dash_duration
		endpoint -= moving.velocity * actor.dash_duration
	if parent_kind == 1:
		var multiplier := 1.0
		var to_start := actor.position - invulnerable_end
		var floor_hit := ground.world.circle_cast(invulnerable_end,0.0,A.normalized(to_start),A.magnitude(to_start),ground.floor_mask)
		if not floor_hit.is_empty() and int(floor_hit.shape.id)==parent_shape_id:
			multiplier = C.f32(A.magnitude(floor_hit.point - actor.position) / A.magnitude(invulnerable_end - actor.position))
		invulnerable_end += parent_velocity * actor.dash_duration * multiplier
		endpoint += parent_velocity * actor.dash_duration * multiplier
	var hit := ground.world.circle_cast(actor.position,actor.radius,direction,A.magnitude(endpoint - actor.position),256)
	if not hit.is_empty():
		if actor.dash_through_walls and avoid_obstacles:return false
		if not actor.dash_through_walls:
			if avoid_obstacles and A.magnitude(hit.point-endpoint)>C.f32(C.f32(11.06)*C.f32(0.3)):return false
			if not ground.point_on_layer(hit.point,ground.floor_mask) or not ground.point_on_layer(hit.point - A.normalized(direction),ground.floor_mask): return false
			var path := invulnerable_end - actor.position
			var interpolated := Vector2(C.lerp32(-hit.normal.x,A.normalized(actor.velocity).x,0.5),C.lerp32(-hit.normal.y,A.normalized(actor.velocity).y,0.5))
			var corrected: Vector2 = hit.point - interpolated * actor.radius * C.f32(1.05)
			var remainder: Vector2 = path - (corrected - actor.position)
			var dot := C.f32(remainder.x * hit.normal.x + remainder.y * hit.normal.y)
			var tangent: Vector2 = remainder - hit.normal * dot
			invulnerable_end = corrected + tangent
			endpoint = invulnerable_end + A.normalized(tangent) * C.f32(C.f32(11.06)-C.f32(9.45))
	else:
		# Retain the source's redundant second query.
		hit = ground.world.circle_cast(actor.position,actor.radius,direction,A.magnitude(endpoint-actor.position),256)
		if not hit.is_empty(): endpoint = hit.point + A.reflect(direction,hit.normal) * actor.radius * 1.5
	if not allow_outside and boundary!=null and (not boundary.is_in_bounds(invulnerable_end) or not boundary.is_in_bounds(endpoint)):return false
	if not allow_hazard_end and over_hazard(endpoint+direction):return false
	return not over_hazard(invulnerable_end) and not over_hazard((invulnerable_end+endpoint)*0.5) and not over_hazard(endpoint)
