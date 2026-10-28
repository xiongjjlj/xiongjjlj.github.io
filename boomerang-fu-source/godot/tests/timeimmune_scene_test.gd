extends SceneTree
const C = preload("res://core/original_timekeeper.gd")
var checks: Array = []
func check(name:String,passed:bool)->void:checks.append({"name":name,"passed":passed})
func _initialize()->void:call_deferred("run")
func run()->void:
	var scene=load("res://main.tscn").instantiate()
	root.add_child(scene)
	scene.set_process(false)
	var baseline:=get_node_count()
	scene.grant_preview_power(0,4096)
	for frame in 3:scene._process(1.0/60.0)
	check("grant reaches the shared source clock",scene.clock.slow_mo and scene.clock.time_scale==C.f32(0.4))
	check("only the holder has personal time compensation",scene.actors[0].time_scaler==2.5 and scene.actors[1].time_scaler==1.0)
	check("temporary warp does not consume persistent power slots",scene.weapons[0].powers.history.is_empty())
	var weapon:OriginalPlayerWeapons=scene.weapons[0]
	weapon.display_position=weapon.actor.position
	weapon.display_rotation=weapon.actor.rotation_z
	check("holder can throw during warp",weapon.try_throw())
	var disc:OriginalDiscMotion=weapon.spawned_discs[0]
	check("base and derived throw calls each issue and drain a contact query",disc.time_scaler==2.5 and disc.time_scaler_cooldown==1.0 and disc.time_scaled_contact_requests==2 and disc.pending_time_scaled_contacts==0)
	scene.spawn_discs(0,weapon.actor.rotation_z)
	scene._process(1.0/60.0)
	check("disc compensation decays in the actual frame loop",disc.time_scaler>1.0 and disc.time_scaler<2.5 and disc.time_scaler_cooldown<1.0)
	scene.clock.freeze(0.04)
	scene._process(1.0/60.0)
	scene._process(1.0/60.0)
	check("freeze uses the source division-by-zero guard",scene.actors[0].time_scaler==1.0 and not is_nan(disc.time_scaler))
	for frame in 180:scene._process(1.0/60.0)
	check("warp expires and restores the shared and personal clocks",not weapon.powers.has_power(4096) and scene.clock.time_scale==1.0 and weapon.actor.time_scaler==1.0)
	check("power event queue is drained",weapon.powers.events.is_empty())
	scene.reset_motions(true)
	scene.grant_preview_power(0,4096)
	for frame in 3:scene._process(1.0/60.0)
	scene.combat.kill(0,"fall")
	check("last holder death resets time before clearing the temporary flag",scene.clock.time_scale==1.0 and scene.clock.slow_scale==C.f32(0.2) and not scene.weapons[0].powers.has_power(4096))
	scene.reset_motions(true)
	for index in 2:scene.grant_preview_power(index,4096)
	for frame in 3:scene._process(1.0/60.0)
	scene.combat.kill(0,"fall")
	check("another holder prevents an immediate time reset",scene.clock.time_scale==C.f32(0.4) and scene.weapons[1].powers.has_power(4096))
	for immune in [false,true]:
		scene.reset_motions(true)
		if immune:
			scene.grant_preview_power(0,4096)
			for frame in 3:scene._process(1.0/60.0)
		weapon=scene.weapons[0]
		scene.actors[1].position=weapon.actor.position+Vector2(0,-2.0)
		weapon.display_position=weapon.actor.position
		weapon.display_rotation=PI
		weapon.actor.last_input_move=Vector2(0,-1)
		check("nearby target throw succeeds (%s)"%immune,weapon.try_throw())
		check("only accelerated throw checks target before next physics tick (%s)"%immune,(scene.actors[1].state==OriginalActorMotion.State.DEAD)==immune)
	scene.reset_motions(true)
	await process_frame
	check("reset leaves no extra scene nodes or temporary power",get_node_count()==baseline and not scene.actors[0].time_immune)
	var failed:=checks.filter(func(item):return not item.passed)
	var report:={"passed":failed.is_empty(),"checks":checks,"failed":failed,"nodes_before":baseline,"nodes_after":get_node_count(),"particles_and_full_frame_parity_verified":false}
	FileAccess.open("res://../evidence/godot-timeimmune-scene-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print(JSON.stringify(report))
	scene.free()
	quit(0 if failed.is_empty() else 1)
