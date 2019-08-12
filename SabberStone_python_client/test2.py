import sabber_protocol.server
import random
import cProfile

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


def full_random_game(server, deck1, deck2):
    # game = stub.NewGame(python_pb2.DeckStrings(deck1=deck1, deck2=deck2))
    game = server.new_game(deck1, deck2)
    while game.state != 3:
        options = server.options(game)
        option = options[random.randrange(len(options))]
        game = server.process(game, option)
        print("Turn: {0}".format(game.turn))

    cp = game.current_player
    co = game.current_opponent
    if cp.play_state == 4:
        print("Player{0} Wins!".format(cp.id))
    elif co.play_state == 4:
        print("Player{0} Wins!".format(co.id))
    else:
        print("Tied!")

print("Full Random Game Test")
full_random_game(server, string1, string2)
