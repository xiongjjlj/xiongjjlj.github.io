class_name OriginalBoundaryPreview
extends Node3D
## Source boundary geometry/materials; axis-aligned native BakeMesh calibration.
var line:=MeshInstance3D.new()
var line_material:ShaderMaterial
var glow:Node3D
var model:Dictionary
var last_size:=Vector2.INF
var input:Dictionary
func material(data:Dictionary,source:Dictionary,additive:=false)->ShaderMaterial:
	var result:=ShaderMaterial.new()
	var shader:=Shader.new()
	var code:=FileAccess.get_file_as_string("res://shaders/original_boundary.gdshader")
	if additive:code=code.replace("blend_mix","blend_add").replace("cull_back","cull_disabled")
	shader.code=code
	result.shader=shader
	var color:Dictionary=data.colors["_TintColor" if additive else "_Color"]
	result.set_shader_parameter("source_tint_color",Color(color.r,color.g,color.b,color.a))
	result.set_shader_parameter("source_multiplier",2.0 if additive else 1.0)
	var tex:Dictionary=source.textures._MainTex
	result.set_shader_parameter("source_main",load("res://assets/original/"+tex.texture.path))
	result.set_shader_parameter("source_st",Vector4(tex.scale[0],tex.scale[1],tex.offset[0],tex.offset[1]))
	return result
func initialize(data:Dictionary)->void:
	input=data
	model=JSON.parse_string(FileAccess.get_file_as_string("res://assets/original/boundary-line-mesh.json"))
	line.name="OriginalBoundaryLine"
	line.cast_shadow=GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	add_child(line)
	line_material=material(data.line_material.data,data.line_material.shader_input)
	line.material_override=line_material
	var scene:Node3D=load("res://assets/characters/LevelBoundaryGlow.glb").instantiate()
	add_child(scene)
	for node in scene.find_children("*","Node3D",true,false):
		if String(node.name).ends_with("__39"):glow=node
	for mesh in scene.find_children("*","MeshInstance3D",true,false):
		mesh.material_override=material(data.materials["sharedassets1.assets_171"],data.shader_inputs["sharedassets1.assets_171"],true)
		mesh.cast_shadow=GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	glow.visible=false
func sample(row:Dictionary,size:Vector2)->Array:
	var values:Array=[]
	for i in row.base.size():values.append(row.base[i]+row.x[i]*(size.x-10.0)+row.y[i]*(size.y-10.0))
	return values
func rebuild(size:Vector2)->void:
	var vertices:=PackedVector3Array()
	var uvs:=PackedVector2Array()
	for row in model.vertices:
		var v:=sample(row,size)
		vertices.append(Vector3(v[0],v[1],-v[2]))
	for row in model.uvs:
		var uv:=sample(row,size)
		uvs.append(Vector2(uv[0],1.0-uv[1]))
	var indices:=PackedInt32Array()
	for i in range(0,model.indices.size(),3):indices.append_array([model.indices[i],model.indices[i+1],model.indices[i+2]])
	var arrays:Array=[]
	arrays.resize(Mesh.ARRAY_MAX)
	arrays[Mesh.ARRAY_VERTEX]=vertices
	arrays[Mesh.ARRAY_TEX_UV]=uvs
	arrays[Mesh.ARRAY_INDEX]=indices
	var mesh:=ArrayMesh.new()
	mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES,arrays)
	line.mesh=mesh
	last_size=size
func advance(state:OriginalLevelBoundary)->void:
	if state.size!=last_size:rebuild(state.size)
	line.visible=not state.allow_outside
	line.position=Vector3(state.center.x,state.center.y,0)
	line.rotation_degrees.z=state.angle
	line_material.set_shader_parameter("source_tint_color",state.line_color)
	line_material.set_shader_parameter("source_st",Vector4(input.line_material.shader_input.textures._MainTex.scale[0],1.0,state.scroll,0))
	glow.visible=state.glow
	if state.glow:
		glow.position=Vector3(state.center.x,state.center.y,0)
		glow.scale=Vector3(state.size.x-OriginalTimekeeper.f32(0.33),10,state.size.y-OriginalTimekeeper.f32(0.33))
