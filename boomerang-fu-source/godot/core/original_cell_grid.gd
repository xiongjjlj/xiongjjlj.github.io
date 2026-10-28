class_name OriginalCellGrid
extends RefCounted
## Static-grid CellManager semantics. Sparse storage is bounded by grid_size.
## Particle/decal output and transformed moving-floor routing are separate.
const C = preload("res://core/original_timekeeper.gd")
enum CellType { NONE, FIRE, ICE }
var grid_size := Vector2i(100,100)
var cell_size := C.f32(1.2)
var entries: Dictionary = {}
var fire_count := 0
var ice_count := 0
var current_time := 0.0
var fire_spawn_allowed: Callable
var ice_spawn_allowed: Callable
var world: OriginalCollisionWorld
var events: Array[Dictionary] = []

static func round_even(value: float) -> int:
	var lower := floorf(value)
	if value-lower>0.5 or (value-lower==0.5 and int(lower)%2!=0): lower+=1.0
	return int(lower)

func coordinates(position: Vector2, force := false) -> Vector2i:
	var index := Vector2i(round_even(C.f32(position.x/cell_size))+int(grid_size.x/2),round_even(C.f32(position.y/cell_size))+int(grid_size.y/2))
	if force: index=index.clamp(Vector2i.ZERO,grid_size-Vector2i.ONE)
	if not on_grid(index): return Vector2i(-1,-1)
	return index

func on_grid(index: Vector2i) -> bool:
	return index.x>=0 and index.y>=0 and index.x<grid_size.x and index.y<grid_size.y

func world_position(index: Vector2i) -> Vector2:
	return Vector2(C.f32((index.x-int(grid_size.x/2))*cell_size),C.f32((index.y-int(grid_size.y/2))*cell_size))

func within_radius(center: Vector2i,radius: int) -> Array[Vector2i]:
	var result: Array[Vector2i]=[]
	for x in range(maxi(0,center.x-radius),mini(grid_size.x-1,center.x+radius)+1):
		var d := center.x-x
		var reach := round_even(C.f32(sqrt(C.f32(radius*radius-d*d))))
		for y in range(maxi(0,center.y-reach),mini(grid_size.y-1,center.y+reach)+1): result.append(Vector2i(x,y))
	return result

func neighbors(index: Vector2i) -> Array[Vector2i]:
	return [index,Vector2i(index.x,mini(index.y+1,grid_size.y-1)),Vector2i(index.x,maxi(index.y-1,0)),Vector2i(mini(index.x+1,grid_size.x-1),index.y),Vector2i(maxi(index.x-1,0),index.y)]

func cell_at(index: Vector2i) -> Dictionary:
	return entries.get(index,{"type":CellType.NONE,"duration":0.0,"timestamp":0.0,"owner":-1})

func set_type(index: Vector2i,type: int,duration: float,owner: int,position: Vector2) -> void:
	var cell := cell_at(index)
	if type==CellType.FIRE:
		if cell.type!=CellType.ICE and (not fire_spawn_allowed.is_valid() or fire_spawn_allowed.call(position)):
			if cell.type!=CellType.FIRE: fire_count+=1
			cell.type=CellType.FIRE
			events.append({"type":"fire_emission","position":position})
	elif type==CellType.ICE:
		if not ice_spawn_allowed.is_valid() or ice_spawn_allowed.call(position):
			if cell.type!=CellType.ICE: ice_count+=1
			if cell.type==CellType.FIRE: fire_count-=1
			cell.type=CellType.ICE
			events.append({"type":"ice_decal","position":position})
	# Source refreshes these even when the requested type is denied.
	cell.timestamp=C.f32(current_time)
	cell.duration=C.f32(duration)
	cell.owner=owner
	entries[index]=cell

func register_cell(type: int,position: Vector2,duration: float,owner: int) -> void:
	var index := coordinates(position)
	if on_grid(index): set_type(index,type,duration,owner,position)

func register_area(type: int,position: Vector2,duration: float,owner: int,radius: int,check_obstacles := false) -> void:
	var center := coordinates(position)
	if not on_grid(center): return
	for index in within_radius(center,radius):
		var point := world_position(index)
		if check_obstacles and world!=null:
			var direction := point-position
			if not world.circle_cast(position,0.0,OriginalActorMotion.normalized(direction),OriginalActorMotion.magnitude(direction),256).is_empty(): continue
		set_type(index,type,duration,owner,point)

func deregister(index: Vector2i) -> void:
	var cell := cell_at(index)
	if cell.type==CellType.FIRE: fire_count-=1
	elif cell.type==CellType.ICE: ice_count-=1
	cell.type=CellType.NONE
	cell.duration=0.0
	cell.owner=-1
	entries[index]=cell

func extinguish(position: Vector2,type: int,radius := 1) -> void:
	var center := coordinates(position,true)
	var indices := within_radius(center,radius) if radius>1 else neighbors(center)
	for index in indices:
		if cell_at(index).type==type: deregister(index)

func touching(position: Vector2,type: int) -> Dictionary:
	var center := coordinates(position)
	if on_grid(center):
		for index in neighbors(center):
			var cell := cell_at(index)
			if cell.type==type: return {"touch":true,"owner":cell.owner}
	return {"touch":false,"owner":-1}

func frame_step(time: float) -> void:
	current_time=C.f32(time)
	if fire_count==0 and ice_count==0: return
	for index in entries:
		var cell: Dictionary=entries[index]
		if cell.type!=CellType.NONE and C.f32(current_time-cell.timestamp)>cell.duration: deregister(index)

func clean_up() -> void:
	for index in entries: entries[index].type=CellType.NONE
	fire_count=0
	ice_count=0
	events.clear()
