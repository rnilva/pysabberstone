import numpy as np
from filelock_test import file_lock, release_lock


def open_mmf():
    f = open('../test.mmf', 'r')
    mmf = np.memmap(f, dtype='byte', mode='r', shape=(1, 1000))
    return f, mmf

# lock = file_lock()

# mmf[1] = 0

# release_lock(lock)
# import mmap

# f = open('../test.mmf', 'r+')
# mmf = mmap.mmap(f.fileno(), length=0, tagname="testmmf")

# mmf.read()
