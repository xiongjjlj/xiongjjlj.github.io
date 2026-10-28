class_name OriginalActorBounds
extends RefCounted
## Player.CheckIfPlayerIsInBounds. Timer uses global delta; metadata uses fixed.
const C=preload("res://core/original_timekeeper.gd")
var boundary:OriginalLevelBoundary
var actor:OriginalActorMotion
var outside:=false
var timer:=0.0
var danger:=0.0
var arrow:=0.0
var effect:=false
var vibration:=0.0
var metadata:=0.0
var warnings:=0
var death_requested:=false
var decoy_durations:Dictionary={} # Shared by Decoys only; separate source static.

func stop_effect()->void:
	effect=false
	danger=0.0

func frame_step(delta:float,point:Vector2)->void:
	death_requested=false
	if not boundary.is_in_bounds(point):
		if not outside:
			outside=true
			var duration:=C.f32(3.75 if boundary.shrinking else 3.0)
			if decoy_durations.is_empty():boundary.out_duration=duration
			else:decoy_durations.value=duration
			timer=duration
	elif outside:
		outside=false
		vibration=0.0
	if outside:
		var duration:float=boundary.out_duration if decoy_durations.is_empty() else decoy_durations.value
		if timer>0.0 and actor.state==OriginalActorMotion.State.ALIVE:
			var t:=C.f32(1.0-C.f32(timer/duration))
			var eased:=C.f32(1.0-C.f32(cos(C.f32(C.f32(PI/2.0)*t))))
			timer=C.f32(timer-C.f32(delta))
			var threshold:=C.f32(duration*C.f32(0.8))
			if timer<=threshold:
				effect=true
				var phase:=C.f32(C.f32(4.712389)+C.f32(C.f32(threshold-timer)*C.f32(3.0+C.f32(eased*17.0))))
				var pulse:=C.f32(0.5+C.f32(C.f32(sin(phase))*0.5))
				if pulse>C.f32(0.8) and danger<=C.f32(0.8):warnings+=1
				danger=pulse
				arrow=pulse
			else:danger=0.0
			if decoy_durations.is_empty():
				vibration=C.f32(eased*0.5)
				metadata=C.f32(metadata+actor.fixed_delta)
		elif timer<=0.0:
			vibration=0.0
			stop_effect()
			if not boundary.is_in_bounds(point):death_requested=true
	else:stop_effect()
