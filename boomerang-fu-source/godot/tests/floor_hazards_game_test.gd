extends SceneTree
func _initialize()->void:call_deferred("run")
func run()->void:
	var game=load("res://main.tscn").instantiate();root.add_child(game);game.set_process(false)
	var key:=str(game.active_scene.file).get_file().get_basename()
	var names:Array[String]=["Avocado","Watermelon"];var devices:Array[int]=[-100,-101]
	if key=="level40":devices[0]=-1
	game.configure_match(names,devices,FfaSetup.Length.MEDIUM)
	var floors:OriginalFloorHazards=game.floor_hazards
	if key=="level40":
		# A human stands on a trap while the opponent continues to navigate.
		# Bots may correctly avoid every trap, so their routes alone cannot
		# guarantee that this regression exercises the collapsing geometry.
		for item in floors.floors.values():
			var p:Vector3=item.node.global_position
			var point:=Vector2(p.x,p.y)
			if item.model.trap and not game.ground_queries.solid_ground(point,game.actors[0].radius*0.95):
				game.actors[0].position=point
				break
	var active_frames:=0;var finite:=true;var moved:=0.0;var resets:=0;var holes:=0
	var before:Vector2=game.actors[1].position
	for frame in 900:
		game._process(1.0/60.0)
		moved+=game.actors[1].position.distance_to(before);before=game.actors[1].position
		for actor:OriginalActorMotion in game.actors:finite=finite and actor.position.is_finite()
		for item in floors.floors.values():
			if item.model.state!=0:active_frames+=1
			if item.model.layer==0:holes+=1
		if game.round_state.phase in [OriginalFfaRound.Phase.SCORES,OriginalFfaRound.Phase.MATCH_END]:
			game.next_round();before=game.actors[1].position;resets+=1
		await process_frame
	var disc_views:=0
	for child in game.get_children():
		if child is OriginalDiscPreview:disc_views+=1
	var report:={"passed":finite and moved>1 and active_frames>0 and holes>0 and game.decoys.navigation.pending_updates.is_empty() and disc_views==game.flying_discs.size(),"map":key,"frames":900,"active_tile_frames":active_frames,"hole_frames":holes,"distance":moved,"round_resets":resets,"finite":finite,"disc_views":disc_views,"active_discs":game.flying_discs.size()}
	FileAccess.open("res://../evidence/godot-floor-hazards-game-"+key+"-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  "))
	print("BF_FLOOR_HAZARDS_GAME "+JSON.stringify(report))
	game.queue_free();await process_frame;quit(0 if report.passed else 1)
