class_name OriginalDecoyWorld
extends RefCounted
## GameManager/Player spawn lifecycle and Decoy routing, separate from FFA scores.
## Static Enclosed graph; native callback order and pooled-object reuse remain open.
const C=preload("res://core/original_timekeeper.gd")
const A=preload("res://core/original_actor_motion.gd")
const M=preload("res://core/original_decoy_motion.gd")
const I=preload("res://core/original_decoy_input.gd")
const AI=preload("res://core/original_decoy_ai.gd")
var actors:Array[OriginalActorMotion]=[]
var weapons:Array[OriginalPlayerWeapons]=[]
var world:OriginalCollisionWorld
var ground:OriginalGroundQueries
var grid:OriginalCellGrid
var boundary:OriginalLevelBoundary
var navigation:OriginalNavigation
var inputs:Dictionary
var rng:=OriginalUnityRandom.new(0xB00F1)
var members:Dictionary={}
var created:Dictionary={}
var events:Array[Dictionary]=[]
var serial:=0
var current_time:=0.0
var realtime:=0.0
var playing:=true
var duration_group:Dictionary={"value":3.0}
var owner_deadlines:Dictionary={}
var survivor_check:=-1.0
var survivor_defer:=false
var kill_all_due:=-1.0
var kill_all_index:=0
var melee_contacts:Dictionary={}
var disc_contacts:Dictionary={}
var combat:WeakRef

func reset_round()->void:
	for item in members.values():world.unregister_actor(item.actor)
	members.clear()
	created.clear()
	owner_deadlines.clear()
	melee_contacts.clear()
	disc_contacts.clear()
	survivor_check=-1.0
	kill_all_due=-1.0
	events.clear()

func after_successful_dash(owner_index:int)->bool:
	if not playing or created.has(owner_index) or not weapons[owner_index].powers.has_power(32768):return false
	created[owner_index]=true
	serial+=1
	var owner:=actors[owner_index]
	var actor:=M.new()
	actor.serial=serial
	actor.owner_actor=owner
	actor.lifecycle_hook=weakref(self)
	actor.world=world
	actor.position=owner.position
	actor.source_z=owner.source_z
	actor.rotation_z=0.0
	actor.last_rotation=Vector2.ZERO
	actor.last_input_move=Vector2.ZERO
	actor.move_faster=weapons[owner_index].powers.has_power(2)
	actor.is_shielded=weapons[owner_index].powers.has_power(16)
	world.register_actor(actor)
	world.ignore_actor_pair(actor,owner)
	var environment:=OriginalActorEnvironment.new()
	environment.initialize(actor,ground)
	environment.boundary=boundary
	var bounds:=OriginalActorBounds.new()
	bounds.actor=actor
	bounds.boundary=boundary
	bounds.decoy_durations=duration_group
	var input:=I.new()
	input.actor=actor
	input.input_move=Vector2(0,-1) # Original prefab, before AI's first update.
	var ai:=AI.new()
	ai.actor=actor
	ai.decoy_input=input
	ai.boundary=boundary
	ai.rng=rng
	ai.path_provider=navigation.actor_path
	ai.dash_safety=environment.is_dash_safe
	var query_ground:=ground
	ai.point_hazard=func(p:Vector3)->bool:return not query_ground.point_on_layer(Vector2(p.x,p.y),query_ground.floor_mask)
	var radius:=C.f32(actor.radius*C.f32(1.15))
	ai.touching_hazard=func(p:Vector3)->bool:return query_ground.hazard_contacts(Vector2(p.x,p.y),radius)>0
	var weak_actor:WeakRef=weakref(actor)
	var query_world:=world
	ai.avoidance_query=func(direction:Vector2,distance:float)->Variant:
		var a:OriginalActorMotion=weak_actor.get_ref()
		var hit:=query_world.circle_cast(a.position,0.0,A.normalized(direction),distance,512)
		if hit.is_empty():return null
		var delta:Vector2=hit.point-a.position
		if not query_world.circle_cast(a.position,0.0,A.normalized(delta),A.magnitude(delta),256).is_empty():return null
		return Vector3(hit.point.x,hit.point.y,0)
	var cells:=grid
	ai.fire_query=func(p:Vector3)->bool:return cells.touching(Vector2(p.x,p.y),OriginalCellGrid.CellType.FIRE).touch
	var elements:=OriginalDecoyElements.new()
	elements.actor=actor
	elements.grid=grid
	elements.index=-1000000-serial
	elements.lifecycle_hook=weakref(self)
	elements.serial=serial
	elements.unity_rng=rng
	actor.elements_hook=weakref(elements)
	var item:Dictionary={"id":serial,"owner":owner_index,"actor":actor,"ai":ai,"input":input,"environment":environment,"bounds":bounds,"elements":elements,"powers":weapons[owner_index].powers.active,"held":maxi(1,weapons[owner_index].held_discs),"spawned":current_time,"previous":actor.position,"previous_rotation":0.0,"render":actor.position,"render_rotation":0.0}
	members[serial]=item
	ai.initialize()
	var direction:=A.normalized(rng.inside_unit_circle())
	var facing:=Vector2(C.f32(-sin(owner.rotation_z)),C.f32(cos(owner.rotation_z)))
	if C.f32(direction.x*facing.x+direction.y*facing.y)>0.5:direction=-direction
	ai.force_dash(direction)
	item.render_rotation=actor.rotation_z
	item.previous_rotation=actor.rotation_z
	events.append({"type":"spawn","id":serial,"owner":owner_index,"position":actor.position,"powers":item.powers,"held":item.held})
	return true

func die(id:int,cause:String)->bool:
	if not playing or not members.has(id):return false
	var item:Dictionary=members[id]
	var actor:OriginalActorMotion=item.actor
	if actor.state==A.State.DEAD:return false
	actor.state=A.State.DEAD
	actor.is_dashing=false
	actor.velocity=Vector2.ZERO
	actor.physics_enabled=false
	actor.appearance_visible=false
	actor.is_shielded=false
	world.unregister_actor(actor)
	members.erase(id)
	owner_deadlines.erase(id)
	events.append({"type":"death","id":id,"owner":item.owner,"cause":cause,"position":actor.position})
	return true

func on_owner_death(owner:int)->void:
	for item in members.values():
		if item.owner==owner and item.actor.state==A.State.ALIVE:owner_deadlines[item.id]=C.f32(realtime+C.f32(0.333))
	survivor_check=C.f32(0.15)
	survivor_defer=true

func lifecycle_frame(delta:float,time:float,unscaled:float,is_playing:bool)->void:
	current_time=C.f32(time)
	realtime=C.f32(unscaled)
	playing=is_playing
	for id in owner_deadlines.keys():
		if realtime>=owner_deadlines[id]:
			owner_deadlines.erase(id)
			if members.has(id) and members[id].actor.state==A.State.ALIVE:die(id,"owner")
	if survivor_defer:survivor_defer=false
	elif survivor_check>=0.0:
		survivor_check=C.f32(survivor_check-C.f32(delta))
		if survivor_check<=0.0:
			survivor_check=-1.0
			if actors.filter(func(a):return a.state!=A.State.DEAD).size()<=1:
				kill_all_due=C.f32(realtime+C.f32(0.2))
				kill_all_index=0
	if kill_all_due>=0.0 and realtime>=kill_all_due:
		var remaining:=members.keys()
		if kill_all_index<remaining.size():
			die(remaining[kill_all_index],"round_end")
			kill_all_index+=1 # Source iterates the same shrinking activeDecoys list.
			kill_all_due=C.f32(realtime+C.f32(0.1))
		else:kill_all_due=-1.0

func fixed_step()->void:
	for item in members.values():
		item.previous=item.actor.position
		item.previous_rotation=item.actor.rotation_z
		item.actor.input_move=item.input.input_move
		item.environment.before_fixed_step()
		item.actor.fixed_step()
		consume_environment(item)

func prepare_render(alpha:float)->void:
	for item in members.values():
		item.render=item.previous.lerp(item.actor.position,alpha)
		item.render_rotation=lerp_angle(item.previous_rotation,item.actor.rotation_z,alpha)

func ai_phase(delta:float,only_phase:=-1)->void:
	for phase in 2:
		if only_phase>=0 and phase!=only_phase:continue
		for item in members.values():
			var physics:Vector2=item.actor.position
			var rotation:float=item.actor.rotation_z
			var snaps:int=item.ai.snaps
			item.actor.position=item.render
			item.actor.rotation_z=item.render_rotation
			if phase==0:item.ai.ai_update(delta,playing)
			else:item.ai.state_update(delta)
			if item.ai.snaps>snaps:
				item.previous=item.actor.position
				item.previous_rotation=item.actor.rotation_z
				item.render_rotation=item.actor.rotation_z
			else:
				item.actor.rotation_z=rotation
				item.actor.position=physics
			if phase==1:item.ai.events.clear()

func actor_phase(delta:float)->void:
	for item in members.values():
		item.actor.frame_step(delta)
		item.environment.frame_step(delta)
		item.elements.timers(delta)
		item.actor.time_scaler=1.0
		if playing:item.bounds.frame_step(delta,item.render)
		if item.bounds.death_requested:die(item.id,"outofbounds")
		consume_environment(item)
		item.elements.events.clear()

func input_phase()->void:
	for item in members.values():
		var physics:Vector2=item.actor.position
		var before:int=item.actor.successful_dashes
		item.actor.position=item.render
		item.input.frame_step(I.GameState.PLAYING if playing else I.GameState.MENU,current_time)
		if item.actor.successful_dashes>before:
			item.previous=item.actor.position
			item.previous_rotation=item.actor.rotation_z
			item.render_rotation=item.actor.rotation_z
		else:item.actor.position=physics
		item.ai.deliver_path_result()

func end_frame()->void:
	for item in members.values():
		item.ai.coroutine_phase()
		item.input.end_frame()

func consume_environment(item:Dictionary)->void:
	for event in item.environment.events:
		if event.type=="fall_death":die(item.id,"fall")
	item.environment.events.clear()

func damage(id:int,cause:String)->bool:
	if not members.has(id) or members[id].actor.is_dashing:return false
	return die(id,cause)

func append_disc_hits(disc:OriginalDiscMotion,direction:Vector2,distance:float,hits:Array[Dictionary])->void:
	if not disc.projectile_layer or disc.started_dropped or disc.movement==OriginalDiscMotion.Movement.DROPPED:return
	for item in members.values():
		var hit:=OriginalCollisionWorld.cast_point_circle(disc.position,direction,distance,item.actor.position,C.f32(C.f32(0.75)*C.f32(1.33)+disc.radius))
		if not hit.is_empty():hits.append({"kind":"decoy","id":item.id,"distance":hit.distance})

func projectile_contact(id:int,disc:OriginalDiscMotion)->void:
	if disc.owner_actor!=null:disc.can_home=false
	damage(id,"disc")

func append_current_contacts(disc:OriginalDiscMotion,contacts:Array[Dictionary])->void:
	if not disc.projectile_layer or disc.started_dropped or disc.movement==OriginalDiscMotion.Movement.DROPPED:return
	for item in members.values():
		if not OriginalCollisionWorld.penetration(disc.position,disc.radius,{"kind":"circle","center":item.actor.position,"radius":C.f32(C.f32(0.75)*C.f32(1.33))},true).is_empty():contacts.append({"kind":"decoy","id":item.id})

func native_contacts(discs:Array[OriginalDiscMotion])->void:
	var next_melee:Dictionary={}
	var next_disc:Dictionary={}
	var router:OriginalCombatWorld=combat.get_ref() if combat!=null else null
	for item in members.values():
		var actor:OriginalActorMotion=item.actor
		for disc in discs:
			if not disc.active or not disc.projectile_layer or disc.started_dropped or disc.movement==OriginalDiscMotion.Movement.DROPPED or disc.collider_disabled_remaining>0.0:continue
			if A.magnitude(disc.position-actor.position)<C.f32(C.f32(0.75)*C.f32(1.33)+disc.collider_radius):
				var key:=Vector2i(item.id,disc.get_instance_id())
				next_disc[key]=true
				if not disc_contacts.has(key):projectile_contact(item.id,disc)
		if router==null:continue
		for i in actors.size():
			var attacker:=actors[i]
			if attacker.state==A.State.DEAD:continue
			for kind in ["melee","disarm"]:
				if not (attacker.is_attacking if kind=="melee" else attacker.is_disarming):continue
				var polygon:=router.melee_polygon(i) if kind=="melee" else router.disarm_polygon(i)
				if OriginalCollisionWorld.penetration(actor.position,C.f32(C.f32(0.75)*C.f32(1.33)),{"kind":"polygon","vertices":polygon}).is_empty():continue
				var key:="%d/%d/%s"%[item.id,i,kind]
				next_melee[key]=true
				var center:=router.melee_center(i) if kind=="melee" else router.disarm_center(i)
				if not melee_contacts.has(key) and not router.line_blocked(actor.position,center):damage(item.id,kind)
	melee_contacts=next_melee
	disc_contacts=next_disc

func apply_explosion(blast:OriginalExplosion)->void:
	if not blast.damage_active:return
	for item in members.values():
		if item.actor.is_dashing or item.actor.is_invulnerable:continue
		if blast.reaches(item.actor.position,item.actor.source_z,C.f32(C.f32(0.75)*C.f32(1.33))):die(item.id,"explosion")

func telefrag(destination:Vector2,radius:float)->void:
	for item in members.values():
		if A.magnitude(item.actor.position-destination)<C.f32(radius+item.actor.radius):die(item.id,"telefrag")
