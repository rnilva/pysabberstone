from python_pb2 import MatchRequest, QueueRequest, Empty
from abc import ABC
import random


class AbstractAI(ABC):
    def __init__(self, stub, account_name):
        self._stub = stub
        self.metadata = (('id', account_name),)
        self.game_id = ""
        self.response_stream = None
        self.request_stream = None

    def connect(self, ip, port):
        request = MatchRequest(ip=ip,
                               port=port,
                               id=self.metadata[0][1],
                               password="")
        self._stub.Initialise(request)

    def queue(self, deckstring):
        request = QueueRequest(deckstring=deckstring,
                               id=self.metadata[0][1],
                               password="")
        self._stub.Queue(request)


def start_ai_match(ai, ip, port, id, deckstring, sabberstone_stub):
    ai.connect(ip, port)
    ai.queue(deckstring)
    stub = ai._stub;
    game = stub.GetState(Empty(), metadata=ai.metadata)
    while game.state != 3:
        stub.SendOption(ai.get_option(sabberstone_stub, game), metadata=ai.metadata)
        game = stub.GetState(Empty(), metadata=ai.metadata)
    print("Game Finished.")


class RandomAI(AbstractAI):
    def get_option(self, sabberstone_stub, game):
        options = sabberstone_stub.GetOptions(game.id)
        return options.list[random.randrange(len(options.list))]
