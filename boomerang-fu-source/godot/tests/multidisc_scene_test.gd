extends SceneTree
var checks:Array=[]
func check(name:String,passed:bool)->void:checks.append({"name":name,"passed":passed})
func _initialize()->void:call_deferred("run")
func throw_forward(scene,twice:=false)->void:
	var weapon:OriginalPlayerWeapons=scene.weapons[0]
	weapon.actor.last_input_move=Vector2.UP
	weapon.display_position=weapon.actor.position
	weapon.display_rotation=PI
	weapon.try_throw()
	if twice:weapon.try_throw()
	scene.spawn_discs(0,weapon.actor.rotation_z)
func collect_all(scene)->void:
	for disc in scene.weapons[0].owned_discs.duplicate():
		disc.position=scene.actors[0].position
		disc.can_collide_with_owner=true
		scene.combat.trigger_pickup(0,disc)
func run()->void:
	var scene=load("res://main.tscn").instantiate()
	root.add_child(scene)
	scene.set_process(false)
	var baseline:=get_node_count()
	check("original normal-power pool includes enabled MultiDisc",scene.powerup_world.possible.has(64) and not scene.powerup_world.possible.has(4096))
	scene.actors[1].is_invulnerable=true
	scene.grant_preview_power(0,64)
	throw_forward(scene)
	var weapon:OriginalPlayerWeapons=scene.weapons[0]
	var parent:OriginalDiscMotion=weapon.owned_discs[0]
	check("throw captures the player's power flags",parent.disc_powerups==64 and not parent.is_mini_disc)
	weapon.powers.clear()
	for frame in 20:scene._process(1.0/60.0)
	check("disc does not split before original throw duration",parent.active and weapon.owned_discs.size()==1)
	for frame in 15:scene._process(1.0/60.0)
	check("in-flight power remains after inventory flag removal",not parent.active and weapon.owned_discs.size()==5)
	check("five live children clear the split flag",weapon.owned_discs.all(func(d):return d.is_mini_disc and d.disc_powerups==0) and scene.flying_discs.size()==5)
	check("native mini throw duration is applied",weapon.owned_discs.all(func(d):return d.throw_duration==OriginalTimekeeper.f32(0.3378)))
	var first:OriginalDiscMotion=weapon.owned_discs[0]
	first.position=weapon.actor.position
	first.can_collide_with_owner=true
	scene.combat.trigger_pickup(0,first)
	scene._process(1.0/60.0)
	check("one fragment leaves the player unarmed",weapon.held_discs==0 and weapon.mini_discs_collected==1 and weapon.ghost_discs==1)
	var right:String=scene.weapon_bindings.Avocado.rightDisc
	check("partial collection shows a ghost held disc",scene.characters[0].targets[right].node.visible and scene.characters[0].held_ghost_states.get("rightDisc",false))
	check("ghost disc still uses the source unarmed attack",weapon.try_attack() and weapon.actor.is_disarming and not weapon.actor.is_attacking)
	collect_all(scene)
	scene._process(1.0/60.0)
	check("five fragments restore one real disc",weapon.held_discs==1 and weapon.mini_discs_collected==0 and weapon.owned_discs.is_empty())
	check("complete pickup restores normal held material",not scene.characters[0].held_ghost_states.get("rightDisc",false) and scene.characters[0].held_normal_materials.is_empty())
	scene.reset_motions(true)
	scene.actors[1].is_invulnerable=true
	scene.grant_preview_power(0,64)
	scene.grant_preview_power(0,512)
	for frame in 5:scene._process(1.0/60.0)
	throw_forward(scene,true)
	for frame in 35:scene._process(1.0/60.0)
	check("Extra plus Multi creates two five-disc bursts",scene.weapons[0].owned_discs.size()==10)
	collect_all(scene)
	check("ten fragments restore both extra discs",scene.weapons[0].held_discs==2 and scene.weapons[0].mini_discs_collected==0)
	scene.reset_motions(true)
	scene.actors[1].is_invulnerable=true
	scene.grant_preview_power(0,64)
	throw_forward(scene)
	for frame in 35:scene._process(1.0/60.0)
	var children:Array=scene.weapons[0].owned_discs.duplicate()
	scene.combat.kill(0,"fall")
	for frame in 180:scene._process(1.0/60.0)
	check("dead owner's fragments vanish instead of becoming public discs",children.size()==5 and children.all(func(d):return not d.active and d.owner_actor==scene.actors[0]) and scene.weapons[0].owned_discs.is_empty())
	scene.reset_motions(true)
	await process_frame
	check("reset clears all disc views and ghost material caches",get_node_count()==baseline and scene.flying_discs.is_empty() and scene.characters.all(func(c):return c.held_normal_materials.is_empty()))
	var failed:=checks.filter(func(item):return not item.passed)
	var report:={"passed":failed.is_empty(),"checks":checks,"failed":failed,"nodes_before":baseline,"nodes_after":get_node_count(),"ghost_shader_and_full_frame_parity_verified":false}
	FileAccess.open("res://../evidence/godot-multidisc-scene-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print(JSON.stringify(report))
	scene.free()
	quit(0 if failed.is_empty() else 1)
