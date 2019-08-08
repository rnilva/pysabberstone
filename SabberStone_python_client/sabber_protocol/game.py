from collections import namedtuple
from struct import *
from sabber_protocol.controller import Controller


class Game:

    def __init__(self, data_bytes):
        self._id, self.state, self.turn = unpack('3i', data_bytes[0:12])
        offset = 12
        self.current_player = Controller(data_bytes[offset:])
        offset += self.current_player.size
        self.current_opponent = Controller(data_bytes[offset:])
