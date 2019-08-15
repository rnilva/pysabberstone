import mmap
import socket
import subprocess
import time

from ..sabber_protocol import entities
from ..sabber_protocol import function
from ..sabber_protocol.game import Game
from ..sabber_protocol import option

SERVER_ADDRESS = '/tmp/CoreFxPipe_sabberstoneserver_'
MMF_NAME_POSTFIX = '_sabberstoneserver.mmf'
DEFAULT_SERVER_PATH = 'sb_mm_env/SabberStone_gRPC/'
TIMEOUT = 50


class SabberStoneServer:

    def __init__(self, server_path=DEFAULT_SERVER_PATH, id: str = "", run_csharp_process=True):
        self.id = id
        self.socket = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
        mmf_path = '../' + id + MMF_NAME_POSTFIX
        uds_path = SERVER_ADDRESS + id
        if run_csharp_process:
            csharp_server = subprocess.Popen(["dotnet", "run",
                                              "-p", server_path,
                                              "-v", "m",
                                              "-c", "Release",
                                              "mmf", id],
                                             stdout=subprocess.DEVNULL)
            self.csharp_server = csharp_server
            self.is_thread = False
        else:
            self.is_thread = True

        timeout = time.time() + TIMEOUT
        for retry_nr in range(10000):
            time.sleep(1.0)
            try:
                self.socket.connect(uds_path)
                break
            except socket.error as e:
                if time.time() > timeout:
                    print(id, 'stopped retrying (uds timeout)', retry_nr)
                    raise e

        mmf_path = self.socket.recv(1024).decode()
        self.mmf_fd = open(mmf_path, 'r+')
        self.mmf_fd.write("0")
        self.mmf_fd.flush()
        self.mmf = mmap.mmap(self.mmf_fd.fileno(), 0)
        # self.mmf = numpy.memmap(self.mmf_fd, dtype='byte', mode='r+',
        #                         shape=(10000))

        print(id, 'Connected to SabberStoneServer ({0}), sleeping(2)'.format(mmf_path))
        time.sleep(2)

    def new_thread(self, thread_id: int):
        function.call_function_void_return_int_arg(self.socket, 9, thread_id)
        id = self.id + str(thread_id)
        print('Connecting to thread {0}......'.format(thread_id))
        return SabberStoneServer(id=id, run_csharp_process=False)

    def new_game(self, deckstr1, deckstr2):
        data_bytes = function.call_function_multiargs(self.socket, self.mmf, 4, deckstr1, deckstr2)
        return Game(data_bytes)

    def reset(self, game):
        data_bytes = function.call_function(self.socket, self.mmf, 5, game.id)
        return game.reset_with_bytes(data_bytes)

    def options(self, game):
        data_bytes = function.call_function(self.socket, self.mmf, 6, game.id)
        return option.get_options_list(data_bytes)

    def process(self, game, option):
        data_bytes = function.send_option(self.socket, self.mmf, game.id, option)
        return Game(data_bytes)

    def get_server_status(self):
        data_bytes = function.call_function_multiargs(self.socket, self.mmf, 10)
        print(data_bytes.decode())

    def _test_get_one_playable(self):
        data_bytes = function.call_function(self.socket, self.mmf, 2, 0)
        return entities.Playable(data_bytes)

    def _test_zone_with_playables(self):
        data_bytes = function.call_function(self.socket, self.mmf, 3, 0)
        return entities.HandZone(data_bytes)

    def __del__(self):
        function.call_function_void_return(self.socket, 8)
        if not self.is_thread:
            try:
                function.call_function_void_return(self.socket, 8)
            except:
                pass
        print('server {0} closed'.format(self.id))
