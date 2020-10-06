*/ 225 - No name specified
*
*\

\* Warps */

(0:1) Whenever someone moves,
 (1:35) and they move into position (15, 24),
    (5:19) move the triggering player to (29, 27, 225).

(0:1) Whenever someone moves,
 (1:35) and they move into position (29, 25),
    (5:19) move the triggering player to (17, 24, 225).

(0:1) Whenever someone moves,
 (1:35) and they move into position (22, 26),
    (5:19) move the triggering player to (15, 7, 227).

\* Doors */

(0:151) When someone tries to open a door at (22, 26),
 (1:80) and they can see position (22, 26),
    (5:200) open the door at (22, 26) on the map.

\* NPC Spawns */
(0:0) When everything is starting up,
    (5:220) spawn a new NPC 178 at (20, 36) on the map.
    (5:220) spawn a new NPC 178 at (28, 37) on the map.
    (5:220) spawn a new NPC 178 at (27, 27) on the map.
    (5:220) spawn a new NPC 177 at (17, 25) on the map.

