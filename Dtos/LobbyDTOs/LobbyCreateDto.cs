using System;

namespace DartsAPI.Dtos;

public class LobbyCreateDto
{
    public string Name { get; set; }
    public int MaxPlayers { get; set; }
}
