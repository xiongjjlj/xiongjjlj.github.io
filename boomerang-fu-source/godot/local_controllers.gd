class_name LocalControllers
extends RefCounted
## Browser input adapter; feeds the original one-frame-late PlayerInput pipeline.
var sources: Array[int] = [-1, -2]
var dash: Array[bool] = []
var attack: Array[bool] = []
var release: Array[bool] = []
var blocked_until_release: Dictionary = {}

func configure(devices: Array[int]) -> void:
	sources.assign(devices)
	dash.resize(sources.size()); dash.fill(false)
	attack.resize(sources.size()); attack.fill(false)
	release.resize(sources.size()); release.fill(false)
	block_held_buttons()

func block_held_buttons() -> void:
	dash.fill(false); attack.fill(false); release.fill(false)
	for index in sources.size():
		if throwing(index): blocked_until_release[index] = true

func receive(event: InputEvent) -> void:
	for index in sources.size():
		if FfaSetup.is_bot(sources[index]):continue
		var code := -1
		var pressed := false
		if event is InputEventKey and sources[index] < 0:
			if event.echo: continue
			var keys: Array = [KEY_SPACE, KEY_J, KEY_K] if sources[index] == -1 else [KEY_ENTER, KEY_SLASH, KEY_SHIFT]
			code = keys.find(event.physical_keycode)
			pressed = event.pressed
		elif event is InputEventJoypadButton and event.device == sources[index]:
			code = [JOY_BUTTON_A, JOY_BUTTON_X, JOY_BUTTON_B].find(event.button_index)
			pressed = event.pressed
		if code == 0 and pressed: dash[index] = true
		elif code == 1 and pressed: attack[index] = true
		elif code == 2 and not pressed:
			if blocked_until_release.has(index): blocked_until_release.erase(index)
			else: release[index] = true

func throwing(index: int) -> bool:
	var source := sources[index]
	if FfaSetup.is_bot(source):return false
	if source < 0: return Input.is_physical_key_pressed(KEY_K if source == -1 else KEY_SHIFT)
	return Input.is_joy_button_pressed(source, JOY_BUTTON_B)

func sample(index: int) -> Dictionary:
	var source := sources[index]
	if FfaSetup.is_bot(source):return {"move":Vector2.ZERO,"dash":false,"attack":false,"held":false,"released":false}
	var move := Vector2.ZERO
	if source < 0:
		var keys: Array = [KEY_D, KEY_A, KEY_W, KEY_S] if source == -1 else [KEY_RIGHT, KEY_LEFT, KEY_UP, KEY_DOWN]
		move = Vector2(float(Input.is_physical_key_pressed(keys[0])) - float(Input.is_physical_key_pressed(keys[1])), float(Input.is_physical_key_pressed(keys[2])) - float(Input.is_physical_key_pressed(keys[3])))
	else:
		move = Vector2(Input.get_joy_axis(source, JOY_AXIS_LEFT_X), -Input.get_joy_axis(source, JOY_AXIS_LEFT_Y))
		var magnitude := move.length()
		move = Vector2.ZERO if magnitude <= 0.2 else move / magnitude * minf(1.0, (magnitude - 0.2) / 0.8)
		var dpad := Vector2(float(Input.is_joy_button_pressed(source, JOY_BUTTON_DPAD_RIGHT)) - float(Input.is_joy_button_pressed(source, JOY_BUTTON_DPAD_LEFT)), float(Input.is_joy_button_pressed(source, JOY_BUTTON_DPAD_UP)) - float(Input.is_joy_button_pressed(source, JOY_BUTTON_DPAD_DOWN)))
		if not dpad.is_zero_approx(): move = dpad
	var held := throwing(index)
	if not held: blocked_until_release.erase(index)
	var result := {"move": OriginalActorMotion.clamp_magnitude(move, 1.0), "dash": dash[index], "attack": attack[index], "held": held and not blocked_until_release.has(index), "released": release[index]}
	dash[index] = false; attack[index] = false; release[index] = false
	return result
