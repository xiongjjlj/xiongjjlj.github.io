class_name OriginalDisguise
extends RefCounted
## Player disguise Update/setter and Level.GetNearestDisguse. Production uses
## Godot RNG; the native fixture supplies the actual Unity random draws.
const C=preload("res://core/original_timekeeper.gd")
var inventory:WeakRef
var actor:OriginalActorMotion
var types:Array[int]=[2,4,8,16,32,1,64,128,256,512,1024,4096,8192,16384,32768,65536]
var available:Array[int]=[32,8]
var obstacles:Array[Dictionary]=[]
var disguised:=false
var timer:=0.0
var duration:=C.f32(0.2)
var time_current:=0.0
var time_total:=0.0
var selected:=-1
var obstacle_scale_y:=1.0
var scales:Dictionary={}
var offsets:Dictionary={}
var events:Array[Dictionary]=[]
var rng:=RandomNumberGenerator.new()
var random_draws:Array=[]
var replay_random:=false
var hidden:=false
var level_present:=true

func initialize(weapon:OriginalPlayerWeapons)->void:
	inventory=weakref(weapon)
	actor=weapon.actor
	actor.disguise_hook=weakref(self)
	rng.seed=0xD15

func draw_int(low:int,high:int)->int:
	if replay_random:return int(random_draws.pop_front().value)
	return rng.randi_range(low,high-1)

func draw_float(low:float,high:float)->float:
	if replay_random:return C.f32(random_draws.pop_front().value)
	return C.f32(rng.randf_range(low,high))

func nearest(position:Vector3,radius:float)->int:
	var result:=0
	for obstacle in obstacles:
		if obstacle.type==0 or not obstacle.active:continue
		var v:Vector3=obstacle.position-position
		var distance:=C.f32(sqrt(C.f32(C.f32(C.f32(v.x*v.x)+C.f32(v.y*v.y))+C.f32(v.z*v.z))))
		if distance<=radius:
			radius=distance
			result=obstacle.type
	return result

func turn_on(index:int)->void:
	selected=index
	var kind:int=types[index]
	if kind==1 or kind==4096:
		scales[index]=Vector3(draw_float(0.4,0.5),draw_float(0.75,1.0),draw_float(0.4,0.5))
		offsets[index]=Vector3(0,0,-obstacle_scale_y)
	elif kind==32:
		var amount:=draw_float(0.8,1.25)
		scales[index]=Vector3(amount,draw_float(0.8,1.25),amount)
	elif kind==8:
		var amount:=draw_float(0.95,1.2)
		scales[index]=Vector3.ONE*amount
	events.append({"type":"selected","index":index})

func set_disguised(value:bool)->void:
	if value==disguised:return
	disguised=value
	if value:
		var index:=draw_int(1,types.size())
		if level_present:
			var kind:=nearest(Vector3(actor.position.x,actor.position.y,actor.source_z),15.0)
			if kind==0 and not available.is_empty():kind=available[draw_int(0,available.size())]
			if kind!=0:
				for i in types.size():
					if types[i]==kind:index=i
		turn_on(index)
	else:timer=0.0
	events.append({"type":"changed","value":value})

func break_disguise()->void:
	set_disguised(false)
	timer=0.0
	time_current=0.0

func frame_step(delta:float,playing:bool)->void:
	delta=C.f32(delta)
	var weapon:OriginalPlayerWeapons=inventory.get_ref()
	var speed:=OriginalActorMotion.magnitude(actor.velocity)
	if weapon.powers.has_power(2048) and speed<5.5 and not actor.is_stunned and not actor.is_showing_off and not actor.is_dashing and not actor.is_lunging and not actor.is_frozen and actor.can_walk and not actor.is_spawning_in and not hidden and playing and actor.state==OriginalActorMotion.State.ALIVE:
		if not disguised and speed<0.5:
			timer=C.f32(timer+delta*actor.time_scaler)
			if timer>=duration:set_disguised(true)
	elif playing:break_disguise()
	if disguised:
		time_total=C.f32(time_total+delta)
		time_current=C.f32(time_current+delta)
