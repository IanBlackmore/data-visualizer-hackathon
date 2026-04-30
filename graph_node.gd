class_name Graphnode
extends Area3D

var ID: int = -1
var boardMatrix: Array[Array] = []
var connections: Array[NodeLine] = []
var prevMat: Material
var isWinningPosition: bool = false
var velocity: Vector3 = Vector3.ZERO

func _ready():
	velocity = Vector3.ZERO
	isWinningPosition = false
	connections = []
	$MeshInstance3D.material_override = load("res://whiteMat.tres")
	prevMat = load("res://whiteMat.tres")

	if not AutoloadSignals.nodeSelected.is_connected(_on_node_selected):
		AutoloadSignals.nodeSelected.connect(_on_node_selected)

func _process(_delta):
	pass

func set_board_matrix(matrix: Array[Array]):
	boardMatrix = matrix.duplicate(true)

func get_board_matrix() -> Array[Array]:
	return boardMatrix

func set_node_position(xPos: float, yPos: float, zPos: float):
	position = Vector3(xPos, yPos, zPos)

func _on_input_event(_camera, event, _event_position, _normal, _shape_idx):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		print("Shape " + str(ID) + " Clicked!")
		set_node_selected()
		AutoloadSignals.updateBoard.emit(boardMatrix.duplicate(true))

func _on_node_selected():
	$MeshInstance3D.material_override = prevMat

func set_node_selected():
	AutoloadSignals.nodeSelected.emit()
	$MeshInstance3D.material_override = load("res://redLineMat.tres")
	print(boardMatrix)

func set_node_finish():
	isWinningPosition = true
	$MeshInstance3D.material_override = load("res://greenLineMat.tres")
	prevMat = load("res://greenLineMat.tres")

func set_node_start():
	$MeshInstance3D.material_override = load("res://yellowLineMat.tres")
	prevMat = load("res://yellowLineMat.tres")
