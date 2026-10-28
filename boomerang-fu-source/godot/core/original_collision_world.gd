class_name OriginalCollisionWorld
extends RefCounted
## Continuous circle queries against the original 2D collider geometry.
## Query semantics are isolated for native Unity comparison; numerical parity
## with Unity's Box2D implementation has not yet been established.
var shapes: Array[Dictionary] = []
var source_scene: Dictionary = {}
var source_nodes: Dictionary = {}
var source_components: Dictionary = {}
var actor_shapes: Array[Dictionary] = []
var next_actor_id:=1000000
var ignored_actor_pairs:Dictionary={}
var queries_start_in_colliders := false
const OVERLAP_EPSILON := 1.1920928955078125e-6

func load_scene(scene: Dictionary, scene_nodes: Dictionary) -> void:
	shapes.clear()
	source_scene = scene
	source_nodes = scene_nodes
	source_components.clear()
	actor_shapes.clear()
	for object in scene.nodes:
		source_components[int(object.id)] = object.components
		var node: Node3D = scene_nodes[int(object.id)]
		if not node.is_visible_in_tree(): continue
		for component in object.components:
			if not component.type in ["CircleCollider2D", "BoxCollider2D", "PolygonCollider2D", "EdgeCollider2D"]: continue
			var data: Dictionary = component.data
			if not data.m_Enabled: continue
			var record := {"id": int(component.id), "object_id": int(object.id), "name": object.name,
				"layer": 1 << int(object.layer), "base_layer": 1 << int(object.layer), "trigger": data.m_IsTrigger, "actor": false, "path_index": 0}
			var offset := Vector2(data.m_Offset.x, data.m_Offset.y)
			if component.type == "CircleCollider2D":
				record.kind = "circle"
				record.center = point(node, offset)
				record.radius = data.m_Radius * maxf(node.global_basis.x.length(), node.global_basis.y.length())
				record.bounds = Rect2(record.center - Vector2.ONE * record.radius, Vector2.ONE * record.radius * 2.0)
				shapes.append(record)
			else:
				var paths: Array = []
				if component.type == "BoxCollider2D":
					var half := Vector2(data.m_Size.x, data.m_Size.y) * 0.5
					paths.append([Vector2(-half.x, -half.y), Vector2(half.x, -half.y), half, Vector2(-half.x, half.y)])
				elif component.type == "PolygonCollider2D":
					for path in data.m_Points.m_Paths:
						var points: Array = []
						for p in path: points.append(Vector2(p.x, p.y))
						paths.append(points)
				else:
					var points: Array = []
					for p in data.m_Points: points.append(Vector2(p.x, p.y))
					paths.append(points)
				for path_index in paths.size():
					var path = paths[path_index]
					var item := record.duplicate()
					item.path_index = path_index
					item.kind = "edge" if component.type == "EdgeCollider2D" else "polygon"
					item.edge_radius = data.get("m_EdgeRadius", 0.0) * maxf(node.global_basis.x.length(), node.global_basis.y.length())
					var vertices := PackedVector2Array()
					for p in path: vertices.append(point(node, p + offset))
					item.vertices = vertices
					var bounds := Rect2(vertices[0], Vector2.ZERO)
					for p in vertices: bounds = bounds.expand(p)
					item.bounds = bounds.grow(item.edge_radius)
					shapes.append(item)

static func point(node: Node3D, p: Vector2) -> Vector2:
	var world := node.global_transform * Vector3(p.x, p.y, 0)
	return Vector2(world.x, world.y)

func sync_scene_object(object_id: int) -> void:
	if not source_nodes.has(object_id): return
	var node: Node3D = source_nodes[object_id]
	var components: Array = source_components.get(object_id, [])
	for shape_index in shapes.size():
		var shape: Dictionary = shapes[shape_index]
		if shape.actor or int(shape.object_id) != object_id: continue
		var component: Dictionary = {}
		for candidate in components:
			if int(candidate.id) == int(shape.id): component = candidate; break
		if component.is_empty(): continue
		var data: Dictionary = component.data
		var offset := Vector2(data.m_Offset.x, data.m_Offset.y)
		if shape.kind == "circle":
			shape.center = point(node, offset)
			shape.radius = data.m_Radius * maxf(node.global_basis.x.length(), node.global_basis.y.length())
			shape.bounds = Rect2(shape.center - Vector2.ONE * shape.radius, Vector2.ONE * shape.radius * 2.0)
		else:
			var path: Array = []
			if component.type == "BoxCollider2D":
				var half := Vector2(data.m_Size.x, data.m_Size.y) * 0.5
				path = [Vector2(-half.x, -half.y), Vector2(half.x, -half.y), half, Vector2(-half.x, half.y)]
			elif component.type == "PolygonCollider2D":
				path = data.m_Points.m_Paths[int(shape.get("path_index", 0))]
			else:
				path = data.m_Points
			var vertices := PackedVector2Array()
			for p in path:
				var point_value: Vector2 = Vector2.ZERO
				if p is Dictionary: point_value = Vector2(p.x, p.y)
				else: point_value = p
				vertices.append(point(node, point_value + offset))
			shape.vertices = vertices
			if not vertices.is_empty():
				var bounds := Rect2(vertices[0], Vector2.ZERO)
				for p in vertices: bounds = bounds.expand(p)
				shape.bounds = bounds.grow(shape.get("edge_radius", 0.0))
		shapes[shape_index] = shape

func set_scene_object_enabled(object_id: int, enabled: bool) -> void:
	# Unity disables a destroyed obstacle collider with its GameObject. Keep the
	# source layer so reset can restore the exact query mask.
	if source_nodes.has(object_id):
		var node: Node3D = source_nodes[object_id]
		node.visible = enabled
	for shape in shapes:
		if shape.actor or int(shape.object_id) != object_id: continue
		shape.layer = int(shape.get("base_layer", shape.layer)) if enabled else 0

func register_actor(actor: OriginalActorMotion) -> void:
	actor.query_id = next_actor_id
	next_actor_id+=1
	var shape := {"id": actor.query_id, "kind": "circle", "radius": actor.radius,
		"layer": 512, "actor": true, "actor_ref": weakref(actor), "trigger": false}
	actor_shapes.append(shape)
	shapes.append(shape)
	sync_actors()

func clear_actors() -> void:
	shapes = shapes.filter(func(shape): return not shape.actor)
	actor_shapes.clear()
	ignored_actor_pairs.clear()
	next_actor_id=1000000

func ignore_actor_pair(a:OriginalActorMotion,b:OriginalActorMotion)->void:
	ignored_actor_pairs[Vector2i(a.query_id,b.query_id)]=true
	ignored_actor_pairs[Vector2i(b.query_id,a.query_id)]=true

func unregister_actor(actor:OriginalActorMotion)->void:
	actor_shapes=actor_shapes.filter(func(s):return s.id!=actor.query_id)
	shapes=shapes.filter(func(s):return s.id!=actor.query_id)
	for pair in ignored_actor_pairs.keys():
		if pair.x==actor.query_id or pair.y==actor.query_id:ignored_actor_pairs.erase(pair)

func sync_actors() -> void:
	for shape in actor_shapes:
		var actor: OriginalActorMotion = shape.actor_ref.get_ref()
		shape.layer = 524288 if actor.state == OriginalActorMotion.State.DEAD else 512
		if not actor.physics_enabled: shape.layer = 0
		shape.center = actor.position
		shape.bounds = Rect2(actor.position - Vector2.ONE * shape.radius, Vector2.ONE * shape.radius * 2.0)

func circle_cast(origin: Vector2, radius: float, direction: Vector2, distance: float, mask: int, exclude := -1) -> Dictionary:
	if distance <= 0.0 or direction == Vector2.ZERO: return {}
	# Verified in the original Windows player: Physics2D normalizes non-unit
	# directions, including 1e-7 vectors below Vector2.Normalize's threshold.
	direction = direction.normalized()
	sync_actors()
	var end := origin + direction * distance
	var sweep := Rect2(origin, Vector2.ZERO).expand(end).grow(radius)
	var best: Dictionary = {}
	for shape in shapes:
		if shape.id == exclude or shape.layer & mask == 0 or not sweep.intersects(shape.bounds, true): continue
		# Physics2D.IgnoreCollision does not exclude explicit scene queries.
		var overlap := penetration(origin, radius, shape)
		var ray_on_polygon: bool = radius == 0.0 and shape.kind == "polygon" and Geometry2D.is_point_in_polygon(origin,shape.vertices)
		if (not overlap.is_empty() and overlap.depth > 0.0) or ray_on_polygon:
			if queries_start_in_colliders:
				return {"distance": 0.0, "centroid": origin, "point": origin, "normal": -direction, "shape": shape, "actor": shape.actor}
			continue
		var hit: Dictionary
		if shape.kind == "circle":
			hit = cast_point_circle(origin, direction, distance, shape.center, radius + shape.radius)
		else:
			hit = cast_polygon(origin, direction, distance, radius + shape.edge_radius, shape.vertices, shape.kind == "polygon")
		if not hit.is_empty() and (best.is_empty() or hit.distance < best.distance):
			best = hit
			best.shape = shape
			best.actor = shape.actor
			best.centroid = origin + direction * hit.distance
			best.point = best.centroid - hit.normal * radius
	return best

static func cast_point_circle(origin: Vector2, direction: Vector2, distance: float, center: Vector2, radius: float) -> Dictionary:
	var relative := origin - center
	var b := relative.dot(direction)
	var c := relative.length_squared() - radius * radius
	if b > 0.0 or c < 0.0: return {}
	var discriminant := b * b - c
	if discriminant < 0.0: return {}
	var time := -b - sqrt(discriminant)
	if time < 0.0 or time > distance: return {}
	return {"distance": time, "normal": (origin + direction * time - center).normalized()}

static func cast_polygon(origin: Vector2, direction: Vector2, distance: float, radius: float, vertices: PackedVector2Array, closed := true) -> Dictionary:
	var best: Dictionary = {}
	var edge_count := vertices.size() if closed else vertices.size() - 1
	for index in edge_count:
		var a := vertices[index]
		var b := vertices[(index + 1) % vertices.size()]
		var edge := b - a
		var length := edge.length()
		if length <= 0.0: continue
		var tangent := edge / length
		var normal := Vector2(-tangent.y, tangent.x)
		for side in [-1.0, 1.0]:
			var n: Vector2 = normal * side
			var rate := direction.dot(n)
			if rate >= 0.0: continue
			var time := (radius - (origin - a).dot(n)) / rate
			if time < 0.0 or time > distance: continue
			var along := (origin + direction * time - a).dot(tangent)
			if along < 0.0 or along > length: continue
			if best.is_empty() or time < best.distance: best = {"distance": time, "normal": n}
		var corner := cast_point_circle(origin, direction, distance, a, radius)
		if not corner.is_empty() and (best.is_empty() or corner.distance < best.distance): best = corner
	if not closed:
		var corner := cast_point_circle(origin, direction, distance, vertices[-1], radius)
		if not corner.is_empty() and (best.is_empty() or corner.distance < best.distance): best = corner
	return best

static func penetration(origin: Vector2, radius: float, shape: Dictionary, query_contact := false) -> Dictionary:
	if shape.kind == "circle":
		var diff: Vector2 = origin - shape.center
		var depth: float = radius + shape.radius - diff.length()
		if query_contact: depth = OriginalTimekeeper.f32(OriginalTimekeeper.f32(radius + shape.radius) - diff.length())
		return {"depth": depth, "normal": diff.normalized()} if depth > 0.0 or (query_contact and depth > -OVERLAP_EPSILON) else {}
	var vertices: PackedVector2Array = shape.vertices
	var closest := Vector2.ZERO
	var squared := INF
	var edge_count := vertices.size() if shape.kind == "polygon" else vertices.size() - 1
	for index in edge_count:
		var point_on_edge := Geometry2D.get_closest_point_to_segment(origin, vertices[index], vertices[(index + 1) % vertices.size()])
		var d := origin.distance_squared_to(point_on_edge)
		if d < squared:
			squared = d
			closest = point_on_edge
	var inside: bool = shape.kind == "polygon" and Geometry2D.is_point_in_polygon(origin, vertices)
	var signed_distance := -sqrt(squared) if inside else sqrt(squared)
	if query_contact: signed_distance = OriginalTimekeeper.f32(signed_distance)
	var depth: float = radius + shape.get("edge_radius", 0.0) - signed_distance
	if query_contact:
		# Original UnityPlayer default polygons retain a 0.01 shape skin for
		# circle overlap, unlike point containment. Native boundary fixture.
		var skin: float = maxf(shape.get("edge_radius",0.0),OriginalTimekeeper.f32(0.01))
		depth = OriginalTimekeeper.f32(OriginalTimekeeper.f32(radius + skin) - signed_distance)
	if depth <= 0.0 and not (query_contact and depth > -OVERLAP_EPSILON): return {}
	var normal := (closest - origin).normalized() if inside else (origin - closest).normalized()
	return {"depth": depth, "normal": normal}

func overlap_circle(origin: Vector2, radius: float, mask: int) -> bool:
	# Native near-contact detection is distinct from positive-depth resolution.
	for shape in shapes:
		if shape.layer & mask and not penetration(origin,radius,shape,true).is_empty(): return true
	return false

func overlaps(origin: Vector2, radius: float, mask: int, exclude := -1) -> Array[Dictionary]:
	var result: Array[Dictionary] = []
	for shape in shapes:
		if shape.id == exclude or shape.layer & mask == 0: continue
		var hit := penetration(origin, radius, shape)
		if hit.is_empty(): continue
		hit.shape = shape
		result.append(hit)
		if result.size() == 16: break # Original nonalloc query buffer length.
	return result

func separate_from_obstacles(origin: Vector2, radius: float, mask: int, depth := 0) -> Vector2:
	if depth > 5: return origin
	for overlap in overlaps(origin, radius - 0.01, mask):
		var actual := penetration(origin, radius, overlap.shape)
		var push: Vector2 = actual.normal * actual.depth
		var hit := circle_cast(origin, radius - 0.01, push.normalized(), push.length(), mask)
		if not hit.is_empty(): push -= hit.normal * push.dot(hit.normal)
		return separate_from_obstacles(origin + push, radius, mask, depth + 1)
	return origin
