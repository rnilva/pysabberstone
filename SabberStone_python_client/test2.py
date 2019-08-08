import sabber_protocol.server


server = sabber_protocol.server.SabberStoneServer()

playable = server._test_get_one_playable()

print(playable.card_id)
print(playable.cost)
print(playable.atk)
print(playable.base_health)
print(playable.ghostly)

hand = server._test_zone_with_playables()

for playable in hand.Playables:
    print(playable.card_id)
    print(playable.cost)
    print(playable.atk)
    print(playable.base_health)
    print(playable.ghostly)

hand = server._test_zone_with_playables()
hand = server._test_zone_with_playables()
hand = server._test_zone_with_playables()
hand = server._test_zone_with_playables()
hand = server._test_zone_with_playables()

# https://www.hearthpwn.com/decks/1286667-hand-priest
string1 = r"AAECAa0GBKbwAr3zApeHA+aIAw3lBOEH9geNCKUJ0gryDPsM5fcC0P4C0okD64oDz50DAA=="
# https://www.hearthpwn.com/decks/1286917-starter-pack-mage
string2 = r"AAECAf0EAA8MuwKVA6sEtATmBJYFhQjC8wK0/ALnlQOmmAOfmwP/nQPinwMA"

game = server.new_game(string1, string2)
print('Received game object from NewGame! size: {}'.format(game.size))