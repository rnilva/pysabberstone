from ..rpc.python_pb2 import MatchRequest, QueueRequest, Empty
from ..rpc.python_pb2 import DeckStrings, Game, Option
from ..rpc.python_pb2_grpc import SabberStonePythonStub, , grpc
from abc import ABC
import random


class AbstractAI(ABC):
    def __init__(self, stub, account_name):
        self._stub = stub
        self.metadata = (('id', account_name),)
        self.game_id = ""
        self.response_stream = None
        self.request_stream = None

    @abstractmethod
    def get_option(self, game: Game, sabber_stub) -> Option:
        """Calculate and reutnr the best move
        with regard to the given game state.
        Each AI agent must implement this method.
        """
        pass

    @abstractmethod
    def on_match_started(self):
        """This method will be called when a match is started."""
        pass

    @abstractmethod
    def on_game_started(self):
        """This method will be called when each game(round) is started."""
        pass

    @abstractmethod
    def on_game_finished(self):
        """This method will be called when each game(round) is finished."""
        pass

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


def remote_ai_match(ai: AbstractAI, ip, port,
                    id, deckstring, sabberstone_stub):
    """Connect to a specified remote server with AI
    using the given deckstring.
    """
    ai.connect(ip, port)
    ai.queue(deckstring)
    stub = ai._stub
    game = stub.GetState(Empty(), metadata=ai.metadata)
    while game.state != 3:
        stub.SendOption(ai.get_option(sabberstone_stub, game),
                        metadata=ai.metadata)
        game = stub.GetState(Empty(), metadata=ai.metadata)
    print("Game Finished.")


def dotnet_ai_match(python_ai: AbstractAI, dotnet_ai_name: str,
                    python_ai_deckstring, dotnet_ai_deckstring,
                    sabberstone_stub):
    """Run matches between python AI agent and dotnet AI agent."""
    pass


def match(ai_player_1: AbstractAI, ai_player_2: AbstractAI,
          deckstring_1, deckstring_2, sabber_stub: SabberStonePythonStub,
          count: int = 100):
    """Run matches between two python AI agents."""
    on_match_started(ai_player_1)
    on_match_started(ai_player_2)
    for i in range(count):
        game = sabber_stub.NewGame(DeckStrings(deck1=deckstring_1,
                                               deck2=deckstring_2))
        on_game_started(ai_player_1)
        on_game_started(ai_player_2)
        id = game.id
        while game.state != 3:   # State.COMPLETE
            if game.CurrentPlayer.id == 1:
                option = ai_player_1.get_option(game, sabber_stub)
            else:
                option = ai_player_2.get_option(game, sabber_stub)
            game = sabber_stub.Process(option)
        on_game_finished(ai_player_1)
        on_game_finished(ai_player_2)


class RandomAI(AbstractAI):
    """The most basic AI in the world."""
    def get_option(self, sabberstone_stub, game):
        options = sabberstone_stub.GetOptions(game.id)
        return options.list[random.randrange(len(options.list))]

    def on_match_started(self):
        pass

    def on_game_started(self, game):
        pass

    def on_game_finished(self):
        pass
