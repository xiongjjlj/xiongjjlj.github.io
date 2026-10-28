class_name OriginalNavigationWorld
extends OriginalNavigation
## Per-level grids and LevelManager.GetBestGraphForTarget selection.
## Grid coordinates remain fixed; moving-floor GraphTransform maps queries
## into the original graph and maps retained waypoints back each frame.
var grids: Dictionary = {}
var moving_floors: WeakRef
var legacy := false

static func decode_map(data: Dictionary) -> Dictionary:
	if data.get("encoding","") != "gzip-base64": return data
	var size := int(data.uncompressed_bytes)
	if size <= 0 or size > 16 * 1024 * 1024:
		push_error("Invalid navigation input size"); return {}
	var bytes := Marshalls.base64_to_raw(data.data).decompress_dynamic(size,FileAccess.COMPRESSION_GZIP)
	var hash := HashingContext.new(); hash.start(HashingContext.HASH_SHA256); hash.update(bytes)
	if bytes.size() != size or hash.finish().hex_encode() != str(data.uncompressed_sha256):
		push_error("Navigation input checksum mismatch"); return {}
	var decoded = JSON.parse_string(bytes.get_string_from_utf8())
	return decoded if decoded is Dictionary else {}

func initialize(data: Dictionary) -> void:
	data = decode_map(data)
	grids.clear()
	legacy = not data.has("graphs")
	if legacy:
		super.initialize(data); return
	for entry: Dictionary in data.graphs:
		var input := entry.duplicate(false)
		var others: Array = []
		for other: Dictionary in data.graphs:
			if int(other.index) == int(entry.index): continue
			others.append({"index":int(other.index),"width":int(other.width),"depth":int(other.depth),"matrix":other.matrix,"inverse_matrix":other.inverse_matrix,"positions":other.nodes.map(func(row):return [row[1],row[2],row[3]])})
		input.auxiliary_graphs = others
		var grid := OriginalNavigation.new(); grid.initialize(input)
		grids[int(entry.index)] = grid

func bind_moving_floors(source: OriginalMovingFloors) -> void:
	moving_floors = weakref(source)

func floor_for_graph(index: int) -> Dictionary:
	if index == 0 or moving_floors == null or moving_floors.get_ref() == null: return {}
	for floor: Dictionary in moving_floors.get_ref().floors.values():
		if int(floor.graph_mask) == (1 << index): return floor
	return {}

func best_graph(start: Vector3, finish: Vector3) -> Dictionary:
	var best: Dictionary = {}; var distance := INF
	for index: int in grids:
		var floor := floor_for_graph(index)
		if index > 0 and floor.is_empty(): continue
		var point := OriginalMovingFloors.path_point(floor,start,true)
		var target_point := OriginalMovingFloors.path_point(floor,finish,true)
		var grid: OriginalNavigation = grids[index]
		var nearest := grid.nearest(point,-1,true,true)
		if nearest.is_empty(): continue
		var node_point: Vector3 = grid.positions[nearest.node]
		if squared(node_point-point) > 7.0: continue
		var score := squared(node_point-target_point)
		if score < distance:
			distance = score
			best = {"index":index,"floor":floor,"start":point,"finish":target_point}
	return best

func actor_path(start: Vector3, finish: Vector3) -> Dictionary:
	if legacy: return super.actor_path(start,finish)
	var selected := best_graph(start,finish)
	if selected.is_empty(): return {"error":"No eligible graph within the original seven-unit squared threshold"}
	var result: Dictionary = grids[selected.index].find_path(selected.start,selected.finish)
	result.moving_floor = selected.floor; result.graph_index = selected.index
	return result

func actor_flee_path(start: Vector3, avoid: Vector3, random: RefCounted) -> Dictionary:
	if legacy: return super.actor_flee_path(start,avoid,random)
	var selected := best_graph(start,avoid)
	if selected.is_empty(): return {"error":"No eligible graph within the original seven-unit squared threshold"}
	var result: Dictionary = grids[selected.index].find_flee_path(selected.start,selected.finish,random)
	result.moving_floor = selected.floor; result.graph_index = selected.index
	return result
