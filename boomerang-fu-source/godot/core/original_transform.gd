extends RefCounted
## Unity's serialized quaternion-to-matrix conversion retains the original
## float32 coefficients. Godot's Quaternion -> Basis normalizes them, which
## can change near-tied navigation choices at adjoining platform edges.
const C=preload("res://core/original_timekeeper.gd")
static func from_source(data: Dictionary) -> Transform3D:
	var q: Dictionary=data.m_LocalRotation
	var x:=C.f32(-q.x);var y:=C.f32(-q.y);var z:=C.f32(q.z);var w:=C.f32(q.w)
	var x2:=C.f32(x*2.0);var y2:=C.f32(y*2.0);var z2:=C.f32(z*2.0)
	var xx:=C.f32(x*x2);var yy:=C.f32(y*y2);var zz:=C.f32(z*z2)
	var xy:=C.f32(x*y2);var xz:=C.f32(x*z2);var yz:=C.f32(y*z2)
	var wx:=C.f32(w*x2);var wy:=C.f32(w*y2);var wz:=C.f32(w*z2)
	var basis:=Basis(Vector3(C.f32(1.0-C.f32(yy+zz)),C.f32(xy+wz),C.f32(xz-wy)),Vector3(C.f32(xy-wz),C.f32(1.0-C.f32(xx+zz)),C.f32(yz+wx)),Vector3(C.f32(xz+wy),C.f32(yz-wx),C.f32(1.0-C.f32(xx+yy))))
	var s: Dictionary=data.m_LocalScale;var p: Dictionary=data.m_LocalPosition
	return Transform3D(Basis(basis.x*float(s.x),basis.y*float(s.y),basis.z*float(s.z)),Vector3(p.x,p.y,-p.z))
