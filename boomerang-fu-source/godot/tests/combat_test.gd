extends SceneTree
var checks: Array[Dictionary] = []

func fixture() -> Dictionary:
	var world := OriginalCollisionWorld.new()
	var actors: Array[OriginalActorMotion] = [OriginalActorMotion.new(), OriginalActorMotion.new()]
	var inventories: Array[OriginalPlayerWeapons] = []
	for index in 2:
		actors[index].position = Vector2(-6 + index * 6, 0)
		actors[index].world = world
		world.register_actor(actors[index])
		var weapon := OriginalPlayerWeapons.new()
		weapon.initialize(actors[index], world)
		inventories.append(weapon)
	var round_state := OriginalFfaRound.new()
	round_state.initialize(actors)
	var combat := OriginalCombatWorld.new()
	combat.initialize(actors, inventories, world, round_state)
	var disc := OriginalDiscMotion.new()
	disc.position = Vector2(-2, 0)
	disc.owner_actor = actors[0]
	combat.bind_disc(disc)
	return {"world": world, "actors": actors, "weapons": inventories, "round": round_state, "combat": combat, "disc": disc}

func check(name: String, passed: bool) -> void:
	checks.append({"name": name, "passed": passed})

func _initialize() -> void:
	for kind in ["normal", "dash", "invulnerable", "shield", "dead", "own", "dropped", "started_dropped", "deflected", "parry_front", "parry_back", "public", "public_frozen", "wall"]:
		var f := fixture()
		var victim: OriginalActorMotion = f.actors[1]
		match kind:
			"dash": victim.is_dashing = true
			"invulnerable": victim.is_invulnerable = true
			"shield": victim.is_shielded = true
			"dead": victim.state = OriginalActorMotion.State.DEAD
			"own": f.disc.owner_actor = victim
			"dropped": f.disc.movement = OriginalDiscMotion.Movement.DROPPED
			"started_dropped": f.disc.started_dropped = true
			"deflected": victim.deflected_discs[f.disc.get_instance_id()] = 10
			"parry_front", "parry_back":
				victim.is_attacking = true
				victim.rotation_z = PI / 2.0 if kind == "parry_front" else -PI / 2.0
			"public", "public_frozen":
				f.disc.owner_actor = null
				victim.is_frozen = kind == "public_frozen"
			"wall":
				f.world.shapes.append({"id": 17, "kind": "circle", "center": Vector2(-1, 0), "radius": 0.25, "layer": 256, "actor": false, "bounds": Rect2(-1.25, -0.25, 0.5, 0.5)})
		var killed: bool = f.combat.trigger_damage(1, f.disc)
		check("damage guard: " + kind, killed == (kind in ["normal", "parry_back", "public_frozen"]))
		if killed:
			check("repeated callback does not score twice: " + kind, not f.combat.trigger_damage(1, f.disc) and f.round.scores.net_score(0) == (0 if kind == "public_frozen" else 1))
	var simultaneous := fixture()
	var second := OriginalDiscMotion.new()
	second.owner_actor = simultaneous.actors[1]
	second.position = Vector2(-4, 0)
	simultaneous.weapons[1].owned_discs.append(second)
	simultaneous.combat.trigger_damage(1, simultaneous.disc)
	simultaneous.combat.trigger_damage(0, second)
	check("dead owner's existing disc retains kill attribution", simultaneous.round.scores.net_score(0) == 1 and simultaneous.round.scores.net_score(1) == 1)
	simultaneous.round.frame_step(0.0)
	simultaneous.round.frame_step(0.15)
	simultaneous.round.frame_step(1.29)
	check("round survives until both original waits finish", simultaneous.round.phase == OriginalFfaRound.Phase.WAIT_SURVIVORS)
	simultaneous.round.frame_step(0.02)
	check("mutual death concludes at one point each", simultaneous.round.phase == OriginalFfaRound.Phase.SCORES and simultaneous.round.scores.winners.is_empty())
	var public_case := fixture()
	var owned := OriginalDiscMotion.new()
	owned.owner_actor = public_case.actors[1]
	owned.movement = OriginalDiscMotion.Movement.DROPPED
	public_case.weapons[1].owned_discs.append(owned)
	public_case.combat.trigger_damage(1, public_case.disc)
	public_case.combat.frame_step(1.0 / 60.0)
	public_case.combat.frame_step(1.0 / 60.0)
	for frame in 59: public_case.combat.frame_step(1.0 / 60.0)
	check("dead disc remains owned during one-second delay", owned.owner_actor != null)
	for frame in 2: public_case.combat.frame_step(1.0 / 60.0)
	check("dead disc becomes public and can be picked up", owned.owner_actor == null and public_case.combat.trigger_pickup(0, owned) and public_case.weapons[0].held_discs == 2)
	var phase := fixture()
	phase.actors[1].is_dashing = true
	phase.disc.position = Vector2(-0.5, 0)
	var discs: Array[OriginalDiscMotion] = [phase.disc]
	phase.combat.native_overlap_phase(discs)
	phase.actors[1].is_dashing = false
	phase.combat.native_overlap_phase(discs)
	check("dash overlap stay is not fabricated as another enter", phase.actors[1].state == OriginalActorMotion.State.ALIVE)
	phase.disc.position = Vector2(-4, 0)
	phase.combat.native_overlap_phase(discs)
	phase.disc.position = Vector2(-0.5, 0)
	phase.combat.native_overlap_phase(discs)
	check("leaving and re-entering damage trigger can kill", phase.actors[1].state == OriginalActorMotion.State.DEAD)
	var passed := checks.all(func(item: Dictionary) -> bool: return item.passed)
	var report := {"passed": passed, "checks": checks, "scope": "Source-derived damage/score boundaries and trigger adapter integration; no native engine or full-match parity claim", "native_parity_verified": false}
	var output := FileAccess.open("res://../evidence/godot-combat-check.json", FileAccess.WRITE)
	output.store_string(JSON.stringify(report, "\t") + "\n")
	output.close()
	print(JSON.stringify(report))
	quit(0 if passed else 1)
