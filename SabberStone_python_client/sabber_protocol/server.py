import socket
import platform
import numpy
import sys
from sabber_protocol import function, entities


class SabberStoneServer:

    SERVER_ADDRESS = '/tmp/CoreFxPipe_sabberstoneserver'

    def __init__(self):
        self.socket = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
        self.mmf_fd = open('../sabberstoneserver.mmf', 'r')
        self.mmf = numpy.memmap(self.mmf_fd, dtype='byte', mode='r', shape=(1000))
        try:
            self.socket.connect(SabberStoneServer.SERVER_ADDRESS)
        except socket.error as msg:
            print(msg)
            sys.exit(1)
        print('Connected to SabberStoneServer')

    def _test_get_one_playable(self):
        data_bytes = function.call_function(self.socket, self.mmf, 2, 0)
        return entities.Playable(data_bytes)

    def _test_zone_with_playables(self):
        data_bytes = function.call_function(self.socket, self.mmf, 3, 0)
        return entities.HandZone(data_bytes)
