extends Node
func _ready()->void:
 run.call_deferred()
func color3(data:Dictionary,key:String)->Vector3:
 var c:Dictionary=data.colors[key];return Vector3(c.r,c.g,c.b)
func run()->void:
 var fixture:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://fixture.json"))
 var lighting:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://lighting.json")).maps.level4
 var coefficients:=PackedVector3Array()
 for i in 9:coefficients.append(Vector3(lighting.sh_coefficients_rgb[0][i],lighting.sh_coefficients_rgb[1][i],lighting.sh_coefficients_rgb[2][i]))
 var sun:Dictionary=lighting.lights.filter(func(l):return not l.vertex)[0]
 var materials:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://materials.json"))
 var viewport:=SubViewport.new();viewport.size=Vector2i(64,64);viewport.own_world_3d=true;viewport.render_target_update_mode=SubViewport.UPDATE_ALWAYS;add_child(viewport)
 var environment:=WorldEnvironment.new();environment.environment=Environment.new();environment.environment.background_mode=Environment.BG_COLOR;environment.environment.background_color=Color.BLACK;viewport.add_child(environment)
 var camera:=Camera3D.new();camera.fov=60;camera.near=0.1;camera.far=30;camera.position=Vector3(0,0,5);viewport.add_child(camera);camera.look_at(Vector3.ZERO);camera.current=true
 var shader:=load("res://water.gdshader");var material:=ShaderMaterial.new();material.shader=shader
 var water:=MeshInstance3D.new();var mesh:=QuadMesh.new();mesh.size=Vector2(6,6);water.mesh=mesh;water.material_override=material;viewport.add_child(water)
 var back:=MeshInstance3D.new();var back_mesh:=QuadMesh.new();back_mesh.size=Vector2(48,48);back.mesh=back_mesh;viewport.add_child(back)
 var base:=StandardMaterial3D.new();base.shading_mode=BaseMaterial3D.SHADING_MODE_UNSHADED;base.albedo_color=Color.WHITE;back.material_override=base
 var pixels:Array=[Vector2i(32,32),Vector2i(16,16),Vector2i(48,16),Vector2i(16,48),Vector2i(48,48),Vector2i(4,32),Vector2i(60,32)]
 var results:Array=[];var largest:=0.0;var failures:Array=[]
 for offset in range(0,fixture.cases.size(),7):
  var sample:Dictionary=fixture.cases[offset];var data:Dictionary=materials[sample.material]
  for pair in [["source_shallow","_ShallowColor"],["source_deep","_DeepColor"],["source_vignette","_VignetteColor"]]:material.set_shader_parameter(pair[0],color3(data,pair[1]))
  for pair in [["source_depth_offset","_DepthOffset"],["source_shallow_depth","_ShallowDepth"],["source_vignette_size","_VignetteSize"],["source_vignette_intensity","_VignetteIntensity"]]:material.set_shader_parameter(pair[0],data.floats[pair[1]])
  var source_day:bool=sample.lighting=="source_day"
  material.set_shader_parameter("source_use_sh",source_day);material.set_shader_parameter("source_sh",coefficients)
  material.set_shader_parameter("source_sun_direction",Vector3(sun.direction[0],sun.direction[1],-sun.direction[2]) if source_day else Vector3(0,0,1))
  material.set_shader_parameter("source_sun_color",Vector3(sun.color_gamma[0],sun.color_gamma[1],sun.color_gamma[2]) if source_day else Vector3.ONE)
  back.position.z=-float(sample.depth)
  await get_tree().process_frame
  await RenderingServer.frame_post_draw
  var image:Image=viewport.get_texture().get_image()
  for index in 7:
   var original:Dictionary=fixture.cases[offset+index];var pixel:Vector2i=pixels[index];var c:=image.get_pixelv(pixel);var actual:Array=[c.r,c.g,c.b];var error:=0.0
   for channel in 3:error=maxf(error,absf(actual[channel]-clampf(original.rgba[channel],0,1)))
   largest=maxf(largest,error)
   results.append({"lighting":original.lighting,"material":original.material,"depth":original.depth,"pixel":[pixel.x,pixel.y],"original":original.rgba,"web":actual,"max_error":error})
   if error>1.0/255.0:failures.append(results[-1])
 print("BF_WATER_RENDER_CHECK "+JSON.stringify({"passed":failures.is_empty(),"samples":results.size(),"max_error":largest,"tolerance":1.0/255.0,"failures":failures,"results":results,"scope":"Original water GPU float output vs Godot WebGL 8-bit output, three source materials and four depths. White direct-light and original map day-light including ambient/secondary SH; reflections/fog disabled."}))
 viewport.render_target_update_mode=SubViewport.UPDATE_DISABLED
