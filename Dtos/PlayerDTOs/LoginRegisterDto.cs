using System;
using System.ComponentModel.DataAnnotations;

namespace DartsAPI.Dtos;

public class LoginRegisterDto
{ 
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
