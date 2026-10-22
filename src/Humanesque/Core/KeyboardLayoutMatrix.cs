namespace Humanesque.Core;

/// <summary>
/// Physical 2D Cartesian coordinate layout for standard QWERTY keyboard.
/// Used to calculate spatial distance between keys to produce realistic finger-slip typos.
/// </summary>
public static class KeyboardLayoutMatrix
{
    private static readonly Dictionary<char, (double X, double Y)> KeyCoords = new()
    {
        // Row 1 (Numbers)
        ['1'] = (0.0, 0.0), ['2'] = (1.0, 0.0), ['3'] = (2.0, 0.0), ['4'] = (3.0, 0.0), ['5'] = (4.0, 0.0),
        ['6'] = (5.0, 0.0), ['7'] = (6.0, 0.0), ['8'] = (7.0, 0.0), ['9'] = (8.0, 0.0), ['0'] = (9.0, 0.0),
        ['-'] = (10.0, 0.0), ['='] = (11.0, 0.0),

        // Row 2 (Top letter row)
        ['q'] = (0.5, 1.0), ['w'] = (1.5, 1.0), ['e'] = (2.5, 1.0), ['r'] = (3.5, 1.0), ['t'] = (4.5, 1.0),
        ['y'] = (5.5, 1.0), ['u'] = (6.5, 1.0), ['i'] = (7.5, 1.0), ['o'] = (8.5, 1.0), ['p'] = (9.5, 1.0),
        ['['] = (10.5, 1.0), [']'] = (11.5, 1.0),

        // Row 3 (Home row)
        ['a'] = (0.8, 2.0), ['s'] = (1.8, 2.0), ['d'] = (2.8, 2.0), ['f'] = (3.8, 2.0), ['g'] = (4.8, 2.0),
        ['h'] = (5.8, 2.0), ['j'] = (6.8, 2.0), ['k'] = (7.8, 2.0), ['l'] = (8.8, 2.0), [';'] = (9.8, 2.0),
        ['\''] = (10.8, 2.0),

        // Row 4 (Bottom letter row)
        ['z'] = (1.2, 3.0), ['x'] = (2.2, 3.0), ['c'] = (3.2, 3.0), ['v'] = (4.2, 3.0), ['b'] = (5.2, 3.0),
        ['n'] = (6.2, 3.0), ['m'] = (7.2, 3.0), [','] = (8.2, 3.0), ['.'] = (9.2, 3.0), ['/'] = (10.2, 3.0)
    };

    public static char GetSpatialNeighbor(char key, Random rng)
    {
        char lower = char.ToLowerInvariant(key);
        if (!KeyCoords.TryGetValue(lower, out var target))
        {
            return key; // Non-standard or space, keep original
        }

        var neighbors = KeyCoords
            .Where(k => k.Key != lower)
            .Select(k => new
            {
                Key = k.Key,
                Distance = Math.Sqrt(Math.Pow(k.Value.X - target.X, 2) + Math.Pow(k.Value.Y - target.Y, 2))
            })
            .Where(k => k.Distance <= 1.45) // Physically adjacent keys
            .OrderBy(k => k.Distance)
            .ToList();

        if (neighbors.Count == 0) return key;

        char picked = neighbors[rng.Next(neighbors.Count)].Key;
        return char.IsUpper(key) ? char.ToUpperInvariant(picked) : picked;
    }
}
