import sabber_protocol.server


server = sabber_protocol.server.SabberStoneServer()

print("Send one playable test")
playable = server._test_get_one_playable()
print(playable)

print("Send hand test")
hand = server._test_zone_with_playables()
print(hand)

print("Send game test")
# https://www.hearthpwn.com/decks/1286667-hand-priest
string1 = r"AAECAa0GBKbwAr3zApeHA+aIAw3lBOEH9geNCKUJ0gryDPsM5fcC0P4C0okD64oDz50DAA=="
# https://www.hearthpwn.com/decks/1286917-starter-pack-mage
string2 = r"AAECAf0EAA8MuwKVA6sEtATmBJYFhQjC8wK0/ALnlQOmmAOfmwP/nQPinwMA"

game = server.new_game(string1, string2)
print('Received game object from NewGame! size: {}'.format(game.size))
print(game)

# Reset Test
server.reset(game)
print()
print('game after reset:')
print(game)

# Options Test
options = server.options(game)
for option in options:
    print(option)
