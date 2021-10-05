from tkinter import *
from typing import Dict

master = Tk()

canvas_width = 1200
canvas_height = 600
w = Canvas(master, width=canvas_width, height=canvas_height)

tile_size = 40

active_player = 'A'
tile_colors = {' ': '#eee', 'o': '#333', 'O': '#383', '2': '#666', '1': '#999', 'A': '#0ee', 'B': '#e0e', 'x': '#e3e', 'F': '#00e',
  'X': '#e8e'}
tile_under_player = 'x'

class Pos:
  def __init__(self, x: int, y: int):
    self.x = x
    self.y = y
  def __hash__(self) -> int:
    return hash((self.x, self.y))
  def __eq__(self, other):
    if not isinstance(other, Pos):
      return False
    return self.x == other.x and self.y == other.y
  def __add__(self, other):
    return Pos(self.x + other.x, self.y + other.y)

tiles: Dict[Pos, str] = {}
won = False

def draw_tiles():
  w.delete("all")
  for pos, tile in tiles.items():
    color = tile_colors[tile]
    w.create_rectangle(pos.x * tile_size, pos.y * tile_size, pos.x * tile_size + tile_size, pos.y * tile_size + tile_size, fill=color)
  active_pos = find_pos(active_player)
  w.create_rectangle(active_pos.x * tile_size, active_pos.y * tile_size, active_pos.x * tile_size + tile_size, active_pos.y * tile_size + tile_size, outline='blue', width=5)


def find_pos(tile: str):
  return list(tiles.keys())[list(tiles.values()).index(tile)]


def can_shift_tiles(pos: Pos, direction: Pos):
  new_pos = pos + direction
  old_tile = None
  while old_tile != ' ':
    if old_tile is None:
      old_tile = tiles.get(new_pos, ' ')
    else:
      old_tile = tiles.get(new_pos, ' ')
    if old_tile in {'x', 'X'}:
      return False
    new_pos = new_pos + direction
  return True


def shift_tiles(pos: Pos, direction: Pos):
  assert can_shift_tiles(pos, direction)
  new_pos = pos + direction
  old_tile = None
  while old_tile != ' ':
    if old_tile is None:
      old_tile = tiles.get(new_pos, ' ')
      tiles[new_pos] = ' '
    else:
      before_tile = tiles.get(new_pos, ' ')
      tiles[new_pos] = {'o': '2', '2': '1', '1': '1'}.get(old_tile, old_tile)
      old_tile = before_tile
    new_pos = new_pos + direction

def simulate_step(action: chr):
  global active_player, won, level
  if won:
    won = False
    level = min(level + 1, len(tiles_initial_per_level) - 1)
    restart_level()
  # if action == '\t':
  #   active_player = 'A' if active_player == 'B' else 'B'
  #   draw_tiles()
  #   return
  if action == 'r':
    restart_level()
    return

  global tiles, tile_under_player
  anton_pos = find_pos(active_player)
  if action in {'w', 'a', 's', 'd'}:
    new_anton_pos = {'w': Pos(anton_pos.x, anton_pos.y - 1), 's': Pos(anton_pos.x, anton_pos.y + 1), 'a': Pos(anton_pos.x - 1, anton_pos.y), 'd': Pos(anton_pos.x + 1, anton_pos.y)}[action]
    old_tile = tiles.get(new_anton_pos, ' ')
    if old_tile in {' ', '_', 'A', 'B', 'X', 'O'}:
      return  # cannot move here
    if old_tile == 'F':
      won = True  # you win
    tiles[anton_pos] = tile_under_player
    tiles[new_anton_pos] = active_player
    tile_under_player = old_tile
  if action in {'Up', 'Left', 'Right', 'Down'}:
    dir = {'Up': Pos(0, -1), 'Down': Pos(0, 1), 'Left': Pos(-1, 0), 'Right': Pos(1, 0)}[action]
    if not can_shift_tiles(pos=anton_pos, direction=dir):
      return
    shift_tiles(pos=anton_pos, direction=dir)

  new_tiles: Dict[Pos, str] = tiles.copy()

  for pos in tiles:
    tile = tiles[pos]
    if tile in {' ', 'A', 'B', '_', 'o'}:
      continue
  tiles = new_tiles
  draw_tiles()

def on_key_press(event):
  simulate_step(event.keysym)

w.bind("<KeyPress>", on_key_press)
w.pack()
w.focus_set()


tiles_initial_per_level = [
"""
   oooxx
  ooAoooooooo
            o
        Foooo
""",
"""
  
  Aoo
  oo oooF

""",
"""

  oooo
  oA  ooF
  oo

""",
"""

   ooo  oF
 oooA
 oooo
   oo
""",
# "Ausfahrer"
"""
    F
        
  xo    
  xoo    
  xxA
""",
# "Doppelter Ausfahrer"
"""
 xxx  F
 xxo      
 xo  o    
 x  xoo    
 xxxxxA     
""",
# "Dreieck-Shift"
"""
    xxx     xF
    oooOooo xx
      
  x22   oox
  xxxxxAxxx
""",
# Steintor
"""
     F
      oxoo
     A xoo
     x xoo
 ooOOoOOoo
 xo  x x
 xxxxxxx
""",
# Tor Intro
"""
 xxoxxxA
 xoO
 xxOxxxF
""",
# die Pumpe
"""
  
  A X
xoxxX
xooXXxF
x oo
xxxxX
""",
# Laufsteg Intro
"""

  FooOooA
     ox
     ox
""",
# Laufsteg Intro 2
"""
    F Axo xx
    x   o ox
     ooooOox
        o  x
        xxxx
""",
# Hello 3
"""
 Axxoo ooxF
    ooo
""",
# Advanced Ausfahrer
"""
     xxx
     xoo
  xo    xF
 Axoo      
  xxx
"""
]

level = 13

def restart_level():
  global level, tile_under_player
  tiles.clear()
  for row, tile_row in enumerate(tiles_initial_per_level[level].splitlines(keepends=False)):
    for col, tile in enumerate(tile_row):
      tiles[Pos(col, row)] = tile
  max_x = max(pos.x for pos in tiles)
  max_y = max(pos.y for pos in tiles)
  for x in range(max_x + 2):
    for y in range(max_y + 2):
      if not Pos(x, y) in tiles:
        tiles[Pos(x, y)] = ' '
  tile_under_player = 'x'
  draw_tiles()

restart_level()

mainloop()

if __name__ == "main":
  breakpoint()
