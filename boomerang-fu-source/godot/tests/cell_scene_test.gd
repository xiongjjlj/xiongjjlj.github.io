extends SceneTree
const C=preload("res://core/original_timekeeper.gd")
var checks:Array=[]
func check(title:String,passed:bool)->void:checks.append({"name":title,"passed":passed})
func _initialize()->void:call_deferred("run")
func throw_disc(scene,power:int)->OriginalDiscMotion:
	scene.grant_preview_power(0,power)
	var weapon:OriginalPlayerWeapons=scene.weapons[0]
	weapon.display_position=weapon.actor.position
	weapon.display_rotation=0.0
	weapon.try_throw()
	scene.spawn_discs(0,0.0)
	return weapon.owned_discs[0]
func blast_frames(scene,count:int)->void:
	for i in count:
		scene.clock.time_counter=C.f32(scene.clock.time_counter+C.f32(1.0/60.0))
		scene.clock.unscaled_time_counter=scene.clock.time_counter
		scene.cell_grid.current_time=scene.clock.time_counter
		scene.advance_explosions(1.0/60.0)
		scene.consume_combat_events()
func run()->void:
	var scene=load("res://main.tscn").instantiate()
	root.add_child(scene)
	scene.set_process(false)
	await process_frame # Resolve render resources before rapid reset/material mutations.
	var baseline:=get_node_count()
	check("Fire and Ice available in normal pool",scene.powerup_world.possible.has(256) and scene.powerup_world.possible.has(1024))
	var point:Vector2=scene.actors[1].position
	scene.cell_grid.register_cell(OriginalCellGrid.CellType.FIRE,point,10.0,0)
	scene.actors[1].fixed_step()
	check("fixed actor step ignites from enemy fire cell",scene.actor_elements[1].burning and scene.actor_elements[1].fire_owner==0)
	for i in 153:scene.actor_elements[1].timers(1.0/60.0)
	scene.consume_element_events(1)
	scene.consume_combat_events()
	check("burn kills and attributes score",scene.actors[1].state==OriginalActorMotion.State.DEAD and scene.round_state.scores.net_score(0)==1)
	check("fire death keeps normal body and original burnt material",scene.actors[1].is_burnt and not scene.characters[1].burnt_overrides.is_empty())
	scene.reset_motions(true)
	point=scene.actors[1].position
	scene.cell_grid.register_cell(OriginalCellGrid.CellType.ICE,point,10.0,0)
	for i in 34:scene.actors[1].fixed_step()
	check("fixed actor step freezes after ice countdown",scene.actors[1].is_frozen)
	var before:float=scene.actor_elements[1].freeze_timer
	check("frozen attack accepted as chip",scene.weapons[1].try_attack())
	check("chip adds one twelfth of thaw duration",scene.actor_elements[1].freeze_timer==C.f32(before+7.0/12.0))
	scene.consume_element_events(1)
	scene.cell_preview.update_frozen(scene.actors,0.016666)
	scene.cell_preview.advance(scene.cell_grid,0.016666,scene.camera)
	check("ice creates source decal and frozen shell",scene.cell_preview.decals.size()>0 and scene.cell_preview.frozen[1].visible)
	scene.reset_motions(true)
	var disc:=throw_disc(scene,256)
	disc.position=Vector2(7,-8)
	disc.velocity=Vector2.RIGHT*30
	for i in 8:scene.step_discs()
	check("live thrown Fire disc populates grid",scene.cell_grid.fire_count>0)
	scene.cell_preview.advance(scene.cell_grid,0.016666,scene.camera)
	check("source fire and glow emitted",scene.cell_preview.groups.fireSystem.particles.size()>0 and scene.cell_preview.groups.glowSystem.particles.size()>0)
	scene.reset_motions(true)
	scene.grant_preview_power(0,1024)
	disc=throw_disc(scene,32)
	disc.position=Vector2(0,-8)
	scene.actors[0].position=Vector2(-40,0)
	scene.actors[1].position=disc.position
	blast_frames(scene,90)
	check("Ice plus Explosive freezes instead of killing",scene.actors[1].is_frozen and scene.actors[1].state==OriginalActorMotion.State.ALIVE)
	check("freeze blast uses original radius",scene.explosions.size()==1 and scene.explosions[0].motion.target_radius==17.0)
	scene.reset_motions(true)
	scene.grant_preview_power(0,256)
	disc=throw_disc(scene,32)
	disc.position=Vector2(0,-8)
	for actor in scene.actors:actor.position=Vector2(-40,0)
	blast_frames(scene,97)
	check("Fire plus Explosive deposits fire cells",scene.cell_grid.fire_count>0)
	scene.prepare_element_preview()
	for i in 65:scene._process(1.0/60.0)
	var visible_meshes:Array=scene.cell_preview.frozen[1].find_children("*","MeshInstance3D",true,false)
	check("zero-duration frozen idle preserves renderer visibility",visible_meshes.all(func(mesh):return mesh.is_visible_in_tree()))
	check("contact preview runs actor fire emission and ice freeze",scene.actor_elements[0].burning and scene.actors[1].is_frozen and scene.cell_preview.groups.actorFire_8909.particles.size()>0)
	for i in 360:scene._process(1.0/60.0)
	check("score screen lets cells and particles expire",scene.cell_grid.fire_count==0 and scene.cell_preview.groups.values().all(func(group):return group.particles.is_empty()))
	scene.reset_motions(true)
	await process_frame
	check("reset clears particles and returns node count",get_node_count()==baseline and scene.cell_preview.decals.is_empty() and scene.cell_preview.groups.fireSystem.particles.is_empty())
	var report:Dictionary={"passed":checks.all(func(x):return x.passed),"checks":checks,"node_baseline":baseline,"node_after":get_node_count(),"full_game_parity_verified":false}
	FileAccess.open("res://../evidence/godot-cell-scene-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print("BF_CELL_SCENE_TEST "+JSON.stringify(report))
	scene.queue_free()
	await process_frame
	quit(0 if report.passed else 1)
