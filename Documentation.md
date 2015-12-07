# Santase Game - Team Sixty-six Project

Primary methods in our AI bot:

  - PlayCardFirstWhenRulesDoNotApply
  - PlayCardSecondWhenRulesDoNotApply 
  -  PlayCardFirstWhenRulesApply
  -  PlayCardWhenRulesApply
  -  ColoseGame

#### 1.PlayCardFirstWhenRulesDoNotApply
> We first check to see if we can announce a 20 or 40. After that we check if the
> other player is about to win and play a high card. Then, we check to see
> if we can play a king or queen card,depending on if the other one has
already been played.

#### 2.PlayCardSecondWhenRulesDoNotApply
> First we see if we the other player has given an ace or ten, and if we
> can take them with a small trump card.
> After that we see if we can find a bigger card to play, and if all options
are impossible we play a random card.

#### 3.PlayCardFirstWhenRulesApply
> We first check to see if we can announce a 20 or 40. After that we check if the
opponent has any weak cards and we have an ace or ten in our availableCards and we play
one. If that doesn't work, we check to see if the opponent has a queen - king combo of any suit and try to make him give it up.If that doesn't work, we play a trump card.

#### 4.PlayCardSecondWhenRulesApply
> First we try to play a card of the same suit. If that is not possible, we paly a random card..

#### 5.CloseGame
> We try to close a game in a few situatuions.If we have tens and aces non trump cards and any stronger non trump have been played, we close the game. Also, if we have 56 points or more and we have a strong turmp card, the function returns true as well.
