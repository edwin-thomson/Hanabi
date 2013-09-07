using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


/*
 * TrackingPlayer:
 *  Given a clue: If the oldest clued card might be playable, it is.
 *                If the oldest clued card might be unsafe, it is.
 *                If the clue is for no cards, if it's a colour then the oldest card is unsafe
 *                                             if it's a number then couldn't think of a useful clue
 * 
 * */

class TrackingPlayer : IPlayer
{
    List<PossibleCard>[] hand_knowledge_;
    List<Card> public_cards_;

    List<Card> playable_cards_;
    List<Card> unsafe_cards_;
    List<Card> useless_cards_;

    struct PendingPlay
    {
        public readonly int Player;
        public readonly int Index;
        public PendingPlay(int player, int index)
        {
            Player = player;
            Index = index;
        }
        public override bool Equals(object obj)
        {
            if (!(obj is PendingPlay))
                return false;
            PendingPlay other = (PendingPlay)obj;
            return this == other;
        }
        public static bool operator ==(PendingPlay a, PendingPlay b)
        {
            return a.Player == b.Player && a.Index == b.Index;
        }
        public static bool operator !=(PendingPlay a, PendingPlay b)
        {
            return !(a == b);
        }
        public override int GetHashCode()
        {
            return Player.GetHashCode() * 23 + Index.GetHashCode();
        }
        public override string ToString()
        {
            return string.Format("[P{0}->{1}]", Player, Index);
        }
    }
    List<PendingPlay> pending_plays_;

    IEnumerable<PendingPlay> MyPendingPlays
    {
        get { return from pp in pending_plays_ where pp.Player == 0 select pp; }
    }

    Game.Viewer view_;

    static readonly int[] value_counts_ = { 3, 2, 2, 2, 1 };

    public void Init(Game.Viewer view)
    {
        view_ = view;

        public_cards_ = new List<Card>();
        playable_cards_ = new List<Card>();
        unsafe_cards_ = new List<Card>();
        useless_cards_ = new List<Card>();

        pending_plays_ = new List<PendingPlay>();

        for (int i = 0; i < 5; i++)
        {
            playable_cards_.Add(new Card(i, 1));
            unsafe_cards_.Add(new Card(i, 5));
        }

        hand_knowledge_ = new List<PossibleCard>[view.NumPlayers];
        for (int i = 0; i < view.NumPlayers; i++)
        {
            hand_knowledge_[i] = new List<PossibleCard>();
            for (int j = 0; j < view.CardsInHand; j++)
                hand_knowledge_[i].Add(new PossibleCard());
        }
        MakeDeductionsFromKnowledge();
    }
    IEnumerable<Card> CardsPlayerCanSee(int player)
    {
        IEnumerable<Card> ret = public_cards_;
        for (int i = 1; i < view_.NumPlayers; i++)
            if (i != player)
                ret = ret.Concat(view_.GetHand(i));
        return ret;
    }

    void DEBUG_ValidatePossibilities()
    {
        for (int i = 0; i < hand_knowledge_[0].Count; i++)
            Debug.Assert(hand_knowledge_[0][i].CouldBe(view_.DEBUG_GetOwnHand[i]));
        for (int player = 1; player < view_.NumPlayers; player++)
        {
            for (int i = 0; i < hand_knowledge_[player].Count; i++)
                Debug.Assert(hand_knowledge_[player][i].CouldBe(view_.GetHand(player)[i]));
        }
    }

    void MakeDeductionsFromKnowledge()
    {
        /*
        if (view_.ActualPlayerId == 2)
        {
            for (int i = 0; i < hand_knowledge_[3].Count; i++)
            {
                if (view_.GetHand(3)[i] == new Card(3, 5))
                {
                    view_.Log("I think the card {0} in slot {1} might be {2}", view_.GetHand(3)[i], i, hand_knowledge_[3][i]);
                }
            }
        }
         */
        DEBUG_ValidatePossibilities();
        PossibleCard.MakeDeductionsForSet(hand_knowledge_[0], CardsPlayerCanSee(0), Enumerable.Empty<PossibleCard>());
        for (int i = 1; i < view_.NumPlayers; i++)
            PossibleCard.MakeDeductionsForSet(hand_knowledge_[i], CardsPlayerCanSee(i), hand_knowledge_[0]);
        DEBUG_ValidatePossibilities();
    }


    public void NotifyAction(int currentPlayer, Action action, ActionResult result)
    {
        if (action.Type == ActionType.Discard || action.Type == ActionType.Play)
        {
            hand_knowledge_[currentPlayer].RemoveAt(action.Card);
            

            public_cards_.Add(result.Card);
            if (result.NewCard)
            {
                hand_knowledge_[currentPlayer].Add(new PossibleCard());
                MakeDeductionsFromKnowledge();
            }
            else
            {
                int i = 0;
            }


            if (action.Type == ActionType.Play && result.Accepted)
            {
                if (view_.ActualPlayerId == 2)
                    view_.Log("Play action noticed: P{0}->{1} pending_plays={2}", currentPlayer, action.Card, string.Join(", ", pending_plays_));
                for (int i = 0; i < pending_plays_.Count; i++)
                {
                    if (pending_plays_[i].Player == currentPlayer && pending_plays_[i].Index == action.Card)
                    {
                        pending_plays_.RemoveAt(i);
                        break;
                    }
                }
                //XXX shouldn't do this sort of shuffling
                for (int i = 0; i < pending_plays_.Count; i++)
                {
                    if (pending_plays_[i].Player == currentPlayer && pending_plays_[i].Index > action.Card)
                        pending_plays_[i] = new PendingPlay(currentPlayer, pending_plays_[i].Index - 1);
                }
                if (view_.ActualPlayerId == 2)
                    view_.Log("pending_plays={0}", string.Join(", ", pending_plays_));

                playable_cards_.Remove(result.Card);
                useless_cards_.Add(result.Card);
                unsafe_cards_.Remove(result.Card);
                if (result.Card.Number != 5)
                    playable_cards_.Add(new Card(result.Card.Colour, result.Card.Number + 1));
            }
            else
            {
                if (unsafe_cards_.Contains(result.Card))
                {
                    // Ooops!
                   // System.Console.WriteLine("Ooops! Discarded last {0}", result.Card);
                    unsafe_cards_.Remove(result.Card);
                }
                else if (!useless_cards_.Contains(result.Card))
                {
                    if (public_cards_.Count(c => c == result.Card) == value_counts_[result.Card.Number - 1] - 1)
                        unsafe_cards_.Add(result.Card);
                }
            }
        }
        
        if (action.Type == ActionType.Clue)
        {
            if (action.Clue == ClueType.Colour)
            {
                for (int i = 0; i < hand_knowledge_[action.TargetPlayer].Count; i++)
                {
                    if (result.SelectedCards.Contains(i))
                        hand_knowledge_[action.TargetPlayer][i].SetColour(action.Value);
                    else
                        hand_knowledge_[action.TargetPlayer][i].EliminateColour(action.Value);
                }
            }
            else
            {
                for (int i = 0; i < hand_knowledge_[action.TargetPlayer].Count; i++)
                {
                    if (result.SelectedCards.Contains(i))
                        hand_knowledge_[action.TargetPlayer][i].SetNumber(action.Value);
                    else
                        hand_knowledge_[action.TargetPlayer][i].EliminateNumber(action.Value);
                }
            }
            MakeDeductionsFromKnowledge();
            HandleClueLogic(currentPlayer, action, result);
            MakeDeductionsFromKnowledge();
        }
    }

    enum DiscardPriority
    {
        Low,
        Medium,
        High,
        Certain
    }
    Action MakeDiscardAction(DiscardPriority priority)
    {
        for (int i = 0; i < hand_knowledge_[0].Count; i++)
        {
            if (hand_knowledge_[0][i].MustBeIn(useless_cards_))
            {
                    view_.Log("Discarding useless ix {0}", i);
                    return new Action(ActionType.Discard, i);
            }
        }
        if (priority == DiscardPriority.Low) return null;

        for (int i = 0; i < hand_knowledge_[0].Count; i++)
        {
            if (!hand_knowledge_[0][i].CouldBeIn(unsafe_cards_))
            {
                view_.Log("Discarding safe ix {0}", i);
                return new Action(ActionType.Discard, i);
            }
        }

        if (priority == DiscardPriority.Medium) return null;

        for (int i = 0; i < hand_knowledge_[0].Count; i++)
        {
            if (!hand_knowledge_[0][i].MustBeIn(unsafe_cards_))
            {
                view_.Log("Discarding safe ix {0}", i);
                return new Action(ActionType.Discard, i);
            }
        }

        if (priority == DiscardPriority.High) return null;

        return new Action(ActionType.Discard, 0);


        return null;
    }

    public Action RequestAction()
    {
               if (view_.ActualPlayerId == 2)
                   view_.Log("RequestAction pending_plays={0}", string.Join(", ", pending_plays_));
        foreach (PendingPlay pp in MyPendingPlays)
        {
            view_.Log("Told to play ix {0}", pp.Index);
            return new Action(ActionType.Play, pp.Index);
        }

        List<Card> playables = GetPlayables();

        // Sneak in an unclued action if possible
        for (int i = 0; i < hand_knowledge_[0].Count; i++)
        {
            if (hand_knowledge_[0][i].MustBeIn(playable_cards_))
            {
                view_.Log("Decided to play ix {0}", i);
                return new Action(ActionType.Play, i);
            }
        }

        Action ret = null;

        if (view_.Clues == 1)
        {
            ret = MakeDiscardAction(DiscardPriority.High);
            if (ret != null)
                return ret;
        }
        if (view_.Clues == 0)
        {
            return MakeDiscardAction(DiscardPriority.Certain);
        }


        for (int player = 1; player < view_.NumPlayers; player++)
        {
            IReadOnlyList<Card> hand = view_.GetHand(player);
            // Start by finding a play
            for (int i = 0; i < hand.Count; i++)
            {
                if (pending_plays_.Contains(new PendingPlay(player, i))) continue;
                if (playables.Contains(hand[i]))
                {
                    // Candidate card
                    bool number_ok = true;
                    bool colour_ok = true;
                    for (int j = 0; j < i; j++)
                    {
                        if (pending_plays_.Contains(new PendingPlay(player, j))) continue;
                        if (hand[j].Colour == hand[i].Colour)
                            colour_ok = false;
                        if (hand[j].Number == hand[i].Number)
                            number_ok = false;
                    }
                    if (number_ok)
                    {
                        view_.Log("Giving number clue to ask for play of {0}", hand[i]);
                        return new Action(player, ClueType.Number, hand[i].Number);
                    }
                    if (colour_ok)
                    {
                        view_.Log("Giving colour clue to ask for play of {0}", hand[i]);
                        return new Action(player, ClueType.Colour, hand[i].Colour);
                    }
                }
            }
            if (view_.Clues <= 3)
            {
                ret = TryDiscardClue(playables, player);
                if (ret != null) return ret;
            }
        }

        for (int player = 1; player < view_.NumPlayers; player++)
        {
            ret = TryDiscardClue(playables, player);
            if (ret != null) return ret;
        }

        if (view_.Clues < 4)
        {
            MakeDiscardAction(DiscardPriority.Medium);
        }

        if (view_.Clues < 8)
        {
            MakeDiscardAction(DiscardPriority.Low);
        }



        bool[] numbers = new bool[5];
        int number = 0;
        foreach (Card c in view_.GetHand(1))
            numbers[c.Number - 1] = true;
        for (int j = 0; j < 5; j++)
        {
            if (!numbers[j])
            {
                number = j;
                break;
            }
        }
        view_.Log("Giving useless number clue");
        return new Action(1, ClueType.Number, number + 1);
    }

    private Action TryDiscardClue(List<Card> playables, int player)
    {
        IReadOnlyList<Card> hand = view_.GetHand(player);
        for (int i = 0; i < hand.Count; i++)
        {
            if (unsafe_cards_.Contains(hand[i]) && !hand_knowledge_[player][i].MustBeIn(unsafe_cards_) && !pending_plays_.Contains(new PendingPlay(player, i)))
            {
                if (!hand_knowledge_[player][i].CouldBeIn(playables))
                {
                    // Candidate card
                    bool number_ok = true;
                    bool colour_ok = true;
                    for (int j = 0; j < i; j++)
                    {
                        if (hand[j].Colour == hand[i].Colour)
                            colour_ok = false;
                        if (hand[j].Number == hand[i].Number)
                            number_ok = false;
                    }
                    if (number_ok)
                    {
                        view_.Log("Giving number discard clue to mark {0}", hand[i]);
                        return new Action(player, ClueType.Number, hand[i].Number);
                    }
                    if (colour_ok)
                    {
                        view_.Log("Giving number discard clue to mark {0}", hand[i]);
                        return new Action(player, ClueType.Colour, hand[i].Colour);
                    }
                }
                else if (i == 0)
                {
                    bool[] colours = new bool[5];
                    int colour = 0;
                    foreach (Card c in hand)
                        colours[c.Colour] = true;
                    for (int j = 0; j < 5; j++)
                    {
                        if (!colours[j])
                        {
                            colour = j;
                            break;
                        }
                    }
                    view_.Log("Giving empty colour discard clue to mark {0}", hand[i]);
                    return new Action(player, ClueType.Colour, colour);
                }
            }
        }
        return null;
    }

    List<Card> GetPlayables()
    {
        List<Card> playables = new List<Card>(playable_cards_);
        foreach (PendingPlay pp in pending_plays_)
        {
            if (pp.Player == 0)
                continue;
            Card c = view_.GetHand(pp.Player)[pp.Index];
            playables.Remove(c);
        }
        return playables;
    }

    void HandleClueLogic(int currentPlayer, Action action, ActionResult result)
    {
        // XXX This logic doesn't work if we don't know one of the pending plays
        //    but some deduction should still be done
        List<Card> playables = GetPlayables();
        if (playables != null)
        {
            if (result.SelectedCards.Count > 0)
            {
                int first_ix = -1;
                for (int i = 0; i < result.SelectedCards.Count; i++)
                {
                    if (!pending_plays_.Contains(new PendingPlay(action.TargetPlayer, result.SelectedCards[i])))
                    {
                        first_ix = result.SelectedCards[i];
                        break;
                    }
                }
                if (first_ix == -1)
                {
                    System.Console.WriteLine("No nonpending card? what does this mean?");
                    return;
                }
                if (hand_knowledge_[action.TargetPlayer][first_ix].CouldBeIn(playables))
                {
                    if (action.TargetPlayer == 0 || playables.Contains(view_.GetHand(action.TargetPlayer)[first_ix]))
                    {
                        pending_plays_.Add(new PendingPlay(action.TargetPlayer, first_ix));
                        if (action.TargetPlayer == 0)
                        {
                            view_.Log("This clue is telling me to play ix {0}", first_ix);
                        }
              //          if (view_.ActualPlayerId == 2)
              //              view_.Log("handlecluelogic pending_plays={0}", string.Join(", ", pending_plays_));

                        hand_knowledge_[action.TargetPlayer][first_ix].SetIsOneOf(playables);
                        DEBUG_ValidatePossibilities();
                        for (int i = 0; i < first_ix; i++)
                        {
                            // Not actually true - maybe it just couldn't be clued
                            //      if (!pending_plays_.Contains(new PendingPlay(action.TargetPlayer, i)))
                            //          hand_knowledge_[action.TargetPlayer][i].SetIsNotOneOf(playables);
                        }
                    }
                }
                else if (hand_knowledge_[action.TargetPlayer][first_ix].CouldBeIn(unsafe_cards_))
                {
                    if (action.TargetPlayer == 0)
                    {
                        view_.Log("I am being told not to discard ix {0}", first_ix);
                    }
                    hand_knowledge_[action.TargetPlayer][first_ix].SetIsOneOf(unsafe_cards_);
                    DEBUG_ValidatePossibilities();
                //    for (int i = 0; i < first_ix; i++)
                //        hand_knowledge_[action.TargetPlayer][i].SetIsNotOneOf(unsafe_cards_);
                }
            }
            else if (action.Clue == ClueType.Colour)
            {
                if (action.TargetPlayer == 0)
                {
                    view_.Log("I am being told not to discard ix 0");
                }
                hand_knowledge_[action.TargetPlayer][0].SetIsOneOf(unsafe_cards_);
                DEBUG_ValidatePossibilities();
            }
        }
    }


    public void GivenCard()
    {
        hand_knowledge_[0].Add(new PossibleCard());
        MakeDeductionsFromKnowledge();
    }
    public void PlayerGivenCard(int player, Card c)
    {
        Debug.Assert(player != 0);
        hand_knowledge_[player].Add(new PossibleCard());
        MakeDeductionsFromKnowledge();

    }
}