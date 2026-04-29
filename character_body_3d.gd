extends CharacterBody3D

@export var speed := 45.0
@export var rotation_speed := 1.5

var yaw := 0.0
var pitch := 0.0

@onready var camera = $Camera3D

func _physics_process(delta):
	handle_movement(delta)
	handle_rotation(delta)

func handle_movement(_delta):
	var direction = Vector3.ZERO

	if Input.is_action_pressed("move_forward"):
		direction -= transform.basis.z
	if Input.is_action_pressed("move_backward"):
		direction += transform.basis.z
	if Input.is_action_pressed("move_left"):
		direction -= transform.basis.x
	if Input.is_action_pressed("move_right"):
		direction += transform.basis.x
	if Input.is_action_pressed("move_up"):
		direction += transform.basis.y
	if Input.is_action_pressed("move_down"):
		direction -= transform.basis.y

	direction = direction.normalized()

	velocity = direction * speed
	move_and_slide()

func handle_rotation(delta):
	if Input.is_action_pressed("look_left"):
		yaw += rotation_speed * delta
	if Input.is_action_pressed("look_right"):
		yaw -= rotation_speed * delta
	if Input.is_action_pressed("look_up"):
		pitch += rotation_speed * delta
	if Input.is_action_pressed("look_down"):
		pitch -= rotation_speed * delta

	# Clamp pitch so you don't flip upside down
	pitch = clamp(pitch, -1.5, 1.5)

	# Apply rotation
	rotation.y = yaw
	camera.rotation.x = pitch
