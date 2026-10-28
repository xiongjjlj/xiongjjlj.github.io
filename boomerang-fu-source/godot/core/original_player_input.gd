class_name OriginalPlayerInput
extends RefCounted
## PlayerInput.Update's normal FFA actions and eight-entry oldest-first buffer.
const C = preload("res://core/original_timekeeper.gd")
var weapon: OriginalPlayerWeapons
var buffer: Array[Dictionary] = []
var actions: Array[String] = []
var view_position := Vector2.ZERO
var view_rotation := PI
var snapped := false
var current_time := 0.0

func initialize(inventory: OriginalPlayerWeapons) -> void:
	weapon = inventory

func attempt(kind: String) -> bool:
	var actor := weapon.actor
	weapon.display_position = view_position
	weapon.display_rotation = view_rotation
	var before := actor.position
	var accepted := false
	if kind == "throw": accepted = weapon.try_throw()
	else:
		actor.position = view_position
		accepted = weapon.try_attack() if kind == "attack" else weapon.teleport.dash(current_time)
		if accepted:
			snapped = true
			view_position = actor.position
			view_rotation = actor.rotation_z
		else: actor.position = before
	if accepted: actions.append(kind)
	return accepted

func remember(kind: String, time: float) -> void:
	if buffer.size() == 8: buffer.pop_front()
	buffer.append({"kind": kind, "time": time})

func process_frame(attack: bool, held: bool, released: bool, dash: bool, delta: float, time: float, position: Vector2, rotation: float) -> void:
	actions.clear()
	snapped = false
	current_time = time
	view_position = position
	view_rotation = rotation
	weapon.input_held = held
	var actor := weapon.actor
	if actor.state == OriginalActorMotion.State.ALIVE:
		if attack:
			if attempt("attack"): buffer.clear()
			else: remember("attack", time)
		if released and not weapon.input_blocked:
			if attempt("throw"): buffer.clear()
			elif weapon.owned_discs.is_empty(): remember("throw", time)
		elif released: weapon.input_blocked = false
		if weapon.input_held and not weapon.input_blocked: weapon.aim(delta)
	if actor.state != OriginalActorMotion.State.DEAD and dash:
		if attempt("dash"): buffer.clear()
		else: remember("dash", time)
	if released: actor.stop_aiming()
	# Original resets inputThrow before the retry guard, but keeps attack/dash.
	if attack or dash or actor.state != OriginalActorMotion.State.ALIVE: return
	for failed in buffer:
		var limit := C.f32(0.25 if failed.kind == "dash" else 0.35)
		if C.f32(time - failed.time) > limit: continue
		if failed.kind == "throw" and (weapon.input_blocked or not weapon.owned_discs.is_empty()): continue
		if attempt(failed.kind):
			buffer.clear()
			return
