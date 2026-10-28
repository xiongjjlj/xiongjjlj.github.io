extends SceneTree
var checks:Array=[]
func check(name:String,passed:bool)->void:checks.append({"name":name,"passed":passed})
func _initialize()->void:call_deferred("run")
func step(scene,input:Vector2,held:=true,released:=false)->void:
	scene.input_states[0]=input
	scene.actors[0].raw_input_move=input
	scene.input_throw_states[0]=held
	scene.input_release_states[0]=released
	scene._process(1.0/60.0)
func throw_disc(scene)->OriginalDiscMotion:
	var weapon:OriginalPlayerWeapons=scene.weapons[0]
	weapon.actor.last_input_move=Vector2(0,1)
	weapon.display_position=weapon.actor.position
	weapon.display_rotation=0.0
	weapon.try_throw()
	scene.spawn_discs(0,weapon.actor.rotation_z)
	return weapon.owned_discs[0]
func run()->void:
	var scene=load("res://main.tscn").instantiate()
	root.add_child(scene)
	scene.set_process(false)
	var baseline:=get_node_count()
	scene.actors[1].is_invulnerable=true
	check("normal book pool includes controlled disc",scene.powerup_world.possible.has(8192))
	scene.grant_preview_power(0,8192)
	var disc:=throw_disc(scene)
	check("throw captures controlled flag",disc.disc_powerups==8192)
	for frame in 8:step(scene,Vector2(1,0))
	check("held throw enters controlled movement in scene update",disc.active and disc.movement==OriginalDiscMotion.Movement.CONTROLLED)
	check("control resets charge power and disables spin",disc.aim_power==0.0 and not disc.is_spinning)
	check("raw input reaches disc steering",disc.controlled_direction==Vector2(1,0))
	check("owner uses original tiny control movement force",scene.actors[0].get_move_speed()==OriginalTimekeeper.f32(0.1))
	check("control time metadata accumulates",scene.actors[0].time_controlling_disc>0.0)
	scene.grant_preview_power(0,8)
	for frame in 3:step(scene,Vector2(0,1))
	check("reverse player movement does not reverse disc input",disc.controlled_direction==Vector2(0,1) and scene.actors[0].last_input_move==Vector2(0,-1))
	step(scene,Vector2.ZERO,false,true)
	step(scene,Vector2.ZERO,false,false)
	check("release exits controlled state",disc.movement!=OriginalDiscMotion.Movement.CONTROLLED and scene.actors[0].retrieve_charge==0.0)
	var elapsed:float=scene.actors[0].time_controlling_disc
	scene.reset_motions(false)
	check("next round preserves power and accumulated control time",scene.weapons[0].powers.has_power(8192) and scene.actors[0].time_controlling_disc==elapsed and scene.flying_discs.is_empty())
	scene.reset_motions(true)
	scene.actors[1].is_invulnerable=true
	scene.grant_preview_power(0,8192)
	scene.grant_preview_power(0,64)
	var parent:=throw_disc(scene)
	for frame in 8:step(scene,Vector2(0,1))
	check("Multi parent stays intact during control",parent.active and parent.movement==OriginalDiscMotion.Movement.CONTROLLED and scene.weapons[0].owned_discs.size()==1)
	step(scene,Vector2.ZERO,false,true)
	for frame in 3:step(scene,Vector2.ZERO,false,false)
	check("released Multi parent splits and children retain Control",not parent.active and scene.weapons[0].owned_discs.size()==5 and scene.weapons[0].owned_discs.all(func(d):return d.is_mini_disc and d.disc_powerups==8192))
	for frame in 3:step(scene,Vector2(1,0))
	check("all five fragments can be controlled together",scene.weapons[0].owned_discs.size()==5 and scene.weapons[0].owned_discs.all(func(d):return d.movement==OriginalDiscMotion.Movement.CONTROLLED and d.controlled_direction==Vector2(1,0)))
	scene.reset_motions(true)
	await process_frame
	check("new match clears control stats flags views and restores node count",scene.actors[0].time_controlling_disc==0.0 and not scene.actors[0].controlled_disc and scene.flying_discs.is_empty() and get_node_count()==baseline)
	var failed:=checks.filter(func(x):return not x.passed)
	var report:={"passed":failed.is_empty(),"checks":checks,"failed":failed,"nodes_before":baseline,"nodes_after":get_node_count(),"original_control_pfx_audio_and_frame_parity_verified":false}
	FileAccess.open("res://../evidence/godot-controlled-scene-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print(JSON.stringify(report))
	scene.free()
	quit(0 if failed.is_empty() else 1)
