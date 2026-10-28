extends SceneTree
## Use real hierarchy, collider transforms and main-loop wiring; never a flat mock scene.
var checks: Array = []
func check(label: String, passed: bool, details = null) -> void:
	checks.append({"name": label, "passed": passed, "details": details})
func _initialize() -> void: call_deferred("run")
func run() -> void:
	var game = load("res://main.tscn").instantiate()
	root.add_child(game); game.set_process(false)
	var adapter = game.level_behaviors
	var map_name := str(game.active_scene.file).get_file().get_basename()
	check("loaded requested map with decoded behaviors", adapter.initialized and not adapter.source_counts.is_empty(), map_name)
	var selected: Dictionary = {}
	for item in adapter.switches.values():
		if not item.enabled or item.targets.is_empty(): continue
		if item.targets.all(func(id): return adapter.obstacles.has(id) and adapter.obstacles[id].enabled):
			selected = item; break
	if not selected.is_empty():
		check("switch resolves serialized component references", not selected.targets.is_empty(), selected.targets)
		var item: Dictionary = adapter.obstacles[selected.targets[0]]
		var original_rotation: float = item.node.rotation.z
		var original_direction: bool = item.clockwise
		var before_vertices: Dictionary = {}
		var owner: Node3D = item.node
		for shape in game.collision_world.shapes:
			if shape.actor: continue
			var node: Node3D = game.nodes[int(shape.object_id)]
			if (node == owner or owner.is_ancestor_of(node)) and shape.kind == "polygon": before_vertices[shape.id] = shape.vertices.duplicate()
		# Real attack polygon enters the switch's serialized trigger from its front.
		var up3: Vector3 = selected.node.global_basis.y.normalized()
		var up := Vector2(up3.x, up3.y)
		var center := Vector2(selected.node.global_position.x, selected.node.global_position.y)
		var actor: OriginalActorMotion = game.actors[0]
		actor.position = center + up * 2.5
		actor.rotation_z = atan2(up.x, -up.y)
		var before_index: int = item.pos_index
		for i in 45: adapter.step(0.016666, game.actors, [])
		check("walking near switch does not activate an attack trigger", not selected.triggered)
		actor.is_attacking = true
		adapter.interact_melee(game.actors, game.combat)
		check("actual melee polygon activates switch", selected.triggered)
		check("activation reaches the linked mover", item.clockwise != original_direction if item.rotating else item.pos_index != before_index)
		var after_index: int = item.pos_index
		var after_direction: bool = item.clockwise
		adapter.drain_events()
		for i in 60:
			adapter.frame_step(0.016666)
			adapter.interact_melee(game.actors, game.combat)
			adapter.fixed_step(OriginalTimekeeper.f32(0.016666))
		var emitted: Array = adapter.drain_events()
		check("continuous overlap does not retrigger after cooldown", emitted.filter(func(e): return e.type == "switch_activate").is_empty())
		var changed := false
		for shape in game.collision_world.shapes:
			if before_vertices.has(shape.id) and shape.vertices != before_vertices[shape.id]: changed = true
		check("moving/rotating geometry updates actual collision vertices", changed, before_vertices.size())
		actor.is_attacking = false; adapter.interact_melee(game.actors, game.combat)
		after_index = item.pos_index; after_direction = item.clockwise
		var before_state: int = item.state
		actor.is_disarming = true; adapter.interact_melee(game.actors, game.combat)
		var toggled: bool = item.clockwise != after_direction if item.rotating else (item.state == 1 and before_state == 3) or item.pos_index != after_index
		check("disarm re-entry activates after melee exits", toggled and adapter.drain_events().any(func(e): return e.type == "switch_activate" and e.reason == "disarm"))
		actor.is_disarming = false; adapter.interact_melee(game.actors, game.combat)
		adapter.reset(false)
		check("reset restores rotation, direction and trigger state", abs(item.node.rotation.z - original_rotation) < 0.00001 and item.clockwise == original_direction and not selected.triggered)
		# Projectile OnTriggerEnter direction uses stored velocity, not a later ricochet velocity.
		var disc := OriginalDiscMotion.new()
		disc.position = center + up
		disc.stored = up * 10.0; disc.velocity = -disc.stored
		adapter._switch_disc_contacts([{"motion":disc}])
		check("projectile approaching from rejected direction stays inactive", not selected.triggered)
		adapter._switch_disc_contacts([])
		disc.stored = -up * 10.0
		adapter._switch_disc_contacts([{"motion":disc}])
		check("projectile re-entry from accepted direction triggers", selected.triggered)
		adapter.reset(false)
		disc.stored = up * 10.0
		disc.owner_actor = actor; actor.position = disc.position
		disc.source_z = actor.source_z + 10.0
		adapter._switch_disc_contacts([{"motion":disc}])
		check("fresh throw exception includes original Z distance", not selected.triggered)
		adapter._switch_disc_contacts([]); disc.source_z = actor.source_z
		adapter._switch_disc_contacts([{"motion":disc}])
		check("near-owner fresh throw bypasses direction restriction", selected.triggered)
		adapter.reset(false)
		selected.one_shot = true
		adapter._switch_activate(selected, "test")
		check("one-shot switch disables and resets", not selected.active)
		adapter.reset(false); check("reset restores one-shot activation state", selected.active)
		selected.one_shot = false
		for other in adapter.obstacles.values():
			if other.enabled: continue
			var old_transform: Transform3D = other.node.transform
			for i in 4: adapter.fixed_step(0.016666)
			check("inactive original obstacle remains static", other.node.transform == old_transform)
			break
		# Standalone wrapper and actual game share the exact serialized fixed interval.
		game.reset_motions()
		var ticks: int = adapter.fixed_ticks
		var sim_ticks: int = game.simulated_ticks
		game.update_movement(0.053)
		check("main-loop obstacle and actor physics steps stay aligned", adapter.fixed_ticks - ticks == game.simulated_ticks - sim_ticks and adapter.fixed_ticks > ticks, {"map_ticks":adapter.fixed_ticks-ticks,"actor_ticks":game.simulated_ticks-sim_ticks})
		var paused_ticks: int = adapter.fixed_ticks
		game.update_movement(0.0)
		check("zero-time frame does not tick map behaviors", adapter.fixed_ticks == paused_ticks)
	elif map_name == "level55":
		var changed_child := false
		var before: Dictionary = {}
		for shape in game.collision_world.shapes:
			if not shape.actor and shape.kind == "polygon": before[shape.id] = shape.vertices.duplicate()
		for i in 20: adapter.fixed_step(0.016666)
		for shape in game.collision_world.shapes:
			if before.has(shape.id) and before[shape.id] != shape.vertices: changed_child = true
		check("rotating parent updates child colliders", changed_child)
	else:
		check("map has a switch linked to implemented movers", false)
	var report := {"map":map_name,"passed":checks.all(func(c): return c.passed),"checks":checks,"scope":"Real-scene switch trigger, mover transforms/colliders, reset, and shared physics scheduling; no Unity frame-parity claim."}
	FileAccess.open("res://../evidence/godot-map-behavior-" + map_name + "-check.json", FileAccess.WRITE).store_string(JSON.stringify(report,"  "))
	print("BF_MAP_BEHAVIOR_TEST " + JSON.stringify(report))
	game.queue_free(); await process_frame
	quit(0 if report.passed else 1)
