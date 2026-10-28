class_name OriginalUnityRandom
extends RefCounted
## UnityPlayer Random state/output reconstruction, verified against native samples.
const C=preload("res://core/original_timekeeper.gd")
const MASK:=0xffffffff
var words:Array[int]=[0,1,1812433254,1900727103]
var draws:=0

func _init(seed_value:=0)->void:
	initialize(seed_value)

func initialize(seed_value:int)->void:
	words[0]=seed_value&MASK
	for i in range(1,4):words[i]=(1812433253*words[i-1]+1)&MASK
	draws=0

func set_state(values:Array)->void:
	assert(values.size()==4)
	for i in 4:words[i]=int(values[i])&MASK

func get_state()->Array[int]:
	return words.duplicate()

func next_uint()->int:
	var t:=(words[0]^(words[0]<<11))&MASK
	var next:=(words[3]^(words[3]>>19)^t^(t>>8))&MASK
	words[0]=words[1]
	words[1]=words[2]
	words[2]=words[3]
	words[3]=next
	draws+=1
	return next

func value()->float:
	return C.f32(float(next_uint()&0x7fffff)/8388607.0)

func range_float(lower:float,upper:float)->float:
	lower=C.f32(lower)
	upper=C.f32(upper)
	var t:=value()
	# Native C++ uses float products and reversed interpolation, including when
	# both bounds are equal. Algebraic lerp rewrites change the final float bits.
	return C.f32(C.f32(lower*t)+C.f32(upper*C.f32(1.0-t)))

func range_int(lower:int,upper:int)->int:
	if lower==upper:return lower
	var offset:=next_uint()%absi(upper-lower)
	return lower+offset if upper>lower else lower-offset

func inside_unit_circle()->Vector2:
	var angle:=range_float(0.0,C.f32(TAU))
	var radius:=C.f32(sqrt(range_float(0.0,1.0)))
	return Vector2(C.f32(C.f32(cos(angle))*radius),C.f32(C.f32(sin(angle))*radius))
