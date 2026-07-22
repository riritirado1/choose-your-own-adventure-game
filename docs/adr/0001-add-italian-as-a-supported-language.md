# 1. Add Italian as a supported language

- **Status:** Accepted
- **Date:** 2026-07-23
- **Deciders:** Raul Tirado

## Context

The game shipped with three languages — English, Spanish, and French — selected at
startup and served through the `Messages` class from a single `language_data.json`
file. We wanted to add a fourth language, Italian.

Two properties of the existing design shaped this decision:

1. **Localization data is fully externalized.** `Messages` reads every user-facing
   string from `language_data.json` at runtime. No game logic branches on language.
   `Game`, `Player`, `Dragon`, and `Combat` only ever call `GetMessage(key)` or one
   of the `Translate*ForDisplay` methods.

2. **Player input is normalized to canonical English.** `raceMap` and
   `occupationMap` translate localized input words (`guerrero`, `guerrier`) into
   canonical English keys (`Fighter`). Game logic — notably `AssignWeaponByOccupation()`
   and the Halfling/Elf stat modifiers in `Player` — switches on those canonical
   English values. The `display*Map` sections translate back for output.

Together these mean a new language is a **data change plus one menu entry**, not a
code change. That is the property we wanted to preserve.

## Decision

Add Italian by:

1. Appending an `"Italian"` section to `language_data.json` containing all six
   sub-sections (`dictionary`, `raceMap`, `occupationMap`, `displayRaceMap`,
   `displayOccupationMap`, `displayWeaponMap`).
2. Adding option `4` to the startup language menu in `Game.cs`, and reformatting the
   switch expression across multiple lines so further languages stay readable.

No other source file was modified.

### Supporting choices

**Italian strings are written without accents** (`Agilita`, `piu`, `Qual e`), matching
the existing Spanish and French entries, which are likewise accent-free (`Ocupacion`,
`etes`, `degats`). The original authors appear to have done this to avoid Windows
console codepage problems, where non-ASCII output can render as mojibake under the
default OEM codepage. We chose consistency with the existing file over typographic
correctness. See "Accented text" under Consequences.

**Terminology follows standard Italian tabletop RPG conventions:** `Mezzuomo`
(Halfling), `Ladro` (Thief), `Guerriero` (Fighter), `Arciere` (Archer),
`Spada lunga`, `Pugnale`, `Artigli`.

**The canonical keys stay English.** Italian input words were added to `raceMap` and
`occupationMap` as keys mapping to the same canonical values the other languages use.
This keeps `AssignWeaponByOccupation()` and the race-based stat modifiers working
untouched.

## Alternatives considered

**Per-language files (`language_data.it.json`).** Would scale better and reduce merge
conflicts on a shared team repo. Rejected for now: it changes `Messages.ReadDictionary()`
and the `.csproj` copy rule, which is a larger blast radius than this change warrants.
Worth revisiting past ~6 languages.

**A .NET resource-based approach (`.resx` / `CultureInfo`).** The idiomatic .NET
answer, with tooling and pluralization support. Rejected as disproportionate for a
course project, and it would discard the working JSON pipeline for no gameplay benefit.

**Machine translation at runtime.** Rejected — adds a network dependency and a cost
per run to a self-contained offline console game, with unpredictable output quality.

## Consequences

### Positive

- Italian players get a fully localized experience: menus, character creation,
  combat narration, the dragon's taunts, and both endings.
- Confirms the localization design generalizes. Adding a fifth language is now a
  known, documented, data-only procedure.
- No change to game logic means no new paths through combat or character creation,
  so the risk of regression in existing languages is essentially zero.

### Negative / accepted trade-offs

- **Accented text.** `Agilita` and `piu` are misspellings in real Italian. Fixing
  this properly means adding accents across *all four* languages and setting
  `Console.OutputEncoding = Encoding.UTF8` in `Program.cs` — a separate decision,
  deliberately not bundled here.
- **No automated test coverage for Italian.** `GameTests` and `DragonTests` are
  parameterized by language string but only exercise English, Spanish, and French.
  Italian is verified manually (see below) but is not protected against regression.
- **Translation quality is unreviewed.** No native Italian speaker has checked the
  strings. Errors would ship silently — `GetMessage` returns `[key]` for a *missing*
  key, but a *wrong translation* has no such signal.
- **`language_data.json` is now ~420 lines.** Approaching the size where the
  per-language-file alternative becomes the better structure.

### Verification performed

Because a translation error surfaces only at runtime, and only for the affected
language, correctness was checked programmatically rather than by eye:

| Check | Result |
| --- | --- |
| `dictionary` keys match English | 69 / 69 |
| `raceMap` / `occupationMap` canonical values match English | identical sets |
| `display*Map` keyed by canonical English | all match |
| `{0}` / `{1}` placeholders match English per key | no mismatches |
| Non-ASCII characters in the Italian section | none |
| Build | 0 warnings, 0 errors |
| Existing test suite | 94 / 94 pass |

The placeholder check is the important one: a dropped `{0}` in a key such as
`dragon_attack_intro` would throw `FormatException` mid-combat, and only for Italian
players. Key-count parity alone would not catch it.

Manual playthroughs additionally confirmed Halfling/Thief, Human/Fighter through to
victory, and Dwarf/Archer down the south path and out — verifying Italian input words
resolve to canonical values, weapon names translate, and `{0}` interpolation in the
dragon's taunts renders correctly.

## Follow-ups

- Add Italian to the language-parameterized rows in `GameTests` and `DragonTests`.
- Consider a test that asserts key and placeholder parity across *all* languages,
  turning the manual checks above into a permanent guarantee.
- Decide separately whether to adopt UTF-8 console output and proper accents.
