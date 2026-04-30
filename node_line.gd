class_name NodeLine
extends MeshInstance3D

var storedSize: float
var nodeID1: int
var nodeID2: int
var is_short_path: bool = false

func _ready():
	pass

func changeHeight(size: float):
	var cylinder: CylinderMesh = CylinderMesh.new()
	cylinder.height = size
	cylinder.material = load("res://whiteMat.tres")
	storedSize = size
	mesh = cylinder

func _process(_delta):
	pass

func set_connection_shortpath():
	if is_short_path:
		return

	is_short_path = true
	material_override = load("res://greenLineMat.tres")

	if mesh is CylinderMesh:
		mesh = mesh.duplicate(true)
		mesh.top_radius = 0.6
		mesh.bottom_radius = 0.6

func set_connection_geminipath():
	material_override = load("res://redLineMat.tres")

func create_line(first: Vector3, second: Vector3, id1: int, id2: int):
	position = (first + second) / 2.0
	material_override = load("res://whiteMat.tres")

	var distance := first.distance_to(second)
	if distance <= 0.01:
		distance = 0.5

	# Cylinder height is 1, so scale.y should be the full distance.
	scale.y = distance

	if position != first:
		look_at_from_position(position, first, Vector3(0, 1, 0.001))
	rotation_degrees.x += 90

	nodeID1 = id1
	nodeID2 = id2
