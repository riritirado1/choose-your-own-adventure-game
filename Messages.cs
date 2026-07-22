using System.Globalization;
using System.Text;
using System.Text.Json;

public class Messages
{
    private string _currentLanguage;
    private bool _isLoaded;

    private Dictionary<string, string> _dictionary;
    private Dictionary<string, string> _raceMap;
    private Dictionary<string, string> _occupationMap;
    private Dictionary<string, string> _displayRaceMap;
    private Dictionary<string, string> _displayOccupationMap;
    private Dictionary<string, string> _displayWeaponMap;

    public Messages()
    {
        _currentLanguage = "English";
        _isLoaded = false;

        _dictionary = new Dictionary<string, string>();
        _raceMap = new Dictionary<string, string>();
        _occupationMap = new Dictionary<string, string>();
        _displayRaceMap = new Dictionary<string, string>();
        _displayOccupationMap = new Dictionary<string, string>();
        _displayWeaponMap = new Dictionary<string, string>();
    }

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set => SetCurrentLanguage(value);
    }

    public void SetCurrentLanguage(string language)
    {
        if (_currentLanguage == language)
            return;

        _currentLanguage = language;

        // Reload so the dictionaries match the language that was just selected.
        // Without this, changing the language after ReadDictionary() silently did nothing.
        if (_isLoaded)
            ReadDictionary();
    }

    public void ReadDictionary()
    {
        string jsonPath = Path.Combine(AppContext.BaseDirectory, "language_data.json");

        if (!File.Exists(jsonPath))
            throw new FileNotFoundException(
                $"Could not find the language file at '{jsonPath}'. " +
                "Make sure language_data.json is copied to the output directory.", jsonPath);

        string jsonText = File.ReadAllText(jsonPath);

        using JsonDocument doc = JsonDocument.Parse(jsonText);

        JsonElement langSection = FindLanguageSection(doc.RootElement, _currentLanguage);

        _dictionary = LoadSection(langSection, "dictionary");
        _raceMap = LoadSection(langSection, "raceMap");
        _occupationMap = LoadSection(langSection, "occupationMap");
        _displayRaceMap = LoadSection(langSection, "displayRaceMap");
        _displayOccupationMap = LoadSection(langSection, "displayOccupationMap");
        _displayWeaponMap = LoadSection(langSection, "displayWeaponMap");

        _isLoaded = true;
    }

    private JsonElement FindLanguageSection(JsonElement root, string language)
    {
        // Matched case-insensitively so "spanish" works as well as "Spanish".
        foreach (JsonProperty lang in root.EnumerateObject())
        {
            if (string.Equals(lang.Name, language, StringComparison.OrdinalIgnoreCase))
                return lang.Value;
        }

        var available = new List<string>();
        foreach (JsonProperty lang in root.EnumerateObject())
            available.Add(lang.Name);

        throw new InvalidOperationException(
            $"Language '{language}' was not found in language_data.json. " +
            $"Available languages: {string.Join(", ", available)}.");
    }

    private Dictionary<string, string> LoadSection(JsonElement langSection, string sectionName)
    {
        if (!langSection.TryGetProperty(sectionName, out JsonElement section))
            throw new InvalidOperationException(
                $"Section '{sectionName}' is missing from the '{_currentLanguage}' entry in language_data.json.");

        var result = new Dictionary<string, string>();

        foreach (JsonProperty entry in section.EnumerateObject())
            result[entry.Name] = entry.Value.GetString() ?? string.Empty;

        return result;
    }

    public string GetMessage(string key)
    {
        return _dictionary.TryGetValue(key, out string? message) ? message : $"[{key}]";
    }

    public bool IsValidRace(string? input)
    {
        return input != null && _raceMap.ContainsKey(NormalizeLookupKey(input));
    }

    public string NormalizeRace(string input)
    {
        return _raceMap.TryGetValue(NormalizeLookupKey(input), out string? canonical) ? canonical : input;
    }

    public bool IsValidOccupation(string? input)
    {
        return input != null && _occupationMap.ContainsKey(NormalizeLookupKey(input));
    }

    public string NormalizeOccupation(string input)
    {
        return _occupationMap.TryGetValue(NormalizeLookupKey(input), out string? canonical) ? canonical : input;
    }

    public string TranslateRaceForDisplay(string race)
    {
        return _displayRaceMap.TryGetValue(race, out string? translated) ? translated : race;
    }

    public string TranslateOccupationForDisplay(string occupation)
    {
        return _displayOccupationMap.TryGetValue(occupation, out string? translated) ? translated : occupation;
    }

    public string TranslateWeaponForDisplay(string weaponType)
    {
        return _displayWeaponMap.TryGetValue(weaponType, out string? translated) ? translated : weaponType;
    }

    /// <summary>
    /// Turns player input into a key the maps can match: trimmed, lowercased with the
    /// invariant culture, and stripped of accents so "ladron" and "ladrón" both work.
    /// </summary>
    private string NormalizeLookupKey(string input)
    {
        string trimmed = input.Trim().ToLowerInvariant();

        // Decompose accented letters into base letter + accent, then drop the accents.
        string decomposed = trimmed.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (char c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
