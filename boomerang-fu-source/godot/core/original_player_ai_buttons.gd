class_name OriginalPlayerAiButtons
extends RefCounted
## PlayerAI's button iterators. Call begin_frame before AI Update, consume the
## flags in PlayerInput.Update, then coroutine_phase before LateUpdate.
const C=preload("res://core/original_timekeeper.gd")
enum ButtonType { DASH,ATTACK,THROW }
var input_timer:=0.0
var input_delay:=C.f32(0.15)
var attack_pressed:=false
var dash_pressed:=false
var attack:=false
var dash:=false
var released:=false
var held:=false
var blocked:=false
var frame:=-1
var delta:=0.0
var hold_handle:=-1
var next_handle:=0
var routines:Dictionary={}
var order:Array[int]=[]

func begin_frame(frame_delta:float)->void:
	frame+=1;delta=C.f32(frame_delta)

func advance_input_timer()->void:
	if input_timer<=input_delay:input_timer=C.f32(input_timer+delta)

func cancel_all()->void:
	# AI.Init calls StopAllCoroutines without resetting its stored button bits.
	routines.clear();order.clear()

func clear()->void:
	cancel_all();hold_handle=-1
	attack=false;dash=false;released=false;held=false;blocked=false
	attack_pressed=false;dash_pressed=false;input_timer=0.0

func _schedule(state:Dictionary)->int:
	var handle:=next_handle;next_handle+=1
	state.wake=frame+1;routines[handle]=state;order.append(handle)
	return handle

func cancel(handle:int)->void:
	routines.erase(handle);order.erase(handle)

func push_button(button:int)->void:
	match button:
		ButtonType.DASH:
			input_timer=0.0;dash=true;dash_pressed=true
		ButtonType.ATTACK:
			if attack_pressed or input_timer<input_delay:return
			input_timer=0.0;attack=true;attack_pressed=true
		ButtonType.THROW:
			if held or blocked or input_timer<input_delay:return
			input_timer=0.0;held=true
	release_button(button)

func release_button(button:int)->int:
	if button==ButtonType.THROW:
		if not held and not blocked:return -1
		held=false;released=true
	return _schedule({"kind":"release","button":button})

func hold_button(button:int,duration:float)->int:
	if button!=ButtonType.THROW or input_timer<input_delay:return -1
	input_timer=0.0
	if blocked:
		var child:=release_button(ButtonType.THROW)
		return _schedule({"kind":"hold_wait","child":child,"duration":C.f32(duration),"elapsed":0.0})
	held=true
	var state:={"kind":"hold","duration":C.f32(duration),"elapsed":0.0}
	var handle:=_schedule(state)
	_resume_hold(handle,state)
	return handle

func replace_hold(button:int,duration:float)->void:
	cancel(hold_handle);hold_handle=hold_button(button,duration)

func stop_holding_throw()->void:
	cancel(hold_handle);release_button(ButtonType.THROW)

func _resume_hold(handle:int,state:Dictionary)->void:
	if state.elapsed<state.duration:
		state.elapsed=C.f32(state.elapsed+delta)
		if not held:cancel(handle);return
		state.wake=frame+1
	elif held:
		held=false;released=true
		state.kind="release";state.button=ButtonType.THROW;state.wake=frame+1
	else:cancel(handle)

func consume_release()->void:
	# Normal active-play PlayerInput.Update resets its release bit, and clears
	# inputThrowIsBlocked when a blocked release is consumed.
	if released and blocked:blocked=false
	released=false

func coroutine_phase()->void:
	# Children are registered before parents waiting on StartCoroutine. Native
	# Unity resumes such parents in the same phase in which their child ends.
	for handle:int in order.duplicate():
		if not routines.has(handle):continue
		var state:Dictionary=routines[handle]
		if state.wake>frame:continue
		match state.kind:
			"release":
				match int(state.button):
					ButtonType.DASH:dash=false;dash_pressed=false
					ButtonType.ATTACK:attack=false;attack_pressed=false
					ButtonType.THROW:released=false
				cancel(handle)
			"hold_wait":
				if routines.has(int(state.child)):continue
				held=true;state.kind="hold";_resume_hold(handle,state)
			"hold":_resume_hold(handle,state)

func snapshot()->Dictionary:
	return {"timer":input_timer,"delay":input_delay,"attack_pressed":attack_pressed,"dash_pressed":dash_pressed,"attack":attack,"dash":dash,"thrown":released,"held":held,"blocked":blocked}
