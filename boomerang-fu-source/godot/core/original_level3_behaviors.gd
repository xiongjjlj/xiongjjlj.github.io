class_name OriginalLevel3Behaviors
extends RefCounted
## Unity level3 gameplay behaviours recovered from serialized MonoBehaviours.
## MovingObstacle follows the source FixedUpdate/Toggler state machine. The
## remaining behaviours stay in the sidecar for subsequent ports.
const FIXED_DELTA := 1.0 / 60.0
const MOVING := 1
const PRE_MOVING := 2
const SPRING_PAUSING := 3
var nodes: Dictionary = {}
var collision_world: OriginalCollisionWorld
var obstacles: Dictionary = {}
var switches: Dictionary = {}
var source_counts: Dictionary = {}
var accumulator := 0.0
var events: Array[Dictionary] = []
var initialized := false

func initialize(sidecar: Dictionary, scene_nodes: Dictionary, world: OriginalCollisionWorld) -> void:
	nodes = scene_nodes
	collision_world = world
	obstacles.clear(); switches.clear(); source_counts = sidecar.get("class_counts", {})
	initialized = false
	for record in sidecar.get("behaviours", []):
		var cls := str(record.get("class", ""))
		var component_id := int(record.get("component_id", 0))
		if cls == "MovingObstacle":
			var data: Dictionary = record.get("data", {})
			var points: Array = data.get("positions", [])
			if points.size() < 2 or not nodes.has(int(record.get("game_object", 0))): continue
			var node: Node3D = nodes[int(record.game_object)]
			obstacles[component_id] = {
				"component_id": component_id, "game_object": int(record.game_object), "node": node,
				"positions": [Vector2(float(points[0].x), float(points[0].y)), Vector2(float(points[1].x), float(points[1].y))],
				"z": node.position.z, "pos_index": 0, "state": 0, "reverse": false,
				"springing_back": false, "elapsed": 0.0, "move_percent": 0.0,
				"move_dur": float(data.get("moveDur", 0.5)), "pause_dur": float(data.get("springPauseDur", 0.25)),
				"windback_dur": float(data.get("springWindBackDur", 2.0)), "pre_dur": float(data.get("preMoveDur", 0.35)),
				"is_spring": int(data.get("isSpring", 0)) != 0, "initial": node.position
			}
		elif cls == "Switch":
			var data: Dictionary = record.get("data", {})
			var targets: Array[int] = []
			for ref in data.get("movingObstacles", []):
				var target := int(ref.get("path_id", 0))
				if target != 0: targets.append(target)
			if nodes.has(int(record.get("game_object", 0))):
				switches[component_id] = {"component_id": component_id, "game_object": int(record.game_object), "node": nodes[int(record.game_object)], "targets": targets, "cooldown": 0.0, "active": int(data.get("isActive", 1)) != 0, "cooldown_dur": float(data.get("cooldownDur", 0.5)), "triggered": false}
	reset()
	initialized = true
	print("BF_LEVEL3_BEHAVIORS " + JSON.stringify({"moving_obstacles": obstacles.size(), "switches": switches.size(), "source_counts": source_counts}))

func reset() -> void:
	accumulator = 0.0; events.clear()
	for obstacle in obstacles.values():
		var node: Node3D = obstacle.node
		obstacle.pos_index = 0; obstacle.state = 0; obstacle.reverse = false; obstacle.springing_back = false
		obstacle.elapsed = 0.0; obstacle.move_percent = 0.0
		node.position = Vector3(obstacle.positions[0].x, obstacle.positions[0].y, obstacle.z)
		collision_world.sync_scene_object(obstacle.game_object)
	for item in switches.values():
		item.cooldown = 0.0; item.triggered = false

func toggle(component_id: int, reason := "script") -> bool:
	if not obstacles.has(component_id): return false
	var o: Dictionary = obstacles[component_id]
	# Faithful ToggleTargetPosition: a switch can reverse a moving obstacle in place.
	if o.state == SPRING_PAUSING:
		o.state = MOVING; o.elapsed = 0.0; o.springing_back = false
	else:
		var current := int(o.pos_index)
		var next := 1 - current
		if (o.state == MOVING or o.state == PRE_MOVING) and not o.reverse:
			next = current - 1
			if next < 0: next = 1
			o.reverse = true
		else:
			o.reverse = false
		# The source computes the remaining fraction when reversing.
		if o.state == MOVING or o.state == PRE_MOVING: o.move_percent = 1.0 - o.move_percent
		else: o.move_percent = 0.0
		o.pos_index = next; o.state = SPRING_PAUSING if o.springing_back else PRE_MOVING
		o.elapsed = 0.0
	o.springing_back = false if reason != "spring_back" else true
	if reason == "spring_back": o.state = SPRING_PAUSING
	o.has_reached_end_audio = false
	events.append({"type":"moving_obstacle_toggle", "component_id":component_id, "reason":reason, "target_index":int(o.pos_index)})
	return true

func _spring_back(o: Dictionary) -> void:
	# Source invokes ToggleTargetPosition(springBack:true) immediately at the end.
	o.springing_back = true
	o.reverse = false
	o.pos_index = 0
	o.move_percent = 0.0
	o.elapsed = 0.0
	o.state = SPRING_PAUSING
	events.append({"type":"moving_obstacle_spring_back", "component_id":int(o.component_id)})

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
	# Easing.EaseIn/EaseOut in the recovered source are the standard quarter sine forms.
	var eased := 1.0 - cos(PI * 0.5 * t) if o.reverse else sin(PI * 0.5 * t)
	var start: Vector2 = o.positions[1 - int(o.pos_index)] if o.reverse else o.positions[1 - int(o.pos_index)]
	# start/target are captured by posIndex and reverse; for the normal path this is p0 -> p1,
	# and spring-back is p1 -> p0. Keeping these explicit avoids accumulated float drift.
	if o.springing_back:
		start = o.positions[1]; var target_back: Vector2 = o.positions[0]
		(o.node as Node3D).position = Vector3(lerp(start.x, target_back.x, eased), lerp(start.y, target_back.y, eased), o.z)
	else:
		var target: Vector2 = o.positions[int(o.pos_index)]
		var from: Vector2 = o.positions[1 - int(o.pos_index)]
		(o.node as Node3D).position = Vector3(lerp(from.x, target.x, eased), lerp(from.y, target.y, eased), o.z)
	collision_world.sync_scene_object(o.game_object)
	if t >= 1.0:
		o.node.position = Vector3(o.positions[int(o.pos_index)].x, o.positions[int(o.pos_index)].y, o.z)
		o.state = 0; o.elapsed = 0.0
		if o.is_spring and not o.springing_back and int(o.pos_index) == 1:
			_spring_back(o)
		else:
			o.reverse = false; o.springing_back = false
		collision_world.sync_scene_object(o.game_object)

func _switch_activate(item: Dictionary, reason := "contact") -> void:
	if not item.active or item.cooldown > 0.0: return
	for target in item.targets: toggle(int(target), reason)
	item.cooldown = item.cooldown_dur; item.triggered = true
	events.append({"type":"switch_activate", "component_id":int(item.component_id), "game_object":int(item.game_object), "reason":reason})

func step(delta: float, actors: Array = [], flying_discs: Array = []) -> void:
	if not initialized: return
	accumulator += minf(delta, 0.25)
	while accumulator >= FIXED_DELTA:
		accumulator -= FIXED_DELTA
		for item in switches.values(): item.cooldown = maxf(0.0, float(item.cooldown) - FIXED_DELTA)
		for obstacle in obstacles.values(): _step_obstacle(obstacle, FIXED_DELTA)
		# Switch.cs activates from Actor or Projectile trigger enters. Actor proximity
		# and projectile proximity use the serialized trigger locations and preserve cooldown.
		for item in switches.values():
			if not item.active or item.cooldown > 0.0: continue
			var center: Vector2 = Vector2(item.node.global_position.x, item.node.global_position.y)
			for actor in actors:
				if actor == null or actor.state == OriginalActorMotion.State.DEAD: continue
				if actor.position.distance_to(center) <= 1.35:
					_switch_activate(item, "actor"); break
			if item.cooldown > 0.0: continue
			for disc_item in flying_discs:
				var motion = disc_item.get("motion")
				if motion != null and motion.active and motion.position.distance_to(center) <= 1.0:
					_switch_activate(item, "projectile"); break

func drain_events() -> Array[Dictionary]:
	var result := events.duplicate()
	events.clear()
	return result
