

public interface ITeammate 
{
    public static bool IsAlly (ITeammate teammate1, ITeammate teammate2)
    {
        if (teammate1 == null || teammate2 == null)
            return false;
            
        return 
            teammate1.TeamIndex == teammate2.TeamIndex && 
            teammate1.TeamIndex != 0 && 
            teammate2.TeamIndex != 0;
    }

    int TeamIndex { get; }
}