extends Node3D
## Original-asset import and Web export validation. Not a complete playable match.
const ROOT := "res://assets/original/"
var manifest: Dictionary
var active_scene: Dictionary
var active_scene_index := 0
var nodes: Dictionary = {}
var transforms: Dictionary = {}
var mesh_cache: Dictionary = {}
var material_cache: Dictionary = {}
var scene_materials := OriginalSceneMaterials.new()
var clock := OriginalTimekeeper.new()
var camera: Camera3D
var status: Label
var camera_yaw := 0.0
var camera_pitch := 0.9
var distance := 100.0
var center := Vector3.ZERO
var mesh_count := 0
var spawned := false
var collider_instances: Array[MeshInstance3D] = []
var characters: Array[OriginalCharacterPreview] = []
var actors: Array[OriginalActorMotion] = []
var actor_environments: Array[OriginalActorEnvironment] = []
var ground_queries := OriginalGroundQueries.new()
var collision_world := OriginalCollisionWorld.new()
const Level3Behaviors = preload("res://core/original_level3_behaviors.gd")
var level_behaviors = Level3Behaviors.new()
var movement_enabled := true
var movement_toggle: CheckButton
var physics_accumulator := 0.0
var pending_dash := [false, false]
var input_dash_states := [false, false]
var player_inputs: Array[OriginalPlayerInput] = []
var pending_attack := [false, false]
var input_attack_states := [false, false]
var current_clips := ["idle", "idle"]
var simulated_ticks := 0
var input_states: Array[Vector2] = [Vector2.ZERO, Vector2.ZERO]
var previous_positions: Array[Vector2] = []
var previous_rotations: Array[float] = []
var reported_stun_states := [false, false]
var weapons: Array[OriginalPlayerWeapons] = []
var flying_discs: Array[Dictionary] = []
var explosions: Array[Dictionary] = []
var cell_grid := OriginalCellGrid.new()
var cell_preview := OriginalCellPreview.new()
var actor_elements: Array[OriginalActorElements] = []
var disguises:Array[OriginalDisguise]=[]
var disguise_views:Array[OriginalDisguisePreview]=[]
var disguise_inputs:Dictionary
var boundary_inputs:Dictionary
var boundary:=OriginalLevelBoundary.new()
var boundary_view:=OriginalBoundaryPreview.new()
var actor_bounds:Array[OriginalActorBounds]=[]
var cell_rng := RandomNumberGenerator.new()
var explosion_parameters: Dictionary = {}
var explosion_corners := PackedVector2Array()
var weapon_bindings: Dictionary
var input_throw_states := [false, false]
var input_release_states := [false, false]
var pending_throw_release := [false, false]
var combat: OriginalCombatWorld
var round_state := OriginalFfaRound.new()
var body_bindings: Dictionary
var next_round_button: Button
var power_labels: Array[Label] = []
var shield_views: Array[Node3D] = []
var powerup_world := OriginalPowerupWorld.new()
var book_views: Dictionary = {}
var camera_rig := OriginalCameraRig.new()
var camera_auto := true
var actor_render_positions: Array[Vector3] = []
var actor_render_rotations:Array[float]=[]
var decoys:=OriginalDecoyWorld.new()
var decoy_views:Dictionary={}
var audio:=OriginalAudio.new()
var setup := FfaSetup.new()
var bots := OriginalPlayerAiWorld.new()
var controllers := LocalControllers.new()
var shell: FfaShell
var debug_canvas: CanvasLayer

func _ready() -> void:
	audio.initialize()
	manifest = read_json(ROOT + "manifest.json")
	active_scene_index = requested_scene_index()
	active_scene = manifest.scenes[active_scene_index]
	scene_materials.initialize(ROOT)
	disguise_inputs=read_json(ROOT+"disguise-inputs.json")
	var scene_key := str(active_scene.file).get_file().get_basename()
	var scene_boundary_path := ROOT + "boundary-inputs-" + scene_key + ".json"
	boundary_inputs=read_json(scene_boundary_path if FileAccess.file_exists(scene_boundary_path) else ROOT+"boundary-inputs.json")
	decoys.inputs=read_json(ROOT+"decoy-inputs.json")
	var navigation_world := OriginalNavigationWorld.new()
	decoys.navigation=navigation_world
	var navigation_path := ROOT+"navigation/"+scene_key+".json"
	decoys.navigation.initialize(read_json(navigation_path))
	for item in JSON.parse_string(FileAccess.get_file_as_string(ROOT+"explosion-inputs.json")):
		if item.script == "Explosion": explosion_parameters[item.name] = item.data
	load_original_scene(active_scene.file)
	# Every imported Unity level now carries a decoded behaviour sidecar.  Keep
	# the same deterministic adapter for shared obstacle families (moving
	# obstacles, switches, wobbly props, destructibles and debris), while
	# leaving room for map-specific adapters to opt in later.
	var behavior_path := ROOT + scene_key + "/behaviors.json"
	if FileAccess.file_exists(behavior_path):
		level_behaviors.initialize(read_json(behavior_path), nodes, collision_world, decoys.rng)
	navigation_world.bind_moving_floors(level_behaviors.moving_floors)
	ground_queries.world = collision_world
	ground_queries.water_mask = 16 # Original serialized Water layer (index 4).
	create_characters()
	add_child(cell_preview)
	var original_material:ShaderMaterial=scene_materials.materials[0]
	var sun:Vector3=original_material.get_shader_parameter("source_sun_direction")
	var tint:Vector3=original_material.get_shader_parameter("source_tint")
	var fog:Vector3=original_material.get_shader_parameter("source_fog_color")
	cell_preview.initialize(scene_materials,sun)
	for index in actors.size():
		var view:=OriginalDisguisePreview.new()
		view.name="Disguise%d"%index
		add_child(view)
		view.initialize(disguise_inputs,scene_materials)
		disguise_views.append(view)
	scene_materials.configure_lighting(sun,Color(tint.x,tint.y,tint.z),Color(fog.x,fog.y,fog.z))
	add_child(boundary_view)
	boundary_view.initialize(boundary_inputs)
	create_ui()
	if DisplayServer.get_name() != "headless":
		shell = FfaShell.new()
		add_child(shell)
		shell.initialize(self)
	print("BF_GODOT_READY " + JSON.stringify({"scene": active_scene.name, "scene_index": active_scene_index,
		"nodes": nodes.size(), "meshes": mesh_count, "missing": manifest.missing_references.size()}))

func requested_scene_index() -> int:
	var requested := ""
	for argument in OS.get_cmdline_user_args():
		if argument.begins_with("--map="): requested = argument.trim_prefix("--map=").to_lower()
	if OS.has_feature("web"):
		var browser_value = JavaScriptBridge.eval("new URLSearchParams(window.location.search).get('map')", true)
		if browser_value != null and str(browser_value) != "": requested = str(browser_value).to_lower()
	if requested.is_empty(): return 0
	var aliases := {"slide":"levelslidetraps", "level3":"levelslidetraps"}
	var wanted := str(aliases.get(requested, requested)).replace("_", "").replace("-", "")
	for index in manifest.scenes.size():
		var scene_name := str(manifest.scenes[index].name).to_lower()
		var compact := scene_name.replace("_", "").replace("-", "")
		var file_key := str(manifest.scenes[index].file).get_file().get_basename().to_lower()
		if wanted == compact or wanted == file_key: return index
	return 0

func read_json(path: String) -> Dictionary:
	return JSON.parse_string(FileAccess.get_file_as_string(path))

func create_characters() -> void:
	weapon_bindings = read_json("res://assets/characters/weapon-bindings.json")
	body_bindings = read_json("res://assets/characters/body-bindings.json")
	current_clips.resize(setup.roster.size())
	current_clips.fill("idle")
	controllers.configure(setup.sources)
	for index in setup.roster.size():
		var character_name: String = setup.roster[index]
		var actor := OriginalCharacterPreview.new()
		actor.name = character_name + str(index)
		add_child(actor)
		var base := "res://assets/characters/" + character_name
		actor.configure(base + ".glb", base + "-idle.json")
		# Actor prefab scale and TiltActorTowardsCamera, actor Z=180 faces camera.
		actor.scale = Vector3.ONE * 1.3300000429153442
		actor.quaternion = Quaternion(Vector3.RIGHT, deg_to_rad(-25.0)) * Quaternion(Vector3(0, 0, 1), deg_to_rad(90.0))
		actor.position = Vector3(-3.5 + index * 7.0, -8.4, 0.25)
		characters.append(actor)
		var shield: Node3D = load("res://assets/characters/Shield.glb").instantiate()
		actor.add_child(shield)
		shield.position = Vector3(0, 0, 0.75)
		shield.visible = false
		# Source sphere mesh and scale are preserved. Its distortion shader is
		# still a preview material, independently of the source damage rules.
		var shield_material := StandardMaterial3D.new()
		shield_material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
		shield_material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
		shield_material.albedo_color = Color(0.55, 0.88, 1.0, 0.2)
		for mesh in shield.find_children("*", "MeshInstance3D", true, false): mesh.material_override = shield_material
		shield_views.append(shield)
	reset_motions()
	distance = 28.0
	center = Vector3(0, -8, 1)
	print("BF_MOTION_READY " + JSON.stringify({"shapes": collision_world.shapes.size(),
		"fixed_delta": actors[0].fixed_delta, "radius": actors[0].radius}))

func reset_motions(new_match := true) -> void:
	if level_behaviors.initialized: level_behaviors.reset()
	if decoys.world!=null:decoys.reset_round()
	for item in decoy_views.values():item.root.queue_free()
	decoy_views.clear()
	for view in book_views.values(): view.queue_free()
	book_views.clear()
	for character in characters: character.set_burnt(false)
	cell_preview.clear()
	actor_elements.clear()
	actor_bounds.clear()
	boundary=OriginalLevelBoundary.new()
	boundary.initialize(boundary_inputs)
	disguises.clear()
	for view in disguise_views:view.reset()
	cell_grid = OriginalCellGrid.new()
	cell_grid.world = collision_world
	cell_grid.current_time = clock.time_counter
	cell_rng.seed = 0xB00F1
	var saved_powers: Array[Dictionary] = []
	var saved_control_time: Array[float] = []
	var saved_teleports: Array[Dictionary] = []
	for weapon in weapons: saved_teleports.append({"count":0 if new_match else weapon.teleport.count,"timestamp":weapon.teleport.last_timestamp})
	if not new_match:
		for weapon in weapons: saved_powers.append(weapon.powers.round_snapshot())
		for actor in actors: saved_control_time.append(actor.time_controlling_disc)
	for explosion in explosions: explosion.view.queue_free()
	explosions.clear()
	for disc in flying_discs: disc.view.queue_free()
	flying_discs.clear()
	weapons.clear()
	player_inputs.clear()
	pending_attack.resize(characters.size()); pending_attack.fill(false)
	input_attack_states.resize(characters.size()); input_attack_states.fill(false)
	input_throw_states.resize(characters.size()); input_throw_states.fill(false)
	input_release_states.resize(characters.size()); input_release_states.fill(false)
	pending_throw_release.resize(characters.size()); pending_throw_release.fill(false)
	collision_world.clear_actors()
	actor_environments.clear()
	actors.clear()
	previous_positions.clear()
	previous_rotations.clear()
	actor_render_positions.clear()
	actor_render_rotations.clear()
	var viewport_size := get_viewport().get_visible_rect().size
	camera_rig.initialize(viewport_size.x / viewport_size.y)
	physics_accumulator = 0.0
	input_states.resize(characters.size()); input_states.fill(Vector2.ZERO)
	pending_dash.resize(characters.size()); pending_dash.fill(false)
	input_dash_states.resize(characters.size()); input_dash_states.fill(false)
	reported_stun_states.resize(characters.size()); reported_stun_states.fill(false)
	var spawn_order: Array[int] = []
	if setup.original_spawns: spawn_order = setup.spawn_indices(characters.size(), round_state.scores != null and round_state.scores.tie_breaker and not new_match, [] if round_state.scores == null else round_state.scores.winners)
	var scene_key := str(active_scene.file).get_file().get_basename()
	var spawn_path := ROOT + "ffa-spawn-inputs-" + scene_key + ".json"
	var spawn_input: Dictionary = read_json(spawn_path if FileAccess.file_exists(spawn_path) else ROOT + "ffa-spawn-inputs.json")
	for index in characters.size():
		var motion := OriginalActorMotion.new()
		if index < saved_control_time.size(): motion.time_controlling_disc = saved_control_time[index]
		motion.position = Vector2(-3.5 + index * 7.0, -8.0)
		if setup.original_spawns and spawn_order[index] >= 0:
			var point: Vector3 = transforms[int(spawn_input.transform_ids[spawn_order[index]])].node.global_position
			motion.position = Vector2(point.x, point.y)
		motion.world = collision_world
		actors.append(motion)
		characters[index].position = Vector3(motion.position.x, motion.position.y - 0.4, 0.25)
		var environment := OriginalActorEnvironment.new()
		environment.initialize(motion,ground_queries)
		actor_environments.append(environment)
		collision_world.register_actor(motion)
		previous_positions.append(motion.position)
		previous_rotations.append(motion.rotation_z)
		actor_render_positions.append(Vector3(motion.position.x,motion.position.y,0))
		actor_render_rotations.append(motion.rotation_z)
		camera_rig.add_target("player_%d" % index,actor_render_positions[index],Vector3(motion.radius*2,motion.radius*2,0),1.0,1.0)
		var inventory := OriginalPlayerWeapons.new()
		inventory.initialize(motion, collision_world)
		if index < saved_teleports.size():
			inventory.teleport.count = saved_teleports[index].count
			inventory.teleport.last_timestamp = saved_teleports[index].timestamp
		if index < saved_powers.size(): inventory.powers.restore_round(saved_powers[index])
		weapons.append(inventory)
		var disguise:=OriginalDisguise.new()
		disguise.initialize(inventory)
		disguise.available.assign(disguise_inputs.available)
		for item in disguise_inputs.obstacles:
			if not nodes.has(int(item.node)): continue
			var obstacle:Node3D=nodes[int(item.node)]
			var p:=obstacle.global_position
			disguise.obstacles.append({"type":int(item.type),"active":obstacle.visible,"position":Vector3(p.x,p.y,-p.z)})
		disguises.append(disguise)
		var input_controller := OriginalPlayerInput.new()
		input_controller.initialize(inventory)
		player_inputs.append(input_controller)
		characters[index].set_held_discs(weapon_bindings[characters[index].model_name], inventory.held_discs,inventory.ghost_discs)
		var binding: Dictionary = weapon_bindings[characters[index].model_name]
		characters[index].set_sliced(body_bindings[characters[index].model_name], false)
		characters[index].set_squished(body_bindings[characters[index].model_name], false)
		characters[index].action_steps.clear()
		characters[index].set_clip("res://assets/characters/" + characters[index].model_name + "-idle.json")
		current_clips[index] = "idle"
		var rgba: Dictionary = binding.color
		for key in ["rightDisc", "leftDisc"]:
			var target: Dictionary = characters[index].targets.get(binding[key], {})
			if target.has("node"):
				OriginalDiscPreview.color_meshes(target.node, Color(rgba.r, rgba.g, rgba.b, rgba.a))
	round_state.initialize(actors, new_match)
	if new_match: round_state.scores.goal = FfaSetup.score_goal(setup.match_length, actors.size())
	if setup.original_spawns:
		for index in actors.size():
			if spawn_order[index] < 0:
				actors[index].state = OriginalActorMotion.State.DEAD
				actors[index].appearance_visible = false
				actors[index].physics_enabled = false
				collision_world.unregister_actor(actors[index])
		round_state.sync_scores()
	combat = OriginalCombatWorld.new()
	combat.initialize(actors, weapons, collision_world, round_state)
	combat.clock = clock
	decoys.actors=actors
	decoys.weapons=weapons
	decoys.world=collision_world
	decoys.boundary=boundary
	decoys.grid=cell_grid
	decoys.combat=weakref(combat)
	decoys.ground=OriginalGroundQueries.new()
	decoys.ground.world=collision_world
	for component in decoys.inputs.source_components:
		if component.script.ends_with("::Decoy"):decoys.ground.floor_mask=int(component.data.floorLayers.m_Bits)
	combat.decoy_world=weakref(decoys)
	var cell_ground := OriginalGroundQueries.new()
	cell_ground.world = collision_world
	cell_grid.fire_spawn_allowed = func(point:Vector2): return cell_ground.point_on_layer(point,1073807360) and not cell_ground.point_on_layer(point,603979776)
	cell_grid.ice_spawn_allowed = func(point:Vector2): return cell_ground.point_on_layer(point,1073807360)
	for index in actors.size():
		var element := OriginalActorElements.new()
		element.actor = actors[index]
		element.inventory = weapons[index]
		element.grid = cell_grid
		element.combat = combat
		element.index = index
		element.previous_position = actors[index].position
		element.previous_z = actors[index].source_z
		actors[index].elements_hook = weakref(element)
		actor_elements.append(element)
		var bounds:=OriginalActorBounds.new()
		bounds.actor=actors[index]
		bounds.boundary=boundary
		actor_bounds.append(bounds)
	powerup_world.weapons = weapons
	if powerup_world.binding.is_empty():
		var power_binding := read_json("res://assets/powers/powerup-bindings-level3.json" if active_scene.name == "LevelSlideTraps" else "res://assets/powers/powerup-bindings.json")
		powerup_world.initialize(weapons, collision_world, power_binding)
	powerup_world.begin_round(round_state.round_number, clock.time_counter, new_match)
	level_behaviors.moving_floors.start_all()
	bots.initialize(self)
	clock.stop_slow_mo()
	if next_round_button != null: next_round_button.visible = false
	audio.round_started(new_match,actors)
	print("BF_ROUND_START " + JSON.stringify({"round": round_state.round_number, "goal": round_state.scores.goal}))

func next_round() -> void:
	if round_state.phase == OriginalFfaRound.Phase.MATCH_END:
		reset_motions(true)
	elif round_state.phase == OriginalFfaRound.Phase.SCORES:
		if round_state.scores.winners.size() == 1:
			round_state.phase = OriginalFfaRound.Phase.MATCH_END
		else:
			if round_state.scores.winners.size() > 1: round_state.scores.tie_breaker = true
			round_state.round_number += 1
			reset_motions(false)

func set_character_clip(clip_name: String) -> void:
	movement_enabled = false
	if movement_toggle != null: movement_toggle.set_pressed_no_signal(false)
	for character in characters:
		character.action_steps.clear()
		character.set_clip("res://assets/characters/" + character.model_name + "-" + clip_name + ".json")
	current_clips.fill(clip_name)

func update_movement(scaled_delta: float) -> void:
	level_behaviors.moving_floors.now = clock.time_counter
	physics_accumulator += minf(scaled_delta, 0.3333333432674408)
	var steps := 0
	while physics_accumulator >= actors[0].fixed_delta and steps < 20 and round_state.simulation_running():
		for index in actors.size():
			previous_positions[index] = actors[index].position
			previous_rotations[index] = actors[index].rotation_z
			actors[index].input_move = input_states[index]
			actor_environments[index].before_fixed_step()
			consume_environment_events(index)
			var ending_dash:bool=actors[index].is_dashing and actors[index].dash_timer>=actors[index].dash_duration
			actors[index].fixed_step()
			if ending_dash and not actors[index].is_dashing and not actor_environments[index].over_hazard(actors[index].position):audio.play("event:/combat/dash_end",actors[index].position)
			weapons[index].fixed_step()
		decoys.fixed_step()
		step_discs()
		level_behaviors.fixed_step(actors[0].fixed_delta, actors, flying_discs, combat)
		for index in actor_environments.size():
			var environment: OriginalActorEnvironment = actor_environments[index]
			if not environment.parent_floor.is_empty():
				previous_positions[index] = level_behaviors.moving_floors.carry(environment.parent_floor,previous_positions[index],actors[index].source_z)
				previous_rotations[index] += level_behaviors.moving_floors.rotation_delta(environment.parent_floor)
			level_behaviors.moving_floors.update_actor(environment)
		level_behaviors.moving_floors.update_discs(flying_discs)
		collision_world.sync_actors()
		physics_accumulator -= actors[0].fixed_delta
		steps += 1
		simulated_ticks += 1
	# Serialized MonoScript order: interpolation -100/-50, PlayerInput -3,
	# Player -2, RewiredController/TimeKeeper 0. See unity-script-order.json.
	decoys.prepare_render(OriginalTimekeeper.f32(clampf(physics_accumulator/actors[0].fixed_delta,0.0,1.0)))
	for index in actors.size():
		var alpha:=OriginalTimekeeper.f32(clampf(physics_accumulator/actors[index].fixed_delta,0.0,1.0))
		var point:=previous_positions[index].lerp(actors[index].position,alpha)
		actor_render_positions[index]=Vector3(point.x,point.y,actors[index].source_z)
		actor_render_rotations[index]=lerp_angle(previous_rotations[index],actors[index].rotation_z,alpha)
	bots.refresh()
	bots.phase(scaled_delta,0) # PlayerAI -8.
	decoys.ai_phase(scaled_delta,0) # DecoyAI -5.
	bots.phase(scaled_delta,1) # StateMachineRunner -4.
	decoys.ai_phase(scaled_delta,1)
	for index in actors.size():
		var actor := actors[index]
		var alpha := OriginalTimekeeper.f32(clampf(physics_accumulator / actor.fixed_delta, 0.0, 1.0))
		var display_position := Vector2(actor_render_positions[index].x,actor_render_positions[index].y)
		actor_render_positions[index] = Vector3(display_position.x,display_position.y,0)
		var display_rotation := actor_render_rotations[index]
		if round_state.simulation_running():
			var inputs := player_inputs[index]
			bots.before_input(index)
			inputs.process_frame(input_attack_states[index], input_throw_states[index], input_release_states[index], input_dash_states[index], scaled_delta, clock.time_counter, display_position, display_rotation)
			bots.after_input(index)
			if inputs.snapped:
				display_position = inputs.view_position
				previous_positions[index] = display_position
				actor_render_positions[index] = Vector3(display_position.x,display_position.y,actor.source_z)
				previous_rotations[index] = actor.rotation_z
				display_rotation = actor.rotation_z
			for teleport_event in weapons[index].teleport.events:
				audio.play("event:/powerups/teleport",actors[index].position)
				print("BF_TELEPORT " + JSON.stringify({"actor":index,"from":[teleport_event.from.x,teleport_event.from.y],"to":[teleport_event.to.x,teleport_event.to.y],"count":weapons[index].teleport.count,"tick":simulated_ticks}))
			consume_combat_events()
			for action in inputs.actions:
				audio.action(index,action,self)
				if action=="dash":decoys.after_successful_dash(index)
				if action == "attack" and not actor.is_frozen:
					characters[index].play_melee(weapons[index].action_hand)
					current_clips[index] = "melee"
				if action != "throw": print("BF_" + action.to_upper() + " " + JSON.stringify({"actor": index, "tick": simulated_ticks}))
		weapons[index].teleport.events.clear()
		actor_render_rotations[index]=display_rotation
		actor.frame_step(scaled_delta)
		combat.run_killer_timer(index,scaled_delta)
		weapons[index].frame_step(scaled_delta, false)
		actor_environments[index].frame_step(scaled_delta)
		actor_elements[index].timers(scaled_delta)
		actor_elements[index].player_frame(scaled_delta)
		actor_elements[index].last_teleport = weapons[index].teleport.last_timestamp
		actor_elements[index].movement_frame(scaled_delta,clock.time_counter)
		consume_element_events(index)
		consume_environment_events(index)
		weapons[index].powers.update_time_immune(scaled_delta,clock.time_scale,clock.target_scale,round_state.simulation_running())
		if actor.state!=OriginalActorMotion.State.DEAD:
			actor_bounds[index].frame_step(scaled_delta,display_position)
			if actor_bounds[index].death_requested:
				combat.boundary_owner=boundary.owner
				combat.kill(index,"outofbounds")
				consume_combat_events()
		elif actor_bounds[index].outside:
			actor_bounds[index].outside=false
			actor_bounds[index].stop_effect()
		weapons[index].powers.frame_step(scaled_delta)
		disguises[index].frame_step(scaled_delta,round_state.simulation_running())
		characters[index].set_held_discs(weapon_bindings[characters[index].model_name], weapons[index].held_discs,weapons[index].ghost_discs)
		spawn_discs(index, display_rotation)
		if reported_stun_states[index] != actor.is_stunned:
			reported_stun_states[index] = actor.is_stunned
			print("BF_STUN " + JSON.stringify({"actor": index, "stunned": actor.is_stunned, "tick": simulated_ticks}))
		characters[index].position = Vector3(display_position.x, display_position.y - 0.4, 0.25)
		characters[index].quaternion = Quaternion(Vector3.RIGHT, deg_to_rad(-25.0)) * Quaternion(Vector3(0, 0, 1), display_rotation - PI / 2.0)
		var clip_name := "death" if actor.state == OriginalActorMotion.State.DEAD else ("walk" if actor.move_speed_parameter > 0.25 else "idle")
		if actor.state == OriginalActorMotion.State.DEAD and actor.is_burnt: clip_name = "burnt"
		elif actor.is_frozen: clip_name = "frozen"
		if actor.is_stunned and actor.state != OriginalActorMotion.State.DEAD: clip_name = "stunned"
		if actor.is_falling: clip_name = "fall"
		if actor.state == OriginalActorMotion.State.DEAD or actor.is_stunned or actor.is_falling: characters[index].action_steps.clear()
		if characters[index].action_steps.is_empty() and clip_name != current_clips[index]:
			characters[index].set_clip("res://assets/characters/" + characters[index].model_name + "-" + clip_name + ".json")
			current_clips[index] = clip_name
	decoys.actor_phase(scaled_delta)
	decoys.input_phase()
	bots.phase(scaled_delta,2) # AstarPath callback phase, order 0.
	for index in actors.size():
		if bots.members.has(index):continue
		var sampled := controllers.sample(index)
		input_states[index] = sampled.move
		actors[index].raw_input_move = sampled.move
		input_dash_states[index] = sampled.dash or pending_dash[index]
		input_attack_states[index] = sampled.attack or pending_attack[index]
		input_throw_states[index] = sampled.held
		input_release_states[index] = sampled.released or pending_throw_release[index]
	pending_dash.fill(false)
	pending_attack.fill(false)
	pending_throw_release.fill(false)
	for item in flying_discs:
		item.motion.frame_step(scaled_delta)
		var controlled: bool = item.motion.movement == OriginalDiscMotion.Movement.CONTROLLED
		if controlled != bool(item.get("controlled",false)):
			item.controlled = controlled
			print("BF_CONTROLLED_STATE " + JSON.stringify({"actor":item.player,"controlled":controlled,"tick":simulated_ticks}))
		var alpha := OriginalTimekeeper.f32(clampf(physics_accumulator / item.motion.fixed_delta, 0.0, 1.0))
		var rendered: Vector2 = item.previous_position.lerp(item.motion.position, alpha)
		item.view.position = Vector3(rendered.x, rendered.y, -item.motion.source_z)

func spawn_discs(index: int, rotation: float) -> void:
	for motion in weapons[index].spawned_discs:
		if not motion.active: continue
		combat.bind_disc(motion)
		var view := OriginalDiscPreview.new()
		add_child(view)
		var rgba: Dictionary = weapon_bindings[characters[index].model_name].color
		view.initialize(Color(rgba.r, rgba.g, rgba.b, rgba.a))
		view.position = Vector3(motion.position.x, motion.position.y, -motion.source_z)
		view.quaternion = Quaternion(Vector3.BACK, motion.spawn_rotation_z if not is_nan(motion.spawn_rotation_z) else rotation)
		flying_discs.append({"motion": motion, "view": view, "previous_position": motion.position, "player": index,"camera_tracked":true})
		camera_rig.add_target("disc_%d" % motion.get_instance_id(),Vector3(motion.position.x,motion.position.y,motion.source_z),Vector3(motion.collider_radius*2,motion.collider_radius*2,0),1.0 if (motion.disc_powerups&32)!=0 else 0.66,0.25)
		if not motion.started_dropped:audio.play("event:/combat/throw",motion.position,{"aimPower":motion.aim_power},motion.source_z)
		print(("BF_DROP " if motion.started_dropped else "BF_THROW ") + JSON.stringify({"actor": index, "tick": simulated_ticks, "aim": motion.aim_power, "spin": motion.spin_angle, "position": [motion.position.x, motion.position.y]}))
	weapons[index].spawned_discs.clear()

func step_discs() -> void:
	for item in flying_discs:
		if not round_state.simulation_running(): break
		var disc: OriginalDiscMotion = item.motion
		item.previous_position = disc.position
		disc.cell_grid = cell_grid
		disc.cell_owner = actors.find(disc.owner_actor)
		disc.fixed_step()
		for event in disc.events:
			if event.type == "wall":
				audio.wall(event,disc.source_z)
				if level_behaviors.initialized:
					level_behaviors.interact_disc_wall(disc,int(event.get("object_id",-1)),disc.velocity)
			if event.type == "wall": print("BF_DISC_BOUNCE " + JSON.stringify({"actor": item.player, "tick": simulated_ticks, "position": [disc.position.x, disc.position.y]}))
		disc.events.clear()
	var discs: Array[OriginalDiscMotion] = []
	for item in flying_discs: discs.append(item.motion)
	combat.native_overlap_phase(discs)
	powerup_world.collectable_discs = discs
	powerup_world.native_overlap_phase()
	consume_power_events()
	consume_book_events()
	consume_combat_events()
	cleanup_inactive_discs()

func cleanup_inactive_discs() -> void:
	for item in flying_discs:
		if not item.motion.active:
			if item.camera_tracked: camera_rig.remove_target("disc_%d" % item.motion.get_instance_id(),Vector3(item.motion.position.x,item.motion.position.y,item.motion.source_z))
			item.view.queue_free()
	flying_discs = flying_discs.filter(func(item: Dictionary) -> bool: return item.motion.active)

func consume_environment_events(index: int) -> void:
	var environment := actor_environments[index]
	var pending := environment.events.duplicate()
	environment.events.clear()
	for event in pending:
		if event.type=="fall_hidden" and not event.cliff:audio.play(audio.inputs.particle_audio.get("6",""),actors[index].position)
		if event.type == "fall_death":
			if combat.kill(index,"fall"): environment.fall_in_hazard()
		print("BF_" + String(event.type).to_upper() + " " + JSON.stringify({"actor":index,"tick":simulated_ticks}))

func consume_combat_events() -> void:
	for event in combat.events:
		audio.combat_event(event,self)
		if event.type == "catch": print("BF_CATCH " + JSON.stringify({"actor": event.actor, "tick": simulated_ticks}))
		elif event.type == "death":
			decoys.on_owner_death(event.victim)
			actors[event.victim].is_sliced = event.cause not in ["fall","telefrag","explosion","fire","outofbounds"]
			characters[event.victim].set_sliced(body_bindings[characters[event.victim].model_name], actors[event.victim].is_sliced)
			if event.cause in ["telefrag","explosion"]: characters[event.victim].set_squished(body_bindings[characters[event.victim].model_name],true)
			if event.cause in ["explosion","fire"]: characters[event.victim].set_burnt(true)
			if not event.get("clock_handled",false): clock.slow_mo_effect(0.5, 0.5)
			print("BF_DEATH " + JSON.stringify({"victim": event.victim, "killer": event.killer, "cause": event.cause, "tick": simulated_ticks, "scores": range(actors.size()).map(func(i): return round_state.scores.net_score(i))}))
		elif event.type in ["parry", "melee_block"]:
			clock.freeze(event.freeze)
			if event.get("charged", false): clock.slow_mo_effect(0.3)
			print("BF_" + String(event.type).to_upper() + " " + JSON.stringify({"actor": event.actor, "tick": simulated_ticks, "charged": event.get("charged", false)}))
		elif event.type == "disarm":
			if event.armed: clock.slow_mo_effect(0.25)
			spawn_discs(event.victim, actors[event.victim].rotation_z)
			print("BF_DISARM " + JSON.stringify({"actor": event.actor, "victim": event.victim, "armed": event.armed, "tick": simulated_ticks}))
		elif event.type == "shield_hit":
			if event.charged: clock.slow_mo_effect(0.25)
			print("BF_SHIELD_HIT " + JSON.stringify({"actor": event.actor, "tick": simulated_ticks, "charged": event.charged}))
		elif event.type == "disc_clash":
			if event.different_owners and not clock.slow_mo: clock.slow_mo_effect(0.1, 0.075, 0.5)
			print("BF_DISC_CLASH " + JSON.stringify({"tick": simulated_ticks, "different_owners": event.different_owners}))
		elif event.type == "disc_untracked":
			for item in flying_discs:
				if item.motion.get_instance_id() == event.disc and item.camera_tracked:
					camera_rig.remove_target("disc_%d" % event.disc,Vector3(item.motion.position.x,item.motion.position.y,item.motion.source_z))
					item.camera_tracked = false
		elif event.type == "public_disc":
			for item in flying_discs:
				if item.motion.get_instance_id() == event.disc:
					item.view.base_color = Color(0.658823549747467, 0.658823549747467, 0.658823549747467)
					OriginalDiscPreview.color_meshes(item.view.model, item.view.base_color)
	combat.events.clear()

func consume_book_events() -> void:
	for event in powerup_world.events:
		if event.type == "book_spawned":
			var view := OriginalPowerupPreview.new()
			view.name = "PowerupBook"
			add_child(view)
			view.initialize()
			view.position = Vector3(event.position.x, event.position.y, 0)
			book_views[event.id] = view
			var book_radius: float = powerup_world.binding.collider_radius * powerup_world.binding.root_scale
			camera_rig.add_target("book_%d" % event.id,view.position,Vector3(book_radius*2,book_radius*2,0),0.85,2.0)
		elif event.type == "book_collected":
			audio.play("event:/powerups/collect_power",actors[event.actor].position)
			book_views[event.id].collect()
		elif event.type == "book_removed":
			camera_rig.remove_target("book_%d" % event.id,book_views[event.id].position,0.5)
			book_views[event.id].queue_free()
			book_views.erase(event.id)
		print("BF_" + String(event.type).to_upper() + " " + JSON.stringify(event))
	powerup_world.events.clear()

func unity_vector(v: Dictionary) -> Vector3:
	return Vector3(v.x, v.y, -v.z)

func load_original_scene(path: String) -> void:
	var scene := read_json(ROOT + path)
	var sun_direction := Vector3(0, 0, 1)
	var global_tint := Color.WHITE
	var fog_color := Color.WHITE
	for object in scene.nodes:
		var node := Node3D.new()
		node.name = "%s_%d" % [object.name, object.id]
		node.visible = object.active
		nodes[int(object.id)] = node
		for component in object.components:
			if component.type == "Transform":
				transforms[int(component.id)] = {"node": node, "data": component.data}
	for item in transforms.values():
		var node: Node3D = item.node
		var data: Dictionary = item.data
		var parent_id := int(data.m_Father.m_PathID)
		if transforms.has(parent_id):
			transforms[parent_id].node.add_child(node)
		else:
			add_child(node)
		node.transform = preload("res://core/original_transform.gd").from_source(data)
	for object in scene.nodes:
		var node: Node3D = nodes[int(object.id)]
		if object.has("mesh") and object.mesh.resource != null and object.mesh.enabled:
			var mesh := make_mesh(object.mesh.resource, object.mesh.static_batch)
			var instance := MeshInstance3D.new()
			instance.mesh = mesh
			for i in mesh.get_surface_count():
				var slot: int = mini(i, object.mesh.materials.size() - 1)
				if slot >= 0 and object.mesh.materials[slot] != null:
					instance.set_surface_override_material(i, make_material(object.mesh.materials[slot]))
			node.add_child(instance)
			mesh_count += 1
		for component in object.components:
			if component.type == "MonoBehaviour" and component.data.has("globalTintColorLight"):
				var tint: Dictionary = component.data.globalTintColorLight
				var fog: Dictionary = component.data.fogColor
				global_tint = Color(tint.r, tint.g, tint.b, tint.a)
				fog_color = Color(fog.r, fog.g, fog.b, fog.a)
			if component.type in ["BoxCollider2D", "CircleCollider2D", "PolygonCollider2D"] and component.data.m_Enabled:
				add_collider_outline(node, component.type, component.data)
			if component.type == "Light" and component.data.m_Enabled and int(component.data.m_Type) == 1:
				var light := DirectionalLight3D.new()
				var c: Dictionary = component.data.m_Color
				light.light_color = Color(c.r, c.g, c.b)
				light.light_energy = component.data.m_Intensity
				light.shadow_enabled = int(component.data.m_Shadows.m_Type) != 0
				node.add_child(light)
				if light.shadow_enabled and node.is_visible_in_tree(): sun_direction = light.global_basis.z.normalized()
	load_original_sprites()
	scene_materials.configure_lighting(sun_direction, global_tint, fog_color)
	collision_world.load_scene(scene, nodes)
	explosion_corners.clear()
	for object in scene.nodes:
		var node: Node3D = nodes[int(object.id)]
		if int(object.layer) != 8 or not node.is_visible_in_tree(): continue
		for component in object.components:
			if component.type != "BoxCollider2D" or not component.data.m_Enabled: continue
			var data: Dictionary = component.data
			var half := Vector2(data.m_Size.x*node.scale.x,data.m_Size.y*node.scale.y)*0.5
			# Source Level.GetAllCorners uses local scale and world Z angle, no offset.
			var angle := node.global_rotation.z
			for corner in [Vector2(-half.x,-half.y),Vector2(-half.x,half.y),Vector2(half.x,-half.y),half]:
				explosion_corners.append(Vector2(node.global_position.x,node.global_position.y)+corner.rotated(angle))
	# Runtime camera tracking updates this camera through OriginalCameraRig.
	camera = Camera3D.new()
	camera.fov = 38.0
	camera.near = 1.0
	camera.far = 400.0
	add_child(camera)
	camera.current = true
	var environment_node := WorldEnvironment.new()
	var environment := Environment.new()
	environment.background_mode = Environment.BG_COLOR
	environment.background_color = Color("d6e3e8")
	environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	environment.ambient_light_color = Color.WHITE
	environment.ambient_light_energy = 0.65
	environment_node.environment = environment
	add_child(environment_node)

func make_mesh(id: String, batch: Dictionary) -> ArrayMesh:
	var first := int(batch.get("firstSubMesh", 0))
	var count := int(batch.get("subMeshCount", 0))
	var cache_key := "%s:%d:%d" % [id, first, count]
	if mesh_cache.has(cache_key):
		return mesh_cache[cache_key]
	var data := read_json(ROOT + manifest.meshes[id].path)
	var vertices := PackedVector3Array()
	var normals := PackedVector3Array()
	var uv := PackedVector2Array()
	var colors := PackedColorArray()
	for v in data.vertices:
		vertices.append(Vector3(v[0], v[1], -v[2]))
	if data.normals != null:
		for v in data.normals:
			normals.append(Vector3(v[0], v[1], -v[2]))
	if data.uv != null:
		for v in data.uv:
			uv.append(Vector2(v[0], 1.0 - v[1]))
	if data.colors != null:
		var divisor := 255.0 if data.get("color_encoding", "float") == "unorm8" else 1.0
		for v in data.colors:
			colors.append(Color(v[0] / divisor, v[1] / divisor, v[2] / divisor, v[3] / divisor))
	var result := ArrayMesh.new()
	var stop: int = first + count if count > 0 else data.submeshes.size()
	for index in range(first if count > 0 else 0, stop):
		var indices := PackedInt32Array()
		for triangle in data.submeshes[index]:
			# Reflecting Z already gives Godot's clockwise front faces. Reversing
			# indices as an OBJ exporter would do makes these meshes inside out.
			indices.append_array([int(triangle[0]), int(triangle[1]), int(triangle[2])])
		if indices.is_empty():
			continue
		var arrays: Array = []
		arrays.resize(Mesh.ARRAY_MAX)
		arrays[Mesh.ARRAY_VERTEX] = vertices
		arrays[Mesh.ARRAY_INDEX] = indices
		if normals.size() == vertices.size(): arrays[Mesh.ARRAY_NORMAL] = normals
		if uv.size() == vertices.size(): arrays[Mesh.ARRAY_TEX_UV] = uv
		if colors.size() == vertices.size(): arrays[Mesh.ARRAY_COLOR] = colors
		result.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, arrays)
	mesh_cache[cache_key] = result
	return result

func make_material(id: String) -> Material:
	if material_cache.has(id): return material_cache[id]
	var data: Dictionary = manifest.materials[id]
	var material := scene_materials.make(id, data)
	material_cache[id] = material
	return material

func load_original_sprites() -> void:
	var data := read_json(ROOT + "sprite-inputs.json")
	var imported := 0
	var pending := 0
	for sprite in data.sprites:
		var source: Dictionary = data.materials[sprite.material]
		if source.shader not in ["UU/Unlit/Shadow_Receiver", "PK/SpritesDiffuseWave"]:
			pending += 1
			continue
		if not nodes.has(int(sprite.node_id)):
			pending += 1
			continue
		var vertices := PackedVector3Array()
		var normals := PackedVector3Array()
		var colors := PackedColorArray()
		var uv := PackedVector2Array()
		var tint: Dictionary = sprite.color
		for index in sprite.vertices.size():
			var v: Array = sprite.vertices[index]
			vertices.append(Vector3(-v[0] if sprite.flip_x else v[0], -v[1] if sprite.flip_y else v[1], -v[2]))
			normals.append(Vector3(0, 0, 1))
			colors.append(Color(tint.r, tint.g, tint.b, tint.a))
			uv.append(Vector2(sprite.uv[index][0], 1.0 - sprite.uv[index][1]))
		var arrays: Array = []
		arrays.resize(Mesh.ARRAY_MAX)
		arrays[Mesh.ARRAY_VERTEX] = vertices
		arrays[Mesh.ARRAY_NORMAL] = normals
		arrays[Mesh.ARRAY_COLOR] = colors
		arrays[Mesh.ARRAY_TEX_UV] = uv
		arrays[Mesh.ARRAY_INDEX] = PackedInt32Array(sprite.indices)
		var mesh := ArrayMesh.new()
		mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, arrays)
		var material := scene_materials.make(sprite.material, source)
		material.set_shader_parameter("source_main", load(ROOT + sprite.texture.path))
		var instance := MeshInstance3D.new()
		instance.name = "OriginalSprite_%d" % int(sprite.renderer_id)
		instance.extra_cull_margin = 1.0
		instance.mesh = mesh
		instance.material_override = material
		instance.visible = sprite.enabled
		instance.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF if int(sprite.cast_shadows) == 0 else GeometryInstance3D.SHADOW_CASTING_SETTING_ON
		nodes[int(sprite.node_id)].add_child(instance)
		imported += 1
	print("BF_SPRITES_READY " + JSON.stringify({"imported": imported, "pending_wave_shader": pending}))

func add_collider_outline(parent: Node3D, kind: String, data: Dictionary) -> void:
	var paths: Array = []
	var offset := Vector2(data.m_Offset.x, data.m_Offset.y)
	if kind == "BoxCollider2D":
		var half := Vector2(data.m_Size.x, data.m_Size.y) * 0.5
		paths.append([Vector2(-half.x, -half.y), Vector2(half.x, -half.y), half, Vector2(-half.x, half.y)])
	elif kind == "CircleCollider2D":
		var points: Array = []
		for i in 48:
			points.append(Vector2.from_angle(i * TAU / 48.0) * data.m_Radius)
		paths.append(points)
	elif kind == "PolygonCollider2D":
		for path in data.m_Points.m_Paths:
			var points: Array = []
			for p in path:
				points.append(Vector2(p.x, p.y))
			paths.append(points)
	var mesh := ImmediateMesh.new()
	var material := StandardMaterial3D.new()
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.albedo_color = Color("15bba1") if not data.m_IsTrigger else Color("df795f")
	mesh.surface_begin(Mesh.PRIMITIVE_LINES, material)
	for points in paths:
		for i in points.size():
			var a: Vector2 = points[i] + offset
			var b: Vector2 = points[(i + 1) % points.size()] + offset
			mesh.surface_add_vertex(Vector3(a.x, a.y, 0.05))
			mesh.surface_add_vertex(Vector3(b.x, b.y, 0.05))
	mesh.surface_end()
	var instance := MeshInstance3D.new()
	instance.mesh = mesh
	parent.add_child(instance)
	collider_instances.append(instance)

func create_ui() -> void:
	var canvas := CanvasLayer.new()
	debug_canvas = canvas
	add_child(canvas)
	var panel := PanelContainer.new()
	panel.position = Vector2(20, 20)
	canvas.add_child(panel)
	var margin := MarginContainer.new()
	for side in ["left", "right", "top", "bottom"]:
		margin.add_theme_constant_override("margin_" + side, 14)
	panel.add_child(margin)
	var column := VBoxContainer.new()
	column.add_theme_constant_override("separation", 8)
	margin.add_child(column)
	var title := Label.new()
	title.text = "BOOMERANG FU · GODOT PORT"
	title.add_theme_font_size_override("font_size", 18)
	column.add_child(title)
	status = Label.new()
	status.add_theme_font_size_override("font_size", 13)
	column.add_child(status)
	var description := Label.new()
	description.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	description.custom_minimum_size.x = 330
	description.add_theme_font_size_override("font_size", 14)
	description.text = "P1: WASD · Space dash · J attack · K throw\nP2: Arrows · Enter dash · / attack · Shift throw\nAttack with empty hands to disarm\nHold throw to charge / retrieve · Release to throw\nCombat preview · Powers and presentation in progress."
	column.add_child(description)
	movement_toggle = CheckButton.new()
	movement_toggle.text = "Enable movement and map collisions"
	movement_toggle.button_pressed = true
	movement_toggle.focus_mode = Control.FOCUS_NONE
	movement_toggle.toggled.connect(func(value): movement_enabled = value)
	column.add_child(movement_toggle)
	column.add_child(audio.make_button())
	var reset_button := Button.new()
	reset_button.text = "Restart match"
	reset_button.focus_mode = Control.FOCUS_NONE
	reset_button.pressed.connect(reset_motions)
	column.add_child(reset_button)
	next_round_button = Button.new()
	next_round_button.focus_mode = Control.FOCUS_NONE
	next_round_button.visible = false
	next_round_button.text = "Next round"
	next_round_button.pressed.connect(next_round)
	column.add_child(next_round_button)
	var animation_buttons := HBoxContainer.new()
	column.add_child(animation_buttons)
	for clip_name in ["idle", "walk"]:
		var button := Button.new()
		button.focus_mode = Control.FOCUS_NONE
		button.text = "Original " + clip_name
		button.pressed.connect(func(): set_character_clip(clip_name))
		animation_buttons.add_child(button)
	var view_button := Button.new()
	view_button.focus_mode = Control.FOCUS_NONE
	view_button.text = "View full map"
	view_button.pressed.connect(func():
		camera_auto = not camera_auto
		distance = 100.0
		center = Vector3.ZERO
		view_button.text = "Follow original camera" if not camera_auto else "View full map")
	column.add_child(view_button)
	var colliders_button := CheckButton.new()
	colliders_button.text = "Show original collision shapes"
	colliders_button.button_pressed = false
	for instance in collider_instances: instance.visible = false
	colliders_button.toggled.connect(func(value):
		for instance in collider_instances: instance.visible = value)
	column.add_child(colliders_button)
	var slow_button := Button.new()
	slow_button.focus_mode = Control.FOCUS_NONE
	slow_button.text = "Test original slow-motion timing"
	slow_button.pressed.connect(func(): clock.slow_mo_effect(0.3))
	column.add_child(slow_button)
	var freeze_button := Button.new()
	freeze_button.focus_mode = Control.FOCUS_NONE
	freeze_button.text = "Test original hit stop (40 ms)"
	freeze_button.pressed.connect(func(): clock.freeze(0.04))
	column.add_child(freeze_button)
	var power_background := PanelContainer.new()
	power_background.position = Vector2(860, 20)
	var power_style := StyleBoxFlat.new()
	power_style.bg_color = Color(0.035, 0.05, 0.065, 0.8)
	power_style.set_content_margin_all(10)
	power_background.add_theme_stylebox_override("panel", power_style)
	canvas.add_child(power_background)
	var power_panel := VBoxContainer.new()
	power_panel.add_theme_constant_override("separation", 8)
	power_background.add_child(power_panel)
	var power_title := Label.new()
	power_title.text = "Powerup checks (temporary controls)"
	power_title.add_theme_font_size_override("font_size", 13)
	power_panel.add_child(power_title)
	for index in actors.size():
		var label := Label.new()
		label.add_theme_font_size_override("font_size", 13)
		power_panel.add_child(label)
		power_labels.append(label)
		var row := GridContainer.new()
		row.columns = 4
		power_panel.add_child(row)
		for option in [["Haste", 2], ["Phase", 4], ["Shield", 16], ["Extra", 512], ["Multi",64], ["Reverse", 8], ["Warp", 4096], ["Control",8192], ["Teleport",128],["Explosive",32],["Fire",256],["Ice",1024],["Disguise",2048],["Shrink",16384],["Decoy",32768]]:
			var button := Button.new()
			button.focus_mode = Control.FOCUS_NONE
			button.text = option[0]
			button.pressed.connect(func(): grant_preview_power(index, option[1]))
			row.add_child(button)
	var book_button := Button.new()
	book_button.text = "Spawn a test book"
	book_button.focus_mode = Control.FOCUS_NONE
	book_button.pressed.connect(func():
		if powerup_world.books.is_empty():
			powerup_world.spawn_book(Vector2(0,-3))
			consume_book_events())
	power_panel.add_child(book_button)
	var fall_button := Button.new()
	fall_button.text = "Reset + test falling"
	fall_button.focus_mode = Control.FOCUS_NONE
	fall_button.pressed.connect(start_fall_preview)
	power_panel.add_child(fall_button)
	var wall_button := Button.new()
	wall_button.text = "Prepare wall test (D + Space)"
	wall_button.focus_mode = Control.FOCUS_NONE
	wall_button.pressed.connect(func(): prepare_wall_preview())
	power_panel.add_child(wall_button)
	var teleport_button := Button.new()
	teleport_button.text = "Test teleport hit (shield + immunity)"
	teleport_button.focus_mode = Control.FOCUS_NONE
	teleport_button.pressed.connect(prepare_teleport_preview)
	power_panel.add_child(teleport_button)
	var element_button:=Button.new()
	element_button.text="Test Fire / Ice contact"
	element_button.focus_mode=Control.FOCUS_NONE
	element_button.pressed.connect(prepare_element_preview)
	power_panel.add_child(element_button)
	var boundary_button:=Button.new()
	boundary_button.text="Test shrinking boundary"
	boundary_button.focus_mode=Control.FOCUS_NONE
	boundary_button.pressed.connect(func():
		reset_motions(true)
		actors[0].position=Vector2(-8,-8)
		previous_positions[0]=actors[0].position
		actors[1].position=Vector2(8,-8)
		previous_positions[1]=actors[1].position
		grant_preview_power(0,16384)
		consume_power_events())
	power_panel.add_child(boundary_button)
	var decoy_button:=Button.new()
	decoy_button.text="Test decoy (first dash)"
	decoy_button.focus_mode=Control.FOCUS_NONE
	decoy_button.pressed.connect(func():
		reset_motions(true)
		grant_preview_power(0,32768)
		input_states[0]=Vector2.RIGHT
		input_dash_states[0]=true)
	power_panel.add_child(decoy_button)

func update_decoy_views(delta:float)->void:
	for event in decoys.events:
		audio.play("event:/powerups/decoy_spawn" if event.type=="spawn" else "event:/powerups/decoy_die",event.position)
		if event.type=="spawn":
			var container:=Node3D.new()
			container.name="Decoy%d"%event.id
			add_child(container)
			var view:=OriginalCharacterPreview.new()
			var character_name:=characters[event.owner].model_name
			view.name=character_name
			container.add_child(view)
			var base:="res://assets/characters/"+character_name
			view.configure(base+".glb",base+"-idle.json")
			view.scale=Vector3.ONE*1.3300000429153442
			view.set_sliced(body_bindings[character_name],false)
			view.set_squished(body_bindings[character_name],false)
			view.set_held_discs(weapon_bindings[character_name],event.held,false)
			var binding:Dictionary=weapon_bindings[character_name]
			var color:Dictionary=binding.color
			for key in ["rightDisc","leftDisc"]:
				var target:Dictionary=view.targets.get(binding[key],{})
				if target.has("node"):OriginalDiscPreview.color_meshes(target.node,Color(color.r,color.g,color.b,color.a))
			var shield:Node3D=shield_views[event.owner].duplicate()
			view.add_child(shield)
			shield.visible=(event.powers&16)!=0
			decoy_views[event.id]={"root":container,"view":view,"clip":"idle","character":character_name,"shield":shield}
			var actor:OriginalActorMotion=decoys.members[event.id].actor if decoys.members.has(event.id) else null
			if actor!=null:camera_rig.add_target("decoy_%d"%event.id,Vector3(actor.position.x,actor.position.y,actor.source_z),Vector3(actor.radius*2,actor.radius*2,0),1.0,1.0)
		else:
			if decoy_views.has(event.id):
				decoy_views[event.id].root.queue_free()
				decoy_views.erase(event.id)
			camera_rig.remove_target("decoy_%d"%event.id,Vector3(event.position.x,event.position.y,0))
		print("BF_DECOY "+JSON.stringify(event))
	decoys.events.clear()
	for id in decoys.members:
		if not decoy_views.has(id):continue
		var item:Dictionary=decoys.members[id]
		var visual:Dictionary=decoy_views[id]
		var actor:OriginalActorMotion=item.actor
		visual.view.position=Vector3(item.render.x,item.render.y-0.4,0.25)
		visual.view.quaternion=Quaternion(Vector3.RIGHT,deg_to_rad(-25.0))*Quaternion(Vector3.BACK,item.render_rotation-PI/2.0)
		var clip:="walk" if actor.move_speed_parameter>0.25 else "idle"
		if actor.is_stunned:clip="stunned"
		if actor.is_frozen:clip="frozen"
		if actor.is_falling:clip="fall"
		if visual.clip!=clip:
			visual.view.set_clip("res://assets/characters/"+visual.character+"-"+clip+".json")
			visual.clip=clip
		visual.view.advance(delta)
		camera_rig.set_target_position("decoy_%d"%id,Vector3(item.render.x,item.render.y,actor.source_z))

func prepare_teleport_preview() -> void:
	# Controlled scene setup for inspecting the source Teleport branch in WebGL.
	reset_motions(true)
	grant_preview_power(0,128)
	grant_preview_power(1,16)
	actors[1].is_invulnerable = true
	weapons[0].display_position = actors[0].position
	weapons[0].display_rotation = 0.0
	weapons[0].try_throw()
	spawn_discs(0,0.0)
	weapons[0].owned_discs[0].position = actors[1].position
	actors[0].can_dash = false
	input_dash_states[0] = true
	print("BF_TELEPORT_PREVIEW controlled destination with shield and invulnerability")
	_process(0.0)

func prepare_wall_preview(phase := true) -> Dictionary:
	reset_motions(true)
	var actor := actors[0]
	for shape in collision_world.shapes:
		if shape.layer != 256 or shape.kind != "polygon": continue
		var box: Rect2 = shape.bounds
		if box.size.x > 3.0 or box.size.y < 4.0: continue
		var start := Vector2(box.position.x-actor.radius-0.3,box.get_center().y)
		var finish := start+Vector2.RIGHT*10.0
		if collision_world.overlap_circle(start,actor.radius,256) or collision_world.overlap_circle(finish,actor.radius,256): continue
		if actor_environments[0].over_hazard(start) or actor_environments[0].over_hazard(finish): continue
		actor.position = start
		previous_positions[0] = start
		if phase: weapons[0].powers.start(4)
		var result := {"wall":shape.name,"start":[start.x,start.y],"wall_end":box.end.x,"phase":phase}
		print("BF_WALL_PREVIEW " + JSON.stringify(result))
		return result
	return {}

func start_fall_preview() -> void:
	reset_motions(true)
	# This source scene has a 100x100 floor collider beyond its 43x43 arena.
	for x in [-52.0,-64.0,-80.0]:
		var candidate := Vector2(x,-8)
		if actor_environments[0].over_hazard(candidate):
			actors[0].position = candidate
			previous_positions[0] = candidate
			print("BF_FALL_PREVIEW " + JSON.stringify({"position":[candidate.x,candidate.y]}))
			return

func grant_preview_power(index: int, power: int) -> void:
	if actors[index].state != OriginalActorMotion.State.ALIVE: return
	weapons[index].powers.start(power)
	print("BF_POWER_COLLECTED " + JSON.stringify({"actor": index, "power": power, "history": weapons[index].powers.history}))

func consume_power_events() -> void:
	for weapon in weapons:
		for event in weapon.powers.events:
			audio.power_event(weapons.find(weapon),event,self)
			if event.type == "time_warp":
				clock.slow_mo_time_warp(event.hold,event.out,event.back,event.scale)
				print("BF_TIME_WARP " + JSON.stringify({"actor":weapons.find(weapon),"tick":simulated_ticks}))
			elif event.type == "shrink_boundary":
				boundary.start(event.position,true,weapons.find(weapon))
				combat.boundary_owner=boundary.owner
			elif event.type == "time_immune_end": print("BF_TIME_IMMUNE_END " + JSON.stringify({"actor":weapons.find(weapon),"tick":simulated_ticks}))
			elif event.type == "multidisc_burst": print("BF_MULTIDISC_BURST " + JSON.stringify({"actor":weapons.find(weapon),"count":event.count,"tick":simulated_ticks}))
		weapon.powers.events.clear()

func _process(delta: float) -> void:
	if shell != null:
		shell.frame(delta)
		if shell.blocks_simulation():
			bots.idle_frame(0.0);bots.end_frame()
			clock.update(delta, OriginalTimekeeper.GameState.MENU if shell.mode == FfaShell.Mode.LOBBY else OriginalTimekeeper.GameState.PAUSED)
			update_camera(delta)
			audio.frame(self,delta,0.0)
			return
	camera_rig.frame_delta = OriginalTimekeeper.f32(delta)
	for weapon in weapons: weapon.powers.unscaled_frame(delta)
	var scaled_delta := OriginalTimekeeper.f32(OriginalTimekeeper.f32(delta) * clock.time_scale)
	decoys.lifecycle_frame(scaled_delta,clock.time_counter,clock.unscaled_time_counter,movement_enabled and round_state.simulation_running())
	if movement_enabled and round_state.simulation_running():
		cell_grid.current_time = clock.time_counter
		update_movement(scaled_delta)
		level_behaviors.frame_step(scaled_delta)
		for behavior_event in level_behaviors.drain_events(): print("BF_LEVEL3_EVENT " + JSON.stringify(behavior_event))
		advance_explosions(scaled_delta)
		for index in actor_elements.size():
			if actor_elements[index].burning: cell_preview.actor_fire(index,actors[index],scaled_delta)
		combat.frame_step(scaled_delta,false)
		consume_combat_events()
		var before := round_state.phase
		round_state.frame_step(scaled_delta)
		if before != round_state.phase and round_state.phase == OriginalFfaRound.Phase.SCORES:
			print("BF_ROUND_SCORES " + JSON.stringify({"round": round_state.round_number, "scores": range(actors.size()).map(func(i): return round_state.scores.net_score(i))}))
	boundary.game_frame(scaled_delta,movement_enabled and round_state.simulation_running(),actors.filter(func(a):return a.state!=OriginalActorMotion.State.DEAD).size())
	boundary.frame_step(scaled_delta,movement_enabled and round_state.simulation_running())
	combat.boundary_owner=boundary.owner
	powerup_world.shrinking_allowed=not boundary.shrinking and not boundary.sudden_death
	boundary.visual_frame(scaled_delta,actor_render_positions.map(func(p):return Vector2(p.x,p.y)),actors.map(func(a):return a.state))
	boundary_view.advance(boundary)
	for event in boundary.events:
		if event.type=="shrink_started":audio.play("event:/powerups/battle_royale",event.target)
		print("BF_BOUNDARY "+JSON.stringify(event))
	boundary.events.clear()
	audio.cell_frame(cell_grid)
	cell_grid.frame_step(clock.time_counter)
	cell_preview.advance(cell_grid,scaled_delta,camera)
	powerup_world.frame_step(scaled_delta, clock.time_counter, movement_enabled and round_state.phase == OriginalFfaRound.Phase.PLAYING)
	consume_book_events()
	consume_power_events()
	if not movement_enabled or not round_state.simulation_running():
		bots.idle_frame(scaled_delta)
		for weapon in weapons: weapon.powers.update_time_immune(scaled_delta,clock.time_scale,clock.target_scale,false)
		for disguise in disguises:disguise.frame_step(scaled_delta,false)
		decoys.input_phase()
	decoys.end_frame()
	bots.end_frame()
	update_decoy_views(scaled_delta)
	for item in flying_discs: item.motion.update_time_scaler(scaled_delta,clock.time_scale)
	cell_preview.update_frozen(actors,scaled_delta)
	for index in disguise_views.size():
		var point:Vector3=actor_render_positions[index]
		point.z=actors[index].source_z
		disguise_views[index].advance_disguise(disguises[index],point,actor_render_rotations[index],scaled_delta)
	clock.update(delta)
	scene_materials.set_time(clock.time_counter)
	for item in flying_discs:
		item.motion.collider_disabled_remaining = maxf(0.0, OriginalTimekeeper.f32(item.motion.collider_disabled_remaining - OriginalTimekeeper.f32(delta)))
	for index in characters.size(): characters[index].advance(OriginalTimekeeper.f32(scaled_delta * actors[index].time_scaler))
	for item in flying_discs: item.view.advance_disc(scaled_delta, item.motion)
	for view in book_views.values(): view.advance_book(scaled_delta)
	for index in power_labels.size():
		var powers := weapons[index].powers
		var names: Array[String] = []
		for power in powers.history:
			names.append({2: "Haste", 4:"Phase", 16: "Shield", 32:"Explosive",64:"Multi",128:"Teleport",256:"Fire",1024:"Ice", 512: "Extra",8192:"Control",2048:"Disguise"}.get(power, str(power)))
		power_labels[index].text = "P%d · %s%s" % [index + 1, " / ".join(names) if not names.is_empty() else "No powers", " · Reverse %.1fs" % maxf(0.0, powers.reverse_timer) if powers.reverse_running else ""]
		if powers.has_power(4096): power_labels[index].text += " · Warp %.2fx" % actors[index].time_scaler
		if weapons[index].mini_discs_collected > 0: power_labels[index].text += " · Parts %d/%d" % [weapons[index].mini_discs_collected,weapons[index].mini_discs_required]
		if actor_bounds[index].outside:power_labels[index].text+=" · Out %.1fs"%maxf(0.0,actor_bounds[index].timer)
		var steering := 0
		for disc in weapons[index].owned_discs:
			if disc.active and disc.movement == OriginalDiscMotion.Movement.CONTROLLED: steering += 1
		if steering > 0: power_labels[index].text += " · Steering %d" % steering
		if index < shield_views.size(): shield_views[index].visible = actors[index].is_shielded and not disguises[index].disguised
		characters[index].visible = actors[index].appearance_visible and powers.invulnerability_visible and not disguises[index].disguised
	update_camera(delta)
	var stage := "Playing"
	if round_state.phase == OriginalFfaRound.Phase.SCORES: stage = "Round over"
	elif round_state.phase == OriginalFfaRound.Phase.MATCH_END: stage = "Match won: P%d" % (round_state.scores.winners[0] + 1)
	status.text = "Round %d · %s · Goal %d\nP1 %d (%s) · P2 %d (%s)\nClock %.3f× · Flying %d" % [round_state.round_number, stage, round_state.scores.goal,
		round_state.scores.net_score(0), "Out" if actors[0].state == OriginalActorMotion.State.DEAD else "Alive",
		round_state.scores.net_score(1), "Out" if actors[1].state == OriginalActorMotion.State.DEAD else "Alive", clock.time_scale, flying_discs.size()]
	next_round_button.visible = not round_state.simulation_running()
	next_round_button.text = "New match" if round_state.phase == OriginalFfaRound.Phase.MATCH_END else "Next round"
	audio.frame(self,delta,scaled_delta)

func _input(event: InputEvent) -> void:
	if shell != null and shell.receive(event):
		get_viewport().set_input_as_handled()
		return
	if movement_enabled and (shell == null or not shell.blocks_simulation()):
		controllers.receive(event)
		if shell != null and (event is InputEventKey or event is InputEventJoypadButton or event is InputEventJoypadMotion):
			get_viewport().set_input_as_handled()

func _unhandled_input(event: InputEvent) -> void:
	if shell != null and not debug_canvas.visible: return
	if event is InputEventMouseMotion and Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT):
		camera_yaw -= event.relative.x * 0.005
		camera_pitch = clampf(camera_pitch + event.relative.y * 0.005, -1.4, 1.4)
	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP: distance = maxf(15.0, distance * 0.9)
		if event.button_index == MOUSE_BUTTON_WHEEL_DOWN: distance = minf(500.0, distance * 1.1)

func advance_explosions(delta: float) -> void:
	for item in flying_discs.duplicate():
		var disc: OriginalDiscMotion = item.motion
		if not disc.active or not disc.grenade_active: continue
		var previous_blinks:=disc.grenade.blink_counter
		var exploded:=disc.grenade.step(delta)
		if disc.grenade.blink_counter>previous_blinks:audio.play("event:/powerups/grenade_blink",disc.position,{"counter":previous_blinks})
		if exploded:
			var blast := OriginalExplosion.new()
			blast.owner = actors.find(disc.grenade_owner)
			# The original grenade child is z=-1; Grenade.Explode adds another -1.
			var kind := "ExplosionFreeze" if (disc.disc_powerups&1024)!=0 else "ExplosionFire" if (disc.disc_powerups&256)!=0 else "Explosion"
			blast.freeze_explosion = kind=="ExplosionFreeze"
			audio.play(explosion_parameters[kind].explosionSoundMini if disc.is_mini_disc else explosion_parameters[kind].explosionSoundBig,disc.position,{},disc.source_z-2.0)
			blast.initialize(explosion_parameters[kind],disc.position,disc.source_z-2.0,disc.is_mini_disc,clock.time_counter,collision_world,explosion_corners)
			var view := OriginalExplosionPreview.new()
			view.initialize()
			add_child(view)
			view.update_blast(blast)
			explosions.append({"motion":blast,"view":view})
			camera_rig.add_target("blast_%d"%blast.get_instance_id(),Vector3(blast.position.x,blast.position.y,blast.source_z),Vector3(blast.parameters.radiusFinal*2,blast.parameters.radiusFinal*2,0),0.4,0.25)
			if not disc.is_mini_disc and disc.owner_inventory != null and disc.owner_inventory.get_ref() != null: disc.owner_inventory.get_ref().request_respawn(1.75)
			disc.recycle()
			print("BF_EXPLOSION "+JSON.stringify({"owner":blast.owner,"mini":blast.mini,"position":[blast.position.x,blast.position.y],"tick":simulated_ticks}))
	for item in explosions.duplicate():
		var blast: OriginalExplosion = item.motion
		blast.frame_step(clock.time_counter,clock.unscaled_time_counter)
		combat.apply_explosion(blast)
		decoys.apply_explosion(blast)
		for point in blast.fire_points: cell_grid.register_cell(OriginalCellGrid.CellType.FIRE,point,cell_rng.randf_range(1.6,1.9),blast.owner)
		blast.fire_points.clear()
		if blast.damage_active and not blast.freeze_explosion:
			for book in powerup_world.books.duplicate():
				if book.collected: continue
				if blast.reaches(book.position,0.0,OriginalTimekeeper.f32(powerup_world.binding.collider_radius*powerup_world.binding.root_scale)):
					powerup_world.books.erase(book)
					powerup_world.events.append({"type":"book_removed","id":book.id})
		item.view.update_blast(blast)
		if not blast.active:
			camera_rig.remove_target("blast_%d"%blast.get_instance_id(),Vector3(blast.position.x,blast.position.y,blast.source_z))
			item.view.queue_free()
			explosions.erase(item)
	for weapon in weapons:
		for duration in weapon.respawn_requests: weapon.request_respawn(duration)
		weapon.respawn_requests.clear()
		var before_held:int=weapon.held_discs
		weapon.advance_respawns(delta)
		if weapon.held_discs>before_held:audio.play("event:/combat/disc_spawn",weapon.actor.position)
	cleanup_inactive_discs()

func consume_element_events(index:int)->void:
	for event in actor_elements[index].events:
		audio.element_event(index,event,self)
		if event.type=="burnt": characters[index].set_burnt(true)
		elif event.type=="fire_changed" and not event.value: cell_preview.stop_actor_fire(index,event.steam)
		elif event.type=="chip": cell_preview.chip(index)
		elif event.type=="steam": cell_preview.emit_particle("steamSystem",actors[index].position)
		print("BF_ELEMENT "+JSON.stringify({"actor":index,"event":event.type,"tick":simulated_ticks}))
	actor_elements[index].events.clear()

func prepare_element_preview()->void:
	reset_motions(true)
	movement_enabled=true
	for index in 2:
		var type:=OriginalCellGrid.CellType.FIRE if index==0 else OriginalCellGrid.CellType.ICE
		cell_grid.register_area(type,actors[index].position,4.0,1-index,1)
	print("BF_ELEMENT_PREVIEW "+JSON.stringify({"fire":cell_grid.fire_count,"ice":cell_grid.ice_count}))

func configure_match(names: Array[String], devices: Array[int], length: int) -> void:
	for view in disguise_views: view.free()
	disguise_views.clear()
	for character in characters: character.free()
	characters.clear()
	shield_views.clear()
	setup.roster.assign(names)
	setup.sources.assign(devices)
	setup.match_length = length
	setup.original_spawns = true
	setup.spawn_rng = OriginalUnityRandom.new(int(Time.get_ticks_usec()) & 0x7fffffff)
	clock.reset_time_scale()
	create_characters()
	for index in actors.size():
		var view := OriginalDisguisePreview.new()
		view.name = "Disguise%d" % index
		add_child(view)
		view.initialize(disguise_inputs, scene_materials)
		disguise_views.append(view)
	debug_canvas.free()
	power_labels.clear()
	create_ui()
	debug_canvas.visible = false
	movement_enabled = true
	camera_auto = true
	clear_local_input()
	print("BF_MATCH_CONFIG " + JSON.stringify({"characters":setup.roster,"sources":setup.sources,"goal":round_state.scores.goal,"positions":actors.map(func(a):return [a.position.x,a.position.y])}))

func clear_local_input() -> void:
	controllers.block_held_buttons()
	input_states.fill(Vector2.ZERO)
	input_dash_states.fill(false)
	input_attack_states.fill(false)
	input_throw_states.fill(false)
	input_release_states.fill(false)
	pending_dash.fill(false)
	pending_attack.fill(false)
	pending_throw_release.fill(false)
	for index in actors.size():
		actors[index].input_move = Vector2.ZERO
		actors[index].raw_input_move = Vector2.ZERO
		actors[index].stop_aiming()
		weapons[index].input_held = false
		player_inputs[index].buffer.clear()

func update_camera(delta: float) -> void:
	for index in actors.size(): camera_rig.set_target_position("player_%d" % index,actor_render_positions[index])
	for item in flying_discs: camera_rig.set_target_position("disc_%d" % item.motion.get_instance_id(),Vector3(item.view.position.x,item.view.position.y,-item.view.position.z))
	camera_rig.advance_tracking(delta,not clock.freeze_frame)
	var viewport_size := get_viewport().get_visible_rect().size
	camera_rig.aspect = OriginalTimekeeper.f32(viewport_size.x / viewport_size.y)
	camera_rig.step(delta,clock.freeze_frame)
	if camera_auto:
		camera.fov = camera_rig.fov
		camera.position = Vector3(camera_rig.aim_position.x,camera_rig.aim_position.y,-camera_rig.aim_position.z)
		camera.look_at(Vector3(camera_rig.focus.x,camera_rig.focus.y,-camera_rig.focus.z),Vector3.UP)
		camera.position = Vector3(camera_rig.position.x,camera_rig.position.y,-camera_rig.position.z)
	else:
		camera.fov = 38.0
		camera.position = center + Vector3(sin(camera_yaw) * cos(camera_pitch), -sin(camera_pitch), cos(camera_yaw) * cos(camera_pitch)) * distance
		camera.look_at(center, Vector3.UP)

func _exit_tree()->void:
	bots.dispose()
