class_name OriginalSystemRandom
extends RefCounted
## Mono System.Random's seeded subtractive generator, used by RandomPath.
const BIG:=2147483647
var values:=PackedInt64Array()
var next_index:=0
var next_partner:=21
static func int32(value:int)->int:return ((value+0x80000000)&0xffffffff)-0x80000000
func _init(seed_value:=0)->void: initialize(seed_value)
func initialize(seed_value:int)->void:
	values.resize(56);values.fill(0)
	var subtraction:=BIG if seed_value==-2147483648 else absi(seed_value)
	var current:=161803398-subtraction
	values[55]=current
	var previous:=1
	for i in range(1,55):
		var index:=(21*i)%55
		values[index]=previous
		previous=int32(current-previous)
		if previous<0:previous+=BIG
		current=values[index]
	for pass_index in 4:
		for i in range(1,56):
			values[i]=int32(values[i]-values[1+(i+30)%55])
			if values[i]<0:values[i]+=BIG
	next_index=0;next_partner=21
func sample_int()->int:
	next_index+=1;next_partner+=1
	if next_index>=56:next_index=1
	if next_partner>=56:next_partner=1
	var result:=int32(values[next_index]-values[next_partner])
	if result==BIG:result-=1
	if result<0:result+=BIG
	values[next_index]=result
	return result
func next_double()->float:return float(sample_int())*(1.0/2147483647.0)
