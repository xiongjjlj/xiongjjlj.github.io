extends SceneTree
var failures:Array=[]
var checks:=0
func check(condition:bool,message:String)->void:
 checks+=1
 if not condition:failures.append(message)
func _init()->void:
 run.call_deferred()
func run()->void:
 var game:Node=load("res://main.tscn").instantiate();root.add_child(game)
 await process_frame
 var key:=str(game.active_scene.file).get_file().get_basename()
 var source:Dictionary=game.scene_materials.water_lighting
 check(not source.is_empty(),"Scene-specific source lighting exists")
 var primary:Array=source.lights.filter(func(light):return not light.vertex)
 check(primary.size()==1,"One light affects source water layer")
 var expected:=Vector3(primary[0].color_gamma[0],primary[0].color_gamma[1],primary[0].color_gamma[2])
 var found:=0
 var visible_water:=0
 for mesh:MeshInstance3D in game.find_children("*","MeshInstance3D",true,false):
  if mesh.mesh==null:continue # Runtime effect/retired preview nodes may have no mesh.
  for index in mesh.mesh.get_surface_count():
   var material:=mesh.get_active_material(index)
   if material is not ShaderMaterial or material.shader.resource_path!="res://shaders/original_water.gdshader":continue
   found+=1
   if mesh.is_visible_in_tree():visible_water+=1
   check(material.get_shader_parameter("source_sun_color").distance_to(expected)<0.000001,"Water uses its map's sun")
   check(material.get_shader_parameter("source_use_sh"),"Water uses source SH")
   check(material.get_shader_parameter("source_sh").size()==9,"Nine SH coefficients")
 check(found>0,"Imported water mesh uses restored shader")
 # Serialized source hierarchy: Turnstile's Ocean and three Onsen alternatives
 # are disabled. Do not activate them merely because their material is restored.
 var source_visible:Dictionary={"level4":0,"level15":1,"level34":3,"level40":1,"level44":1,"level59":1}
 check(source_visible.has(key),"Fixture has a source visibility expectation")
 check(visible_water==source_visible.get(key,-1),"Source water visibility is preserved")
 check(game.actors.size()==2,"Gameplay still initializes")
 var result:={"passed":failures.is_empty(),"checks":checks,"failures":failures,"map":key,"scene":game.active_scene.name,"water_surfaces":found,"visible_water_surfaces":visible_water,"scope":"Actual imported scene, source water visibility, material dispatch and map-specific light bindings; GPU pixel parity is covered only by the controlled water fixture."}
 var file:=FileAccess.open("res://../evidence/godot-water-scene-"+key+"-check.json",FileAccess.WRITE);file.store_string(JSON.stringify(result,"  ")+"\n");file.close();print("BF_WATER_SCENE "+JSON.stringify(result))
 game.queue_free();await process_frame
 quit(0 if failures.is_empty() else 1)
