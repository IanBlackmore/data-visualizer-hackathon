class_name NodeLine
extends MeshInstance3D


var nodeID1: int
var nodeID2: int

# Called when the node enters the scene tree for the first time.
func _ready():
	nodeID1 = 0
	nodeID2 = 0

func changeHeight(size: float):
	var cylinder: CylinderMesh = CylinderMesh.new()
	cylinder.height = size
	mesh = cylinder

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta):
	pass
