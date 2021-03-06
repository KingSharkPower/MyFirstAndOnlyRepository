﻿namespace Santase.AI.SmartPlayer
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

        private ProbabilityCalculator calculator;

        private static CardSuit[] suitArr = new CardSuit[] { CardSuit.Club, CardSuit.Diamond, CardSuit.Heart, CardSuit.Spade };

        public override string Name => "Smart Player";

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
            bool isValid = this.PlayerActionValidator.IsValid(PlayerAction.CloseGame(), context, this.Cards);
            if (!isValid)
            {
                return false;
            }

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

            // if we have tens and the ace cards have been playd or if we have atleast to aces we close
            if ((noTrumpTensCount >= 2 || noTrupmAces.Count >= 2) && powerfullTrumps.Count >= 2 && points > 46)
            {
                GlobalStats.GamesClosedByPlayer++;
                return true;
            }

            // have a strong trump card and a lot of points
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
                        if (cardsBiggerThanTen[0] == null || !this.Cards.Contains(cardsBiggerThanTen[0]))
                        {
                            cardsBiggerThanTen[0] = this.Cards.First();
                        }

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

            if (cardToPlay == null || !this.Cards.Contains(cardToPlay))
            {
                cardToPlay = this.Cards.First();
            }

            return this.PlayCard(cardToPlay);
        }

        private PlayerAction ChooseCardWhenPlayingFirstAndRulesApply(PlayerTurnContext context, ICollection<Card> possibleCardsToPlay)
        {
            var action = this.TryToAnnounce20Or40(context, possibleCardsToPlay);
            if (action != null)
            {
                return action;
            }

            // If we have a ten or ace and the other player doesn't have a stronger card of same suit
            var tensAndAces = this.Cards.Where(e => e.GetValue() >= 10).ToList();
            if (tensAndAces.Count != 0)
            {
                foreach (var card in tensAndAces)
                {
                    var cards = this.opponentSuitCardsProvider.GetOpponentCards(this.Cards, this.playedCards, context.TrumpCard, card.Suit);
                    if (cards != null && cards.Where(e => e.GetValue() > card.GetValue()).ToList().Count != 0)
                    {
                        return this.PlayCard(card);
                    }
                }
            }

            // check if opponent has queens and kings combo
            for (int i = 0; i <= 3; i++)
            {
                var oppCards = this.opponentSuitCardsProvider.GetOpponentCards(this.Cards, this.playedCards, context.TrumpCard, suitArr[i]).ToList();
                var queensAndKings = oppCards.Where(e => e.GetValue() == 4 || e.GetValue() == 3).ToList();
                if (oppCards.Count == 0 || queensAndKings.Count == 0) continue;
                if (oppCards.Count == queensAndKings.Count && queensAndKings.Count == 2)
                {
                    var greaterCards = this.Cards.Where(e => e.Suit == context.TrumpCard.Suit).ToList();
                    if (greaterCards.Count != 0)
                    {
                        return this.PlayCard(greaterCards[0]);
                    }
                }
            }

            var opponentHasTrump = this.opponentSuitCardsProvider.GetOpponentCards(this.Cards,this.playedCards,context.CardsLeftInDeck == 0 ? null : context.TrumpCard,context.TrumpCard.Suit).Any();

            var trumpCard = this.GetCardWhichWillSurelyWinTheTrick(context.TrumpCard.Suit, context.CardsLeftInDeck == 0 ? null : context.TrumpCard, opponentHasTrump);
            if (trumpCard != null)
            {
                return this.PlayCard(trumpCard);
            }

            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                var possibleCard = this.GetCardWhichWillSurelyWinTheTrick(suit, context.CardsLeftInDeck == 0 ? null : context.TrumpCard, opponentHasTrump);
                if (possibleCard != null)
                {
                    return this.PlayCard(possibleCard);
                }
            }

            var cardToPlay =
                possibleCardsToPlay.Where(x => x.Suit != context.TrumpCard.Suit)
                    .OrderBy(x => x.GetValue())
                    .FirstOrDefault();
            if (cardToPlay != null)
            {
                return this.PlayCard(cardToPlay);
            }

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
            // get a bigger card
            var biggerCard =
                possibleCardsToPlay.Where(
                    x => x.Suit == context.FirstPlayedCard.Suit && x.GetValue() > context.FirstPlayedCard.GetValue())
                    .OrderByDescending(x => x.GetValue())
                    .FirstOrDefault();
            if (biggerCard != null)
            {
                // check if bigger card isn't in kin queen combo
                if ((biggerCard.Type == CardType.Queen && this.playedCards.Contains(new Card(biggerCard.Suit, CardType.King)))
                    || (biggerCard.Type == CardType.King || this.playedCards.Contains(new Card(biggerCard.Suit, CardType.Queen))))
                {
                    return this.PlayCard(biggerCard);
                }
            }

            // check if you have an ace trump
            if (context.FirstPlayedCard.Type == CardType.Ten && context.FirstPlayedCard.Suit == context.TrumpCard.Suit)
            {
                if (possibleCardsToPlay.Contains(new Card(context.TrumpCard.Suit, CardType.Ace)))
                {
                    return this.PlayCard(new Card(context.TrumpCard.Suit, CardType.Ace));
                }
            }

            // if we get a ace or ten non trump, get it with a trump card
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

            // else get smallest
            var smallestCard = possibleCardsToPlay.OrderBy(x => x.GetValue()).FirstOrDefault();
            return this.PlayCard(smallestCard);
        }

        private PlayerAction ChooseCardWhenPlayingSecondAndRulesApply(PlayerTurnContext context, ICollection<Card> possibleCardsToPlay)
        {
            var biggerCard =
                possibleCardsToPlay.Where(
                    x => x.Suit == context.FirstPlayedCard.Suit)
                    .OrderBy(x => x.GetValue())
                    .FirstOrDefault();
            if (biggerCard != null)
            {
                return this.PlayCard(biggerCard);
            }

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
