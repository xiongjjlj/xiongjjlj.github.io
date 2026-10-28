class_name OriginalExplosion
extends RefCounted
## Explosion.Init/RunTimers and its source ray fan. Weighted curve evaluation is
## measured against native AnimationCurve; tiny float differences are reported.
const C = preload("res://core/original_timekeeper.gd")
const V = preload("res://core/original_actor_motion.gd")
var parameters: Dictionary
var world: OriginalCollisionWorld
var corners := PackedVector2Array()
var position := Vector2.ZERO
var source_z := 0.0
var radius := C.f32(0.1)
var target_radius := 0.0
var started := 0.0
var active := true
var exploding := true
var damage_active := false
var mini := false
var segments := 48
var degrees := 7.5
var switch_platform := false
var last_update := 0.0
var vertices := PackedVector3Array()
var triangles := PackedInt32Array()
var color := Color.WHITE
var fire_spawned := false
var fire_points := PackedVector2Array()
var mesh_revision := 0
var owner := -1
var freeze_explosion := false

static func curve_value(keys: Array, time: float) -> float:
	time = C.f32(time)
	if time <= keys[0].time: return C.f32(keys[0].value)
	if time >= keys[-1].time: return C.f32(keys[-1].value)
	var index := 0
	while time >= keys[index+1].time: index += 1
	var a: Dictionary = keys[index]
	var b: Dictionary = keys[index+1]
	var duration := C.f32(b.time-a.time)
	var t := C.f32(C.f32(time-a.time)/duration)
	if (int(a.weightedMode)&2) != 0 or (int(b.weightedMode)&1) != 0:
		var w0 := float(a.outWeight) if (int(a.weightedMode)&2) != 0 else C.f32(1.0/3.0)
		var w1 := float(b.inWeight) if (int(b.weightedMode)&1) != 0 else C.f32(1.0/3.0)
		var lo := 0.0
		var hi := 1.0
		# Monotonic x handles: a bounded solve, independent of frame rate.
		for _iteration in 32:
			var v := (lo+hi)*0.5
			var x := 3.0*(1.0-v)*(1.0-v)*v*w0+3.0*(1.0-v)*v*v*(1.0-w1)+v*v*v
			if x < t: lo = v
			else: hi = v
		var v := (lo+hi)*0.5
		var u := 1.0-v
		var y1: float = a.value+a.outSlope*duration*w0
		var y2: float = b.value-b.inSlope*duration*w1
		return C.f32(u*u*u*a.value+3.0*u*u*v*y1+3.0*u*v*v*y2+v*v*v*b.value)
	return C.f32((2.0*t*t*t-3.0*t*t+1.0)*a.value+(t*t*t-2.0*t*t+t)*duration*a.outSlope+(-2.0*t*t*t+3.0*t*t)*b.value+(t*t*t-t*t)*duration*b.inSlope)

static func gradient_value(gradient: Dictionary, time: float) -> Color:
	var result: Array[float] = []
	time = C.f32(time)
	for channel in ["r","g","b","a"]:
		var alpha: bool = channel == "a"
		var count: int = int(gradient.m_NumAlphaKeys if alpha else gradient.m_NumColorKeys)
		var prefix := "atime" if alpha else "ctime"
		var index := 0
		while index < count-1 and time >= C.f32(float(gradient[prefix+str(index+1)])/65535.0): index += 1
		var a: float = gradient["key"+str(index)][channel]
		if index == count-1:
			result.append(a)
			continue
		var start := C.f32(float(gradient[prefix+str(index)])/65535.0)
		var end := C.f32(float(gradient[prefix+str(index+1)])/65535.0)
		var b: float = gradient["key"+str(index+1)][channel]
		result.append(C.lerp32(a,b,C.f32(C.f32(time-start)/C.f32(end-start))))
	return Color(result[0],result[1],result[2],result[3])

func initialize(data: Dictionary, origin: Vector2, z: float, is_mini: bool, current_time: float, queries: OriginalCollisionWorld, obstacle_corners: PackedVector2Array, use_switch := false) -> void:
	parameters = data
	position = origin
	source_z = C.f32(z)
	mini = is_mini
	started = C.f32(current_time)
	world = queries
	corners = obstacle_corners
	switch_platform = use_switch
	segments = (16 if use_switch else 32) if mini else (24 if use_switch else 48)
	degrees = C.f32(360.0/segments)
	target_radius = C.f32(data.radiusFinal * (0.5 if mini else 1.0))
	radius = C.f32(0.1)
	last_update = 0.0
	update_mesh(current_time)
	exploding = true
	active = true

func frame_step(current_time: float, unscaled_time: float) -> void:
	damage_active = false
	if not active: return
	var elapsed := C.f32(C.f32(current_time)-started)
	var normalized := C.f32(elapsed/C.f32(parameters.explosionDuration))
	color = gradient_value(parameters.gradient, normalized)
	if world.overlap_circle(position,0.0,256): exploding = false
	if exploding and elapsed < C.f32(parameters.explosionDuration):
		radius = C.f32(curve_value(parameters.radiusCurve.m_Curve, clampf(normalized,0.0,1.0))*target_radius)
		update_mesh(unscaled_time)
		damage_active = elapsed < C.f32(parameters.explosionDuration-parameters.fadeDuration)
	else:
		exploding = false
		active = false
	if exploding and bool(parameters.isFireExplosion) and elapsed >= C.f32(0.25) and not fire_spawned:
		fire_spawned = true
		var count := 40 if mini else 80
		var step := C.f32(360.0/count)
		var distance := C.f32(target_radius-1.0)
		for index in count:
			var direction := V.normalized(angle_vector(C.f32(step*index)))
			var hit := world.circle_cast(position,0.0,direction,distance,256)
			fire_points.append(position+direction*distance if hit.is_empty() else hit.point-direction)

static func angle_vector(angle: float) -> Vector2:
	var radians := C.f32(C.f32(angle)*C.f32(C.f32(PI)/C.f32(180.0)))
	return Vector2(C.f32(cos(radians)),C.f32(sin(radians)))

func update_mesh(unscaled_time: float) -> void:
	if switch_platform:
		if C.f32(C.f32(unscaled_time)-last_update) < C.f32(0.033): return
		last_update = C.f32(unscaled_time)
	if world.overlap_circle(position,0.0,256):
		exploding = false
		return
	var points: Array[Dictionary] = []
	for corner in corners:
		var direction := V.normalized(corner-position)
		var point := corner
		if V.magnitude(corner-position)>radius: point = position+direction*radius
		var hit := world.circle_cast(position,0.0,direction,V.magnitude(point-position),256)
		if not hit.is_empty(): point = hit.point
		points.append({"point":point-position,"angle":C.f32(C.f32(atan2(point.y-position.y,point.x-position.x))*C.f32(57.29578))})
	for index in segments:
		var direction := angle_vector(C.f32(degrees*index))
		var hit := world.circle_cast(position,0.0,direction,radius,256)
		var point: Vector2 = position+direction*radius if hit.is_empty() else hit.point
		points.append({"point":point-position,"angle":C.f32(C.f32(atan2(point.y-position.y,point.x-position.x))*C.f32(57.29578))})
	points.sort_custom(func(a:Dictionary,b:Dictionary)->bool:return a.angle>b.angle)
	vertices = PackedVector3Array([Vector3.ZERO])
	for entry in points: vertices.append(Vector3(entry.point.x,entry.point.y,0))
	triangles.resize(maxi(999,points.size()*3))
	triangles.fill(0)
	for index in points.size():
		triangles[index*3+1] = index+1
		triangles[index*3+2] = 1 if index==points.size()-1 else index+2
	# Source GenerateMesh clears from the final triangle's second index.
	triangles[(points.size()-1)*3+1] = 0
	triangles[(points.size()-1)*3+2] = 0
	mesh_revision += 1

func reaches(point: Vector2, point_z: float, collider_radius: float) -> bool:
	if not damage_active: return false
	var shape := {"kind":"circle","center":point,"radius":collider_radius}
	if OriginalCollisionWorld.penetration(position,radius,shape,true).is_empty(): return false
	var delta := Vector3(point.x-position.x,point.y-position.y,point_z-source_z)
	var distance := V.magnitude_xyz(Vector2(delta.x,delta.y),delta.z)
	return world.circle_cast(position,0.0,Vector2(delta.x,delta.y),distance,256).is_empty()
