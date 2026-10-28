class_name OriginalFloorHazards
extends RefCounted
## Scene ownership and original level collapse scheduling for floor hazards.
const C=preload("res://core/original_timekeeper.gd")
const H=preload("res://core/original_floor_hazard.gd")
var floors:Dictionary={}
var objects:Dictionary={}
var nodes:Dictionary={}
var contacts:Dictionary={}
var world:OriginalCollisionWorld
var rng:OriginalUnityRandom
var navigation:WeakRef
var settings:Dictionary={}
var collapsing:=false
var countdown:=0.0
var playlist:Array=[]
var events:Array[Dictionary]=[]

func initialize(sidecar:Dictionary,scene_nodes:Dictionary,collision:OriginalCollisionWorld,random:OriginalUnityRandom,data:Dictionary,materials:OriginalSceneMaterials)->void:
	nodes=scene_nodes;world=collision;rng=random;settings=data
	floors.clear();objects.clear()
	for record:Dictionary in sidecar.get("behaviours",[]):
		if record["class"] not in ["TrapFloor","FallingFloor"] or not record.active_in_hierarchy or not record.data.m_Enabled:continue
		var id:=int(record.component_id);var object_id:=int(record.game_object)
		var visual_id:=int(record.resolved_refs.floor);var renderer_id:=int(record.resolved_refs.floorRenderer)
		var node:Node3D=nodes[object_id];var visual:Node3D=nodes[visual_id]
		var material_data:Dictionary=data.materials[str(id)]
		var color:Dictionary=material_data.flash
		var model:=H.new();var p:=visual.global_position
		model.initialize(record["class"]=="TrapFloor",Vector3(p.x,p.y,-p.z),Color(color.r,color.g,color.b,color.a),rng,int(record.data.get("tileSize",0)))
		var box:Dictionary={}
		for component in world.source_components[object_id]:
			if component.type=="BoxCollider2D":box=component.data;break
		assert(not box.is_empty())
		var copies:Array[ShaderMaterial]=[]
		for child in nodes[renderer_id].get_children():
			if child is not MeshInstance3D:continue
			for surface in child.mesh.get_surface_count():
				var original:Material=child.get_active_material(surface)
				if original is ShaderMaterial:
					var copy:ShaderMaterial=original.duplicate();copies.append(copy)
					child.set_surface_override_material(surface,copy);materials.materials.append(copy)
		var initial:Dictionary={}
		for key in nodes:
			if nodes[key]==visual or visual.is_ancestor_of(nodes[key]):initial[key]={"visible":nodes[key].visible,"transform":nodes[key].transform}
		floors[id]={"id":id,"object_id":object_id,"visual_id":visual_id,"node":node,"visual":visual,"model":model,"box":box,"materials":copies,"initial":initial}
		objects[object_id]=id
		if model.trap:
			for shape in world.shapes:
				if not shape.actor and int(shape.object_id)==object_id:shape.tag="TrapFloor"
	reset(false)

func reset(randomize:=true)->void:
	contacts.clear();events.clear();playlist.clear()
	collapsing=bool(settings.get("collapsing",false));countdown=C.f32(settings.get("duration",0.5))
	for item in floors.values():
		item.model.reset()
		for key in item.initial:
			var old:Dictionary=item.initial[key];nodes[key].transform=old.transform
			world.set_scene_object_enabled(int(key),old.visible)
		set_layer(item,16);apply_visual(item)
	initialize_playlist(randomize)

func initialize_playlist(randomize:=true)->void:
	playlist.clear();collapsing=bool(settings.get("collapsing",false));countdown=C.f32(settings.get("duration",0.5))
	if collapsing and randomize:
		var size:Dictionary=settings.box_size
		var point:=Vector2(C.f32(rng.range_float(-size.x,size.x)*C.f32(0.4)),C.f32(rng.range_float(-size.y,size.y)*C.f32(0.4)))
		playlist=settings.falling_order.duplicate()
		playlist.sort_custom(func(a,b):return distance(floors[int(a)],point)<distance(floors[int(b)],point))

func distance(item:Dictionary,point:Vector2)->float:
	var p:Vector3=item.node.global_position
	return OriginalActorMotion.magnitude(point-Vector2(p.x,p.y))

func set_layer(item:Dictionary,layer:int)->void:
	for shape in world.shapes:
		if not shape.actor and int(shape.object_id)==int(item.object_id):shape.layer=1<<layer

func start(item:Dictionary,playing:=true)->bool:
	if not item.model.start_falling(playing):return false
	consume(item);return true

func fixed_contacts(actors:Array,ground:OriginalGroundQueries,playing:=true)->void:
	if floors.is_empty():return
	var current:Dictionary={}
	for actor:OriginalActorMotion in actors:
		if not actor.physics_enabled:continue
		for hit in world.overlaps(actor.position,actor.radius,ground.floor_mask):
			var id:=int(objects.get(int(hit.shape.get("object_id",-1)),-1))
			if id<0 or not floors[id].model.trap:continue
			var key:=str(actor.get_instance_id())+":"+str(id);current[key]=true
			if contacts.has(key) and not actor.is_dashing and not ground.solid_ground(actor.position,C.f32(actor.radius*C.f32(0.95))):start(floors[id],playing)
	contacts=current

func level_step(playing:bool,opponents:int,fixed_delta:float)->void:
	fixed_delta=C.f32(fixed_delta)
	if playing and collapsing and playlist.size()>6 and opponents>1:
		if countdown>0.0:countdown=C.f32(countdown-fixed_delta)
		else:
			countdown=C.f32(C.f32(settings.duration)*rng.range_float(0.9,1.111))
			start(floors[int(playlist.pop_back())],playing)
	if playing and collapsing and opponents<=1:collapsing=false

func frame_step(delta:float,playing:bool,opponents:int,fixed_delta:float)->void:
	level_step(playing,opponents,fixed_delta)
	for item in floors.values():
		var model:OriginalFloorHazard=item.model
		if model.state==H.State.IDLE or (not model.trap and model.state==H.State.RESPAWNING):continue
		var old_layer:=model.layer;var old_position:=model.position
		model.frame_step(delta,collapsing)
		if model.layer!=old_layer:set_layer(item,model.layer)
		apply_visual(item)
		if old_position!=model.position:world.sync_scene_subtree(int(item.visual_id))
		consume(item)

func apply_visual(item:Dictionary)->void:
	var model:OriginalFloorHazard=item.model
	item.visual.global_position=Vector3(model.position.x,model.position.y,-model.position.z)
	for material:ShaderMaterial in item.materials:material.set_shader_parameter("source_flash",model.flash)

func root_polygon(item:Dictionary)->PackedVector2Array:
	# The source query uses unscaled BoxCollider2D.size and transform rotation,
	# not the collider offset or visual renderer bounds.
	var half:=Vector2(item.box.m_Size.x,item.box.m_Size.y)*0.5
	var p:Vector3=item.node.global_position;var angle:float=item.node.global_rotation.z
	var polygon:=PackedVector2Array()
	for v:Vector2 in [Vector2(-half.x,-half.y),Vector2(-half.x,half.y),half,Vector2(half.x,-half.y)]:polygon.append(Vector2(p.x,p.y)+v.rotated(angle))
	return polygon

func consume(item:Dictionary)->void:
	var nav:OriginalNavigationWorld=navigation.get_ref() if navigation!=null else null
	for call:String in item.model.events:
		if call=="rescan" and nav!=null:nav.rescan(world.scene_bounds(item.object_id))
		elif call=="tag" and nav!=null:nav.tag_area(root_polygon(item))
		events.append({"type":call,"floor":item.id,"object_id":item.object_id,"position":Vector2(item.node.global_position.x,item.node.global_position.y),"source_z":-item.node.global_position.z,"trap":item.model.trap})
	item.model.events.clear()

func release_children(item:Dictionary,behaviors:OriginalLevel3Behaviors)->void:
	var children:Array=[]
	for key in item.initial:
		if nodes[key].is_visible_in_tree():children.append(int(key))
	for key in children:
		# Scene projectiles are represented separately from source hierarchy;
		# the runtime owns their interpolation/attachment adapters.
		if item.model.trap:
			for obstacle in behaviors.destructables.values():
				if not obstacle.node.is_visible_in_tree() or not item.visual.is_ancestor_of(obstacle.node):continue
				obstacle.hits_left-=1
				if obstacle.hits_left==0:
					obstacle.destroyed=true;world.set_scene_object_enabled(obstacle.game_object,false)
		else:world.set_scene_object_enabled(key,false)

func contains(item:Dictionary,point:Vector2)->bool:
	for shape in world.shapes:
		if shape.actor or int(shape.object_id)!=int(item.object_id):continue
		if shape.kind=="polygon" and Geometry2D.is_point_in_polygon(point,shape.vertices):return true
	return false
