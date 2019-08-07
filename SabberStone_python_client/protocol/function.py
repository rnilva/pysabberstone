# functionc call protocol
from struct import *


def call_function(socket, mmf, function_id, *args):
    # Send function id as a byte
    socket.send(bytes([function_id]))
    for arg in args:
        if type(arg) is int:
            socket.send(pack('i', arg))
    return _retrieve_returned_value(socket, mmf)


def call_function(socket, mmf, function_id, int_arg: int):
    socket.send(bytes([function_id]))
    socket.send(pack('i', arg))
    return _retrieve_returned_value(socket, mmf)


def _retrieve_returned_value(socket, mmf):
    size = struct.unpack('I', socket.recv(4))[0]
    return mmf[0:size].tobytes()
