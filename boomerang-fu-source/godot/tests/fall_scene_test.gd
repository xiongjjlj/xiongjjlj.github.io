extends SceneTree
var checks: Array = []
func check(name: String, passed: bool) -> void: checks.append({"name":name,"passed":passed})
func _initialize() -> void: call_deferred("run")
func run() -> void:
	var scene = load("res://main.tscn").instantiate()
	root.add_child(scene)
	scene.set_process(false)
	var baseline := get_node_count()
	scene.start_fall_preview()
	var actor: OriginalActorMotion = scene.actors[0]
	scene._process(1.0/60.0)
	check("actual imported map starts falling beyond floor",actor.is_falling and actor.state==OriginalActorMotion.State.FALLING)
	check("original fall clip binds in the match",scene.current_clips[0]=="fall" and scene.characters[0].missing_targets.is_empty() and scene.characters[0].bound_tracks==116)
	for frame in 85: scene._process(1.0/60.0)
	check("fall causes death and disables the physical body",actor.state==OriginalActorMotion.State.DEAD and not actor.physics_enabled)
	check("fall hides the unsliced character",not scene.characters[0].visible and not actor.is_sliced)
	scene.collision_world.sync_actors()
	check("hidden body is excluded from collision queries",scene.collision_world.actor_shapes[0].layer==0)
	check("uncredited fall does not invent kill or suicide points",scene.round_state.scores.net_score(0)==0 and scene.round_state.scores.net_score(1)==0)
	scene.reset_motions(true)
	actor=scene.actors[0]
	scene.actor_environments[0].start_fall()
	actor.input_move=Vector2.RIGHT
	scene.player_inputs[0].process_frame(false,false,false,true,1.0/60.0,0.0,actor.position,actor.rotation_z)
	check("input controller can dash-save a fall",scene.player_inputs[0].actions.has("dash") and not actor.is_falling and actor.state==OriginalActorMotion.State.ALIVE and actor.falls_saved==1)
	actor.dash_timer=actor.dash_duration
	actor.calculate_dash()
	check("landing a dash on safe floor restores one fall save",actor.falls_saved==0)
	scene.actor_environments[0].start_fall()
	actor.fall_timer=OriginalTimekeeper.f32(0.34)
	actor.can_dash=true
	check("late dash cannot rescue a fall",not actor.dash() and actor.is_falling)
	scene.reset_motions(true)
	scene._process(1.0/60.0)
	await process_frame
	check("reset restores visible live bodies without extra nodes",scene.characters[0].visible and scene.actors[0].physics_enabled and scene.actors[0].state==OriginalActorMotion.State.ALIVE and get_node_count()==baseline)
	var failed := checks.filter(func(item):return not item.passed)
	var report := {"passed":failed.is_empty(),"checks":checks,"failed":failed,"nodes_before":baseline,"nodes_after":get_node_count(),"visual_parity_verified":false}
	FileAccess.open("res://../evidence/godot-fall-scene-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print(JSON.stringify(report))
	scene.free()
	quit(0 if failed.is_empty() else 1)
