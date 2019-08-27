import grpc
import python_pb2
import python_pb2_grpc
import random
import cProfile
import re
# from google.protobuf import empty_pb2

# python -m grpc_tools.protoc -I../../dotnet_core/Protos --python_out=. --grpc_python_out=. ../../dotnet_core/Protos/python.proto


def full_random_game(stub, deck1, deck2):
    game = stub.NewGame(python_pb2.DeckStrings(deck1=deck1, deck2=deck2))
    id = game.id
    options_list = []
    while game.state != 3:
        options = stub.GetOptions(id)
        for opt in options.list:
            print(opt.print)
        option = options.list[random.randrange(len(options.list))]
        game = stub.Process(option)
        print("Turn: {0}".format(game.turn))

    cp = game.CurrentPlayer
    co = game.CurrentOpponent
    if cp.play_state == python_pb2.Controller.PlayState.WON:
        print("Player{0} Wins!".format(cp.id))
    elif co.play_state == python_pb2.Controller.PlayState.WON:
        print("Player{0} Wins!".format(co.id))
    else:
        print("Tied!")

# run_server()
channel = grpc.insecure_channel('localhost:50052')
stub = python_pb2_grpc.SabberStonePythonStub(channel)

# Generate card dictionary
cards = stub.GetCardDictionary(python_pb2.Empty())


# https://www.hearthpwn.com/decks/1286667-hand-priest
string1 = r"AAECAa0GBKbwAr3zApeHA+aIAw3lBOEH9geNCKUJ0gryDPsM5fcC0P4C0okD64oDz50DAA=="
# https://www.hearthpwn.com/decks/1286917-starter-pack-mage
string2 = r"AAECAf0EAA8MuwKVA6sEtATmBJYFhQjC8wK0/ALnlQOmmAOfmwP/nQPinwMA"

game = stub.NewGame(python_pb2.DeckStrings(deck1=string1, deck2=string2))

# player1 = game.CurrentPlayer
# player2 = game.CurrentOpponent

# hand1 = player1.hand_zone.entities
# hand2 = player2.hand_zone.entities
# print("Player1 Hand Cards:")
# for card in hand1:
#     print('\t' + cards.cards[card.card_id].name)

# print("Player2 Hand Cards:")
# for card in hand2:
#     print('\t' + cards.cards[card.card_id].name)


options = stub.GetOptions(game.id)

for option in options.list:
    print(option.print)


# game = stub.Process(option_list[0])

full_random_game(stub, string1, string2)
# stats = cProfile.run('profiler.random_games(stub, r"AAEBAf0EAA8MLU1xwwG7ApUDrgO/A4AEtATmBO0EoAW5BgA=", 100)')

# profiler.random_games(stub, r"AAEBAf0EAA8MLU1xwwG7ApUDrgO/A4AEtATmBO0EoAW5BgA=", 100)
