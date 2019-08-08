import socket
import platform
import numpy
import sys
from sabber_protocol import function, entities, game, option


class SabberStoneServer:

    SERVER_ADDRESS = '/tmp/CoreFxPipe_sabberstoneserver_'

    def __init__(self):
        self.socket = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
        self.mmf_fd = open('../_sabberstoneserver.mmf', 'r')
        self.mmf = numpy.memmap(self.mmf_fd, dtype='byte', mode='r', shape=(10000))
        try:
            self.socket.connect(SabberStoneServer.SERVER_ADDRESS)
        except socket.error as msg:
            print(msg)
            sys.exit(1)
        print('Connected to SabberStoneServer')
        print('mmf length: {0}', len(self.mmf))

    def new_game(self, deckstr1, deckstr2):
        data_bytes = function.call_function_multiargs(self.socket, self.mmf, 4, deckstr1, deckstr2)
        return game.Game(data_bytes)

    def reset(self, game):
        data_bytes = function.call_function(self.socket, self.mmf, 5, game.id)
        return game.reset_with_bytes(data_bytes)

    def options(self, game):
        data_bytes = function.call_function(self.socket, self.mmf, 6, game.id)
        return option.get_options_list(data_bytes)

    def _test_get_one_playable(self):
        data_bytes = function.call_function(self.socket, self.mmf, 2, 0)
        return entities.Playable(data_bytes)

    def _test_zone_with_playables(self):
        data_bytes = function.call_function(self.socket, self.mmf, 3, 0)
        return entities.HandZone(data_bytes)
