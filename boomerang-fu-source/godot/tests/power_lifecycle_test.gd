extends SceneTree
const P = preload("res://core/original_player_powers.gd")
var differences: Array = []
func snapshot(power: OriginalPlayerPowers, weapon: OriginalPlayerWeapons) -> Dictionary:
	return {"active": power.active, "history": power.history.duplicate(), "held": weapon.held_discs, "pending": power.pending_discs, "shield": weapon.actor.is_shielded, "shield_hits": power.shield_hits, "reverse_timer": power.reverse_timer, "reverse_limit": power.reverse_limit, "reverse_running": power.reverse_running, "reverse_fill": power.reverse_fill, "immune_timer": power.time_immune_timer, "invulnerable": weapon.actor.is_invulnerable, "cooling": weapon.cooling_attack, "attack_cooldown": weapon.attack_cooldown}
func _initialize() -> void:
	var fixture: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/power-native-lifecycle.json"))
	var states := 0
	for item in fixture.cases:
		var actor := OriginalActorMotion.new()
		actor.time_scaler = item.scale
		var weapon := OriginalPlayerWeapons.new()
		weapon.initialize(actor, OriginalCollisionWorld.new())
		weapon.held_discs = item.held
		for step in item.trace:
			match step.op:
				"power": weapon.powers.start(int(step.value))
				"step": weapon.powers.frame_step(step.value)
				"shield_hit": weapon.powers.damage_shield()
				"stop_temporary": weapon.powers.stop_temporary()
				"clear": weapon.powers.clear()
			var actual := snapshot(weapon.powers, weapon)
			for key in step.state:
				var expected = step.state[key]
				if expected is float: expected = OriginalTimekeeper.f32(expected)
				if key == "history":
					var ids: Array[int] = []
					for value in expected: ids.append(int(value))
					expected = ids
				if actual[key] != expected:
					if differences.size() < 30: differences.append({"case": item.name, "step": states, "field": key, "actual": actual[key], "expected": expected})
			states += 1
	var report := {"passed": differences.is_empty(), "cases": fixture.cases.size(), "states": states, "differences": differences, "source_runtime": fixture.runtime, "original_player_execution": fixture.native_runtime, "full_game_parity_verified": false}
	FileAccess.open("res://../evidence/godot-native-power-lifecycle-check.json", FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print(JSON.stringify(report))
	quit(0 if report.passed else 1)
