# function call protocol
from struct import *
import numpy as np


def call_function_multiargs(socket, mmf, function_id, *args):
    # Send function id as a byte
    byte = bytes([function_id])
    for arg in args:
        if type(arg) is int:
            byte += b'i' + pack('i', arg)
        elif type(arg) is str:
            byte += b's' + pack('i', len(arg)) + arg.encode()
    socket.send(byte + b'4')
    return _retrieve_returned_value(socket, mmf)


def call_function(socket, mmf, function_id, int_arg: int):
    socket.send(bytes([function_id]) + b'i' + pack('i', int_arg) + b'4')
    return _retrieve_returned_value(socket, mmf)


def call_function_void_return(socket, function_id):
    socket.send(bytes([function_id]))


def call_function_void_return_int_arg(socket, function_id, int_arg: int):
    socket.send(bytes([function_id]) + pack('i', int_arg))
    eot = socket.recv(1)


def send_option(socket, mmf, game_id, option):
    socket.send(b'\x07i' + pack('i', game_id) + b'o' + bytes(option) + b'4')
    return _retrieve_returned_value(socket, mmf)


def _retrieve_returned_value(socket, mmf):
    size = unpack('I', socket.recv(4))[0]
    return mmf[0:size]
    # return memoryview(mmf)[0:size]


def _encode_argument(arg):
    pass
