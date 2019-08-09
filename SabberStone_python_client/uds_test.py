# Testing scripts for Unix Domain Socket Communication
import socket
import sys
import os
import struct
import time
import ctypes
from mmf_test import open_mmf

server_address = '/tmp/CoreFxPipe_testpipe'

# # Make sure the socket does not already exist
# try:
#     os.unlink(server_address)
# except OSError:
#     if os.path.exists(server_address):
#         raise

# Creates a UDS socket
sock = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)


def call_function(socket, function_id, arg):
    socket.send(bytes([function_id]))
    socket.send(struct.pack('i', arg))


class TestStructure:
    fmt = 'ii200s200s'

    def __init__(self, data_bytes):
        fields = struct.unpack(self.fmt, data_bytes)
        (
            self.data1,
            self.data2,
            self.data3,
            self.data4
        ) = fields
        self.data3 = self.data3[:self.data3.index(b'\0')]
        self.data4 = self.data4[:self.data4.index(b'\0')]


# Open MMF
f, mmf = open_mmf()

# Connect the socket to the port where the server is listening
print('connecting to {}'.format(server_address))
try:
    sock.connect(server_address)
except socket.error as msg:
    print(msg)
    sys.exit(1)
print('connected!')


try:
    i = 1
    while True:
        # amount_expected = struct.unpack('I', sock.recv(4))[0]
        # print("amount_expected :", amount_expected)

        # message = sock.recv(amount_expected)
        # print("Received message : ", message.decode())

        # message_rev = message[::-1].decode()
        # print("Sent message : ", message_rev)

        # sock.sendall(struct.pack('I', len(message_rev))
        #     + message_rev.encode('utf-8'))

        call_function(sock, i, 999)
        print('Call function {0} with argument 999'.format(i))

        if struct.unpack('i', sock.recv(4))[0] == -1:
            print('Successfully returned from the function call!')
            print('Let\'s read the returned structure now!')

        size = struct.unpack('I', sock.recv(4))[0]

        print('Received a struct of size ', size)

        data_from_mmf = mmf[0][0:size].tobytes()
        print('Reading bytes in the mmf......')

        struct_received = TestStructure(data_from_mmf)
        print('data1', struct_received.data1)
        print('data2', struct_received.data2)
        print('data3', struct_received.data3)
        print('data4', struct_received.data4)

        time.sleep(5)
        i += 1

finally:
    print('closing socket')
    sock.close()
