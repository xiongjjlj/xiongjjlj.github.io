extends SceneTree
var checks: Array = []
func check(name: String, passed: bool, details: Variant = null) -> void: checks.append({"name":name,"passed":passed,"details":details})
func _initialize() -> void: call_deferred("run")
func run() -> void:
	var game=load("res://main.tscn").instantiate();root.add_child(game);game.set_process(false)
	var behavior=game.level_behaviors
	var node:Node3D=game.nodes[269]
	var p0:=Vector2(-14.5,0.0);var p1:=Vector2(-14.5,-21.700000762939453)
	check("level3 sidecar preserves two MovingObstacle and four Switch behaviours",behavior.obstacles.size()==2 and behavior.switches.size()==4 and behavior.source_counts.get("WobblyObstacle",0)==25)
	check("moving obstacle starts at serialized position",is_equal_approx(node.position.x,p0.x) and is_equal_approx(node.position.y,p0.y))
	behavior.toggle(2316,"test")
	for i in 18: behavior.step(0.016666,[],[])
	check("serialized preMove delay and EaseOut motion move the first brush",node.position.y < p0.y and node.position.y > p1.y,{"y":node.position.y,"state":behavior.obstacles[2316].state,"elapsed":behavior.obstacles[2316].elapsed})
	var after_motion:=node.position.y
	for i in 36: behavior.step(0.016666,[],[])
	check("moveDur reaches the serialized target before spring pause",abs(node.position.y-p1.y)<0.05 and behavior.obstacles[2316].state==3,{"y":node.position.y,"state":behavior.obstacles[2316].state,"elapsed":behavior.obstacles[2316].elapsed,"percent":behavior.obstacles[2316].move_percent})
	for i in 75: behavior.step(0.016666,[],[])
	check("springPauseDur releases the spring wind-back",node.position.y < after_motion and behavior.obstacles[2316].springing_back,{"y":node.position.y,"state":behavior.obstacles[2316].state,"spring":behavior.obstacles[2316].springing_back})
	for i in 90: behavior.step(0.016666,[],[])
	check("springWindBackDur returns the brush to its original slot",abs(node.position.y-p0.y)<0.05 and behavior.obstacles[2316].state==0,{"y":node.position.y,"state":behavior.obstacles[2316].state,"spring":behavior.obstacles[2316].springing_back})
	behavior.reset();behavior.toggle(2316,"test");behavior.toggle(2316,"test")
	check("Toggle while pre-moving reverses without creating a second obstacle",behavior.obstacles.size()==2 and behavior.obstacles[2316].reverse)
	check("serialized WobblyObstacle records resolve spring targets",behavior.wobbles.size()==25 and behavior.wobbles.values().all(func(item): return item.target != null),{"wobbles":behavior.wobbles.size()})
	var wobble:Dictionary = behavior.wobbles[behavior.wobbles.keys()[0]]
	var wobble_angle := float(wobble.root.rotation.z)
	behavior._wobble_apply(wobble,Vector2(30.0,5.0))
	for i in 2: behavior.step(0.016666,[],[])
	check("WobblyObstacle spring force changes the rendered root",wobble.offset.length()>0.00001 and abs(float(wobble.root.rotation.z)-wobble_angle)>0.000001,{"offset":[wobble.offset.x,wobble.offset.y],"rotation":wobble.root.rotation.z})
	var destructable:Dictionary = {}
	for item in behavior.destructables.values():
		if bool(item.initial_visible): destructable=item; break
	var destroyed_before := false if destructable.is_empty() else bool(destructable.destroyed)
	if not destructable.is_empty(): behavior._destructable_hit(destructable,Vector2(24.0,2.0))
	var disabled_shape := false
	if not destructable.is_empty():
		for shape in game.collision_world.shapes:
			if int(shape.get("object_id",-1)) == int(destructable.game_object): disabled_shape = int(shape.layer)==0
	check("DestructableObstacle hides its collider and releases debris",not destructable.is_empty() and not destroyed_before and destructable.destroyed and not destructable.node.visible and disabled_shape,{"game_object":destructable.get("game_object",-1),"debris":destructable.get("debris",[]).size()})
	var report={"passed":checks.all(func(c):return c.passed),"checks":checks,"scope":"LevelSlideTraps serialized MovingObstacle/Switch sidecar and source timing state machine."}
	FileAccess.open("res://../evidence/godot-level3-behavior-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  "))
	print("BF_LEVEL3_BEHAVIOR_TEST "+JSON.stringify(report));game.queue_free();await process_frame;quit(0 if report.passed else 1)
