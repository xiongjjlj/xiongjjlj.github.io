class_name OriginalPlayerAiWorld
extends RefCounted
## Live FFA adapter for LevelEnclosed. Keeps target dictionaries alive across
## frames; removal marks retained targets inactive. No controller samples bots.
const AI=preload("res://core/original_player_ai.gd")
const C=preload("res://core/original_timekeeper.gd")
const A=preload("res://core/original_actor_motion.gd")
const D=preload("res://core/original_disc_motion.gd")
var scene:WeakRef
var members:Dictionary={}
var records:Dictionary={}
var flee_random:Dictionary={}
var peak_records:=0

func dispose()->void:
	for ai:OriginalPlayerAI in members.values():ai.dispose()
	members.clear();records.clear();flee_random.clear()

func idle_frame(delta:float)->void:
	for ai:OriginalPlayerAI in members.values():
		ai.playing=false;ai.buttons.begin_frame(delta)

func initialize(game:Node)->void:
	# Unity keeps PlayerAI as a component when a round is reset. The Godot
	# scene rebuilds the lightweight Actor records, so retain the AI object and
	# rebind its actor when this is the same match scene. A scene replacement or
	# a removed slot still disposes the old object and its closures.
	var previous:Dictionary=members.duplicate()
	var same_scene:bool=scene!=null and scene.get_ref()==game
	var reusable:Dictionary={}
	if same_scene:
		for key in previous.keys():
			if key is int and key<game.actors.size() and FfaSetup.is_bot(game.setup.sources[key]):reusable[key]=previous[key]
	for key in previous.keys():
		if not reusable.has(key):
			var old:OriginalPlayerAI=previous[key];old.dispose()
	members.clear();scene=weakref(game)
	for i in game.actors.size():
		if not FfaSetup.is_bot(game.setup.sources[i]):continue
		var ai:OriginalPlayerAI=reusable.get(i,AI.new())
		var weak_ai:WeakRef=weakref(ai);var weak_world:WeakRef=weakref(self)
		ai.actor=game.actors[i];ai.boundary=game.boundary;ai.rng=game.decoys.rng
		ai.policy.difficulty=game.setup.bot_difficulty
		ai.path_provider=game.decoys.navigation.actor_path
		# Astar's native pool/tick-count seed is not yet reproduced; this stream
		# stays bounded and deterministic per bot for integration replay.
		flee_random[i]=OriginalSystemRandom.new(0xF1EE+i)
		ai.flee_provider=func(start:Vector3,avoid:Vector3)->Dictionary:return game.decoys.navigation.actor_flee_path(start,avoid,weak_world.get_ref().flee_random[i])
		ai.world_query=func(kind:String,args:Array)->Variant:return weak_world.get_ref().query(i,kind,args)
		var ground:OriginalGroundQueries=game.actor_environments[i].ground
		ai.point_hazard=func(p:Vector3)->bool:return not ground.point_on_layer(Vector2(p.x,p.y),ground.floor_mask)
		ai.touching_hazard=func(p:Vector3)->bool:return ground.hazard_contacts(Vector2(p.x,p.y),C.f32(weak_ai.get_ref().actor.radius*C.f32(1.15)))>0
		ai.avoidance_query=func(direction:Vector2,distance:float)->Variant:
			var body:OriginalActorMotion=weak_ai.get_ref().actor
			var hit:Dictionary=game.collision_world.circle_cast(body.position,0.0,A.normalized(direction),distance,512)
			if hit.is_empty() or game.combat.line_blocked(body.position,hit.point):return null
			return Vector3(hit.point.x,hit.point.y,0)
		ai.fire_query=func(p:Vector3)->bool:return game.cell_grid.touching(Vector2(p.x,p.y),OriginalCellGrid.CellType.FIRE).touch
		members[i]=ai
	refresh()
	for ai:OriginalPlayerAI in members.values():ai.initialize()

func record(key:String,data:Dictionary)->Dictionary:
	if not records.has(key):records[key]={}
	var item:Dictionary=records[key]
	item.merge(data,true);item.active=true
	return item

func player_snapshot(i:int)->Dictionary:
	var game:Node=scene.get_ref()
	var actor:OriginalActorMotion=game.actors[i]
	var weapon:OriginalPlayerWeapons=game.weapons[i]
	var pos:Vector3=game.actor_render_positions[i]
	var angle:float=game.actor_render_rotations[i]
	var thrown:Array[Vector3]=[]
	for disc in weapon.owned_discs:
		if disc.active:thrown.append(Vector3(disc.position.x,disc.position.y,disc.source_z))
	var disguise:OriginalDisguise=game.disguises[i]
	var powers:int=weapon.powers.active
	var scores:Array=[]
	for index in game.actors.size():scores.append(game.round_state.scores.net_score(index))
	return {"id":i,"kind":"player","position":pos,"forward":Vector3(C.f32(-sin(angle)),C.f32(cos(angle)),0),"velocity":Vector3(actor.velocity.x,actor.velocity.y,0),
		"alive":actor.state==A.State.ALIVE,"opponent":true,"body":true,"discs":weapon.held_discs,"disarmed":actor.is_disarmed,"frozen":actor.is_frozen,
		"since_teleport":C.f32(game.clock.time_counter-weapon.teleport.last_timestamp),"aiming":actor.is_aiming,"aim":weapon.aim_power,"aim_max":1.0,
		"retrieve_charge":actor.retrieve_charge,"dashing":actor.is_dashing,"can_dash":actor.can_dash,"last_rotation":actor.last_rotation,"input_move":actor.input_move,
		"move_faster":bool(powers&2),"dash_walls":bool(powers&4),"reverse":bool(powers&8),"explosive":bool(powers&32),"multi":bool(powers&64),"teleport":bool(powers&128),"fire":bool(powers&256),
		"thrown_positions":thrown,"completed_tutorial":true,"has_parent":game.actor_environments[i].parent_kind!=0,"outside":game.actor_bounds[i].outside,"outside_timer":game.actor_bounds[i].timer,
		# PlayerAI.CanISeeTarget uses Actor.velocityStored, which is the
		# interpolation-independent movement value retained by Unity. Keep the
		# two values separate: policy visibility must not read the render speed.
		"dash_distance":C.f32(11.06),"stored_speed":A.magnitude(actor.velocity_stored),"speed":A.magnitude(actor.velocity_stored),"on_fire":game.actor_elements[i].burning,"leading":scores[i]==scores.max(),"points_left":game.round_state.scores.goal-scores[i],
		"disguised":disguise.disguised,"disguise_time":disguise.time_current,"hidden":disguise.hidden,"shadows":false,"golden_held":false,"los":true}

func refresh()->void:
	if members.is_empty():return
	var game:Node=scene.get_ref()
	for entry:Dictionary in records.values():entry.active=false
	var players:Array=[]
	var standing:=0
	for i in game.actors.size():
		var entry:=record("player/"+str(i),player_snapshot(i));players.append(entry)
		if entry.alive:standing+=1
	var powerups:Array=[]
	for book:Dictionary in game.powerup_world.books:
		if not book.collected:powerups.append(record("book/"+str(book.id),{"id":book.id,"kind":"powerup","position":Vector3(book.position.x,book.position.y,0),"unlocked":true,"reverse":book.power==8}))
	var fires:Array=[]
	for index in game.cell_grid.entries:
		var cell:Dictionary=game.cell_grid.entries[index]
		if cell.type==OriginalCellGrid.CellType.FIRE:
			var p:Vector2=game.cell_grid.world_position(index)
			fires.append(record("fire/"+str(index),{"position":Vector3(p.x,p.y,0)}))
	var explosives:Array=[]
	for explosion:Dictionary in game.explosions:
		var blast:OriginalExplosion=explosion.motion
		explosives.append(record("blast/"+str(blast.get_instance_id()),{"position":Vector3(blast.position.x,blast.position.y,0)}))
	for i:int in members:
		var ai:OriginalPlayerAI=members[i]
		ai.own=players[i]
		var discs:Array=[]
		var transit:=0
		for disc:OriginalDiscMotion in game.weapons[i].owned_discs:
			if disc.movement in [D.Movement.THROWING,D.Movement.RETURNING]:transit+=1
		for disc:OriginalDiscMotion in game.combat.discs:
			if not disc.active:continue
			var owner:int=game.actors.find(disc.owner_actor)
			discs.append(record(str(i)+"/disc/"+str(disc.get_instance_id()),{"id":disc.get_instance_id(),"kind":"disc","position":Vector3(disc.position.x,disc.position.y,disc.source_z),"velocity":Vector3(disc.velocity.x,disc.velocity.y,0),"body":true,
				"movement":["Throwing","Returning","Free","Dropped","Squished","Controlled"][disc.movement],"owner":0 if owner<0 else (1 if owner==i else 2),"owner_exists":owner>=0,"owner_self":owner==i,"opponent":owner!=i,"opponents":owner!=i,"owner_dead":owner>=0 and game.actors[owner].state==A.State.DEAD,
				"explosive":bool(disc.disc_powerups&32),"multi":bool(disc.disc_powerups&64),"burning":bool(disc.disc_powerups&256),"transit":transit,"can_steal":false,"hazard":game.actor_environments[i].over_hazard(disc.position),"in_bounds":game.boundary.is_in_bounds(disc.position)}))
		var decoys:Array=[]
		for item:Dictionary in game.decoys.members.values():
			var actor:OriginalDecoyMotion=item.actor
			decoys.append(record(str(i)+"/decoy/"+str(item.id),{"id":item.id,"kind":"decoy","position":Vector3(item.render.x,item.render.y,actor.source_z),"velocity":Vector3(actor.velocity.x,actor.velocity.y,0),"body":true,"alive":actor.state==A.State.ALIVE,"owner_exists":true,"owner_self":actor.owner_actor==game.actors[i],"opponent":actor.owner_actor!=game.actors[i]}))
		ai.facts={"players":players,"discs":discs,"powerups":powerups,"decoys":decoys,"fires":fires,"explosives":explosives,"switches":[],"moving_floors":[],"size":game.boundary.size,"time":game.clock.time_counter,"standing":standing}
	for key in records.keys():
		if not records[key].active:records.erase(key)
	peak_records=maxi(peak_records,records.size())

func phase(delta:float,which:int)->void:
	var game:Node=scene.get_ref()
	for i:int in members:
		var ai:OriginalPlayerAI=members[i]
		var actor:OriginalActorMotion=ai.actor
		var physics:=actor.position;var rotation:=actor.rotation_z;var snaps:=ai.snaps
		actor.position=Vector2(game.actor_render_positions[i].x,game.actor_render_positions[i].y);actor.rotation_z=game.actor_render_rotations[i]
		if which==0:
			ai.buttons.blocked=game.weapons[i].input_blocked
			ai.ai_update(delta,game.round_state.simulation_running())
			game.input_states[i]=ai.output_move;actor.raw_input_move=ai.output_move
		elif which==1:ai.state_update(delta)
		else:ai.deliver_path_result()
		if ai.snaps!=snaps:
			game.previous_positions[i]=actor.position;game.previous_rotations[i]=actor.rotation_z
			game.actor_render_positions[i]=Vector3(actor.position.x,actor.position.y,actor.source_z);game.actor_render_rotations[i]=actor.rotation_z
		else:actor.position=physics;actor.rotation_z=rotation
		ai.actions.events.clear();ai.events.clear()

func before_input(i:int)->void:
	if not members.has(i):return
	var game:Node=scene.get_ref()
	var b:OriginalPlayerAiButtons=members[i].buttons
	game.input_attack_states[i]=b.attack;game.input_dash_states[i]=b.dash;game.input_throw_states[i]=b.held;game.input_release_states[i]=b.released

func after_input(i:int)->void:
	if not members.has(i):return
	var game:Node=scene.get_ref()
	var b:OriginalPlayerAiButtons=members[i].buttons
	if game.actors[i].state==A.State.ALIVE:b.consume_release()
	b.blocked=game.weapons[i].input_blocked;b.held=game.weapons[i].input_held

func end_frame()->void:
	for ai:OriginalPlayerAI in members.values():ai.coroutine_phase()

func query(i:int,kind:String,args:Array)->Variant:
	var game:Node=scene.get_ref()
	var ai:OriginalPlayerAI=members[i]
	var actor:OriginalActorMotion=ai.actor
	var env:OriginalActorEnvironment=game.actor_environments[i]
	match kind:
		"sight":return not game.combat.line_blocked(actor.position,Vector2(args[0].x,args[0].y),actor.source_z,args[0].z)
		"dash_safe":return env.is_dash_safe(args[0],args[1],args[2],args[3])
		"dash_safe_from":
			var old:=actor.position;actor.position=Vector2(args[0].x,args[0].y)
			var safe:=env.is_dash_safe(Vector2(args[1].x,args[1].y),args[2],args[3],args[4]);actor.position=old;return safe
		"lunge_safe":return env.is_lunge_safe(args[0])
		"teleport_safe":return game.weapons[i].teleport.is_safe()
		"rotate":
			actor.last_rotation=args[0];actor.last_input_move=args[0]
			actor.rotation_z=C.f32(C.f32(atan2(args[0].y,args[0].x))*C.f32(57.29578)-90.0)*PI/180.0;return null
		"teleport":
			game.weapons[i].teleport.attempt(game.clock.time_counter)
			ai.own.position=Vector3(actor.position.x,actor.position.y,actor.source_z);return null
		"obstacle_ray":return not game.collision_world.circle_cast(actor.position,0.0,args[0],args[1],256).is_empty()
		"obstacle_overlap":return not game.collision_world.overlaps(actor.position,args[0],256).is_empty()
		"same_moving_floor":return false # Enclosed has no moving floors.
		"random_floor_target":return ai.target
		"hidden_by_plants":return args[0].get("hidden",false)
		"nearby_plant","teleportal_ray":return {} # No active providers in this map adapter.
		"tag_penalty":return null # Static extracted Enclosed graph lacks tag metadata.
	assert(false,"Unknown PlayerAI world query: "+kind)
	return null
