class_name Graphnode
extends Area3D

var ID: int = -1
var boardMatrix: Array[Array] = []
var connections: Array[NodeLine] = []
var prevMat: Material
var isWinningPosition: bool = false
var velocity: Vector3 = Vector3.ZERO

# The click-selected node and current-board node are now the same visual concept.
# The old isClickedSelected flag is kept so existing selection functions still exist,
# but the visible highlight is driven by isCurrentBoardArrangement only.
var isClickedSelected: bool = false
var isCurrentBoardArrangement: bool = false
var currentBoardMat: Material

func _ready():
	velocity = Vector3.ZERO
	isWinningPosition = false
	connections = []
	prevMat = load("res://whiteMat.tres")
	currentBoardMat = make_current_board_material()
	apply_visual_material()

	if not AutoloadSignals.nodeSelected.is_connected(_on_node_selected):
		AutoloadSignals.nodeSelected.connect(_on_node_selected)

func _process(_delta):
	pass

func make_current_board_material() -> StandardMaterial3D:
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(0.10, 0.55, 1.00, 1.0)
	mat.emission_enabled = true
	mat.emission = Color(0.10, 0.55, 1.00, 1.0)
	mat.emission_energy_multiplier = 1.8
	return mat

func apply_visual_material():
	# One moving highlight only:
	# - clicking a graph node turns this same blue highlight on
	# - moving a 2D block moves this same blue highlight to the matching node
	# The previous red selection material is intentionally no longer used visually.
	if isCurrentBoardArrangement:
		$MeshInstance3D.material_override = currentBoardMat
	else:
		$MeshInstance3D.material_override = prevMat

func set_board_matrix(matrix: Array[Array]):
	boardMatrix = matrix.duplicate(true)

func get_board_matrix() -> Array[Array]:
	return boardMatrix

func set_node_position(xPos: float, yPos: float, zPos: float):
	position = Vector3(xPos, yPos, zPos)

func matrix_to_state_hash(matrix: Array[Array]) -> String:
	var state := ""

	for row in matrix:
		for value in row:
			var number := int(value)
			state += "." if number == 0 else str(number)

	return state

func _on_input_event(_camera, event, _event_position, _normal, _shape_idx):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		print("Shape " + str(ID) + " Clicked!")
		set_node_selected()

		# Treat a clicked node as the current board arrangement too.
		# multi_nodes.gd already owns the one-at-a-time blue highlight,
		# so this moves the blue highlight to this node and clears the old one.
		AutoloadSignals.board_position_changed.emit(matrix_to_state_hash(boardMatrix))
		AutoloadSignals.updateBoard.emit(boardMatrix.duplicate(true))

func _on_node_selected():
	isClickedSelected = false
	apply_visual_material()

func set_node_selected():
	AutoloadSignals.nodeSelected.emit()
	isClickedSelected = true
	apply_visual_material()
	print(boardMatrix)

func set_node_finish():
	isWinningPosition = true
	prevMat = load("res://greenLineMat.tres")
	apply_visual_material()

func set_node_start():
	prevMat = load("res://yellowLineMat.tres")
	apply_visual_material()

func set_node_current_board(active: bool):
	isCurrentBoardArrangement = active
	apply_visual_material()
