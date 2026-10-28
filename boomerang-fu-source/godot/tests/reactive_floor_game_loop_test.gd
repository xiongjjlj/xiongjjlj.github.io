extends SceneTree
var checks: Array = []
func check(label: String, passed: bool, details = null) -> void: checks.append({"name":label,"passed":passed,"details":details})
func _initialize() -> void: call_deferred("run")
func run() -> void:
	var game = load("res://main.tscn").instantiate(); root.add_child(game); game.set_process(false)
	var names: Array[String] = ["Avocado","Watermelon"]; var devices: Array[int] = [-1,-2]
	game.configure_match(names,devices,FfaSetup.Length.MEDIUM)
	var floors: OriginalMovingFloors = game.level_behaviors.moving_floors
	var floor: Dictionary = floors.floors[2678]
	var local := Vector3(-8,0,0)
	var start: Vector3 = floor.node.global_transform * local
	game.actors[0].position = Vector2(start.x,start.y)
	game.actors[1].position = Vector2(18.56155,-21.56155)
	var maximum_error := 0.0; var remained_alive := true
	for tick in 1250:
		if tick == 60 or tick == 620: floors.toggle_reactive(floor)
		game._process(0.016666)
		var expected: Vector3 = floor.node.global_transform * local
		maximum_error = maxf(maximum_error,game.actors[0].position.distance_to(Vector2(expected.x,expected.y)))
		remained_alive = remained_alive and game.actors[0].state == OriginalActorMotion.State.ALIVE
	check("central rider survives full outward and return journey",remained_alive)
	check("full game loop preserves platform-local rider position",maximum_error < 0.002,maximum_error)
	check("platform finishes its return and retains rider",floor.node.transform.is_equal_approx(floor.initial) and int(floor.state)==0 and game.actor_environments[0].parent_kind==1)
	game.reset_motions(true)
	start = floor.node.global_transform * Vector3(-8,12,0)
	game.actors[0].position = Vector2(start.x,start.y)
	game.actors[1].position = Vector2(18.56155,-21.56155)
	var roller_contact_tick := -1; var fall_tick := -1
	for tick in 650:
		if tick == 60: floors.toggle_reactive(floor)
		game._process(0.016666)
		var actor: OriginalActorMotion = game.actors[0]
		for hit in game.collision_world.overlaps(actor.position,actor.radius+0.2,256):
			if int(hit.shape.object_id)==33 and roller_contact_tick<0: roller_contact_tick=tick
		if actor.is_falling: fall_tick=tick; break
	check("edge rider contacts original fixed roller before losing ground",roller_contact_tick>60 and fall_tick>roller_contact_tick,{"roller_contact_tick":roller_contact_tick,"fall_tick":fall_tick})
	check("edge fall is ground loss, not broken platform attachment",game.actor_environments[0].parent_kind==1 and game.actor_environments[0].over_hazard(game.actors[0].position))
	var report := {"passed":checks.all(func(c):return c.passed),"checks":checks,"scope":"Full Godot game loop on actual Big Raft: central rider round trip and fixed-roller obstruction. Original full-scene callback ordering is not established by this test."}
	FileAccess.open("res://../evidence/godot-reactive-floor-game-loop-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  "))
	print("BF_REACTIVE_FLOOR_GAME_LOOP "+JSON.stringify(report))
	game.queue_free(); await process_frame; quit(0 if report.passed else 1)
