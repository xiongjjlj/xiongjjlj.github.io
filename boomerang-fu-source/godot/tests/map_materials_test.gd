extends SceneTree
var failures:Array=[]
var checks:=0
func check(condition:bool,message:String)->void:
 checks+=1
 if not condition:failures.append(message)
func _init()->void:
 run.call_deferred()
func run()->void:
 var root_path:="res://assets/original/"
 var manifest:Dictionary=JSON.parse_string(FileAccess.get_file_as_string(root_path+"manifest.json"))
 var sprites:Dictionary=JSON.parse_string(FileAccess.get_file_as_string(root_path+"sprite-inputs.json"))
 var data:Dictionary=manifest.materials.duplicate(true);data.merge(sprites.materials)
 var factory:=OriginalSceneMaterials.new();factory.initialize(root_path,"level4")
 check(data.size()==219,"219 imported scene/sprite materials")
 var water_count:=0;var bound_textures:=0
 for id in data:
  check(factory.inputs.has(id),"Missing shader sidecar: "+id)
  var input:Dictionary=factory.inputs[id];var material:ShaderMaterial=factory.make(id,data[id]);var kind:=str(input.shader)
  check(material!=null and material.shader!=null,"Missing material/shader: "+id)
  if kind=="FX/Water_DepthVignette":
   water_count+=1
   check(material.shader.resource_path=="res://shaders/original_water.gdshader","Water factory dispatch: "+id)
   check(material.get_shader_parameter("source_use_sh")==true,"Source map SH configured: "+id)
   for pair in [["source_shallow","_ShallowColor"],["source_deep","_DeepColor"],["source_vignette","_VignetteColor"]]:
    var c:Dictionary=data[id].colors[pair[1]];var expected:=Vector3(c.r,c.g,c.b)
    check(material.get_shader_parameter(pair[0]).distance_to(expected)<0.000001,"Source water color: "+id+pair[0])
   continue
  if kind=="PK/SpritesDiffuseWave":continue # Sprite renderer supplies its own texture.
  for pair in [["_MainTex","source_main"],["_GrainTex","source_grain"],["_Ramp","source_ramp"],["_NoiseTex","source_noise"],["_TileTex","source_tile"]]:
   if not input.textures.has(pair[0]):continue
   var texture:Variant=input.textures[pair[0]].texture
   if texture==null or not texture.has("path"):continue
   bound_textures+=1;var actual:Texture2D=material.get_shader_parameter(pair[1])
   check(actual!=null and actual.resource_path==root_path+texture.path,"Texture binding: "+id+pair[0])
 check(water_count==3,"Three source water materials")
 var light_data:Dictionary=JSON.parse_string(FileAccess.get_file_as_string(root_path+"water-lighting-inputs.json"))
 check(light_data.maps.size()==59,"59 resource-scene light records")
 for key in light_data.maps:
  var lights:Array=light_data.maps[key].lights
  check(lights.filter(func(l):return not l.vertex).size()==1,"One source layer-0 main light: "+key)
  check(light_data.maps[key].sh_coefficients_rgb.size()==3,"RGB SH channels: "+key)
 var result:={"passed":failures.is_empty(),"checks":checks,"failures":failures,"materials":data.size(),"water_materials":water_count,"bound_texture_slots":bound_textures,"scope":"Shader identity, preserved inputs, texture binding and layer-0 lighting data. Does not verify all shader families or full-scene pixel parity."}
 var file:=FileAccess.open("res://../evidence/godot-map-materials-check.json",FileAccess.WRITE);file.store_string(JSON.stringify(result,"  ")+"\n");file.close();print("BF_MAP_MATERIALS "+JSON.stringify(result))
 quit(0 if failures.is_empty() else 1)
