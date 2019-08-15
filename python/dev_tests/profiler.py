import pysabberstone.python.rpc.python_pb2
import random
import time


def random_games_rpc(stub, deck, count):
    game = stub.NewGame(python_pb2.DeckStrings(deck1=deck, deck2=deck))
    game_id = game.id
    for i in range(count):
        while (game.state != 3):
            options = stub.GetOptions(game_id)
            option = options.list[random.randrange(len(options.list))]
            game = stub.Process(option)
        game = stub.Reset(game_id)
        print("{0}th game is finished.".format(i + 1))


def random_games_mmf(server, deck, count):
    process_time = 0
    game = server.new_game(deck, deck)
    for i in range(count):
        while game.state != 3:
            options = server.options(game)
            option = options[random.randrange(len(options))]
            t = time.time()
            game = server.process(game, option)
            process_time += time.time() - t
        game = server.reset(game)
        print("{0}th game is finished.".format(i + 1))
    print("Process time: ", process_time)
