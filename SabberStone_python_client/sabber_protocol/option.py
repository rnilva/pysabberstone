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
    size_self = 20

    def __init__(self, data_bytes):
        fields = unpack(Option.fmt, data_bytes[0:Option.size_self])
        self.type = PlayerTaskType(fields[0])
        (
            self.source_position,
            self.target_position,
            self.sub_option,
            self.choice
        ) = fields[1:5]
        length = unpack('i', data_bytes[Option.size_self:Option.size_self+4])[0]
        self.size = Option.size_self + 4 + length
        self.print = data_bytes[Option.size_self+4:self.size].decode()

    def __str__(self):
        return "[{0}] {1} => {2}".format(self.type, self.source_position,
                                         self.target_position)

    def __bytes__(self):
        return pack(Option.fmt, self.type.value, self.source_position,
                    self.target_position, self.sub_option, self.choice)


def get_options_list(data_bytes):
    options = []
    offset = 0
    while offset < len(data_bytes):
        option = Option(data_bytes[offset:])
        options.append(option)
        offset += option.size
    return options
