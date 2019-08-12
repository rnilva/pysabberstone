# function call protocol
from struct import *


def call_function_multiargs(socket, mmf, function_id, *args):
    # Send function id as a byte
    socket.send(bytes([function_id]))
    for arg in args:
        if type(arg) is int:
            socket.send(b'i')
            socket.send(pack('i', arg))
        elif type(arg) is str:
            socket.send(b's')
            socket.send(pack('i', len(arg)))
            socket.send(arg.encode())
    socket.send(b'4')
    return _retrieve_returned_value(socket, mmf)


def call_function(socket, mmf, function_id, int_arg: int):
    socket.send(bytes([function_id]))
    # Send 1 int argument.
    socket.send(b'i')
    socket.send(pack('i', int_arg))
    socket.send(b'4')
    return _retrieve_returned_value(socket, mmf)


def call_function_void_return(socket, function_id):
    socket.send(bytes([function_id]) + b'4')


def send_option(socket, mmf, game_id, option):
    socket.send(bytes([7]))
    socket.send(b'i')
    socket.send(pack('i', game_id))
    socket.send(b'o')
    socket.send(bytes(option))
    socket.send(b'4')
    return _retrieve_returned_value(socket, mmf)


def _retrieve_returned_value(socket, mmf):
    size = unpack('I', socket.recv(4))[0]
    return mmf[0:size].tobytes()


def _encode_argument(arg):
    pass
