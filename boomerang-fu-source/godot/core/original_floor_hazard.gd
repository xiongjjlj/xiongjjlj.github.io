class_name OriginalFloorHazard
extends RefCounted
## TrapFloor/FallingFloor state and source-space visual position. Scene adapters
## execute the emitted service calls; renderer motion never moves the collider.
const C=preload("res://core/original_timekeeper.gd")
enum State { IDLE, WARNING, FALLING, RESPAWNING, RISING }
var trap:=true
var tile_size:=0
var state:=State.IDLE
var layer:=16
var start_position:=Vector3.ZERO
var target_position:=Vector3.ZERO
var position:=Vector3.ZERO
var start_color:=Color(1,0.1,0.2,0)
var flash:=Color(1,0.1,0.2,0)
var warning_timer:=0.0
var shake_timer:=0.0
var fall_timer:=0.0
var respawn_timer:=0.0
var rising_timer:=0.0
var rising_solid:=false
var visible:=true
var loop_active:=false
var events:Array[String]=[]
var rng:=OriginalUnityRandom.new()

func initialize(is_trap:bool,origin:Vector3,color:Color,shared_rng:OriginalUnityRandom,size:=0)->void:
	trap=is_trap;start_position=origin;start_color=color;rng=shared_rng;tile_size=size
	reset()

func reset()->void:
	state=State.IDLE;layer=16;position=start_position;target_position=start_position;flash=start_color
	warning_timer=0.0;shake_timer=0.0;fall_timer=0.0;respawn_timer=0.0;rising_timer=0.0
	rising_solid=false;visible=true;loop_active=false;events.clear()

func start_falling(playing:=true)->bool:
	if trap and (state!=State.IDLE or not playing):return false
	state=State.WARNING;loop_active=true
	events.append("event:/environment/floor_cracking");events.append("tag")
	return true

func stop_loop()->void:
	if loop_active:events.append("stop_loop");loop_active=false

func disable()->void:
	stop_loop()

func color_lerp(a:Color,b:Color,t:float)->Color:
	return Color(C.lerp32(a.r,b.r,t),C.lerp32(a.g,b.g,t),C.lerp32(a.b,b.b,t),C.lerp32(a.a,b.a,t))

func frame_step(delta:float,collapsing:=true)->void:
	delta=C.f32(delta)
	var end_color:=Color(start_color.r,start_color.g,start_color.b,0.5)
	if state==State.WARNING:
		warning_timer=C.f32(warning_timer+delta)
		if not trap and not collapsing:stop_loop();return
		var warning_duration:=2.5 if trap else 2.0
		if shake_timer<C.f32(0.01):shake_timer=C.f32(shake_timer+delta)
		else:
			shake_timer=0.0
			var amount:=C.f32(0.25+0.75*(warning_timer/warning_duration)) if trap else 1.0
			var radius:=0.25 if trap else C.f32(0.2)
			target_position=start_position+Vector3(rng.range_float(-radius,radius),rng.range_float(-radius,radius),0)*amount
		var lerp_t:=C.f32(delta*100.0)
		position=Vector3(C.lerp32(position.x,target_position.x,lerp_t),C.lerp32(position.y,target_position.y,lerp_t),C.lerp32(position.z,target_position.z,lerp_t))
		var t:=C.f32(warning_timer/warning_duration)
		flash=color_lerp(start_color,end_color,C.f32(t*t*t) if trap else t)
		if warning_timer<warning_duration:return
		if trap:warning_timer=0.0
		state=State.FALLING;layer=0
		events.append("rescan");events.append("event:/environment/floor_sink");stop_loop()
		events.append("powerups")
		if trap:events.append("debris")
		events.append("extinguish");events.append("remove_decals")
	elif state==State.FALLING:
		fall_timer=C.f32(fall_timer+delta)
		var duration:=1.0 if trap else 0.75
		var distance:=5.0 if trap else 6.0
		var old_z:=position.z
		position=Vector3(start_position.x,start_position.y,C.lerp32(start_position.z,C.f32(start_position.z+distance),C.ease_out(C.f32(fall_timer/duration))))
		if trap and position.z>1.0 and old_z<1.0:events.append(["TileSplash","TileSplashMedium","TileSplashLarge"][tile_size])
		if fall_timer<duration:return
		state=State.RESPAWNING
		if trap:position=start_position+Vector3(0,0,distance);fall_timer=0.0
		else:visible=false
		events.append("release_children")
	elif trap and state==State.RESPAWNING:
		respawn_timer=C.f32(respawn_timer+delta)
		flash=color_lerp(end_color,start_color,C.f32(respawn_timer/4.0))
		if respawn_timer>=4.0:
			respawn_timer=0.0;state=State.RISING;rising_solid=false
			events.append("event:/environment/floor_rise")
	elif trap and state==State.RISING:
		rising_timer=C.f32(rising_timer+delta)
		position=Vector3(start_position.x,start_position.y,C.lerp32(C.f32(start_position.z+5.0),start_position.z,C.ease_out(C.f32(rising_timer/0.5))))
		if rising_timer>=0.5:
			position=start_position;rising_timer=0.0;state=State.IDLE;events.append("rescan")
		elif rising_timer>=C.f32(0.2) and not rising_solid:rising_solid=true;layer=16

func snapshot()->Dictionary:
	return {"p":[position.x,position.y,position.z],"flash":[flash.r,flash.g,flash.b,flash.a],"state":state,"layer":layer,
		"timers":[warning_timer,shake_timer,fall_timer,respawn_timer,rising_timer] if trap else [warning_timer,shake_timer,fall_timer],
		"rising_solid":rising_solid,"visible":visible}
