class_name OriginalLevelBoundary
extends RefCounted
## LevelManager shrink, strict bounds tests and source corner interpolation.
const C=preload("res://core/original_timekeeper.gd")
var data:Dictionary
var size:=Vector2(43,43)
var original_size:=Vector2(43,43)
var center:=Vector2.ZERO
var offset:=Vector2.ZERO
var scale:=Vector2.ONE
var angle:=0.0
var allow_outside:=false
var level_present:=true
var boundary_present:=true
var shrinking:=false
var timer:=0.0
var duration:=0.0
var start_size:=Vector2.ZERO
var target_size:=Vector2(10,10)
var start_corners:=PackedVector2Array()
var target_corners:=PackedVector2Array()
var owner:=-1
var marker:=false
var glow:=false
var flash_remaining:=0.0
var scroll:=0.0
var line_color:=Color(0,0,0,0.1019607857)
var round_timer:=0.0
var sudden_death:=false
var tutorial:=false
var out_duration:=3.0 # Player.outOfBoundsDuration is shared across players.
var rng:=RandomNumberGenerator.new()
var events:Array[Dictionary]=[]

func initialize(inputs:Dictionary)->void:
	data=inputs
	original_size=Vector2(data.level_box.m_Size.x,data.level_box.m_Size.y)
	size=original_size
	center=Vector2(data.level_transform.m_LocalPosition.x,data.level_transform.m_LocalPosition.y)
	scale=Vector2(data.level_transform.m_LocalScale.x,data.level_transform.m_LocalScale.y)
	offset=Vector2(data.level_box.m_Offset.x,data.level_box.m_Offset.y)
	allow_outside=data.allow_outside
	tutorial=data.tutorial
	rng.seed=0xB00F2

static func rotate(point:Vector2,degrees:float)->Vector2:
	var half:=C.f32(C.f32(degrees)*C.f32(0.008726646259971648))
	var z:=C.f32(sin(half))
	var w:=C.f32(cos(half))
	var twice:=C.f32(z*2.0)
	var diagonal:=C.f32(1.0-C.f32(z*twice))
	var cross_term:=C.f32(w*twice)
	return Vector2(C.f32(C.f32(diagonal*point.x)-C.f32(cross_term*point.y)),C.f32(C.f32(cross_term*point.x)+C.f32(diagonal*point.y)))

func local_point(point:Vector2)->Vector2:
	var half:=C.f32(C.f32(angle)*C.f32(0.008726646259971648))
	var z:=C.f32(sin(half))
	var w:=C.f32(cos(half))
	var twice:=C.f32(z*2.0)
	var diagonal:=C.f32(1.0-C.f32(z*twice))
	var cross_term:=C.f32(w*twice)
	var determinant:=C.f32(C.f32(diagonal*diagonal)+C.f32(cross_term*cross_term))
	diagonal=C.f32(diagonal/determinant)
	cross_term=C.f32(cross_term/determinant)
	point=point-center
	return Vector2(C.f32(C.f32(diagonal*point.x)+C.f32(cross_term*point.y)),C.f32(C.f32(-cross_term*point.x)+C.f32(diagonal*point.y)))/scale-offset

func is_in_bounds(point:Vector2)->bool:
	if not level_present or not boundary_present or allow_outside:return true
	point=local_point(point)
	var half:=size*0.5
	return point.x<half.x and point.x>-half.x and point.y<half.y and point.y>-half.y

func is_near(point:Vector2)->bool:
	if not boundary_present:return false
	point=local_point(point)
	var half:=size*0.5-Vector2(4,4)
	return point.x>half.x or point.x<-half.x or point.y>half.y or point.y<-half.y

func clamp_position(point:Vector2,padding:Vector2)->Vector2:
	# Source clamps around world origin against the original level box.
	point=rotate(point,-angle)
	point.x=maxf(minf(point.x,C.f32(original_size.x*0.5-padding.x)),C.f32(-original_size.x*0.5+padding.x))
	point.y=maxf(minf(point.y,C.f32(original_size.y*0.5-padding.y)),C.f32(-original_size.y*0.5+padding.y))
	return rotate(point,angle)

func corners(point:Vector2,dimensions:Vector2)->PackedVector2Array:
	var half:=dimensions*0.5
	var result:=PackedVector2Array()
	for local in [Vector2(-half.x,-half.y),Vector2(-half.x,half.y),Vector2(half.x,half.y),Vector2(half.x,-half.y)]:
		# Match source add/subtract before the quaternion operation.
		result.append(rotate((point+local)-point,angle)+point)
	return result

func start(target:Vector2,show_marker:=false,boundary_owner:=-1)->void:
	timer=0.0
	shrinking=true
	start_size=size
	duration=C.f32(size.x/7.0)
	start_corners=corners(center,size)
	allow_outside=false
	flash_remaining=C.f32(0.5)
	line_color=Color(1,1,1,0)
	glow=true
	target=clamp_position(target,target_size/2.0)
	target_corners=corners(target,target_size)
	marker=show_marker
	owner=boundary_owner
	events.append({"type":"shrink_started","target":target,"owner":owner,"duration":duration,"marker":marker})

func stop()->void:
	shrinking=false
	marker=false

static func lerp_vector(a:Vector2,b:Vector2,t:float)->Vector2:
	return Vector2(C.lerp32(a.x,b.x,t),C.lerp32(a.y,b.y,t))

func frame_step(delta:float,playing:bool)->void:
	delta=C.f32(delta)
	if not playing or not shrinking:return
	if timer<duration:
		timer=C.f32(timer+delta)
		var t:=clampf(OriginalExplosion.curve_value(data.curve.m_Curve,C.f32(timer/duration)),0,1)
		size=lerp_vector(start_size,target_size,t)
		center=lerp_vector(lerp_vector(start_corners[0],target_corners[0],t),lerp_vector(start_corners[2],target_corners[2],t),0.5)
	else:
		size=target_size
		stop()

func game_frame(delta:float,playing:bool,survivors:int)->void:
	if not playing:return
	round_timer=C.f32(round_timer+C.f32(delta))
	if round_timer>180.0 and not sudden_death and level_present and not tutorial and not shrinking and survivors>1:
		sudden_death=true
		var target:=center+Vector2(C.f32(rng.randf_range(-size.x*0.5,size.x*0.5)),C.f32(rng.randf_range(-size.y*0.5,size.y*0.5)))
		target=rotate(target-center,angle)+center
		start(target,true)
	if shrinking and survivors<2:stop()

func visual_frame(delta:float,positions:Array,states:Array)->void:
	delta=C.f32(delta)
	scroll=fmod(C.f32(scroll-C.f32(C.f32(delta*C.f32(0.5))*(-0.5 if shrinking else 1.0))),1.0)
	if flash_remaining>0.0:
		flash_remaining=C.f32(flash_remaining-delta)
		var t:=clampf(1.0-flash_remaining/0.5,0,1)
		t=1.0-(1.0-t)*(1.0-t) # DOTween default OutQuad.
		line_color=Color(1,1,1,0).lerp(Color(0,0,0,0.2980392277),t)
		return
	if positions.is_empty():return
	var target:=Color(0,0,0,0.1019607857)
	if shrinking:target=Color(0,0,0,0.8)
	else:
		for i in positions.size():
			if states[i]!=OriginalActorMotion.State.ALIVE:continue
			if not is_in_bounds(positions[i]):
				target=Color(0,0,0,0.8)
				break
			if is_near(positions[i]):target=Color(0,0,0,0.2980392277)
	line_color=line_color.lerp(target,clampf(C.f32(delta*5.0),0,1))
