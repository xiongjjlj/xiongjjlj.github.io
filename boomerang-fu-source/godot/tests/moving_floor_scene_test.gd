extends SceneTree
var checks: Array = []
func check(label: String, passed: bool, details = null) -> void: checks.append({"name":label,"passed":passed,"details":details})
func _initialize() -> void: call_deferred("run")
func run() -> void:
	var game = load("res://main.tscn").instantiate(); root.add_child(game); game.set_process(false)
	var floors: OriginalMovingFloors = game.level_behaviors.moving_floors
	check("all normal moving floors bound",floors.floors.size() == int(game.level_behaviors.source_counts.get("MovingFloor",0)) and not floors.floors.is_empty())
	var f: Dictionary = floors.floors.values()[0]
	var shapes: Array = game.collision_world.shapes.filter(func(s):return not s.actor and int(s.object_id)==int(f.game_object))
	check("real floor colliders reference runtime floor",not shapes.is_empty() and shapes.all(func(s):return s.get("moving_floor",{}).get("component_id",-1)==f.component_id))
	var point := Vector2.ZERO; var found := false
	for shape in shapes:
		for x in range(1,10):
			for y in range(1,10):
				var p: Vector2 = shape.bounds.position + shape.bounds.size*Vector2(float(x)/10.0,float(y)/10.0)
				if OriginalMovingFloors.contains_point(shape,p) and not game.collision_world.overlap_circle(p,1.2,256): point=p;found=true;break
			if found: break
		if found: break
	check("found actual walkable platform point",found,point)
	var actor := OriginalActorMotion.new(); actor.position=point; actor.physics_enabled=true
	var env := OriginalActorEnvironment.new(); env.initialize(actor,game.ground_queries)
	floors.update_actor(env)
	check("trigger attaches actor to original collider",env.parent_kind==1 and env.parent_floor.get("component_id",-1)==f.component_id)
	var local: Vector3 = f.node.global_transform.affine_inverse()*Vector3(actor.position.x,actor.position.y,actor.source_z)
	var disc := OriginalDiscMotion.new(); disc.position=point; disc.movement=OriginalDiscMotion.Movement.DROPPED; disc.active=true
	floors.update_discs([{"motion":disc}])
	check("dropped disc attaches",floors.disc_parents.has(disc.get_instance_id()),floors.disc_parents.get(disc.get_instance_id(),{}).get("component_id",-1))
	var before: Transform3D = f.node.global_transform
	var before_vertices: Array = shapes.map(func(s): return s.vertices.duplicate() if s.kind=="polygon" else s.center)
	var maximum_error := 0.0
	for tick in 180:
		floors.fixed_step(OriginalTimekeeper.f32(0.016666)); floors.update_actor(env); floors.update_discs([{"motion":disc}])
		var expected: Vector3 = f.node.global_transform*local
		maximum_error=maxf(maximum_error,actor.position.distance_to(Vector2(expected.x,expected.y)))
	check("platform follows original trajectory",before!=f.node.global_transform)
	check("actor remains at its local platform position",maximum_error<0.002,maximum_error)
	check("dropped disc follows same platform",disc.position.distance_to(actor.position)<0.002,{"error":disc.position.distance_to(actor.position),"actor_z":actor.source_z,"disc_z":disc.source_z,"floor":floors.disc_parents.get(disc.get_instance_id(),{}).get("component_id",-1),"actor":actor.position,"disc":disc.position})
	var updated := false
	for i in shapes.size():
		if (shapes[i].vertices if shapes[i].kind=="polygon" else shapes[i].center)!=before_vertices[i]:updated=true
	# Rotation-invariant circle colliders may retain their center while children move.
	check("collision shapes remain bound to moving scene", updated or f.use_rotations)
	actor.position=Vector2(10000,10000); floors.update_actor(env)
	check("trigger exit detaches actor",env.parent_kind==0 and env.parent_floor.is_empty())
	disc.movement=OriginalDiscMotion.Movement.RETURNING; floors.update_discs([{"motion":disc}])
	check("recalled disc detaches",not floors.disc_parents.has(disc.get_instance_id()))
	floors.reset(false); var reset_pose: Transform3D=f.node.global_transform
	floors.fixed_step(0.016666)
	check("reset restores pose and awaits StartMoving",reset_pose==f.node.global_transform and f.node.transform==f.initial and not f.moving)
	floors.start_all(); floors.fixed_step(0.016666)
	check("round start enables floor movement",f.moving)
	var report := {"passed":checks.all(func(c):return c.passed),"map":game.active_scene.name,"checks":checks,"scope":"Real scene collider binding, platform carry and round reset. Native trigger callback order remains unverified."}
	FileAccess.open("res://../evidence/godot-moving-floor-scene-"+str(game.active_scene_index)+"-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  "))
	print("BF_MOVING_FLOOR_SCENE_TEST "+JSON.stringify(report))
	game.queue_free(); await process_frame; quit(0 if report.passed else 1)
