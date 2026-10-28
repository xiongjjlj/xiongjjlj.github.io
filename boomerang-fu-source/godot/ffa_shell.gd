class_name FfaShell
extends CanvasLayer
## Playable local FFA shell. Source font/portraits/rules are preserved; original
## Unity menu layouts, transitions, replay and awards are not yet reproduced.
enum Mode { LOBBY, READY, PLAYING, PAUSED, SCORES, MATCH_END }
var mode := Mode.LOBBY
var resume_mode := Mode.PLAYING
var game: Node
var root: Control
var backdrop: ColorRect
var lobby: VBoxContainer
var cards: HBoxContainer
var hud: Control
var hud_scores: HBoxContainer
var hud_round: Label
var title: Label
var modal: PanelContainer
var modal_content: VBoxContainer
var start_button: Button
var validation: Label
var goal_label: Label
var count_choice: OptionButton
var difficulty_choice: OptionButton
var length_choice: OptionButton
var device_choices: Array[OptionButton] = []
var character_choices: Array[OptionButton] = []
var portraits: Array[TextureRect] = []
var available: Array = []
var selected_names: Array[String] = ["Avocado", "Watermelon", "Bread", "Banana", "Milk", "Coffee"]
var selected_devices: Array[int] = [-1, -101, -102, -103, -104, -105]
var player_count := 2
var ready_timer := 0.0
var go_timer := 0.0
var focus_return: Control
var last_hud := ""
var pad_list: Array[int] = []

func style(color: Color, radius := 14, padding := 18) -> StyleBoxFlat:
	var result := StyleBoxFlat.new()
	result.bg_color = color
	result.set_corner_radius_all(radius)
	result.set_content_margin_all(padding)
	return result

func label(text: String, size := 24) -> Label:
	var result := Label.new()
	result.text = text
	result.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	result.add_theme_font_size_override("font_size", size)
	return result

func button(text: String, action: Callable) -> Button:
	var result := Button.new()
	result.text = text
	result.custom_minimum_size.y = 46
	result.pressed.connect(func():
		game.audio.play("event:/ui/submit")
		action.call())
	return result

func initialize(scene: Node) -> void:
	game = scene
	layer = 5
	available = game.read_json("res://assets/characters/roster.json").characters
	root = Control.new()
	root.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	root.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(root)
	var theme := Theme.new()
	theme.default_font = load("res://assets/ui/MikadoBold.ttf")
	theme.default_font_size = 22
	for type in ["Button", "OptionButton"]:
		theme.set_stylebox("normal", type, style(Color("31435a"), 10, 12))
		theme.set_stylebox("hover", type, style(Color("52667c"), 10, 12))
		theme.set_stylebox("pressed", type, style(Color("c28739"), 10, 12))
		var focused := style(Color(0,0,0,0), 10, 12)
		focused.set_border_width_all(3); focused.border_color = Color("ffd877")
		theme.set_stylebox("focus", type, focused)
	root.theme = theme
	backdrop = ColorRect.new()
	backdrop.color = Color(0.035,0.065,0.12,0.88)
	backdrop.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	root.add_child(backdrop)
	lobby = VBoxContainer.new()
	lobby.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	lobby.offset_left = 40; lobby.offset_right = -40; lobby.offset_top = 20; lobby.offset_bottom = -26
	lobby.add_theme_constant_override("separation", 8)
	root.add_child(lobby)
	var heading := label("BOOMERANG FU", 50)
	heading.add_theme_color_override("font_color", Color("ffd877"))
	lobby.add_child(heading)
	lobby.add_child(label("FREE FOR ALL", 26))
	var settings := HBoxContainer.new()
	settings.alignment = BoxContainer.ALIGNMENT_CENTER
	settings.add_theme_constant_override("separation", 14)
	lobby.add_child(settings)
	settings.add_child(label("Players", 22))
	count_choice = OptionButton.new()
	for count in range(2,7): count_choice.add_item(str(count), count)
	count_choice.item_selected.connect(func(index):
		player_count = count_choice.get_item_id(index)
		build_cards())
	settings.add_child(count_choice)
	settings.add_child(label("Match", 22))
	length_choice = OptionButton.new()
	for choice in ["Short", "Medium", "Long"]: length_choice.add_item(choice)
	length_choice.select(1)
	length_choice.item_selected.connect(func(_index): validate_lobby())
	settings.add_child(length_choice)
	settings.add_child(label("Bots",22))
	difficulty_choice=OptionButton.new()
	for level in ["Easy","Medium","Hard","Impossible"]:difficulty_choice.add_item(level)
	difficulty_choice.select(1);settings.add_child(difficulty_choice)
	goal_label = label("", 22); settings.add_child(goal_label)
	cards = HBoxContainer.new()
	cards.alignment = BoxContainer.ALIGNMENT_CENTER
	cards.size_flags_vertical = Control.SIZE_EXPAND_FILL
	cards.add_theme_constant_override("separation", 12)
	lobby.add_child(cards)
	validation = label("", 18)
	validation.custom_minimum_size.y = 24
	lobby.add_child(validation)
	var bottom := HBoxContainer.new()
	bottom.alignment = BoxContainer.ALIGNMENT_CENTER
	bottom.add_theme_constant_override("separation", 14)
	lobby.add_child(bottom)
	start_button = button("FIGHT!", start_match)
	start_button.custom_minimum_size.x = 290
	start_button.add_theme_font_size_override("font_size", 28)
	bottom.add_child(start_button)
	var sound: Button = game.audio.make_button()
	sound.set_meta("audio_label", "Sound")
	sound.custom_minimum_size = Vector2(270,46)
	bottom.add_child(sound)
	lobby.add_child(label("Keyboard 1: WASD / Space / J / K     Keyboard 2: Arrows / Enter / / / Shift", 17))
	lobby.add_child(label("Gamepad: left stick or D-pad / A dash / X attack / hold B to throw", 17))
	build_hud()
	modal = PanelContainer.new()
	modal.set_anchors_and_offsets_preset(Control.PRESET_CENTER)
	modal.offset_left = -260; modal.offset_right = 260
	modal.offset_top = -170; modal.offset_bottom = 170
	modal.add_theme_stylebox_override("panel", style(Color("17283d"), 22, 28))
	root.add_child(modal)
	modal_content = VBoxContainer.new()
	modal_content.add_theme_constant_override("separation", 15)
	modal.add_child(modal_content)
	modal.hide()
	title = label("", 92)
	title.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	title.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	title.mouse_filter = Control.MOUSE_FILTER_IGNORE
	title.add_theme_color_override("font_color",Color("ffd877"))
	title.add_theme_color_override("font_outline_color",Color("17283d"))
	title.add_theme_constant_override("outline_size",14)
	root.add_child(title)
	title.hide()
	Input.joy_connection_changed.connect(controllers_changed)
	get_viewport().get_window().focus_exited.connect(on_focus_lost)
	pad_list.assign(Input.get_connected_joypads())
	build_cards()
	show_lobby()

func build_cards() -> void:
	for child in cards.get_children(): child.free()
	device_choices.clear(); character_choices.clear(); portraits.clear()
	for index in player_count:
		var panel := PanelContainer.new()
		panel.custom_minimum_size.x = 184 if player_count <= 5 else 182
		panel.size_flags_vertical = Control.SIZE_SHRINK_CENTER
		panel.add_theme_stylebox_override("panel",style(Color("23384e"),16,12))
		cards.add_child(panel)
		var column := VBoxContainer.new()
		column.add_theme_constant_override("separation",10)
		panel.add_child(column)
		column.add_child(label("PLAYER %d" % (index+1),23))
		var image := TextureRect.new()
		image.custom_minimum_size = Vector2(150,112)
		image.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		image.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		image.texture = load("res://assets/ui/"+selected_names[index]+"-lobbySpriteIdle.png")
		column.add_child(image); portraits.append(image)
		var choice := OptionButton.new()
		for entry in available: choice.add_item(entry.name)
		choice.select(available.find(available.filter(func(c):return c.name==selected_names[index])[0]))
		choice.item_selected.connect(func(item):
			selected_names[index] = available[item].name
			portraits[index].texture = load("res://assets/ui/"+selected_names[index]+"-lobbySpriteIdle.png")
			game.audio.play("event:/ui/alt_character")
			validate_lobby())
		column.add_child(choice); character_choices.append(choice)
		var device := OptionButton.new()
		device.add_theme_font_size_override("font_size",18)
		device.add_item("Keyboard 1", -1); device.add_item("Keyboard 2", -2)
		# OptionButton treats -1 as an automatic ID; use metadata for signed IDs.
		device.set_item_metadata(0,-1); device.set_item_metadata(1,-2)
		device.add_item("Bot");device.set_item_metadata(2,FfaSetup.BOT_SOURCE-index)
		for pad in Input.get_connected_joypads():
			device.add_item("Gamepad %d" % (pad+1)); device.set_item_metadata(device.item_count-1,pad)
		if selected_devices[index]>=0 and not Input.get_connected_joypads().has(selected_devices[index]):
			device.add_item("Gamepad %d" % (selected_devices[index]+1)); device.set_item_metadata(device.item_count-1,selected_devices[index])
		for item in device.item_count:
			if device.get_item_metadata(item)==selected_devices[index]:device.select(item)
		device.item_selected.connect(func(item):
			selected_devices[index] = int(device.get_item_metadata(item))
			validate_lobby())
		column.add_child(device); device_choices.append(device)
	validate_lobby()

func validate_lobby() -> void:
	var message := "Same-screen multiplayer · Level Enclosed"
	var used_devices: Array[int] = []
	var used_names: Array[String] = []
	var valid := true
	for index in player_count:
		if used_names.has(selected_names[index]):
			message = "Choose a different character for each player."
			valid = false
		if used_devices.has(selected_devices[index]):
			message = "Choose a different controller for each player."
			valid = false
		if selected_devices[index]>=0 and not Input.get_connected_joypads().has(selected_devices[index]):
			message = "Connect a gamepad and press a button to join."
			valid = false
		used_devices.append(selected_devices[index]); used_names.append(selected_names[index])
	validation.text = message
	start_button.disabled = not valid
	goal_label.text = "First to %d kills" % FfaSetup.score_goal(length_choice.selected,player_count)

func build_hud() -> void:
	hud = Control.new()
	hud.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	hud.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.add_child(hud)
	hud_round = label("",22)
	hud_round.set_anchors_and_offsets_preset(Control.PRESET_CENTER_TOP)
	hud_round.offset_left = -300; hud_round.offset_right = 300; hud_round.offset_top = 18
	hud_round.add_theme_color_override("font_outline_color",Color("17283d"))
	hud_round.add_theme_constant_override("outline_size",6)
	hud.add_child(hud_round)
	var pause_button := button("II",pause_game)
	pause_button.set_anchors_and_offsets_preset(Control.PRESET_TOP_RIGHT)
	pause_button.offset_left = -74; pause_button.offset_right = -20; pause_button.offset_top = 16
	hud.add_child(pause_button)
	hud_scores = HBoxContainer.new()
	hud_scores.set_anchors_and_offsets_preset(Control.PRESET_CENTER_BOTTOM)
	hud_scores.alignment = BoxContainer.ALIGNMENT_CENTER
	hud_scores.offset_left = -575; hud_scores.offset_right = 575; hud_scores.offset_top = -87; hud_scores.offset_bottom = -18
	hud_scores.add_theme_constant_override("separation",12)
	hud.add_child(hud_scores)

func update_hud() -> void:
	var signature := str(game.round_state.round_number)+str(game.round_state.scores.scores)+str(game.actors.map(func(a):return a.state))
	if signature == last_hud:return
	last_hud = signature
	hud_round.text = "ROUND %d   ·   FIRST TO %d" % [game.round_state.round_number,game.round_state.scores.goal]
	for child in hud_scores.get_children():child.free()
	for index in game.actors.size():
		var panel := PanelContainer.new()
		panel.add_theme_stylebox_override("panel",style(Color(0.055,0.10,0.16,0.84),12,8))
		hud_scores.add_child(panel)
		var row := HBoxContainer.new(); panel.add_child(row)
		var portrait := TextureRect.new()
		portrait.custom_minimum_size=Vector2(50,50)
		portrait.expand_mode=TextureRect.EXPAND_IGNORE_SIZE
		portrait.stretch_mode=TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		portrait.texture=load("res://assets/ui/"+game.setup.roster[index]+"-scoresSpriteIdle.png")
		if game.actors[index].state==OriginalActorMotion.State.DEAD:portrait.modulate=Color(0.55,0.55,0.55)
		row.add_child(portrait)
		var value:=label("P%d  %d"%[index+1,game.round_state.scores.net_score(index)],23)
		row.add_child(value)

func blocks_simulation() -> bool:
	return mode in [Mode.LOBBY,Mode.READY,Mode.PAUSED,Mode.SCORES,Mode.MATCH_END]

func show_lobby() -> void:
	mode=Mode.LOBBY
	game.clock.pause_time()
	game.audio.menu_music=true
	game.audio.music_filter=0.0
	game.clear_local_input()
	game.debug_canvas.hide()
	lobby.show();backdrop.show();hud.hide();modal.hide();title.hide()
	start_button.grab_focus()
	print("BF_FLOW lobby")

func start_match() -> void:
	validate_lobby()
	if start_button.disabled:return
	var names:Array[String]=[];names.assign(selected_names.slice(0,player_count))
	var devices:Array[int]=[];devices.assign(selected_devices.slice(0,player_count))
	game.setup.bot_difficulty=difficulty_choice.selected
	game.configure_match(names,devices,length_choice.selected)
	begin_round()

func begin_round() -> void:
	mode=Mode.READY
	ready_timer=1.0
	if game.round_state.scores.tie_breaker:ready_timer=4.3
	game.clock.pause_time()
	game.audio.menu_music=false
	game.audio.music_filter=0.0
	game.clear_local_input()
	lobby.hide();backdrop.hide();hud.show();modal.hide()
	title.text="TIE BREAKER" if ready_timer>1.0 else "READY?"
	title.show()
	game.audio.play("event:/ui/roundtitle_other" if ready_timer>1.0 else "event:/ui/roundtitle_ready")
	update_hud()
	print("BF_FLOW ready")

func pause_game() -> void:
	if mode not in [Mode.PLAYING,Mode.READY]:return
	resume_mode=mode;mode=Mode.PAUSED
	game.clock.pause_time();game.clear_local_input()
	game.audio.music_filter=1.0
	game.audio.play("event:/ui/show")
	show_modal("PAUSED")
	modal_content.add_child(button("RESUME",resume_game))
	var sound:Button=game.audio.make_button();sound.set_meta("audio_label","Sound")
	modal_content.add_child(sound)
	modal_content.add_child(button("CHARACTER SETUP",show_lobby))
	modal_content.get_child(1).grab_focus()
	print("BF_FLOW paused")

func resume_game() -> void:
	for source in game.setup.sources:
		if source>=0 and not Input.get_connected_joypads().has(source):
			modal_content.get_child(0).text="RECONNECT GAMEPAD %d"%(source+1)
			return
	mode=resume_mode
	game.clock.unpause_time()
	if mode==Mode.READY:game.clock.pause_time()
	game.audio.music_filter=0.0
	game.clear_local_input()
	modal.hide();backdrop.hide()
	if get_viewport().gui_get_focus_owner()!=null:get_viewport().gui_get_focus_owner().release_focus()
	print("BF_FLOW resumed")

func show_modal(heading:String) -> void:
	for child in modal_content.get_children():child.free()
	modal_content.add_child(label(heading,38))
	backdrop.show();modal.show();title.hide()

func show_scores() -> void:
	mode=Mode.MATCH_END if game.round_state.phase==OriginalFfaRound.Phase.MATCH_END else Mode.SCORES
	game.clock.pause_time();game.clear_local_input()
	var winners:Array=game.round_state.scores.winners
	var won:bool=mode==Mode.MATCH_END or winners.size()==1
	show_modal("PLAYER %d WINS!"%(winners[0]+1) if won and not winners.is_empty() else "ROUND COMPLETE")
	for index in game.actors.size():
		var name:String=game.setup.roster[index]
		modal_content.add_child(label("P%d  %s     %d"%[index+1,name,game.round_state.scores.net_score(index)],23))
	var next:=button("REMATCH" if won else ("TIE BREAKER" if winners.size()>1 else "CONTINUE"),continue_round)
	modal_content.add_child(next)
	modal_content.add_child(button("CHARACTER SETUP",show_lobby))
	next.grab_focus()
	update_hud()
	print("BF_FLOW "+("match_end" if won else "scores"))

func continue_round() -> void:
	if mode==Mode.MATCH_END or game.round_state.scores.winners.size()==1:game.reset_motions(true)
	else:game.next_round()
	begin_round()

func frame(delta:float) -> void:
	if mode==Mode.READY:
		var before:=ready_timer
		ready_timer=maxf(0.0,ready_timer-delta)
		if before>2.8 and ready_timer<=2.8:title.text="FIRST KILL WINS"
		if before>1.3 and ready_timer<=1.3:title.hide()
		if before>1.0 and ready_timer<=1.0:
			title.text="READY?";title.show();game.audio.play("event:/ui/roundtitle_ready")
		if ready_timer<=0.0:
			mode=Mode.PLAYING;game.clock.unpause_time();game.clear_local_input()
			if get_viewport().gui_get_focus_owner()!=null:get_viewport().gui_get_focus_owner().release_focus()
			title.text="GO!";title.show();go_timer=0.7
			game.audio.play("event:/ui/roundtitle_go")
			print("BF_FLOW playing")
	elif mode==Mode.PLAYING:
		go_timer=maxf(0.0,go_timer-delta)
		if go_timer<=0.0:title.hide()
		if not game.round_state.simulation_running():show_scores()
		update_hud()

func receive(event:InputEvent) -> bool:
	var pressed:bool=event.is_pressed()
	if event is InputEventKey and pressed and not event.echo:
		if event.physical_keycode==KEY_F9:
			game.debug_canvas.visible=not game.debug_canvas.visible
			return true
		if event.physical_keycode==KEY_ESCAPE:
			if mode==Mode.PAUSED:resume_game()
			elif mode in [Mode.PLAYING,Mode.READY]:pause_game()
			return true
	if event is InputEventJoypadButton and pressed and event.button_index==JOY_BUTTON_START:
		if mode==Mode.PAUSED:resume_game()
		elif mode in [Mode.PLAYING,Mode.READY]:pause_game()
		return true
	# Menu UI events must still reach Godot's focused Controls. Gameplay is gated
	# independently by blocks_simulation(), avoiding Enter starting a dash.
	return false

func controllers_changed(device:int,connected:bool) -> void:
	if mode==Mode.LOBBY:build_cards()
	elif not connected and game.setup.sources.has(device):pause_game()

func on_focus_lost() -> void:
	pause_game()
