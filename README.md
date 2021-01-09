# Othello
This project is an AI playing Othello, the infamous board game that no one played before this TP. It is done for the Artificial intelligence course, chapter "MinMax"

# Existing base
The project already has a fully functionnal GUI and two exemples AIs.
* The GUI and the controller are not modified AT ALL
* The IA_STUB is kept as an exemple of the base AI implamentation
* The Othello_OHU is modified to host our newly done Despacito'thello

The othello OHU had some functions to specify the rules of othello such as playMove, that flipped the pawns, or getPossibleMove, that calculated the next possible moves. These functions are not specifically related to the AI - they have been kept to gain time and avoid errors on othello rules.\
[iplayable](./img/isplayable_default.png?raw=true)
_before, according to interface, with board by default_
[iplayable](./img/isplayable_field.png?raw=true)
_after, function using the field and used by the interface gateway_
\

The MinMax AI is based on a multiple rounds ahead calculation - it needs to "play" rounds in its head without impacting the game board. The only addition to those functions has been the addition of a "field" parameter, which defaults to the game board. The AI can then uses these to get he possible moves and to play the moves on fields that are not the game board, but intermediate, fictive boards.


# Despacito'thello
Despacito'tello is an AI using MinMax. Its code is located on the Othello_OHU/ArcOthello_Despacito.cs file, and is accessible by opening the Othello.sln project.

## Heuristic function
### Static
### Dynamic
### Late game
### End game

## Implementation specificities