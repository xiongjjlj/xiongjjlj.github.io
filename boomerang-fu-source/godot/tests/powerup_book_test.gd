extends SceneTree
var checks: Array = []
func check(name: String, result: bool) -> void: checks.append({"name":name,"passed":result})
func _initialize() -> void: call_deferred("run")
func run() -> void:
	var scene = load("res://main.tscn").instantiate()
	root.add_child(scene)
	scene.set_process(false)
	var baseline := get_node_count()
	var world: OriginalPowerupWorld = scene.powerup_world
	for number in 4:
		world.begin_round(number+1,0.0,true)
		check("no book before source round threshold %d" % (number+1),not world.attempt_spawn())
	world.begin_round(5,0.0,true)
	var automatic := world.attempt_spawn()
	check("round-five source placement can find clear floor",automatic)
	if automatic:
		check("automatic location passes all nine floor samples and exclusion radii",world.spawn_point_clear(world.books[0].position))
		check("only one concurrent book",not world.attempt_spawn() and world.books.size()==1)
	world.begin_round(1,0.0,true)
	var id := world.spawn_book(Vector2(0,-3),OriginalPlayerPowers.Power.MOVE_FASTER)
	scene.consume_book_events()
	var view: OriginalPowerupPreview = scene.book_views[id]
	check("source book mesh and spawn tracks bind",view.missing_targets.is_empty() and view.bound_tracks==2)
	view.advance_book(0.4)
	view.advance_book(0.2)
	check("source idle motion and four material channels bind",view.clip.name=="PowerupGlow" and view.bound_tracks==6 and view.missing_targets.is_empty() and view.animated_materials.size()>0)
	check("spawn overrides serialized prefab root position",view.targets["Powerup__2066"].node.global_position==view.global_position and view.position==Vector3(0,-3,0))
	scene.actors[0].position=Vector2(0,-3)
	world.native_overlap_phase()
	check("enter alone does not collect a book",not world.books[0].collected)
	world.native_overlap_phase()
	scene.consume_book_events()
	check("pickup Stay collects once and applies source power",world.books[0].collected and scene.actors[0].move_faster and scene.weapons[0].powers.history==[2])
	check("original collection clip binds",view.collected and view.bound_tracks==1 and view.clip.name=="PowerupCollect")
	world.native_overlap_phase()
	check("disabled pickup cannot apply twice",scene.weapons[0].powers.history==[2])
	world.frame_step(0.2,0.2,false)
	check("collection waits until end of collection frame",world.books.size()==1)
	world.frame_step(0.1,0.3,false)
	check("book remains before .2-second delay",world.books.size()==1)
	world.frame_step(0.1,0.4,false)
	scene.consume_book_events()
	check("book is removed after source delay",world.books.is_empty() and scene.book_views.is_empty())
	await process_frame
	check("book lifecycle leaves no nodes",get_node_count()==baseline)
	# Pool selection excludes a full extra-disc inventory and owned powers.
	world.available=2|16|512
	world.build_pool()
	scene.weapons[0].held_discs=2
	check("useful pool selects shield when haste owned and hands full",world.select_power(scene.weapons[0])==16)
	world.available=65534 & ~4096
	world.build_pool()
	for power in [32,64,128,256,1024]:world.cycle_power(power)
	check("source cooldown queue reinserts oldest after four exclusions",world.removed.size()==4 and world.possible.has(32) and not world.possible.has(64))
	scene.reset_motions(true)
	await process_frame
	check("new match clears powers and books",scene.book_views.is_empty() and scene.weapons[0].powers.history.is_empty() and get_node_count()==baseline)
	var failed := checks.filter(func(x: Dictionary)->bool:return not x.passed)
	var report := {"passed":failed.is_empty(),"checks":checks,"failed":failed,"nodes_before":baseline,"nodes_after":get_node_count(),"native_spawn_rng_verified":false,"full_game_parity_verified":false}
	FileAccess.open("res://../evidence/godot-powerup-book-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print(JSON.stringify(report))
	scene.free()
	quit(0 if failed.is_empty() else 1)
