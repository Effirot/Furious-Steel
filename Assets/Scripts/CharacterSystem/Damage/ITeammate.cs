

public interface ITeammate 
{
    Team team { get; }
}

public class Team
{
    public static bool IsAlly (ITeammate teammate1, ITeammate teammate2)
    {            
        if (teammate1 == null || teammate2 == null)
            return false;

        return (teammate1.team != null && teammate2.team != null && teammate1.team == teammate2.team) || teammate1 == teammate2;
    }
}