import profiler
import cProfile
import python_pb2_grpc
import grpc


channel = grpc.insecure_channel('localhost:50052')
stub = python_pb2_grpc.SabberStonePythonStub(channel)
stats = cProfile.run('profiler.random_games_rpc(stub, r"AAEBAf0EAA8MLU1xwwG7ApUDrgO/A4AEtATmBO0EoAW5BgA=", 100)')