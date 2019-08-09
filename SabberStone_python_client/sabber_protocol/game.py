from collections import namedtuple
from struct import *
from sabber_protocol.controller import Controller


class Game:

    def __init__(self, data_bytes):
        self.id, self.state, self.turn = unpack('3i', data_bytes[0:12])
        offset = 12
        self.current_player = Controller(data_bytes[offset:])
        offset += self.current_player.size
        self.current_opponent = Controller(data_bytes[offset:])
        self.size = offset + self.current_opponent.size

    def __str__(self):
        return """\
        Id: {0}
        State: {1}, Turn: {2}
        Current Player: {3}
        Current Opponent: {4}
        """.format(self.id, self.state, self.turn,
                   self.current_player, self.current_opponent)

    def reset_with_bytes(self, data_bytes):
        self.id, self.state, self.turn = unpack('3i', data_bytes[0:12])
        self.current_player.reset_with_bytes(data_bytes[12:])
        self.current_opponent.reset_with_bytes(
            data_bytes[12 + self.current_player.size:])
        return self
