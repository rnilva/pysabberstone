from .entities import *


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
        self.hand_zone = HandZone(data_bytes[offset:])
        offset += self.hand_zone.size
        self.board_zone = BoardZone(data_bytes[offset:])
        offset += self.board_zone.size
        self.secret_zone = SecretZone(data_bytes[offset:])
        offset += self.secret_zone.size
        self.deck_zone = DeckZone(data_bytes[offset:])

        self.size = offset + self.deck_zone.size

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
                   None, self.hand_zone, self.board_zone, self.secret_zone, self.deck_zone.count)

