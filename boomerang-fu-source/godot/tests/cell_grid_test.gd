extends SceneTree
const C=preload("res://core/original_timekeeper.gd")
var differences:Array=[]
var mismatch_count:=0
func vector(value:Array)->Vector2:return Vector2(value[0],value[1])
func compare(a:Variant,b:Variant,path:String)->void:
	if a is Dictionary:
		for key in a: compare(a[key],b[key],path+"."+key)
		return
	if a is Array and b is Array and a.size()==b.size():
		for i in a.size():compare(a[i],b[i],path+"[%d]"%i)
		return
	if a is float:b=C.f32(b)
	if a!=b:
		mismatch_count+=1
		if differences.size()<30:differences.append({"field":path,"actual":a,"expected":b})
func snapshot(grid:OriginalCellGrid,queries:Array)->Dictionary:
	var cells:Array=[]
	var indices:=grid.entries.keys()
	indices.sort_custom(func(a:Vector2i,b:Vector2i)->bool:return a.y<b.y or (a.y==b.y and a.x<b.x))
	for index in indices:
		var cell:Dictionary=grid.entries[index]
		if cell.type!=0 or cell.duration!=0 or cell.timestamp!=0 or cell.owner!=-1:
			cells.append({"x":index.x,"y":index.y,"type":cell.type,"duration":cell.duration,"timestamp":cell.timestamp,"owner":cell.owner})
	var results:Array=[]
	for query in queries:
		var point:=vector(query.position)
		results.append({"position":[point.x,point.y],"fire":grid.touching(point,1),"ice":grid.touching(point,2)})
	return {"cells":cells,"fire":grid.fire_count,"ice":grid.ice_count,"queries":results}
func _initialize()->void:
	var fixture:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/cell-native-grid.json"))
	var grid:=OriginalCellGrid.new()
	for i in fixture.coordinates.size():
		var item:Dictionary=fixture.coordinates[i]
		grid.grid_size=Vector2i(item.size[0],item.size[1])
		var index:=grid.coordinates(vector(item.position),item.force)
		var point:=grid.world_position(index)
		compare({"index":[index.x,index.y],"world":[point.x,point.y]},item,"coords[%d]"%i)
	for i in fixture.radii.size():
		var item:Dictionary=fixture.radii[i]
		grid.grid_size=Vector2i(item.size[0],item.size[1])
		var indices:=grid.within_radius(Vector2i(item.center[0],item.center[1]),int(item.radius))
		compare(indices.map(func(index):return [index.x,index.y]),item.coords,"radii[%d]"%i)
	grid.grid_size=Vector2i(100,100)
	for i in fixture.steps.size():
		var step:Dictionary=fixture.steps[i]
		var op:Dictionary=step.op
		if op.has("time"):grid.current_time=C.f32(op.time)
		if op.kind=="register":
			grid.fire_spawn_allowed=func(_point):return op.fire_allowed
			grid.ice_spawn_allowed=func(_point):return op.ice_allowed
			grid.register_cell(int(op.type),vector(op.position),op.duration,int(op.owner))
		elif op.kind=="area":
			grid.fire_spawn_allowed=Callable()
			grid.ice_spawn_allowed=Callable()
			grid.register_area(int(op.type),vector(op.position),op.duration,int(op.owner),int(op.radius))
		elif op.kind=="update":grid.frame_step(op.time)
		elif op.kind=="extinguish":grid.extinguish(vector(op.position),int(op.type),int(op.radius))
		elif op.kind=="cleanup":grid.clean_up()
		compare(snapshot(grid,step.state.queries),step.state,"steps[%d]"%i)
	var report:Dictionary={"passed":mismatch_count==0,"coordinates":fixture.coordinates.size(),"radii":fixture.radii.size(),"steps":fixture.steps.size(),"mismatch_count":mismatch_count,"differences":differences,"scope":fixture.scope,"full_game_parity_verified":false}
	FileAccess.open("res://../evidence/godot-native-cell-grid-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print("BF_CELL_GRID_TEST "+JSON.stringify(report))
	quit(0 if report.passed else 1)
