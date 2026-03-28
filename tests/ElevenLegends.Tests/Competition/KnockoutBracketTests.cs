using ElevenLegends.Competition;
using ElevenLegends.Data.Enums;

namespace ElevenLegends.Tests.Competition;

public class KnockoutBracketTests
{
    [Fact]
    public void Requires_Exactly_8_Teams()
    {
        Assert.Throws<ArgumentException>(() => new KnockoutBracket([1, 2, 3]));
    }

    [Fact]
    public void Generates_4_Quarterfinal_Fixtures()
    {
        var bracket = new KnockoutBracket([1, 2, 3, 4, 5, 6, 7, 8]);
        var fixtures = bracket.GenerateNextRound(1);

        Assert.Equal(4, fixtures.Count);
        Assert.All(fixtures, f => Assert.Equal(CompetitionPhase.Quarterfinals, f.Phase));
    }

    [Fact]
    public void Winners_Advance_Through_All_Rounds()
    {
        var bracket = new KnockoutBracket([1, 2, 3, 4, 5, 6, 7, 8]);

        // Quarterfinals: 1v2, 3v4, 5v6, 7v8
        var qf = bracket.GenerateNextRound(1);
        qf[0].Result = (3, 0); // 1 wins
        qf[1].Result = (0, 2); // 4 wins
        qf[2].Result = (1, 0); // 5 wins
        qf[3].Result = (2, 1); // 7 wins
        bracket.AdvanceRound();

        Assert.Equal(CompetitionPhase.Semifinals, bracket.CurrentPhase);

        // Semifinals: 1v4, 5v7
        var sf = bracket.GenerateNextRound(3);
        Assert.Equal(2, sf.Count);
        sf[0].Result = (2, 0); // 1 wins
        sf[1].Result = (0, 1); // 7 wins
        bracket.AdvanceRound();

        Assert.Equal(CompetitionPhase.Final, bracket.CurrentPhase);

        // Final: 1v7
        var final_ = bracket.GenerateNextRound(5);
        Assert.Single(final_);
        final_[0].Result = (1, 0); // 1 wins
        bracket.AdvanceRound();

        Assert.True(bracket.IsFinished);
        Assert.Equal(1, bracket.ChampionId);
    }

    [Fact]
    public void Draw_Goes_To_Home_Team()
    {
        var bracket = new KnockoutBracket([1, 2, 3, 4, 5, 6, 7, 8]);
        var qf = bracket.GenerateNextRound(1);
        qf[0].Result = (1, 1); // Draw → home wins (1)

        Assert.Equal(1, qf[0].WinnerClubId);
    }
}

public class MundialBracketTests
{
    [Fact]
    public void Requires_Exactly_4_Teams()
    {
        Assert.Throws<ArgumentException>(() => new MundialBracket([1, 2]));
    }

    [Fact]
    public void Full_Mundial_Produces_Champion()
    {
        var bracket = new MundialBracket([10, 20, 30, 40]);

        var sf = bracket.GenerateNextRound(1);
        Assert.Equal(2, sf.Count);
        Assert.All(sf, f => Assert.Equal(CompetitionPhase.MundialSemifinals, f.Phase));

        sf[0].Result = (2, 0); // 10 wins
        sf[1].Result = (1, 3); // 40 wins
        bracket.AdvanceRound();

        var final_ = bracket.GenerateNextRound(3);
        Assert.Single(final_);
        Assert.Equal(CompetitionPhase.MundialFinal, final_[0].Phase);

        final_[0].Result = (0, 2); // 40 wins
        bracket.AdvanceRound();

        Assert.True(bracket.IsFinished);
        Assert.Equal(40, bracket.ChampionId);
    }
}
