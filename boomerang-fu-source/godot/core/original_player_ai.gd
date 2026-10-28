class_name OriginalPlayerAI
extends OriginalDecoyAI
## PlayerAI's FFA state/action loop. World queries and path callback delivery
## are explicit. The shared navigation helpers retain Unity's Y-up coordinates.
const Policy=preload("res://core/original_player_ai_policy.gd")
const Targeter=preload("res://core/original_player_ai_targeting.gd")
const ActionCore=preload("res://core/original_player_ai_actions.gd")
const ButtonCore=preload("res://core/original_player_ai_buttons.gd")
var policy:=Policy.new()
var targeting:=Targeter.new()
var actions:=ActionCore.new()
var buttons:=ButtonCore.new()
var own:Dictionary={}
var facts:Dictionary={}
var world_query:Callable
var flee_provider:Callable
var target_object:Dictionary={}
var victim:Dictionary={}
var threat:Dictionary={}
var desire:Dictionary={}
var best_desire:=0.0
var best_victim:=0.0
var best_threat:=0.0
var total_desire:=0.0
var total_victim:=0.0
var total_threat:=0.0
var ambient_interest:=0.0
var output_move:=Vector2.ZERO
var standing_timer:=0.0
var closest_retrieve:=3.4028234663852886e38
var retrieve_checked:=0.0
var modifier_remaining:=1.0
var divert_after:=-1.0
var state_changes:=0
var action_count:=0
var scans:=0
var callback_depth:=0

func dispose()->void:
	buttons.clear();actions.command_handler=Callable();actions.dash_safety=Callable()
	actions.lunge_safety=Callable();actions.teleport_safety=Callable();targeting.line_of_sight=Callable()
	world_query=Callable();flee_provider=Callable();path_provider=Callable()
	point_hazard=Callable();touching_hazard=Callable();avoidance_query=Callable();fire_query=Callable()
	own={};facts={};target_object={};victim={};threat={};desire={};actor=null

func query(kind:String,args:Array=[],fallback:Variant=null)->Variant:
	return world_query.call(kind,args) if world_query.is_valid() else fallback
func sight(point:Vector3)->bool:return bool(query("sight",[point],true))
func chance(percent:float)->bool:return rng.range_int(0,100)<=percent
func valid_target(item:Dictionary)->bool:return not item.is_empty() and item.get("active",true)
func distance_to(point:Vector3)->float:return N.magnitude(point-position3())
func target_pathable()->bool:return has_path and not path.is_empty() and N.magnitude(waypoint(-1)-target)<=2.0

func initialize()->void:
	buttons.cancel_all();has_path=false;reached_end=true;move_dir=Vector2.ZERO
	target_object={};victim={};threat={};desire={}
	policy.rng=rng;targeting.policy=policy;targeting.line_of_sight=sight
	actions.targeting=targeting;actions.command_handler=_command
	actions.dash_safety=func(d:Vector2,a:bool,o:bool,h:bool)->bool:return bool(query("dash_safe",[d,a,o,h],false))
	actions.lunge_safety=func(d:Vector2)->bool:return bool(query("lunge_safe",[d],false))
	actions.teleport_safety=func()->bool:return bool(query("teleport_safe",[],false))
	create_path();policy.set_difficulty(policy.difficulty);buttons.input_delay=policy.input_delay
	modifier_remaining=1.0;divert_after=-1.0;policy.aggression=0.0
	change_state(AIState.WANDERING);actions.unsafe_attempts=0

func _command(kind:String,vector:Vector2)->void:
	match kind:
		"button_attack":buttons.push_button(ButtonCore.ButtonType.ATTACK);action_count+=1
		"button_dash":buttons.push_button(ButtonCore.ButtonType.DASH);action_count+=1
		"button_throwdisc":buttons.push_button(ButtonCore.ButtonType.THROW);action_count+=1
		"stop_hold":buttons.stop_holding_throw()
		"rotate":snaps+=1;query("rotate",[vector])
		"teleport":query("teleport");snaps+=1
		"create_path":create_path()
		"find_safe_dash":find_safe_dash()
		"divert_after_throw":divert_after=C.f32(0.1)

func _begin_action()->void:
	actions.move_dir=move_dir;actions.has_path=has_path
func _end_action()->void:
	move_dir=actions.move_dir;has_path=actions.has_path
func attack_target()->void:
	_begin_action();actions.attack(own,victim);_end_action()
func throw_target(item:Dictionary)->void:
	_begin_action();actions.throw_at(own,item);_end_action()
func block_target(item:Dictionary)->void:
	_begin_action();actions.block(own,item);_end_action()
func dash(direction:Vector2,avoid_obstacles:=true,allow_outside:=true,allow_hazard_end:=true,track_failures:=true)->bool:
	_begin_action()
	var accepted:=actions.dash(own,direction,avoid_obstacles,allow_outside,allow_hazard_end,track_failures)
	_end_action();return accepted

func random_reaction()->float:return policy.random_reaction()

func scan_points()->void:
	scans+=1;best_desire=0.0;best_victim=0.0;best_threat=0.0
	total_desire=0.0;total_victim=0.0;total_threat=0.0
	for item:Dictionary in facts.explosives:_scan_threat(item,1)
	var stride:=int(facts.fires.size()/5)
	if stride>0:
		for i in facts.fires.size():
			if i%stride==0:_scan_threat(facts.fires[i],2)
	for item:Dictionary in facts.players:
		if item.id==own.id or not item.alive or not item.opponent:continue
		own.los=sight(item.position)
		var interest:=policy.victim_interest(own,item,facts.size)
		if interest>best_victim:best_victim=interest;victim=item
		total_victim=C.f32(total_victim+interest);_scan_threat(item,0)
	for item:Dictionary in facts.discs:
		if item.movement=="Dropped":_scan_desire(item,1)
		elif item.explosive or item.multi:_scan_threat(item,1)
	for item:Dictionary in facts.powerups:_scan_desire(item,0)

func _scan_threat(item:Dictionary,kind:int)->void:
	own.los=sight(item.position)
	var interest:=policy.threat_interest(own,item,facts.size,kind)
	if interest>best_threat:best_threat=interest;threat=item
	total_threat=C.f32(total_threat+interest)
func _scan_desire(item:Dictionary,kind:int)->void:
	var interest:=policy.desire_interest(own,item,facts.size,kind)
	if interest>best_desire:best_desire=interest;desire=item
	total_desire=C.f32(total_desire+interest)

func consider_state()->void:
	if policy.bearings_timer>C.f32(0.1) and policy.difficulty!=Policy.Difficulty.IMPOSSIBLE:
		change_state(AIState.WANDERING)
	elif best_desire>best_victim and best_desire>best_threat and best_desire>ambient_interest and best_desire>reluctance and int(facts.standing)>1 and valid_target(desire) and Policy.distance2(desire.position,position3())>=next_waypoint_distance:
		target_object=desire
		if last_state!=AIState.RETRIEVING and not own.explosive and not own.outside and own.disarmed and not own.thrown_positions.is_empty() and desire.get("kind","")=="disc" and sight(desire.position) and ((distance_to(desire.position)>dash_distance*2.0 and (chance(10.0 if policy.difficulty==Policy.Difficulty.EASY else 33.0) or not boundary.is_in_bounds(Vector2(desire.position.x,desire.position.y)))) or not actions.safe_dash(own,Vector2(desire.position.x-own.position.x,desire.position.y-own.position.y))) and best_threat<best_desire*0.5:
			change_state(AIState.RETRIEVING)
		else:change_state(AIState.COLLECTING)
	elif best_victim>best_desire and best_victim>best_threat and best_victim>ambient_interest and best_victim>reluctance and not victim.is_empty():
		target_object=victim
		if bool(query("hidden_by_plants",[victim],false)):target_object=query("nearby_plant",[victim.position],{})
		change_state(AIState.ATTACKING)
	elif best_threat>best_desire and best_threat>best_victim and best_threat>ambient_interest and best_threat>reluctance:
		if state!=AIState.FLEEING or chance(33.0):target_object=threat
		change_state(AIState.FLEEING)
	elif ambient_interest>reluctance:change_state(AIState.WANDERING)

func locate_target()->void:
	if valid_target(target_object):target=target_object.position
	else:
		reluctance=reluctance_default;scan_points();consider_state()

func change_state(next:int)->void:
	if state==next:return
	if state==AIState.ATTACKING and buttons.held and (own.aiming or own.retrieve_charge>0.0):buttons.stop_holding_throw()
	last_state=state;state=next;state_changes+=1
	match state:
		AIState.WANDERING:
			target_object={};set_random_target();create_path();reluctance=C.f32(1.25);reluctance_cooldown=C.f32(1.5)
		AIState.DIVERTING:create_path();reluctance=2.0;reluctance_cooldown=1.0
		AIState.ATTACKING:
			if own.retrieve_charge>0.0:buttons.stop_holding_throw()
			locate_target();create_path();query("tag_penalty",[2,100]);reluctance=1.5;reluctance_cooldown=2.5
		AIState.COLLECTING:
			if valid_target(target_object):locate_target();create_path()
			else:scan_points();consider_state()
		AIState.FLEEING:
			buttons.stop_holding_throw();has_path=false;locate_target();create_path();reluctance=1.5;reluctance_cooldown=1.0
		AIState.RETRIEVING:
			if not chance([60.0,80.0,100.0,100.0][policy.difficulty]) or own.outside:target_object={};locate_target();return
			if own.teleport and actions.safe_teleport(own):actions.teleport();return
			if own.explosive:target_object={};locate_target();return
			set_random_target();create_path();buttons.replace_hold(ButtonCore.ButtonType.THROW,5.0)
			closest_retrieve=3.4028234663852886e38;retrieve_checked=facts.time
		AIState.FLICKING_SWITCH:
			if valid_target(target_object):
				target=target_object.position-target_object.forward*rng.range_float(1.25,2.5);create_path()
			else:scan_points();consider_state()
			try_hit_switch()
		AIState.IDLE:
			move_dir=Vector2.ZERO;reached_end=true;target_object={};standing_timer=1.0;reluctance=1.5;reluctance_cooldown=1.0
		AIState.DEAD:move_dir=Vector2.ZERO;output_move=Vector2.ZERO;reached_end=true;target_object={}
	events.append({"type":"state","state":state,"last":last_state})

func create_path()->void:
	if not playing or create_timer<C.f32(0.01666):return
	create_timer=0.0
	if distance_to(target)<next_waypoint_distance:
		if state==AIState.FLICKING_SWITCH and valid_target(target_object) and target_object.get("active_switch",false):
			actions.rotate(own,Policy.direction(position3(),target_object.position));attack_target()
		elif state==AIState.COLLECTING:change_state(AIState.DIVERTING)
		reached_end=true;return
	var result:Dictionary={}
	if state==AIState.FLEEING and flee_provider.is_valid():result=flee_provider.call(position3(),target)
	elif state in [AIState.WANDERING,AIState.COLLECTING,AIState.ATTACKING,AIState.DIVERTING,AIState.FLICKING_SWITCH] and path_provider.is_valid():result=path_provider.call(position3(),target)
	# Original CreatePath deliberately has no Retrieving branch.
	if not result.is_empty():pending_result=result;recalculating=true;reached_end=false

func deliver_path_result()->void:
	if not recalculating:return
	var failed:bool=pending_result.get("error","")!=""
	super.deliver_path_result()
	if failed or not playing or target_pathable():return
	if valid_target(target_object) and state==AIState.COLLECTING and own.disarmed and not own.thrown_positions.is_empty() and (threat.is_empty() or distance_to(threat.position)>5.0) and (sight(target_object.position) or chance(50.0)):
		change_state(AIState.RETRIEVING);return
	if dash_across_hazards():return
	if own.discs>0 and state!=AIState.RETRIEVING and not own.get("golden_held",false):
		var best:=targeting.best_throw(own,facts.players,facts.decoys)
		if state==AIState.ATTACKING and not best.is_empty() and action_timer>action_delay*0.25:throw_target(best);return
		if not victim.is_empty() and state in [AIState.ATTACKING,AIState.DIVERTING] and not buttons.held and buttons.input_timer>=buttons.input_delay and policy.can_see_target(victim) and victim.alive and distance_to(victim.position)<=34.0*C.f32(1.33) and distance_to(victim.position)>17.0 and not bool(query("obstacle_ray",[Policy.direction(position3(),victim.position),25.5],false)) and aim_disc(rng.range_float(1.0,1.5)):return
	if (has_path and not path.is_empty() and bool(query("dash_safe_from",[waypoint(-1),target-normalized3(waypoint(-1)),false,true,true],false))) or not own.completed_tutorial:return
	if own.has_parent:
		if state!=AIState.DIVERTING and chance(20.0):target=query("random_floor_target",[],target);change_state(AIState.DIVERTING);return
		if chance(66.0):change_state(AIState.IDLE);return
	change_state(AIState.WANDERING)

func ai_update(frame_delta:float,is_playing:bool)->void:
	frame_number+=1;buttons.begin_frame(frame_delta);playing=is_playing
	if not playing:return
	if actor.state==A.State.DEAD:has_path=false;change_state(AIState.DEAD);return
	if state==AIState.DEAD:return
	var dt:=C.f32(frame_delta)
	dash_distance=C.f32(own.dash_distance*(C.f32(1.037037) if own.move_faster else 1.0))
	diverting_distance=3.0 if own.discs>0 else 5.0
	if buttons.held and own.retrieve_charge>C.f32(0.2) and retrieving_stuck():buttons.stop_holding_throw();scan_points();consider_state()
	move_input_speed=40.0 if own.move_faster else 20.0
	if boundary.is_in_bounds(actor.position):ambient_interest=C.f32(C.f32(0.7)*policy.ambient)
	else:ambient_interest=C.f32(2.75*policy.ambient);policy.nervous=0.5
	if own.aiming and valid_target(victim) and policy.can_see_target(victim):
		move_dir=lerp2(output_move,Policy.direction(position3(),victim.position),C.f32(move_input_speed*dt))
		if policy.difficulty>Policy.Difficulty.EASY and not threat.is_empty() and distance_to(threat.position)<5.0*C.f32(1.1) and sight(threat.position) and threat.get("kind","")=="player" and policy.can_see_target(threat) and action_timer>action_delay*0.5:buttons.stop_holding_throw()
	if policy.bearings_timer>0.0:
		policy.bearings_timer=C.f32(policy.bearings_timer-dt)
		if policy.bearings_timer>policy.bearings_duration*C.f32(0.85):move_dir*=policy.bearings_speed
	if scan_timer>scan_delay and state!=AIState.ATTACKING:
		scan_points();consider_state();scan_timer=0.0;scan_delay=random_reaction()
		if policy.difficulty>=Policy.Difficulty.HARD:scan_delay=C.f32(scan_delay*3.0)
	scan_timer=C.f32(scan_timer+dt);action_timer=C.f32(action_timer+dt);create_timer=C.f32(create_timer+dt);buttons.advance_input_timer()
	if reluctance_cooldown>0.0:reluctance_cooldown=C.f32(reluctance_cooldown-dt)
	else:reluctance=reluctance_default
	if actor.is_dashing:create_after_dash=true
	elif create_after_dash:create_path();create_after_dash=false
	if action_timer>action_delay:action_timer=0.0;action_delay=random_reaction();take_action()
	output_move=move_dir
	var probability:float=[60.0,70.0,80.0,90.0][policy.difficulty]
	if policy.bearings_timer>0.0:probability*=0.5
	if (own.discs<=0 and policy.difficulty!=Policy.Difficulty.IMPOSSIBLE) or not chance(probability) or not action_timer>action_delay*0.5:return
	for item:Dictionary in facts.players:
		if item.id!=own.id and distance_to(item.position)<actor.radius*C.f32(2.2):
			if not targeting.best_attack(own,facts.players,facts.decoys,facts.switches,state).is_empty():attack_target();action_timer=0.0
			break

func state_update(dt:float)->void:
	if not playing:return
	match state:
		AIState.WANDERING:
			if reached_end:scan_points();consider_state();set_random_target();create_path()
			walk_path(dt,dash_across_hazards)
		AIState.DIVERTING:
			walk_path(dt)
			if reached_end:change_state(last_state)
		AIState.ATTACKING:
			if reached_end or scan_timer>scan_delay:
				scan_timer=0.0
				if valid_target(victim) and victim.alive:
					scan_points();consider_state()
					if policy.difficulty!=Policy.Difficulty.IMPOSSIBLE and bool(query("hidden_by_plants",[victim],false)) and chance(95.0):
						if (valid_target(target_object) and target_object.get("kind","")!="plant") or reached_end:target_object=query("nearby_plant",[victim.position],{});locate_target();create_path()
						if chance(70.0) and own.hidden and actions.safe_lunge(own,move_dir) and own.discs>0:attack_target()
					else:target_object=victim;locate_target();create_path()
				else:reached_end=true;victim={};target_object={};change_state(AIState.WANDERING);scan_points();consider_state()
			walk_path(dt,try_attack)
		AIState.COLLECTING:
			if reached_end:scan_points();consider_state();locate_target();create_path()
			else:walk_path(dt,dash_towards_disc)
		AIState.FLEEING:
			if reached_end:scan_points();consider_state();locate_target();create_path()
			walk_path(dt,dash_across_hazards)
		AIState.RETRIEVING:
			walk_path(dt)
			if reached_end:
				set_random_target();create_path()
				if not buttons.held and not own.thrown_positions.is_empty():buttons.replace_hold(ButtonCore.ButtonType.THROW,rng.range_float(3.0,4.0));return
			if own.thrown_positions.is_empty():scan_points();consider_state()
		AIState.FLICKING_SWITCH:
			if reached_end:
				if not dash_across_hazards():scan_points();consider_state()
				if valid_target(target_object):target=target_object.position-target_object.forward*0.5
				else:locate_target()
				create_path()
			else:walk_path(dt,try_hit_switch)
		AIState.IDLE:
			if not reached_end:walk_path(dt,dash_across_hazards)
			elif standing_timer<=0.0:
				if standing_timer> -1.0:standing_timer=C.f32(standing_timer-dt)
				elif not actor.is_dashing:standing_timer=0.0
				move_dir=Vector2.ZERO;scan_points();consider_state()
			else:
				if Policy.dot2(Policy.direction(position3(),target),own.forward)<C.f32(0.9):move_dir=Policy.direction(position3(),target)*C.f32(0.1)
				else:move_dir=lerp2(actor.input_move,Vector2.ZERO,C.f32(move_input_speed*3.0*dt))
				standing_timer=C.f32(standing_timer-dt)

func walk_path(dt:float,on_end:Callable=Callable())->void:
	if actor.is_aiming:return
	move_dir=lerp2(actor.input_move,Vector2.ZERO,C.f32(move_input_speed*(1.0 if state==AIState.FLEEING else 2.0)*dt))
	if not has_path:
		if not recalculating:reached_end=true
		return
	while current_waypoint<path.size() and distance_to(waypoint(current_waypoint))<next_waypoint_distance*(2.0 if actor.on_wet_floor else 1.0):
		current_waypoint+=1
		if current_waypoint==path.size()-1 and is_point_hazard(waypoint(current_waypoint)):
			if chance(25.0) and own.has_parent:change_state(AIState.IDLE)
			else:current_waypoint=path.size()
	if current_waypoint>=path.size():
		has_path=false;reached_end=true;move_dir=Vector2.ZERO
		if on_end.is_valid():on_end.call()
		elif not actor.is_dashing and touches_hazard(position3()+own.forward) and distance_to(target)>5.0:dash(forward(),true,true,false)
	else:
		var direction:=Policy.direction(position3(),waypoint(current_waypoint))
		move_dir=-lerp2(-output_move,direction,C.f32(move_input_speed*dt)) if own.reverse else lerp2(output_move,direction,C.f32(move_input_speed*dt))
		if not actor.is_dashing and avoidance_query.is_valid():
			var avoid:Variant=avoidance_query.call(move_dir,diverting_distance)
			if avoid is Vector3:divert_path(avoid)
		if not actor.is_dashing and not actor.is_invulnerable and fire_query.is_valid():
			var fire_point:Vector3=position3()+own.forward*C.f32(3.0*C.f32(1.33))
			if fire_query.call(fire_point):divert_path(fire_point)

func divert_path(point:Vector3)->void:
	if last_state==AIState.DIVERTING:consider_state();return
	var v:=point-position3();var cross:float=own.forward.x*v.y-own.forward.y*v.x
	var angle:float=rng.range_float(40.0,70.0)
	var rotated:=ActionCore.rotate_direction(Vector2(v.x,v.y),-angle if cross<0.0 else angle)
	target=position3()+Vector3(rotated.x,rotated.y,v.z)
	target+=normalized3(target-position3())*rng.range_float(3.0,6.0);change_state(AIState.DIVERTING)

func retrieving_stuck()->bool:
	if own.thrown_positions.is_empty() or C.f32(facts.time-retrieve_checked)<C.f32(0.4):return false
	retrieve_checked=facts.time;var closest:=3.4028234663852886e38
	for point:Vector3 in own.thrown_positions:closest=minf(closest,distance_to(point))
	var stuck:=closest_retrieve-closest<3.0;closest_retrieve=closest;return stuck

func aim_disc(duration:=1.0)->bool:
	if own.outside or bool(query("obstacle_overlap",[3.0],false)):return false
	if own.explosive:duration=C.f32(duration*C.f32(0.4))
	buttons.replace_hold(ButtonCore.ButtonType.THROW,duration);return true

func take_action()->void:
	if policy.difficulty==Policy.Difficulty.EASY and chance(25.0):return
	if own.frozen and policy.difficulty>Policy.Difficulty.EASY and chance(66.0):buttons.push_button(ButtonCore.ButtonType.ATTACK)
	if state==AIState.FLICKING_SWITCH and try_hit_switch():return
	var candidate:=targeting.best_block(own,facts.players,facts.discs)
	if not candidate.is_empty() and chance([20.0,50.0,75.0,90.0][policy.difficulty]):block_target(candidate);return
	candidate=targeting.best_attack(own,facts.players,facts.decoys,facts.switches,state)
	var probability:=90.0
	if own.discs==0 and policy.difficulty==Policy.Difficulty.MEDIUM:probability=45.0
	elif own.discs==0 and policy.difficulty==Policy.Difficulty.EASY:probability=0.0
	if not candidate.is_empty() and policy.bearings_timer<=0.0 and (state==AIState.ATTACKING or chance(probability)):attack_target();return
	candidate=targeting.best_throw(own,facts.players,facts.decoys)
	if not candidate.is_empty() and policy.bearings_timer<=0.0 and (state==AIState.ATTACKING or chance(75.0)):
		if own.discs>1:action_delay=C.f32(action_delay*2.0)
		throw_target(candidate);return
	if (own.discs>0 and policy.bearings_timer<=0.0 and not buttons.held and chance(4.0*(1+policy.difficulty)) and state==AIState.ATTACKING and not victim.is_empty() and distance_to(victim.position)>17.0 and policy.can_see_target(victim) and aim_disc()) or (policy.difficulty==Policy.Difficulty.IMPOSSIBLE and chance(30.0)):return
	var portal:Dictionary=query("teleportal_ray",[forward(),dash_distance*0.5],{})
	if not actor.is_dashing and not portal.is_empty() and chance(50.0) and portal.safe and sight(portal.point) and dash(actor.last_rotation,false):return
	if not actor.is_dashing and chance(60.0) and sight(target) and not target_pathable() and touches_hazard(position3()+normalized3(target-position3())*C.f32(dash_distance*0.5)) and dash(Policy.direction(position3(),target),true,false,false):return
	if own.outside and has_path and not path.is_empty() and path_length*(1.0-clampf(float(current_waypoint)/path.size(),0.0,1.0))>own.stored_speed*actor.fixed_delta*own.outside_timer and state in [AIState.WANDERING,AIState.ATTACKING] and (policy.difficulty<Policy.Difficulty.HARD or chance(25.0)):
		var half:Vector2=-boundary.size*0.5
		var point:=ActionCore.rotate_direction(Vector2(rng.range_float(-half.x,half.x),rng.range_float(-half.y,half.y)),boundary.angle)+boundary.center
		if not actor.is_dashing and dash(A.normalized(point-actor.position)):return
		for floor_point:Vector3 in facts.moving_floors:
			if not actor.is_dashing and dash(A.normalized(Vector2(floor_point.x,floor_point.y)-actor.position)):return
	if not actor.is_dashing and not own.has_parent and chance(33.0 if boundary.is_in_bounds(actor.position) else 50.0):
		var direction:=Policy.direction(position3(),target)
		var up:=Policy.direction(Vector3.ZERO,own.forward)
		var dot:=C.f32(direction.x*up.x+direction.y*up.y)
		var offset:=Vector2(target.x-actor.position.x,target.y-actor.position.y)
		var distance:=Policy.magnitude2(offset)
		if own.dash_walls and not sight(target):
			if distance>dash_distance*0.75 and dot>C.f32(0.9) and dash(lerp2(A.normalized(offset),up,rng.range_float(0.0,1.0)),false):return
			if chance(25.0) and dash(A.normalized(offset),false):return
		if distance>dash_distance and (dot>C.f32(0.99) or (state==AIState.FLEEING and dot>0.75)) and (policy.difficulty<Policy.Difficulty.HARD or chance(25.0)) and dash(up):return
	if own.discs==0 and own.teleport and not own.explosive and (chance(30.0) or own.disarmed) and actions.safe_teleport(own):actions.teleport()

func dash_across_hazards()->bool:
	if actor.is_dashing or not touches_hazard(position3()+Vector3(A.normalized(forward()).x,A.normalized(forward()).y,0)) or chance(90.0) or Policy.distance2(target,position3())<=5.0:return false
	return dash(A.normalized(forward()),false,false,false)

func find_safe_dash(_starting_angle:float=0.0)->void:
	if actor.is_dashing:return
	actions.unsafe_attempts=0
	var angle:float=rng.range_float(0.0,360.0)
	for i in 16:
		var direction:=vector_from_angle(C.f32(22.5*i+angle))*C.f32(9.45)
		var endpoint:=position3()+Vector3(direction.x,direction.y,0)
		if is_point_hazard(endpoint):continue
		if own.has_parent and bool(query("same_moving_floor",[endpoint],false)):continue
		if dash(direction,false,true,true,false):scan_points();consider_state();break

func try_attack()->bool:
	locate_target()
	if valid_target(victim) and victim.alive and distance_to(victim.position)<=5.0 and Policy.dot2(Policy.direction(position3(),victim.position),own.forward)>0.25:
		attack_target();consider_state();return true
	dash_across_hazards();consider_state();return false

func try_hit_switch()->bool:
	if valid_target(target_object) and target_object.get("active_switch",true) and distance_to(target_object.position)<=5.0 and (reached_end or (not victim.is_empty() and Policy.dot2(Policy.direction(position3(),victim.position),own.forward)>=0.0)):
		attack_target();return true
	if reached_end and not dash_across_hazards():change_state(AIState.WANDERING)
	return false

func dash_towards_disc()->bool:
	if valid_target(target_object) and target_object.get("kind","")=="disc" and own.discs<1:
		if not dash(Policy.direction(position3(),target_object.position),true,false,false):
			if chance(75.0) and sight(target_object.position) and own.disarmed and not own.thrown_positions.is_empty() and (threat.is_empty() or distance_to(threat.position)>5.0):change_state(AIState.RETRIEVING)
			elif last_state!=AIState.DIVERTING:
				target+=Vector3(rng.range_float(-5.0,5.0),rng.range_float(-5.0,5.0),0);change_state(AIState.DIVERTING)
			else:consider_state()
	elif not dash_across_hazards():change_state(AIState.WANDERING)
	return false

func coroutine_phase()->void:
	buttons.coroutine_phase()
	modifier_remaining=C.f32(modifier_remaining-buttons.delta)
	if modifier_remaining<=0.0:policy.randomize_modifiers(own.reverse,state==AIState.RETRIEVING);modifier_remaining=1.0
	if divert_after>=0.0:
		divert_after=C.f32(divert_after-buttons.delta)
		if divert_after<=0.0:
			divert_after=-1.0
			if playing:divert_path(own.forward)
