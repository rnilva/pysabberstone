from enum import IntEnum
from struct import unpack, pack
import numpy as np

# class PlayerTaskType(IntEnum):
#     CHOOSE = 0
#     CONCEDE = 1
#     END_TURN = 2
#     HERO_ATTACK = 3
#     HERO_POWER = 4
#     MINION_ATTACK = 5
#     PLAY_CARD = 6

class PlayerTaskType:
    (CHOOSE, CONCEDE, END_TURN, HERO_ATTACK, 
     HERO_POWER, MINION_ATTACK, PLAY_CARD) = range(7)


# class Option:
#     fmt = '5i?'
#     size_self = 21

#     def __init__(self, data_bytes):
#         fields = unpack(Option.fmt, data_bytes[0:Option.size_self])
#         self.type = PlayerTaskType(fields[0])
#         (
#             self.source_position,
#             self.target_position,
#             self.sub_option,
#             self.choice,
#             self.is_spell
#         ) = fields[1:6]
#         # length = unpack('i', data_bytes[Option.size_self:Option.size_self + 4])[0]
#         # self.size = Option.size_self + 4 + length
#         # self.print = data_bytes[Option.size_self + 4:self.size].decode()

#     def __str__(self):
#         return "[{0}] {1} => {2}".format(self.type, self.source_position,
#                                          self.target_position)

#     def __bytes__(self):
#         return pack(Option.fmt, self.type.value, self.source_position,
#                     self.target_position, self.sub_option, self.choice,
#                     self.is_spell)

Option = np.dtype([('type', np.int32),
                   ('source_position', np.int32),
                   ('target_position', np.int32),
                   ('sub_option', np.int32),
                   ('choice', np.int32),
                   ('is_spell', np.bool_)
                  ])


def get_options_list(data_bytes):
    # options = []
    # offset = 0
    # while offset < len(data_bytes):
    #     option = Option(data_bytes[offset:])
    #     options.append(option)
    #     offset += option.size
# )    num_options = len(data_bytes) // Option.itemsize
#     options = np.ndarray(num_options, dtype=Option, 
    # print(len(data_bytes))
    options = np.frombuffer(data_bytes, Option)
    return options


class FastPlayerTaskType:
    (CHOOSE, CONCEDE, END_TURN, HERO_ATTACK, 
     HERO_POWER, MINION_ATTACK, PLAY_CARD) = range(7)


from ctypes import Structure, c_int, c_bool

class COption(Structure):
    _fields_ = [("type", c_int),
                ("source_position", c_int),
                ("target_position", c_int),
                ("sub_option", c_int),
                ("choice", c_int),
                ("is_spell", c_bool)]


# def get_coptions_list(data_bytes: bytearray):
    