# automatically generated source
# SabberStoneServer entities
from struct import *
import itertools

class Playable:
    fmt = 'iiii?'
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
        

class HandZone:

    def __init__(self, data_bytes):
        self.count = unpack('i', data_bytes[0:4])[0]
        print("count: ", self.count)
        self.Playables = []
        i = 4
        for _ in itertools.repeat(None, self.count):
            self.Playables.append(Playable(data_bytes[i:i + Playable.size]))
            i += Playable.size



