using System.ComponentModel.DataAnnotations;

namespace NirUsb.Domain.Models;

public class User {
    [Key] public Guid Id { get; } = Guid.CreateVersion7();
    public required string Name { get; init; }
    public required byte[] Salt { get; init; }
    public required byte[] PublicKey { get; init; }
}