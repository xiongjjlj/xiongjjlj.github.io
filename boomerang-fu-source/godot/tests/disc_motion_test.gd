extends SceneTree
const Disc = preload("res://core/original_disc_motion.gd")
class QueryFixture extends OriginalCollisionWorld:
	var blocked := false
	func circle_cast(_origin: Vector2, radius: float, _direction: Vector2, _distance: float, _mask: int, _exclude := -1) -> Dictionary:
		return {"distance": 1.0} if radius == 0.0 and blocked else {}

func _initialize() -> void:
	var native := OS.get_cmdline_user_args().has("--native")
	var height := native or OS.get_cmdline_user_args().has("--height")
	var reference: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/disc-native-height.json" if native else "res://../../../work/disc-height-oracle.json" if height else "res://../../../work/disc-motion-oracle.json"))
	var differences: Array = []
	var different_steps := 0
	var steps := 0
	var maximum := 0.0
	for scenario in reference.cases:
		var owner_actor := OriginalActorMotion.new()
		var enemy := OriginalActorMotion.new()
		var initial: Dictionary = scenario.initial
		var owner_position: Array = initial.get("owner_position", [-0.75, 0])
		owner_actor.position = Vector2(owner_position[0], owner_position[1])
		owner_actor.source_z = OriginalTimekeeper.f32(initial.get("owner_z", 0.0))
		owner_actor.velocity = Vector2(initial.owner_velocity[0], initial.owner_velocity[1])
		var disc := Disc.new()
		disc.owner_actor = owner_actor
		disc.source_z = OriginalTimekeeper.f32(initial.get("disc_z", 0.0))
		disc.aim_power = OriginalTimekeeper.f32(initial.aim_power)
		disc.spin_angle = OriginalTimekeeper.f32(initial.spin_angle)
		disc.time_scaler = OriginalTimekeeper.f32(initial.time_scaler)
		disc.throw_direction = Vector2(initial.throw_direction[0], initial.throw_direction[1])
		disc.is_mini_disc = initial.mini
		disc.can_home = initial.homing
		if initial.homing:
			var world := QueryFixture.new()
			world.blocked = initial.blocked
			world.register_actor(owner_actor)
			var enemy_position: Array = initial.get("enemy_position", [10, 3, 0])
			enemy.position = Vector2(enemy_position[0], enemy_position[1])
			enemy.source_z = OriginalTimekeeper.f32(enemy_position[2])
			world.register_actor(enemy)
			disc.world = world
		disc.initialize(initial.dropped)
		disc.can_return = not initial.no_return
		var index := 0
		for frame in scenario.frames:
			owner_actor.position = Vector2(frame.owner_position[0], frame.owner_position[1])
			owner_actor.retrieve_charge = OriginalTimekeeper.f32(frame.retrieve)
			owner_actor.state = OriginalActorMotion.State.DEAD if frame.owner_dead else OriginalActorMotion.State.ALIVE
			if frame.free: disc.start_freeform()
			if frame.bounce: disc.bounce(disc.velocity, Vector2(frame.normal[0], frame.normal[1]))
			disc.fixed_step()
			disc.frame_step(disc.fixed_delta)
			var actual := disc.snapshot()
			var different := false
			for field in frame.state:
				var expected: Variant = frame.state[field]
				var value: Variant = actual[field]
				var error := 0.0
				if expected is Array:
					for i in expected.size(): error = maxf(error, absf(value[i] - OriginalTimekeeper.f32(expected[i])))
				elif expected is bool:
					error = 0.0 if expected == value else 1.0
				else: error = absf(value - OriginalTimekeeper.f32(expected))
				if error > 0.0:
					different = true
					maximum = maxf(maximum, error)
					if differences.size() < 24: differences.append({"case":scenario.name,"step":index,"field":field,"expected":expected,"actual":value,"error":error})
			if different: different_steps += 1
			index += 1
			steps += 1
	var report := {"source_runtime": reference.get("runtime", "Legacy desktop CoreCLR fixture"), "original_player_execution": native, "strict_equal":different_steps==0,"steps":steps,"scenarios":reference.cases.size(),"different_steps":different_steps,
		"maximum_absolute_difference":maximum,"first_differences":differences,"native_unity_parity_verified":false,
		"scope":"Flight arithmetic from original C# methods; engine/API and spatial-query substitutes; no damage or native collision validation"}
	report["source_disc_z"] = -0.75 if height else 0.0
	var file := FileAccess.open("res://../evidence/godot-native-disc-height-check.json" if native else "res://../evidence/godot-disc-height-check.json" if height else "res://../evidence/godot-disc-motion-check.json",FileAccess.WRITE)
	file.store_string(JSON.stringify(report,"\t")+"\n")
	file.close()
	print(JSON.stringify(report))
	quit(0 if different_steps==0 else 1)
