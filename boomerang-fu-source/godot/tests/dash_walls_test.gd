extends SceneTree
const C = preload("res://core/original_timekeeper.gd")
var differences: Array = []
var mismatch_count := 0
func vector(value: Array) -> Vector2: return Vector2(value[0],value[1])
func compare(actual: Dictionary, expected: Dictionary, name: String, frame: int) -> void:
	for key in actual:
		var reference: Variant = expected[key]
		if actual[key] is float: reference = C.f32(reference)
		elif actual[key] is Array: reference = reference.map(func(value):return C.f32(value))
		if actual[key] != reference:
			mismatch_count += 1
			if differences.size()<30: differences.append({"case":name,"frame":frame,"field":key,"actual":actual[key],"expected":reference})
func configure_power(actor: OriginalActorMotion, power: int) -> void:
	actor.dash_through_walls = (power & 4)!=0
	actor.move_faster = (power & 2)!=0
func _initialize() -> void:
	var fixture: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/dash-native-walls.json"))
	var ground := OriginalGroundQueries.new()
	ground.world = OriginalCollisionWorld.new()
	for definition in fixture.shapes:
		var shape: Dictionary = definition.duplicate()
		shape.id = ground.world.shapes.size()+1
		shape.layer = int(shape.layer)
		shape.actor = false
		shape.edge_radius = 0.0
		if shape.kind == "circle":
			shape.center = vector(definition.center)
			shape.radius = C.f32(definition.radius)
			shape.bounds = Rect2(shape.center-Vector2.ONE*shape.radius,Vector2.ONE*shape.radius*2)
			ground.world.shapes.append(shape)
			continue
		shape.vertices = PackedVector2Array()
		for point in definition.vertices: shape.vertices.append(vector(point))
		shape.bounds = Rect2(shape.vertices[0],Vector2.ZERO)
		for point in shape.vertices: shape.bounds = shape.bounds.expand(point)
		ground.world.shapes.append(shape)
	var frames := 0
	for item in fixture.cases:
		var actor := OriginalActorMotion.new()
		actor.world = ground.world
		var environment := OriginalActorEnvironment.new()
		environment.initialize(actor,ground)
		var initial: Dictionary = item.initial
		actor.position = vector(initial.position)
		actor.input_move = vector(initial.direction)
		actor.is_falling = initial.falling
		actor.state = int(initial.state)
		actor.fall_timer = C.f32(initial.timer)
		actor.falls_saved = int(initial.saved)
		configure_power(actor,int(initial.power))
		compare({"accepted":actor.dash()},item,item.name,-2)
		compare({"stop_dash":actor.stop_dash_on_hazards,"velocity":[actor.velocity.x,actor.velocity.y],"dash_timer":actor.dash_timer,"cooldown":actor.dash_cooldown_timer,"mask":actor.collision_layer,"insides":actor.insides_enabled,"falling":actor.is_falling,"state":actor.state,"saved":actor.falls_saved},item.dash_start,item.name,-1)
		for expected in item.frames:
			actor.position = vector(expected.input.position)
			actor.velocity = vector(expected.input.velocity)
			configure_power(actor,int(expected.input.power))
			actor.stop_dash_on_hazards = expected.input.stop_dash
			actor.is_lunging = expected.input.lunge
			actor.stop_lunge_on_hazards = expected.input.stop_lunge
			actor.lunge_timer = C.f32(expected.input.lunge_timer)
			var force := actor.calculate_dash()
			actor.velocity += force * actor.fixed_delta * actor.time_scaler * actor.slide_factor
			environment.stop_dash_on_hazards()
			environment.stop_lunge_on_hazards()
			actor.cooldown_dash()
			compare({"force":[force.x,force.y],"velocity":[actor.velocity.x,actor.velocity.y],"dash_timer":actor.dash_timer,"cooldown":actor.dash_cooldown_timer,"current_amount":actor.current_dash_amount,"dashing":actor.is_dashing,"can_walk":actor.can_walk,"can_dash":actor.can_dash,"mask":actor.collision_layer,"insides":actor.insides_enabled,"falling":actor.is_falling,"state":actor.state,"saved":actor.falls_saved,"lunge_timer":actor.lunge_timer,"will_hazard":environment.will_move_into_hazard()},expected,item.name,int(expected.frame))
			frames += 1
	var query_actor := OriginalActorMotion.new()
	query_actor.world = ground.world
	query_actor.velocity = Vector2(12,3)
	var queries := OriginalActorEnvironment.new()
	queries.initialize(query_actor,ground)
	for index in fixture.queries.size():
		var expected: Dictionary = fixture.queries[index]
		query_actor.position = vector(expected.point)
		query_actor.dash_through_walls = expected.phase
		compare({"safe":queries.is_dash_safe(vector(expected.direction)),"lunge_safe":queries.is_lunge_safe(vector(expected.direction)),"will_hazard":queries.will_move_into_hazard(),"inside":queries.inside_obstacle()},expected,"queries",index)
	var report := {"passed":mismatch_count==0,"strict_equal":mismatch_count==0,"cases":fixture.cases.size(),"frames":frames,"queries":fixture.queries.size(),"mismatched_fields":mismatch_count,"first_differences":differences,"scope":"Original Actor/Player dash and hazard guard bodies, native Unity math and Physics2D; controlled transforms and audiovisual adapters. Static supplied floors/walls, default FFA safety flags. Full movement and native rendering are not covered.","full_game_parity_verified":false}
	FileAccess.open("res://../evidence/godot-native-dash-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print(JSON.stringify(report))
	quit(0 if mismatch_count==0 else 1)
