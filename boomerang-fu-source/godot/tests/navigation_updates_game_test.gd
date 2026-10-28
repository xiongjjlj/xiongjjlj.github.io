extends SceneTree
func _initialize()->void:call_deferred("run")
func run()->void:
	var game=load("res://main.tscn").instantiate();root.add_child(game);game.set_process(false)
	var names:Array[String]=["Avocado","Watermelon"];var devices:Array[int]=[-100,-101]
	game.configure_match(names,devices,FfaSetup.Length.MEDIUM)
	var nav:OriginalNavigationWorld=game.decoys.navigation
	var floor_id:=2764
	game.level_behaviors.moving_floors.toggle_reactive(game.level_behaviors.moving_floors.floors[floor_id])
	var total_updates:=0;var last_updates:=0;var finite:=true;var moved:=0.0;var resets:=0
	var before:Vector2=game.actors[1].position
	for frame in 360:
		game._process(1.0/60.0)
		total_updates+=nav.updates_applied-last_updates;last_updates=nav.updates_applied
		moved+=game.actors[1].position.distance_to(before);before=game.actors[1].position
		for actor:OriginalActorMotion in game.actors:finite=finite and actor.position.is_finite()
		if game.round_state.phase in [OriginalFfaRound.Phase.SCORES,OriginalFfaRound.Phase.MATCH_END]:
			game.next_round();last_updates=nav.updates_applied;before=game.actors[1].position;resets+=1
			game.level_behaviors.moving_floors.toggle_reactive(game.level_behaviors.moving_floors.floors[floor_id])
	var report:={"passed":finite and total_updates>0 and moved>1.0 and nav.pending_updates.is_empty(),"frames":360,"graph_updates":total_updates,"bot_distance":moved,"round_resets":resets,"finite_positions":finite,"pending_updates":nav.pending_updates.size(),"scope":"Full Godot game loop with two bots and a rotating reactive maze; native whole-frame parity remains unproven."}
	FileAccess.open("res://../evidence/godot-navigation-updates-game-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  "))
	print("BF_NAVIGATION_UPDATES_GAME "+JSON.stringify(report));game.queue_free();await process_frame;quit(0 if report.passed else 1)
