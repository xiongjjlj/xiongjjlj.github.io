extends SceneTree
var checks: Array = []
func fixture() -> Dictionary:
	var world := OriginalCollisionWorld.new()
	var actors: Array[OriginalActorMotion] = [OriginalActorMotion.new(), OriginalActorMotion.new()]
	var weapons: Array[OriginalPlayerWeapons] = []
	for index in 2:
		actors[index].position = Vector2(index * 2.5, 0)
		actors[index].last_input_move = Vector2.RIGHT
		actors[index].last_rotation = Vector2.RIGHT
		actors[index].rotation_z = -PI / 2.0
		var weapon := OriginalPlayerWeapons.new()
		weapon.initialize(actors[index], world)
		weapons.append(weapon)
	weapons[0].held_discs = 0
	weapons[0].attacks_left = 0
	var round_state := OriginalFfaRound.new()
	round_state.initialize(actors)
	var combat := OriginalCombatWorld.new()
	combat.initialize(actors, weapons, world, round_state)
	return {"actors": actors, "weapons": weapons, "world": world, "round": round_state, "combat": combat}
func check(label: String, passed: bool) -> void:
	checks.append({"name": label, "passed": passed})
func _initialize() -> void:
	var empty: Array[OriginalDiscMotion] = []
	for kind in ["normal", "two_discs", "unarmed", "frozen", "dash", "invulnerable", "shield", "dead", "wall", "outside"]:
		var f := fixture()
		match kind:
			"two_discs": f.weapons[1].held_discs = 2
			"unarmed": f.weapons[1].held_discs = 0
			"frozen": f.actors[1].is_frozen = true
			"dash": f.actors[1].is_dashing = true
			"invulnerable": f.actors[1].is_invulnerable = true
			"shield": f.actors[1].is_shielded = true
			"dead": f.actors[1].state = OriginalActorMotion.State.DEAD
			"wall": f.world.shapes.append({"id": 17, "kind": "circle", "center": Vector2(1.25, 0), "radius": 0.25, "layer": 256, "actor": false, "bounds": Rect2(1.0, -0.25, 0.5, 0.5)})
			"outside": f.actors[1].position = Vector2(4, 0)
		var accepted: bool = f.weapons[0].try_attack()
		check("empty hands use disarm " + kind, accepted and f.actors[0].is_disarming and not f.actors[0].is_attacking)
		f.combat.native_overlap_phase(empty)
		var hit: bool = kind in ["normal", "two_discs", "unarmed", "frozen"]
		check("disarm guard " + kind, f.actors[1].is_stunned == hit)
		check("disarm never scores a direct kill " + kind, f.round.scores.net_score(0) == 0 and (kind == "dead" or f.actors[1].state == OriginalActorMotion.State.ALIVE))
		if kind == "normal":
			check("one disc leaves inventory and enters flight", f.weapons[1].held_discs == 0 and f.weapons[1].spawned_discs.size() == 1)
			var disc: OriginalDiscMotion = f.weapons[1].spawned_discs[0]
			check("drop starts Free on Collectables layer with original throw speed", disc.started_dropped and not disc.projectile_layer and disc.movement == OriginalDiscMotion.Movement.FREE and disc.velocity.length() == 142.5 and disc.velocity.x > 0.0)
			check("drop retains original owner before slowing", disc.owner_actor == f.actors[1] and f.weapons[1].owned_discs.has(disc))
			check("source drop init observes held inventory before decrement", not f.actors[1].is_disarmed)
			check("armed target receives 0.8-second stun", f.actors[1].push_stun_duration == OriginalTimekeeper.f32(0.8))
			check("lunge cooldown shortened by original 0.85 multiplier", f.actors[0].lunge_cooldown_timer == OriginalTimekeeper.f32(OriginalTimekeeper.f32(0.5) * OriginalTimekeeper.f32(0.85)))
			check("disabled physical-layer callback does not halve attacker velocity", f.actors[0].melee_timer == OriginalTimekeeper.f32(0.2) and f.actors[0].velocity.x == OriginalTimekeeper.f32(40.0 * OriginalTimekeeper.f32(0.66)))
			for frame in 240:
				disc.fixed_step()
				disc.frame_step(disc.fixed_delta)
				if disc.owner_actor == null: break
			check("slowed dropped disc becomes public and leaves old owner's list", disc.owner_actor == null and f.weapons[1].owned_discs.is_empty())
			check("public dropped disc can be collected by attacker", f.weapons[0].collect(disc) and f.weapons[0].held_discs == 1)
		elif kind == "two_discs": check("disarm drops only one of two discs", f.weapons[1].held_discs == 1 and f.weapons[1].spawned_discs.size() == 1)
		elif kind == "unarmed": check("empty target gets shorter stun without invented disc", f.actors[1].push_stun_duration == 0.5 and f.weapons[1].spawned_discs.is_empty())
		elif kind == "frozen": check("frozen target is stunned but keeps its disc", f.actors[1].push_stun_duration == OriginalTimekeeper.f32(0.8) and f.weapons[1].held_discs == 1 and f.weapons[1].spawned_discs.is_empty())
	var expiry := fixture()
	expiry.weapons[0].try_attack()
	expiry.combat.native_overlap_phase(empty)
	check("push kill attribution names attacker", expiry.combat.killer_ids[1] == 0)
	expiry.combat.frame_step(2.0)
	check("kill attribution expires on following frame at zero", expiry.combat.killer_ids[1] == 0)
	expiry.combat.frame_step(0.01)
	check("expired push attribution returns to the player", expiry.combat.killer_ids[1] == 1)
	expiry.combat.native_overlap_phase(empty)
	check("continued overlap does not fabricate another disarm", expiry.weapons[1].spawned_discs.size() == 1 and expiry.combat.killer_ids[1] == 1)
	var retry := fixture()
	retry.weapons[0].try_attack()
	check("empty-hand repeat is blocked during cooldown", not retry.weapons[0].try_attack())
	for index in 42: retry.weapons[0].frame_step(1.0 / 60.0)
	check("disarm charge refills after attack cooldown", retry.weapons[0].try_attack())
	var repeated := fixture()
	repeated.actors[1].is_stunned = true
	repeated.actors[1].push_stun_duration = 0.4
	repeated.actors[1].stun_timer = 0.2
	repeated.weapons[0].try_attack()
	repeated.combat.native_overlap_phase(empty)
	check("already-stunned target keeps its existing stun timer", repeated.actors[1].push_stun_duration == 0.4 and repeated.actors[1].stun_timer == 0.2)
	var failed := checks.filter(func(item: Dictionary) -> bool: return not item.passed)
	var report := {"passed": failed.is_empty(), "checks": checks, "failed": failed, "native_frame_parity_verified": false, "scope": "Original disarm geometry/guards, stun, one-disc drop, source collision-matrix exclusions, ownership release and pickup"}
	var file := FileAccess.open("res://../evidence/godot-disarm-check.json", FileAccess.WRITE)
	file.store_string(JSON.stringify(report, "\t") + "\n")
	file.close()
	print(JSON.stringify(report))
	quit(0 if failed.is_empty() else 1)
