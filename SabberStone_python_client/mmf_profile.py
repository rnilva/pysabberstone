import sabber_protocol.server
import profiler
import cProfile
import time
from multiprocessing import Process

server = sabber_protocol.server.SabberStoneServer()
deck = r"AAEBAf0EAA8MLU1xwwG7ApUDrgO/A4AEtATmBO0EoAW5BgA="
init = server.new_game(deck, deck)
count = 500
print("Run %d random games..." % count)
stats = cProfile.run('profiler.random_games_mmf(server, r"AAEBAf0EAA8MLU1xwwG7ApUDrgO/A4AEtATmBO0EoAW5BgA=", ' + str(count) + ')')
# pr = cProfile.Profile()

# threads = [
#     server,
#     server.new_thread(1),
#     server.new_thread(2),
#     server.new_thread(3),
#     server.new_thread(4),
#     server.new_thread(5),
#     server.new_thread(6),
#     server.new_thread(7),
# ]
# processes = [Process(target=profiler.random_games_mmf(t, r"AAEBAf0EAA8MLU1xwwG7ApUDrgO/A4AEtATmBO0EoAW5BgA=", 125)) for t in threads]

# pr.enable()

# for p in processes:
#     p.start()
# for p in processes:
#     p.join()

# pr.disable()

# pr.print_stats(sort='time')
