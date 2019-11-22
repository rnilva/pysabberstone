import grpc
from abstract_ai import RandomAI, dotnet_ai_match
import python_pb2_grpc as rpc
from python_pb2 import Empty
import random


channel = grpc.insecure_channel("localhost:50052")
stub = rpc.MatchServiceStub(channel)
game_stub = rpc.SabberStonePythonStub(channel)

account_name = "TestPythonRandomAI"
deck = r"AAEBAQcCrwSRvAIOHLACkQP/A44FqAXUBaQG7gbnB+8HgrACiLACub8CAA=="
matchserver_ip = "127.0.0.1"
matchserver_port = 50051
ai = abstract_ai.RandomAI(stub, account_name)

# abstract_ai.start_remote_ai_match(ai, matchserver_ip, matchserver_port, 
# 				  account_name, deck, game_stub)

