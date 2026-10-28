extends SceneTree
var checks: Array = []
var game: Node
func check(name: String, passed: bool, details: Variant = null) -> void:
	checks.append({"name":name,"passed":passed,"details":details})
func _initialize() -> void: call_deferred("run")
func tick(count := 1) -> void:
	for i in count:game._process(0.016666)
func run() -> void:
	game=load("res://main.tscn").instantiate();root.add_child(game);game.set_process(false)
	var names:Array[String]=["Avocado","Watermelon","Bread","Banana","Milk","Coffee"]
	var devices:Array[int]=[-1,-2,0,1,2,3]
	game.configure_match(names,devices,FfaSetup.Length.SHORT)
	check("six distinct original characters and source spawn points",game.actors.size()==6 and game.characters.size()==6 and game.round_state.scores.goal==15 and game.collision_world.actor_shapes.size()==6)
	var all_bound:=true
	for view in game.characters:
		for clip in ["idle","walk","death","windup-right","windup-left","melee-right","melee-left","disarm","stunned","fall","frozen","burnt"]:
			view.set_clip("res://assets/characters/"+view.model_name+"-"+clip+".json")
			view.advance(0.12)
			all_bound=all_bound and view.missing_targets.is_empty()
		view.set_burnt(true);view.set_burnt(false)
		view.set_sliced(game.body_bindings[view.model_name],true)
		view.set_sliced(game.body_bindings[view.model_name],false)
		view.set_squished(game.body_bindings[view.model_name],true)
		view.set_squished(game.body_bindings[view.model_name],false)
	check("all 72 character clips bind with no missing targets",all_bound)
	game.reset_motions(true)
	for index in 6:game.actors[index].is_frozen=true
	tick()
	check("frozen visuals cover all six players",game.cell_preview.frozen.size()==6 and game.cell_preview.frozen.all(func(v):return v.visible))
	game.reset_motions(true)
	for index in 6:game.cell_preview.actor_fire(index,game.actors[index],0.016666)
	check("six fire emitters have independent bounded state",game.cell_preview.actor_emission.size()==6)
	game.reset_motions(true)
	for i in range(1,6):
		game.combat.killer_ids[i]=0
		game.combat.kill(i,"melee")
	game.consume_combat_events()
	for i in 300:
		tick()
		if game.round_state.phase==OriginalFfaRound.Phase.SCORES:break
	check("six player kills lead to one round result",game.round_state.scores.net_score(0)==5 and game.round_state.phase==OriginalFfaRound.Phase.SCORES)
	game.next_round()
	check("next round keeps scores and resets six actors",game.round_state.round_number==2 and game.round_state.scores.net_score(0)==5 and game.actors.all(func(a):return a.state==OriginalActorMotion.State.ALIVE))
	game.round_state.scores.winners.assign([1,4])
	game.round_state.phase=OriginalFfaRound.Phase.SCORES
	game.next_round()
	check("tiebreak only enables the two finalists",game.actors.filter(func(a):return a.state!=OriginalActorMotion.State.DEAD).size()==2 and game.collision_world.actor_shapes.size()==2 and game.round_state.scores.tie_breaker)
	game.combat.killer_ids[4]=1;game.combat.kill(4,"melee");game.consume_combat_events()
	check("first tiebreak kill ends match with one winner",game.round_state.phase==OriginalFfaRound.Phase.MATCH_END and game.round_state.scores.winners==[1])
	var short_names:Array[String]=["Bread","Coffee"];var short_devices:Array[int]=[-1,-2]
	game.configure_match(short_names,short_devices,FfaSetup.Length.LONG)
	check("new two-player long match resets roster score and queries",game.characters.size()==2 and game.actors.size()==2 and game.round_state.scores.goal==16 and game.round_state.scores.net_score(0)==0 and game.collision_world.actor_shapes.size()==2)
	var flow:=FfaShell.new();game.add_child(flow);game.shell=flow;flow.initialize(game)
	check("lobby hides validation controls and suspends simulation",flow.mode==FfaShell.Mode.LOBBY and not game.debug_canvas.visible and game.clock.time_scale==0.0)
	flow.selected_names.assign(["Bread","Coffee","Avocado","Watermelon","Banana","Milk"])
	flow.start_match()
	check("menu start selects characters and match length",game.characters[0].model_name=="Bread" and game.characters[1].model_name=="Coffee" and game.round_state.scores.goal==14 and flow.mode==FfaShell.Mode.READY)
	var point:Vector2=game.actors[0].position;tick(30)
	check("ready phase holds actor simulation",game.actors[0].position==point and flow.mode==FfaShell.Mode.READY)
	tick(31)
	check("source one-second ready leads to gameplay",flow.mode==FfaShell.Mode.PLAYING and game.clock.time_scale>0.0)
	game.clock.slow_mo_effect(0.3)
	tick(2)
	var scale:float=game.clock.time_scale
	flow.pause_game();var time:float=game.clock.time_counter;var timer:float=game.clock.slow_mo_timer
	tick(60)
	check("pause freezes world and slow-motion timers",game.clock.time_counter==time and game.clock.slow_mo_timer==timer and game.actors[0].position==point and flow.mode==FfaShell.Mode.PAUSED)
	flow.resume_game()
	check("resume preserves pre-pause time scale",is_equal_approx(game.clock.time_scale,scale) and flow.mode==FfaShell.Mode.PLAYING)
	game.clock.reset_time_scale()
	game.combat.killer_ids[1]=0;game.combat.kill(1,"melee");game.consume_combat_events()
	for i in 300:
		tick()
		if flow.mode==FfaShell.Mode.SCORES:break
	check("normal match exposes round scoreboard",flow.mode==FfaShell.Mode.SCORES and flow.modal.visible)
	flow.continue_round();check("continue returns to ready preserving points",flow.mode==FfaShell.Mode.READY and game.round_state.scores.net_score(0)==1)
	flow.on_focus_lost();check("focus loss pauses ready countdown",flow.mode==FfaShell.Mode.PAUSED)
	var adapter:=LocalControllers.new();adapter.configure(devices)
	var event:=InputEventJoypadButton.new();event.device=3;event.button_index=JOY_BUTTON_X;event.pressed=true;adapter.receive(event)
	check("gamepad edge routes only to its assigned slot",adapter.attack==[false,false,false,false,false,true])
	adapter.sample(5);check("gamepad press consumed once",not adapter.attack[5])
	var goals:=[]
	for length in 3:
		for count in [2,3,6]:goals.append(FfaSetup.score_goal(length,count))
	check("original FFA score goals for lengths and player counts",goals==[10,15,15,14,16,16,16,24,24])
	game.shell=null;flow.free()
	game.configure_match(short_names,short_devices,FfaSetup.Length.MEDIUM)
	await process_frame;await process_frame
	var baseline:=get_node_count()
	for i in 3:
		game.configure_match(names,devices,FfaSetup.Length.MEDIUM)
		game.configure_match(short_names,short_devices,FfaSetup.Length.MEDIUM)
		await process_frame;await process_frame
	check("repeated two/six player setup reclaims scene nodes",get_node_count()==baseline,{"baseline":baseline,"after":get_node_count()})
	var passed:bool=checks.all(func(c):return c.passed)
	var report:Dictionary={"passed":passed,"checks":checks,"scope":"Actual imported scene and six models, full local FFA shell, input adapter routing and node reclamation. Gamepad hardware/browser mapping, original menu pixels, full native scene scheduling and character-specific effects are not proven."}
	FileAccess.open("res://../evidence/godot-ffa-shell-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  "))
	print("BF_FFA_SHELL "+JSON.stringify(report))
	game.queue_free();await process_frame;quit(0 if passed else 1)
