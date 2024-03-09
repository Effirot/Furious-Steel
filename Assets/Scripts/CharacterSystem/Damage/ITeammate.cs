

public interface ITeammate 
{
    public static bool IsAlly (ITeammate teammate1, ITeammate teammate2)
    {
        return 
            teammate1.TeamIndex == teammate2.TeamIndex && 
            teammate1.TeamIndex != 0 && 
            teammate2.TeamIndex != 0;
    }

    int TeamIndex { get; }
}