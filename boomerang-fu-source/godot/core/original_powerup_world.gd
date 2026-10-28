class_name OriginalPowerupWorld
extends RefCounted
## Source FFA spawn gates, useful-power pool and pickup-Stay lifecycle.
## Random samples and spatial queries are adapters; original RNG parity is open.
const C = preload("res://core/original_timekeeper.gd")
const A = preload("res://core/original_actor_motion.gd")
const P = preload("res://core/original_player_powers.gd")
const PROBABILITIES: Array[int] = [32,64,128,256,1024,2,4,2048,512,16,8192,16384,32768]
var available := 2 | 4 | 8 | 16 | 32 | 64 | 128 | 256 | 512 | 1024 | 2048 | 8192 | 16384 | 32768 # All thirteen normal source powerups now have gameplay routing.
var possible: Array[int] = []
var removed: Array[int] = []
var weapons: Array[OriginalPlayerWeapons] = []
var world: OriginalCollisionWorld
var ground: OriginalGroundQueries
var binding: Dictionary
var books: Array[Dictionary] = []
var events: Array[Dictionary] = []
var collected_total := 0
var spawned_round := 0
var attempts_round := 0
var last_round_before_powers := 4
var collected_threshold := 0.0
var spawn_delay := 8.0
var last_attempt := 0.0
var frequency := 1 # Slow=0, Normal=1, Fast=2.
var serial := 0
var collectable_discs: Array[OriginalDiscMotion] = []
var shrinking_allowed := true
var rng := RandomNumberGenerator.new()

func initialize(inventories: Array[OriginalPlayerWeapons], queries: OriginalCollisionWorld, data: Dictionary) -> void:
	weapons = inventories
	world = queries
	binding = data
	ground = OriginalGroundQueries.new()
	ground.world = world
	ground.floor_mask = int(binding.floor_mask)
	rng.seed = 0xB00FA
	build_pool()

func build_pool() -> void:
	possible.clear()
	removed.clear()
	for power in PROBABILITIES:
		if available & power == power: possible.append(power)

func begin_round(number: int, current_time: float, new_match: bool) -> void:
	books.clear()
	collectable_discs.clear()
	events.clear()
	spawned_round = 0
	attempts_round = 0
	if new_match:
		collected_total = 0
		build_pool()
	spawn_delay = C.f32(8.0 / (clampi(weapons.size(), 0, 5) * 0.5))
	collected_threshold = C.f32((number - last_round_before_powers) * C.f32(0.35) * clampi(weapons.size(), 0, 4))
	# Both operands of the source /10 are integers.
	collected_threshold = C.f32(collected_threshold * clampf(int((possible.size() + removed.size()) / 10), C.f32(0.38), 1.0))
	if frequency == 0:
		collected_threshold = C.f32(collected_threshold * C.f32(0.8))
		spawn_delay = C.f32(spawn_delay * 1.5)
	elif frequency == 2:
		collected_threshold = C.f32(collected_threshold * C.f32(1.33))
		spawn_delay = C.f32(spawn_delay * 0.75)
		last_round_before_powers = 1
	last_attempt = C.f32(C.f32(current_time + spawn_delay * C.f32(rng.randf_range(0.0,0.5))) * C.f32(0.66))

func frame_step(delta: float, current_time: float, playing: bool) -> void:
	for book in books:
		if book.collected:
			# WaitForSeconds starts at the end of the collection frame.
			if book.just_collected: book.just_collected = false
			else: book.remaining = C.f32(book.remaining - C.f32(delta))
	for index in range(books.size() - 1, -1, -1):
		if books[index].collected and books[index].remaining <= 0.0:
			events.append({"type":"book_removed", "id":books[index].id})
			books.remove_at(index)
	if playing and available != 0 and C.f32(current_time - last_attempt) > spawn_delay:
		attempt_spawn()
		last_attempt = C.f32(current_time + spawn_delay * C.f32(rng.randf_range(0.0,0.5)))

func attempt_spawn() -> bool:
	var living := 0
	for weapon in weapons:
		if weapon.actor.state == A.State.ALIVE: living += 1
	if collected_total >= collected_threshold or books.size() >= 1 or living <= 1 or spawned_round > 1 or attempts_round > 6: return false
	if possible.size() < 4 and available != P.Power.REVERSE_INPUTS:
		if weapons.all(func(w):return w.powers.history.size() >= possible.size()): return false
	var point = find_spawn_point()
	attempts_round += 1
	if point == null: return false
	spawn_book(point)
	spawned_round += 1
	return true

func find_spawn_point() -> Variant:
	var candidates: Array[int] = []
	var highest := 0
	for index in weapons.size():
		if weapons[index].actor.state != A.State.ALIVE: continue
		var bits := weapons[index].powers.active
		var count := 0
		while bits > 0:
			count += bits & 1
			bits >>= 1
		if count > highest: candidates.clear(); highest = count
		if count == highest: candidates.append(index)
	if candidates.is_empty(): return null
	var chosen := candidates[rng.randi_range(0,candidates.size()-1)]
	var direction := -A.normalized(weapons[chosen].actor.position)
	for attempt in 20:
		var point := Vector2(C.f32(rng.randf_range(3.0, binding.level_size[0] * 0.5 - 3.0) * direction.x), C.f32(rng.randf_range(3.0, binding.level_size[1] * 0.5 - 3.0) * direction.y))
		if spawn_point_clear(point): return point
	return null

func spawn_point_clear(point: Vector2) -> bool:
	for sample in ground.contact_points(point,3.0):
		if not ground.point_on_layer(sample,ground.floor_mask): return false
	if not world.overlaps(point,3.0,int(binding.avoid_close)).is_empty(): return false
	if not world.overlaps(point,8.0,int(binding.avoid_far)).is_empty(): return false
	for disc in collectable_discs:
		if disc.active and not disc.projectile_layer and point.distance_to(disc.position) < 8.0 + disc.collider_radius: return false
	return true

func spawn_book(point: Vector2, power := 0) -> int:
	serial += 1
	books.append({"id":serial,"position":point,"power":power,"collected":false,"overlaps":{},"remaining":0.0,"just_collected":false})
	events.append({"type":"book_spawned","id":serial,"position":point})
	return serial

func native_overlap_phase() -> void:
	var radius := C.f32(C.f32(binding.collider_radius * binding.root_scale) + C.f32(C.f32(1.1) * C.f32(1.33)))
	for book in books:
		if book.collected: continue
		var now: Dictionary = {}
		for index in weapons.size():
			if weapons[index].actor.state == A.State.DEAD: continue
			if weapons[index].actor.position.distance_to(book.position) >= radius: continue
			now[index] = true
			if book.overlaps.has(index):
				var power: int = book.power if book.power != 0 else select_power(weapons[index])
				cycle_power(power)
				weapons[index].powers.start(power)
				book.collected = true
				book.just_collected = true
				book.remaining = C.f32(0.2)
				events.append({"type":"book_collected","id":book.id,"actor":index,"power":power})
				break
		book.overlaps = now

func select_power(weapon: OriginalPlayerWeapons) -> int:
	if available == P.Power.REVERSE_INPUTS: return P.Power.REVERSE_INPUTS
	var useful := possible.duplicate()
	var selected := 0
	while not useful.is_empty():
		selected = useful[rng.randi_range(0,useful.size()-1)]
		if weapon.powers.has_power(selected) or (selected == P.Power.EXTRA_DISC and weapon.held_discs + weapon.powers.pending_discs > 1) or (selected == P.Power.ICE_DISC and weapon.powers.has_power(P.Power.FIRE_DISC)):
			useful.erase(selected)
			continue
		if selected == P.Power.SHRINKING_BOUNDS and not shrinking_allowed:
			useful.erase(selected)
			continue
		break
	if useful.is_empty():
		if possible.is_empty(): return 0
		var index := rng.randi_range(0,possible.size()-1)
		selected = possible[index]
		# Preserve the recovered fallback condition, including its asymmetry.
		if selected == P.Power.SHRINKING_BOUNDS and shrinking_allowed:
			index = (index + rng.randi_range(0,maxi(0,possible.size()-3))) % possible.size()
			selected = possible[index]
	var bits := weapon.powers.active
	var count := 0
	while bits > 0:
		count += bits & 1
		bits >>= 1
	if collected_total > 0 and available & P.Power.REVERSE_INPUTS and rng.randf() * 100.0 < 5.0 + count * 0.25:
		selected = P.Power.REVERSE_INPUTS
	else: collected_total += 1
	return selected

func cycle_power(power: int) -> void:
	if possible.size() > 4:
		possible.erase(power)
		removed.append(power)
		if removed.size() > 4: possible.append(removed.pop_front())
