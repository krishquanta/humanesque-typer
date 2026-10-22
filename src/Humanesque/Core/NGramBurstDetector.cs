namespace Humanesque.Core;

public static class NGramBurstDetector
{
    // High-frequency English digraphs and trigraphs where muscle memory causes burst acceleration
    private static readonly HashSet<string> CommonNGrams = new(StringComparer.OrdinalIgnoreCase)
    {
        "th", "he", "in", "er", "an", "re", "on", "at", "en", "nd",
        "ti", "es", "or", "te", "of", "ed", "is", "it", "al", "ar",
        "st", "to", "nt", "ng", "se", "ha", "as", "ou", "io", "le",
        "the", "and", "ing", "ion", "tio", "ent", "ati", "for", "her",
        "ter", "hat", "tha", "ere", "ate", "his", "con", "res", "ver",
        "all", "ons", "nce", "men", "ith", "ted", "ers", "pro", "thi"
    };

    public static bool IsInBurstSequence(string text, int currentIndex)
    {
        if (currentIndex < 1 || currentIndex >= text.Length) return false;

        // Check 2-letter digraph
        string digraph = text.Substring(currentIndex - 1, 2);
        if (CommonNGrams.Contains(digraph)) return true;

        // Check 3-letter trigraph
        if (currentIndex >= 2)
        {
            string trigraph = text.Substring(currentIndex - 2, 3);
            if (CommonNGrams.Contains(trigraph)) return true;
        }

        return false;
    }
}
