extends SceneTree
## Integration checks; these do not establish native full-match parity.
var failures:Array=[]
var cases:Array=[]
func check(condition:bool,message:String)->void:
	if not condition:failures.append(message)
func _initialize()->void:call_deferred("run")
func run()->void:
	var game:Node=load("res://main.tscn").instantiate()
	root.add_child(game);game.set_process(false)
	for difficulty in 4:
		game.setup.bot_difficulty=difficulty
		var names:Array[String]=["Avocado","Watermelon"]
		var devices:Array[int]=[-100,-101]
		game.configure_match(names,devices,0)
		game.decoys.rng=OriginalUnityRandom.new(725+difficulty)
		game.setup.spawn_rng=OriginalUnityRandom.new(450+difficulty)
		game.reset_motions(true);game.movement_enabled=true
		var moved:=0.0;var commands:=0;var kills:=0;var rounds:=0
		var states:Dictionary={};var weak_old:Array=[]
		for tick in 1200:
			var before:Vector2=game.actors[1].position
			game._process(1.0/60.0)
			moved+=game.actors[1].position.distance_to(before)
			for ai:OriginalPlayerAI in game.bots.members.values():states[ai.state]=true
			if game.round_state.phase in [OriginalFfaRound.Phase.SCORES,OriginalFfaRound.Phase.MATCH_END]:
				for ai:OriginalPlayerAI in game.bots.members.values():commands+=ai.action_count;weak_old.append(weakref(ai))
				for actor:OriginalActorMotion in game.actors:
					if actor.state==OriginalActorMotion.State.DEAD:kills+=1
				rounds+=1;game.next_round()
		for ai:OriginalPlayerAI in game.bots.members.values():commands+=ai.action_count
		check(moved>5.0,"difficulty %d: bots did not move"%difficulty)
		check(commands>0,"difficulty %d: bots issued no action"%difficulty)
		check(game.bots.records.size()<300,"target registry grew beyond current map population")
		var retained_ids:Dictionary={}
		for ref:WeakRef in weak_old:
			var old:Variant=ref.get_ref()
			if old!=null:retained_ids[old.get_instance_id()]=true
		# Unity retains the PlayerAI component across a round reset; only its
		# actor/world bindings are refreshed. Disposal is checked below.
		check(retained_ids.size()==game.bots.members.size(),"round reset did not retain exactly one AI component per bot: "+str(retained_ids.size()))
		cases.append({"difficulty":difficulty,"frames":1200,"distance":moved,"commands":commands,"rounds":rounds,"deaths_at_round_end":kills,"states":states.keys(),"retained_old_ai":retained_ids.size(),"records":game.bots.records.size()})
	var human_and_bots:Array[int]=[-1,-101,-102,-103,-104,-105]
	var names:Array[String]=["Avocado","Watermelon","Bread","Banana","Milk","Coffee"]
	game.configure_match(names,human_and_bots,0);game.movement_enabled=true
	check(game.bots.members.size()==5,"mixed six-player lobby should create five bots")
	var event:=InputEventKey.new();event.physical_keycode=KEY_SLASH;event.pressed=true
	game.controllers.receive(event)
	for i in range(1,6):check(not game.controllers.attack[i],"keyboard event routed to bot "+str(i))
	for tick in 180:game._process(1.0/60.0)
	var report:={"passed":failures.is_empty(),"failures":failures,"cases":cases,"mixed_players":6,"mixed_bots":5,"native_full_match_parity":false}
	FileAccess.open("res://../evidence/godot-player-ai-game-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  ")+"\n")
	print("BF_PLAYER_AI_GAME "+JSON.stringify(report))
	var before_dispose:Array[WeakRef]=[]
	for ai:OriginalPlayerAI in game.bots.members.values():before_dispose.append(weakref(ai))
	game.bots.dispose();await process_frame
	var retained_after_dispose:=0
	for ref:WeakRef in before_dispose:
		if ref.get_ref()!=null:retained_after_dispose+=1
	check(retained_after_dispose==0,"disposed bot AI remained reachable: "+str(retained_after_dispose))
	game.queue_free()
	await process_frame
	quit(0 if failures.is_empty() else 1)
