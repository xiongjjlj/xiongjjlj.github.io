class_name OriginalTimekeeper
extends RefCounted
## Port of recovered-source/TimeKeeper.cs. No Godot Engine.time_scale dependency:
## the simulation owns its scaled clock, while UI/browser frame time stays live.
## Fields and method boundaries use binary32; original Mono expressions may
## retain wider intermediate precision. Native-player fixtures are authoritative.

enum GameState { MENU, PAUSED, LOADING, PLAYING, TRANSITION_OUT, TRANSITION_IN }
var last_time_scale := 1.0
var freeze_timer := 0.0
var freeze_frame := false
var target_scale := 1.0
var slow_mo_timer := 0.0
var slow_scale := f32(0.2)
var slow_mo_in_dur := 0.0
var slow_mo_hold_dur := 0.0
var slow_mo_out_dur := 0.0
var slow_mo := false
var slow_mo_is_locked_in := false
var time_counter := 0.0
var unscaled_time_counter := 0.0
var slow_mo_state := ""
var time_scale := 1.0
var audio_scale := 1.0
var vignette := 0.0
var enable_slow_mo := true
var freeze_frames_enabled := true

static func f32(value: float) -> float:
	return PackedFloat32Array([value])[0]

static func ease_in(value: float) -> float:
	return f32(1.0 - f32(cos(f32(f32(f32(PI) / 2.0) * value))))

static func ease_out(value: float) -> float:
	return f32(sin(f32(f32(f32(PI) / 2.0) * value)))

static func lerp32(a: float, b: float, t: float) -> float:
	return f32(a + (b - a) * clampf(t, 0.0, 1.0))

func update(unscaled_delta: float, state := GameState.PLAYING) -> void:
	unscaled_delta = f32(unscaled_delta)
	time_counter = f32(time_counter + f32(unscaled_delta * time_scale))
	unscaled_time_counter = f32(unscaled_time_counter + unscaled_delta)
	if state in [GameState.MENU, GameState.PAUSED, GameState.LOADING]:
		audio_scale = 1.0
		return
	time_scale = target_scale
	if slow_mo_timer < slow_mo_in_dur + slow_mo_hold_dur + slow_mo_out_dur:
		slow_mo = true
		slow_mo_timer = f32(slow_mo_timer + unscaled_delta)
		var amount := 0.0
		if slow_mo_timer < slow_mo_in_dur:
			amount = ease_out(clampf(f32(slow_mo_timer / slow_mo_in_dur), 0.0, 1.0))
			slow_mo_state = "Lerp In"
		elif slow_mo_timer < slow_mo_in_dur + slow_mo_hold_dur or slow_mo_is_locked_in:
			amount = 1.0
			slow_mo_state = "Hold"
		elif not slow_mo_is_locked_in:
			amount = f32(1.0 - clampf(f32((slow_mo_timer - slow_mo_hold_dur - slow_mo_in_dur) / slow_mo_out_dur), 0.0, 1.0))
			amount = ease_in(amount)
			slow_mo_state = "Lerp Out"
		time_scale = lerp32(target_scale, slow_scale, amount)
		vignette = clampf(amount, 0.0, 1.0)
	elif slow_mo and not slow_mo_is_locked_in:
		stop_slow_mo()
	audio_scale = time_scale
	if freeze_timer > 0.0:
		freeze_frame = true
		freeze_timer = f32(freeze_timer - unscaled_delta)
		time_scale = 0.0
	elif freeze_frame:
		freeze_frame = false
		time_scale = last_time_scale

func stop_slow_mo() -> void:
	slow_mo_is_locked_in = false
	slow_mo = false
	slow_mo_timer = 3.4028234663852886e38
	slow_mo_in_dur = 0.0
	slow_mo_hold_dur = 0.0
	slow_mo_out_dur = 0.0
	time_scale = target_scale

func slow_mo_effect(hold_duration: float, in_duration := 0.1, scale := 0.2, force := false) -> void:
	hold_duration = f32(hold_duration)
	in_duration = f32(in_duration)
	scale = f32(scale)
	if (not force and not enable_slow_mo) or slow_mo_is_locked_in:
		return
	if scale < time_scale or time_scale == 0.0:
		slow_scale = scale
	if not slow_mo:
		slow_mo_in_dur = in_duration
		slow_mo_timer = 0.0
	slow_mo_out_dur = hold_duration
	if slow_mo:
		if slow_mo_timer > f32(f32(slow_mo_in_dur + slow_mo_hold_dur) - f32(slow_mo_hold_dur * f32(0.15))):
			slow_mo_timer = slow_mo_in_dur
			slow_mo_hold_dur = f32(hold_duration * f32(0.4))
		slow_mo_hold_dur = f32(slow_mo_hold_dur + f32(hold_duration * f32(0.2)))
	else:
		slow_mo_hold_dur = f32(slow_mo_hold_dur + hold_duration)

func slow_mo_time_warp(hold_duration: float, in_duration := 0.1, out_duration := 1.0, scale := 0.2) -> void:
	if not slow_mo_is_locked_in:
		slow_scale = f32(scale)
		slow_mo_in_dur = f32(in_duration)
		slow_mo_out_dur = f32(out_duration)
		hold_duration = f32(f32(hold_duration) + f32(slow_mo_in_dur + slow_mo_out_dur))
		slow_mo_timer = hold_duration
		slow_mo_hold_dur = hold_duration

func end_match() -> void:
	slow_mo_effect(10.0, 0.5, 0.015, true)
	slow_mo_is_locked_in = true

func end_golden_disc_round() -> void:
	slow_mo_effect(10.0, 0.5, 0.2, true)
	slow_mo_is_locked_in = true

func pause_time() -> void:
	if time_scale != 0.0:
		last_time_scale = time_scale
	time_scale = 0.0

func unpause_time() -> void:
	time_scale = last_time_scale

func freeze(duration: float) -> void:
	if freeze_frames_enabled:
		last_time_scale = time_scale
		freeze_timer = f32(duration)

func set_time_scale(scale: float) -> void:
	stop_slow_mo()
	time_scale = f32(scale)
	vignette = f32(scale)

func reset_time_scale() -> void:
	stop_slow_mo()
	target_scale = 1.0
	slow_scale = f32(0.2)
	time_scale = 1.0
	vignette = 0.0

func snapshot() -> Dictionary:
	return {"time": time_counter, "unscaled": unscaled_time_counter,
		"scale": time_scale, "audio": audio_scale, "vignette": vignette,
		"freeze": freeze_frame, "slow": slow_mo, "state": slow_mo_state}
