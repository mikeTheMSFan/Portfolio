namespace Portfolio.Services.Interfaces;

public interface ICivility
{
    public (bool Virdict, List<string> badWords) IsCivil(string phrase);
}