extends SceneTree
const P = preload("res://core/original_player_powers.gd")
var checks: Array = []
func check(name: String, result: bool) -> void:
	checks.append({"name": name, "passed": result})
func fixture() -> Dictionary:
	var actors: Array[OriginalActorMotion] = [OriginalActorMotion.new(), OriginalActorMotion.new()]
	actors[1].position = Vector2(3,0)
	actors[0].rotation_z = -PI/2
	actors[0].last_input_move = Vector2.RIGHT
	var world := OriginalCollisionWorld.new()
	var weapons: Array[OriginalPlayerWeapons] = []
	for actor in actors:
		var weapon := OriginalPlayerWeapons.new()
		weapon.initialize(actor,world)
		weapons.append(weapon)
	var round_state := OriginalFfaRound.new()
	round_state.initialize(actors)
	var combat := OriginalCombatWorld.new()
	combat.initialize(actors,weapons,world,round_state)
	weapons[1].powers.start(P.Power.SHIELD)
	return {"actors":actors,"weapons":weapons,"world":world,"round":round_state,"combat":combat}
func projectile(f: Dictionary) -> OriginalDiscMotion:
	var disc := OriginalDiscMotion.new()
	disc.world = f.world
	disc.owner_actor = f.actors[0]
	disc.position = Vector2(1.4,0)
	disc.source_z = -0.75
	disc.throw_direction = Vector2.RIGHT
	disc.initialize()
	disc.stored = Vector2(70,0)
	f.combat.bind_disc(disc)
	return disc
func _initialize() -> void:
	for kind in ["normal","charged","owner","dash","wall","frontal_melee"]:
		var f := fixture()
		var disc := projectile(f)
		match kind:
			"charged":disc.aim_power = 1.0
			"owner":disc.owner_actor = f.actors[1]
			"dash":f.actors[1].is_dashing = true
			"wall":f.world.shapes.append({"id":17,"kind":"circle","center":Vector2(2.2,0),"radius":0.25,"layer":256,"actor":false,"bounds":Rect2(1.95,-0.25,0.5,0.5)})
			"frontal_melee":f.actors[1].is_attacking = true;f.actors[1].rotation_z = PI/2
		var hit: bool = f.combat.trigger_shield_disc(1,disc)
		var accepted: bool = kind in ["normal","charged"]
		check("shield damage guards "+kind,hit==accepted and f.actors[1].is_shielded!=accepted)
		check("shield contact does not score "+kind,f.round.scores.net_score(0)==0 and f.actors[1].state==OriginalActorMotion.State.ALIVE)
		if accepted:
			check("shield consumed and removed from history "+kind,not f.weapons[1].powers.has_power(P.Power.SHIELD) and f.weapons[1].powers.history.is_empty())
			check("disc reflects without melee freeform bonus "+kind,disc.velocity.x<0 and not disc.was_just_deflected)
			check("post-shield same-tick damage rejected "+kind,not f.combat.trigger_damage(1,disc))
			check("duplicate shield callback rejected "+kind,not f.combat.trigger_shield_disc(1,disc))
			check("source stun duration "+kind,f.actors[1].push_stun_duration==OriginalTimekeeper.f32(0.75 if kind=="charged" else 0.2))
	var timer := fixture()
	timer.weapons[1].powers.damage_shield()
	for i in 50:timer.weapons[1].powers.unscaled_frame(0.016666)
	check("unscaled invulnerability still active before .85",timer.actors[1].is_invulnerable)
	for i in 2:timer.weapons[1].powers.unscaled_frame(0.016666)
	check("crossing duration still yields one frame",timer.actors[1].is_invulnerable)
	timer.weapons[1].powers.unscaled_frame(0.016666)
	check("unscaled timer expires and enables stay damage",not timer.actors[1].is_invulnerable and timer.actors[1].just_became_vulnerable)
	var flash := fixture()
	flash.weapons[1].powers.damage_shield()
	flash.weapons[1].powers.unscaled_frame(0.0125)
	check("blink half-tick rounds to even",not flash.weapons[1].powers.invulnerability_visible)
	var paused := fixture()
	paused.weapons[1].powers.damage_shield()
	for i in 100:paused.weapons[1].powers.unscaled_frame(0.02,true)
	check("pause does not consume shield immunity",paused.weapons[1].powers.invulnerability_time==0)
	var melee := fixture()
	melee.weapons[0].try_attack()
	var empty: Array[OriginalDiscMotion] = []
	melee.combat.native_overlap_phase(empty)
	check("melee breaks shield without killing",not melee.actors[1].is_shielded and melee.actors[1].state==OriginalActorMotion.State.ALIVE)
	check("shield stops attacking actor's lunge",melee.actors[0].is_stunned and melee.actors[0].melee_timer==0)
	var inventory := fixture()
	inventory.weapons[0].powers.start(P.Power.MOVE_FASTER)
	inventory.weapons[0].powers.start(P.Power.EXTRA_DISC)
	var saved: Dictionary = inventory.weapons[0].powers.round_snapshot()
	var next_actor := OriginalActorMotion.new()
	var next_weapon := OriginalPlayerWeapons.new()
	next_weapon.initialize(next_actor,inventory.world)
	next_weapon.powers.restore_round(saved)
	check("next round preserves haste and two discs",next_actor.move_faster and next_weapon.held_discs==2 and next_weapon.attacks_left==2)
	next_actor.input_move = Vector2.RIGHT
	check("haste increases source movement speed",next_actor.get_move_speed()>OriginalActorMotion.new().get_move_speed())
	next_weapon.powers.start(P.Power.REVERSE_INPUTS)
	check("bamboozle reverses movement",next_actor.calculate_walk().x<0)
	var dead := fixture()
	dead.weapons[1].powers.start(P.Power.REVERSE_INPUTS)
	dead.combat.kill(1,"fall")
	check("death removes shield and temporary effects",not dead.actors[1].is_shielded and not dead.actors[1].reverse_inputs and dead.weapons[1].powers.history.is_empty())
	var failed := checks.filter(func(x: Dictionary) -> bool:return not x.passed)
	var report := {"passed":failed.is_empty(),"checks":checks,"failed":failed,"native_contact_order_verified":false,"full_game_parity_verified":false}
	FileAccess.open("res://../evidence/godot-power-combat-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print(JSON.stringify(report))
	quit(0 if failed.is_empty() else 1)
