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
    public async Task<IActionResult> Register([FromBody] AuthRequests.RegisterRequest req) {
        if (await _repository.IsUserExists(req.Name)) {
            return Conflict("User is already registered");
        }

        UsbDevice? device = await UsbHelper.GetConnectedDevice();
        if (device is null) {
            return Unauthorized("Device not found");
        }

        Console.WriteLine(device.Id);

        byte[] salt = _cryptoService.GenerateRandomBytes(16);
        byte[] derivedKey =
            _cryptoService.DeriveKeyFromCredentials(device.Id, req.Password, salt);

        (byte[] publicKey, byte[] privateKey) rsaKeys = _cryptoService.GenerateRsaKeys();

        byte[] iv = _cryptoService.GenerateRandomBytes(16);
        byte[] encryptedPrivateKey =
            _cryptoService.EncryptWithAes(derivedKey, rsaKeys.privateKey, iv);

        User user = new() {
            Name = req.Name,
            PublicKey = rsaKeys.publicKey,
            Salt = salt
        };

        bool writeOk = await UsbHelper.WriteKeyOnDevice(
            device.Letter, user.Id.ToString(), encryptedPrivateKey
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

        try {
            UsbDevice? device = await UsbHelper.GetConnectedDevice();
            if (device is null) {
                return Unauthorized("Device not found");
            }

            byte[] derivedKey = _cryptoService.DeriveKeyFromCredentials(
                device.Id, req.Password, user.Salt
            );

            IEnumerable<string> filePaths = UsbHelper.EnumerateDatFiles(device.Letter);
            byte[]? validKey = null;

            await Parallel.ForEachAsync(
                filePaths, new ParallelOptions {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                }, async (path, ct) => {
                    if (validKey != null) {
                        return;
                    }

                    try {
                        byte[] content = await System.IO.File
                            .ReadAllBytesAsync(path, ct)
                            .ConfigureAwait(false);

                        bool isVerified = _cryptoService.VerifyUserIdentity(
                            derivedKey, content, user.PublicKey
                        );

                        if (isVerified) {
                            validKey = content;
                        }
                    } catch {
                    }
                }
            );

            if (validKey is null) {
                return Unauthorized("Invalid password or security token");
            }

            return Ok(new { Message = "Login successful", UserId = user.Id });
        } catch {
            return Unauthorized("An error occurred during authentication");
        }
    }
}