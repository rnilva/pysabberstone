import sabber_protocol.server
import random
import cProfile
from multiprocessing import Process

server = sabber_protocol.server.SabberStoneServer(id="test")

server.get_server_status()

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
    print(option.print)


def full_random_game(server, deck1, deck2):
    # game = stub.NewGame(python_pb2.DeckStrings(deck1=deck1, deck2=deck2))
    game = server.new_game(deck1, deck2)
    while game.state != 3:
        options = server.options(game)
        option = options[random.randrange(len(options))]
        game = server.process(game, option)

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

print("New Thread Test")
thread1 = server.new_thread(thread_id=1)
thread2 = server.new_thread(thread_id=2)
thread3 = server.new_thread(thread_id=3)
thread4 = server.new_thread(thread_id=4)

server.get_server_status()

processes = [
    Process(target=full_random_game, args=(thread1, string1, string2)),
    Process(target=full_random_game, args=(thread2, string1, string2)),
    Process(target=full_random_game, args=(thread3, string1, string2)),
    Process(target=full_random_game, args=(thread4, string1, string2))]

for p in processes:
    p.start()

for p in processes:
    p.join()

server.get_server_status()
