# automatically generated source
# SabberStoneServer entities
from struct import *
import itertools


class Playable:
    fmt = '4i?'
    size = 17

    def __init__(self, data_bytes):
        fields = unpack(Playable.fmt, data_bytes)
        (
            self.card_id,
            self.cost,
            self.atk,
            self.base_health,
            self.ghostly
        ) = fields

    def __str__(self):
        return "{{CardId:{0}, Cost:{1}, ATK:{2}, HP:{3}}}".format(
                self.card_id, self.cost, self.atk, self.base_health)


class HeroPower:
    fmt = '2i?'
    size = 9

    def __init__(self, data_bytes):
        fields = unpack(HeroPower.fmt, data_bytes)
        (
            self.card_id,
            self.cost,
            self.exhausted
        ) = fields


class Weapon:
    fmt = '4i3?'
    size = 19

    def __init__(self, data_bytes):
        fields = unpack(HeroPower.fmt, data_bytes)
        (
            self.card_id,
            self.atk,
            self.durability,
            self.damage,
            self.windfury,
            self.lifesteal,
            self.immune
        ) = fields


class Hero:
    fmt_self = '6i3?'
    size_self = 27 + HeroPower.size

    def __init__(self, data_bytes):
        fields = unpack(Hero.fmt_self, data_bytes[0:27])
        (
            self.card_class,
            self.atk,
            self.base_health,
            self.damage,
            self.num_attacks_this_turn,
            self.armor,
            self.exhausted,
            self.stealth,
            self.immune
        ) = fields
        self.hero_power = HeroPower(data_bytes[27:27+HeroPower.size])

        if data_bytes[27+HeroPower.size] != 0:
            self.weapon = Weapon(data_bytes[27+HeroPower.size:27+HeroPower.size+Weapon.size])
            self.size = Hero.size_self + Weapon.size
        else:
            self.size = Hero.size_self + 4


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

    def __str__(self):
        ints = "{{CardId:{0}, ATK:{1}, HP:{2}, Exhausted{3}".format(
            self.card_id, self.atk, (self.base_health - self.atk),
            self.exhausted)
        bools = ""
        if self.stealth:
            bools += " STEALTH "
        if self.immune:
            bools += " IMMUNE "
        if self.windfury:
            bools += " WINDFURY "
        if self.lifesteal:
            bools += " LIFESTEAL "
        if self.taunt:
            bools += " TAUNT "
        if self.divine_shield:
            bools += " DIVINE_SHIELD "
        if self.elusive:
            bools += " ELUSIVE "
        if self.frozen:
            bools += " FROZEN "
        bools += "}"
        return ints + bools


class HandZone:

    def __init__(self, data_bytes):
        self.count = unpack('i', data_bytes[0:4])[0]
        self.Playables = []
        i = 4
        for _ in itertools.repeat(None, self.count):
            self.Playables.append(Playable(data_bytes[i:i + Playable.size]))
            i += Playable.size
        self.size = i

    def __str__(self):
        string = ""
        for playable in self.Playables:
            string += str(playable)
        return string


class BoardZone:

    def __init__(self, data_bytes):
        self.count = unpack('i', data_bytes[0:4])[0]
        self.Playables = []
        i = 4
        for _ in itertools.repeat(None, self.count):
            self.Playables.append(Minion(data_bytes[i:i + Minion.size]))
            i += Minion.size
        self.size = i

    def __str__(self):
        string = ""
        for playable in self.Playables:
            string += str(playable)
        return string


class SecretZone:

    def __init__(self, data_bytes):
        self.count = unpack('i', data_bytes[0:4])[0]
        self.Playables = []
        i = 4
        for _ in itertools.repeat(None, self.count):
            self.Playables.append(Playable(data_bytes[i:i + Playable.size]))
            i += Playable.size
        self.size = i

    def __str__(self):
        string = ""
        for playable in self.Playables:
            string += str(playable)
        return string


class DeckZone:

    def __init__(self, data_bytes):
        self.count = unpack('i', data_bytes[0:4])[0]
        self.Playables = []
        i = 4
        for _ in itertools.repeat(None, self.count):
            self.Playables.append(Playable(data_bytes[i:i + Playable.size]))
            i += Playable.size
        self.size = i
