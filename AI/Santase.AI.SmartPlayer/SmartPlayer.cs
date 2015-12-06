namespace Santase.AI.SmartPlayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Santase.AI.SmartPlayer.Helpers;
    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    // Overall strategy can be based on the game score. When opponent is close to the winning the player should be riskier.
    public class SmartPlayer : BasePlayer
    {
        private readonly ICollection<Card> playedCards = new List<Card>();

        private readonly OpponentSuitCardsProvider opponentSuitCardsProvider = new OpponentSuitCardsProvider();

        public override string Name => "Smart Player";

        public ProbabilityCalculator calculator;

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            if (this.PlayerActionValidator.IsValid(PlayerAction.ChangeTrump(), context, this.Cards))
            {
                return this.ChangeTrump(context.TrumpCard);
            }

            if (this.CloseGame(context))
            {
                return this.CloseGame();
            }

            return this.ChooseCard(context);
        }

        public override void EndRound()
        {
            this.playedCards.Clear();
            base.EndRound();
        }

        public override void EndTurn(PlayerTurnContext context)
        {
            this.playedCards.Add(context.FirstPlayedCard);
            this.playedCards.Add(context.SecondPlayedCard);
        }

        private bool CloseGame(PlayerTurnContext context)
        {
            List<Card> strongTrumpCards = this.Cards.Where(c => c.Suit == context.TrumpCard.Suit && c.GetValue() >= 10).ToList();
            int points = 0;
            var noTrupmAces = this.Cards.Where(c => c.Suit != context.TrumpCard.Suit && c.GetValue() == 11).ToList();
            var noTrumpTens = this.Cards.Where(c => c.Suit != context.TrumpCard.Suit && c.GetValue() == 10).ToList();
            var powerfullTrumps = this.Cards.Where(c => c.Suit == context.TrumpCard.Suit && c.GetValue() >= 4).ToList();
            int noTrumpTensCount = 0;
            if (context.IsFirstPlayerTurn)
            {
                points = context.FirstPlayerRoundPoints;
            }
            else
            {
                points = context.SecondPlayerRoundPoints;
            }

            if (noTrumpTens.Count > 0)
            {
                foreach (var ten in noTrumpTens)
                {
                    if (this.playedCards.Where(p => p.Suit == ten.Suit && p.GetValue() == 11).Count() == 1)
                    {
                        noTrumpTensCount++;
                    }
                }
            }

            if ((noTrumpTensCount >= 2 || noTrupmAces.Count >= 2) && powerfullTrumps.Count >= 2 && points > 46)
            {
                return true;
            }

            if (strongTrumpCards.Count() >= 1
                && (strongTrumpCards[0].GetValue() == 10 || strongTrumpCards[0].GetValue() == 11)
                && points >= 56)
            {
                if (this.playedCards.Where(p => p.GetValue() == 11 && p.Suit == context.TrumpCard.Suit).Count() > 0)
                {
                    GlobalStats.GamesClosedByPlayer++;
                    return true;
                }
            }

            return false;
        }

        private PlayerAction ChooseCard(PlayerTurnContext context)
        {
            var possibleCardsToPlay = this.PlayerActionValidator.GetPossibleCardsToPlay(context, this.Cards);
            return context.State.ShouldObserveRules
                       ? (context.IsFirstPlayerTurn
                              ? this.ChooseCardWhenPlayingFirstAndRulesApply(context, possibleCardsToPlay)
                              : this.ChooseCardWhenPlayingSecondAndRulesApply(context, possibleCardsToPlay))
                       : (context.IsFirstPlayerTurn
                              ? this.ChooseCardWhenPlayingFirstAndRulesDoNotApply(context, possibleCardsToPlay)
                              : this.ChooseCardWhenPlayingSecondAndRulesDoNotApply(context, possibleCardsToPlay));
        }

        private PlayerAction ChooseCardWhenPlayingFirstAndRulesDoNotApply(PlayerTurnContext context, ICollection<Card> possibleCardsToPlay)
        {
            var action = this.TryToAnnounce20Or40(context, possibleCardsToPlay);
            if (action != null)
            {
                return action;
            }

            int points = 0;
            int secondPlayerPoints = 0;
            if (context.IsFirstPlayerTurn)
            {
                points = context.FirstPlayerRoundPoints;
                secondPlayerPoints = context.SecondPlayerRoundPoints;
            }
            else
            {
                points = context.SecondPlayerRoundPoints;
                secondPlayerPoints = context.FirstPlayerRoundPoints;
            }

            if (points < 33 && secondPlayerPoints >= 50)
            {
                List<Card> cardsBiggerThanTen = this.Cards.Where(c => c.Suit == context.TrumpCard.Suit && c.GetValue() >= 10).ToList();
                if (cardsBiggerThanTen.Count() == 1 && cardsBiggerThanTen[0].GetValue() == 10)
                {
                    this.calculator = new ProbabilityCalculator(context, this.opponentSuitCardsProvider);
                    double probabilityTenToBeTaken = this.calculator.CalculateProbabilityCardToBeTaken(cardsBiggerThanTen[0], this.Cards, this.playedCards);

                    if (probabilityTenToBeTaken <= 0.5)
                    {
                        return this.PlayCard(cardsBiggerThanTen[0]);
                    }
                }
            }

            Card cardToPlay = new Card(context.TrumpCard.Suit, CardType.Ace);
            foreach (var card in possibleCardsToPlay)
            {
                if (card.GetValue() < cardToPlay.GetValue() && card.Suit != context.TrumpCard.Suit)
                {

                    if ((card.Type == CardType.King && !this.playedCards.Any(p => p.Type == CardType.Queen && p.Suit == card.Suit))
                        || (card.Type == CardType.Queen && !this.playedCards.Any(p => p.Type == CardType.King && p.Suit == card.Suit)))
                    {
                        continue;
                    }

                    cardToPlay = card;
                }
            }

            return this.PlayCard(cardToPlay);
        }

        private PlayerAction ChooseCardWhenPlayingFirstAndRulesApply(PlayerTurnContext context, ICollection<Card> possibleCardsToPlay)
        {
            // Find card that will surely win the trick
            var opponentHasTrump =
                this.opponentSuitCardsProvider.GetOpponentCards(
                    this.Cards,
                    this.playedCards,
                    context.CardsLeftInDeck == 0 ? null : context.TrumpCard,
                    context.TrumpCard.Suit).Any();

            var trumpCard = this.GetCardWhichWillSurelyWinTheTrick(
                context.TrumpCard.Suit,
                context.CardsLeftInDeck == 0 ? null : context.TrumpCard,
                opponentHasTrump);
            if (trumpCard != null)
            {
                return this.PlayCard(trumpCard);
            }

            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                var possibleCard = this.GetCardWhichWillSurelyWinTheTrick(
                    suit,
                    context.CardsLeftInDeck == 0 ? null : context.TrumpCard,
                    opponentHasTrump);
                if (possibleCard != null)
                {
                    return this.PlayCard(possibleCard);
                }
            }

            // Announce 40 or 20 if possible
            var action = this.TryToAnnounce20Or40(context, possibleCardsToPlay);
            if (action != null)
            {
                return action;
            }

            // Smallest non-trump card
            var cardToPlay =
                possibleCardsToPlay.Where(x => x.Suit != context.TrumpCard.Suit)
                    .OrderBy(x => x.GetValue())
                    .FirstOrDefault();
            if (cardToPlay != null)
            {
                return this.PlayCard(cardToPlay);
            }

            // Smallest card
            cardToPlay = possibleCardsToPlay.OrderBy(x => x.GetValue()).FirstOrDefault();
            return this.PlayCard(cardToPlay);
        }

        private Card GetCardWhichWillSurelyWinTheTrick(CardSuit suit, Card trumpCard, bool opponentHasTrump)
        {
            var myBiggestCard =
                this.Cards.Where(x => x.Suit == suit).OrderByDescending(x => x.GetValue()).FirstOrDefault();
            if (myBiggestCard == null)
            {
                return null;
            }

            var opponentBiggestCard =
                this.opponentSuitCardsProvider.GetOpponentCards(this.Cards, this.playedCards, trumpCard, suit)
                    .OrderByDescending(x => x.GetValue())
                    .FirstOrDefault();

            if (!opponentHasTrump && opponentBiggestCard == null)
            {
                return myBiggestCard;
            }

            if (opponentBiggestCard != null && opponentBiggestCard.GetValue() < myBiggestCard.GetValue())
            {
                return myBiggestCard;
            }

            return null;
        }

        private PlayerAction ChooseCardWhenPlayingSecondAndRulesDoNotApply(PlayerTurnContext context, ICollection<Card> possibleCardsToPlay)
        {
            var biggerCard =
                possibleCardsToPlay.Where(
                    x => x.Suit == context.FirstPlayedCard.Suit && x.GetValue() > context.FirstPlayedCard.GetValue())
                    .OrderByDescending(x => x.GetValue())
                    .FirstOrDefault();
            if (biggerCard != null)
            {
                if ((biggerCard.Type == CardType.Queen && this.playedCards.Contains(new Card(biggerCard.Suit, CardType.King)))
                    || (biggerCard.Type == CardType.King || this.playedCards.Contains(new Card(biggerCard.Suit, CardType.Queen))))
                {
                    return this.PlayCard(biggerCard);
                }
            }

            if (context.FirstPlayedCard.Type == CardType.Ten && context.FirstPlayedCard.Suit == context.TrumpCard.Suit)
            {
                if (possibleCardsToPlay.Contains(new Card(context.TrumpCard.Suit, CardType.Ace)))
                {
                    return this.PlayCard(new Card(context.TrumpCard.Suit, CardType.Ace));
                }
            }

            if ((context.FirstPlayedCard.Type == CardType.Ace || context.FirstPlayedCard.Type == CardType.Ten) && context.FirstPlayedCard.Suit != context.TrumpCard.Suit)
            {
                if (possibleCardsToPlay.Contains(new Card(context.TrumpCard.Suit, CardType.Nine)))
                {
                    return this.PlayCard(new Card(context.TrumpCard.Suit, CardType.Nine));
                }

                if (possibleCardsToPlay.Contains(new Card(context.TrumpCard.Suit, CardType.Jack)))
                {
                    return this.PlayCard(new Card(context.TrumpCard.Suit, CardType.Jack));
                }

                if (possibleCardsToPlay.Contains(new Card(context.TrumpCard.Suit, CardType.Queen))
                    && this.playedCards.Contains(new Card(context.TrumpCard.Suit, CardType.King)))
                {
                    return this.PlayCard(new Card(context.TrumpCard.Suit, CardType.Queen));
                }

                if (possibleCardsToPlay.Contains(new Card(context.TrumpCard.Suit, CardType.King))
                    && this.playedCards.Contains(new Card(context.TrumpCard.Suit, CardType.Queen)))
                {
                    return this.PlayCard(new Card(context.TrumpCard.Suit, CardType.King));
                }

                if (possibleCardsToPlay.Contains(new Card(context.TrumpCard.Suit, CardType.Ten)))
                {
                    return this.PlayCard(new Card(context.TrumpCard.Suit, CardType.Ten));
                }

                if (possibleCardsToPlay.Contains(new Card(context.TrumpCard.Suit, CardType.Ace)))
                {
                    return this.PlayCard(new Card(context.TrumpCard.Suit, CardType.Ace));
                }
            }

            var smallestCard = possibleCardsToPlay.OrderBy(x => x.GetValue()).FirstOrDefault();
            return this.PlayCard(smallestCard);
        }

        private PlayerAction ChooseCardWhenPlayingSecondAndRulesApply(PlayerTurnContext context, ICollection<Card> possibleCardsToPlay)
        {
            // If bigger card is available => play it
            var biggerCard =
                possibleCardsToPlay.Where(
                    x => x.Suit == context.FirstPlayedCard.Suit && x.GetValue() > context.FirstPlayedCard.GetValue())
                    .OrderByDescending(x => x.GetValue())
                    .FirstOrDefault();
            if (biggerCard != null)
            {
                return this.PlayCard(biggerCard);
            }

            // Play smallest trump card?
            var smallestTrumpCard =
                possibleCardsToPlay.Where(x => x.Suit == context.TrumpCard.Suit)
                    .OrderBy(x => x.GetValue())
                    .FirstOrDefault();
            if (smallestTrumpCard != null)
            {
                return this.PlayCard(smallestTrumpCard);
            }

            // Smallest card
            var cardToPlay = possibleCardsToPlay.OrderBy(x => x.GetValue()).FirstOrDefault();
            return this.PlayCard(cardToPlay);
        }

        private PlayerAction TryToAnnounce20Or40(PlayerTurnContext context, ICollection<Card> possibleCardsToPlay)
        {
            foreach (var card in possibleCardsToPlay)
            {
                if (card.Type == CardType.Queen
                    && this.AnnounceValidator.GetPossibleAnnounce(this.Cards, card, context.TrumpCard) == Announce.Forty)
                {
                    return this.PlayCard(card);
                }
            }

            foreach (var card in possibleCardsToPlay)
            {
                if (card.Type == CardType.Queen
                    && this.AnnounceValidator.GetPossibleAnnounce(this.Cards, card, context.TrumpCard)
                    == Announce.Twenty)
                {
                    return this.PlayCard(card);
                }
            }

            return null;
        }
    }
}
