class_name OriginalCameraRig
extends RefCounted
## Source Camera3D framing in Unity coordinates (negative Z above the floor).
## Native noise, impact shake, kill zoom and matrix parity remain separate work.
const C = preload("res://core/original_timekeeper.gd")
var aspect := C.f32(16.0 / 9.0)
var fov := 16.0
var minimum_zoom := 75.0
var maximum_zoom := 350.0
var current_zoom := 0.0
var target_zoom := 0.0
var camera_angle := Vector3(0, C.f32(-0.425), -0.625)
var zoom_y_offset := C.f32(0.008)
var focus := Vector3.ZERO
var position := Vector3.ZERO
var aim_position := Vector3.ZERO
var targets: Array[Dictionary] = []
var level_size := Vector3(43,43,0)
var tracked: Dictionary = {}
var frame_delta := C.f32(1.0 / 60.0)
var transitions: Array[Dictionary] = []
var transition_serial := 0

static func vlerp(a: Vector3, b: Vector3, t: float) -> Vector3:
	return Vector3(C.lerp32(a.x,b.x,t),C.lerp32(a.y,b.y,t),C.lerp32(a.z,b.z,t))

func initialize(ratio: float) -> void:
	aspect = C.f32(ratio)
	maximum_zoom = zoom_from_box(level_size.x + 15.0,level_size.y + 15.0)
	current_zoom = maximum_zoom
	target_zoom = 0.0
	focus = Vector3.ZERO
	aim_position = focus + camera_angle * current_zoom
	position = aim_position + Vector3(0,C.f32(-current_zoom * zoom_y_offset),0)
	targets.clear()
	tracked.clear()
	transitions.clear()

func add_target(id: String, point: Vector3, collider_size: Vector3, influence: float, duration: float) -> void:
	var target := {"position":Vector3.ZERO,"last":Vector3.ZERO,"live":point,"size":collider_size * C.f32(influence),"influence":C.f32(influence),"transitioning":duration>0,"detached":false}
	tracked[id] = target
	if duration>0: start_transition(id,target,"add",duration)

func set_target_position(id: String, point: Vector3) -> void:
	if tracked.has(id): tracked[id].live = point

func remove_target(id: String, point: Vector3, duration := 1.5) -> void:
	if not tracked.has(id): return
	if duration <= 0:
		tracked.erase(id)
		return
	var target: Dictionary = tracked[id]
	target.last = point
	target.live = point
	target.detached = true
	target.transitioning = true
	start_transition(id,target,"remove",duration)

func start_transition(id: String, target: Dictionary, kind: String, duration: float) -> void:
	transition_serial += 1
	var task := {"id":id,"target":target,"kind":kind,"timer":0.0,"duration":C.f32(duration),"start":Vector3(focus.x,focus.y,0),"fresh":true,"serial":transition_serial}
	transitions.append(task)
	transition_step(task,frame_delta)

func transition_step(task: Dictionary, delta: float) -> void:
	var target: Dictionary = task.target
	if task.timer >= task.duration:
		target.transitioning = false
		if task.kind == "remove": tracked.erase(task.id)
		transitions.erase(task)
		return
	task.timer = C.f32(task.timer + C.f32(delta))
	var amount := C.ease_out(C.f32(task.timer / task.duration))
	var start: Vector3 = task.start if task.kind == "add" else target.last
	var end: Vector3 = target.last if task.kind == "add" else focus
	# The original coroutines use Vector2.Lerp and assign a zero Z.
	target.position = Vector3(C.lerp32(start.x,end.x,amount),C.lerp32(start.y,end.y,amount),0)

func advance_tracking(delta: float, update_transforms := true) -> void:
	# Keep concurrent source coroutines: an early collection may overlap add/remove.
	for task in transitions.duplicate():
		if task.fresh: task.fresh = false
		else: transition_step(task,delta)
	# UpdateTargetTransforms runs after coroutine resumption, in LateUpdate.
	if update_transforms:
		for target in tracked.values():
			if not target.detached: target.last = target.live
			if not target.transitioning: target.position = target.last
	targets.assign(tracked.values())

func zoom_from_box(width: float, height: float) -> float:
	var tangent := C.f32(tan(C.f32(fov * 0.5 * C.f32(C.f32(PI) / 180.0))))
	return maxf(C.f32(width / aspect * 0.5 / tangent),C.f32(height * 0.5 / tangent))

func weighted_centroid() -> Vector3:
	if targets.is_empty(): return Vector3.ZERO
	var low := vlerp(focus,targets[0].position,targets[0].influence)
	var high := low
	for target in targets.slice(1):
		var point := vlerp(focus,target.position,target.influence)
		low = low.min(point)
		high = high.max(point)
	var middle := low + (high - low) * 0.5
	# Current LevelEnclosed has no rotation and a centered 43 x 43 box.
	return middle.clamp(-level_size * 0.5,level_size * 0.5)

static func encapsulate(bounds: Dictionary, point: Vector3) -> void:
	var low: Vector3 = (bounds.center - bounds.extents).min(point)
	var high: Vector3 = (bounds.center + bounds.extents).max(point)
	bounds.extents = (high - low) * 0.5
	bounds.center = low + bounds.extents

func step(delta: float, freeze := false, paused := false) -> void:
	if freeze or paused: return
	delta = C.f32(delta)
	focus = vlerp(focus,weighted_centroid() + Vector3(0,0,-1),C.f32(delta * 5.0))
	var bounds := {"center":focus,"extents":Vector3.ZERO}
	for target in targets:
		var middle := vlerp(focus,target.position,target.influence)
		var extent: Vector3 = (target.size * 2.0 + Vector3(15,15,0)) * C.f32(target.influence) * 0.5
		encapsulate(bounds,middle - extent)
		encapsulate(bounds,middle + extent)
	var size: Vector3 = bounds.extents * 2.0 + Vector3(C.f32(8.0 * aspect),10,0)
	target_zoom = clampf(zoom_from_box(size.x,size.y),minimum_zoom,maximum_zoom)
	var speed := 2.0 if current_zoom > target_zoom else 6.0
	current_zoom = C.lerp32(current_zoom,target_zoom,C.f32(speed * delta))
	aim_position = focus + camera_angle * current_zoom
	position = aim_position + Vector3(0,C.f32(-current_zoom * zoom_y_offset),0)
