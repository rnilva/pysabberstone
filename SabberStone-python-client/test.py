import grpc
import python_pb2
import python_pb2_grpc
#from google.protobuf import empty_pb2

# python -m grpc_tools.protoc -I../SabberStone_gRPC/Protos --python_out=. --grpc_python_out=. ../SabberStone_gRPC/Protos/python.proto

channel = grpc.insecure_channel('localhost:50052')
stub = python_pb2_grpc.SabberStonePythonStub(channel)

# Generate card dictionary
cards = stub.GetCardDictionary(python_pb2.Empty())


# https://www.hearthpwn.com/decks/1286667-hand-priest
string1 = r"AAECAa0GBKbwAr3zApeHA+aIAw3lBOEH9geNCKUJ0gryDPsM5fcC0P4C0okD64oDz50DAA=="
# https://www.hearthpwn.com/decks/1286917-starter-pack-mage
string2 = r"AAECAf0EAA8MuwKVA6sEtATmBJYFhQjC8wK0/ALnlQOmmAOfmwP/nQPinwMA"

game = stub.NewGame(python_pb2.DeckStrings(deck1=string1, deck2=string2))

player1 = game.player1
player2 = game.player2

hand1 = player1.hand_zone
hand2 = player2.hand_zone
print("Player1 Hand Cards:")
for card in hand1.entities:
    print('\t' + cards.cards[card.card_id].name)

print("Player2 Hand Cards:")
for card in hand2.entities:
    print('\t' + cards.cards[card.card_id].name)


