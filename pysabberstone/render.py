import subprocess
import time
import signal
import os


def render():
    proc = subprocess.Popen(["./viz/SabberStoneUnityClient.x86_64"],
            shell=True, 
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE
            )
    try:
        stdout, stder  = proc.communicate()
    except KeyboardInterrupt:
        proc.terminate()

if __name__ == "__main__":
    render()
