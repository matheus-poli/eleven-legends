using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Simulation;

namespace ElevenLegends.Data.Generators;

/// <summary>
/// Generates 32 fictional clubs (4 countries × 8 teams × 18 players each).
/// All generation is seeded for deterministic, reproducible results.
/// </summary>
public static class TeamGenerator
{
    private static readonly string[] Countries =
        ["Brasil", "España", "England", "Italia"];

    private static readonly string[][] TeamNames =
    [
        // Brasil
        ["Flamingos FC", "Palmares SC", "São Marcos EC", "Coríntios SC",
         "Cruzado EC", "Botafoguense FC", "Atleticano MG", "Gremista RS"],
        // España
        ["Real Madriz CF", "FC Barcino", "Atlético Madriz", "Sevícia FC",
         "Valência CF", "Real Bétis CF", "Villarejo CF", "Athletic Bilbão"],
        // England
        ["Redpool FC", "Mancastle United", "Chelsington FC", "Gunners FC",
         "Totterham FC", "Mancastle City", "Leicestershire FC", "Evertown FC"],
        // Italia
        ["Juventa FC", "AC Milanello", "Inter Milanello", "AS Romagna",
         "SS Lazzio", "SSC Napolitano", "Fiorença FC", "Atalância FC"]
    ];

    private static readonly string[][] FirstNames =
    [
        // Brasil
        ["Lucas", "Gabriel", "Matheus", "Pedro", "Rafael", "André", "Bruno",
         "Carlos", "Diego", "Eduardo", "Felipe", "Gustavo", "Hugo", "Igor",
         "João", "Kaio", "Leonardo", "Marcos", "Neto", "Oscar"],
        // España
        ["Alejandro", "Carlos", "Diego", "Fernando", "Gonzalo", "Héctor",
         "Iván", "Javier", "Luis", "Miguel", "Pablo", "Raúl", "Sergio",
         "Tomás", "Álvaro", "Andrés", "Borja", "César", "Dani", "Enrique"],
        // England
        ["James", "Thomas", "Oliver", "Harry", "Jack", "Charlie", "George",
         "William", "Henry", "Alexander", "Daniel", "Luke", "Ryan", "Marcus",
         "Jordan", "Kyle", "Aaron", "Ben", "Chris", "David"],
        // Italia
        ["Alessandro", "Marco", "Lorenzo", "Francesco", "Andrea", "Gianluca",
         "Paolo", "Roberto", "Stefano", "Giuseppe", "Luca", "Matteo",
         "Nicola", "Fabio", "Davide", "Giovanni", "Simone", "Antonio",
         "Daniele", "Emanuele"]
    ];

    private static readonly string[][] LastNames =
    [
        // Brasil
        ["Silva", "Santos", "Oliveira", "Souza", "Pereira", "Costa",
         "Rodrigues", "Ferreira", "Almeida", "Nascimento", "Lima", "Araújo",
         "Ribeiro", "Carvalho", "Gomes", "Martins", "Rocha", "Moura",
         "Barbosa", "Cavalcanti"],
        // España
        ["García", "Rodríguez", "Martínez", "López", "González", "Hernández",
         "Pérez", "Sánchez", "Ramírez", "Torres", "Flores", "Rivera",
         "Gómez", "Díaz", "Ruiz", "Moreno", "Jiménez", "Álvarez",
         "Romero", "Navarro"],
        // England
        ["Smith", "Johnson", "Williams", "Brown", "Jones", "Miller", "Davis",
         "Wilson", "Moore", "Taylor", "Anderson", "Jackson", "White",
         "Harris", "Martin", "Thompson", "Clark", "Walker", "Hall", "Young"],
        // Italia
        ["Rossi", "Russo", "Ferrari", "Esposito", "Bianchi", "Romano",
         "Colombo", "Ricci", "Marino", "Greco", "Bruno", "Gallo", "Conti",
         "De Luca", "Costa", "Mancini", "Barbieri", "Fontana", "Santoro",
         "Mariani"]
    ];

    /// <summary>Starting 11: 1 GK + 4 DEF + 4 MID + 2 ST.</summary>
    private static readonly Position[] StartingFormation =
    [
        Position.GK,
        Position.LB, Position.CB, Position.CB, Position.RB,
        Position.LM, Position.CM, Position.CM, Position.RM,
        Position.ST, Position.ST
    ];

    /// <summary>7 reserves covering all position groups.</summary>
    private static readonly Position[] ReservePositions =
    [
        Position.GK,
        Position.CB, Position.LB,
        Position.CM, Position.CAM,
        Position.RW, Position.CF
    ];

    /// <summary>
    /// Generates all 32 clubs with deterministic seeded RNG.
    /// </summary>
    public static List<Club> Generate(int seed)
    {
        var rng = new SeededRng(seed);
        var clubs = new List<Club>(32);
        int clubId = 1;
        int playerId = 1;

        for (int countryIdx = 0; countryIdx < Countries.Length; countryIdx++)
        {
            for (int teamIdx = 0; teamIdx < TeamNames[countryIdx].Length; teamIdx++)
            {
                string teamName = TeamNames[countryIdx][teamIdx];
                int teamRank = teamIdx + 1; // 1 = strongest, 8 = weakest

                var players = new List<Player>(18);
                var startingIds = new List<int>(11);

                foreach (var pos in StartingFormation)
                {
                    players.Add(GeneratePlayer(playerId, countryIdx, pos, teamRank, rng));
                    startingIds.Add(playerId);
                    playerId++;
                }

                foreach (var pos in ReservePositions)
                {
                    var starter = GeneratePlayer(playerId, countryIdx, pos, teamRank, rng);
                    players.Add(starter with { Attributes = ReduceAttributes(starter.Attributes, 5) });
                    playerId++;
                }

                var team = new Team
                {
                    Id = clubId,
                    Name = teamName,
                    Players = players,
                    StartingLineup = startingIds
                };

                decimal initialBalance = (9 - teamRank) * 50_000m + rng.NextInt(0, 50_000);
                int reputation = Math.Clamp(90 - (teamRank - 1) * 8 + rng.NextInt(-5, 5), 20, 100);

                clubs.Add(new Club
                {
                    Id = clubId,
                    Name = teamName,
                    Country = Countries[countryIdx],
                    Team = team,
                    Balance = initialBalance,
                    Reputation = reputation
                });

                clubId++;
            }
        }

        return clubs;
    }

    private static Player GeneratePlayer(
        int id, int countryIdx, Position position, int teamRank, IRng rng)
    {
        string firstName = Pick(FirstNames[countryIdx], rng);
        string lastName = Pick(LastNames[countryIdx], rng);
        string name = $"{firstName[0]}. {lastName}";

        var (attrMin, attrMax) = GetQualityRange(teamRank);
        var attrs = GenerateAttributes(position, attrMin, attrMax, rng);

        return new Player
        {
            Id = id,
            Name = name,
            PrimaryPosition = position,
            Attributes = attrs,
            Age = rng.NextInt(18, 35),
            Morale = rng.NextInt(40, 70),
            Chemistry = rng.NextInt(40, 70)
        };
    }

    private static (int min, int max) GetQualityRange(int teamRank) => teamRank switch
    {
        1 or 2 => (65, 85),
        3 or 4 => (55, 75),
        5 or 6 => (45, 65),
        _ => (35, 55)
    };

    private static PlayerAttributes GenerateAttributes(
        Position position, int min, int max, IRng rng)
    {
        int Attr() => rng.NextInt(min, max);
        int High() => rng.NextInt(Math.Min(min + 10, max), Math.Min(max + 10, 100));
        int Low() => rng.NextInt(Math.Max(min - 15, 0), Math.Max(max - 15, min));

        if (position == Position.GK)
        {
            return new PlayerAttributes
            {
                Finishing = Low(), Passing = Low(), Dribbling = Low(),
                FirstTouch = Low(), Technique = Low(),
                Decisions = Attr(), Composure = High(), Positioning = High(),
                Anticipation = Attr(), OffTheBall = Low(),
                Speed = Low(), Acceleration = Low(), Stamina = Attr(),
                Strength = Attr(), Agility = High(),
                Consistency = Attr(), Leadership = Attr(), Flair = Low(),
                BigMatches = Attr(),
                Reflexes = High(), Handling = High(), GkPositioning = High(),
                Aerial = High()
            };
        }

        bool isDef = position is Position.CB or Position.LB or Position.RB
            or Position.LWB or Position.RWB;
        bool isMid = position is Position.CDM or Position.CM or Position.CAM
            or Position.LM or Position.RM;
        bool isAtt = position is Position.LW or Position.RW or Position.CF
            or Position.ST;
        bool isWide = position is Position.LB or Position.RB or Position.LWB
            or Position.RWB or Position.LM or Position.RM or Position.LW
            or Position.RW;

        return new PlayerAttributes
        {
            Finishing = isAtt ? High() : (isDef ? Low() : Attr()),
            Passing = isMid ? High() : Attr(),
            Dribbling = (isAtt || isWide) ? High() : (isDef ? Low() : Attr()),
            FirstTouch = (isAtt || isMid) ? High() : Attr(),
            Technique = isAtt ? High() : Attr(),
            Decisions = isMid ? High() : Attr(),
            Composure = (isAtt || isMid) ? High() : Attr(),
            Positioning = isDef ? High() : Attr(),
            Anticipation = isDef ? High() : Attr(),
            OffTheBall = isAtt ? High() : (isDef ? Low() : Attr()),
            Speed = isWide ? High() : Attr(),
            Acceleration = isWide ? High() : Attr(),
            Stamina = Attr(),
            Strength = isDef ? High() : Attr(),
            Agility = (isWide || isAtt) ? High() : Attr(),
            Consistency = Attr(), Leadership = Attr(),
            Flair = isAtt ? High() : Attr(),
            BigMatches = Attr(),
            Reflexes = Low(), Handling = Low(), GkPositioning = Low(),
            Aerial = isDef ? High() : Low()
        };
    }

    private static PlayerAttributes ReduceAttributes(PlayerAttributes a, int r)
    {
        int R(int val) => Math.Max(val - r, 1);
        return new PlayerAttributes
        {
            Finishing = R(a.Finishing), Passing = R(a.Passing),
            Dribbling = R(a.Dribbling), FirstTouch = R(a.FirstTouch),
            Technique = R(a.Technique), Decisions = R(a.Decisions),
            Composure = R(a.Composure), Positioning = R(a.Positioning),
            Anticipation = R(a.Anticipation), OffTheBall = R(a.OffTheBall),
            Speed = R(a.Speed), Acceleration = R(a.Acceleration),
            Stamina = R(a.Stamina), Strength = R(a.Strength),
            Agility = R(a.Agility), Consistency = R(a.Consistency),
            Leadership = R(a.Leadership), Flair = R(a.Flair),
            BigMatches = R(a.BigMatches), Reflexes = R(a.Reflexes),
            Handling = R(a.Handling), GkPositioning = R(a.GkPositioning),
            Aerial = R(a.Aerial)
        };
    }

    private static string Pick(string[] array, IRng rng) =>
        array[rng.NextInt(0, array.Length - 1)];
}
