import python_pb2
import python_pb2_grpc
from enum import Enum


class Game:
    """
    An object that represents the game state.
    This class encapsulates grpc Game object.
    Attributes:
        id (int): The id.

    """

    class State(Enum):
        """An enum class to represent Game's state."""
        INVALID = 0
        LOADING = 1
        RUNNING = 2
        COMPLETE = 3

    def __init__(self, grpc_game: python_pb2.Game):
        self._grpc_game = grpc_game
        self.id: int = grpc_game.id.value
        self.CurrentPlayer: Controller = grpc_game.CurrentPlayer

    def get_state(self):
        """Returns the current condition of this Game.
        See also Game.State
        """
        return self._grpc_game.state


class Controller:
    """
    An object that represents a player of Game.

    """

    def __init__(self, grpc_controller: python_pb2.Controller):
        pass
