import random
import time
from multiprocessing import Process

import grpc
import pysabberstone
import pysabberstone.rpc.python_pb2_grpc as rpc
from pysabberstone.ai import abstract_ai
from pysabberstone.server import SabberStoneServer

account_name = "TestPythonRandomAI"
deck = r"AAEBAQcCrwSRvAIOHLACkQP/A44FqAXUBaQG7gbnB+8HgrACiLACub8CAA=="
matchserver_ip = "127.0.0.1"
matchserver_port = 50051


def _test_game():
    server = SabberStoneServer(id="test")
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
    print('game after reset:')
    print(game)

    # Options Test
    options = server.options(game)
    for option in options:
        print(option.print)

    del server

    def full_random_game(id, deck1, deck2, server=None):
        if server is None:
            server = pysabberstone.server.SabberStoneServer(id=id)

        game = server.new_game(deck1, deck2)
        start_game = time.time()
        while game.state != 3:
            options = server.options(game)
            option = options[random.randrange(len(options))]
            game = server.process(game, option)

        cp = game.current_player
        co = game.current_opponent
        winner_id = cp.id if cp.play_state == 4 else co.id
        end_game = time.time() - start_game
        print(f'id:{id}\tgame_time:{end_game:.2f}\tw:{winner_id}')

    def game_per_second(n_matches):
        server = pysabberstone.server.SabberStoneServer(id="test")
        avg_time = []
        start_test = time.time()
        for ep in range(n_matches):
            start_time = time.time()
            full_random_game("0", string1, string2, server=server)
            end_time = time.time() - start_time
            avg_time.append(end_time)
        avg_time = sum(avg_time) / len(avg_time)
        end_time = time.time() - start_test
        print(f"ep:{ep}\tavg_time:{avg_time:.2f}\tgps:{1/ avg_time:.2f}\ttotal_time:{end_time:.2f}")

    game_per_second( 100)

    print("Multithreading Test")
    processes = [
        Process(target=full_random_game, args=("thread1", string1, string2)),
        Process(target=full_random_game, args=("thread2", string1, string2)),
        Process(target=full_random_game, args=("thread3", string1, string2)),
        Process(target=full_random_game, args=("thread4", string1, string2))]

    for p in processes:
        p.start()

    for p in processes:
        p.join()


def run_remote_ai(pid, match_stub, game_stub):
    ai = abstract_ai.RandomAI(match_stub, account_name + pid)
    abstract_ai.remote_ai_match(ai, matchserver_ip, matchserver_port, account_name, deck, game_stub)


def run_match_server():
    channel = grpc.insecure_channel("localhost:50052")
    match_stub = rpc.MatchServiceStub(channel)
    game_stub = rpc.SabberStonePythonStub(channel)
    return match_stub, game_stub, channel


def _test_remote_match():
    match_stub, game_stub, _ = run_match_server()
    n_players = 2
    players = []
    for idx in range(n_players):
        p = Process(target=run_remote_ai, args=(idx, match_stub, game_stub))
        players.append(p)
    for p in players:
        p.start()
    for p in players:
        p.join()


def _test_mcts_match():
    match_stub, game_stub, channel = run_match_server()

    dotnet_ai_stub = rpc.DotnetAIServiceStub(channel)

    account_name = "TestPythonRandomAI"
    ai = abstract_ai.RandomAI(account_name, match_stub, )
    abstract_ai.dotnet_ai_match(python_ai=ai,
                                dotnet_ai_name="MonteCarloGraphSearch",
                                python_ai_deckstring=deck,
                                dotnet_ai_deckstring=deck,
                                sabber_stub=game_stub,
                                dotnet_ai_stub=dotnet_ai_stub,
                                history=True
                                )
    # server.get_server_status()


if __name__ == "__main__":
    _test_game()
    _test_mcts_match()
