class_name Graphnode
extends Area3D


var ID: int
var boardMatrix: Array[Array]
var connections: Array[NodeLine]

# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta):
	pass

func set_board_matrix(matrix: Array[Array]):
	boardMatrix = matrix

func get_board_matrix() -> Array[Array]:
	return boardMatrix



func set_node_position(xPos:float, yPos:float, zPos:float):
	$".".position = Vector3(xPos, yPos, zPos)


func _on_input_event(_camera, event, _event_position, _normal, _shape_idx):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		print("Shape " + str(ID) + " Clicked!")
		#do other stuff to display current instance of graph
		set_node_selected()

func set_node_selected():
	$MeshInstance3D.material_override = load("res://redLineMat.tres")

func set_node_finish():
	$MeshInstance3D.material_override = load("res://greenLineMat.tres")
