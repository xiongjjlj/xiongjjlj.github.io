class_name OriginalDiscTrail
extends RefCounted
## Unchanged Disc fire/ice gate and MoveProjectile trail semantics translated.
const C=preload("res://core/original_timekeeper.gd")
const A=preload("res://core/original_actor_motion.gd")
var burning:=false
var sliming:=false
var fire_distance:=0.0
var slime_distance:=0.0
var spacing:=C.f32(0.4)

func update_gate(disc:OriginalDiscMotion,dark:bool)->void:
	var owner:=disc.owner_actor
	if owner==null:return
	var offset:=disc.position-owner.position
	var normal:=A.normalized_xyz_xy(offset,C.f32(disc.source_z-owner.source_z))
	var dot:=OriginalDiscMotion.dot32(A.normalized(owner.velocity),normal)
	var threshold:=C.f32(C.f32(disc.min_distance_from_owner*2.0)+C.f32(C.f32(dot*C.f32(A.magnitude(owner.velocity)/owner.speed_limit))*10.0))
	var distance:=A.magnitude_xyz(offset,C.f32(disc.source_z-owner.source_z))
	var flight:bool=disc.movement in [OriginalDiscMotion.Movement.THROWING,OriginalDiscMotion.Movement.RETURNING,OriginalDiscMotion.Movement.CONTROLLED] or disc.orbit_is_charged
	var moving:=A.magnitude(disc.velocity)>9.0
	if (disc.disc_powerups&256)!=0 and (disc.disc_powerups&32)==0 and ((disc.disc_powerups&64)==0 or disc.is_mini_disc):
		if not dark and flight and distance>threshold and moving:burning=true
		elif disc.movement==OriginalDiscMotion.Movement.DROPPED or distance<=threshold:burning=false
	if (disc.disc_powerups&1024)!=0 and ((disc.disc_powerups&64)==0 or disc.is_mini_disc):
		if flight and distance>threshold and moving:sliming=true
		elif disc.movement==OriginalDiscMotion.Movement.DROPPED or distance<=threshold:sliming=false

func after_move(disc:OriginalDiscMotion,move_amount:Vector2,grid:OriginalCellGrid,owner:int)->void:
	var distance:=A.magnitude(move_amount*disc.fixed_delta)
	if disc.disc_powerups&256:fire_distance=C.f32(fire_distance+distance)
	elif disc.disc_powerups&1024:slime_distance=C.f32(slime_distance+distance)
	if burning and fire_distance>spacing:
		deposit(disc,move_amount,distance,grid,owner,OriginalCellGrid.CellType.FIRE,1.25)
		fire_distance=0.0
	if sliming and slime_distance>spacing:
		deposit(disc,move_amount,distance,grid,owner,OriginalCellGrid.CellType.ICE,4.0)
		slime_distance=0.0

func deposit(disc:OriginalDiscMotion,move_amount:Vector2,distance:float,grid:OriginalCellGrid,owner:int,type:int,duration:float)->void:
	grid.register_cell(type,disc.position,duration,owner)
	if distance>C.f32(spacing*2.0):
		var travelled:=0.0
		while travelled<distance:
			travelled=C.f32(travelled+spacing)
			grid.register_cell(type,disc.position-A.normalized(move_amount)*travelled,duration,owner)
