using ECommerce.Application.Abstractions;
using ECommerce.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Infrastructure.Auth;

public class PasswordHasherService : IPasswordHasher
{
    private static readonly User Placeholder = new();
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(Placeholder, password);

    public bool Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(Placeholder, hash, password) != PasswordVerificationResult.Failed;
}