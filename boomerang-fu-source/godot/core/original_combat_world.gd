class_name OriginalCombatWorld
extends RefCounted
## Normal-disc trigger routing and Actor/Player damage guards. Source query
## radii/masks are retained; native Unity query/callback order is not verified.
const C = preload("res://core/original_timekeeper.gd")
const A = preload("res://core/original_actor_motion.gd")
const D = preload("res://core/original_disc_motion.gd")
var actors: Array[OriginalActorMotion] = []
var weapons: Array[OriginalPlayerWeapons] = []
var world: OriginalCollisionWorld
var damage_radius := C.f32(C.f32(0.75) * C.f32(1.33))
var pickup_radius := C.f32(C.f32(1.1) * C.f32(1.33))
var shield_radius := C.f32(C.f32(1.4) * C.f32(1.33))
var events: Array[Dictionary] = []
var killer_ids: Array[int] = []
var public_timers: Dictionary = {}
var round_state: OriginalFfaRound
var clock: OriginalTimekeeper
var melee_binding: Dictionary
var melee_pairs: Dictionary = {}
var melee_damage_pairs: Dictionary = {}
var disarm_binding: Dictionary
var disarm_damage_pairs: Dictionary = {}
var discs: Array[OriginalDiscMotion] = []
var killer_timers: Dictionary = {}
var boundary_owner := -1
var decoy_world:WeakRef

func telefrag(attacker: OriginalActorMotion, destination: Vector2, radius: float) -> void:
	world.sync_actors()
	for hit in world.overlaps(destination,radius,512):
		if not hit.shape.actor: continue
		var victim: OriginalActorMotion = hit.shape.actor_ref.get_ref()
		if victim == attacker: continue
		var index := actors.find(victim)
		if index < 0:
			if victim.is_decoy and decoy_world!=null:decoy_world.get_ref().die(victim.get("serial"),"telefrag")
			continue
		killer_ids[index] = actors.find(attacker)
		killer_timers[index] = 0.0
		kill(index,"telefrag")

func initialize(motions: Array[OriginalActorMotion], inventories: Array[OriginalPlayerWeapons], queries: OriginalCollisionWorld, round_controller: OriginalFfaRound = null) -> void:
	actors = motions
	weapons = inventories
	for weapon in weapons: weapon.collision_receiver = weakref(self)
	world = queries
	round_state = round_controller
	killer_ids.resize(actors.size())
	for index in actors.size(): killer_ids[index] = index
	melee_binding = JSON.parse_string(FileAccess.get_file_as_string("res://assets/characters/melee-bindings.json"))
	disarm_binding = JSON.parse_string(FileAccess.get_file_as_string("res://assets/characters/disarm-bindings.json"))

func melee_center(index: int) -> Vector2:
	var offset := Vector2(melee_binding.position.x, melee_binding.position.y) * Vector2(melee_binding.actor_scale.x, melee_binding.actor_scale.y)
	return actors[index].position + offset.rotated(actors[index].rotation_z)

func melee_polygon(index: int) -> PackedVector2Array:
	var points := PackedVector2Array()
	var local_scale := Vector2(melee_binding.scale.x, melee_binding.scale.y)
	var actor_scale := Vector2(melee_binding.actor_scale.x, melee_binding.actor_scale.y)
	var offset := Vector2(melee_binding.offset.x, melee_binding.offset.y)
	for point in melee_binding.paths[0]:
		var local := (Vector2(point.x, point.y) + offset) * local_scale * actor_scale
		points.append(melee_center(index) + local.rotated(actors[index].rotation_z))
	return points

func melee_touches_circle(index: int, position: Vector2, radius: float) -> bool:
	return not OriginalCollisionWorld.penetration(position, radius, {"kind": "polygon", "vertices": melee_polygon(index)}).is_empty()

func bind_disc(disc: OriginalDiscMotion) -> void:
	disc.collision_receiver = weakref(self)
	if not discs.has(disc): discs.append(disc)
	if disc.pending_time_scaled_contacts > 0: check_disc_current_location(disc)

func check_disc_current_location(disc: OriginalDiscMotion) -> void:
	# Disc.StartThrow explicitly performs a radius-0.5 trigger overlap when
	# personally accelerated, before the following native physics tick.
	# Native overlap ordering among simultaneous contacts remains unverified.
	disc.pending_time_scaled_contacts = 0
	var contacts: Array[Dictionary] = []
	for index in actors.size():
		var actor := actors[index]
		if actor == disc.owner_actor or actor.state == A.State.DEAD: continue
		var circle_shape := {"kind":"circle","center":actor.position,"radius":0.0}
		var touches := func(radius:float)->bool:
			circle_shape.radius=radius
			return not OriginalCollisionWorld.penetration(disc.position,disc.radius,circle_shape,true).is_empty()
		if actor.is_attacking and not disc.started_dropped and not OriginalCollisionWorld.penetration(disc.position,disc.radius,{"kind":"polygon","vertices":melee_polygon(index)},true).is_empty(): contacts.append({"kind":"melee","actor":index})
		if actor.is_shielded and touches.call(shield_radius): contacts.append({"kind":"shield","actor":index})
		if not disc.started_dropped and disc.movement != D.Movement.DROPPED and touches.call(damage_radius): contacts.append({"kind":"damage","actor":index})
		if touches.call(pickup_radius): contacts.append({"kind":"pickup","actor":index})
	if not disc.started_dropped and disc.movement != D.Movement.DROPPED:
		for other in discs:
			if other != disc and other.active and other.projectile_layer and not other.is_mini_disc and other.collider_disabled_remaining <= 0.0 and A.magnitude(other.position-disc.position) <= C.f32(other.collider_radius+disc.radius): contacts.append({"kind":"disc","other":other})
	if decoy_world!=null:decoy_world.get_ref().append_current_contacts(disc,contacts)
	for contact in contacts.slice(0,16):
		if not disc.active: break
		match contact.kind:
			"melee": trigger_disc_parry(contact.actor,disc)
			"shield": trigger_shield_disc(contact.actor,disc)
			"damage": trigger_damage(contact.actor,disc)
			"pickup": trigger_pickup(contact.actor,disc)
			"disc": trigger_disc_contact(disc,contact.other)
			"decoy":decoy_world.get_ref().projectile_contact(contact.id,disc)

func on_disc_public(disc: OriginalDiscMotion) -> void:
	for inventory in weapons: inventory.owned_discs.erase(disc)

func on_disc_recycled(disc: OriginalDiscMotion) -> void:
	discs.erase(disc)
	public_timers.erase(disc.get_instance_id())
	public_timers.erase(disc.get_instance_id())
	events.append({"type": "public_disc", "disc": disc.get_instance_id()})

func disarm_center(index: int) -> Vector2:
	var offset := Vector2(disarm_binding.position.x, disarm_binding.position.y) * Vector2(disarm_binding.actor_scale.x, disarm_binding.actor_scale.y)
	return actors[index].position + offset.rotated(actors[index].rotation_z)

func disarm_polygon(index: int) -> PackedVector2Array:
	var points := PackedVector2Array()
	var scale := Vector2(disarm_binding.scale.x, disarm_binding.scale.y) * Vector2(disarm_binding.actor_scale.x, disarm_binding.actor_scale.y)
	for point in disarm_binding.paths[0]:
		var local := (Vector2(point.x, point.y) + Vector2(disarm_binding.offset.x, disarm_binding.offset.y)) * scale
		points.append(disarm_center(index) + local.rotated(actors[index].rotation_z))
	return points

func line_blocked(from: Vector2, to: Vector2, from_z := 0.0, to_z := 0.0) -> bool:
	var difference := to - from
	var z := C.f32(to_z - from_z)
	return not world.circle_cast(from, 0.0, A.normalized_xyz_xy(difference, z), A.magnitude_xyz(difference, z), 256).is_empty()

func sweep_disc(disc: OriginalDiscMotion, amount: Vector2, incoming_velocity: Variant = null) -> Vector2:
	var hits: Array[Dictionary] = []
	var direction := A.normalized(amount)
	var distance := C.f32(amount.length() * disc.fixed_delta)
	if disc.movement != D.Movement.DROPPED and not disc.started_dropped:
		for other in discs:
			if other == disc or not other.active or not other.projectile_layer or other.is_mini_disc or other.collider_disabled_remaining > 0.0: continue
			var hit := OriginalCollisionWorld.cast_point_circle(disc.position, direction, distance, other.position, C.f32(disc.radius + other.collider_radius))
			if not hit.is_empty(): hits.append({"kind": "disc", "other": other, "distance": hit.distance})
	for index in actors.size():
		if actors[index].state == A.State.DEAD: continue
		if actors[index].is_shielded and disc.projectile_layer:
			var shield_hit := OriginalCollisionWorld.cast_point_circle(disc.position, direction, distance, actors[index].position, C.f32(shield_radius + disc.radius))
			if not shield_hit.is_empty(): hits.append({"actor": index, "kind": "shield", "distance": shield_hit.distance})
		if actors[index].is_attacking and disc.movement != D.Movement.DROPPED and not disc.started_dropped and (disc.owner_actor != actors[index] or disc.can_collide_with_owner):
			var polygon := melee_polygon(index)
			var overlap := OriginalCollisionWorld.penetration(disc.position, disc.radius, {"kind": "polygon", "vertices": polygon})
			var hit: Dictionary = {}
			if overlap.is_empty(): hit = OriginalCollisionWorld.cast_polygon(disc.position, direction, distance, disc.radius, polygon)
			elif world.queries_start_in_colliders: hit = {"distance": 0.0}
			if not hit.is_empty(): hits.append({"actor": index, "kind": "melee", "distance": hit.distance, "centroid": disc.position + direction * hit.distance})
		for kind in ["pickup", "damage"]:
			if kind == "damage" and (disc.movement == D.Movement.DROPPED or disc.started_dropped): continue
			var radius := pickup_radius if kind == "pickup" else damage_radius
			var hit := OriginalCollisionWorld.cast_point_circle(disc.position, direction, distance, actors[index].position, C.f32(radius + disc.radius))
			if not hit.is_empty(): hits.append({"actor": index, "kind": kind, "distance": hit.distance})
	if decoy_world!=null:decoy_world.get_ref().append_disc_hits(disc,direction,distance,hits)
	hits.sort_custom(func(a: Dictionary, b: Dictionary) -> bool: return a.distance < b.distance)
	for hit in hits.slice(0, 16):
		if not disc.active: break
		if hit.kind == "disc": trigger_disc_contact(disc, hit.other)
		elif hit.kind=="decoy":decoy_world.get_ref().projectile_contact(hit.id,disc)
		elif hit.kind == "melee":
			trigger_disc_parry(hit.actor, disc)
			if not line_blocked(disc.position, melee_center(hit.actor), disc.source_z, actors[hit.actor].source_z):
				# CheckForCollisions reflects the pre-callback input, not the newly
				# boosted disc velocity, and MoveProjectile multiplies this by dt.
				var incoming: Vector2 = amount if incoming_velocity == null else incoming_velocity
				return hit.centroid - disc.position + A.reflect(incoming, A.normalized_xyz_xy(disc.position - melee_center(hit.actor), disc.source_z - actors[hit.actor].source_z))
		elif hit.kind == "shield": trigger_shield_disc(hit.actor, disc)
		elif hit.kind == "damage": trigger_damage(hit.actor, disc)
		else: trigger_pickup(hit.actor, disc)
	return amount

func native_overlap_phase(active_discs: Array[OriginalDiscMotion]) -> void:
	# Unity runs native trigger callbacks after the complete FixedUpdate batch.
	native_melee_phase()
	native_disarm_phase()
	# The source Physics2D matrix disables Projectile/Projectile native triggers.
	# Their mutual callbacks are produced by the explicit sweep above.
	discs = discs.filter(func(disc: OriginalDiscMotion) -> bool: return disc.active)
	for disc in active_discs:
		if not disc.active: continue
		if disc.collider_disabled_remaining > 0.0:
			disc.damage_overlaps.clear()
			disc.melee_overlaps.clear()
			continue
		var overlaps_now: Dictionary = {}
		var shields_now: Dictionary = {}
		var melees_now: Dictionary = {}
		for index in actors.size():
			if actors[index].state != A.State.DEAD and actors[index].is_attacking and melee_touches_circle(index, disc.position, disc.collider_radius):
				melees_now[index] = true
				if not disc.melee_overlaps.has(index): trigger_disc_parry(index, disc)
		disc.melee_overlaps = melees_now
		for index in actors.size():
			var actor := actors[index]
			if actor.state == A.State.DEAD: continue
			var distance := disc.position.distance_to(actor.position)
			if actor.is_shielded and disc.projectile_layer and distance < C.f32(shield_radius + disc.collider_radius):
				shields_now[index] = true
				if not disc.shield_overlaps.has(index): trigger_shield_disc(index, disc)
			if disc.movement != D.Movement.DROPPED and not disc.started_dropped and distance < C.f32(damage_radius + disc.collider_radius):
				overlaps_now[index] = true
				if not disc.damage_overlaps.has(index) or actor.just_became_vulnerable:
					actor.just_became_vulnerable = false
					trigger_damage(index, disc)
			if disc.active and distance < C.f32(pickup_radius + disc.collider_radius): trigger_pickup(index, disc)
		disc.damage_overlaps = overlaps_now
		disc.shield_overlaps = shields_now
	if decoy_world!=null:decoy_world.get_ref().native_contacts(active_discs)

func trigger_shield_disc(index: int, disc: OriginalDiscMotion) -> bool:
	var actor := actors[index]
	if not disc.active or not actor.is_shielded or actor.state == A.State.DEAD or not disc.projectile_layer: return false
	if disc.shield_triggers_this_step.has(index): return false
	disc.shield_triggers_this_step[index] = true
	# Player's callback and Projectile's callback have different guards. Dashing
	# rejects both; frontal melee rejects only the shield damage callback.
	var blocked := line_blocked(actor.position, disc.position, actor.source_z, disc.source_z)
	var facing := Vector2(-sin(actor.rotation_z), cos(actor.rotation_z))
	var outward := A.normalized_xyz_xy(disc.position - actor.position, disc.source_z - actor.source_z)
	var accepted := not actor.is_dashing and not blocked and not (actor.is_attacking and D.dot32(outward, facing) > 0.0) and disc.owner_actor != actor
	if accepted:
		killer_ids[index] = actors.find(disc.owner_actor)
		killer_timers[index] = 1.5
		weapons[index].powers.damage_shield()
		actor.stop_aiming()
		actor.velocity = Vector2.ZERO
		var ratio := maxf(C.f32(1.0 / actor.time_scaler * disc.time_scaler), C.f32(1.0 / disc.time_scaler * actor.time_scaler))
		if disc.aim_power >= 1.0:
			actor.velocity = A.reflect(actor.velocity_stored, outward)
			actor.start_pushback(-(actor.velocity_stored - disc.stored) * C.f32(1.2) * ratio, C.f32(0.2), C.f32(0.75))
		else:
			actor.start_pushback(disc.stored * ratio, C.f32(0.4), 0.0 if actor.is_stunned else C.f32(0.2))
		events.append({"type": "shield_hit", "actor": index, "charged": disc.aim_power >= 1.0})
	# Disc.OnTriggerEnter disables homing even when its owner's shield is hit.
	if disc.owner_actor != null: disc.can_home = false
	if disc.owner_actor != null and disc.owner_actor != actor and not actor.deflected_discs.has(disc.get_instance_id()):
		if not disc.started_dropped: disc.can_collide_with_owner = true
		var incoming := disc.velocity
		var inward := A.magnitude(disc.velocity) * -outward
		var dot := D.dot32(A.normalized(incoming), outward)
		if dot > 0.0: incoming = inward
		elif dot > -0.5: incoming = incoming * 0.25 + inward * 0.75
		if A.magnitude(disc.velocity) < C.f32(0.1): incoming = -outward
		if not actor.is_dashing: disc.bounce(incoming, outward)
	return accepted

func trigger_shield_melee(victim: int, attacker: int) -> bool:
	var target := actors[victim]
	if not target.is_shielded or target.is_dashing or target.state == A.State.DEAD: return false
	var center := melee_center(attacker)
	var normal := A.normalized(center - target.position)
	var facing := Vector2(-sin(target.rotation_z), cos(target.rotation_z))
	if (target.is_attacking and normal.dot(facing) > 0.0) or line_blocked(target.position, center): return false
	target.stop_aiming()
	target.velocity = Vector2.ZERO
	var ratio := maxf(C.f32(1.0 / target.time_scaler * actors[attacker].time_scaler), C.f32(1.0 / actors[attacker].time_scaler * target.time_scaler))
	killer_ids[victim] = attacker
	killer_timers[victim] = 1.5
	weapons[victim].powers.damage_shield()
	target.start_pushback(-normal * 60.0 * ratio, C.f32(0.3), 0.0 if target.is_stunned else C.f32(0.325))
	# Actor.OnMeleeColliderTriggered("ShieldCollider") stops the attack and
	# pushes its owner back as well; a shield break cannot continue into a kill.
	end_melee_on_block(attacker)
	actors[attacker].velocity = Vector2.ZERO
	actors[attacker].start_pushback(-A.normalized(target.position - actors[attacker].position) * 24.0 * actors[attacker].time_scaler, C.f32(0.4), C.f32(0.4))
	events.append({"type": "shield_hit", "actor": victim, "charged": false})
	return true

func trigger_damage(index: int, disc: OriginalDiscMotion) -> bool:
	var actor := actors[index]
	# DamageCollider checks occlusion before Player's shield and Actor's guards.
	if not disc.active or line_blocked(actor.position, disc.position, actor.source_z, disc.source_z): return false
	if actor.is_shielded or actor.state == A.State.DEAD or actor.is_dashing or actor.is_invulnerable or actor.deflected_discs.has(disc.get_instance_id()): return false
	if disc.movement == D.Movement.DROPPED or disc.started_dropped or disc.owner_actor == actor: return false
	var facing := Vector2(-sin(actor.rotation_z), cos(actor.rotation_z))
	if actor.is_attacking and A.normalized_xyz_xy(disc.position - actor.position, disc.source_z - actor.source_z).dot(facing) > 0.0:
		if disc.owner_actor != null and A.magnitude_xyz(actor.position - disc.position, actor.source_z - disc.source_z) < A.magnitude_xyz(disc.position - disc.owner_actor.position, disc.source_z - disc.owner_actor.source_z):
			disc.owner_actor.deflected_melees[index] = 10
		return false
	if disc.owner_actor != null:
		killer_ids[index] = actors.find(disc.owner_actor)
		return kill(index, "disc")
	if actor.is_frozen: return kill(index, "disc")
	return false

func trigger_disc_contact(moving: OriginalDiscMotion, other: OriginalDiscMotion) -> void:
	# Projectile.CheckForCollisions invokes the hit component before itself.
	for pair in [[other, moving], [moving, other]]:
		var receiver: OriginalDiscMotion = pair[0]
		var collider: OriginalDiscMotion = pair[1]
		if receiver.is_mini_disc or not collider.projectile_layer: continue
		var audio_velocity:=C.f32(A.magnitude(receiver.velocity-collider.stored)/receiver.speed_limit)
		if receiver.collide_with_disc(collider, A.normalized_xyz_xy(collider.position - receiver.position, collider.source_z - receiver.source_z)):
			events.append({"type": "disc_clash", "disc": receiver.get_instance_id(), "other": collider.get_instance_id(), "different_owners": receiver.owner_actor != collider.owner_actor,"position":receiver.position,"audio_velocity":audio_velocity})

func native_disarm_phase() -> void:
	var pairs_now: Dictionary = {}
	for attacker in actors.size():
		if actors[attacker].state == A.State.DEAD or not actors[attacker].is_disarming: continue
		var shape := {"kind": "polygon", "vertices": disarm_polygon(attacker)}
		for victim in actors.size():
			if victim == attacker or actors[victim].state == A.State.DEAD: continue
			var key := "%d:%d" % [attacker, victim]
			if not OriginalCollisionWorld.penetration(actors[victim].position, damage_radius, shape).is_empty():
				pairs_now[key] = true
				if not disarm_damage_pairs.has(key) or actors[victim].just_became_vulnerable:
					actors[victim].just_became_vulnerable = false
					trigger_disarm(victim, attacker)
	# Matrix layer 27 does not contact physical Player layer 9. Do not fabricate
	# the otherwise present OnDisarmColliderTriggered("Player") callback.
	disarm_damage_pairs = pairs_now

func trigger_disarm(victim: int, attacker: int) -> bool:
	var target := actors[victim]
	var source := actors[attacker]
	if not source.is_disarming or source.state == A.State.DEAD or target.state == A.State.DEAD: return false
	if target.is_dashing or target.is_invulnerable or target.is_shielded: return false
	if line_blocked(target.position, disarm_center(attacker)): return false
	killer_ids[victim] = attacker
	killer_timers[victim] = 2.0
	var direction := -A.normalized(disarm_center(attacker) - target.position) * 30.0 * target.time_scaler
	var armed := weapons[victim].held_discs > 0
	target.get_stunned(direction, C.f32(0.8 if armed or target.is_frozen else 0.5))
	if armed and not target.is_frozen:
		var disc := weapons[victim].drop_disc(A.normalized(direction) * 1.5)
		bind_disc(disc)
	events.append({"type": "disarm", "actor": attacker, "victim": victim, "armed": armed, "frozen": target.is_frozen})
	return true

func native_melee_phase() -> void:
	var damage_now: Dictionary = {}
	var pairs_now: Dictionary = {}
	for attacker in actors.size():
		if actors[attacker].state == A.State.DEAD or not actors[attacker].is_attacking: continue
		for victim in actors.size():
			if victim == attacker or actors[victim].state == A.State.DEAD: continue
			var key := "%d:%d" % [attacker, victim]
			if actors[victim].is_shielded and melee_touches_circle(attacker, actors[victim].position, shield_radius):
				trigger_shield_melee(victim, attacker)
			if melee_touches_circle(attacker, actors[victim].position, damage_radius):
				damage_now[key] = true
				if not melee_damage_pairs.has(key) or actors[victim].just_became_vulnerable:
					actors[victim].just_became_vulnerable = false
					if actors[victim].is_invulnerable and not line_blocked(actors[attacker].position, actors[victim].position): actors[attacker].stop_lunge(false)
					trigger_melee_damage(victim, attacker)
			if victim > attacker and actors[victim].is_attacking:
				if not Geometry2D.intersect_polygons(melee_polygon(attacker), melee_polygon(victim)).is_empty():
					pairs_now[key] = true
					if not melee_pairs.has(key):
						trigger_melee_block(attacker, victim)
						trigger_melee_block(victim, attacker)
	melee_damage_pairs = damage_now
	melee_pairs = pairs_now

func trigger_melee_damage(victim: int, attacker: int) -> bool:
	var target := actors[victim]
	var source := actors[attacker]
	if not source.is_attacking or source.state == A.State.DEAD or weapons[attacker].held_discs == 0: return false
	if target.state == A.State.DEAD or target.is_shielded or target.is_dashing or target.is_invulnerable or target.deflected_melees.has(attacker): return false
	if line_blocked(target.position, melee_center(attacker)): return false
	var facing := Vector2(-sin(target.rotation_z), cos(target.rotation_z))
	if target.is_attacking and A.normalized(source.position - target.position).dot(facing) > 0.0: return false
	killer_ids[victim] = attacker
	return kill(victim, "melee")

func end_melee_on_block(index: int) -> void:
	actors[index].melee_timer = 0.0
	weapons[index].attack_cooldown = C.f32(C.f32(0.66) * C.f32(1.2))
	if weapons[index].attacks_left > 0:
		weapons[index].attacks_left = 0
		weapons[index].cooling_attack = true
		weapons[index].attack_cooldown = C.f32(0.15)
	actors[index].stop_lunge()

func trigger_melee_block(index: int, other: int) -> bool:
	var actor := actors[index]
	var other_position := melee_center(other)
	if actor.state == A.State.DEAD or line_blocked(actor.position, other_position): return false
	var up := Vector2(-sin(actors[other].rotation_z), cos(actors[other].rotation_z))
	if A.normalized(actor.position - other_position).dot(up) < C.f32(-0.25): return false
	end_melee_on_block(index)
	var direction := -A.normalized(other_position - actor.position) * 24.0 * actor.time_scaler
	actor.velocity = Vector2.ZERO
	actor.start_pushback(direction * 1.5, C.f32(0.4), C.f32(0.4))
	actor.deflected_melees[other] = 10
	if index > other: events.append({"type": "melee_block", "actor": index, "other": other, "freeze": C.f32(0.2)})
	return true

func trigger_disc_parry(index: int, disc: OriginalDiscMotion) -> bool:
	var actor := actors[index]
	var id := disc.get_instance_id()
	if not disc.active or actor.state == A.State.DEAD or not actor.is_attacking: return false
	var center := melee_center(index)
	var actor_blocked := line_blocked(actor.position, disc.position, actor.source_z, disc.source_z)
	var disc_blocked := line_blocked(disc.position, center, disc.source_z, actor.source_z)
	if actor.deflected_discs.has(id) or disc.melee_triggers_this_step.has(index) or (disc.owner_actor == actor and not disc.can_collide_with_owner):
		apply_disc_melee_override(index, disc, disc_blocked)
		return false
	if actor_blocked and disc_blocked: return false
	disc.melee_triggers_this_step[index] = true
	# Other CollisionBehaviour receives the swept callback first. Protection is
	# added after BOTH callbacks, so it cannot incorrectly suppress reflection.
	if not actor_blocked and disc.movement != D.Movement.DROPPED and not disc.started_dropped:
		end_melee_on_block(index)
		actor.velocity = A.reflect(actor.velocity_stored, A.normalized_xyz_xy(disc.position - actor.position, disc.source_z - actor.source_z))
		if disc.aim_power >= 1.0:
			var ratio := maxf(C.f32(C.f32(1.0 / actor.time_scaler) * disc.time_scaler), C.f32(C.f32(1.0 / disc.time_scaler) * actor.time_scaler))
			var direction := -(actor.velocity_stored - disc.stored) * C.f32(1.2) * ratio
			if direction.length() < 1.0: direction = A.normalized_xyz_xy(actor.position - disc.position, actor.source_z - disc.source_z) * 80.0
			actor.start_pushback(direction, C.f32(0.3), C.f32(0.8))
		events.append({"type": "parry", "actor": index, "disc": id, "charged": disc.aim_power >= 1.0, "freeze": C.f32(0.25)})
	apply_disc_melee_override(index, disc, disc_blocked)
	if disc.owner_actor != null and not disc_blocked:
		if not disc.started_dropped: disc.can_collide_with_owner = true
		var normal := A.normalized_xyz_xy(disc.position - actor.position, disc.source_z - actor.source_z)
		var incoming := disc.velocity
		var inward := disc.velocity.length() * -normal
		var dot := D.dot32(A.normalized(incoming), normal)
		if dot > 0.0: incoming = inward
		elif dot > -0.5: incoming = incoming * 0.25 + inward * 0.75
		if disc.velocity.length() < C.f32(0.1): incoming = -normal
		if D.dot32(A.normalized(A.reflect(incoming, normal)), A.normalized_xyz_xy(disc.owner_actor.position - disc.position, disc.owner_actor.source_z - disc.source_z)) > C.f32(0.9):
			# Native Quaternion.AngleAxis rounding remains a Unity trace comparison.
			incoming = D.quaternion_turn(incoming, -45.0)
		if D.dot32(A.normalized(A.reflect(incoming, normal)), A.normalized_xyz_xy(actor.position - disc.position, actor.source_z - disc.source_z)) > C.f32(0.9): normal *= -1.0
		disc.was_just_deflected = true
		disc.deflected_timer = C.f32(0.15)
		disc.bounce(incoming, normal)
		if disc.movement != D.Movement.DROPPED:
			if disc.owner_actor == actor:
				disc.throw_direction = A.normalized(disc.velocity)
				disc.start_throw(true)
			else:
				disc.start_freeform(2000.0)
				if A.magnitude_xyz(disc.position - disc.owner_actor.position, disc.source_z - disc.owner_actor.source_z) < A.magnitude_xyz(center - disc.owner_actor.position, actor.source_z - disc.owner_actor.source_z): disc.owner_actor.deflected_melees[index] = 10
		else: disc.start_freeform(24.0, true)
		disc.collider_disabled_remaining = C.f32(0.02)
	if not actor_blocked: actor.deflected_discs[id] = 10
	return true

func apply_disc_melee_override(index: int, disc: OriginalDiscMotion, blocked: bool) -> void:
	# Disc's override runs before the base Projectile callback's protection guard.
	if disc.owner_actor == null or blocked: return
	disc.can_be_controlled = false
	disc.can_home = false
	if disc.owner_actor == actors[index] and disc.movement != D.Movement.DROPPED: disc.aim_power = 1.0

func trigger_pickup(index: int, disc: OriginalDiscMotion) -> bool:
	if not disc.active: return false
	if weapons[index].collect(disc):
		for inventory in weapons: inventory.owned_discs.erase(disc)
		public_timers.erase(disc.get_instance_id())
		events.append({"type": "catch", "actor": index, "disc": disc.get_instance_id()})
		return true
	return false

func kill(index: int, death_type: String) -> bool:
	var actor := actors[index]
	if actor.state == A.State.DEAD: return false
	if round_state != null and not round_state.simulation_running(): return false
	actor.stop_aiming()
	actor.is_stunned = false
	if clock != null:
		weapons[index].powers.death_time_immune(clock,weapons.map(func(weapon):return weapon.powers))
		clock.slow_mo_effect(0.5,0.5)
	weapons[index].powers.stop_temporary()
	weapons[index].powers.stop_shield()
	actor.is_showing_off = false
	actor.is_slimed = false
	actor.is_attacking = false
	actor.is_disarming = false
	actor.is_dashing = false
	actor.velocity = Vector2.ZERO
	actor.move_speed_parameter = 0.0
	actor.state = A.State.DEAD
	if death_type in ["telefrag","explosion"]:
		if death_type == "telefrag": actor.physics_enabled = false
		actor.last_rotation = Vector2(0,-1)
		actor.last_input_move = Vector2(0,-1)
		actor.rotation_z = -PI
	if killer_ids[index] == index and boundary_owner >= 0: killer_ids[index] = boundary_owner
	if death_type == "outofbounds":
		actor.physics_enabled = false
		actor.appearance_visible = false
	if round_state != null: round_state.on_death(index, killer_ids[index], death_type)
	for disc in weapons[index].owned_discs:
		public_timers[disc.get_instance_id()] = {"disc": weakref(disc), "yield_frame": true, "wait_frame": true, "remaining": 0.5 if disc.is_mini_disc else 1.0, "owner": index}
	events.append({"type": "death", "victim": index, "killer": killer_ids[index], "cause": death_type,"clock_handled":clock != null})
	return true

func run_killer_timer(index:int,delta:float)->void:
	if killer_ids[index] == index: return
	if killer_timers.get(index,0.0) > 0.0:
		killer_timers[index] = C.f32(killer_timers[index] - C.f32(C.f32(delta) * actors[index].time_scaler))
	else:
		killer_ids[index] = index
		killer_timers[index] = 0.0

func frame_step(delta: float, advance_killers:=true) -> void:
	if advance_killers:
		for index in actors.size():run_killer_timer(index,delta)
	# Disc.BecomePublic: yield null, then WaitForSeconds(1). Dead-owner discs
	# keep their original owner during this delay, including kill attribution.
	for id in public_timers.keys():
		var entry: Dictionary = public_timers[id]
		var disc: OriginalDiscMotion = entry.disc.get_ref()
		if disc == null or not disc.active:
			public_timers.erase(id)
			continue
		if entry.yield_frame:
			entry.yield_frame = false
			continue
		if entry.wait_frame:
			entry.wait_frame = false
			events.append({"type":"disc_untracked","disc":id})
			continue
		entry.remaining = C.f32(entry.remaining - C.f32(delta))
		if entry.remaining <= 0.0:
			weapons[entry.owner].owned_discs.erase(disc)
			if disc.is_mini_disc:
				disc.recycle()
				events.append({"type":"mini_vanish","disc":id})
			else:
				disc.owner_actor = null
				events.append({"type": "public_disc", "disc": id})
			public_timers.erase(id)

func explosion_shield(index: int, blast: OriginalExplosion) -> void:
	var actor := actors[index]
	if actor.is_dashing or actor.is_invulnerable or not actor.is_shielded: return
	actor.velocity = Vector2.ZERO
	var direction := A.normalized_xyz_xy(actor.position-blast.position,actor.source_z-blast.source_z)*40.0*actor.time_scaler
	actor.start_pushback(direction,C.f32(0.5),C.f32(0.6))
	weapons[index].powers.damage_shield()
	killer_ids[index] = blast.owner
	killer_timers[index] = C.f32(1.5)
	events.append({"type":"explosion_shield","actor":index,"direction":direction,"duration":C.f32(0.5),"stun":C.f32(0.6)})

func explosion_damage(index: int, blast: OriginalExplosion) -> void:
	var actor := actors[index]
	if actor.is_dashing: actor.explosion_dodges += 1
	if actor.is_shielded or actor.is_dashing or actor.is_invulnerable: return
	if blast.freeze_explosion and actor.elements_hook != null and actor.elements_hook.get_ref() != null:
		var elements = actor.elements_hook.get_ref()
		elements.freeze()
		elements.set_killer(blast.owner,"Freeze",C.f32(elements.freeze_duration+C.f32(0.01)))
		return
	killer_ids[index] = blast.owner
	killer_timers[index] = 0.0
	if kill(index,"explosion"): actor.is_burnt = true

func apply_explosion(blast: OriginalExplosion) -> void:
	for index in actors.size():
		var actor := actors[index]
		if actor.state == A.State.DEAD or not actor.appearance_visible: continue
		if actor.is_shielded and blast.reaches(actor.position,actor.source_z,shield_radius): explosion_shield(index,blast)
		if blast.reaches(actor.position,actor.source_z,damage_radius): explosion_damage(index,blast)
