extends RefCounted
## GridGraph.UpdateArea for the original game's 2D, eight-neighbour grids.
const N=preload("res://core/original_navigation.gd")
const C=preload("res://core/original_timekeeper.gd")

static func vector(a:Array)->Vector3:return Vector3(a[0],a[1],a[2])

static func quadrants(queue:Array,bounds:Dictionary)->void:
	if bounds.is_empty():return
	while queue.size()>16:queue.pop_front()
	var center:=vector(bounds.center);var size:=vector(bounds.size)*0.5;size.z=0
	for offset:Vector3 in [Vector3(-1,-1,0),Vector3(-1,1,0),Vector3(1,1,0),Vector3(1,-1,0)]:
		var point:=center+size*0.5*offset;point.z=0
		queue.append({"center":[point.x,point.y,point.z],"size":[size.x,size.y,size.z]})

static func union_bounds(a:Dictionary,b:Dictionary)->Dictionary:
	var ca:=vector(a.center);var cb:=vector(b.center);var ea:=vector(a.size)*0.5;var eb:=vector(b.size)*0.5
	var low:=(ca-ea).min(cb-eb);var high:=(ca+ea).max(cb+eb);var center:=(low+high)*0.5;var size:=high-low
	return {"center":[center.x,center.y,center.z],"size":[size.x,size.y,size.z]}

static func grid_rect(low:Vector3,high:Vector3)->Vector4i:
	return Vector4i(N.round_even(C.f32(low.x-0.5)),N.round_even(C.f32(low.z-0.5)),N.round_even(C.f32(high.x-0.5)),N.round_even(C.f32(high.z-0.5)))

static func clipped(r:Vector4i,grid:OriginalNavigation)->Vector4i:
	return Vector4i(maxi(r.x,0),maxi(r.y,0),mini(r.z,grid.width-1),mini(r.w,grid.depth-1))

static func cells(r:Vector4i,grid:OriginalNavigation)->PackedInt32Array:
	r=clipped(r,grid)
	var result:=PackedInt32Array()
	for z in range(r.y,r.w+1):
		for x in range(r.x,r.z+1):result.append(z*grid.width+x)
	return result

static func apply(grid:OriginalNavigation,world:OriginalCollisionWorld,update:Dictionary)->Dictionary:
	var settings:=grid.physics_settings
	assert(not settings.is_empty() and int(settings.erosion)==0 and settings.neighbours=="Eight")
	var collision:Dictionary=settings.collision
	assert(collision.use2D)
	var center:=vector(update.bounds.center)
	var extent:=vector(update.bounds.size)*0.5
	var low:=Vector3(INF,INF,INF);var high:=-low
	for x in [-1,1]:
		for y in [-1,1]:
			for z in [-1,1]:
				var p:=N.transform_point(grid.inverse_matrix,center+extent*Vector3(x,y,z))
				low=low.min(p);high=high.max(p)
	# GraphTransform returns Bounds(center,size), whose min/max re-round.
	var local_center:=(low+high)*0.5;var local_extent:=(high-low)*0.5
	low=local_center-local_extent;high=local_center+local_extent
	var original:=grid_rect(low,high);var physics:=original;var affected:=original
	var recalculate:bool=update.get("physics",true)
	var walkability:int=update.get("walkability",-1)
	var tag:int=update.get("tag",-1)
	var will_change:=recalculate or walkability>=0
	if recalculate and walkability<0 and collision.collisionCheck:
		var padding:=Vector3(collision.diameter,0,collision.diameter)*0.5*C.f32(1.02)
		physics=grid_rect(low-padding,high+padding)
		affected=Vector4i(mini(original.x,physics.x),mini(original.y,physics.y),maxi(original.z,physics.z),maxi(original.w,physics.w))
	if will_change:affected+=Vector4i(-1,-1,1,1)
	var updated:=0
	if recalculate and walkability<0:
		for index in cells(physics,grid):
			var point:=N.transform_point(grid.matrix,Vector3(index%grid.width+0.5,0,index/grid.width+0.5))
			var integer:=Vector3(N.round_even(C.f32(point.x*1000)),N.round_even(C.f32(point.y*1000)),N.round_even(C.f32(point.z*1000)))
			grid.integer_positions[index]=integer;grid.positions[index]=integer*C.f32(0.001)
			if update.get("reset_penalty",true):grid.base_penalties[index]=int(settings.initial_penalty)
			var p:=Vector2(grid.positions[index].x,grid.positions[index].y)
			var valid:=true
			if collision.collisionCheck:
				valid=world.overlap_point(p,1<<16 if collision.floorCheck else 1<<30)
				var obstacle:bool=world.overlap_circle(p,C.f32(collision.diameter*grid.node_size*0.5),int(collision.mask.value)) if collision.type in ["Sphere","Capsule"] else world.overlap_point(p,int(collision.mask.value))
				valid=valid and not obstacle
			grid.walkable[index]=int(valid)
			grid.penalties[index]=grid.base_penalties[index]+(4000 if grid.tags[index]==1 else 0)
			updated+=1
	var minimum:=center-extent;var maximum:=center+extent
	for index in cells(original,grid):
		var p:Vector3=grid.positions[index]
		if p.x<minimum.x or p.y<minimum.y or p.z<minimum.z or p.x>maximum.x or p.y>maximum.y or p.z>maximum.z:continue
		if walkability>=0:grid.walkable[index]=walkability
		if tag>=0:grid.tags[index]=tag
		grid.base_penalties[index]+=int(update.get("add_penalty",0))
		grid.penalties[index]=grid.base_penalties[index]+(4000 if grid.tags[index]==1 else 0)
	if will_change:
		for index in cells(affected,grid):reconnect(grid,index,bool(settings.cut_corners))
		grid.areas_dirty=true
	return {"updated":updated,"physics_rect":physics,"affected_rect":affected}

static func reconnect(grid:OriginalNavigation,index:int,cut_corners:bool)->void:
	var bits:=0
	if grid.walkable[index]:
		for direction in 4:
			var next:int=index+grid.offsets[direction]
			if next>=0 and next<grid.walkable.size() and absi(index%grid.width-next%grid.width)<=1 and grid.walkable[next]:bits|=1<<direction
		for direction in 4:
			var a:bool=(bits&(1<<direction))!=0;var b:bool=(bits&(1<<((direction+1)%4)))!=0
			if (a or b) if cut_corners else (a and b):
				var next:int=index+grid.offsets[direction+4]
				if next>=0 and next<grid.walkable.size() and absi(index%grid.width-next%grid.width)<=1 and grid.walkable[next]:bits|=1<<(direction+4)
	grid.connections[index]=bits

