extends SceneTree
var checks:Array=[]
func check(name:String,passed:bool,details=null)->void:checks.append({"name":name,"passed":passed,"details":details})
func _initialize()->void:call_deferred("run")
func run()->void:
	var game=load("res://main.tscn").instantiate();root.add_child(game);game.set_process(false)
	var names:Array[String]=["Avocado","Watermelon"];var devices:Array[int]=[-1,-101]
	game.configure_match(names,devices,FfaSetup.Length.MEDIUM)
	var floors:OriginalFloorHazards=game.floor_hazards;var nav:OriginalNavigationWorld=game.decoys.navigation
	var key:=str(game.active_scene.file).get_file().get_basename()
	var source:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://assets/original/"+key+"/behaviors.json"))
	var expected:int=source.behaviours.filter(func(r):return r["class"] in ["TrapFloor","FallingFloor"] and r.active_in_hierarchy and r.data.m_Enabled).size()
	check("every enabled floor is bound",floors.floors.size()==expected,{"expected":expected,"bound":floors.floors.size()})
	var chosen:Dictionary={}
	for item in floors.floors.values():
		var p:Vector3=item.node.global_position
		if not item.model.trap or not game.ground_queries.solid_ground(Vector2(p.x,p.y),game.actors[0].radius*0.95):chosen=item;break
	if not chosen.is_empty():
		var model:OriginalFloorHazard=chosen.model;var before:Vector3=chosen.visual.global_position
		var point:=Vector2(chosen.node.global_position.x,chosen.node.global_position.y)
		if model.trap:
			game.actors[0].position=point;game.actors[0].is_dashing=true
			floors.fixed_contacts([game.actors[0]],game.ground_queries);floors.fixed_contacts([game.actors[0]],game.ground_queries)
			check("dashing does not trigger trap",model.state==0)
			game.actors[0].is_dashing=false;floors.fixed_contacts([game.actors[0]],game.ground_queries)
			check("stationary contact triggers warning",model.state==1)
		else:
			var count:=floors.playlist.size()
			for i in 40:floors.frame_step(1.0/60.0,true,2,0.016666);game.consume_floor_events();nav.flush_updates()
			check("level scheduler activates floors",floors.playlist.size()<count)
			for item in floors.floors.values():
				if item.model.state==1:chosen=item;model=item.model;point=Vector2(item.node.global_position.x,item.node.global_position.y);before=item.visual.global_position;break
		# Stop the playlist without stopping the active tile's animation.
		floors.playlist.clear()
		game.cell_grid.register_cell(OriginalCellGrid.CellType.FIRE,point,30,0)
		var fire_cell:Vector2i=game.cell_grid.coordinates(point)
		check("fire exists before floor sinks",game.cell_grid.cell_at(fire_cell).type==OriginalCellGrid.CellType.FIRE)
		var book_id:int=game.powerup_world.spawn_book(point)
		game.cell_preview.decals.append({"position":point})
		var lost_ground:=false;var moved:=false;var restored:=false;var maximum_queue:=0
		for i in 600:
			floors.frame_step(1.0/60.0,true,2,0.016666);game.consume_floor_events()
			maximum_queue=maxi(maximum_queue,nav.pending_updates.size());nav.flush_updates()
			if model.state==2:
				lost_ground=lost_ground or not game.ground_queries.point_on_layer(point,game.ground_queries.floor_mask)
				moved=moved or chosen.visual.global_position.distance_to(before)>0.2
			if i>300 and model.state==0:restored=true
		check("sinking removes floor collision",lost_ground)
		check("original renderer hierarchy moves",moved)
		check("falling ground removes overlapping powerup",game.powerup_world.books.all(func(b):return b.id!=book_id))
		check("falling ground removes decals",game.cell_preview.decals.is_empty())
		check("falling ground extinguishes fire",game.cell_grid.cell_at(fire_cell).type==OriginalCellGrid.CellType.NONE)
		check("floor changes rescan navigation",nav.updates_applied>0 and maximum_queue<512,{"updates":nav.updates_applied,"maximum_queue":maximum_queue})
		if model.trap:check("trap rises and restores collision",restored and game.ground_queries.point_on_layer(point,game.ground_queries.floor_mask))
		else:check("collapsed floor remains hidden",model.state==3 and not chosen.visual.visible)
		floors.reset();nav.reset_grids()
		check("round reset restores floor state and visuals",model.state==0 and chosen.visual.visible and chosen.visual.global_position.distance_to(model.start_position*Vector3(1,1,-1))<0.0001)
	else:check("only disabled source floors may be absent",expected==0)
	var report:={"passed":checks.all(func(c):return c.passed),"map":key,"checks":checks,"scope":"Real map hierarchy, floor triggers, collision holes, powerups, decals, rescans and round reset. Native callback ordering and pixels are separate."}
	FileAccess.open("res://../evidence/godot-floor-hazards-scene-"+key+"-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  "))
	print("BF_FLOOR_HAZARDS_SCENE "+JSON.stringify(report));game.queue_free();await process_frame;quit(0 if report.passed else 1)
