from ..rpc.python_pb2 import MatchRequest, QueueRequest, Empty
from ..rpc.python_pb2 import DeckStrings, Game, Option
from ..rpc.python_pb2 import DotnetAIRequest, DotnetAIResponse
from ..rpc.python_pb2_grpc import SabberStonePythonStub, grpc
from ..rpc.python_pb2_grpc import DotnetAIServiceStub
from abc import ABC, abstractmethod
import random


class AbstractAI(ABC):
    def __init__(self, stub, account_name):
        self._stub = stub
        self.metadata = (('id', account_name),)
        self.game_id = ""
        self.response_stream = None
        self.request_stream = None

    @abstractmethod
    def get_option(self, game: Game,
                   sabber_stub: SabberStonePythonStub) -> Option:
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
    def on_game_finished(self, game):
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
                    sabber_stub: SabberStonePythonStub,
                    dotnet_ai_stub: DotnetAIServiceStub,
                    count: int = 1):
    """Run matches between python AI agent and dotnet AI agent."""
    python_ai.on_match_started()
    for i in range(count):
        python_ai.on_game_started()
        # Request: Initialise dotnet AI agent with a specified name and
        #          get new established game.
        #          If dotnet AI is the first player of the game, it will
        #          play its turn.
        #          The game used in matches is partially observable, i.e.,
        #          the opponent's hand and deck is filled with 'Unknown' cards.
        response = dotnet_ai_stub.Request(
            DotnetAIRequest(dotnet_ai_name=dotnet_ai_name,
                            dotnet_ai_deckstring=dotnet_ai_deckstring,
                            python_ai_deckstring=python_ai_deckstring))
        if response.Type == 1:  # NOT_FOUND
            print("dotnet agent with name {} is not found.".format(
                dotnet_ai_name))
            return
        elif response.Type == 2:  # OCCUPIED
            print("Only one match can be run.")
            return
        elif response.Type == 3:  # INVALID_DECKSTRING
            print("Invalid deckstring.")
            return

        game = response.game

        # Main loop
        while game.state != 3:
            # Get option from the python ai
            option = python_ai.get_option(game, sabber_stub)

            # Send Option: If the given option is end turn option,
            #              python client receives the start of the next
            #              turn after the dotnet client plays its turn.
            #              Therefore 'game' is always python client's turn.
            game = dotnet_ai_stub.SendPythonAIOption(option)

        # TODO: winner

        python_ai.on_game_finished(game)


def match(ai_player_1: AbstractAI, ai_player_2: AbstractAI,
          deckstring_1, deckstring_2, sabber_stub: SabberStonePythonStub,
          count: int = 100):
    """Run matches between two python AI agents."""
    ai_player_1.on_match_started()
    ai_player_2.on_match_started()
    for i in range(count):
        game = sabber_stub.NewGame(DeckStrings(deck1=deckstring_1,
                                               deck2=deckstring_2))
        ai_player_1.on_game_started()
        ai_player_2.on_game_started()
        # id = game.id
        while game.state != 3:   # State.COMPLETE
            if game.CurrentPlayer.id == 1:
                option = ai_player_1.get_option(game, sabber_stub)
            else:
                option = ai_player_2.get_option(game, sabber_stub)
            game = sabber_stub.Process(option)
        ai_player_1.on_game_finished()
        ai_player_2.on_game_finished()


class RandomAI(AbstractAI):
    """The most basic AI in the world."""
    def get_option(self, game, sabberstone_stub):
        sabberstone_stub.GetOptions(game.id)
        options = sabberstone_stub.GetOptions(game.id)
        return options.list[random.randrange(len(options.list))]

    def on_match_started(self):
        pass

    def on_game_started(self):
        pass

    def on_game_finished(self, game):
        pass
