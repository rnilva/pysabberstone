# automatically generated source
# SabberStoneServer entities
from struct import *


class Playable:
    fmt = 'iiii?i'
    size = 17

    def __init__(self, data_bytes):
        fields = unpack(Playable.fmt, data_bytes)
        (
            self.card_id,
            self.cost,
            self.atk,
            self.base_health,
            self.ghostly,
            self.size
        ) = fields


class HeroPower:
    fmt = 'ii?'
    size = 9

    def __init__(self, data_bytes):
        fields = unpack(HeroPower.fmt, data_bytes)
        (
            self.card_id,
            self.cost,
            self.exhausted
        ) = fields


class Weapon:
    fmt = 'iiii???'
    size = 19

    def __init__(self, data_bytes):
        fields = unpack(Weapon.fmt, data_bytes)
        (
            self.card_id,
            self.atk,
            self.durability,
            self.damage,
            self.windfury,
            self.lifesteal,
            self.immune
        ) = fields


class Minion:
    fmt = 'iiiiiii?????????????'
    size = 41

    def __init__(self, data_bytes):
        fields = unpack(Minion.fmt, data_bytes)
        (
            self.card_id,
            self.atk,
            self.base_health,
            self.damage,
            self.num_attacks_this_turn,
            self.zone_position,
            self.order_of_play,
            self.exhausted,
            self.stealth,
            self.immune,
            self.charge,
            self.attackable_by_rush,
            self.windfury,
            self.lifesteal,
            self.taunt,
            self.divine_shield,
            self.elusive,
            self.frozen,
            self.deathrattle,
            self.silenced
        ) = fields

