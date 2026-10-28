class_name OriginalMovingFloors
extends RefCounted
## MovingFloor.FixedUpdate/ToggleTargetPosition. Coordinates remain Unity XY;
## source Z is reflected by the scene loader. Reactive movement reads the
## shared TimeKeeper timestamp; rolling-pin visuals use its latest velocity.
const C = preload("res://core/original_timekeeper.gd")
var floors: Dictionary = {}
var world: OriginalCollisionWorld
var rng: OriginalUnityRandom
var disc_parents: Dictionary = {}
var rollers: Array[Dictionary] = []
var now := 0.0

func initialize(sidecar: Dictionary, nodes: Dictionary, collision: OriginalCollisionWorld, random: OriginalUnityRandom) -> void:
	world = collision; rng = random
	floors.clear(); disc_parents.clear(); rollers.clear()
	for record in sidecar.get("behaviours", []):
		if record.get("class") not in ["MovingFloor", "MovingFloorReactive"] or not nodes.has(int(record.game_object)): continue
		var reactive: bool = record.get("class") == "MovingFloorReactive"
		var data: Dictionary = record.data
		var node: Node3D = nodes[int(record.game_object)]
		var points: Array[Vector3] = []
		for p in data.get("positions" if reactive else "positionRotations", []): points.append(Vector3(p.x, p.y, p.z))
		if points.size() < 2: continue
		var floor := {"component_id":int(record.component_id), "game_object":int(record.game_object), "node":node,
			"points":points, "initial":node.transform, "initial_global":node.global_transform, "reactive":reactive,
			"initial_state":int(data.get("movementState",0)), "is_spring":bool(data.get("isSpring",0)),
			"pre_duration":float(data.get("preMoveDur",0.35)), "pause_duration":float(data.get("springPauseDur",0.25)), "windback_duration":float(data.get("springWindBackDur",2.0)), "initial_index":int(data.posIndex),
			"initial_percent":float(data.movePercent), "use_rotations":bool(data.useRotations),
			"counter_clockwise":bool(data.counterClockwise), "movement_type":int(data.movementType),
			"min_duration":float(data.moveDurMin), "max_duration":float(data.moveDurMax),
			"enabled":bool(data.get("m_Enabled",1)) and bool(record.get("active_in_hierarchy",true)),
			"collider_ids":[], "id":-1, "graph_mask":int(data.get("graphMask",{}).get("value",0))}
		floors[int(record.component_id)] = floor
		for shape in world.shapes:
			if shape.actor or int(shape.object_id) != int(record.game_object): continue
			floor.collider_ids.append(int(shape.id))
			if floor.id == -1: floor.id = int(shape.id)
			shape.tag = "MovingFloor"
			shape.moving_floor = floor
	for record in sidecar.get("behaviours", []):
		if record.get("class") != "MovingFloorRollingPin": continue
		var data: Dictionary = record.data; var refs: Dictionary = record.get("resolved_refs",{})
		var floor_id := int(data.pairedMovingFloor.m_PathID)
		if not floors.has(floor_id) or not nodes.has(int(refs.get("roller",0))): continue
		var roller: Node3D = nodes[int(refs.roller)]
		rollers.append({"node":nodes[int(record.game_object)], "roller":roller, "floor":floor_id, "spin_factor":float(data.spinFactor), "initial":roller.transform,
			"enabled":bool(data.get("m_Enabled",1)) and bool(record.get("active_in_hierarchy",true))})
	reset(false)

func reset(consume_start_random := true) -> void:
	disc_parents.clear()
	for f in floors.values():
		f.node.transform = f.initial
		f.pos_index = 0 if f.reactive else f.initial_index; f.move_percent = f.initial_percent
		f.state = f.initial_state; f.reverse = false; f.spring_back = false; f.toggle_timestamp = 0.0
		f.move_duration = f.min_duration; f.new_duration = 0.0
		f.start_pos = Vector2.ZERO; f.target_pos = Vector2.ZERO
		f.start_rotation = 0.0; f.target_rotation = 0.0
		f.z = f.node.position.z; f.moving = false; f.velocity = Vector2.ZERO
		f.last_pos = Vector2(f.node.global_position.x, f.node.global_position.y)
		f.previous_transform = f.node.global_transform
		# Start consumes a random scan timer even though this port's collider
		# synchronization is immediate. Retain its draw in the shared stream.
		if consume_start_random and f.enabled: rng.range_float(C.f32(-0.05 * 0.75), 0.0)
		world.sync_scene_subtree(int(f.game_object))

	for roller in rollers: roller.roller.transform = roller.initial

func start_all() -> void:
	for f in floors.values():
		if f.enabled and not f.reactive: toggle(f)

func euler_z(node: Node3D) -> float:
	return fposmod(C.f32(rad_to_deg(node.rotation.z)), 360.0)

func toggle(f: Dictionary) -> void:
	f.new_duration = rng.range_float(f.min_duration, f.max_duration)
	var p: Vector3 = f.points[int(f.pos_index)]
	f.start_pos = Vector2(p.x,p.y)
	if f.use_rotations:
		f.start_rotation = p.z
		if f.start_rotation == 0.0 and f.counter_clockwise: f.start_rotation = 360.0
	f.pos_index = (int(f.pos_index) + 1) % f.points.size()
	p = f.points[int(f.pos_index)]; f.target_pos = Vector2(p.x,p.y)
	if f.use_rotations:
		f.target_rotation = p.z
		if f.target_rotation == 0.0 and euler_z(f.node) > 180.0 and not f.counter_clockwise: f.target_rotation = 360.0
		f.move_percent = C.f32((euler_z(f.node) - fmod(f.start_rotation,360.0)) / (f.start_rotation-f.target_rotation))
	else:
		f.move_percent = C.f32((Vector2(f.node.position.x,f.node.position.y)-f.start_pos).length() / (f.start_pos-f.target_pos).length())
	f.moving = true

static func ease_in_out(t: float) -> float:
	return C.f32(0.5 * (1.0-C.f32(cos(C.f32(C.f32(PI)*t)))))

static func cubic_in_out(t: float) -> float:
	t = C.f32(t*2.0)
	if t < 1.0: return C.f32(0.5*t*t*t)
	t = C.f32(t-2.0)
	return C.f32(0.5*(t*t*t+2.0))

func step_floor(f: Dictionary, delta: float) -> void:
	f.previous_transform = f.node.global_transform
	if not f.enabled: return
	delta = C.f32(delta)
	if f.reactive:
		step_reactive(f,delta)
		return
	if not f.moving: return
	var p: Vector2
	if f.move_percent < 1.0:
		f.move_duration = C.lerp32(f.move_duration,f.new_duration,delta)
		f.move_percent = C.f32(f.move_percent + C.f32(delta/f.move_duration))
		var t: float = f.move_percent
		var x := t; var y := t
		match int(f.movement_type):
			1:
				x = C.ease_in(t) if int(f.pos_index)%2 == 0 else C.ease_out(t)
				y = C.ease_out(t) if int(f.pos_index)%2 == 0 else C.ease_in(t)
			2: x = cubic_in_out(t); y = ease_in_out(t)
			4: x = ease_in_out(t); y = x
		p = Vector2(C.lerp32(f.start_pos.x,f.target_pos.x,x),C.lerp32(f.start_pos.y,f.target_pos.y,y))
		if int(f.movement_type) == 3:
			var smooth := C.f32(t*t*(3.0-2.0*t))
			var subtle := C.f32((t*7.15+smooth*2.85)*0.1)
			var middle: Vector2 = f.start_pos+f.target_pos
			var eased := ease_in_out(t)
			var a := Vector2(C.lerp32(f.start_pos.x,middle.x,eased),C.lerp32(f.start_pos.y,middle.y,eased))
			var b := Vector2(C.lerp32(middle.x,f.target_pos.x,eased),C.lerp32(middle.y,f.target_pos.y,eased))
			p = Vector2(C.lerp32(a.x,b.x,subtle),C.lerp32(a.y,b.y,subtle))
		if f.use_rotations: f.node.rotation = Vector3(0,0,deg_to_rad(C.lerp32(f.start_rotation,f.target_rotation,t)))
	else:
		p = f.target_pos
		# Toggle observes the endpoint before source Z is restored.
		f.node.position = Vector3(p.x,p.y,0)
		if f.use_rotations: f.node.rotation = Vector3(0,0,deg_to_rad(f.target_rotation))
		toggle(f)
	f.node.position = Vector3(p.x,p.y,f.z)
	var current := Vector2(f.node.global_position.x,f.node.global_position.y)
	f.velocity = (current-f.last_pos)/delta; f.last_pos = current
	world.sync_scene_subtree(int(f.game_object))

func fixed_step(delta: float) -> void:
	for f in floors.values(): step_floor(f,delta)

func carry(f: Dictionary, position: Vector2, z: float) -> Vector2:
	var point: Vector3 = f.node.global_transform * (f.previous_transform.affine_inverse() * Vector3(position.x,position.y,-z))
	return Vector2(point.x,point.y)

func rotation_delta(f: Dictionary) -> float:
	return wrapf(f.node.global_transform.basis.get_euler().z-f.previous_transform.basis.get_euler().z,-PI,PI)

static func path_point(f: Dictionary, point: Vector3, inverse := false) -> Vector3:
	if f.is_empty(): return point
	# MovingFloorReactive keeps rotating-platform paths in world coordinates
	# and queues obstacle rescans rather than transforming the waypoints.
	if f.get("reactive",false) and f.use_rotations: return point
	if not f.has("path_transform") or f.path_transform!=f.node.global_transform:
		refresh_path_transform(f)
	if f.path_translation_only:
		return point-f.path_translation if inverse else point+f.path_translation
	return OriginalNavigation.transform_point(f.path_inverse if inverse else f.path_matrix,point)

static func refresh_path_transform(f: Dictionary) -> void:
	# Original GraphTransform is current.localToWorld * original.worldToLocal.
	# Graph coordinates retain source Z, unlike imported Godot scene nodes.
	var original: Transform3D = f.initial_global
	# All 30 original platform hierarchies have unit scale. Unity's Transform
	# builds their inverse from the rotation transpose, retaining quaternion
	# roundoff; a mathematical affine inverse would remove that roundoff.
	var initial_inverse := original.basis.transposed()
	var initial_matrix := source_matrix(Transform3D(initial_inverse,Vector3.ZERO))
	var origin := Vector3(original.origin.x,original.origin.y,-original.origin.z)
	var translation := -OriginalNavigation.transform_point(initial_matrix,origin)
	initial_matrix[3]=translation.x; initial_matrix[7]=translation.y; initial_matrix[11]=translation.z
	var current := source_matrix(f.node.global_transform)
	var matrix: Array = []; matrix.resize(16)
	for row in 4:
		for column in 4:
			var sum := 0.0
			for k in 4: sum += current[row*4+k]*initial_matrix[k*4+column]
			matrix[row*4+column]=C.f32(sum)
	var transform := godot_matrix(matrix)
	# GraphTransform.MatrixIsTranslational uses Unity's approximate Vector4 ==.
	# It ignores tiny basis noise and uses addition/subtraction, not inversion.
	f.path_translation_only=(transform.basis.x-Vector3.RIGHT).length_squared()<1e-10 and (transform.basis.y-Vector3.UP).length_squared()<1e-10 and (transform.basis.z-Vector3.BACK).length_squared()<1e-10
	f.path_transform=f.node.global_transform
	f.path_matrix=matrix
	f.path_inverse=source_matrix(transform.affine_inverse())
	f.path_translation=Vector3(transform.origin.x,transform.origin.y,-transform.origin.z)

static func source_matrix(t: Transform3D) -> Array:
	return [t.basis.x.x,t.basis.y.x,-t.basis.z.x,t.origin.x,t.basis.x.y,t.basis.y.y,-t.basis.z.y,t.origin.y,-t.basis.x.z,-t.basis.y.z,t.basis.z.z,-t.origin.z,0.0,0.0,0.0,1.0]

static func godot_matrix(m: Array) -> Transform3D:
	return Transform3D(Basis(Vector3(m[0],m[4],-m[8]),Vector3(m[1],m[5],-m[9]),Vector3(-m[2],-m[6],m[10])),Vector3(m[3],m[7],-m[11]))

func update_actor(environment: OriginalActorEnvironment) -> void:
	var actor := environment.actor
	if not actor.physics_enabled:
		environment.detach_floor(); return
	var parent: Dictionary = environment.parent_floor
	if not parent.is_empty():
		actor.position = carry(parent,actor.position,actor.source_z)
		actor.rotation_z += rotation_delta(parent)
		environment.parent_velocity = parent.velocity
	var hits: Array = world.overlaps(actor.position,actor.radius,environment.ground.floor_mask)
	var touching := false
	for hit in hits:
		if not parent.is_empty() and hit.shape.get("moving_floor",{}).get("component_id",-1) == parent.component_id: touching = true
	if not parent.is_empty() and not touching: environment.detach_floor()
	if environment.parent_kind != 0: return
	for hit in hits:
		var shape: Dictionary = hit.shape
		if not shape.has("moving_floor"): continue
		if environment.ground.hazard_contacts(actor.position,C.f32(actor.radius*C.f32(0.95))) > 4 or contains_point(shape,actor.position):
			environment.parent_floor = shape.moving_floor
			environment.parent_kind = 1; environment.parent_shape_id = int(shape.id)
			environment.parent_velocity = shape.moving_floor.velocity
			return

static func contains_point(shape: Dictionary, p: Vector2) -> bool:
	if shape.kind == "circle": return p.distance_squared_to(shape.center) <= shape.radius*shape.radius
	return Geometry2D.is_point_in_polygon(p,shape.vertices) if shape.kind == "polygon" else false

func update_discs(discs: Array) -> void:
	var live: Dictionary = {}
	for item in discs:
		var disc: OriginalDiscMotion = item.motion
		var id := disc.get_instance_id()
		if not disc.active or disc.movement != OriginalDiscMotion.Movement.DROPPED: continue
		live[id] = true
		if disc_parents.has(id): disc.position = carry(disc_parents[id],disc.position,disc.source_z)
		var hits := world.overlaps(disc.position,disc.collider_radius,1073807360)
		# Stay requires the centre inside; Exit requires the entire collider to
		# stop overlapping. A rim contact must not drop an existing parent.
		var found := false
		if disc_parents.has(id):
			for hit in hits:
				if hit.shape.get("moving_floor",{}).get("component_id",-1) == disc_parents[id].component_id: found = true
		for hit in hits:
			if hit.shape.has("moving_floor") and contains_point(hit.shape,disc.position):
				disc_parents[id] = hit.shape.moving_floor; found = true; break
		if not found: disc_parents.erase(id)
	for id in disc_parents.keys():
		if not live.has(id): disc_parents.erase(id)

func toggle_reactive(f: Dictionary, spring_back := false) -> void:
	if int(f.state) == 3:
		f.state = 1
	else:
		var p: Vector3 = f.points[int(f.pos_index)]
		f.start_pos = Vector2(p.x,p.y)
		if f.use_rotations: f.start_rotation = p.z
		if int(f.state) in [1,2] and not f.reverse and f.points.size() > 1:
			f.pos_index = posmod(int(f.pos_index)-1,f.points.size()); f.reverse = true
		else:
			f.pos_index = (int(f.pos_index)+1)%f.points.size(); f.reverse = false
		p = f.points[int(f.pos_index)]; f.target_pos = Vector2(p.x,p.y)
		if f.use_rotations: f.target_rotation = p.z
		f.move_percent = C.f32(1.0-f.move_percent) if int(f.state) in [1,2] and f.points.size()>1 else 0.0
		f.state = 3 if spring_back else 2
	f.toggle_timestamp = C.f32(now)
	f.spring_back = spring_back

static func lerp_angle_degrees(start: float, target: float, t: float) -> float:
	var difference := C.f32(target-start)
	difference = C.f32(difference-floor(difference/360.0)*360.0)
	if difference > 180.0: difference = C.f32(difference-360.0)
	return C.f32(start+difference*clampf(t,0.0,1.0))

func step_reactive(f: Dictionary, delta: float) -> void:
	if int(f.state) in [2,3]:
		var wait: float = f.pre_duration if int(f.state)==2 else f.pause_duration
		if C.f32(now-f.toggle_timestamp) < wait:
			f.velocity = Vector2.ZERO; return
		f.state = 1
	if int(f.state) != 1: return
	var point: Vector2
	if f.move_percent < 1.0:
		var duration: float = f.windback_duration if f.spring_back else f.move_duration
		f.move_percent = C.f32(f.move_percent+C.f32(delta/duration))
		var t := clampf(float(f.move_percent),0.0,1.0)
		var easing := C.ease_in(t) if f.reverse else C.ease_out(t)
		point = Vector2(C.lerp32(f.start_pos.x,f.target_pos.x,easing),C.lerp32(f.start_pos.y,f.target_pos.y,easing))
		if f.use_rotations: f.node.rotation = Vector3(0,0,deg_to_rad(lerp_angle_degrees(f.start_rotation,f.target_rotation,ease_in_out(t))))
	else:
		point = f.target_pos
		if f.use_rotations: f.node.rotation = Vector3(0,0,deg_to_rad(f.target_rotation))
		f.state = 0
		if f.is_spring and not f.spring_back and int(f.pos_index)==1: toggle_reactive(f,true)
		f.reverse = false
	f.node.position = Vector3(point.x,point.y,f.z)
	var current := Vector2(f.node.global_position.x,f.node.global_position.y)
	f.velocity = (current-f.last_pos)/delta; f.last_pos = current
	world.sync_scene_subtree(int(f.game_object))

func frame_step(delta: float) -> void:
	for item in rollers:
		if not item.enabled: continue
		var velocity: Vector2 = floors[item.floor].velocity
		var right: Vector3 = item.node.global_basis.x.normalized()
		var direction := OriginalActorMotion.normalized(velocity)
		var amount := C.f32(OriginalActorMotion.magnitude(velocity)*item.spin_factor*delta*C.f32(Vector2(right.x,right.y).dot(direction)))
		item.roller.rotate_object_local(Vector3.BACK,deg_to_rad(amount))
