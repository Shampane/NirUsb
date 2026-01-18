using Microsoft.AspNetCore.Mvc;
using NirUsb.Application.Requests;
using NirUsb.Domain.Interfaces;
using NirUsb.Domain.Models;
using NirUsb.Infrastructure.Helpers;

namespace NirUsb.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase {
    private readonly ICryptoService _cryptoService;
    private readonly IUserRepository _repository;


    public AuthController(IUserRepository repository, ICryptoService cryptoService) {
        _repository = repository;
        _cryptoService = cryptoService;
    }


    [HttpPost("register")]
    public async Task<IActionResult> Register(AuthRequests.RegisterRequest req) {
        if (await _repository.IsUserExists(req.Name)) {
            return BadRequest("User is already registered");
        }

        List<UsbDevice> devices = UsbHelper.GetDevices();
        foreach (UsbDevice device in devices) {
            Console.WriteLine($"{device.Name}");
        }

        switch (devices.Count) {
            case 0:
                return BadRequest("No USB found");
            case > 1:
                return BadRequest("Use only one USB");
        }

        string deviceId = devices[0].Id;

        byte[] salt = _cryptoService.GenerateRandomBytes(16);
        byte[] argon2Key = _cryptoService.CreateArgon2Key(deviceId, req.Password, salt);

        (byte[] publicKey, byte[] privateKey) rsaKeys = _cryptoService.CreateRsaKeys();

        byte[] iv = _cryptoService.GenerateRandomBytes(16);
        byte[] encryptedPrivateKey = _cryptoService.CreateAesKey(argon2Key, rsaKeys.privateKey, iv);

        string letter = devices[0].Letter;

        try {
            string path = Path.Combine($"{letter}:\\", "key.dat");
            await System.IO.File.WriteAllBytesAsync(path, encryptedPrivateKey);
        } catch (Exception ex) {
            return StatusCode(500, $"Failed to write to USB: {ex.Message}");
        }

        User user = new() {
            Name = req.Name,
            PublicKey = rsaKeys.publicKey,
            Salt = salt
        };

        await _repository.CreateUser(user);
        return Created($"auth/{user.Id}", user);
    }
}