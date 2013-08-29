using System;
using System.Collections.Generic;

/*
 * BasicPlayer:
 *  If nobody knows they have a card they can play, give a clue such that all clued cards can be played,
 *  otherwise give a clue that matches nothing or discard oldest card. 
 * 
 * */

class BasicPlayer : IPlayer
{
    Game.Viewer view_;

    int pending_plays_;
//    bool maybe_stuck_;
    List<int> my_plays = new List<int>();

    public void Init(Game.Viewer view)
    {
        view_ = view;
    }

    bool IsCardNeeded(Card c)
    {
        return c.Number == view_.Fireworks[c.Colour] + 1;
    }

    public Action RequestAction()
    {
 //        Console.WriteLine("Player {0} deciding an action (pending_plays={1})", view_.ActualPlayerId, pending_plays_);
        if (my_plays.Count > 0)
        {
            int ix = my_plays[0];
            my_plays.RemoveAt(0);
            Action ret = new Action(ActionType.Play, ix);
            for (int i = 0; i < my_plays.Count; i++)
                if (my_plays[i] > ix) my_plays[i]--;
            return ret;
        }
        if (pending_plays_ == 0 && view_.Clues > 0)
        {
   //         Console.WriteLine("Want to give real clue");
            for (int player = 1; player < 4; player++)
            {
                IReadOnlyList<Card> hand = view_.GetHand(player);
                for (int card_ix = 0; card_ix < hand.Count; card_ix++)
                {
                    Card card = hand[card_ix];
                    if (IsCardNeeded(card))
                    {
                        // Find a safe clue
                        bool colour_ok = true;
                        bool number_ok = true;
                        for (int card2_ix = 0; card2_ix < hand.Count; card2_ix++)
                        {
                            if (card2_ix == card_ix) continue;
                            Card c2 = hand[card2_ix];
                            if (c2 == card)
                            {
                                colour_ok = false;
                                number_ok = false;
                                break;
                            }
                            if (IsCardNeeded(c2))
                            {
                                bool ok = true;
                                for (int card3_ix = 0; card3_ix < hand.Count; card3_ix++)
                                {
                                    if (card3_ix != card_ix && card3_ix != card2_ix && hand[card3_ix] == c2)
                                        ok = false;
                                }
                                if (ok) continue;
                            }
                            if (card.Colour == c2.Colour) colour_ok = false;
                            if (card.Number == c2.Number) number_ok = false;
                        }
                        if (number_ok)
                            return new Action(player, ClueType.Number, card.Number);
                        if (colour_ok)
                            return new Action(player, ClueType.Colour, card.Colour);

                    }
                }
            }
 //           Console.WriteLine("  ...but can't");
        }
        if (view_.Clues > 6)
        {
            if (pending_plays_ > 0)
            {
   //             Console.WriteLine("throwaway clue, pending_plays>0 so safe to be anything");
                return new Action(1, ClueType.Colour, 0);
            }
   //         Console.WriteLine("looking for safe clue");
            // Try to find a safe clue
            for (int player = 1; player < 4; player++)
            {
                IReadOnlyList<Card> hand = view_.GetHand(player);
                for (int i = 0; i < 5; i++)
                {
                    bool colour_ok = true;
                    bool number_ok = true;
                    for (int card_ix = 0; card_ix < hand.Count; card_ix++)
                    {
                        Card card = hand[card_ix];
                        if (card.Number == i + 1) number_ok = false;
                        if (card.Colour == i) colour_ok = false;
                    }
                    if (number_ok)
                        return new Action(player, ClueType.Number, i+1);
                    if (colour_ok)
                        return new Action(player, ClueType.Colour, i);
                }
            }
            if (view_.Clues == 8)
            {
                Console.WriteLine("I AM UNHAPPY");
                return new Action(1, ClueType.Colour, 0);
            }
        }

        return new Action(ActionType.Discard, 0);
    }
    public void NotifyAction(int player, Action action, ActionResult result)
    {
        if (action.Type == ActionType.Play)
            pending_plays_--;
        if (action.Type == ActionType.Clue)
        {
            
            if (action.TargetPlayer == 0 && pending_plays_ == 0)
            {
     //           if (result.SelectedCards.Count > 0)
     //               Console.WriteLine("Player {0} sees clue for self", view_.ActualPlayerId);
                for (int i = 0; i < result.SelectedCards.Count; i++)
                    my_plays.Add(result.SelectedCards[i]);
            }
            if (pending_plays_ == 0)
            {
                pending_plays_ += result.SelectedCards.Count;
       //         Console.WriteLine("Player {0} sees pending_plays now {1}", view_.ActualPlayerId, pending_plays_);
            }
            
        }
    }

}

