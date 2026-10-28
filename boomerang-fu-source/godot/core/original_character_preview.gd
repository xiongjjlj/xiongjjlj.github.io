class_name OriginalCharacterPreview
extends Node3D
## Original generic clip sampler. This displays source poses; the Animator state
## machine, transitions, gameplay events and facial shader remain separate work.
var model_name := ""
var clip: Dictionary = {}
var targets: Dictionary = {}
var model: Node3D
var clip_time := 0.0
var bound_tracks := 0
var missing_targets: Array[String] = []
var skeletons: Array[Skeleton3D] = []
var action_steps: Array[Dictionary] = []
var animated_materials: Dictionary = {}
var squished_node: Node3D
var squish_elapsed := 0.0
var burnt_overrides: Array[Dictionary] = []

func play_melee(hand: String) -> void:
	var base := "res://assets/characters/" + model_name
	if hand == "disarm":
		action_steps.assign([
			{"path": base + "-windup-right.json", "speed": 1.0, "exit": 0.25},
			{"path": base + "-disarm.json", "speed": 1.0, "exit": 1.0}])
		set_clip(action_steps[0].path)
		return
	# Serialized controller: WindUpMelee -> Melee at normalized 0.25;
	# the right and left wind-up state speeds differ in the original controller.
	action_steps.assign([
		{"path": base + "-windup-" + hand + ".json", "speed": 0.8 if hand == "right" else 3.0, "exit": 0.25},
		{"path": base + "-melee-" + hand + ".json", "speed": 1.0, "exit": 1.0}])
	set_clip(action_steps[0].path)

func configure(model_path: String, clip_path: String) -> void:
	model_name = model_path.get_file().get_basename()
	model = load(model_path).instantiate()
	add_child(model)
	var visibility: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(model_path.get_basename() + ".visibility.json"))
	index_targets(model, visibility)
	var squish_path := model_path.get_basename()+"-squished.glb"
	if ResourceLoader.exists(squish_path):
		var squish_model: Node3D = load(squish_path).instantiate()
		add_child(squish_model)
		index_targets(squish_model,JSON.parse_string(FileAccess.get_file_as_string(squish_path.get_basename()+".visibility.json")))
	set_clip(clip_path)

func index_targets(node: Node, visibility: Dictionary) -> void:
	if node is Node3D:
		targets[String(node.name)] = {"node": node}
		if visibility.has(String(node.name)):
			node.visible = visibility[String(node.name)]
	if node is Skeleton3D:
		skeletons.append(node)
		for index in node.get_bone_count():
			targets[node.get_bone_name(index)] = {"skeleton": node, "index": index}
	for child in node.get_children():
		index_targets(child, visibility)

func set_clip(path: String) -> void:
	clip = JSON.parse_string(FileAccess.get_file_as_string(path))
	clip_time = 0.0
	bound_tracks = 0
	missing_targets.clear()
	for track in clip.tracks:
		if targets.has(track.node):
			bound_tracks += 1
		elif not missing_targets.has(track.node):
			missing_targets.append(track.node)
	apply_pose(0.0)
	print("BF_CHARACTER_BOUND " + JSON.stringify({"model": name, "clip": clip.name,
		"tracks": bound_tracks, "missing": missing_targets, "skeletons": skeletons.size()}))

static func sample_curve(curve: Dictionary, time: float) -> float:
	if curve.type == "constant":
		return curve.value
	if curve.type == "linear":
		var frame: float = clampf((time - curve.start) * curve.rate, 0.0, curve.values.size() - 1)
		var left := int(frame)
		return OriginalTimekeeper.lerp32(curve.values[left], curve.values[mini(left + 1, curve.values.size() - 1)], frame - left)
	var index: int = maxi(0, curve.times.bsearch(time, false) - 1)
	var key: Array = curve.keys[index]
	var t := OriginalTimekeeper.f32(maxf(0.0, time - key[0]))
	var c: Array = key[1]
	# Horner evaluation keeps the original cubic coefficients; no frame baking.
	var value := OriginalTimekeeper.f32(OriginalTimekeeper.f32(c[0] * t) + c[1])
	value = OriginalTimekeeper.f32(OriginalTimekeeper.f32(value * t) + c[2])
	return OriginalTimekeeper.f32(OriginalTimekeeper.f32(value * t) + c[3])

func advance(delta: float) -> void:
	if squished_node != null and squish_elapsed < 0.25:
		squish_elapsed = minf(0.25,squish_elapsed+delta)
		var t := squish_elapsed/0.25-1.0
		# DOTween's default OutBack overshoot, Character.IsSquished DOScale.
		squished_node.scale = Vector3.ONE * (0.8*(t*t*((1.70158+1.0)*t+1.70158)+1.0))
	if clip.is_empty(): return
	if not action_steps.is_empty():
		var remaining := delta
		for transition in 3:
			if action_steps.is_empty(): break
			var stage: Dictionary = action_steps[0]
			var until: float = maxf(0.0, clip.duration * stage.exit - clip_time) / stage.speed
			if remaining < until:
				clip_time += remaining * stage.speed
				break
			remaining -= until
			clip_time = clip.duration * stage.exit
			action_steps.pop_front()
			if not action_steps.is_empty(): set_clip(action_steps[0].path)
		apply_pose(clip_time)
		return
	clip_time += delta
	var time := 0.0 if clip.duration <= 0.0 else (fposmod(clip_time, clip.duration) if clip.loop else minf(clip_time, clip.duration))
	apply_pose(time)

func apply_pose(time: float) -> void:
	apply_tracks(clip.get("tracks", []), time)

func apply_tracks(tracks: Array, time: float) -> void:
	for track in tracks:
		if not targets.has(track.node): continue
		var values: Array[float] = []
		for curve in track.curves:
			values.append(sample_curve(curve, time))
		var target: Dictionary = targets[track.node]
		if track.property == "position":
			var value := Vector3(values[0], values[1], -values[2])
			if target.has("skeleton"):
				target.skeleton.set_bone_pose_position(target.index, value)
			else: target.node.position = value
		elif track.property == "scale":
			var value := Vector3(values[0], values[1], values[2])
			if target.has("skeleton"):
				target.skeleton.set_bone_pose_scale(target.index, value)
			else: target.node.scale = value
		elif track.property == "rotation":
			var value := Quaternion(-values[0], -values[1], values[2], values[3]).normalized()
			if target.has("skeleton"):
				target.skeleton.set_bone_pose_rotation(target.index, value)
			else: target.node.quaternion = value
		elif track.property in ["visible", "node_active"]:
			target.node.visible = values[0] >= 0.5
		elif track.property == "material_color":
			apply_material_channel(target.node, track.shader_property, int(track.channel), values[0])
		else:
			push_error("Unimplemented source animation property: " + track.property)

func apply_material_channel(node: Node3D, shader_property: String, channel: int, value: float) -> void:
	var meshes := node.find_children("*", "MeshInstance3D", true, false)
	if node is MeshInstance3D: meshes.append(node)
	for mesh: MeshInstance3D in meshes:
		for surface in mesh.mesh.get_surface_count():
			var key := "%s:%d" % [mesh.get_instance_id(), surface]
			if not animated_materials.has(key):
				var material = mesh.get_active_material(surface)
				if not material is StandardMaterial3D:
					push_error("Unsupported animated material on " + node.name)
					continue
				animated_materials[key] = material.duplicate()
				mesh.set_surface_override_material(surface, animated_materials[key])
			var material: StandardMaterial3D = animated_materials[key]
			var color := material.emission if shader_property == "_EmissionColor" else material.albedo_color
			color[channel] = value
			if shader_property == "_EmissionColor":
				material.emission_enabled = true
				material.emission = color
			else: material.albedo_color = color

var held_ghost_states: Dictionary = {}
var held_normal_materials: Dictionary = {}
var ghost_preview_material: ShaderMaterial
func set_held_discs(bindings: Dictionary, count: int, ghost := 0) -> void:
	for key in ["rightDisc", "leftDisc"]:
		var target: Dictionary = targets.get(bindings[key], {})
		if target.has("node"):
			var slot := 1 if key == "rightDisc" else 2
			target.node.visible = count >= slot or ghost == slot
			var enabled := ghost == slot
			if held_ghost_states.get(key,false) != enabled:
				set_ghost_material(target.node,enabled)
				held_ghost_states[key] = enabled
		else: push_error("Missing original held-disc object: " + bindings[key])

func set_ghost_material(node: Node, enabled: bool) -> void:
	# The original Disc_Ghost material is preserved in evidence. This temporary
	# alpha material shows unavailable fragments; its rim shader is not ported.
	if node is MeshInstance3D:
		for surface in node.mesh.get_surface_count():
			var key := "%d:%d" % [node.get_instance_id(),surface]
			if enabled:
				var original: Material = node.get_active_material(surface)
				if original == null: continue
				held_normal_materials[key] = original
				if ghost_preview_material == null:
					var shader := Shader.new()
					shader.code = "shader_type spatial; render_mode blend_mix; void fragment(){ ALBEDO=vec3(0.50588238,0.31372550,0.66274512); ALPHA=0.31; ROUGHNESS=1.0; }"
					ghost_preview_material = ShaderMaterial.new()
					ghost_preview_material.shader = shader
				node.set_surface_override_material(surface,ghost_preview_material)
			elif held_normal_materials.has(key):
				node.set_surface_override_material(surface,held_normal_materials[key])
				held_normal_materials.erase(key)
	for child in node.get_children(): set_ghost_material(child,enabled)

func set_sliced(bindings: Dictionary, sliced: bool) -> void:
	for key in ["bodyNormal", "bodySliced"]:
		for target_name in bindings[key]:
			var target: Dictionary = targets.get(target_name, {})
			if target.has("node"): target.node.visible = sliced if key == "bodySliced" else not sliced
			else: push_error("Missing original body object: " + target_name)

func set_squished(bindings: Dictionary, squished: bool) -> void:
	for target_name in bindings.get("squishableBits",[]):
		if targets.has(target_name): targets[target_name].node.visible = not squished
	var target: Dictionary = targets.get(bindings.get("bodySquished",""),{})
	if not target.has("node"): return
	target.node.visible = squished
	squished_node = target.node if squished else null
	squish_elapsed = 0.0
	if squished: squished_node.scale = Vector3.ONE * 0.000001
	if squished and bindings.has("squishedMesh") and targets.has(bindings.squishedMesh):
		var rgba:Dictionary=bindings.colorDefault
		OriginalDiscPreview.color_meshes(targets[bindings.squishedMesh].node,Color(rgba.r,rgba.g,rgba.b,rgba.a))

func set_burnt(burnt: bool) -> void:
	if not burnt:
		for entry in burnt_overrides:
			if is_instance_valid(entry.node): entry.node.set_surface_override_material(entry.surface,entry.material)
		burnt_overrides.clear()
		return
	if not burnt_overrides.is_empty(): return
	var bindings: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://assets/characters/burnt-bindings.json"))
	var data: Dictionary = bindings[model_name]
	var material := StandardMaterial3D.new()
	material.albedo_texture = load("res://assets/characters/"+data.texture)
	material.albedo_color = Color(data.color[0],data.color[1],data.color[2],data.color[3])
	material.roughness = 1.0
	for target_name in data.nodes:
		if not targets.has(target_name) or not targets[target_name].has("node"): continue
		var node: Node = targets[target_name].node
		if node is MeshInstance3D:
			for surface in node.mesh.get_surface_count():
				burnt_overrides.append({"node":node,"surface":surface,"material":node.get_surface_override_material(surface)})
				node.set_surface_override_material(surface,material)
