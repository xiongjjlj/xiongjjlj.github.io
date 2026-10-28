extends SceneTree
var checks:Array=[]
func check(name:String,passed:bool)->void:checks.append({"name":name,"passed":passed})
func _initialize()->void:call_deferred("run")
func run()->void:
	var scene=load("res://main.tscn").instantiate()
	root.add_child(scene)
	scene.set_process(false)
	await process_frame
	var baseline:=get_node_count()
	check("Disguise in normal book pool",scene.powerup_world.possible.has(2048))
	check("16 original props and exact animation bindings",scene.disguise_views.all(func(v):return v.props.size()==16 and v.props.all(func(p):return p!=null) and v.missing_targets.is_empty()))
	scene.grant_preview_power(0,2048)
	for i in 25:scene._process(1.0/60.0)
	var state:OriginalDisguise=scene.disguises[0]
	check("stationary player becomes disguised",state.disguised)
	check("original prop replaces character",scene.disguise_views[0].visible and not scene.characters[0].visible and scene.disguise_views[0].props[state.selected].is_visible_in_tree())
	var expected:=state.nearest(Vector3(scene.actors[0].position.x,scene.actors[0].position.y,scene.actors[0].source_z),15.0)
	check("nearest active original obstacle selected",expected!=0 and state.types[state.selected]==expected)
	check("attack breaks disguise",scene.weapons[0].try_attack() and not state.disguised)
	scene._process(1.0/60.0)
	check("character returns after attack",scene.characters[0].visible)
	scene.reset_motions(true)
	scene.grant_preview_power(0,2048)
	state=scene.disguises[0]
	state.set_disguised(true)
	scene.weapons[0].aim(1.0/60.0)
	check("aim breaks disguise before charge threshold",not state.disguised)
	state.set_disguised(true)
	scene.weapons[0].held_discs=0
	check("failed empty throw still breaks disguise",not scene.weapons[0].try_throw() and not state.disguised)
	scene.grant_preview_power(0,16)
	state.set_disguised(true)
	scene._process(1.0/60.0)
	check("disguise hides shield without removing protection",scene.actors[0].is_shielded and not scene.shield_views[0].visible)
	scene.actor_elements[0].toggle_fire(true)
	check("ignition immediately breaks disguise",not state.disguised)
	state.set_disguised(true)
	scene.actors[0].get_stunned(Vector2.RIGHT)
	check("stun immediately breaks disguise",not state.disguised)
	state.set_disguised(true)
	scene.combat.kill(0,"disc")
	check("death clears disguise",not state.disguised)
	scene.reset_motions(true)
	await process_frame
	check("reset restores node count and original visibility",get_node_count()==baseline and scene.disguise_views.all(func(v):return not v.visible) and scene.disguises.all(func(d):return not d.disguised))
	var report:Dictionary={"passed":checks.all(func(x):return x.passed),"checks":checks,"node_baseline":baseline,"node_after":get_node_count(),"full_game_parity_verified":false}
	FileAccess.open("res://../evidence/godot-disguise-scene-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print("BF_DISGUISE_SCENE "+JSON.stringify(report))
	scene.queue_free()
	await process_frame
	quit(0 if report.passed else 1)
