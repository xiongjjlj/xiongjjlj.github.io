extends SceneTree
const C=preload("res://core/original_timekeeper.gd")
var differences:Array=[]
var mismatch_count:=0
func vector(value:Array)->Vector2:return Vector2(value[0],value[1])
func compare(a:Variant,b:Variant,path:String)->void:
	if a is Dictionary:
		for key in a:compare(a[key],b[key],path+"."+key)
		return
	if a is Array and b is Array and a.size()==b.size():
		for i in a.size():compare(a[i],b[i],path+"[%d]"%i)
		return
	if a is float:b=C.f32(b)
	if a!=b:
		mismatch_count+=1
		if differences.size()<20:differences.append({"field":path,"actual":a,"expected":b})
func cell_snapshot(grid:OriginalCellGrid)->Array:
	var result:Array=[]
	var indices:=grid.entries.keys()
	indices.sort_custom(func(a:Vector2i,b:Vector2i)->bool:return a.y<b.y or (a.y==b.y and a.x<b.x))
	for index in indices:
		var cell:Dictionary=grid.entries[index]
		if cell.type!=0 or cell.duration!=0 or cell.timestamp!=0 or cell.owner!=-1:
			result.append({"x":index.x,"y":index.y,"type":cell.type,"duration":cell.duration,"timestamp":cell.timestamp,"owner":cell.owner})
	return result
func _initialize()->void:
	var fixture:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/cell-native-grid.json"))
	var frames:=0
	for item in fixture.trails:
		var grid:=OriginalCellGrid.new()
		var trail:=OriginalDiscTrail.new()
		var disc:=OriginalDiscMotion.new()
		var owner:=OriginalActorMotion.new()
		disc.disc_powerups=int(item.initial.power)
		disc.is_mini_disc=item.initial.mini
		disc.movement=int(item.initial.movement)
		trail.burning=item.initial.burning
		trail.sliming=item.initial.sliming
		trail.fire_distance=C.f32(item.initial.fire_distance)
		trail.slime_distance=C.f32(item.initial.slime_distance)
		for i in item.frames.size():
			var frame:Dictionary=item.frames[i]
			var input:Dictionary=frame.input
			disc.position=vector(input.position)
			disc.source_z=C.f32(input.z)
			disc.velocity=vector(input.velocity)
			disc.fixed_delta=C.f32(input.fixed_delta)
			disc.owner_actor=owner if input.owner_present else null
			owner.position=vector(input.owner_position)
			owner.source_z=C.f32(input.owner_z)
			owner.velocity=vector(input.owner_velocity)
			owner.speed_limit=C.f32(input.owner_speed)
			grid.current_time=C.f32(input.time)
			trail.update_gate(disc,item.initial.dark)
			var amount:=vector(input.move_amount)
			disc.position+=amount*disc.fixed_delta
			trail.after_move(disc,amount,grid,0 if input.owner_present else -1)
			grid.frame_step(input.time)
			compare({"position":[disc.position.x,disc.position.y],"burning":trail.burning,"sliming":trail.sliming,"fire_distance":trail.fire_distance,"slime_distance":trail.slime_distance,"cells":cell_snapshot(grid)},frame.state,item.name+"[%d]"%i)
			grid.events.clear()
			frames+=1
	var report:Dictionary={"passed":mismatch_count==0,"cases":fixture.trails.size(),"frames":frames,"mismatch_count":mismatch_count,"differences":differences,"scope":"Original Disc gate blocks and trail method with injected base movement, time, audio, spawn predicates; full flight and world schedule are separate","full_game_parity_verified":false}
	FileAccess.open("res://../evidence/godot-native-cell-trail-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print("BF_CELL_TRAIL_TEST "+JSON.stringify(report))
	quit(0 if report.passed else 1)
