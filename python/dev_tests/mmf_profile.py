import pysabberstone.python.server
import profiler
import cProfile

server = pysabberstone.python.server.SabberStoneServer()

stats = cProfile.run('profiler.random_games_mmf(server, r"AAEBAf0EAA8MLU1xwwG7ApUDrgO/A4AEtATmBO0EoAW5BgA=", 500)')
