import mmap
import socket
import subprocess
import time
import os

from .entities import *
from .function import *
from .option import *
from .game import Game

SERVER_ADDRESS = '/tmp/CoreFxPipe_sabberstoneserver_'
DEFAULT_DLL_PATH = os.path.join(os.path.dirname(__file__),
                                '_sabberstone_dotnet/SabberStonePython.dll')
TIMEOUT = 50


class SabberStoneServer:

    def __init__(self,
                 id: str = "",
                 dll_path=DEFAULT_DLL_PATH,
                 run_csharp_process=True):
        self.id = id
        self.socket = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
        uds_path = SERVER_ADDRESS + id
        if run_csharp_process:
            csharp_server = subprocess.Popen(["dotnet", dll_path, "mmf", id],
                                             stdout=subprocess.DEVNULL)
            self.csharp_server = csharp_server
            self.is_thread = False
        else:
            self.is_thread = True

        timeout = time.time() + TIMEOUT
        while True:
            try:
                self.socket.connect(uds_path)
                break
            except socket.error as e:
                if time.time() > timeout:
                    raise Exception('''Can\'t connect to the server.
                (uds timeout)''')
                pass

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
        call_function_void_return_int_arg(self.socket, 9, thread_id)
        id = self.id + str(thread_id)
        print('Connecting to thread {0}......'.format(thread_id))
        return SabberStoneServer(id=id, run_csharp_process=False)

    def new_game(self, deckstr1, deckstr2):
        data_bytes = call_function_multiargs(self.socket, self.mmf, 4,
                                             deckstr1, deckstr2)
        return Game(data_bytes)

    def reset(self, game):
        data_bytes = call_function(self.socket, self.mmf, 5, game.id)
        return game.reset_with_bytes(data_bytes)

    def options(self, game):
        data_bytes = call_function(self.socket, self.mmf, 6, game.id)
        return get_options_list(data_bytes)

    def process(self, game, option):
        data_bytes = send_option(self.socket, self.mmf, game.id, option)
        return Game(data_bytes)

    def get_server_status(self):
        data_bytes = call_function_multiargs(self.socket, self.mmf, 10)
        print(data_bytes.decode())

    def _test_get_one_playable(self):
        data_bytes = call_function(self.socket, self.mmf, 2, 0)
        return Playable(data_bytes)

    def _test_zone_with_playables(self):
        data_bytes = call_function(self.socket, self.mmf, 3, 0)
        return HandZone(data_bytes)

    def __del__(self):
        if not self.is_thread:
            try:
                call_function_void_return(self.socket, 8)
            except:
                pass
        print('server {0} closed'.format(self.id))
