from struct import unpack, pack
import itertools
from enum import Enum


class PlayerTaskType(Enum):
    CHOOSE = 0
    CONCEDE = 1
    END_TURN = 2
    HERO_ATTACK = 3
    HERO_POWER = 4
    MINION_ATTACK = 5
    PLAY_CARD = 6


class Option:
    fmt = '5i'
    size = 20

    def __init__(self, data_bytes):
        fields = unpack(Option.fmt, data_bytes)
        self.type = PlayerTaskType(fields[0])
        (
            self.source_position,
            self.target_position,
            self.sub_option,
            self.choice
        ) = fields[1:5]

    def __str__(self):
        return "[{0}] {1} => {2}".format(self.type, self.source_position, self.target_position)

    def __bytes__(self):
        return pack(Option.fmt, self.type.value, self.source_position, self.target_position, self.sub_option, self.choice)


def get_options_list(data_bytes):
    options = []
    for i in range(0, len(data_bytes), Option.size):
        options.append(Option(data_bytes[i:i+Option.size]))
    
    return options
