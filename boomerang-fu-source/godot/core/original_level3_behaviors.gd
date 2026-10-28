class_name OriginalLevel3Behaviors
extends RefCounted
## Shared Unity map behaviours recovered from serialized MonoBehaviours.
## MovingObstacle follows the source FixedUpdate/Toggler state machine. Wobbly
## and DestructableObstacle are kept deterministic and use the same collider
## objects as the scene loader, so native/browser runs share one event path.
const C = preload("res://core/original_timekeeper.gd")
const FIXED_DELTA := 0.016666
const MOVING := 1
const PRE_MOVING := 2
const SPRING_PAUSING := 3
var nodes: Dictionary = {}
var node_to_id: Dictionary = {}
var debris_configs: Dictionary = {}
var collision_world: OriginalCollisionWorld
var obstacles: Dictionary = {}
var switches: Dictionary = {}
var wobbles: Dictionary = {}
var destructables: Dictionary = {}
var debris_states: Array[Dictionary] = []
var source_counts: Dictionary = {}
var accumulator := 0.0
var events: Array[Dictionary] = []
var initialized := false
var interaction_cooldowns: Dictionary = {}
var melee_contacts: Dictionary = {}
var disc_contacts: Dictionary = {}
var rng := OriginalUnityRandom.new(0xB00F1)
var fixed_ticks := 0

func initialize(sidecar: Dictionary, scene_nodes: Dictionary, world: OriginalCollisionWorld, shared_rng = null) -> void:
	if shared_rng != null: rng = shared_rng
	nodes = scene_nodes
	node_to_id.clear()
	for object_id in nodes.keys(): node_to_id[nodes[object_id].get_instance_id()] = int(object_id)
	collision_world = world
	obstacles.clear(); switches.clear(); wobbles.clear(); destructables.clear(); debris_states.clear(); debris_configs.clear()
	source_counts = sidecar.get("class_counts", {})
	initialized = false
	for record in sidecar.get("behaviours", []):
		var cls := str(record.get("class", ""))
		var component_id := int(record.get("component_id", 0))
		var game_object := int(record.get("game_object", 0))
		if cls == "MovingObstacle":
			var data: Dictionary = record.get("data", {})
			var points: Array = data.get("positions", [])
			var rotating := int(data.get("isRotatingObstacle", 0)) != 0
			if (points.size() < 2 and not rotating) or not nodes.has(game_object): continue
			var node: Node3D = nodes[game_object]
			var positions: Array[Vector2] = []
			for point in points:
				positions.append(Vector2(float(point.get("x", 0.0)), float(point.get("y", 0.0))))
			obstacles[component_id] = {"component_id": component_id, "game_object": game_object, "node": node,
				"positions": positions,
				"z": node.position.z, "pos_index": 0, "state": 0, "reverse": false, "springing_back": false,
				"elapsed": 0.0, "move_percent": 0.0, "move_dur": float(data.get("moveDur", 0.5)),
				"pause_dur": float(data.get("springPauseDur", 0.25)), "windback_dur": float(data.get("springWindBackDur", 2.0)),
				"pre_dur": float(data.get("preMoveDur", 0.35)), "is_spring": int(data.get("isSpring", 0)) != 0, "initial": node.position,
				"rotating": rotating, "clockwise": int(data.get("isRotatingClockwise", 1)) != 0,
				"rotating_dur": float(data.get("rotatingDur", 4.0)), "rotating_durations": data.get("rotatingDurations", []),
				"initial_rotation": node.rotation.z, "initial_clockwise": int(data.get("isRotatingClockwise", 1)) != 0,
				"initial_duration": float(data.get("rotatingDur", 4.0)), "angular_acceleration": 0.0,
				"randomized_start": int(data.get("randomizedStart", 0)) != 0,
				"enabled": int(data.get("m_Enabled", 1)) != 0 and bool(record.get("active_in_hierarchy", node.is_visible_in_tree())),
				"start_pos": Vector2.ZERO, "target_pos": Vector2.ZERO, "velocity": Vector2.ZERO}
		elif cls == "Switch":
			var data: Dictionary = record.get("data", {})
			var targets: Array[int] = []
			for ref in data.get("movingObstacles", []):
				var target := int(ref.get("m_PathID", ref.get("path_id", 0)))
				if target != 0: targets.append(target)
			if nodes.has(game_object):
				switches[component_id] = {"component_id": component_id, "game_object": game_object, "node": nodes[game_object], "targets": targets,
					"cooldown": 0.0, "active": int(data.get("isActive", 1)) != 0, "cooldown_dur": float(data.get("cooldownDur", 0.5)), "triggered": false,
					"initial_active": int(data.get("isActive", 1)) != 0, "one_shot": int(data.get("oneShotOnly", 0)) != 0,
					"enabled": int(data.get("m_Enabled", 1)) != 0 and bool(record.get("active_in_hierarchy", nodes[game_object].is_visible_in_tree()))}
		elif cls == "WobblyObstacle":
			var refs: Dictionary = record.get("resolved_refs", {})
			if not nodes.has(game_object) or not nodes.has(int(refs.get("root", 0))) or not nodes.has(int(refs.get("spring", 0))): continue
			var data: Dictionary = record.get("data", {})
			wobbles[component_id] = {"component_id": component_id, "game_object": game_object, "node": nodes[game_object],
				"root": nodes[int(refs.root)], "spring": nodes[int(refs.spring)], "target": nodes.get(int(refs.get("spring_target", 0))),
				"offset": Vector2.ZERO, "velocity": Vector2.ZERO, "root_rotation": nodes[int(refs.root)].rotation.z,
				"drag": float(data.get("drag", 20.0)), "spring_force": float(data.get("springForce", 800.0)),
				"max_distance": float(data.get("maxSpringDistance", 5.0)), "input_modifier": float(data.get("inputForceModifier", 1.0)),
				"initial_root_rotation": nodes[int(refs.root)].rotation.z, "enabled": bool(record.get("active_in_hierarchy", true)) and int(data.get("m_Enabled", 1)) != 0}
		elif cls == "Debris3D":
			# Keep the serialized movement constants with the corresponding
			# rendered child; DestructableObstacle releases these objects later.
			debris_configs[game_object] = record.get("data", {})
		elif cls == "DestructableObstacle":
			var refs: Dictionary = record.get("resolved_refs", {})
			if not nodes.has(game_object): continue
			var parent_id := int(refs.get("derisParent", 0))
			var parent_node: Node3D = nodes.get(parent_id)
			var debris: Array[Node3D] = []
			if parent_node != null:
				for child in parent_node.get_children():
					if child is Node3D: debris.append(child)
			destructables[component_id] = {"component_id": component_id, "game_object": game_object, "node": nodes[game_object],
				"parent": parent_node, "debris": debris, "hits_left": maxi(1, int(record.get("data", {}).get("hitsLeft", 1))),
				"initial_hits": maxi(1, int(record.get("data", {}).get("hitsLeft", 1))),
				"destroyed": false, "initial_visible": nodes[game_object].visible,
				"enabled": bool(record.get("active_in_hierarchy", true)), "debris_transforms": debris.map(func(child): return child.transform)}
	reset(false)
	initialized = true
	print("BF_LEVEL3_BEHAVIORS " + JSON.stringify({"moving_obstacles": obstacles.size(), "switches": switches.size(), "wobbly_obstacles": wobbles.size(), "destructable_obstacles": destructables.size(), "source_counts": source_counts}))

func reset(randomize := true) -> void:
	accumulator = 0.0; fixed_ticks = 0
	events.clear(); interaction_cooldowns.clear(); debris_states.clear(); melee_contacts.clear(); disc_contacts.clear()
	for o in obstacles.values():
		var node: Node3D = o.node
		o.pos_index = 0; o.state = 0; o.reverse = false; o.springing_back = false; o.elapsed = 0.0; o.move_percent = 0.0
		o.clockwise = o.initial_clockwise; o.rotating_dur = o.initial_duration; o.angular_acceleration = 0.0; o.velocity = Vector2.ZERO
		node.position = o.initial; node.rotation.z = float(o.initial_rotation)
		if randomize and o.enabled and o.randomized_start and o.positions.size() > 1 and rng.value() > 0.5:
			o.pos_index = 1
			# MovingObstacle.Start assigns Vector2 directly, setting local Z to zero.
			node.position = Vector3(o.positions[1].x, o.positions[1].y, 0)
		if o.rotating and o.rotating_durations.size() > 1: o.rotating_dur = float(o.rotating_durations[0])
		o.start_pos = Vector2(node.position.x, node.position.y); o.target_pos = o.start_pos
		collision_world.sync_scene_subtree(int(o.game_object))
	for item in switches.values():
		item.cooldown = 0.0; item.triggered = false; item.active = item.initial_active
	for wobble in wobbles.values():
		wobble.offset = Vector2.ZERO; wobble.velocity = Vector2.ZERO
		wobble.root.rotation.z = float(wobble.initial_root_rotation)
	for item in destructables.values():
		item.destroyed = false; item.hits_left = int(item.initial_hits)
		item.node.visible = bool(item.initial_visible)
		collision_world.set_scene_object_enabled(item.game_object, bool(item.initial_visible))
		for i in item.debris.size():
			item.debris[i].transform = item.debris_transforms[i]
			item.debris[i].visible = false

func toggle(component_id: int, reason := "script") -> bool:
	if not obstacles.has(component_id): return false
	var o: Dictionary = obstacles[component_id]
	if not o.enabled: return false
	if o.rotating:
		if o.rotating_durations.size() > 1:
			o.pos_index = (int(o.pos_index) + 1) % o.rotating_durations.size()
		else: o.clockwise = not bool(o.clockwise)
		events.append({"type": "moving_obstacle_toggle", "component_id": component_id, "reason": reason, "rotating": true, "clockwise": bool(o.clockwise)})
		return true
	var spring_back := reason == "spring_back"
	if o.state == SPRING_PAUSING:
		o.state = MOVING
	else:
		o.start_pos = o.positions[int(o.pos_index)]
		if (o.state == MOVING or o.state == PRE_MOVING) and not o.reverse:
			o.pos_index = posmod(int(o.pos_index) - 1, o.positions.size()); o.reverse = true
		else:
			o.pos_index = (int(o.pos_index) + 1) % o.positions.size(); o.reverse = false
		o.target_pos = o.positions[int(o.pos_index)]
		o.move_percent = C.f32(1.0 - float(o.move_percent)) if o.state == MOVING or o.state == PRE_MOVING else 0.0
		o.state = SPRING_PAUSING if spring_back else PRE_MOVING
	o.elapsed = 0.0; o.springing_back = spring_back
	events.append({"type": "moving_obstacle_toggle", "component_id": component_id, "reason": reason, "target_index": int(o.pos_index)})
	return true

func _step_obstacle(o: Dictionary, delta: float) -> void:
	if not o.enabled: return
	delta = C.f32(delta)
	if o.rotating:
		o.state = MOVING
		if o.rotating_durations.size() > 1:
			var target := float(o.rotating_durations[int(o.pos_index)])
			if o.rotating_dur < target:
				# Decompiled Mathf.SmoothDamp with smoothTime=.5, maxSpeed=Infinity.
				var omega := 4.0
				var x := C.f32(omega * delta)
				var decay := C.f32(1.0 / (1.0 + x + 0.48 * x * x + 0.235 * x * x * x))
				var change := C.f32(float(o.rotating_dur) - target)
				var temp := C.f32((float(o.angular_acceleration) + omega * change) * delta)
				o.angular_acceleration = C.f32((float(o.angular_acceleration) - omega * temp) * decay)
				o.rotating_dur = C.f32(target + (change + temp) * decay)
			else: o.rotating_dur = target
		var sign_value := -1.0 if bool(o.clockwise) else 1.0
		var degrees := C.f32(C.f32(sign_value * 360.0 / float(o.rotating_dur)) * delta)
		o.node.rotate_object_local(Vector3.BACK, deg_to_rad(degrees))
		collision_world.sync_scene_subtree(int(o.game_object))
		return
	if o.state == 0: return
	o.elapsed = C.f32(float(o.elapsed) + delta)
	if o.state == PRE_MOVING or o.state == SPRING_PAUSING:
		var wait_duration := float(o.pre_dur if o.state == PRE_MOVING else o.pause_dur)
		if o.elapsed < wait_duration:
			o.velocity = Vector2.ZERO
			return
		o.state = MOVING
	var before: Vector3 = o.node.global_position
	if o.move_percent < 1.0:
		var duration := float(o.windback_dur if o.springing_back else o.move_dur)
		o.move_percent = C.f32(float(o.move_percent) + C.f32(delta / duration))
		var t := clampf(float(o.move_percent), 0.0, 1.0)
		var eased := C.ease_in(t) if o.reverse else C.ease_out(t)
		var start: Vector2 = o.start_pos; var target: Vector2 = o.target_pos
		o.node.position = Vector3(C.lerp32(start.x, target.x, eased), C.lerp32(start.y, target.y, eased), o.z)
	else:
		# The original transitions on the following FixedUpdate, not the tick
		# that first increments movePercent past 1.
		o.node.position = Vector3(o.target_pos.x, o.target_pos.y, o.z); o.state = 0
		if o.is_spring and not o.springing_back and int(o.pos_index) == 1: toggle(int(o.component_id), "spring_back")
		o.reverse = false
	var after: Vector3 = o.node.global_position
	o.velocity = Vector2(after.x - before.x, after.y - before.y) / delta
	collision_world.sync_scene_subtree(int(o.game_object))

func _switch_activate(item: Dictionary, reason := "contact") -> void:
	if not item.enabled or not item.active or item.cooldown > 0.0: return
	if item.one_shot: item.active = false
	for target in item.targets: toggle(int(target), reason)
	item.cooldown = item.cooldown_dur; item.triggered = true
	events.append({"type": "switch_activate", "component_id": int(item.component_id), "game_object": int(item.game_object), "reason": reason})

func _wobble_apply(w: Dictionary, force: Vector2) -> void:
	if not w.enabled: return
	if force.length_squared() <= 1.0: return
	w.velocity += force * float(w.input_modifier)
	events.append({"type": "wobbly_obstacle_hit", "component_id": int(w.component_id), "force": [force.x, force.y]})

func _step_wobble(w: Dictionary, delta: float) -> void:
	if not w.enabled: return
	var acceleration: Vector2 = -float(w.spring_force) * w.offset - float(w.drag) * w.velocity
	w.velocity += acceleration * delta; w.offset += w.velocity * delta
	if w.offset.length() > w.max_distance:
		w.offset = w.offset.normalized() * w.max_distance; w.velocity *= 0.25
	# A Unity LookRotation around the horizontal spring is rendered as a small
	# 2D lean here; this keeps the mesh pivot and collider in the same node.
	w.root.rotation.z = float(w.initial_root_rotation) + clampf(w.offset.x * 0.06 - w.offset.y * 0.06, -0.35, 0.35)

func _destroy(item: Dictionary, force: Vector2) -> void:
	if item.destroyed: return
	item.destroyed = true; item.node.visible = false
	collision_world.set_scene_object_enabled(item.game_object, false)
	var parent: Node3D = item.parent
	if parent != null: parent.visible = true
	var direction := force.normalized() if force.length_squared() > 0.001 else Vector2.UP
	var index := 0
	for child in item.debris:
		child.visible = true
		var radial := Vector2(cos(float(index) * 2.4), sin(float(index) * 2.4))
		var config: Dictionary = debris_configs.get(int(node_to_id.get(child.get_instance_id(), -1)), {})
		var speed := float(config.get("speed", 10.0))
		var spin_start := float(config.get("spinSpeedStart", 50.0))
		var lifetime := maxf(0.25, float(config.get("lifetimeDuration", 0.5)) + float(config.get("vanishDuration", 0.75)))
		debris_states.append({"node": child, "velocity": direction * speed + radial * (speed * 0.12), "age": 0.0,
			"spin": deg_to_rad(-spin_start if index % 2 == 0 else spin_start), "lifetime": lifetime,
			"z_velocity": -float(config.get("speedZ", 20.0)), "gravity": 9.82 * float(config.get("gravityMod", 1.0)),
			"floor": float(config.get("floorThreshold", -0.3)), "bounciness": float(config.get("bounciness", 1.0)), "bounces": 0})
		index += 1
	events.append({"type": "destructable_destroy", "component_id": int(item.component_id), "game_object": int(item.game_object), "debris": item.debris.size()})

func _destructable_hit(item: Dictionary, force: Vector2) -> void:
	if not item.enabled or item.destroyed: return
	item.hits_left = int(item.hits_left) - 1
	if item.hits_left <= 0: _destroy(item, force)
	else: events.append({"type": "destructable_damage", "component_id": int(item.component_id), "hits_left": int(item.hits_left)})

func _step_debris(delta: float) -> void:
	for state in debris_states:
		state.age = float(state.age) + delta
		state.velocity.y = float(state.velocity.y) - 20.0 * delta
		state.z_velocity = float(state.z_velocity) + float(state.gravity) * delta
		var node: Node3D = state.node
		node.position += Vector3(state.velocity.x * delta, state.velocity.y * delta, state.z_velocity * delta)
		if node.position.z <= float(state.floor) and state.z_velocity > 0.0 and int(state.bounces) < 2:
			node.position.z = float(state.floor); state.z_velocity = -state.z_velocity * float(state.bounciness); state.bounces = int(state.bounces) + 1
		node.rotation.z += float(state.spin) * delta
		if state.age >= state.lifetime: node.visible = false
	debris_states = debris_states.filter(func(state: Dictionary) -> bool: return float(state.age) < float(state.lifetime))

func interact_disc_wall(motion, object_id: int, force: Vector2) -> void:
	# Disc movement reports the exact collider that stopped the sweep. Unity's
	# trigger callback reaches InteractiveObstacle before the next FixedUpdate;
	# route that event directly so a high-speed disc cannot tunnel through a
	# crate or bamboo spring between sampled positions.
	if motion == null or not motion.active: return
	for w in wobbles.values():
		if int(w.game_object) == object_id:
			var key := "%d:%d" % [int(w.component_id), motion.get_instance_id()]
			if float(interaction_cooldowns.get(key, 0.0)) <= 0.0:
				_wobble_apply(w, force); interaction_cooldowns[key] = 0.12
	for item in destructables.values():
		if int(item.game_object) != object_id or item.destroyed: continue
		var key := "%d:%d" % [int(item.component_id), motion.get_instance_id()]
		if float(interaction_cooldowns.get(key, 0.0)) <= 0.0:
			_destructable_hit(item, force); interaction_cooldowns[key] = 0.2

func interact_melee(actors: Array, combat_world) -> void:
	# Player melee/disarm colliders are trigger shapes in Unity. Use the exact
	# serialized melee polygon from OriginalCombatWorld so obstacle callbacks
	# share the same geometry as actor damage and parry checks.
	if combat_world == null: return
	var contacts_now: Dictionary = {}
	for index in actors.size():
		var actor = actors[index]
		if actor == null or actor.state == OriginalActorMotion.State.DEAD or not (actor.is_attacking or actor.is_disarming): continue
		var polygon: PackedVector2Array = combat_world.melee_polygon(index) if actor.is_attacking else combat_world.disarm_polygon(index)
		for item in switches.values():
			if not _polygon_touches_object(polygon, int(item.game_object)): continue
			var key := "%d:%d:%s" % [int(item.component_id), actor.get_instance_id(), "melee" if actor.is_attacking else "disarm"]
			contacts_now[key] = true
			var contact_center: Vector2 = combat_world.melee_center(index) if actor.is_attacking else combat_world.disarm_center(index)
			if not melee_contacts.has(key) and not _switch_blocked(item, contact_center): _switch_activate(item, "melee" if actor.is_attacking else "disarm")
		var force := Vector2(-sin(actor.rotation_z), cos(actor.rotation_z)) * 30.0
		for w in wobbles.values():
			var key := "%d:melee:%d" % [int(w.component_id), index]
			if float(interaction_cooldowns.get(key, 0.0)) > 0.0: continue
			if _polygon_touches_object(polygon, int(w.game_object)):
				_wobble_apply(w, force); interaction_cooldowns[key] = 0.12
		for item in destructables.values():
			if item.destroyed: continue
			var key := "%d:melee:%d" % [int(item.component_id), index]
			if float(interaction_cooldowns.get(key, 0.0)) > 0.0: continue
			if _polygon_touches_object(polygon, int(item.game_object)):
				_destructable_hit(item, force); interaction_cooldowns[key] = 0.2

	melee_contacts = contacts_now

func _polygon_touches_object(polygon: PackedVector2Array, object_id: int) -> bool:
	for shape in collision_world.shapes:
		if shape.actor or int(shape.object_id) != object_id or int(shape.layer) == 0: continue
		if shape.kind in ["polygon", "edge"]:
			if not Geometry2D.intersect_polygons(polygon, shape.vertices).is_empty(): return true
		else:
			for point in polygon:
				if point.distance_to(shape.center) <= float(shape.radius) + 0.1: return true
	return false

func _interact_disc(motion) -> void:
	if motion == null or not motion.active: return
	for w in wobbles.values():
		var key := "%d:%d" % [int(w.component_id), motion.get_instance_id()]
		if float(interaction_cooldowns.get(key, 0.0)) > 0.0: continue
		if Vector2(w.node.global_position.x, w.node.global_position.y).distance_to(motion.position) <= float(motion.radius) + 1.0:
			_wobble_apply(w, motion.velocity); interaction_cooldowns[key] = 0.12
	for item in destructables.values():
		if item.destroyed: continue
		for shape in collision_world.shapes:
			if shape.actor or int(shape.object_id) != int(item.game_object): continue
			if not OriginalCollisionWorld.penetration(motion.position, float(motion.radius), shape).is_empty():
				var key := "%d:%d" % [int(item.component_id), motion.get_instance_id()]
				if float(interaction_cooldowns.get(key, 0.0)) <= 0.0:
					_destructable_hit(item, motion.velocity); interaction_cooldowns[key] = 0.2
				break

func _switch_blocked(item: Dictionary, to: Vector2) -> bool:
	var from := Vector2(item.node.global_position.x, item.node.global_position.y)
	var direction := to - from
	return not collision_world.circle_cast(from, 0.0, direction, direction.length(), 256).is_empty()

func _switch_disc_contacts(flying_discs: Array) -> void:
	var contacts_now: Dictionary = {}
	for disc_item in flying_discs:
		var motion = disc_item.get("motion")
		if motion == null or not motion.active or not motion.projectile_layer or motion.collider_disabled_remaining > 0.0: continue
		for item in switches.values():
			var touching := false
			for shape in collision_world.shapes:
				if shape.actor or int(shape.object_id) != int(item.game_object) or int(shape.layer) == 0: continue
				if not OriginalCollisionWorld.penetration(motion.position, motion.collider_radius, shape).is_empty(): touching = true; break
			if not touching: continue
			var key := "%d:%d" % [int(item.component_id), motion.get_instance_id()]
			contacts_now[key] = true
			if disc_contacts.has(key) or _switch_blocked(item, motion.position): continue
			var up3: Vector3 = item.node.global_basis.y.normalized()
			var approach: bool = OriginalDiscMotion.dot32(OriginalActorMotion.normalized(motion.stored), Vector2(up3.x, up3.y)) < C.f32(0.1)
			var fresh_throw := false
			if motion.movement == OriginalDiscMotion.Movement.THROWING and motion.owner_actor != null:
				var owner_distance := OriginalActorMotion.magnitude_xyz(motion.position - motion.owner_actor.position, motion.source_z - motion.owner_actor.source_z)
				fresh_throw = owner_distance < C.f32(C.f32(0.7125) * 3.0)
			if approach or fresh_throw: _switch_activate(item, "projectile")
	disc_contacts = contacts_now

func frame_step(delta: float) -> void:
	if not initialized: return
	for item in switches.values():
		if item.cooldown > 0.0: item.cooldown = C.f32(float(item.cooldown) - C.f32(delta))
	for key in interaction_cooldowns.keys():
		interaction_cooldowns[key] = maxf(0.0, float(interaction_cooldowns[key]) - delta)
		if interaction_cooldowns[key] <= 0.0: interaction_cooldowns.erase(key)

func fixed_step(delta: float, actors: Array = [], flying_discs: Array = [], combat_world = null) -> void:
	if not initialized: return
	fixed_ticks += 1
	for obstacle in obstacles.values(): _step_obstacle(obstacle, delta)
	for wobble in wobbles.values(): _step_wobble(wobble, delta)
	_step_debris(delta)
	interact_melee(actors, combat_world)
	_switch_disc_contacts(flying_discs)
	for disc_item in flying_discs: _interact_disc(disc_item.get("motion"))

func step(delta: float, actors: Array = [], flying_discs: Array = []) -> void:
	# Standalone fixture driver; the game uses its shared fixed-step loop.
	frame_step(delta)
	accumulator += minf(delta, 0.3333333432674408)
	while accumulator >= C.f32(FIXED_DELTA):
		accumulator -= C.f32(FIXED_DELTA)
		fixed_step(C.f32(FIXED_DELTA), actors, flying_discs)

func drain_events() -> Array[Dictionary]:
	var result := events.duplicate(); events.clear(); return result
