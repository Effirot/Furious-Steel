
using CharacterSystem.Objects;

namespace CharacterSystem.Effects
{
    public class ChampionModeEffect : CharacterEffect
    {
    #warning Champion mode fix
        public override bool Existance {
            get => true;
            // get => effectsHolder.character is PlayerNetworkCharacter && ((PlayerNetworkCharacter)effectsHolder.character).ClientData.statistics.Points > 10;
        }

        public override void Start()
        {
            effectsHolder.character.Speed += 3;
        }
        public override void Remove()
        {
            effectsHolder.character.Speed -= 3;
        }
    }
}