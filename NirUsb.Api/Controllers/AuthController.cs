using System.Security.Cryptography;
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
            return Conflict("User is already registered");
        }

        List<UsbDevice> devices = UsbHelper.GetDevices();

        switch (devices.Count) {
            case 0:
                return BadRequest("No USB found");
            case > 1:
                return BadRequest("Use only one USB");
        }

        string deviceId = devices[0].Id;

        byte[] salt = _cryptoService.GenerateRandomBytes(16);
        byte[] argon2Key = _cryptoService.GenerateKeyFromDeviceId(deviceId, req.Password, salt);

        (byte[] publicKey, byte[] privateKey) rsaKeys = _cryptoService.CreateRsaKeys();

        byte[] iv = _cryptoService.GenerateRandomBytes(16);
        byte[] encryptedPrivateKey = _cryptoService.CreateAesKey(argon2Key, rsaKeys.privateKey, iv);

        string letter = devices[0].Letter;

        User user = new() {
            Name = req.Name,
            PublicKey = rsaKeys.publicKey,
            Salt = salt
        };

        bool writeOk = await UsbHelper.WriteKeyToDevice(
            letter, user.Id.ToString(), encryptedPrivateKey
        );
        if (!writeOk) {
            return StatusCode(500, "Failed to write to USB");
        }

        await _repository.CreateUser(user);
        return Created($"auth/{user.Id}", user);
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login(AuthRequests.LoginRequest req) {
        User? user = await _repository.GetUserByName(req.Name);
        if (user == null) {
            return Unauthorized("Invalid username");
        }

        List<UsbDevice> devices = UsbHelper.GetDevices();

        switch (devices.Count) {
            case 0:
                return BadRequest("No USB found");
            case > 1:
                return BadRequest("Use only one USB");
        }

        string deviceId = devices[0].Id;

        byte[] argon2Key = _cryptoService.GenerateKeyFromDeviceId(deviceId, req.Password, user.Salt);
        byte[]? deviceData =
            await UsbHelper.ReadKeyFromDevice(devices[0].Letter, user.Id.ToString());
        if (deviceData is null) {
            return BadRequest("No key data from USB found");
        }

        try {
            byte[] decryptedPrivateKey = _cryptoService.DecryptAes(argon2Key, deviceData);
            using var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(decryptedPrivateKey, out _);
            byte[] derivedPublicKey = rsa.ExportSubjectPublicKeyInfo();

            if (!derivedPublicKey.SequenceEqual(user.PublicKey)) {
                return Unauthorized("Identity verification failed");
            }

            return Ok(new { Message = "Login successful", UserId = user.Id });
        } catch {
            return Unauthorized("Invalid password or security token");
        }
    }
}