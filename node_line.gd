class_name NodeLine
extends MeshInstance3D

var storedSize: float
var nodeID1: int
var nodeID2: int

# Called when the node enters the scene tree for the first time.
func _ready():
	pass

func changeHeight(size: float):
	var cylinder: CylinderMesh = CylinderMesh.new()
	cylinder.height = size
	cylinder.material = load("res://whiteMat.tres")
	storedSize = size
	mesh = cylinder

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta):
	pass

func set_connection_shortpath():
	material_override = load("res://greenLineMat.tres")
	mesh.top_radius *= 3
	mesh.bottom_radius *= 3

func set_connection_geminipath():
	material_override = load("res://redLineMat.tres")

func create_line(first: Vector3, second: Vector3, id1:int,id2:int):
	position = (first + second)/2
	material_override = load("res://whiteMat.tres")
	var distance = (first.distance_to(second))
	if distance == 0:
		distance = 0.5
	scale.y = distance / 2
	if (position != first):
		look_at_from_position(position, first, Vector3(0, 1, 0.001))
	# this is to fix the rotation, since the direction it faces is 90 degrees off from intended
	rotation_degrees.x += 90
	
	nodeID1 = id1
	nodeID2 = id2
	
	
