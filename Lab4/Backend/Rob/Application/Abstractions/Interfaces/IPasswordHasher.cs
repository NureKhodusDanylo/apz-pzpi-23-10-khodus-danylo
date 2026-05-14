namespace Application.Abstractions.Interfaces
{
    public interface IPasswordHasher
    {
        string Hash(string password);
    }
}