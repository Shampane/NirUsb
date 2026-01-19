using Microsoft.AspNetCore.Mvc;
using NirUsb.Domain.Interfaces;
using NirUsb.Domain.Models;

namespace NirUsb.Api.Controllers;

[ApiController]
[Route("admin")]
public class AdminController : ControllerBase {
    private readonly IUserRepository _userRepository;


    public AdminController(IUserRepository userRepository) {
        _userRepository = userRepository;
    }


    [HttpGet("users")]
    public async Task<IActionResult> GetUsers() {
        List<User> list = await _userRepository.GetUsers();
        return Ok(list);
    }


    [HttpGet("users/{name}")]
    public async Task<IActionResult> GetUserByName(string name) {
        User? user = await _userRepository.GetUserByName(name);
        if (user is null) {
            return NotFound("User not found");
        }

        return Ok(user);
    }


    [HttpDelete("users")]
    public async Task<IActionResult> RemoveAllUsers() {
        await _userRepository.RemoveAllUser();
        return NoContent();
    }
}