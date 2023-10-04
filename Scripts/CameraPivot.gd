extends Marker3D

@export var movement_speed : float = 3

@export var rotation_speed : float = 3

@export var player : Node3D

# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):

	var rotation_delta = 0
	if Input.is_action_pressed("rotate_right"):
		rotation_delta += 1
	if Input.is_action_pressed("rotate_left"):
		rotation_delta -= 1	
	rotate_y(rotation_delta * rotation_speed* delta)
	
	position = lerp(position, player.position, movement_speed * delta)
	
	pass
