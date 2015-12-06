namespace Santase.AI.SmartPlayer
{
    using System.Collections.Generic;
    using Santase.AI.SmartPlayer.Helpers;
    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public class ProbabilityCalculator
    {
        private const int SixFactoriel = 720;
        private PlayerTurnContext context;
        private OpponentSuitCardsProvider opponentsCardProvider;

        public ProbabilityCalculator(PlayerTurnContext context, OpponentSuitCardsProvider opponentsCardProvider)
        {
            this.context = context;
            this.opponentsCardProvider = opponentsCardProvider;
        }

        public double CalculateProbabilityCardToBeTaken(Card card, ICollection<Card> myCards, ICollection<Card> playedCards)
        {
            ICollection<Card> opponentCards = this.opponentsCardProvider.GetOpponentCards(myCards, playedCards, this.context.TrumpCard, card.Suit);
            ICollection<Card> trumpOpponentCard = this.opponentsCardProvider.GetOpponentCards(myCards, playedCards, this.context.TrumpCard, this.context.TrumpCard.Suit);
            int biggestThanCardCount = 0;
            foreach (var opponentCard in opponentCards)
            {
                if (opponentCard.Suit == this.context.TrumpCard.Suit && card.Suit != opponentCard.Suit)
                {
                    biggestThanCardCount++;
                }
                else if (opponentCard.GetValue() > card.GetValue())
                {
                    biggestThanCardCount++;
                }
            }

            if (card.Suit != this.context.TrumpCard.Suit)
            {
                biggestThanCardCount += trumpOpponentCard.Count;
            }

            int lesserThanCardCount = 24 - playedCards.Count - biggestThanCardCount - 6;
            if (lesserThanCardCount < 6)
            {
                return 1;
            }

            double result = CalculateFactoriel(lesserThanCardCount) / SixFactoriel * CalculateFactoriel(lesserThanCardCount - 6);
            return 1 - result;
        }

        private static long CalculateFactoriel(int number)
        {
            long result = 1;
            for (int i = 2; i <= number; i++)
            {
                result *= i;
            }

            return result;
        }
    }
}
