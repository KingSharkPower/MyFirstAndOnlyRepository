namespace Santase.Tests.GameSimulations.GameSimulators
{
    using AI.SmartPlayer;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    // ReSharper disable once UnusedMember.Global
    public class DummyPlayersGameSimulator : BaseGameSimulator
    {
        protected override ISantaseGame CreateGame()
        {
            IPlayer firstPlayer = new SmartPlayer();
            IPlayer secondPlayer = new SmartPlayer();
            ISantaseGame game = new SantaseGame(firstPlayer, secondPlayer); // new ConsoleLogger("[game] "));
            return game;
        }
    }
}
