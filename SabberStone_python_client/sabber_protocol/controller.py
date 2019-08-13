from struct import *
from sabber_protocol.entities import *
import numpy as np


def unpack_zone(data_bytes, dtype, offset):
    count = unpack('i', data_bytes[0:4])[0]
    # print("unpacking {0} {1}s... offset becomes {2}".format(count, dtype, offset + dtype.itemsize * count + 4))
    return np.frombuffer(data_bytes, dtype, count, 4), offset + dtype.itemsize * count + 4


class Controller:
    def __init__(self, data_bytes):
        fields = unpack('6i', data_bytes[0:24])
        (
            self.id,
            self.play_state,
            self.base_mana,
            self.remaining_mana,
            self.overload_locked,
            self.overload_owed,
        ) = fields

        offset = 24
        self.hero = Hero(data_bytes[offset:])
        offset += self.hero.size
        # self.hand_zone = HandZone(data_bytes[offset:])
        # offset += self.hand_zone.size
        # self.board_zone = BoardZone(data_bytes[offset:])
        # offset += self.board_zone.size
        # self.secret_zone = SecretZone(data_bytes[offset:])
        # offset += self.secret_zone.size
        # self.deck_zone = DeckZone(data_bytes[offset:])
        self.hand_zone, offset = unpack_zone(data_bytes[offset:], Playable.dtype, offset)
        self.board_zone, offset = unpack_zone(data_bytes[offset:], Minion.dtype, offset)
        self.secret_zone, offset = unpack_zone(data_bytes[offset:], Playable.dtype, offset)
        self.deck_zone, offset = unpack_zone(data_bytes[offset:], Playable.dtype, offset)
        # self.size = offset + self.deck_zone.size
        self.size = offset

    def __str__(self):
        return """
        PlayerId: {0}, PlayState: {1},
        BaseMana: {2}, RemainingMana: {3}, OverloadLocked: {4}, OverloadOwed: {5}
        Hero: {6}
        Hand: {7}
        Board: {8}
        Secret: {9}
        Deck Count: {10}
        """.format(self.id, self.play_state,
                   self.base_mana, self.remaining_mana, self.overload_locked, self.overload_owed,
                   None, self.hand_zone, self.board_zone, self.secret_zone, len(self.deck_zone))



