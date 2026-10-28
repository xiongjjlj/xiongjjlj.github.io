class_name OriginalAudio
extends RefCounted
## FMOD bridge. Bank logic stays in FMOD; scene supplies source gameplay events.
var api:JavaScriptObject
var button:Button
var buttons: Array[Button] = []
var menu_music := false
var music_filter := 0.0
var commands:Array=[]
var playlist:Array[String]=["skulls","trap","stepup","flutey","shuffle","jungle","surf"]
var music_index:=-1
var round_serial:=0
var rng:=OriginalUnityRandom.new(0xA0D10)
var sting_until:=-1.0
var won:=false
var poll_timer:=0.0
var warnings:Dictionary={}
var shield_states:Dictionary={}
var wet_values:Dictionary={}
var inputs:Dictionary={}
var animation_states:Dictionary={}
var environment_counts:Dictionary={}
var fire_count:=0
var fire_position:=Vector2.ZERO
var fire_intensity:=0.0

func initialize()->void:
	inputs=JSON.parse_string(FileAccess.get_file_as_string("res://assets/original/audio-inputs.json"))
	if OS.has_feature("web"):api=JavaScriptBridge.get_interface("BFAudio")

func make_button()->Button:
	button=Button.new()
	buttons.append(button)
	button.text="Original audio · loading" if api!=null else "Original audio · browser build"
	button.focus_mode=Control.FOCUS_NONE
	button.disabled=api==null
	button.pressed.connect(func():
		if api!=null:api.toggle()
		poll_timer=0.0)
	return button

func round_started(new_match:bool,actors:Array)->void:
	round_serial+=1
	if new_match:
		if music_index<0:
			for i in playlist.size():
				var j:=rng.range_int(i,playlist.size())
				var temporary:=playlist[i]
				playlist[i]=playlist[j]
				playlist[j]=temporary
		music_index+=1
		won=false
	warnings.clear()
	shield_states.clear()
	wet_values.clear()
	animation_states.clear()
	environment_counts.clear()
	fire_count=0
	for actor in actors:play("event:/combat/spawnin_whoosh",actor.position)

func play(path:String,point:=Vector2.ZERO,parameters:Dictionary={},source_z:=0.0)->void:
	if api==null or path.is_empty():return
	commands.append({"path":path,"position":[point.x,point.y,source_z],"parameters":parameters})
	if commands.size()>256:commands.pop_front()

func action(index:int,action_name:String,scene:Node)->void:
	var point:Vector2=scene.actors[index].position
	if action_name=="dash" and scene.weapons[index].teleport.events.is_empty():play("event:/combat/dash",point)
	elif action_name=="attack" and not scene.actors[index].is_frozen:
		play("event:/combat/disarm" if scene.weapons[index].action_hand=="disarm" else "event:/combat/melee",point)

func combat_event(event:Dictionary,scene:Node)->void:
	var index:int=event.get("actor",event.get("victim",0))
	var actor:OriginalActorMotion=scene.actors[index]
	var path:String={"catch":"event:/combat/catch","parry":"event:/combat/block_disc","melee_block":"event:/combat/block_melee","disarm":"event:/combat/disarm_hit"}.get(event.type,"")
	if not path.is_empty():play(path,actor.position)
	if event.type=="disc_clash":play("event:/environment/disc_rico_disc",event.position,{"velocity":event.audio_velocity})
	if event.type=="death":
		var cause:String=event.cause
		if cause=="outofbounds":play("event:/death/outofbounds",actor.position)
		elif cause=="explosion":
			play("event:/death/squished",actor.position)
			play("event:/death/exploded",actor.position)
		elif cause in ["telefrag","squished"]:play("event:/death/squished",actor.position)
		elif cause not in ["fall","fire"]:play("event:/death/sliced",actor.position)
		if actor.is_frozen:play("event:/death/shatter",actor.position)
	if (event.type=="parry" and event.get("charged",false)) or event.type=="death":sting_until=scene.clock.unscaled_time_counter+0.2

func element_event(index:int,event:Dictionary,scene:Node)->void:
	var point:Vector2=scene.actors[index].position
	var path:String={"burnt":"event:/death/burnt","frozen":"event:/powerups/ice_freeze","unfrozen":"event:/powerups/unfreeze","steam":"event:/combat/fire_extinguish","chip":"event:/combat/hop"}.get(event.type,"")
	if not path.is_empty():play(path,point)
	if event.type=="fire_changed" and event.value:play("event:/combat/fire_ignite",point)

func loop_spec(key:String,path:String,point:Vector2,parameters:Dictionary={},source_z:=0.0)->Dictionary:
	return {"key":key,"path":path,"position":[point.x,point.y,source_z],"parameters":parameters}

func frame(scene:Node,delta:float,scaled_delta:float)->void:
	if api==null:
		commands.clear()
		return
	var loops:Array=[]
	for item in scene.floor_hazards.floors.values():
		if item.model.loop_active:
			var p:Vector3=item.node.global_position
			loops.append(loop_spec("floor_%d"%item.id,"event:/environment/floor_cracking",Vector2(p.x,p.y),{},-p.z))
	if not str(inputs.ambient).is_empty():loops.append(loop_spec("ambient",inputs.ambient,Vector2.ZERO))
	if scene.cell_grid.fire_count>0:loops.append(loop_spec("fire_cells","event:/environment/firecell_loop",fire_position,{"intensity":fire_intensity,"plants":0.0}))
	for index in scene.actors.size():
		var actor:OriginalActorMotion=scene.actors[index]
		if shield_states.has(index) and bool(shield_states[index])!=actor.is_shielded:play("event:/powerups/shield_on" if actor.is_shielded else "event:/powerups/shield_off",actor.position)
		shield_states[index]=actor.is_shielded
		animation_audio(scene.characters[index],actor,index)
		var environment:OriginalActorEnvironment=scene.actor_environments[index]
		var counts:Vector2i=environment_counts.get(index,Vector2i.ZERO)
		for i in maxi(0,environment.hop_count-counts.x):play("event:/combat/hop",actor.position)
		for i in maxi(0,environment.footstep_count-counts.y):play("event:/combat/footstep",actor.position)
		environment_counts[index]=Vector2i(environment.hop_count,environment.footstep_count)
		var bounds:OriginalActorBounds=scene.actor_bounds[index]
		if bounds.warnings>int(warnings.get(index,0)):
			var duration:float=scene.boundary.out_duration
			var t:=OriginalTimekeeper.f32(1.0-OriginalTimekeeper.f32(OriginalTimekeeper.f32(bounds.timer+scaled_delta)/duration))
			var pitch:=OriginalTimekeeper.f32(1.0-OriginalTimekeeper.f32(cos(OriginalTimekeeper.f32(OriginalTimekeeper.f32(PI/2.0)*t))))
			play("event:/death/outofbounds_warning",actor.position,{"pitch":pitch})
		warnings[index]=bounds.warnings
		if actor.state==OriginalActorMotion.State.DEAD:continue
		if scene.actor_elements[index].burning:loops.append(loop_spec("fire_%d"%index,"event:/combat/fire_loop",actor.position))
		if actor.on_wet_floor:
			if not actor.is_dashing or actor.dash_timer>actor.dash_duration*0.8:
				var up:=OriginalDiscMotion.euler_up_z(actor.rotation_z*57.29578)
				var value:float=(OriginalActorMotion.normalized(actor.velocity).dot(up)*0.5+0.5)*sin(clampf(actor.velocity_stored.length()/actor.speed_limit,0.0,1.0)*PI/2.0)
				wet_values[index]=value
			loops.append(loop_spec("wet_%d"%index,"event:/environment/wet_floor",actor.position,{"velocity":wet_values.get(index,0.0)}))
	for id in scene.decoy_views:
		if scene.decoys.members.has(id):animation_audio(scene.decoy_views[id].view,scene.decoys.members[id].actor,0)
	for item in scene.flying_discs:
		var disc:OriginalDiscMotion=item.motion
		if disc.movement==OriginalDiscMotion.Movement.SQUISHED or disc.owner_actor==null or disc.owner_actor.retrieve_charge<=0.0:continue
		var owner:int=scene.actors.find(disc.owner_actor)
		var pitch:float=owner*1.5+scene.weapons[owner].owned_discs.size()*0.25+(6.0 if disc.is_mini_disc else 0.0)
		loops.append(loop_spec("retrieve_%d"%disc.get_instance_id(),"event:/combat/retrieve",disc.position,{"pitch":pitch,"retrieveCharge":disc.owner_actor.retrieve_charge},disc.source_z))
		if (disc.disc_powerups&8192)!=0 and disc.can_be_controlled:
			loops.append(loop_spec("control_%d"%disc.get_instance_id(),"event:/combat/telekinesis",disc.position,{"pitch":pitch,"velocity":maxf(0.0,disc.velocity.length()/disc.speed_limit)},disc.source_z))
	var round_end:int=1 if scene.round_state.phase in [OriginalFfaRound.Phase.SCORES,OriginalFfaRound.Phase.MATCH_END] else 0
	if round_end and scene.round_state.scores.winners.size()==1 and not won:
		won=true
		play("event:/ui/circlewipe_victory")
		play("event:/music/sting_victory")
	var camera:Camera3D=scene.camera
	var forward:=Vector3(-camera.basis.z.x,-camera.basis.z.y,camera.basis.z.z)
	var up:=Vector3(camera.basis.y.x,camera.basis.y.y,-camera.basis.y.z)
	var listener:=Vector3(camera.position.x,camera.position.y,-camera.position.z)+forward*50.0
	api.frame(JSON.stringify({"commands":commands,"loops":loops,"time":scene.clock.time_counter,"unscaled":scene.clock.unscaled_time_counter,"scale":scene.clock.audio_scale,"sting":scene.clock.unscaled_time_counter<=sting_until,"music":"event:/music/menu" if menu_music else "event:/music/gameplay_"+playlist[maxi(0,music_index)%playlist.size()],"music_filter":music_filter,"section":0,"round_end":round_end,"stop_music":won and not menu_music,"round_serial":round_serial,"listener":{"position":[listener.x,listener.y,listener.z],"forward":[forward.x,forward.y,forward.z],"up":[up.x,up.y,up.z]}}))
	commands.clear()
	poll_timer-=delta
	if poll_timer<=0.0:
		poll_timer=0.5
		var status:Dictionary=JSON.parse_string(api.status())
		buttons = buttons.filter(func(b):return is_instance_valid(b))
		for item in buttons:
			item.disabled=status.phase!="ready"
			item.text=str(item.get_meta("audio_label","Original audio"))+" · "+("ON" if status.enabled else "click to enable") if status.phase=="ready" else str(item.get_meta("audio_label","Original audio"))+" · "+str(status.phase)

func animation_audio(view:OriginalCharacterPreview,actor:OriginalActorMotion,player_id:int)->void:
	var key:=view.get_instance_id()
	var clip_name:String=view.clip.get("source_clip","")
	var previous:Dictionary=animation_states.get(key,{"clip":"","time":0.0})
	var start:float=previous.time if previous.clip==clip_name and previous.time<=view.clip_time else 0.0
	animation_states[key]={"clip":clip_name,"time":view.clip_time}
	if actor.is_dashing or actor.on_wet_floor:return
	for event in inputs.clip_events.get(clip_name,[]):
		if event.functionName!="Footstep":continue
		var count:=0
		if view.clip.loop and view.clip.duration>0.0:
			count=int(floor((view.clip_time-event.time)/view.clip.duration)-floor((start-event.time)/view.clip.duration))
		elif start<event.time and view.clip_time>=event.time:count=1
		for i in mini(16,maxi(0,count)):play("event:/combat/footstep",actor.position,{"pitch":player_id*2.0})

func cell_frame(grid:OriginalCellGrid)->void:
	if grid.fire_count>0 and fire_count==0:
		for event in grid.events:
			if event.type=="fire_emission":
				fire_position=event.position
				play("event:/environment/firecell_start",fire_position)
				break
	fire_count=grid.fire_count
	fire_intensity=OriginalTimekeeper.f32(float(grid.fire_count)/150.0)

func wall(event:Dictionary,source_z:=0.0)->void:
	var material:int=inputs.obstacle_materials.get(str(event.object_id),0)
	play("event:/environment/disc_rico_"+["stone","wood","bamboo","tree","shrub"][material],event.position,{"velocity":event.audio_velocity},source_z)

func power_event(index:int,event:Dictionary,scene:Node)->void:
	if event.type=="shield_started":
		play("event:/powerups/shield_on",scene.actors[index].position)
		shield_states[index]=true
	elif event.type=="shield_broken":
		play("event:/powerups/shield_off",scene.actors[index].position)
		shield_states[index]=false
