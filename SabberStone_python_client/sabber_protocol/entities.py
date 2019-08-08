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


class HeroPower:
    fmt = '2i?'
    size = 9

    def __init__(self, data_bytes):
        print("Init HeroPower: %d bytes" % len(data_bytes))
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
        print("Init Weapon: %d bytes" % len(data_bytes))
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
        print("Init Hero: %d bytes" % len(data_bytes))
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


class HandZone:

    def __init__(self, data_bytes):
        self.count = unpack('i', data_bytes[0:4])[0]
        print("Init HandZone. Count: %d" % self.count)
        self.Playables = []
        i = 4
        for _ in itertools.repeat(None, self.count):
            self.Playables.append(Playable(data_bytes[i:i + Playable.size]))
            i += Playable.size
        self.size = i


class BoardZone:

    def __init__(self, data_bytes):
        self.count = unpack('i', data_bytes[0:4])[0]
        print("Init BoardZone. Count: %d" % self.count)
        self.Playables = []
        i = 4
        for _ in itertools.repeat(None, self.count):
            self.Playables.append(Minion(data_bytes[i:i + Minion.size]))
            i += Minion.size
        self.size = i


class SecretZone:

    def __init__(self, data_bytes):
        self.count = unpack('i', data_bytes[0:4])[0]
        print("Init SecretZone. Count: %d" % self.count)
        self.Playables = []
        i = 4
        for _ in itertools.repeat(None, self.count):
            print(len(data_bytes[i:i + Playable.size]))
            self.Playables.append(Playable(data_bytes[i:i + Playable.size]))
            i += Playable.size
        self.size = i


class DeckZone:

    def __init__(self, data_bytes):
        self.count = unpack('i', data_bytes[0:4])[0]
        self.Playables = []
        i = 4
        print("Init DeckZone. Count: %d" % self.count)
        print("Bytes: %d" % len(data_bytes[i:]))
        for _ in itertools.repeat(None, self.count):
            self.Playables.append(Playable(data_bytes[i:i + Playable.size]))
            i += Playable.size
        self.size = i
