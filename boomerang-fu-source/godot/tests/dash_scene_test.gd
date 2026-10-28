extends SceneTree
var checks: Array = []
func check(name: String, passed: bool) -> void: checks.append({"name":name,"passed":passed})
func _initialize() -> void: call_deferred("run")
func run() -> void:
	var scene = load("res://main.tscn").instantiate()
	root.add_child(scene)
	scene.set_process(false)
	var baseline := get_node_count()
	var positions: Array = []
	for phase in [false,true]:
		var setup: Dictionary = scene.prepare_wall_preview(phase)
		check("clear path to an actual source wall (%s)" % phase,not setup.is_empty())
		var actor: OriginalActorMotion = scene.actors[0]
		actor.input_move=Vector2.RIGHT
		scene.player_inputs[0].process_frame(false,false,false,true,1.0/60.0,0.0,actor.position,actor.rotation_z)
		check("input starts a dash (%s)" % phase,scene.player_inputs[0].actions.has("dash"))
		for frame in 45: scene._process(1.0/60.0)
		positions.append(actor.position.x)
		check("wall passage follows source power flag (%s)" % phase,(actor.position.x>setup.wall_end+actor.radius)==phase)
		check("dash restores collision after leaving wall (%s)" % phase,not actor.is_dashing and not actor.insides_enabled and actor.collision_layer==768)
		check("outside enclosing wall still has original floor (%s)" % phase,actor.state==OriginalActorMotion.State.ALIVE and not actor.is_falling)
	scene.prepare_wall_preview(true)
	scene.actors[0].position=Vector2(21,0)
	check("source phase power blocks melee while inside wall",not scene.weapons[0].try_attack())
	scene.weapons[0].powers.clear()
	check("phase-specific melee gate clears with power",scene.weapons[0].try_attack())
	scene.reset_motions(true)
	await process_frame
	check("preview resets without extra nodes",get_node_count()==baseline and not scene.actors[0].dash_through_walls)
	var failed := checks.filter(func(item):return not item.passed)
	var report := {"passed":failed.is_empty(),"checks":checks,"failed":failed,"final_x_without_and_with_phase":positions,"nodes_before":baseline,"nodes_after":get_node_count(),"full_game_parity_verified":false}
	FileAccess.open("res://../evidence/godot-dash-scene-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print(JSON.stringify(report))
	scene.free()
	quit(0 if failed.is_empty() else 1)
