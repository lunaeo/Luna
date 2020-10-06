*/ 48 - Estaq stronghold
*
*\

\* Warps */

(0:1) Whenever someone moves,
 (1:35) and they move into position (17, 5),
    (5:19) move the triggering player to (2, 22, 115).

(0:1) Whenever someone moves,
 (1:35) and they move into position (18, 12),
    (5:19) move the triggering player to (11, 17, 115).

(0:1) Whenever someone moves,
 (1:35) and they move into position (25, 12),
    (5:19) move the triggering player to (11, 8, 113).

(0:1) Whenever someone moves,
 (1:35) and they move into position (6, 19),
    (5:19) move the triggering player to (7, 11, 114).

(0:1) Whenever someone moves,
 (1:35) and they move into position (12, 20),
    (5:19) move the triggering player to (13, 9, 68).

(0:1) Whenever someone moves,
 (1:35) and they move into position (36, 21),
    (5:19) move the triggering player to (11, 8, 201).

(0:1) Whenever someone moves,
 (1:35) and they move into position (0, 40),
    (5:19) move the triggering player to (42, 20, 40).

(0:1) Whenever someone moves,
 (1:35) and they move into position (0, 41),
    (5:19) move the triggering player to (42, 21, 40).

(0:1) Whenever someone moves,
 (1:35) and they move into position (0, 42),
    (5:19) move the triggering player to (42, 22, 40).

(0:1) Whenever someone moves,
 (1:35) and they move into position (48, 62),
    (5:19) move the triggering player to (1, 37, 117).

(0:1) Whenever someone moves,
 (1:35) and they move into position (48, 63),
    (5:19) move the triggering player to (1, 38, 117).

(0:1) Whenever someone moves,
 (1:35) and they move into position (0, 64),
    (5:19) move the triggering player to (41, 20, 40).

(0:1) Whenever someone moves,
 (1:35) and they move into position (48, 64),
    (5:19) move the triggering player to (1, 39, 117).

(0:1) Whenever someone moves,
 (1:35) and they move into position (0, 65),
    (5:19) move the triggering player to (41, 21, 40).

(0:1) Whenever someone moves,
 (1:35) and they move into position (0, 66),
    (5:19) move the triggering player to (41, 22, 40).

\* Doors */

(0:151) When someone tries to open a door at (18, 12),
 (1:80) and they can see position (18, 12),
    (5:200) open the door at (18, 12) on the map.

(0:151) When someone tries to open a door at (25, 12),
 (1:80) and they can see position (25, 12),
    (5:200) open the door at (25, 12) on the map.

(0:151) When someone tries to open a door at (6, 19),
 (1:80) and they can see position (6, 19),
    (5:200) open the door at (6, 19) on the map.

(0:151) When someone tries to open a door at (12, 20),
 (1:80) and they can see position (12, 20),
    (5:200) open the door at (12, 20) on the map.

(0:151) When someone tries to open a door at (36, 21),
 (1:80) and they can see position (36, 21),
    (5:200) open the door at (36, 21) on the map.

\* NPC Spawns */
(0:0) When everything is starting up,
    (5:220) spawn a new NPC 1 at (22, 20) on the map.
    (5:220) spawn a new NPC 7 at (22, 28) on the map.

