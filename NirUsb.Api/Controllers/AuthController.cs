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
            return Unauthorized("Security device not detected. Please plug in your USB key");
        }

        byte[] salt = _cryptoService.GenerateRandomBytes(16);
        byte[] iv = _cryptoService.GenerateRandomBytes(16);

        (byte[] publicKey, byte[] privateKey) = _cryptoService.GenerateRsaKeys();
        byte[] derivedKey = _cryptoService.DeriveKeyFromCredentials(device.Id, req.Password, salt);

        byte[] encryptedPrivateKey = _cryptoService.EncryptWithAes(derivedKey, privateKey, iv);

        User user = new() {
            Name = req.Name,
            PublicKey = publicKey,
            Salt = salt
        };

        bool writeOk = await UsbHelper.WriteKeyOnDevice(
            device.Letter, user.Id.ToString(), encryptedPrivateKey
        );
        if (!writeOk) {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable, "Failed to initialize USB security token."
            );
        }

        try {
            await _repository.CreateUser(user);
        } catch (Exception ex) {
            return StatusCode(500, "Internal server error during registration.");
        }

        return CreatedAtAction(
            nameof(Login),
            new { id = user.Id }, new { user.Id, user.Name }
        );
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthRequests.LoginRequest req) {
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