extends SceneTree
var checks:Array=[]
func check(name:String,passed:bool)->void:checks.append({"name":name,"passed":passed})
func _initialize()->void:call_deferred("run")
func run()->void:
	var scene=load("res://main.tscn").instantiate()
	root.add_child(scene)
	scene.set_process(false)
	await process_frame
	scene._process(0)
	var baseline:=get_node_count()
	check("Shrink available in normal power pool",scene.powerup_world.possible.has(16384))
	check("original bounds size and mesh",scene.boundary.size==Vector2(43,43) and scene.boundary_view.line.mesh.surface_get_array_len(0)==42)
	check("source glow hidden initially",not scene.boundary_view.glow.visible)
	var history:int=scene.weapons[0].powers.history.size()
	scene.grant_preview_power(0,16384)
	scene.consume_power_events()
	check("instant power starts shrinking without a history entry",scene.boundary.shrinking and scene.boundary.marker and scene.boundary.owner==0 and scene.weapons[0].powers.history.size()==history)
	check("source width determines duration",scene.boundary.duration==OriginalTimekeeper.f32(43.0/7.0))
	scene._process(1.0/60.0)
	check("shrink disables duplicate book selection and shows glow",not scene.powerup_world.shrinking_allowed and scene.boundary_view.glow.visible)
	# Place a non-owner outside after shrinking has started. Immunity/shield do not save them.
	scene.actors[1].position=Vector2(24,0)
	scene.previous_positions[1]=scene.actors[1].position
	scene.actors[1].can_walk=false
	scene.grant_preview_power(1,16)
	scene.actors[1].is_invulnerable=true
	for i in 270:
		scene.actor_bounds[1].frame_step(1.0/60.0,Vector2(24,0))
		if scene.actor_bounds[1].death_requested:
			scene.combat.kill(1,"outofbounds")
			scene.consume_combat_events()
			break
	check("out-of-bounds bypasses shield and immunity",scene.actors[1].state==OriginalActorMotion.State.DEAD)
	check("boundary owner receives kill",scene.round_state.scores.net_score(0)==1 and scene.combat.killer_ids[1]==0)
	check("out-of-bounds hides actor without slicing",not scene.actors[1].appearance_visible and not scene.actors[1].is_sliced and not scene.actors[1].physics_enabled)
	scene._process(1.0/60.0)
	check("last survivor stops contraction",not scene.boundary.shrinking)
	scene.reset_motions(true)
	check("new match clears boundary owner and sudden death",scene.boundary.owner==-1 and not scene.boundary.sudden_death and scene.boundary.size==Vector2(43,43))
	scene.boundary.round_timer=180.0
	scene._process(1.0/60.0)
	check("three-minute sudden death starts ownerless shrinking",scene.boundary.sudden_death and scene.boundary.shrinking and scene.boundary.marker and scene.boundary.owner==-1)
	scene.reset_motions(true)
	scene._process(0)
	await process_frame
	check("repeat reset restores node count",get_node_count()==baseline)
	var report:Dictionary={"passed":checks.all(func(x):return x.passed),"checks":checks,"node_baseline":baseline,"node_after":get_node_count(),"full_game_parity_verified":false}
	FileAccess.open("res://../evidence/godot-boundary-scene-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print("BF_BOUNDARY_SCENE "+JSON.stringify(report))
	scene.queue_free()
	await process_frame
	quit(0 if report.passed else 1)
