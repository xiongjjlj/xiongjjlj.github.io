extends SceneTree
const C=preload("res://core/original_timekeeper.gd")
var checks:Array=[]
func check(title:String,passed:bool)->void:checks.append({"name":title,"passed":passed})
func _initialize()->void:call_deferred("run")
func throw_disc(scene)->OriginalDiscMotion:
	var weapon:OriginalPlayerWeapons=scene.weapons[0]
	weapon.actor.last_input_move=Vector2.UP
	weapon.display_position=weapon.actor.position
	weapon.display_rotation=0.0
	weapon.try_throw()
	scene.spawn_discs(0,weapon.actor.rotation_z)
	return weapon.owned_discs[0]
func advance(scene,frames:int)->void:
	for i in frames:
		scene.clock.time_counter=C.f32(scene.clock.time_counter+C.f32(1.0/60.0))
		scene.clock.unscaled_time_counter=C.f32(scene.clock.unscaled_time_counter+C.f32(1.0/60.0))
		scene.advance_explosions(1.0/60.0)
		scene.consume_combat_events()
		scene.consume_book_events()
func run()->void:
	var scene=load("res://main.tscn").instantiate()
	root.add_child(scene)
	scene.set_process(false)
	await process_frame # Resolve render resources before rapid reset/material mutations.
	var baseline:=get_node_count()
	check("explosive available in normal pool",scene.powerup_world.possible.has(32))
	scene.grant_preview_power(0,32)
	var disc:=throw_disc(scene)
	check("throw arms original 1.33-second fuse",disc.grenade_active and disc.grenade_timer==C.f32(1.33))
	disc.position=Vector2(0,-8)
	scene.actors[0].position=Vector2(-40,0)
	scene.actors[1].position=disc.position
	scene.powerup_world.spawn_book(disc.position)
	scene.consume_book_events()
	advance(scene,75)
	check("fuse does not detonate early",scene.explosions.is_empty() and disc.active)
	advance(scene,12)
	check("fuse recycles thrown disc and creates blast",not disc.active and scene.explosions.size()==1)
	check("blast kills nearby unshielded actor",scene.actors[1].state==OriginalActorMotion.State.DEAD)
	check("blast uses recovered squished body and burnt materials",scene.characters[1].squished_node != null and not scene.characters[1].burnt_overrides.is_empty())
	check("blast destroys powerup book",scene.powerup_world.books.is_empty() and scene.book_views.is_empty())
	check("blast queues one replacement",scene.weapons[0].held_discs==0 and scene.weapons[0].powers.pending_discs==1)
	advance(scene,120)
	check("replacement arrives after 1.75-second coroutine",scene.weapons[0].held_discs==1 and scene.weapons[0].powers.pending_discs==0)
	check("blast expires",scene.explosions.is_empty())
	scene.reset_motions(true)
	check("round restores burnt material overrides",scene.characters[1].burnt_overrides.is_empty() and scene.characters[1].squished_node == null)
	scene.grant_preview_power(0,32)
	disc=throw_disc(scene)
	var captured_owner:OriginalActorMotion=disc.grenade_owner
	disc.owner_actor=null
	disc.position=Vector2(0,-8)
	disc.grenade_timer=0.0
	scene.actors[0].position=Vector2(-40,0)
	scene.actors[1].position=Vector2(40,0)
	advance(scene,1)
	check("public disc retains original explosive owner",captured_owner==scene.actors[0] and scene.explosions[0].motion.owner==0)
	scene.reset_motions(true)
	scene.grant_preview_power(0,32)
	scene.grant_preview_power(0,64)
	disc=throw_disc(scene)
	scene.weapons[0].multidisc_burst(disc)
	scene.spawn_discs(0,0)
	scene.cleanup_inactive_discs()
	check("multi explosive spawns four armed fragments",scene.flying_discs.size()==4 and scene.flying_discs.all(func(item):return item.motion.is_mini_disc and item.motion.grenade_active))
	scene.actors[0].position=Vector2(-40,0)
	scene.actors[1].position=Vector2(40,0)
	for item in scene.flying_discs:
		item.motion.position=Vector2(0,-8)
		item.motion.grenade_timer=0.0
	advance(scene,1)
	check("four fragments create four half-radius blasts",scene.explosions.size()==4 and scene.explosions.all(func(item):return item.motion.target_radius==6.0))
	check("multi explosive queues only one replacement",scene.weapons[0].powers.pending_discs==1 and scene.weapons[0].respawn_jobs.size()==1)
	scene.reset_motions(true)
	await process_frame
	check("reset restores scene node count",get_node_count()==baseline)
	var passed:=checks.all(func(item):return item.passed)
	var report:Dictionary={"passed":passed,"checks":checks,"node_baseline":baseline,"node_after":get_node_count(),"full_game_parity_verified":false}
	FileAccess.open("res://../evidence/godot-explosive-scene-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print("BF_EXPLOSIVE_SCENE_TEST "+JSON.stringify(report))
	scene.queue_free()
	await process_frame
	quit(0 if passed else 1)
