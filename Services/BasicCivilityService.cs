using Portfolio.Services.Interfaces;

namespace Portfolio.Services;

public class BasicCivilityService : ICivility
{
    private readonly IWebHostEnvironment _hostEnvironment;

    public BasicCivilityService(IWebHostEnvironment hostEnvironment)
    {
        _hostEnvironment = hostEnvironment;
    }

    public (bool Virdict, List<string> badWords) IsCivil(string phrase)
    {
        //get phrase to check
        var phraseInLowerCase = phrase.Trim().ToLower();

        //create new profanity filter.
        var filter = new ProfanityFilter.ProfanityFilter();

        //check for bad words.
        var badWordList = filter.DetectAllProfanities(phraseInLowerCase);

        //return result based on the number of bad words.
        if (badWordList.Count > 0) return (false, badWordList.ToList());
        return (true, new List<string>());
    }
}