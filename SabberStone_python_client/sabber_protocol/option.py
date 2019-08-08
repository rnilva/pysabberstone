from struct import unpack
import itertools
from enum import Enum


class PlayerTaskType(Enum):
    CHOOSE = 1,
    CONCEDE = 2,
    END_TURN = 3,
    HERO_ATTACK = 4,
    HERO_POWER = 5,
    MINION_ATTACK = 6,
    PLAY_CARD = 7


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
        ) = fields

    def __str__(self):
        return "[{0}] {1} => {2}".format(self.type, self.source_position, self.target_position)


def get_options_list(data_bytes):
    count = data_bytes[0:4]
    print("Received %d options" % count)
    i = 4
    options = []
    for _ in itertools.repeat(None, count):
        options.append(Option(data_bytes[i:i + Option.size]))
        i += Option.size
    return options
