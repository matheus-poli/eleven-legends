using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Simulation;

namespace ElevenLegends.Transfers;

/// <summary>
/// Generates youth prospects for the academy recruitment system (card-based gacha).
/// </summary>
public static class YouthAcademy
{
    private static readonly Position[] YouthPositions =
    [
        Position.GK, Position.CB, Position.LB, Position.RB,
        Position.CM, Position.CDM, Position.CAM,
        Position.LW, Position.RW, Position.ST, Position.CF
    ];

    /// <summary>
    /// Generates 3 youth prospect cards. Player picks 1.
    /// </summary>
    public static List<(Player Prospect, decimal Fee)> GenerateProspects(
        IRng rng, string country, int nextPlayerId)
    {
        var names = GetNamePool(country);
        var prospects = new List<(Player, decimal)>();

        for (int i = 0; i < 3; i++)
        {
            int age = rng.NextInt(16, 19);
            var pos = YouthPositions[rng.NextInt(0, YouthPositions.Length - 1)];

            int baseAttr = rng.NextInt(30, 55);
            int variance = 12;

            string firstName = names.FirstNames[rng.NextInt(0, names.FirstNames.Count - 1)];
            string lastName = names.LastNames[rng.NextInt(0, names.LastNames.Count - 1)];
            string name = $"{firstName[0]}. {lastName}";

            var attrs = GenerateYouthAttributes(rng, pos, baseAttr, variance);

            var prospect = new Player
            {
                Id = nextPlayerId + i,
                Name = name,
                PrimaryPosition = pos,
                Age = age,
                Morale = rng.NextInt(50, 70),
                Chemistry = rng.NextInt(30, 50),
                Attributes = attrs
            };

            float ovr = pos == Position.GK ? attrs.GoalkeeperOverall : attrs.OutfieldOverall;
            decimal fee = Math.Round((decimal)(ovr * 200) / 1000m) * 1000m;
            fee = Math.Max(5_000m, Math.Min(15_000m, fee));

            prospects.Add((prospect, fee));
        }

        return prospects;
    }

    /// <summary>
    /// Returns the maximum player ID across all clubs (for generating new IDs).
    /// </summary>
    public static int GetMaxPlayerId(IReadOnlyList<Club> clubs)
    {
        return clubs.SelectMany(c => c.Team.Players).Max(p => p.Id);
    }

    private static PlayerAttributes GenerateYouthAttributes(
        IRng rng, Position pos, int baseAttr, int variance)
    {
        int Attr() => Math.Clamp(baseAttr + rng.NextInt(-variance, variance), 15, 70);
        int High() => Math.Clamp(baseAttr + 8 + rng.NextInt(-variance / 2, variance), 20, 75);
        int Low() => Math.Clamp(baseAttr - 5 + rng.NextInt(-variance, variance / 2), 10, 55);

        return pos switch
        {
            Position.GK => new PlayerAttributes
            {
                Finishing = Low(), Passing = Low(), Dribbling = Low(),
                FirstTouch = Attr(), Technique = Low(),
                Decisions = Attr(), Composure = High(), Positioning = Attr(),
                Anticipation = Attr(), OffTheBall = Low(),
                Speed = Attr(), Acceleration = Attr(), Stamina = Attr(),
                Strength = Attr(), Agility = Attr(),
                Consistency = Attr(), Leadership = Attr(), Flair = Low(), BigMatches = Attr(),
                Reflexes = High(), Handling = High(), GkPositioning = High(), Aerial = High()
            },
            Position.CB or Position.LB or Position.RB => new PlayerAttributes
            {
                Finishing = Low(), Passing = Attr(), Dribbling = Low(),
                FirstTouch = Attr(), Technique = Attr(),
                Decisions = High(), Composure = High(), Positioning = High(),
                Anticipation = High(), OffTheBall = Attr(),
                Speed = pos is Position.LB or Position.RB ? High() : Attr(),
                Acceleration = Attr(), Stamina = High(),
                Strength = High(), Agility = Attr(),
                Consistency = Attr(), Leadership = Attr(), Flair = Low(), BigMatches = Attr(),
                Reflexes = Low(), Handling = Low(), GkPositioning = Low(), Aerial = High()
            },
            Position.ST or Position.CF => new PlayerAttributes
            {
                Finishing = High(), Passing = Attr(), Dribbling = High(),
                FirstTouch = High(), Technique = High(),
                Decisions = Attr(), Composure = High(), Positioning = Attr(),
                Anticipation = Attr(), OffTheBall = High(),
                Speed = High(), Acceleration = High(), Stamina = Attr(),
                Strength = Attr(), Agility = High(),
                Consistency = Attr(), Leadership = Low(), Flair = High(), BigMatches = Attr(),
                Reflexes = Low(), Handling = Low(), GkPositioning = Low(), Aerial = Attr()
            },
            Position.LW or Position.RW => new PlayerAttributes
            {
                Finishing = Attr(), Passing = Attr(), Dribbling = High(),
                FirstTouch = High(), Technique = High(),
                Decisions = Attr(), Composure = Attr(), Positioning = Attr(),
                Anticipation = Attr(), OffTheBall = High(),
                Speed = High(), Acceleration = High(), Stamina = Attr(),
                Strength = Low(), Agility = High(),
                Consistency = Attr(), Leadership = Low(), Flair = High(), BigMatches = Attr(),
                Reflexes = Low(), Handling = Low(), GkPositioning = Low(), Aerial = Low()
            },
            _ => new PlayerAttributes
            {
                Finishing = Attr(), Passing = High(), Dribbling = Attr(),
                FirstTouch = High(), Technique = High(),
                Decisions = High(), Composure = High(), Positioning = Attr(),
                Anticipation = Attr(), OffTheBall = Attr(),
                Speed = Attr(), Acceleration = Attr(), Stamina = High(),
                Strength = Attr(), Agility = Attr(),
                Consistency = Attr(), Leadership = Attr(), Flair = Attr(), BigMatches = Attr(),
                Reflexes = Low(), Handling = Low(), GkPositioning = Low(), Aerial = Attr()
            }
        };
    }

    private static (IReadOnlyList<string> FirstNames, IReadOnlyList<string> LastNames) GetNamePool(
        string country)
    {
        return country switch
        {
            "Brasilândia" => (
                new[] { "Lucas", "Gabriel", "Matheus", "Rafael", "Pedro", "João", "Bruno", "Vinícius", "Kaio", "Enzo" },
                new[] { "Silva", "Santos", "Oliveira", "Souza", "Lima", "Costa", "Ferreira", "Almeida", "Pereira", "Nascimento" }),
            "Hispânia" => (
                new[] { "Carlos", "Diego", "Alejandro", "Pablo", "Miguel", "Sergio", "Álvaro", "Javier", "Hugo", "Adrián" },
                new[] { "García", "Rodríguez", "Martínez", "López", "Hernández", "Fernández", "Sánchez", "Pérez", "Ruiz", "Torres" }),
            "Angleterre" => (
                new[] { "James", "Oliver", "Harry", "Jack", "George", "Charlie", "Thomas", "William", "Daniel", "Samuel" },
                new[] { "Smith", "Jones", "Williams", "Brown", "Taylor", "Johnson", "Wilson", "Davies", "Robinson", "Thompson" }),
            "Itália Nova" => (
                new[] { "Marco", "Luca", "Alessandro", "Francesco", "Lorenzo", "Matteo", "Andrea", "Simone", "Giuseppe", "Davide" },
                new[] { "Rossi", "Russo", "Ferrari", "Esposito", "Bianchi", "Romano", "Colombo", "Ricci", "Marino", "Greco" }),
            _ => (
                new[] { "Alex", "Max", "Leo", "Tom", "Sam", "Ben", "Dan", "Chris", "Nick", "Ryan" },
                new[] { "Young", "Green", "White", "Black", "Grey", "Stone", "Wood", "Field", "Brook", "Hill" })
        };
    }
}
