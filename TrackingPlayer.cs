using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


/*
 * TrackingPlayer:
 *  
 * 
 * */

class TrackingPlayer : IPlayer
{
    List<PossibleCard>[] hand_knowledge_;
    List<Card> public_cards_;

    List<Card> playable_cards_;

    Game.Viewer view_;

    public void Init(Game.Viewer view)
    {
        view_ = view;

        public_cards_ = new List<Card>();
        playable_cards_ = new List<Card>();

        for (int i = 0; i < 5; i++)
            playable_cards_.Add(new Card(i, 1));

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
    void MakeDeductionsFromKnowledge()
    {
        PossibleCard.MakeDeductionsForSet(hand_knowledge_[0], CardsPlayerCanSee(0), Enumerable.Empty<PossibleCard>());
        for (int i = 1; i < view_.NumPlayers; i++)
            PossibleCard.MakeDeductionsForSet(hand_knowledge_[i], CardsPlayerCanSee(i), hand_knowledge_[0]);
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
        }
        if (action.Type == ActionType.Play && result.Accepted)
        {
            playable_cards_.Remove(result.Card);
            if (result.Card.Number != 5)
                playable_cards_.Add(new Card(result.Card.Colour, result.Card.Number + 1));
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
        }
    }
    public Action RequestAction()
    {
        return null;
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