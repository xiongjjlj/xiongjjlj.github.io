extends SceneTree
var checks:Array=[]
var trace:Array=[]
var game:Node
var baseline_nodes:=0
const C=preload("res://core/original_timekeeper.gd")
func check(name:String,value:bool,detail:Variant=null)->void:
	checks.append({"name":name,"passed":value,"detail":detail})
func tick(count:=1)->void:
	for i in count:game._process(C.f32(0.016666))
func reset()->void:
	game.reset_motions(true)
	game.weapons[0].powers.start(32768)
func spawn()->Dictionary:
	game.input_states[0]=Vector2.RIGHT
	game.input_dash_states[0]=true
	tick()
	return game.decoys.members.values()[0] if not game.decoys.members.is_empty() else {}
func _initialize()->void:call_deferred("run")
func run()->void:
	game=load("res://main.tscn").instantiate()
	root.add_child(game)
	game.set_process(false)
	baseline_nodes=root.get_tree().get_node_count()
	reset()
	check("pickup alone does not spawn",game.decoys.members.is_empty())
	game.actors[0].is_stunned=true
	game.input_dash_states[0]=true
	tick()
	check("failed dash does not spawn",game.decoys.members.is_empty())
	reset()
	var item:=spawn()
	check("first accepted dash spawns once",game.decoys.members.size()==1 and game.decoy_views.size()==1)
	if item.is_empty():finish();return
	check("copies owner and at least one held disc",item.owner==0 and item.held==1 and item.powers==32768)
	check("decoy is outside player score roster",game.actors.size()==2 and game.round_state.scores.actors.size()==2)
	check("source collision mask is obstacle only",item.actor.collision_layer==256)
	var origin:Vector2=item.actor.position
	for i in 240:
		tick()
		if i%30==0:trace.append({"frame":i,"active":game.decoys.members.has(item.id),"position":[item.actor.position.x,item.actor.position.y],"state":item.ai.state,"path":item.ai.has_path,"waypoint":item.ai.current_waypoint,"dashing":item.actor.is_dashing})
	check("AI moves in the actual imported map",item.actor.position.distance_to(origin)>1.0)
	check("source navigational route requested",game.decoys.navigation.searches>0)
	game.actors[0].can_dash=true
	game.input_dash_states[0]=true
	tick()
	check("later dashes do not create another clone",game.decoys.serial==item.id)
	if game.decoys.members.has(item.id):
		item.actor.is_dashing=false
		item.actor.is_shielded=true
		item.actor.is_invulnerable=true
		check("clone melee damage ignores cosmetic shield and immunity",game.decoys.damage(item.id,"melee"))
		check("clone death leaves scores unchanged",game.round_state.scores.net_score(0)==0 and game.round_state.scores.net_score(1)==0)
	tick()
	check("dead clone removes view and query shape",not game.decoy_views.has(item.id) and game.collision_world.actor_shapes.size()==2)
	reset()
	item=spawn()
	if not item.is_empty():
		game.combat.kill(0,"outofbounds")
		game.consume_combat_events()
		tick(18)
		check("owner death respects realtime delay",game.decoys.members.has(item.id))
		tick(4)
		check("owner death removes clone after delay",not game.decoys.members.has(item.id))
	reset()
	item=spawn()
	if not item.is_empty():
		item.actor.is_dashing=false
		item.actor.is_shielded=true
		item.elements.toggle_fire(true)
		check("fire immediately removes a shielded clone",not game.decoys.members.has(item.id))
	reset()
	item=spawn()
	if not item.is_empty():
		item.actor.position=Vector2(0,0)
		item.actor.is_dashing=true
		item.actor.is_invulnerable=true
		game.combat.telefrag(game.actors[1],Vector2.ZERO,1.0)
		check("telefrag bypasses clone dash and immunity",not game.decoys.members.has(item.id))
	game.reset_motions(true)
	check("round reset reclaims clone physics and visuals",game.decoys.members.is_empty() and game.decoy_views.is_empty() and game.collision_world.actor_shapes.size()==2 and game.collision_world.ignored_actor_pairs.is_empty())
	await process_frame
	await process_frame
	check("scene nodes return to baseline",root.get_tree().get_node_count()==baseline_nodes,{"before":baseline_nodes,"after":root.get_tree().get_node_count()})
	finish()
func finish()->void:
	var passed:bool=checks.all(func(c):return c.passed)
	var report:Dictionary={"passed":passed,"checks":checks,"trace":trace,"scope":"Actual main scene, source assets and spatial queries; integration checks for spawn, movement, damage, owner delay, score isolation and cleanup. Not a native full-match frame comparison."}
	FileAccess.open("res://../evidence/godot-decoy-scene-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  "))
	print("BF_DECOY_SCENE "+JSON.stringify(report))
	game.queue_free()
	await process_frame
	quit(0 if passed else 1)
