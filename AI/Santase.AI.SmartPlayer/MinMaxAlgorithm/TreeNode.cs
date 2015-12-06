using Santase.Logic.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Santase.AI.SmartPlayer.MinMaxAlgorithm
{
     public class TreeNode
    {

        public TreeNode()
        {
            this.Points = 0;
            this.Neighbours = new Dictionary<Card, TreeNode>();
        }

        public int Points { get; set; }

        public bool IsMinimizing { get; set; }

        public int Alpha { get; set; }
        public int Beta { get; set; }

        public Dictionary<Card, TreeNode> Neighbours { get; set; }

        public void Add(Card edge)
        {
            if (!this.Neighbours.ContainsKey(edge))
            {
                var child = new TreeNode();
                this.Neighbours.Add(edge, child);
            }
        }
    }
}
