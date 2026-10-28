class_name OriginalDisguisePreview
extends OriginalCharacterPreview
## Original prop hierarchy and clip tracks; full Animator transitions and
## synchronized native frame comparison are still separate validation.
var props:Array[Node3D]=[]
var object_node:Node3D

func node_for_id(id:int)->Node3D:
	for key in targets:
		if String(key).ends_with("__"+str(id)) and targets[key].has("node"):return targets[key].node
	return null

func initialize(data:Dictionary,materials:OriginalSceneMaterials)->void:
	configure("res://assets/characters/Disguise.glb","res://assets/characters/Disguise-init.json")
	object_node=node_for_id(2625)
	for item in data.types:props.append(node_for_id(int(item.node)))
	materials.inputs.merge(data.shader_inputs,true)
	var cache:Dictionary={}
	for node_data in data.nodes:
		if not node_data.has("mesh"):continue
		var target:=node_for_id(int(node_data.id))
		var mesh:MeshInstance3D=target as MeshInstance3D
		if mesh==null:
			var meshes:=target.find_children("*","MeshInstance3D",true,false)
			if not meshes.is_empty():mesh=meshes[0]
		if mesh==null:continue
		for surface in node_data.mesh.materials.size():
			var key:String=node_data.mesh.materials[surface]
			if not cache.has(key):cache[key]=materials.make(key,data.materials[key])
			mesh.set_surface_override_material(surface,cache[key])
	scale=Vector3.ONE*1.3300000429153442
	visible=false

func reset()->void:
	visible=false
	set_clip("res://assets/characters/Disguise-init.json")

func advance_disguise(state:OriginalDisguise,point:Vector3,angle:float,delta:float)->void:
	position=Vector3(point.x,point.y,-point.z)
	quaternion=Quaternion(Vector3.BACK,angle)
	for event in state.events:
		if event.type=="selected":
			for index in props.size():props[index].visible=index==event.index
			if state.scales.has(event.index):props[event.index].scale=state.scales[event.index]
			if state.offsets.has(event.index):
				var offset:Vector3=state.offsets[event.index]
				props[event.index].position=Vector3(offset.x,offset.y,-offset.z)
		elif event.type=="changed":
			visible=true
			set_clip("res://assets/characters/Disguise-"+("on" if event.value else "off")+".json")
			print("BF_DISGUISE "+JSON.stringify({"actor":name,"active":event.value,"prop":state.selected}))
		elif event.type=="jiggle":set_clip("res://assets/characters/Disguise-jiggle.json")
	state.events.clear()
	advance(delta)
	if clip_time>=clip.duration:
		if state.disguised and clip.name!="DisguiseStayOn":set_clip("res://assets/characters/Disguise-idle.json")
		elif not state.disguised:visible=false
	state.obstacle_scale_y=object_node.scale.y
