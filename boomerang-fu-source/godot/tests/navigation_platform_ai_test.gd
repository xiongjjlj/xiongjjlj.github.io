extends SceneTree
var checks: Array = []
func check(label: String, passed: bool, details = null) -> void: checks.append({"name":label,"passed":passed,"details":details})
func _initialize() -> void: call_deferred("run")
func run() -> void:
	var game = load("res://main.tscn").instantiate();root.add_child(game);game.set_process(false)
	var names: Array[String]=["Avocado","Watermelon"];var devices: Array[int]=[-1,-101]
	game.configure_match(names,devices,FfaSetup.Length.MEDIUM)
	var key := str(game.active_scene.file).get_file().get_basename()
	var nav: OriginalNavigationWorld = game.decoys.navigation
	var ai: OriginalPlayerAI=game.bots.members[1]
	var fixture: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/navigation-selection-native.json"))
	var chosen: Dictionary={}
	for map: Dictionary in fixture.maps:
		if str(map.key)!=key:continue
		for sample: Dictionary in map.phases[0].queries:
			if int(sample.mask)<=1:continue
			var start := Vector3(sample.point[0],sample.point[1],sample.point[2]);var finish := Vector3(sample.target[0],sample.target[1],sample.target[2])
			if start.distance_to(finish)<3.0:continue
			var result := nav.actor_path(start,finish)
			if result.get("error","")!="" or result.get("moving_floor",{}).is_empty() or result.smoothed.size()<2:continue
			chosen={"start":start,"finish":finish};break
	check("real map has a traversable platform path",not chosen.is_empty())
	if not chosen.is_empty():
		ai.actor.position=Vector2(chosen.start.x,chosen.start.y);ai.target=chosen.finish;ai.playing=true;ai.state=OriginalDecoyAI.AIState.WANDERING;ai.create_timer=1.0
		ai.create_path();ai.deliver_path_result()
		check("live bot binds returned path to its platform",ai.has_path and not ai.path_floor.is_empty())
		if ai.has_path and not ai.path_floor.is_empty():
			var floor: Dictionary=ai.path_floor;var last := ai.path.size()-1
			var before := ai.waypoint(last);var local: Vector3=floor.node.global_transform.affine_inverse()*Vector3(before.x,before.y,-before.z)
			var raw: Array[Vector3]=ai.path.duplicate();var maximum_error := 0.0
			var floors: OriginalMovingFloors=game.level_behaviors.moving_floors
			if floor.reactive:floors.toggle_reactive(floor)
			for tick in 180:
				floors.now=OriginalTimekeeper.f32(floors.now+0.016666);floors.fixed_step(0.016666)
				var expected: Vector3=floor.node.global_transform*local;expected.z=-expected.z
				maximum_error=maxf(maximum_error,ai.waypoint(last).distance_to(expected))
				if tick==90:
					var position:=ai.actor.position;ai.actor.position=Vector2(10000,10000);ai.create_timer=1.0;ai.create_path()
					check("failed replan reports unreachable start",ai.pending_result.get("error","")!="")
					ai.deliver_path_result();ai.actor.position=position
			check("retained waypoint follows platform without recalculating",maximum_error<0.0001 and ai.waypoint(last).distance_to(before)>0.5,{"maximum_error":maximum_error,"travel":ai.waypoint(last).distance_to(before)})
			check("failed replan retains source coordinates and platform binding",ai.path==raw and ai.path_floor==floor)
			floors.reset(false)
			check("round reset restores retained waypoint pose",ai.waypoint(last).distance_to(before)<0.0001)
	var report := {"passed":checks.all(func(c):return c.passed),"map":key,"checks":checks,"scope":"Actual bot path provider and completion callback, moving platform waypoint retention, failed replan, round reset; not a full native match comparison."}
	FileAccess.open("res://../evidence/godot-navigation-platform-ai-"+key+"-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  "))
	print("BF_NAVIGATION_PLATFORM_AI "+JSON.stringify(report));game.queue_free();await process_frame;quit(0 if report.passed else 1)
