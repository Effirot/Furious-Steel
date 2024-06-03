
using CharacterSystem.Objects;
using Unity.Netcode;

public class ChampionModeEffect : CharacterEffect
{
    public override bool Existance {
        get => effectsHolder.character is PlayerNetworkCharacter && ((PlayerNetworkCharacter)effectsHolder.character).ClientData.statistics.Points > 10;
    }

    public override void Start()
    {
        effectsHolder.character.CurrentSpeed += 3;
    }
    public override void Remove()
    {
        effectsHolder.character.CurrentSpeed -= 3;
    }
}