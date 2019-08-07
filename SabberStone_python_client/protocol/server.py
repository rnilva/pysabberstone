import socket
import numpy


class SabberStoneServer:

    SERVER_ADDRESS = '/tmp/CoreFxPip_sabberstoneserver'

    def __init__(self):
        self.socket = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
        self.mmf_fd = open('../../sabberstoneserver_mmf.mmf', 'r')
        self.mmf = numpy.memmap(f, dtype='byte', mode='r', shape=(1000))
        try:
            self.socket.connect(SabberStoneServer.SERVER_ADDRESS)
        except socket.error as msg:
            print(msg)
            sys.exit(1)
