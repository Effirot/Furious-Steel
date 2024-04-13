
using CharacterSystem.Objects;

public class ChampionModeEffect : CharacterEffect
{
    public override bool Existance {
        get => effectsHolder.character is PlayerNetworkCharacter && ((PlayerNetworkCharacter)effectsHolder.character).ClientData.statistics.Points > 10;
    }

    public override void Start()
    {
        effectsHolder.character.Speed += 5;
    }
    public override void Remove()
    {
        effectsHolder.character.Speed -= 5;
    }
}