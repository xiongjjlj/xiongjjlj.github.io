extends SceneTree
var checks: Array[Dictionary] = []

func fixture() -> Dictionary:
	var world := OriginalCollisionWorld.new()
	var actors: Array[OriginalActorMotion] = [OriginalActorMotion.new(), OriginalActorMotion.new()]
	var weapons: Array[OriginalPlayerWeapons] = []
	for index in 2:
		actors[index].position = Vector2(index * 3.5, 0)
		actors[index].last_input_move = Vector2.RIGHT if index == 0 else Vector2.LEFT
		actors[index].last_rotation = actors[index].last_input_move
		actors[index].rotation_z = -PI / 2.0 if index == 0 else PI / 2.0
		var weapon := OriginalPlayerWeapons.new()
		weapon.initialize(actors[index], world)
		weapons.append(weapon)
	var round_state := OriginalFfaRound.new()
	round_state.initialize(actors)
	var combat := OriginalCombatWorld.new()
	combat.initialize(actors, weapons, world, round_state)
	var disc := OriginalDiscMotion.new()
	disc.position = Vector2(2.5, 0)
	disc.velocity = Vector2(-20, 0)
	disc.stored = disc.velocity
	disc.owner_actor = actors[1]
	disc.world = world
	combat.bind_disc(disc)
	return {"actors": actors, "weapons": weapons, "world": world, "round": round_state, "combat": combat, "disc": disc}

func check(label: String, result: bool) -> void:
	checks.append({"name": label, "passed": result})

func _initialize() -> void:
	var no_discs: Array[OriginalDiscMotion] = []
	for guard in ["normal", "dash", "shield", "invulnerable", "deflected", "unarmed", "front_attack", "back_attack", "wall", "outside"]:
		var f := fixture()
		f.weapons[0].try_attack()
		match guard:
			"dash": f.actors[1].is_dashing = true
			"shield": f.actors[1].is_shielded = true
			"invulnerable": f.actors[1].is_invulnerable = true
			"deflected": f.actors[1].deflected_melees[0] = 10
			"unarmed": f.weapons[0].held_discs = 0
			"front_attack", "back_attack":
				f.actors[1].is_attacking = true
				if guard == "back_attack": f.actors[1].rotation_z = -PI / 2.0
			"wall": f.world.shapes.append({"id": 17, "kind": "circle", "center": Vector2(1.75, 0), "radius": 0.25, "layer": 256, "actor": false, "bounds": Rect2(1.5, -0.25, 0.5, 0.5)})
			"outside": f.actors[1].position = Vector2(5, 0)
		f.combat.native_overlap_phase(no_discs)
		check("melee guard " + guard, (f.actors[1].state == OriginalActorMotion.State.DEAD) == (guard in ["normal", "back_attack"]))
		if guard == "normal": check("melee kill awards one point", f.round.scores.net_score(0) == 1)
	var shape := fixture()
	check("uses all nine original polygon vertices", shape.combat.melee_polygon(0).size() == 9)
	check("front point overlaps original melee shape", shape.combat.melee_touches_circle(0, Vector2(3, 0), 0.1))
	check("rear point is outside melee shape", not shape.combat.melee_touches_circle(0, Vector2(-1, 0), 0.1))
	for kind in ["normal", "charged", "own_before_return", "own_return", "dropped", "public", "wall"]:
		var f := fixture()
		f.weapons[0].try_attack()
		f.actors[0].velocity_stored = Vector2(10, 0)
		match kind:
			"charged": f.disc.aim_power = 1.0
			"own_before_return", "own_return":
				f.disc.owner_actor = f.actors[0]
				f.disc.can_collide_with_owner = kind == "own_return"
			"dropped": f.disc.movement = OriginalDiscMotion.Movement.DROPPED
			"public": f.disc.owner_actor = null
			"wall": f.world.shapes.append({"id": 17, "kind": "circle", "center": Vector2(1.25, 0), "radius": 0.25, "layer": 256, "actor": false, "bounds": Rect2(1.0, -0.25, 0.5, 0.5)})
		var parried: bool = f.combat.trigger_disc_parry(0, f.disc)
		check("parry guard " + kind, parried == (kind not in ["own_before_return", "wall"]))
		if kind == "normal":
			check("enemy disc becomes free and retains original 2000 impulse", f.disc.movement == OriginalDiscMotion.Movement.FREE and f.disc.velocity.length() > 3000.0 and f.disc.velocity.x > 0.0)
			check("deflection retains owner and briefly suppresses native collider", f.disc.owner_actor == f.actors[1] and f.disc.collider_disabled_remaining == OriginalTimekeeper.f32(0.02))
			check("parry freezes and reflects stored actor velocity", f.combat.events[0].freeze == 0.25 and f.actors[0].velocity == Vector2(-10, 0))
			check("duplicate callback is suppressed", not f.combat.trigger_disc_parry(0, f.disc))
			check("deflecting actor survives damage callback", not f.combat.trigger_damage(0, f.disc))
		elif kind == "charged": check("charged parry adds original stun and slow-motion request", f.actors[0].is_stunned and f.actors[0].push_stun_duration == OriginalTimekeeper.f32(0.8) and f.combat.events[0].charged)
		elif kind == "own_return": check("own return is rethrown", f.disc.movement == OriginalDiscMotion.Movement.THROWING and f.disc.throw_timer == 0.0)
		elif kind == "own_before_return": check("Disc override still runs before owner-protection guard", f.disc.aim_power == 1.0 and not f.disc.can_home and not f.disc.can_be_controlled and f.disc.velocity == Vector2(-20, 0))
		elif kind == "dropped": check("dropped disc stays dropped without freeze", f.disc.movement == OriginalDiscMotion.Movement.DROPPED and f.combat.events.is_empty())
		elif kind == "public": check("public disc has no Projectile owner reflection", f.disc.velocity == Vector2(-20, 0))
	var clash := fixture()
	clash.actors[1].position = Vector2(4, 0)
	clash.weapons[0].try_attack()
	clash.weapons[1].try_attack()
	clash.combat.native_overlap_phase(no_discs)
	check("frontal melee clash preserves both players", clash.actors[0].state == OriginalActorMotion.State.ALIVE and clash.actors[1].state == OriginalActorMotion.State.ALIVE)
	check("frontal melee clash stuns both players", clash.actors[0].is_stunned and clash.actors[1].is_stunned)
	check("melee clash produces one freeze request", clash.combat.events.size() == 1 and clash.combat.events[0].type == "melee_block")
	var failed := checks.filter(func(item: Dictionary) -> bool: return not item.passed)
	var report := {"passed": failed.is_empty(), "checks": checks, "failed": failed, "native_unity_parity_verified": false,
		"scope": "Source-derived guard, polygon, melee clash and ordinary/charged disc-deflection integration boundaries; not a native callback-order oracle"}
	var file := FileAccess.open("res://../evidence/godot-melee-combat-check.json", FileAccess.WRITE)
	file.store_string(JSON.stringify(report, "\t") + "\n")
	file.close()
	print(JSON.stringify(report))
	quit(0 if failed.is_empty() else 1)
