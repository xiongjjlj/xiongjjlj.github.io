class_name OriginalSceneMaterials
extends RefCounted
## Source D3D11 material formulas; native/Web pixel equivalence remains measured separately.
const SHADER_SOURCE := "res://shaders/original_uu.gdshader"
var inputs: Dictionary
var root: String
var materials: Array[ShaderMaterial] = []
var shaders: Dictionary = {}
var lighting: Dictionary
var wave_materials: Array[ShaderMaterial] = []

func initialize(asset_root: String) -> void:
	root = asset_root
	inputs = JSON.parse_string(FileAccess.get_file_as_string(root + "shader-inputs.json"))
	lighting = JSON.parse_string(FileAccess.get_file_as_string(root + "lighting-inputs.json"))

func color_value(data: Dictionary, property: String, fallback: Color) -> Color:
	var c: Dictionary = data.colors.get(property, {})
	return fallback if c.is_empty() else Color(c.r, c.g, c.b, c.a)

func make(id: String, data: Dictionary) -> ShaderMaterial:
	# Later maps can contain material IDs absent from LevelEnclosed's sidecar.
	# Keep their serialized colors, cutoff and texture transform while using the
	# same source shadow formula as the recovered materials.
	var source: Dictionary = inputs.get(id, {})
	var kind: String = str(source.get("shader", "UU/Unlit/Shadow"))
	if not source.has("textures"):
		source = {"shader": kind, "textures": {}}
		var main: Variant = data.get("main_texture", null)
		if main == null: main = {}
		if not main.is_empty():
			var transform: Variant = data.get("main_texture_transform", {})
			if transform == null: transform = {}
			var scale: Dictionary = transform.get("m_Scale", {"x": 1.0, "y": 1.0})
			var offset: Dictionary = transform.get("m_Offset", {"x": 0.0, "y": 0.0})
			source.textures["_MainTex"] = {"scale": [scale.x, scale.y], "offset": [offset.x, offset.y], "texture": main}
	if kind == "PK/SpritesDiffuseWave": return make_wave(data)
	var cutout := "Cutout" in kind or kind == "UU/Unlit/Shadow_Receiver"
	var plain := kind == "Unlit/Transparent Cutout"
	var cull := int(data.floats.get("_Cull", 2))
	var shader_key := "%d:%s" % [cull, str(plain)]
	if not shaders.has(shader_key):
		var shader := Shader.new()
		var code := FileAccess.get_file_as_string(SHADER_SOURCE)
		code = code.replace("cull_back", ["cull_disabled", "cull_front", "cull_back"][clampi(cull, 0, 2)])
		if plain: code = code.replace("ambient_light_disabled, specular_disabled", "unshaded")
		shader.code = code
		shaders[shader_key] = shader
	var material := ShaderMaterial.new()
	material.resource_name = data.name
	material.shader = shaders[shader_key]
	material.set_shader_parameter("source_albedo", color_value(data, "_Color", Color.WHITE))
	material.set_shader_parameter("source_add", color_value(data, "_AddColor", Color(0, 0, 0, 1)))
	var shadow := color_value(data, "_ShadowColor", Color.WHITE)
	material.set_shader_parameter("source_shadow", Vector3(shadow.r, shadow.g, shadow.b))
	material.set_shader_parameter("source_has_grain", "Grain" in kind)
	material.set_shader_parameter("source_has_ramp", kind.begins_with("Toon/"))
	material.set_shader_parameter("source_cutout", cutout)
	material.set_shader_parameter("source_plain_cutout", plain)
	material.set_shader_parameter("source_cutoff", data.floats.get("_Cutoff", 0.5))
	material.set_shader_parameter("source_height_fog", "Heightfog" in kind)
	material.set_shader_parameter("source_fog_depth", Vector2(data.floats.get("_FogMinDepth", 0.0), data.floats.get("_FogMaxDepth", 1.0)))
	for pair in [["_MainTex", "source_main"], ["_GrainTex", "source_grain"], ["_Ramp", "source_ramp"], ["_NoiseTex", "source_noise"]]:
		if not source.textures.has(pair[0]): continue
		var texture: Dictionary = source.textures[pair[0]]
		if texture.texture != null and texture.texture.has("path"): material.set_shader_parameter(pair[1], load(root + texture.texture.path))
		if pair[0] in ["_MainTex", "_GrainTex"]:
			material.set_shader_parameter(pair[1] + "_st", Vector4(texture.scale[0], texture.scale[1], texture.offset[0], texture.offset[1]))
	materials.append(material)
	return material

func configure_lighting(direction: Vector3, tint: Color, fog: Color) -> void:
	for material in materials:
		material.set_shader_parameter("source_sun_direction", direction.normalized())
		material.set_shader_parameter("source_tint", Vector3(tint.r, tint.g, tint.b))
		material.set_shader_parameter("source_fog_color", Vector3(fog.r, fog.g, fog.b))

func make_wave(data: Dictionary) -> ShaderMaterial:
	var material := ShaderMaterial.new()
	material.resource_name = data.name
	material.shader = load("res://shaders/original_sprite_wave.gdshader")
	material.set_shader_parameter("source_albedo", color_value(data, "_Color", Color.WHITE))
	for pair in [["_ShakeTime", "source_shake_time"], ["_ShakeWindspeed", "source_shake_speed"], ["_ShakeBending", "source_shake_bending"]]:
		material.set_shader_parameter(pair[1], data.floats[pair[0]])
	var coefficients := PackedVector3Array()
	for index in 9:
		coefficients.append(Vector3(lighting.sh_coefficients_rgb[0][index], lighting.sh_coefficients_rgb[1][index], lighting.sh_coefficients_rgb[2][index]))
	material.set_shader_parameter("source_sh", coefficients)
	var sun: Array = lighting.sun_color_gamma
	material.set_shader_parameter("source_sun_color", Vector3(sun[0], sun[1], sun[2]))
	materials.append(material)
	wave_materials.append(material)
	return material

func set_time(value: float) -> void:
	for material in wave_materials: material.set_shader_parameter("source_time", value)
