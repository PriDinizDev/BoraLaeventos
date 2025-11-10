namespace Bem_vindo.pt.Services; // CORRIGIDO

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string storedHash);
}