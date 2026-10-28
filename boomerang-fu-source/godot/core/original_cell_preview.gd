class_name OriginalCellPreview
extends Node3D
## Recovered materials/geometry/curves with bounded pools. Unity particle RNG,
## noise/velocity-limit modules and full transparent/shadow frame parity remain open.
const C=preload("res://core/original_timekeeper.gd")
var inputs:Dictionary
var groups:Dictionary={}
var rng:=RandomNumberGenerator.new()
var ice_view:MultiMeshInstance3D
var ice_transform:=Transform3D.IDENTITY
var decals:Array[Dictionary]=[]
var frozen:Array[OriginalCharacterPreview]=[]
var frozen_active:Array[bool]=[]
var pfx_timestamp:=0.0
var emitted:=0
var discarded:=0
var actor_emission:Array[Dictionary]=[{},{},{},{},{},{}]

func initialize(materials:OriginalSceneMaterials,direction:Vector3)->void:
	inputs=JSON.parse_string(FileAccess.get_file_as_string("res://assets/original/cell-visual-inputs.json"))
	rng.seed=0xC311
	for key in inputs.particles:
		var data:Dictionary=inputs.particles[key]
		var material:=ShaderMaterial.new()
		var shader:=Shader.new()
		var code:=FileAccess.get_file_as_string("res://shaders/original_cell_particle.gdshader")
		if data.material.shader[2].ends_with("Additive"):code=code.replace("blend_mix","blend_add")
		shader.code=code
		material.shader=shader
		material.set_shader_parameter("source_main",load("res://assets/original/"+data.material.texture))
		var quad:=QuadMesh.new()
		quad.size=Vector2.ONE
		quad.material=material
		var view:=make_pool(quad,int(data.particle.InitialModule.maxNumParticles),true)
		groups[key]={"data":data.particle,"view":view,"particles":[]}
	var model:Node3D=load("res://assets/characters/IceDecal.glb").instantiate()
	var mesh_data:=find_mesh(model,Transform3D.IDENTITY)
	var ice_mesh:Mesh=mesh_data.mesh.duplicate()
	ice_transform=mesh_data.transform
	var source:Dictionary=inputs.ice_decal.material
	materials.inputs.cell_ice={"shader":source.shader[2],"textures":{"_MainTex":{"texture":{"path":source.texture},"scale":source.scale,"offset":source.offset}}}
	var colors:Dictionary={}
	for key in source.colors:
		var value:Array=source.colors[key]
		if value.all(func(x):return x is float or x is int):colors[key]={"r":value[0],"g":value[1],"b":value[2],"a":value[3]}
	var ice_material:=materials.make("cell_ice",{"name":"Original IceBlob","colors":colors,"floats":source.floats})
	ice_mesh.surface_set_material(0,ice_material)
	ice_view=make_pool(ice_mesh,1024,false)
	model.visible=false
	add_child(model)
	for i in 6:
		var block:=OriginalCharacterPreview.new()
		block.name="FrozenBlock%d"%i
		add_child(block)
		block.configure("res://assets/characters/ActorFrozenBlock.glb","res://assets/characters/ActorFrozenBlock-idle.json")
		var frozen_material:=ShaderMaterial.new()
		frozen_material.shader=load("res://shaders/original_frozen.gdshader")
		frozen_material.set_shader_parameter("source_sun_direction",direction)
		var color:Array=materials.lighting.sun_color_gamma
		frozen_material.set_shader_parameter("source_sun_color",Vector3(color[0],color[1],color[2]))
		for mesh:MeshInstance3D in block.find_children("*","MeshInstance3D",true,false):
			mesh.material_override=frozen_material
			mesh.cast_shadow=GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
		block.targets["ActorFrozenBlock__2259"].node.position=Vector3.ZERO
		block.scale=Vector3.ONE*C.f32(1.33)
		block.visible=false
		frozen.append(block)
		frozen_active.append(false)

func make_pool(mesh:Mesh,capacity:int,colors:bool)->MultiMeshInstance3D:
	var view:=MultiMeshInstance3D.new()
	view.multimesh=MultiMesh.new()
	view.multimesh.transform_format=MultiMesh.TRANSFORM_3D
	view.multimesh.use_colors=colors
	view.multimesh.mesh=mesh
	view.multimesh.instance_count=capacity
	view.multimesh.visible_instance_count=0
	view.cast_shadow=GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	add_child(view)
	return view

func find_mesh(node:Node3D,parent:Transform3D)->Dictionary:
	var transform:=parent*node.transform
	if node is MeshInstance3D:return {"mesh":node.mesh,"transform":transform}
	for child in node.get_children():
		if child is Node3D:
			var value:=find_mesh(child,transform)
			if not value.is_empty():return value
	return {}

func scalar(data:Dictionary,time:float,random:float)->float:
	match int(data.minMaxState):
		0:return data.scalar
		1:return OriginalExplosion.curve_value(data.maxCurve.m_Curve,time)*data.scalar
		2:return lerpf(OriginalExplosion.curve_value(data.minCurve.m_Curve,time)*data.minScalar,OriginalExplosion.curve_value(data.maxCurve.m_Curve,time)*data.scalar,random)
		3:return lerpf(data.minScalar,data.scalar,random)
	return 0.0

func emit_particle(key:String,point:Vector2)->Dictionary:
	var group:Dictionary=groups[key]
	if group.particles.size()>=group.view.multimesh.instance_count:
		discarded+=1
		return {}
	var data:Dictionary=group.data
	var initial:Dictionary=data.InitialModule
	var shape:Dictionary=data.ShapeModule
	var angle:=rng.randf()*TAU
	var radius:float=sqrt(rng.randf())*float(shape.radius.value)
	var position:=Vector3(point.x+cos(angle)*radius+float(shape.m_Position.x),point.y+sin(angle)*radius+float(shape.m_Position.y),-float(shape.m_Position.z))
	var speed:=scalar(initial.startSpeed,0.0,rng.randf())
	var particle:Dictionary={"position":position,"velocity":Vector3(0,0,-speed),"age":0.0,"lifetime":scalar(initial.startLifetime,0,rng.randf()),"size":scalar(initial.startSize,0,rng.randf()),"rotation":scalar(initial.startRotation,0,rng.randf()),"gravity":scalar(initial.gravityModifier,0,rng.randf()),"random":rng.randf()}
	group.particles.append(particle)
	emitted+=1
	return particle

func fire(point:Vector2)->void:
	emit_particle("fireSystem",point)
	emit_particle("glowSystem",point)

func ice(point:Vector2)->void:
	if decals.size()>=1024:
		discarded+=1
		return
	var angle:=rng.randf()*TAU
	var offset:=Vector2(cos(angle),sin(angle))*sqrt(rng.randf())*0.3
	var data:Dictionary=inputs.ice_decal.data
	decals.append({"position":point+offset,"rotation":deg_to_rad(rng.randf_range(0,359)+rng.randf_range(-90,90)),"size":rng.randf_range(data.minSize,data.maxSize),"age":0.0,"state":1,"scale":0.0})

func chip(index:int)->void:
	if index<frozen.size():frozen[index].set_clip("res://assets/characters/ActorFrozenBlock-jiggle.json")

func update_frozen(actors:Array[OriginalActorMotion],delta:float)->void:
	for index in mini(actors.size(),frozen.size()):
		var actor:=actors[index]
		var block:=frozen[index]
		block.position=Vector3(actor.position.x,actor.position.y-0.2,-actor.source_z+C.f32(0.5*C.f32(1.33)))
		if actor.is_frozen!=frozen_active[index]:
			frozen_active[index]=actor.is_frozen
			block.visible=true
			block.set_clip("res://assets/characters/ActorFrozenBlock-"+("on" if actor.is_frozen else "off")+".json")
		block.advance(delta)
		if block.clip_time>=block.clip.duration:
			if not actor.is_frozen:block.visible=false
			elif block.clip.name!="PowerupShieldStayOn":block.set_clip("res://assets/characters/ActorFrozenBlock-idle.json")

func advance(grid:OriginalCellGrid,delta:float,camera:Camera3D)->void:
	for event in grid.events:
		if event.type=="fire_emission":fire(event.position)
		elif event.type=="ice_decal":ice(event.position)
	grid.events.clear()
	if grid.fire_count+grid.ice_count>0 and C.f32(grid.current_time-pfx_timestamp)>=C.f32(0.092):
		pfx_timestamp=grid.current_time
		var jitter:=Vector2(rng.randf_range(-0.25,0.25),rng.randf_range(-0.25,0.25))
		var indices:Array=grid.entries.keys()
		indices.sort_custom(func(a,b):return a.y<b.y or (a.y==b.y and a.x<b.x))
		for index in indices:
			var cell:Dictionary=grid.entries[index]
			var threshold:=C.f32(0.3) if cell.type==OriginalCellGrid.CellType.FIRE else 1.0
			if cell.type==OriginalCellGrid.CellType.NONE or C.f32(C.f32(grid.current_time+threshold)-cell.timestamp)>=cell.duration:continue
			var point:=grid.world_position(index)+jitter
			if cell.type==OriginalCellGrid.CellType.FIRE:fire(point)
			elif rng.randf()<0.08:emit_particle("iceSparkleSystem",point)
	for key in groups:update_particles(groups[key],delta,camera)
	update_decals(delta)

func update_particles(group:Dictionary,delta:float,camera:Camera3D)->void:
	var data:Dictionary=group.data
	var particles:Array=group.particles
	var mesh:MultiMesh=group.view.multimesh
	for i in range(particles.size()-1,-1,-1):
		var p:Dictionary=particles[i]
		p.age+=delta*data.simulationSpeed
		if p.age>=p.lifetime:particles.remove_at(i)
	for i in particles.size():
		var p:Dictionary=particles[i]
		p.velocity.y+=-9.81*p.gravity*delta
		p.position+=p.velocity*delta
		var t:float=p.age/p.lifetime
		var size:float=p.size*(scalar(data.SizeModule.curve,t,p.random) if data.SizeModule.enabled else 1.0)
		var color:=OriginalExplosion.gradient_value(data.ColorModule.gradient.maxGradient,t) if data.ColorModule.enabled else Color.WHITE
		var start:Dictionary=data.InitialModule.startColor.maxColor
		color*=Color(start.r,start.g,start.b,start.a)
		if data.RotationModule.enabled:p.rotation+=scalar(data.RotationModule.curve,t,p.random)*delta
		var basis:=camera.global_basis*Basis(Vector3.BACK,p.rotation)
		mesh.set_instance_transform(i,Transform3D(basis.scaled(Vector3.ONE*maxf(size,0.00001)),p.position))
		mesh.set_instance_color(i,color)
	mesh.visible_instance_count=particles.size()

func update_decals(delta:float)->void:
	var data:Dictionary=inputs.ice_decal.data
	for i in range(decals.size()-1,-1,-1):
		var p:Dictionary=decals[i]
		match p.state:
			1:
				if p.age<data.scaleInDuration:
					p.age=C.f32(p.age+delta)
					p.scale=OriginalExplosion.curve_value(data.scaleInCurve.m_Curve,C.f32(p.age/data.scaleInDuration))
				else:p.age=0.0;p.state=2;p.scale=OriginalExplosion.curve_value(data.scaleInCurve.m_Curve,1.0)
			2:
				if p.age<data.lifetime:p.age=C.f32(p.age+delta)
				else:p.age=0.0;p.state=3
			3:
				if p.age<data.scaleOutDuration:
					p.age=C.f32(p.age+delta)
					p.scale=OriginalExplosion.curve_value(data.scaleOutCurve.m_Curve,C.f32(p.age/data.scaleOutDuration))
				else:decals.remove_at(i)
	for i in decals.size():
		var p:Dictionary=decals[i]
		var rotation:=Basis(Vector3.BACK,p.rotation)
		var basis:Basis=rotation*ice_transform.basis.scaled(Vector3(p.size*p.scale,p.size*p.scale,3))
		ice_view.multimesh.set_instance_transform(i,Transform3D(basis,Vector3(p.position.x,p.position.y,0)+rotation*ice_transform.origin))
	ice_view.multimesh.visible_instance_count=decals.size()

func clear()->void:
	for group in groups.values():group.particles.clear();group.view.multimesh.visible_instance_count=0
	decals.clear()
	if ice_view!=null:ice_view.multimesh.visible_instance_count=0
	for i in frozen.size():frozen[i].visible=false;frozen_active[i]=false
	pfx_timestamp=0.0
	for emitter in actor_emission:emitter.clear()

func actor_fire(index:int,actor:OriginalActorMotion,delta:float)->void:
	for key in groups:
		if not inputs.particles[key].get("actor_attached",false):continue
		var emission:Dictionary=groups[key].data.EmissionModule
		if not emission.enabled:continue
		actor_emission[index][key]=actor_emission[index].get(key,0.0)+scalar(emission.rateOverTime,0.0,0.0)*delta
		var count:=int(actor_emission[index][key])
		actor_emission[index][key]-=count
		for i in count:
			var particle:=emit_particle(key,actor.position)
			if particle.is_empty():continue
			particle.actor=index
			var source:Dictionary=inputs.particles.actorFire_8909.transform.m_LocalRotation
			var rotation:=Basis(Quaternion(-source.x,-source.y,source.z,source.w))
			var local:Dictionary=inputs.particles[key].transform.m_LocalPosition
			var offset:=Vector3.ZERO if key=="actorFire_8909" else Vector3(local.x,local.y,-local.z)
			var origin:=Vector3(actor.position.x,actor.position.y,0)
			var scale_factor:float=2.0*1.33 # Source root scale 2, actor scale 1.33; Hierarchy mode.
			particle.position=origin+rotation*((particle.position-origin+offset)*scale_factor)+Vector3(0,0,1.33-actor.source_z)
			particle.velocity=rotation*particle.velocity*scale_factor
			particle.size*=scale_factor
			particle.gravity*=scale_factor

func stop_actor_fire(index:int,steam:bool)->void:
	actor_emission[index].clear()
	if steam:return
	for key in groups:
		if not inputs.particles[key].get("actor_attached",false):continue
		groups[key].particles=groups[key].particles.filter(func(p):return p.get("actor",-1)!=index)
