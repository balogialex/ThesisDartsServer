using System;
using System.ComponentModel.DataAnnotations;

namespace DartsAPI.Dtos;

public record class PlayerDto(
    string Username, 
    string Password);
