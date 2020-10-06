*/
* This is a MoonScript file containing triggers to handle various standard Endless Online actions.
* You can use this as a reference for your own scripts. You can even remove many of these standard functions.
*\

*/ Emote Handling */
(0:20) When someone uses any emote,
    (5:390) set variable %emoteId to the last emote the triggering player used.
 (1:201) and variable %emoteId is more than 0,
 (1:202) and variable %emoteId is less than 15,
    (5:150) make the triggering player use emote %emoteId.

*/ Item Drop & Picking Up */
(0:100) When someone attempts to drop any item,
    (5:347) set variable %id to the ID of the item the triggering player tried to drop.
    (5:348) set variable %amt to the amount of the item the triggering player tried to drop.
    (5:349) set variable %pos to the X,Y position of the item the triggering player tried to drop.
 (1:10) and they have at least %amt of %id in their inventory,
  (3:2) at position (%pos.x, %pos.y) on the map,
    (5:70) place %amt of item %id on the map.
    (5:76) remove %amt of item %id from the triggering player's inventory.

(0:103) When someone tries to pick up any item,
    (5:350) set variable %id to the ID of the item the triggering player tried to pick up.
    (5:351) set variable %amt to the amount of the item the triggering player tried to pick up.
    (5:352) set variable %pos to the X,Y position of the item the triggering player tried to pick up.
    (5:69) remove %amt of item %id at (%pos.x, %pos.y) on the map.
    (5:75) place %amt of item %id in the triggering player's inventory.

*/ Player attacking an NPC */
(0:16) When someone attacks,
  (3:10) in the space right in front of the triggering player,
   (4:20) only in places where there is an NPC,
    (5:386) set variable %npcIndex to the index of the NPC.
    (5:227) inflict 1 damage upon NPC %npcIndex on the map.

*/ Player talking */
(0:30) When someone says anything,
    (5:406) set message ~name to the triggering player's name.
    (5:405) set message ~msg to what the triggering player last said.
    (5:53) emit message {:from ~name ~msg} to every player who can see the triggering player.