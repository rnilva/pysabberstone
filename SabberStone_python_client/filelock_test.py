import os
import platform


is_win = platform.system() == "Windows"


def file_lock():
    fp = "../test.lock"
    if is_win:
        try:
            if os.path.exists(fp):
                os.unlink(fp)
            fd = os.open(fp, os.O_CREAT | os.O_EXCL | os.O_RDWR)
        except:
            print("Cannot lock!")
            raise
    else:
        import fcntl
        import sys
        try:
            fd = open(fp, "w")
            fcntl.lockf(fd, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except IOError:
            print("Cannot lock!")
            sys.exit()

    print("Python client gets the exclusive lock.")
    return fd


def release_lock(fd):
    if is_win:
        os.close(fd)
    else:
        import fcntl
        fcntl.lockf(fd, fcntl.LOCK_UN)

    print("Python client releases the exclusive lock.")

    input("Process is still hanging on...")


