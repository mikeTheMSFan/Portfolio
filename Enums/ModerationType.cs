using System.ComponentModel;

namespace Portfolio.Enums;

public enum ModerationType
{
    [Description("Political Propoganda")] Political,
    [Description("Offensive Language")] Language,
    [Description("Drug References")] Drugs,
    [Description("Threating Speech")] Threatening,
    [Description("Sexual Content")] Sexual,
    [Description("Hate Speech")] HateSpeech,
    [Description("Targeted Shaming")] Shaming
}