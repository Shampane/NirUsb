using Microsoft.AspNetCore.Mvc;
using NirUsb.Domain.Interfaces;
using NirUsb.Domain.Models;

namespace NirUsb.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase {
    private readonly IUserRepository _repository;


    public UserController(IUserRepository repository) {
        _repository = repository;
    }


    [HttpGet]
    public async Task<IActionResult> GetAll() {
        List<User> list = await _repository.GetUsers();
        return Ok(list);
    }


    [HttpGet("{name}")]
    public async Task<IActionResult> GetByName(string name) {
        User? user = await _repository.GetUserByName(name);
        if (user is null) {
            return NotFound("User not found");
        }

        return Ok(user);
    }


    [HttpPost]
    public async Task<IActionResult> Create(string name, byte[] publicKey, byte[] salt) {
        if (await _repository.IsUserExists(name)) {
            return BadRequest("User is already registered");
        }

        User user = new() {
            Name = name,
            PublicKey = publicKey,
            Salt = salt
        };

        await _repository.CreateUser(user);
        return Created($"api/users/{user.Id}", user);
    }
}