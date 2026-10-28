extends SceneTree
var checks:Array=[]
func check(name:String,passed:bool)->void:checks.append({"name":name,"passed":passed})
func _initialize()->void:call_deferred("run")
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
	check("teleport is available in normal book pool",scene.powerup_world.possible.has(128))
	scene.grant_preview_power(0,128)
	scene.grant_preview_power(1,16)
	scene.actors[1].is_invulnerable=true
	var disc:=throw_disc(scene)
	check("throw preserves teleport flag",disc.disc_powerups==128)
	var destination:Vector2=scene.actors[1].position
	disc.position=destination
	scene.actors[0].can_dash=false
	scene.input_dash_states[0]=true
	scene._process(0.0)
	check("dash input teleports despite cooldown",scene.actors[0].position==destination and scene.weapons[0].teleport.count==1)
	check("render position snaps in the same frame",scene.characters[0].position.x==destination.x and is_equal_approx(scene.characters[0].position.y,destination.y-0.4) and scene.previous_positions[0]==destination and scene.actor_render_positions[0].x==destination.x)
	check("telefrag bypasses shield and invulnerability",scene.actors[1].state==OriginalActorMotion.State.DEAD)
	check("telefrag records source scoring",scene.round_state.scores.net_score(0)==1)
	check("telefrag disables physics",not scene.actors[1].physics_enabled)
	check("telefrag uses recovered squished body",scene.characters[1].squished_node!=null and scene.characters[1].squished_node.visible and not scene.actors[1].is_sliced)
	check("teleport consumes ordinary disc",not disc.active and scene.weapons[0].owned_discs.is_empty() and scene.weapons[0].held_discs==1)
	check("teleport grants no invulnerability",not scene.actors[0].is_invulnerable)
	scene.reset_motions(false)
	check("round preserves teleport metadata",scene.weapons[0].teleport.count==1)
	check("round restores squished character",scene.characters[1].squished_node==null)
	scene.reset_motions(true)
	check("new match resets teleport metadata",scene.weapons[0].teleport.count==0)
	scene.grant_preview_power(0,128)
	scene.grant_preview_power(0,64)
	disc=throw_disc(scene)
	scene.weapons[0].multidisc_burst(disc)
	scene.spawn_discs(0,0.0)
	var children:Array=scene.weapons[0].owned_discs.duplicate()
	for index in children.size(): children[index].position=Vector2(-3.5,-2.0+index)
	scene.input_dash_states[0]=true
	scene._process(0.0)
	check("teleport collects one mini and preserves other fragments",scene.weapons[0].teleport.count==1 and scene.weapons[0].owned_discs.size()==4 and scene.weapons[0].mini_discs_collected==1 and scene.weapons[0].held_discs==0)
	scene.reset_motions(true)
	await process_frame
	await process_frame
	check("scene reset returns node count to baseline",get_node_count()==baseline)
	var passed:=checks.all(func(item):return item.passed)
	var report:Dictionary={"passed":passed,"checks":checks,"node_baseline":baseline,"node_after":get_node_count(),"full_game_parity_verified":false}
	FileAccess.open("res://../evidence/godot-teleport-scene-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print("BF_TELEPORT_SCENE_TEST "+JSON.stringify(report))
	scene.queue_free()
	await process_frame
	quit(0 if passed else 1)
