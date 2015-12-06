//namespace Santase.AI.SmartPlayer.MinMaxAlgorithm
//{
//    using Logic.Cards;
//    using System;
//    using System.Collections.Generic;
//    using System.Linq;
//    using System.Text;
//    using System.Threading.Tasks;

//    public class MinMaxAlgorithm
//    {
//        private ICollection<Card> myCards;
//        private ICollection<Card> opponentCards;
//        private CardSuit trumpSuit;
//        private TreeNode tree;
//        private int turn;

//        public MinMaxAlgorithm(ICollection<Card> my, ICollection<Card> opponent, CardSuit suit, int turn)
//        {
//            this.myCards = my;
//            this.opponentCards = opponent;
//            this.trumpSuit = suit;
//            this.turn = turn;
//        }

//        private void MinNode(TreeNode t, ICollection<Card> usedfirstList, 
//            ICollection<Card> usedsecondList, 
//            int points, int count,bool IsFirstPlayer )
//        { 

//            var availableCards = myCards.Where(e => !usedfirstList.Contains(e)).ToList();
//            var opponentAvailable = opponentCards.Where(e => !usedsecondList.Contains(e)).ToList();
            
//            for(int i = 0; i < availableCards.Count(); i++)
//            {
//                var card = availableCards[i];
//                var validCards = opponentAvailable.Where(e => e.Suit == card.Suit
//                && e.GetValue() > card.GetValue());
//                if (validCards == null) validCards = opponentAvailable.Where(e => e.Suit == card.Suit);
//                else if (validCards == null) validCards = opponentAvailable.Where(e => e.Suit == this.trumpSuit);
//                else if (validCards == null) validCards = opponentAvailable;

//                foreach(var opponentCard in validCards)
//                {
//                    int points = 0;
//                    if(opponentCard.GetValue() > card.GetValue() && (opponentCard.Suit == card.Suit) ||
//                        (opponentCard.Suit == trumpSuit && card.Suit != trumpSuit))
//                    {
                        
//                    }
//                }
//            }

//            t.Neighbours = new Dictionary<Card, TreeNode>();
//            for (int i = 0; i < availableOptions.Count; i++)
//            {
//                t.Neighbours.Add(availableOptions[i], new TreeNode());

//                MinNode(t.Neighbours[availableOptions[i]],usedsecondList,availableOptions,(turn+1)%2)
//            }
//        }
//        private void MaxNode() { }
//        public void CreateTree()
//        {
//            tree = new TreeNode();
//            if (turn == 0)
//            {
//                MinNode(tree, myCards, opponentCards);
//            }
//            else
//            {
//                MaxNode(tree, opponentCards, myCards);
//            }
//        }
//    }
//}
