# automatically generated source
# SabberStoneServer entities
from struct import *


class Playable:
    fmt = 'iiii?'

    def __init__(self, data_bytes):
        fields = unpack(Playable.fmt, data_bytes)
        (
            self.card_id,
            self.cost,
            self.atk,
            self.base_health,
            self.ghostly
        ) = fields

