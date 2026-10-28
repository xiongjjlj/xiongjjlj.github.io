extends SceneTree
var checks: Array = []
func fixture() -> OriginalPlayerInput:
	var actor := OriginalActorMotion.new()
	var weapon := OriginalPlayerWeapons.new()
	weapon.initialize(actor, OriginalCollisionWorld.new())
	var input := OriginalPlayerInput.new()
	input.initialize(weapon)
	return input
func step(input: OriginalPlayerInput, attack := false, held := false, released := false, dash := false, time := 0.0, delta := 0.016666) -> void:
	input.process_frame(attack, held, released, dash, delta, time, Vector2.ZERO, PI)
func check(label: String, result: bool) -> void:
	checks.append({"name": label, "passed": result})
func _initialize() -> void:
	var one := fixture()
	step(one, true, false, true)
	check("same-frame armed attack precedes throw", one.actions == ["attack"] and one.weapon.held_discs == 1 and one.weapon.spawned_discs.is_empty())
	var two := fixture()
	two.weapon.held_discs = 2
	two.weapon.attacks_left = 2
	step(two, true, false, true)
	check("two-disc attack and throw can both succeed in source order", two.actions == ["attack", "throw"] and two.weapon.held_discs == 1 and two.weapon.action_hand == "left")
	var bounded := fixture()
	bounded.weapon.actor.is_stunned = true
	for index in 12: step(bounded, true, false, false, false, float(index) / 100.0)
	check("failed inputs retain only eight entries", bounded.buffer.size() == 8 and bounded.buffer[0].time == 0.04)
	bounded.weapon.actor.is_stunned = false
	step(bounded, false, false, false, false, 0.15)
	check("oldest valid retry succeeds and clears all pending input", bounded.actions == ["attack"] and bounded.buffer.is_empty())
	var expired := fixture()
	expired.remember("attack", 0.0)
	step(expired, false, false, false, false, 0.36)
	check("attack expires after source 0.35-second window", expired.actions.is_empty() and expired.buffer.size() == 1)
	for time in [0.25, 0.25000003]:
		var dash := fixture()
		dash.remember("dash", 0.0)
		step(dash, false, false, false, false, time)
		check("dash retry boundary " + str(time), dash.actions.has("dash") == (time == 0.25))
	var aim := fixture()
	step(aim, false, true, false, false, 0.0, 0.2)
	step(aim, false, true, false, true, 0.2)
	check("dash follows aim and blocks held throw until release", aim.actions == ["dash"] and aim.weapon.input_blocked and not aim.weapon.actor.is_aiming)
	step(aim, false, false, true, false, 0.21)
	check("release clears blocked throw without shooting", not aim.weapon.input_blocked and aim.weapon.spawned_discs.is_empty())
	var cancelled := fixture()
	cancelled.weapon.actor.is_aiming = true
	cancelled.weapon.input_blocked = true
	cancelled.remember("attack", 0.0)
	step(cancelled, false, false, true, false, 0.1)
	check("release stops aim before buffered attack retry", cancelled.actions == ["attack"] and cancelled.weapon.held_discs == 1)
	var failed := checks.filter(func(item: Dictionary) -> bool: return not item.passed)
	var report := {"passed": failed.is_empty(), "checks": checks, "failed": failed, "scope": "Source-derived PlayerInput action order, bounded FIFO, retry boundaries and held-input cancellation; no native Rewired timing comparison"}
	var file := FileAccess.open("res://../evidence/godot-player-input-check.json", FileAccess.WRITE)
	file.store_string(JSON.stringify(report, "\t") + "\n")
	file.close()
	print(JSON.stringify(report))
	quit(0 if failed.is_empty() else 1)
