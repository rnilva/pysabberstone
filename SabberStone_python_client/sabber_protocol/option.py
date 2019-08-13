from struct import unpack, pack
import itertools
from enum import Enum
import numpy as np


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
    types = [
        "CHOOSE",
        "CONCEDE",
        "END_TURN",
        "HERO_ATTACK",
        "HERO_POWER",
        "MINION_ATTACK",
        "PLAY_CARD"
    ]
    dtype = np.dtype([
        ('type', 'i'),
        ('source_position', 'i'),
        ('target_position', 'i'),
        ('sub_option', 'i'),
        ('choice', 'i')
    ], copy=True)
    size = 20

    def __init__(self, data_bytes):
        # self.data = data_bytes.tobytes()
        self.data = data_bytes
        fields = unpack(Option.fmt, data_bytes)
        self.type = PlayerTaskType(fields[0])  # Slow
        (
            self.source_position,
            self.target_position,
            self.sub_option,
            self.choice
        ) = fields[1:5]

    def __str__(self):
        return "[{0}] {1} => {2}".format(self.type, self.source_position,
                                         self.target_position)

    def __bytes__(self):
        # return pack(Option.fmt, self.type.value, self.source_position,
        #             self.target_position, self.sub_option, self.choice)
        return self.data


def get_options_list(data_bytes):
    # return [Option(data_bytes[i:i+Option.size]) for i in range(0, len(data_bytes), Option.size)]
    return np.frombuffer(data_bytes, Option.dtype)

def print_options(options):
    for option in options:
        print("[{0}] {1} => {2}".format(Option.types[option[0]],
                                        option[1], option[2]))
