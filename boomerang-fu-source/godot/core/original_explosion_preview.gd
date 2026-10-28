class_name OriginalExplosionPreview
extends MeshInstance3D
var source_material := ShaderMaterial.new()
var revision := -1
func initialize() -> void:
	var data: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://assets/original/explosion-visual-inputs.json"))
	source_material.shader = load("res://shaders/original_explosion.gdshader")
	source_material.set_shader_parameter("source_main",load("res://assets/original/"+data.texture))
	material_override = source_material
	cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
func update_blast(blast: OriginalExplosion) -> void:
	position = Vector3(blast.position.x,blast.position.y,-blast.source_z)
	source_material.set_shader_parameter("source_tint",blast.color)
	if revision == blast.mesh_revision: return
	revision = blast.mesh_revision
	if blast.vertices.is_empty(): return
	var arrays: Array = []
	arrays.resize(Mesh.ARRAY_MAX)
	arrays[Mesh.ARRAY_VERTEX] = blast.vertices
	var uv := PackedVector2Array()
	for point in blast.vertices:
		var value := Vector2(point.x,point.y)/(blast.radius*2.0) if blast.radius > 0.0 else Vector2.ZERO
		# Original material _MainTex_ST is (1,1,.5,.5); imported Y is inverted.
		uv.append(Vector2(value.x+0.5,0.5-value.y))
	arrays[Mesh.ARRAY_TEX_UV] = uv
	arrays[Mesh.ARRAY_INDEX] = blast.triangles
	var surface := ArrayMesh.new()
	surface.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES,arrays)
	mesh = surface
