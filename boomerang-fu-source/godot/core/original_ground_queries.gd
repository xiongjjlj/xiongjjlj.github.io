class_name OriginalGroundQueries
extends RefCounted
## Recovered nine-point ground predicates. Circle/box point boundaries are
## native-tested; overlapping trap-floor selection order remains unverified.
const C = preload("res://core/original_timekeeper.gd")
const A = preload("res://core/original_actor_motion.gd")
var world: OriginalCollisionWorld
var floor_mask := 1073807360
var water_mask := 0
var cloud_level := false

func point_on_layer(point: Vector2, mask: int, exclude_traps := false) -> bool:
	for shape in world.shapes:
		if shape.layer & mask == 0: continue
		if exclude_traps and shape.get("tag","") == "TrapFloor": continue
		if shape.kind == "circle":
			if point.distance_squared_to(shape.center) <= shape.radius * shape.radius: return true
		elif shape.kind == "polygon" and Geometry2D.is_point_in_polygon(point,shape.vertices): return true
	return false

func contact_points(point: Vector2, radius: float) -> Array[Vector2]:
	var offsets: Array[Vector2] = [Vector2.ZERO,Vector2(0,1.25),A.normalized(Vector2(1,1)),Vector2.RIGHT,A.normalized(Vector2(1,-1)),Vector2(0,-1),A.normalized(Vector2(-1,-1)),Vector2.LEFT,A.normalized(Vector2(-1,1))]
	var result: Array[Vector2] = []
	for offset in offsets: result.append(point + offset * C.f32(radius))
	return result

func hazard_contacts(point: Vector2, radius: float, include_obstacles := true) -> int:
	var points := contact_points(point,radius)
	var count := 0
	var center_missing := false
	for i in points.size():
		if not point_on_layer(points[i],floor_mask):
			count += 1
			if i == 0: center_missing = true
		if include_obstacles and center_missing and i > 0 and point_on_layer(points[i],256): count += 1
	return count

func solid_ground(point: Vector2, radius: float) -> bool:
	for sample in contact_points(point,radius):
		if point_on_layer(sample,floor_mask,true): return true
	return false

func over_hazard(point: Vector2, radius: float) -> bool:
	return hazard_contacts(point,radius) > 8

func off_cliff(point: Vector2) -> bool:
	return cloud_level and not point_on_layer(point,water_mask)
