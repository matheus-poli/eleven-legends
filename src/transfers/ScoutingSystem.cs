using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Simulation;

namespace ElevenLegends.Transfers;

/// <summary>
/// Regional scouting system. Pay to scout a region, reveals free agents.
/// </summary>
public static class ScoutingSystem
{
    private static readonly List<ScoutRegion> _regions =
    [
        new()
        {
            Name = "Brasilândia",
            Cost = 5_000m,
            FirstNames = ["Lucas", "Gabriel", "Matheus", "Rafael", "Pedro", "João", "Bruno", "Vinícius"],
            LastNames = ["Silva", "Santos", "Oliveira", "Souza", "Lima", "Costa", "Ferreira", "Almeida"]
        },
        new()
        {
            Name = "Hispânia",
            Cost = 5_000m,
            FirstNames = ["Carlos", "Diego", "Alejandro", "Pablo", "Miguel", "Sergio", "Álvaro", "Javier"],
            LastNames = ["García", "Rodríguez", "Martínez", "López", "Hernández", "Fernández", "Sánchez", "Pérez"]
        },
        new()
        {
            Name = "Angleterre",
            Cost = 4_500m,
            FirstNames = ["James", "Oliver", "Harry", "Jack", "George", "Charlie", "Thomas", "William"],
            LastNames = ["Smith", "Jones", "Williams", "Brown", "Taylor", "Johnson", "Wilson", "Davies"]
        },
        new()
        {
            Name = "Itália Nova",
            Cost = 4_500m,
            FirstNames = ["Marco", "Luca", "Alessandro", "Francesco", "Lorenzo", "Matteo", "Andrea", "Simone"],
            LastNames = ["Rossi", "Russo", "Ferrari", "Esposito", "Bianchi", "Romano", "Colombo", "Ricci"]
        },
        new()
        {
            Name = "África",
            Cost = 2_000m,
            FirstNames = ["Kwame", "Abdou", "Moussa", "Ibrahim", "Youssef", "Amara", "Kofi", "Sekou"],
            LastNames = ["Diallo", "Touré", "Traoré", "Coulibaly", "Diop", "Camara", "Sylla", "Keita"]
        },
        new()
        {
            Name = "Ásia",
            Cost = 2_500m,
            FirstNames = ["Takumi", "Hiroshi", "Jin", "Wei", "Min-Jun", "Ryu", "Haruto", "Yuto"],
            LastNames = ["Tanaka", "Kim", "Park", "Chen", "Lee", "Yamamoto", "Suzuki", "Watanabe"]
        },
        new()
        {
            Name = "Américas",
            Cost = 2_500m,
            FirstNames = ["Santiago", "Matías", "Nicolás", "Sebastián", "Tomás", "Valentín", "Emiliano", "Thiago"],
            LastNames = ["González", "Muñoz", "Rojas", "Díaz", "Reyes", "Morales", "Jiménez", "Vargas"]
        }
    ];

    /// <summary>
    /// Returns all available scout regions.
    /// </summary>
    public static IReadOnlyList<ScoutRegion> GetRegions() => _regions;

    /// <summary>
    /// Scouts a region and reveals 3-5 free agents.
    /// </summary>
    /// <summary>
    /// Scouts a region and reveals 3-5 players with optional sign fees.
    /// Better players (OVR 55+) have signing fees.
    /// </summary>
    public static List<(Player Player, decimal SignFee)> Scout(IRng rng, ScoutRegion region, int nextPlayerId)
    {
        int count = rng.NextInt(3, 5);
        var results = new List<(Player, decimal)>();

        for (int i = 0; i < count; i++)
        {
            var pos = PickRandomPosition(rng);
            int age = rng.NextInt(19, 33);
            int baseAttr = rng.NextInt(35, 70);

            string firstName = region.FirstNames[rng.NextInt(0, region.FirstNames.Count - 1)];
            string lastName = region.LastNames[rng.NextInt(0, region.LastNames.Count - 1)];
            string name = $"{firstName[0]}. {lastName}";

            var attrs = GenerateScoutAttributes(rng, pos, baseAttr);

            var player = new Player
            {
                Id = nextPlayerId + i,
                Name = name,
                PrimaryPosition = pos,
                Age = age,
                Morale = rng.NextInt(40, 65),
                Chemistry = rng.NextInt(20, 45),
                Attributes = attrs
            };

            // Players with OVR 55+ have a signing fee
            float ovr = pos == Position.GK ? attrs.GoalkeeperOverall : attrs.OutfieldOverall;
            decimal signFee = ovr >= 55 ? Math.Round((decimal)(ovr * 150) / 1000m) * 1000m : 0m;

            results.Add((player, signFee));
        }

        return results;
    }

    private static Position PickRandomPosition(IRng rng)
    {
        Position[] positions =
        [
            Position.GK, Position.CB, Position.CB, Position.LB, Position.RB,
            Position.CM, Position.CM, Position.CDM, Position.CAM,
            Position.LW, Position.RW, Position.ST
        ];
        return positions[rng.NextInt(0, positions.Length - 1)];
    }

    private static PlayerAttributes GenerateScoutAttributes(IRng rng, Position pos, int baseAttr)
    {
        int Attr() => Math.Clamp(baseAttr + rng.NextInt(-10, 10), 15, 90);
        int High() => Math.Clamp(baseAttr + 8 + rng.NextInt(-5, 10), 20, 95);
        int Low() => Math.Clamp(baseAttr - 8 + rng.NextInt(-10, 5), 10, 70);

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
                Speed = Attr(), Acceleration = Attr(), Stamina = High(),
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
}
