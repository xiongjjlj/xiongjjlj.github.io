class_name OriginalNavigation
extends RefCounted
## Source GridGraph/ABPath + 4-ary BinaryHeap + original path modifiers.
## One serialized source grid; OriginalNavigationWorld selects each map's
## static or platform grid. Input contains node data, never recorded paths.
const C=preload("res://core/original_timekeeper.gd")
var width:=0
var depth:=0
var node_size:=1.2
var matrix:Array=[]
var inverse_matrix:Array=[]
var auxiliary_graphs:Array=[]
var positions:=PackedVector3Array()
var integer_positions:=PackedVector3Array()
var walkable:=PackedByteArray()
var connections:=PackedByteArray()
var penalties:=PackedInt32Array()
var areas:=PackedInt32Array()
var offsets:=PackedInt32Array()
var costs:=PackedInt32Array()
var parent:=PackedInt32Array()
var g:=PackedInt32Array()
var h:=PackedInt32Array()
var edge_cost:=PackedInt32Array()
var seen:=PackedByteArray()
var heap_index:=PackedInt32Array()
var heap:=PackedInt32Array()
var heap_f:=PackedInt32Array()
var target:=Vector3.ZERO
var targets:Dictionary={}
var searches:=0
var heuristic_scale:=1.0
var graph_index:=0

func initialize(data:Dictionary)->void:
	graph_index=int(data.get("index",0))
	width=data.width
	depth=data.depth
	node_size=C.f32(data.node_size)
	matrix=data.matrix.map(func(value:float)->float:return C.f32(value))
	inverse_matrix=data.inverse_matrix.map(func(value:float)->float:return C.f32(value))
	auxiliary_graphs=data.auxiliary_graphs.duplicate(true)
	for graph in auxiliary_graphs:
		graph.matrix=graph.matrix.map(func(value:float)->float:return C.f32(value))
		graph.inverse_matrix=graph.inverse_matrix.map(func(value:float)->float:return C.f32(value))
		graph.positions=graph.positions.map(func(value:Array)->Vector3:return Vector3(value[0],value[1],value[2])*C.f32(0.001))
	offsets=PackedInt32Array(data.offsets)
	costs=PackedInt32Array(data.costs)
	var count:=width*depth
	positions.resize(count)
	integer_positions.resize(count)
	walkable.resize(count)
	connections.resize(count)
	penalties.resize(count)
	areas.resize(count)
	for row in data.nodes:
		var i:int=row[0]
		integer_positions[i]=Vector3(row[1],row[2],row[3])
		positions[i]=integer_positions[i]*C.f32(0.001)
		walkable[i]=row[4]
		penalties[i]=row[5]+(4000 if row[6]==1 else 0)
		areas[i]=row[7]
		connections[i]=row[8]
	parent.resize(count)
	parent.fill(-1)
	g.resize(count)
	h.resize(count)
	edge_cost.resize(count)
	seen.resize(count)
	heap_index.resize(count)

static func transform_point(m:Array,p:Vector3)->Vector3:
	var v:=Vector3.ZERO
	for row in 3:
		var i:=row*4
		# Mono retains expression precision until the float field assignment.
		v[row]=C.f32(m[i]*p.x+m[i+1]*p.y+m[i+2]*p.z+m[i+3])
	return v

static func squared(p:Vector3)->float:
	return C.f32(p.x*p.x+p.y*p.y+p.z*p.z)

static func magnitude(p:Vector3)->float:
	return C.f32(sqrt(p.x*p.x+p.y*p.y+p.z*p.z))

func geometric_nearest(point:Vector3)->Dictionary:
	var best:=nearest(point,-1,false)
	best.graph=graph_index
	var distance:=magnitude(best.point-point)
	# ABPath's unconstrained endpoint query checks all serialized graphs even
	# when the traversal mask selects static graph 0. This changes edge handling.
	for graph in auxiliary_graphs:
		var local:=transform_point(graph.inverse_matrix,point)
		var x:=clampi(int(local.x),0,int(graph.width)-1)
		var z:=clampi(int(local.z),0,int(graph.depth)-1)
		var index:int=z*int(graph.width)+x
		var y:=transform_point(graph.inverse_matrix,graph.positions[index]).y
		var clamped:=transform_point(graph.matrix,Vector3(clampf(local.x,x,x+1),y,clampf(local.z,z,z+1)))
		var d:=magnitude(clamped-point)
		# AstarPath.GetNearest visits graphs in index order and only replaces
		# on a smaller distance, including when this grid is not graph zero.
		if d<distance or (d==distance and int(graph.index)<int(best.graph)):
			distance=d
			best={"node":index,"point":clamped,"graph":graph.index}
	return best

static func round_even(v:float)->int:
	var lower:=floori(v)
	var fraction:=v-float(lower)
	return lower if fraction<0.5 or (fraction==0.5 and lower%2==0) else lower+1

func closest_point(index:int,point:Vector3)->Vector3:
	var local:=transform_point(inverse_matrix,point)
	var x:=index%width
	var z:=index/width
	var y:=transform_point(inverse_matrix,positions[index]).y
	return transform_point(matrix,Vector3(clampf(local.x,x,x+1),y,clampf(local.z,z,z+1)))

func _suitable(index:int,area:int)->bool:
	return walkable[index]!=0 and (area<0 or areas[index]==area)

func nearest(point:Vector3,area:=-1,constrained:=true,force:=false)->Dictionary:
	var local:=transform_point(inverse_matrix,point)
	var x:=clampi(int(local.x),0,width-1)
	var z:=clampi(int(local.z),0,depth-1)
	var index:=z*width+x
	if not force and (not constrained or _suitable(index,area)):
		var clamped:=closest_point(index,point)
		return {"node":index,"point":clamped} if not constrained or squared(clamped-point)<=10000.0 else {}
	var best:=-1
	var distance:=INF
	var overlap:=2
	if force and _suitable(index,area):
		best=index
		distance=squared(positions[index]-point)
		overlap-=1
	var limit:=INF if force else 100.0
	for radius in range(1,width+depth):
		if C.f32(node_size*radius)>limit:break
		var cells:Array[Vector2i]=[]
		for cx in range(x-radius,x+radius+1):cells.append(Vector2i(cx,z+radius))
		for cx in range(x-radius,x+radius+1):cells.append(Vector2i(cx,z-radius))
		for cz in range(z-radius+1,z+radius):cells.append(Vector2i(x-radius,cz))
		for cz in range(z-radius+1,z+radius):cells.append(Vector2i(x+radius,cz))
		var any_cell:=false
		for cell in cells:
			if cell.x<0 or cell.y<0 or cell.x>=width or cell.y>=depth:continue
			any_cell=true
			var candidate:=cell.y*width+cell.x
			if not _suitable(candidate,area):continue
			var d:=squared(positions[candidate]-point)
			if d<distance and d<limit*limit:
				distance=d
				best=candidate
		if best>=0:
			if overlap==0:break
			overlap-=1
		if not any_cell:break
	if best<0:return {}
	var clamped:=closest_point(best,point)
	return {"node":best,"point":clamped} if force or squared(clamped-point)<=10000.0 else {}

func actor_path(start:Vector3,finish:Vector3)->Dictionary:
	# LevelEnclosed has no moving-floor graph masks. GetBestGraphForTarget
	# uses GetNearestForce and a squared distance of 7 from the node center.
	var closest:=nearest(start,-1,true,true)
	if closest.is_empty() or squared(positions[closest.node]-start)>7.0:
		return {"error":"No eligible graph within the original seven-unit squared threshold"}
	return find_path(start,finish)

func _heuristic(index:int)->int:
	var d:=target-integer_positions[index]
	# Path.CalculateHScore converts the scaled costMagnitude to uint (truncation).
	return int(float(round_even(C.f32(sqrt(float(d.x)*d.x+float(d.y)*d.y+float(d.z)*d.z))))*heuristic_scale)

func _heap_add(node:int)->void:
	var index:=heap_index[node]
	if index<0:
		index=heap.size()
		heap.append(node)
		heap_f.append(0)
	var score:=g[node]+h[node]
	while index!=0:
		var above:=(index-1)/4
		if score>=heap_f[above] and (score!=heap_f[above] or g[node]<=g[heap[above]]):break
		heap[index]=heap[above]
		heap_f[index]=heap_f[above]
		heap_index[heap[index]]=index
		index=above
	heap[index]=node
	heap_f[index]=score
	heap_index[node]=index

func _heap_remove()->int:
	var result:=heap[0]
	heap_index[result]=-1
	var last:=heap[heap.size()-1]
	var last_f:=heap_f[heap.size()-1]
	heap.resize(heap.size()-1)
	heap_f.resize(heap_f.size()-1)
	if heap.is_empty():return result
	var index:=0
	while true:
		var previous:=index
		var best_f:=last_f
		var best_g:=g[last]
		for child in range(previous*4+1,mini(previous*4+5,heap.size())):
			if heap_f[child]<best_f or (heap_f[child]==best_f and g[heap[child]]<best_g):
				index=child
				best_f=heap_f[child]
				best_g=g[heap[child]]
		if index==previous:break
		heap[previous]=heap[index]
		heap_f[previous]=heap_f[index]
		heap_index[heap[previous]]=previous
	heap[index]=last
	heap_f[index]=last_f
	heap_index[last]=index
	return result

func _update_recursive(node:int)->void:
	g[node]=g[parent[node]]+edge_cost[node]+penalties[node]
	_heap_add(node)
	for direction in 8:
		if connections[node]&(1<<direction)==0:continue
		var child:=node+offsets[direction]
		if seen[child]!=0 and parent[child]==node:_update_recursive(child)

func _open(node:int)->void:
	for direction in 8:
		if connections[node]&(1<<direction)==0:continue
		var other:=node+offsets[direction]
		if walkable[other]==0:continue
		var next_g:=g[node]+costs[direction]+penalties[other]
		if seen[other]==0:
			parent[other]=node
			seen[other]=1
			edge_cost[other]=costs[direction]
			h[other]=_heuristic(other)
			g[other]=next_g
			_heap_add(other)
		elif next_g<g[other]:
			edge_cost[other]=costs[direction]
			parent[other]=node
			_update_recursive(other)

func find_path(start:Vector3,end:Vector3)->Dictionary:
	heuristic_scale=1.0
	var first:=nearest(start)
	if first.is_empty():return {"error":"Couldn't find a node close to the start point"}
	var last:=nearest(end,areas[first.node])
	if last.is_empty():return {"error":"Couldn't find a node close to the end point"}
	var start_node:int=first.node
	var end_node:int=last.node
	var start_point:Vector3=first.point
	var end_point:Vector3=last.point
	target=Vector3(round_even(C.f32(end_point.x*1000.0)),round_even(C.f32(end_point.y*1000.0)),round_even(C.f32(end_point.z*1000.0)))
	targets={end_node:true}
	var geometric:=geometric_nearest(end)
	var geometric_node:int=geometric.node
	var special_node:=-1
	if geometric.graph==graph_index and end_node!=geometric_node and absi(end_node%width-geometric_node%width)<=1 and absi(end_node/width-geometric_node/width)<=1:
		special_node=geometric_node
		target=integer_positions[geometric_node]
		targets.clear()
		for direction in 8:
			var other:=geometric_node+offsets[direction]
			if other>=0 and other<positions.size() and absi(other%width-geometric_node%width)<=1:targets[other]=true
		end_node=geometric_node
	heap.clear()
	heap_f.clear()
	heap_index.fill(-1)
	seen.fill(0)
	parent[start_node]=-1
	seen[start_node]=1
	g[start_node]=penalties[start_node]
	h[start_node]=_heuristic(start_node)
	var current:=start_node
	searches=0
	while not targets.has(current):
		_open(current)
		searches+=1
		if heap.is_empty():return {"error":"Searched whole area but could not find target"}
		if searches>1000000:return {"error":"Original node search limit exceeded"}
		current=_heap_remove()
	if end_node!=current:end_point=closest_point(current,end)
	end_node=current
	var node_ids:Array[int]=[]
	while current>=0:
		node_ids.append(current)
		current=parent[current]
	node_ids.reverse()
	var raw:Array[Vector3]=[]
	for node in node_ids:raw.append(positions[node])
	var snapped:=raw.duplicate()
	if snapped.size()==1:snapped.append(snapped[0])
	snapped[0]=start_point
	snapped[snapped.size()-1]=end_point
	var trimmed:=snapped.duplicate()
	var edge:=trimmed.size()>2 and connections[end_node]!=255
	if edge:trimmed.remove_at(trimmed.size()-1)
	var smoothed:=smooth_path(trimmed)
	var total:=0.0
	for i in range(1,smoothed.size()):total=C.f32(total+magnitude(smoothed[i]-smoothed[i-1]))
	return {"error":"","node_ids":node_ids,"start_node":start_node,"end_node":end_node,"clamped_start":start_point,"clamped_end":end_point,"h_target":target,"geometric_graph":geometric.graph,"geometric_node":geometric_node,"special_node":special_node,"raw":raw,"snapped":snapped,"trimmed":trimmed,"smoothed":smoothed,"edge":edge,"total_length":total}

func actor_flee_path(start:Vector3,avoid:Vector3,random:RefCounted)->Dictionary:
	var closest:=nearest(start,-1,true,true)
	if closest.is_empty() or squared(positions[closest.node]-start)>7.0:
		return {"error":"No eligible graph within the original seven-unit squared threshold"}
	return find_flee_path(start,avoid,random)

func find_flee_path(start:Vector3,avoid:Vector3,random:RefCounted,length:=15000,spread:=1000,strength:=0.75)->Dictionary:
	var first:=nearest(start)
	if first.is_empty():return {"error":"Couldn't find close nodes to the start point"}
	var start_node:int=first.node
	var start_point:Vector3=first.point
	var aim:=start-(avoid-start)*10.0
	heuristic_scale=C.f32(strength)
	target=Vector3(round_even(C.f32(aim.x*1000.0)),round_even(C.f32(aim.y*1000.0)),round_even(C.f32(aim.z*1000.0)))
	# Original PathHandler retains parents between requests. RandomPath's
	# nonpositive-length early return traces those parents before resetting one.
	heap.clear();heap_f.clear();heap_index.fill(-1);seen.fill(0)
	seen[start_node]=1
	g[start_node]=penalties[start_node]
	h[start_node]=_heuristic(start_node)
	searches=0
	var chosen:=start_node if length+spread<=0 else -1
	var greatest:=-1
	var greatest_score:=0
	var evaluated:=0
	if chosen<0:
		parent[start_node]=-1
		_open(start_node);searches+=1
		if heap.is_empty():return {"error":"No open points, the start node didn't open any nodes"}
		var current:=_heap_remove()
		while true:
			searches+=1
			if g[current]>=length:
				if g[current]>length+spread:
					if chosen<0:chosen=current
					break
				evaluated+=1
				if random.next_double()<=1.0/float(evaluated):chosen=current
			elif g[current]>greatest_score:
				greatest_score=g[current];greatest=current
			_open(current)
			if heap.is_empty():
				if chosen<0:chosen=greatest
				if chosen<0:return {"error":"Not a single node found to search"}
				break
			current=_heap_remove()
			if searches>1000000:return {"error":"Original node search limit exceeded"}
	var end_point:Vector3=positions[chosen]
	var node_ids:Array[int]=[]
	var cursor:=chosen
	while cursor>=0:
		node_ids.append(cursor);cursor=parent[cursor]
	node_ids.reverse()
	var raw:Array[Vector3]=[]
	for node in node_ids:raw.append(positions[node])
	var snapped:=raw.duplicate()
	if snapped.size()==1:snapped.append(snapped[0])
	snapped[0]=start_point;snapped[snapped.size()-1]=end_point
	var trimmed:=snapped.duplicate()
	var edge:=trimmed.size()>2 and connections[chosen]!=255
	if edge:trimmed.remove_at(trimmed.size()-1)
	var smoothed:=smooth_path(trimmed)
	var total:=0.0
	for i in range(1,smoothed.size()):total=C.f32(total+magnitude(smoothed[i]-smoothed[i-1]))
	return {"error":"","aim":aim,"node_ids":node_ids,"start_node":start_node,"end_node":chosen,"clamped_start":start_point,"clamped_end":end_point,"raw":raw,"snapped":snapped,"trimmed":trimmed,"smoothed":smoothed,"edge":edge,"total_length":total,"searched":searches}

static func smooth_path(points:Array)->Array[Vector3]:
	var result:Array[Vector3]=[]
	if points.size()<2:
		result.assign(points)
		return result
	var remainder:=0.0
	for i in range(points.size()-1):
		var a:Vector3=points[i]
		var b:Vector3=points[i+1]
		var distance:=magnitude(a-b)
		while remainder<distance:
			var t:=clampf(C.f32(remainder/distance),0.0,1.0)
			result.append(Vector3(C.lerp32(a.x,b.x,t),C.lerp32(a.y,b.y,t),C.lerp32(a.z,b.z,t)))
			remainder=C.f32(remainder+1.0)
		remainder=C.f32(remainder-distance)
	result.append(points[points.size()-1])
	var previous:=result[0]
	for i in range(1,result.size()-1):
		var old:=result[i]
		var middle:=(previous+result[i+1])/2.0
		result[i]=Vector3(C.lerp32(old.x,middle.x,0.5),C.lerp32(old.y,middle.y,0.5),C.lerp32(old.z,middle.z,0.5))
		previous=old
	return result
