extends SceneTree
const C = preload("res://core/original_timekeeper.gd")
func vector(value: Array) -> Vector2: return Vector2(value[0],value[1])
func _initialize() -> void:
	var fixture: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/fall-native-lifecycle.json"))
	var ground := OriginalGroundQueries.new()
	ground.world = OriginalCollisionWorld.new()
	ground.water_mask = int(fixture.water_mask)
	for definition in fixture.shapes:
		var shape: Dictionary = definition.duplicate()
		shape.layer = int(shape.layer)
		shape.vertices = PackedVector2Array()
		for point in definition.vertices: shape.vertices.append(vector(point))
		ground.world.shapes.append(shape)
	var differences: Array = []
	var frames := 0
	var different_frames := 0
	for item in fixture.cases:
		var actor := OriginalActorMotion.new()
		var environment := OriginalActorEnvironment.new()
		environment.initialize(actor,ground)
		var initial: Dictionary = item.initial
		actor.position = vector(initial.position)
		actor.velocity = vector(initial.velocity)
		actor.velocity_stored = vector(initial.stored)
		actor.state = int(initial.state)
		actor.is_falling = initial.falling
		actor.is_sliced = initial.sliced
		actor.time_scaler = C.f32(initial.time_scaler)
		actor.slide_factor = C.f32(initial.slide)
		environment.parent_kind = int(initial.parent_kind)
		environment.parent_velocity = vector(initial.parent_velocity)
		ground.cloud_level = initial.cloud
		var deaths := 0
		for expected in item.states:
			actor.position = vector(expected.input.position)
			actor.velocity = vector(expected.input.velocity)
			actor.is_dashing = expected.input.dashing
			if expected.input.save: environment.fall_saved()
			environment.before_fixed_step()
			environment.frame_step(C.f32(fixture.delta))
			for event in environment.events.duplicate():
				if event.type == "fall_death":
					deaths += 1
					actor.state = OriginalActorMotion.State.DEAD
					actor.velocity = Vector2.ZERO
					environment.fall_in_hazard()
			environment.events.clear()
			var actual := {"position":[actor.position.x,actor.position.y],"velocity":[actor.velocity.x,actor.velocity.y],"timer":actor.fall_timer,"duration":actor.fall_duration,"state":actor.state,"falling":actor.is_falling,"saved":actor.falls_saved,"slide":actor.slide_factor,"visible":actor.appearance_visible,"physics":actor.physics_enabled,"fall_animation":environment.fall_animation,"frozen_animation":environment.frozen_animation,"hops":environment.hop_count,"footsteps":environment.footstep_count,"hazards":environment.hazard_count,"deaths":deaths}
			var different := false
			for key in actual:
				var reference: Variant = expected[key]
				# Native JSON uses round-trip float32 decimals, parsed here as f64.
				if actual[key] is float: reference = C.f32(reference)
				elif actual[key] is Array: reference = reference.map(func(value): return C.f32(value))
				if actual[key] != reference:
					different = true
					if differences.size()<24: differences.append({"case":item.name,"frame":expected.frame,"field":key,"actual":actual[key],"expected":reference})
			if different: different_frames += 1
			frames += 1
	var report := {"passed":different_frames==0,"strict_equal":different_frames==0,"cases":fixture.cases.size(),"frames":frames,"different_frames":different_frames,"first_differences":differences,"scope":"Unchanged Actor fall methods and extracted update branches in original UnityPlayer, with real Physics2D; controlled time/positions and explicit death/audio/animation adapters. Full moving-floor motion, death effects and Animator rendering are not covered.","full_game_parity_verified":false}
	FileAccess.open("res://../evidence/godot-native-fall-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print(JSON.stringify(report))
	quit(0 if different_frames==0 else 1)
