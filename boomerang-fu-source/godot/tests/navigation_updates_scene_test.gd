extends SceneTree
var checks:Array=[]
func check(name:String,passed:bool,details=null)->void:checks.append({"name":name,"passed":passed,"details":details})
func _initialize()->void:call_deferred("run")
func run()->void:
	var game=load("res://main.tscn").instantiate();root.add_child(game);game.set_process(false)
	var names:Array[String]=["Avocado","Watermelon"];var devices:Array[int]=[-1,-101]
	game.configure_match(names,devices,FfaSetup.Length.MEDIUM)
	var nav:OriginalNavigationWorld=game.decoys.navigation
	var key:=str(game.active_scene.file).get_file().get_basename()
	var grid:OriginalNavigation=nav.grids[0]
	var original:=grid.walkable.duplicate();var original_connections:=grid.connections.duplicate()
	var level:OriginalLevel3Behaviors=game.level_behaviors
	var targets:=0
	for o in level.obstacles.values():
		if o.enabled:
			targets+=1
			if not o.rotating:level.toggle(int(o.component_id))
	for f in level.moving_floors.floors.values():
		if f.enabled and f.reactive and f.use_rotations:targets+=1;level.moving_floors.toggle_reactive(f)
	var differences:=0;var maximum_queue:=0
	for tick in 180:
		level.moving_floors.now=OriginalTimekeeper.f32((tick+1)*0.016666)
		level.fixed_step(0.016666)
		level.frame_step(0.016666)
		maximum_queue=maxi(maximum_queue,nav.pending_updates.size())
		nav.flush_updates()
		for i in grid.walkable.size():
			if grid.walkable[i]!=original[i]:differences+=1
	check("live mechanisms update actual map navigation",targets>0 and nav.updates_applied>0 and differences>0,{"targets":targets,"updates":nav.updates_applied,"changed_node_samples":differences,"maximum_queue":maximum_queue,"rescan_milliseconds":nav.update_microseconds/1000.0})
	check("navigation queue drains each frame",nav.pending_updates.is_empty() and maximum_queue<512)
	var destroyed:=0
	for item in level.destructables.values():
		if item.enabled and item.rescan and not item.destroyed:
			var before:=nav.updates_applied;level._destroy(item,Vector2.UP);nav.flush_updates()
			check("destroyed obstacle queues navigation rescan",item.destroyed and nav.updates_applied>before)
			destroyed+=1;break
	nav.reset_grids();level.reset(false)
	check("round reset restores every serialized static node and connection",grid.walkable==nav.initial_grids[0].walkable and grid.connections==nav.initial_grids[0].connections and nav.pending_updates.is_empty())
	# A real retained ABPath must be recreated if any node after its start is
	# blocked; failure to calculate a replacement retains the existing path.
	var ai:OriginalPlayerAI=game.bots.members[1]
	var chosen:Dictionary={}
	for i in grid.walkable.size():
		if not grid.walkable[i]:continue
		var source:Vector3=grid.positions[i]
		var path:=grid.find_path(source,source+Vector3(0,8,0))
		if path.error=="" and path.node_ids.size()>3:chosen={"start":source,"end":source+Vector3(0,8,0),"path":path};break
	check("map provides an initial path for invalidation",not chosen.is_empty())
	if not chosen.is_empty():
		ai.actor.position=Vector2(chosen.start.x,chosen.start.y);ai.actor.source_z=chosen.start.z;ai.target=chosen.end
		ai.state=OriginalDecoyAI.AIState.WANDERING;ai.playing=true;ai.create_timer=1.0
		ai.pending_result=chosen.path;ai.recalculating=true;ai.deliver_path_result()
		var old_nodes:=ai.path_nodes.duplicate();grid.walkable[old_nodes[1]]=0
		nav.request_recreate();nav.recreate_invalid_paths([ai])
		check("invalidated retained path requests replacement",ai.recalculating)
		ai.deliver_path_result()
		nav.reset_grids()
	var report:={"passed":checks.all(func(c):return c.passed),"map":key,"checks":checks,"destroyed_obstacles":destroyed,"scope":"Real map mechanism callbacks, queued graph rescans, retained-path invalidation and round reset. No native callback ordering or visual parity claim."}
	FileAccess.open("res://../evidence/godot-navigation-updates-scene-"+key+"-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  "))
	print("BF_NAVIGATION_UPDATES_SCENE "+JSON.stringify(report));game.queue_free();await process_frame;quit(0 if report.passed else 1)
