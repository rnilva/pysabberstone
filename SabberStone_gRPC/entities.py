# automatically generated source
# SabberStoneServer entities
from struct import *
import numpy as np


class Playable:
    dtype = np.dtype([
        ('card_id', 'i'),
        ('cost', 'i'),
        ('atk', 'i'),
        ('base_health', 'i'),
        ('ghostly', 'b')
    ])
    size = dtype.itemsize

class HeroPower:
    dtype = np.dtype([
        ('card_id', 'i'),
        ('cost', 'i'),
        ('exhausted', 'b')
    ])
    size = dtype.itemsize

class Weapon:
    dtype = np.dtype([
        ('card_id', 'i'),
        ('atk', 'i'),
        ('durability', 'i'),
        ('damage', 'i'),
        ('windfury', 'b'),
        ('lifesteal', 'b'),
        ('immune', 'b')
    ])
    size = dtype.itemsize

class Minion:
    dtype = np.dtype([
        ('card_id', 'i'),
        ('atk', 'i'),
        ('base_health', 'i'),
        ('damage', 'i'),
        ('num_attacks_this_turn', 'i'),
        ('zone_position', 'i'),
        ('order_of_play', 'i'),
        ('exhausted', 'b'),
        ('stealth', 'b'),
        ('immune', 'b'),
        ('charge', 'b'),
        ('attackable_by_rush', 'b'),
        ('windfury', 'b'),
        ('lifesteal', 'b'),
        ('taunt', 'b'),
        ('divine_shield', 'b'),
        ('elusive', 'b'),
        ('frozen', 'b'),
        ('deathrattle', 'b'),
        ('silenced', 'b')
    ])
    size = dtype.itemsize

class Option:
    dtype = np.dtype([
        ('type', 'i'),
        ('source_position', 'i'),
        ('target_position', 'i'),
        ('sub_option', 'i'),
        ('choice', 'i')
    ])
    size = dtype.itemsize
