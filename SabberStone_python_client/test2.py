import sabber_protocol.server


server = sabber_protocol.server.SabberStoneServer()

playable = server._test_get_one_playable()

print(playable.card_id)
print(playable.cost)
print(playable.atk)
print(playable.base_health)
print(playable.ghostly)

hand = server._test_zone_with_playables()

for playable in hand.Playables:
    print(playable.card_id)
    print(playable.cost)
    print(playable.atk)
    print(playable.base_health)
    print(playable.ghostly)

hand = server._test_zone_with_playables()
hand = server._test_zone_with_playables()
hand = server._test_zone_with_playables()
hand = server._test_zone_with_playables()
hand = server._test_zone_with_playables()
