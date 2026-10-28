class_name OriginalLevel3Behaviors
extends RefCounted
## Unity level3 gameplay behaviours recovered from serialized MonoBehaviours.
## MovingObstacle follows the source FixedUpdate/Toggler state machine. Wobbly
## and DestructableObstacle are kept deterministic and use the same collider
## objects as the scene loader, so native/browser runs share one event path.
const FIXED_DELTA := 1.0 / 60.0
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

func initialize(sidecar: Dictionary, scene_nodes: Dictionary, world: OriginalCollisionWorld) -> void:
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
			if points.size() < 2 or not nodes.has(game_object): continue
			var node: Node3D = nodes[game_object]
			obstacles[component_id] = {"component_id": component_id, "game_object": game_object, "node": node,
				"positions": [Vector2(float(points[0].x), float(points[0].y)), Vector2(float(points[1].x), float(points[1].y))],
				"z": node.position.z, "pos_index": 0, "state": 0, "reverse": false, "springing_back": false,
				"elapsed": 0.0, "move_percent": 0.0, "move_dur": float(data.get("moveDur", 0.5)),
				"pause_dur": float(data.get("springPauseDur", 0.25)), "windback_dur": float(data.get("springWindBackDur", 2.0)),
				"pre_dur": float(data.get("preMoveDur", 0.35)), "is_spring": int(data.get("isSpring", 0)) != 0, "initial": node.position}
		elif cls == "Switch":
			var data: Dictionary = record.get("data", {})
			var targets: Array[int] = []
			for ref in data.get("movingObstacles", []):
				var target := int(ref.get("path_id", 0))
				if target != 0: targets.append(target)
			if nodes.has(game_object):
				switches[component_id] = {"component_id": component_id, "game_object": game_object, "node": nodes[game_object], "targets": targets,
					"cooldown": 0.0, "active": int(data.get("isActive", 1)) != 0, "cooldown_dur": float(data.get("cooldownDur", 0.5)), "triggered": false}
		elif cls == "WobblyObstacle":
			var refs: Dictionary = record.get("resolved_refs", {})
			if not nodes.has(game_object) or not nodes.has(int(refs.get("root", 0))) or not nodes.has(int(refs.get("spring", 0))): continue
			var data: Dictionary = record.get("data", {})
			wobbles[component_id] = {"component_id": component_id, "game_object": game_object, "node": nodes[game_object],
				"root": nodes[int(refs.root)], "spring": nodes[int(refs.spring)], "target": nodes.get(int(refs.get("spring_target", 0))),
				"offset": Vector2.ZERO, "velocity": Vector2.ZERO, "root_rotation": nodes[int(refs.root)].rotation.z,
				"drag": float(data.get("drag", 20.0)), "spring_force": float(data.get("springForce", 800.0)),
				"max_distance": float(data.get("maxSpringDistance", 5.0)), "input_modifier": float(data.get("inputForceModifier", 1.0)),
				"initial_root_rotation": nodes[int(refs.root)].rotation.z}
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
				"destroyed": false, "initial_visible": nodes[game_object].visible}
	reset()
	initialized = true
	print("BF_LEVEL3_BEHAVIORS " + JSON.stringify({"moving_obstacles": obstacles.size(), "switches": switches.size(), "wobbly_obstacles": wobbles.size(), "destructable_obstacles": destructables.size(), "source_counts": source_counts}))

func reset() -> void:
	accumulator = 0.0; events.clear(); interaction_cooldowns.clear(); debris_states.clear()
	for obstacle in obstacles.values():
		var node: Node3D = obstacle.node
		obstacle.pos_index = 0; obstacle.state = 0; obstacle.reverse = false; obstacle.springing_back = false; obstacle.elapsed = 0.0; obstacle.move_percent = 0.0
		node.position = Vector3(obstacle.positions[0].x, obstacle.positions[0].y, obstacle.z)
		collision_world.sync_scene_object(obstacle.game_object)
	for item in switches.values(): item.cooldown = 0.0; item.triggered = false
	for wobble in wobbles.values():
		wobble.offset = Vector2.ZERO; wobble.velocity = Vector2.ZERO
		wobble.root.rotation.z = float(wobble.initial_root_rotation)
	for item in destructables.values():
		item.destroyed = false; item.hits_left = maxi(1, int(item.hits_left))
		item.node.visible = bool(item.initial_visible)
		collision_world.set_scene_object_enabled(item.game_object, bool(item.initial_visible))
		for child in item.debris:
			child.visible = false

func toggle(component_id: int, reason := "script") -> bool:
	if not obstacles.has(component_id): return false
	var o: Dictionary = obstacles[component_id]
	if o.state == SPRING_PAUSING:
		o.state = MOVING; o.elapsed = 0.0; o.springing_back = false
	else:
		var current := int(o.pos_index); var next := 1 - current
		if (o.state == MOVING or o.state == PRE_MOVING) and not o.reverse:
			next = current - 1
			if next < 0: next = 1
			o.reverse = true
		else: o.reverse = false
		if o.state == MOVING or o.state == PRE_MOVING: o.move_percent = 1.0 - o.move_percent
		else: o.move_percent = 0.0
		o.pos_index = next; o.state = SPRING_PAUSING if o.springing_back else PRE_MOVING; o.elapsed = 0.0
	o.springing_back = false if reason != "spring_back" else true
	if reason == "spring_back": o.state = SPRING_PAUSING
	events.append({"type": "moving_obstacle_toggle", "component_id": component_id, "reason": reason, "target_index": int(o.pos_index)})
	return true

func _spring_back(o: Dictionary) -> void:
	o.springing_back = true; o.reverse = false; o.pos_index = 0; o.move_percent = 0.0; o.elapsed = 0.0; o.state = SPRING_PAUSING
	events.append({"type": "moving_obstacle_spring_back", "component_id": int(o.component_id)})

func _step_obstacle(o: Dictionary, delta: float) -> void:
	if o.state == 0: return
	o.elapsed = float(o.elapsed) + delta
	if o.state == PRE_MOVING:
		if o.elapsed < o.pre_dur - 0.000001: return
		o.elapsed = 0.0; o.state = MOVING
	elif o.state == SPRING_PAUSING:
		if o.elapsed < o.pause_dur - 0.000001: return
		o.elapsed = 0.0; o.state = MOVING
	if o.state != MOVING: return
	var duration: float = float(o.windback_dur if o.springing_back else o.move_dur)
	o.move_percent = minf(1.0, float(o.move_percent) + delta / maxf(duration, 0.000001))
	var t := clampf(float(o.move_percent), 0.0, 1.0)
	var eased := 1.0 - cos(PI * 0.5 * t) if o.reverse else sin(PI * 0.5 * t)
	var from: Vector2; var target: Vector2
	if o.springing_back: from = o.positions[1]; target = o.positions[0]
	else: target = o.positions[int(o.pos_index)]; from = o.positions[1 - int(o.pos_index)]
	o.node.position = Vector3(lerp(from.x, target.x, eased), lerp(from.y, target.y, eased), o.z)
	collision_world.sync_scene_object(o.game_object)
	if t >= 1.0:
		o.node.position = Vector3(target.x, target.y, o.z); o.state = 0; o.elapsed = 0.0
		if o.is_spring and not o.springing_back and int(o.pos_index) == 1: _spring_back(o)
		else: o.reverse = false; o.springing_back = false
		collision_world.sync_scene_object(o.game_object)

func _switch_activate(item: Dictionary, reason := "contact") -> void:
	if not item.active or item.cooldown > 0.0: return
	for target in item.targets: toggle(int(target), reason)
	item.cooldown = item.cooldown_dur; item.triggered = true
	events.append({"type": "switch_activate", "component_id": int(item.component_id), "game_object": int(item.game_object), "reason": reason})

func _wobble_apply(w: Dictionary, force: Vector2) -> void:
	if force.length_squared() <= 1.0: return
	w.velocity += force * float(w.input_modifier)
	events.append({"type": "wobbly_obstacle_hit", "component_id": int(w.component_id), "force": [force.x, force.y]})

func _step_wobble(w: Dictionary, delta: float) -> void:
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
	if item.destroyed: return
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
	for index in actors.size():
		var actor = actors[index]
		if actor == null or actor.state == OriginalActorMotion.State.DEAD or not actor.is_attacking: continue
		var polygon: PackedVector2Array = combat_world.melee_polygon(index)
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

func step(delta: float, actors: Array = [], flying_discs: Array = []) -> void:
	if not initialized: return
	accumulator += minf(delta, 0.25)
	for key in interaction_cooldowns.keys(): interaction_cooldowns[key] = maxf(0.0, float(interaction_cooldowns[key]) - delta)
	while accumulator >= FIXED_DELTA:
		accumulator -= FIXED_DELTA
		for item in switches.values(): item.cooldown = maxf(0.0, float(item.cooldown) - FIXED_DELTA)
		for obstacle in obstacles.values(): _step_obstacle(obstacle, FIXED_DELTA)
		for wobble in wobbles.values(): _step_wobble(wobble, FIXED_DELTA)
		_step_debris(FIXED_DELTA)
		for item in switches.values():
			if not item.active or item.cooldown > 0.0: continue
			var center: Vector2 = Vector2(item.node.global_position.x, item.node.global_position.y)
			for actor in actors:
				if actor != null and actor.state != OriginalActorMotion.State.DEAD and actor.position.distance_to(center) <= 1.35:
					_switch_activate(item, "actor"); break
			if item.cooldown > 0.0: continue
			for disc_item in flying_discs:
				var motion = disc_item.get("motion")
				if motion != null and motion.active and motion.position.distance_to(center) <= 1.0:
					_switch_activate(item, "projectile"); break
	for disc_item in flying_discs: _interact_disc(disc_item.get("motion"))

func drain_events() -> Array[Dictionary]:
	var result := events.duplicate(); events.clear(); return result
