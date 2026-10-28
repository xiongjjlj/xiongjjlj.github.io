extends SceneTree
class QueryFixture extends OriginalCollisionWorld:
	var blocked := false
	func circle_cast(_origin: Vector2, _radius: float, _direction: Vector2, _distance: float, _mask: int, _exclude := -1) -> Dictionary:
		return {"distance": 1.0} if blocked else {}
func _initialize() -> void:
	var native := OS.get_cmdline_user_args().has("--native")
	var reference: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/disc-native-parry.json" if native else "res://../../../work/disc-parry-oracle.json"))
	var differences: Array = []
	var differing_cases := 0
	for sample in reference.cases:
		var world := QueryFixture.new()
		world.blocked = sample.name == "wall"
		var actors: Array[OriginalActorMotion] = [OriginalActorMotion.new(), OriginalActorMotion.new()]
		actors[0].is_attacking = true
		actors[0].rotation_z = 0.0
		actors[1].position = Vector2(0, 3.5)
		var weapons: Array[OriginalPlayerWeapons] = []
		for actor in actors:
			var weapon := OriginalPlayerWeapons.new()
			weapon.initialize(actor, world)
			weapons.append(weapon)
		var owner_index := 0 if sample.initial.same_owner else 1
		weapons[owner_index].held_discs = sample.initial.owner_held
		var combat := OriginalCombatWorld.new()
		combat.initialize(actors, weapons, world)
		var disc := OriginalDiscMotion.new()
		disc.owner_actor = actors[owner_index]
		disc.position = Vector2(sample.initial.position[0], sample.initial.position[1])
		disc.source_z = OriginalTimekeeper.f32(sample.initial.source_z)
		disc.throw_direction = Vector2(0, 1)
		disc.aim_power = 1.0 if sample.name == "charged" else 0.0
		disc.initialize(sample.name == "fresh_drop")
		disc.velocity = Vector2(sample.initial.velocity[0], sample.initial.velocity[1])
		disc.stored = disc.velocity
		disc.throw_timer = OriginalTimekeeper.f32(0.12)
		disc.can_collide_with_owner = sample.name != "own_before_return"
		if sample.name == "dropped": disc.movement = OriginalDiscMotion.Movement.DROPPED
		if sample.name == "public": disc.owner_actor = null
		if sample.name == "protected": actors[0].deflected_discs[disc.get_instance_id()] = 10
		combat.trigger_disc_parry(0, disc)
		if sample.name == "duplicate": combat.trigger_disc_parry(0, disc)
		var actual := disc.snapshot()
		actual.merge({"throw_direction": [disc.throw_direction.x, disc.throw_direction.y], "can_home": disc.can_home,
			"can_control": disc.can_be_controlled, "deflected": disc.was_just_deflected, "deflected_timer": disc.deflected_timer,
			"owner_disarmed": actors[owner_index].is_disarmed, "owner_protected": actors[owner_index].deflected_melees.has(0)})
		var changed := false
		for field in sample.state:
			var expected: Variant = sample.state[field]
			var value: Variant = actual[field]
			var error := 0.0
			if expected is Array:
				for index in expected.size(): error = maxf(error, absf(value[index] - OriginalTimekeeper.f32(expected[index])))
			elif expected is bool: error = 0.0 if expected == value else 1.0
			else: error = absf(value - OriginalTimekeeper.f32(expected))
			if error > 0.0:
				changed = true
				if differences.size() < 30: differences.append({"case": sample.name, "z": sample.initial.source_z, "field": field, "actual": value, "expected": expected, "error": error})
		if changed: differing_cases += 1
	var report := {"source_runtime": reference.get("runtime", "Legacy desktop CoreCLR fixture"), "original_player_execution": native, "strict_equal": differing_cases == 0, "cases": reference.cases.size(), "different_cases": differing_cases, "first_differences": differences, "native_parity_verified": false, "scope": reference.scope}
	var file := FileAccess.open("res://../evidence/godot-native-disc-parry-motion-check.json" if native else "res://../evidence/godot-disc-parry-motion-check.json", FileAccess.WRITE)
	file.store_string(JSON.stringify(report, "\t") + "\n")
	file.close()
	print(JSON.stringify(report))
	quit(0 if differing_cases == 0 else 1)
