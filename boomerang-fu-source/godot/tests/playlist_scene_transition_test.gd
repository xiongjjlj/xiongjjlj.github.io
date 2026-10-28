extends SceneTree
var failures:Array=[]
var checks:=0
var visited:Array=[]
var game:Node

func check(value:bool,message:String)->void:
	checks+=1
	if not value:failures.append(message)

func _initialize()->void:call_deferred("run")

func changed(previous:WeakRef)->bool:
	for frame in 120:
		await process_frame
		if current_scene!=null and current_scene!=previous.get_ref() and not current_scene.transition_pending:
			game=current_scene;game.set_process(false)
			await process_frame
			check(previous.get_ref()==null,"old scene released")
			check(not has_meta("boomerang_ffa_scene_transition"),"transition snapshot consumed")
			check(root.get_child_count()==1,"one active scene under root")
			visited.append({"scene":game.active_scene.name,"round":game.round_state.round_number,"players":game.actors.size(),"pool":game.match_playlist.playlist.size()})
			return true
	failures.append("scene transition timed out")
	return false

func run()->void:
	var orphan_start:=int(Performance.get_monitor(Performance.OBJECT_ORPHAN_NODE_COUNT))
	game=load("res://main.tscn").instantiate();root.add_child(game);current_scene=game;game.set_process(false)
	var names:Array[String]=["Avocado","Watermelon"]
	var devices:Array[int]=[-1,-2]
	var old:WeakRef=weakref(game)
	game.start_playlist_match(names,devices,1,false,0)
	game.start_playlist_match(names,devices,1,true,999) # Repeated button press must not mutate pending session.
	if not await changed(old):finish();return
	check(game.active_scene.name=="LevelBambooGrove","seeded first map matches native fixture")
	check(game.match_playlist.playlist.size()==36,"default two-player base pool has 36 maps")
	check(game.match_playlist.match_index==1,"first map consumed once")
	check(game.setup.original_spawns,"original map spawn points enabled")
	game.round_state.scores.scores[0].points=3
	game.round_state.scores.scores[0].suicides=1
	game.weapons[0].powers.start(512);game.weapons[0].powers.start(2)
	game.actors[0].time_controlling_disc=3.25
	game.weapons[0].teleport.count=2;game.weapons[0].teleport.last_timestamp=4.5
	game.clock.time_counter=18.25
	game.powerup_world.collected_total=7;game.powerup_world.serial=42
	game.powerup_world.possible.assign([2,32,256]);game.powerup_world.removed.assign([16])
	var expected_rng:=RandomNumberGenerator.new()
	expected_rng.seed=game.powerup_world.rng.seed;expected_rng.state=game.powerup_world.rng.state
	expected_rng.randf_range(0.0,0.5)
	game.round_state.phase=OriginalFfaRound.Phase.SCORES
	old=weakref(game);game.next_round();game.next_round()
	if not await changed(old):finish();return
	check(game.active_scene.name=="LevelMarketplace","native second map selected")
	check(game.round_state.round_number==2,"round number advances once")
	check(game.round_state.scores.net_score(0)==2,"net score preserved across map load")
	check(game.round_state.scores.scores[0].points==3 and game.round_state.scores.scores[0].suicides==1,"raw score components preserved")
	check((game.weapons[0].powers.active&514)==514,"cross-round powers preserved")
	check(game.actors[0].time_controlling_disc==3.25,"controlled disc duration preserved")
	check(game.weapons[0].teleport.count==2 and game.weapons[0].teleport.last_timestamp==4.5,"teleport statistics preserved")
	check(game.powerup_world.collected_total==7 and game.powerup_world.serial==42,"powerup match counters preserved")
	check(game.powerup_world.possible==[2,32,256] and game.powerup_world.removed==[16],"powerup pool preserved")
	check(game.powerup_world.rng.state==expected_rng.state,"powerup RNG continues across map load")
	check(game.clock.time_counter>=18.25,"match clock survives map change")
	names.assign(["Avocado","Watermelon","Bread","Banana","Milk","Coffee"])
	devices.assign([-1,-2,-102,-103,-104,-105])
	old=weakref(game);game.start_playlist_match(names,devices,1,false,0)
	if not await changed(old):finish();return
	check(game.actors.size()==6 and game.setup.roster==names and game.setup.sources==devices,"six-player configuration preserved")
	check(game.match_playlist.playlist.size()==31,"six-player base pool excludes five small maps")
	check(game.match_playlist.playlist.all(func(id):return int(game.match_playlist.entries[id].max_players)>=6),"no undersized maps in normal pool")
	old=weakref(game);game.advance_playlist_round(true);game.advance_playlist_round(true)
	if not await changed(old):finish();return
	check(game.round_state.round_number==1 and game.round_state.scores.net_score(0)==0,"rematch resets scores and round")
	check(game.powerup_world.collected_total==0 and game.powerup_world.last_round_before_powers==1,"rematch resets powerups and uses original earlier spawn gate")
	check(game.match_playlist.playlist.size()==31 and game.match_playlist.match_index==1,"rematch cursor initialized once")
	check(game.weapons.all(func(w):return w.powers.active==0),"rematch clears powers")
	names.assign(["Avocado","Watermelon"]);devices.assign([-1,-2])
	old=weakref(game);game.start_playlist_match(names,devices,1,true,0)
	if not await changed(old):finish();return
	check(game.match_playlist.playlist.size()==57,"optional DLC pool has 57 regular maps")
	check(not game.match_playlist.playlist.has(13),"tutorial excluded from regular arena pool")
	check(game.match_playlist.playlist.count(24)==1,"Toaster counts once")
	check(int(Performance.get_monitor(Performance.OBJECT_ORPHAN_NODE_COUNT))<=orphan_start,"scene changes do not leave orphan nodes")
	finish()

func finish()->void:
	var report:Dictionary={"passed":failures.is_empty(),"checks":checks,"failures":failures,"visited":visited,"scope":"Actual Godot scene reloads: base/DLC player-count filtering, scores/powers/controller state, powerup RNG continuity, rematch and duplicate-input guard. Browser presentation and original transition timing are separate."}
	FileAccess.open("res://../evidence/godot-playlist-scene-transition-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  ")+"\n")
	print("PLAYLIST_TRANSITION_CHECK "+JSON.stringify(report));quit(0 if failures.is_empty() else 1)
