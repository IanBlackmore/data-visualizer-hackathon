class_name NodeLine
extends MeshInstance3D

var storedSize: float
var nodeID1: int
var nodeID2: int

# Called when the node enters the scene tree for the first time.
func _ready():
	nodeID1 = 0
	nodeID2 = 0

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
	var cylinder: CylinderMesh = CylinderMesh.new()
	cylinder.height = storedSize
	cylinder.material = load("res://greenLineMat.tres")
	mesh = cylinder

func set_connection_geminipath():
	var cylinder: CylinderMesh = CylinderMesh.new()
	cylinder.height = storedSize
	cylinder.material = load("res://redLineMat.tres")
	mesh = cylinder

func create_line(first: Vector3, second: Vector3, id1:int,id2:int):
	position = (first + second)/2
	
	var distance = (first.distance_to(second))
	scale.y = distance * 2
	look_at_from_position(position, first, Vector3(0, 1, 0.001))
	# this is to fix the rotation, since the direction it faces is 90 degrees off from intended
	rotation_degrees.x += 90
	
	nodeID1 = id1
	nodeID2 = id2
