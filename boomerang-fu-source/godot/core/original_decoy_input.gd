class_name OriginalDecoyInput
extends RefCounted
const C=preload("res://core/original_timekeeper.gd")
const A=preload("res://core/original_actor_motion.gd")
enum GameState { PLAYING,PAUSED,TRANSITION_IN,MENU }
var actor:OriginalActorMotion
var input_move:=Vector2.ZERO
var input_dash:=false
var enabled:=true # GameManager.CreateDecoy does not call DecoyInput.Init.
var buffer:Array[float]=[]
var pending_end_frames:Array[int]=[]

func initialize()->void:
	pending_end_frames.clear()
	enabled=false

func frame_step(game_state:int,current_time:float)->void:
	if game_state==GameState.PLAYING:
		if not enabled:pending_end_frames.append(2)
	else:
		if game_state!=GameState.TRANSITION_IN:
			enabled=false
			input_move=Vector2.ZERO
		input_dash=false
	if not enabled or actor.state==A.State.DEAD:input_move=Vector2.ZERO
	if actor.state!=A.State.DEAD and enabled and input_dash:
		if actor.dash():buffer.clear()
		else:
			if buffer.size()==8:buffer.pop_front()
			buffer.append(C.f32(current_time))
	if input_dash or game_state!=GameState.PLAYING or actor.state!=A.State.ALIVE or buffer.is_empty():return
	for timestamp in buffer:
		if C.f32(current_time-timestamp)<=C.f32(0.25) and actor.dash():
			buffer.clear()
			break

func end_frame()->void:
	for i in range(pending_end_frames.size()-1,-1,-1):
		pending_end_frames[i]-=1
		if pending_end_frames[i]==0:
			enabled=true
			pending_end_frames.remove_at(i)

func snapshot()->Dictionary:
	return {"enabled":enabled,"dash":input_dash,"move":[input_move.x,input_move.y],"buffer":buffer.duplicate(),"pending":pending_end_frames.size()}
