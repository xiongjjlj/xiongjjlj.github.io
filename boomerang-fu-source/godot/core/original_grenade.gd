class_name OriginalGrenade
extends RefCounted
## Grenade.OnEnable / RunExplosiveTimer / Blink. Pool reuse retains blink state.
const C = preload("res://core/original_timekeeper.gd")
var active := false
var timer := 0.0
var blink_on := false
var blink_timer := 0.0
var blink_rate := C.f32(0.25)
var blink_counter := 0

func arm() -> void:
	active = true
	blink_rate = C.f32(0.25)
	blink_counter = 0
	timer = C.f32(1.33)

func step(delta: float) -> bool:
	if not active: return false
	delta = C.f32(delta)
	var exploded := false
	if timer > 0.0:
		timer = C.f32(timer - delta)
		blink_rate = C.lerp32(C.f32(0.03), C.f32(0.25), C.ease_in(C.f32(timer / C.f32(1.5))))
	else:
		timer = 0.0
		active = false
		exploded = true
	# The original still runs Blink after Explode deactivates its GameObject.
	if blink_timer < blink_rate:
		blink_timer = C.f32(blink_timer + delta)
	else:
		blink_on = not blink_on
		if blink_on: blink_counter += 1
		blink_timer = 0.0
	return exploded
