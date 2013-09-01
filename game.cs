using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;

enum Colour
{
    Red,
    Yellow,
    Green,
    Blue,
    White
}

struct Card
{
    public readonly int Colour;
    public readonly int Number;
    public Card(int colour, int number)
    {
        Colour = colour;
        Number = number;
    }
    public override string ToString()
    {
        return string.Format("[{0} {1}]", ((Colour)Colour).ToString(), Number);
    }
    public override bool Equals(object obj)
    {
        if (obj is Card)
            return false;
        Card other = (Card)obj;
        return this == other;
    }
    public static bool operator ==(Card a, Card b)
    {
        return a.Colour == b.Colour && a.Number == b.Number;
    }
    public static bool operator !=(Card a, Card b)
    {
        return !(a == b);
    }
    public override int GetHashCode()
    {
        return Colour.GetHashCode() * 23 + Number.GetHashCode();
    }
}

enum ActionType
{
    Play,
    Discard,
    Clue
}

enum ClueType
{
    Number,
    Colour
}


class Action
{
    // Laaazzzy
    public readonly ActionType Type;
    // For play/discard
    public readonly int Card;
    // For clue
    public readonly int TargetPlayer;
    public readonly ClueType Clue;
    public readonly int Value;
    public Action(ActionType type, int card)
    {
        Type = type;
        Card = card;
    }
    public Action(int player, ClueType clue, int value)
    {
        TargetPlayer = player;
        Type = ActionType.Clue;
        Clue = clue;
        Value = value;
    }

}



class ActionResult
{
    // Laaazzzy
    public readonly Card Card;
    public readonly bool Accepted;
    public readonly bool NewCard;
    public readonly IReadOnlyList<int> SelectedCards;
    public ActionResult(Card card, bool new_card)
    {
        Card = card;
        NewCard = new_card;
    }
    public ActionResult(Card card, bool accepted, bool new_card)
    {
        Card = card;
        NewCard = new_card;
        Accepted = accepted;
    }
    public ActionResult(IReadOnlyList<int> selected)
    {
        SelectedCards = selected;
    }
}


interface IPlayer
{
    void Init(Game.Viewer game);
    
    Action RequestAction();

    void NotifyAction(int fromPlayer, Action action, ActionResult result);
}



class Game
{
    public class Viewer
    {
        Game game_;
        List<ReadOnlyCollection<Card>> hands_;
        ReadOnlyCollection<Card> discards_;
        ReadOnlyCollection<int> fireworks_;
        public Viewer(Game game, int player_ix)
        {
            game_ = game;
            ActualPlayerId = player_ix;
            discards_ = new ReadOnlyCollection<Card>(game.discards_);
            fireworks_ = new ReadOnlyCollection<int>(game.fireworks_);
            hands_ = new List<ReadOnlyCollection<Card>>();
            for (int i = 1; i < NumPlayers; i++)
            {
                hands_.Add(new ReadOnlyCollection<Card>(game.hands_[(i + player_ix) % NumPlayers]));
            }
        }
        public int NumPlayers
        {
            get { return game_.NumPlayers; }
        }
        public int ActualPlayerId { get; private set; }
        public IReadOnlyList<Card> GetHand(int player)
        {
            int requested_player = ActualPlayer(player, ActualPlayerId);
            if (requested_player == ActualPlayerId)
                throw new Exception("Cheating! Can't access own hand");
            return game_.hands_[requested_player];
        }
        public int CardsInHand
        {
            get { return game_.hands_[ActualPlayerId].Count; }
        }
        public int Clues
        {
            get { return game_.Clues; }
        }
        public int Lives
        {
            get { return game_.Lives; }
        }
        public int Score
        {
            get { return game_.Score; }
        }
        public IReadOnlyList<Card> Discards
        {
            get { return discards_; }
        }
        public IReadOnlyList<int> Fireworks
        {
            get { return fireworks_; }
        }
        public int RemainingCards
        {
            get { return game_.deck_.Count; }
        }
    }



    List<IPlayer> players_;
    List<Card> deck_;
    List<Card> discards_;
    List<int> fireworks_;
    List<List<Card>> hands_;

    System.Random rng_;

    public int Clues { get; private set; }
    public const int MaxClues = 8;

    public int Lives { get; private set; }
    public int Score { get; private set; }

    public int NumPlayers
    {
        get { return players_.Count; }
    }

    bool log_;
    void Log(string text, params object[] args)
    {
        if (log_)
            Console.WriteLine(text, args);
    }

    public Game(int seed, bool logging)
    {
        deck_ = new List<Card>();
        hands_ = new List<List<Card>>();
        players_ = new List<IPlayer>();
        discards_ = new List<Card>();
        fireworks_ = new List<int>();

        rng_ = new System.Random(seed);
        log_ = logging;

        for (int i = 0; i < 5; i++)
            fireworks_.Add(0);

        Lives = 3;
        Clues = MaxClues;

        for (int i = 0; i < 5; i++)
        {
            int[] nums = { 1, 1, 1, 2, 2, 3, 3, 4, 4, 5};
            for (int j = 0; j < 10; j++)
            {
                deck_.Add(new Card(i, nums[j]));
            }
        }
    }

    public void AddPlayer(IPlayer player)
    {
        players_.Add(player);
        hands_.Add(new List<Card>());
    }

    Card DealOneCard()
    {
        Card ret = deck_[deck_.Count - 1];
        deck_.RemoveAt(deck_.Count - 1);
        return ret;
    }

    void Shuffle()
    {
        for (int i = 0; i < deck_.Count; i++)
        {
            int newLoc = rng_.Next(0, deck_.Count);
            Card c = deck_[i];
            deck_[i] = deck_[newLoc];
            deck_[newLoc] = c;
        }
    }

    // Given a player number 'actual' in the game's numbering, convert to what 'viewer' thinks it is
    static int ApparentPlayer(int actual, int viewer)
    {
        return (4 + actual - viewer) % 4;
    }
    // Covnert the other way
    static int ActualPlayer(int apparent, int viewer)
    {
        return (apparent + viewer) % 4;
    }
    static int AdjustApparent(int apparent, int source, int dest)
    {
        return ApparentPlayer(ActualPlayer(apparent, source), dest);
    }

    public int Play()
    {
        int cards = 4;
        Shuffle();

        for (int i = 0; i < players_.Count; i++)
        {
            for (int j = 0; j < cards; j++)
            {
                hands_[i].Add(DealOneCard());
            }
            
            Log("Player {0} has hand {1}", i, string.Join(" ", hands_[i]));
        }

        for (int i = 0; i < players_.Count; i++)
        {
            players_[i].Init(new Viewer(this, i));
        }

        int remaining_turns = NumPlayers;
        int current_player = 0;
        while (Lives > 0 && (deck_.Count > 0 || remaining_turns-- > 0))
        {
            Action act = players_[current_player].RequestAction();
            ActionResult result = null;
            Card? new_card = null;
            switch (act.Type)
            {
                case ActionType.Play:
                    {
                        Card card = hands_[current_player][act.Card];
                        hands_[current_player].RemoveAt(act.Card);
                        Log("Player {0} played {1}", current_player, card);
                        if (fireworks_[card.Colour] + 1 == card.Number)
                        {
                            Log("   Success!");
                            fireworks_[card.Colour]++;
                            Score++;
                            if (Score == 25)
                                return 25; // Won!
                            if (fireworks_[card.Colour] == 5 && Clues < MaxClues)
                                Clues++;
                            result = new ActionResult(card, true, deck_.Count > 0);
                        }
                        else
                        {
                            Log("   Failed!");
                            Lives--;
                            discards_.Add(card);
                            if (Lives == 0)
                                return 0; // Died!
                            result = new ActionResult(card, false, deck_.Count > 0);
                        }
                        if (deck_.Count > 0)
                        {
                            new_card = DealOneCard();
                            hands_[current_player].Add(new_card.Value);
                            Log(" Received card {0}", new_card);
                        }
                    }
                    break;
                case ActionType.Discard:
                    {
                        if (Clues == MaxClues)
                            throw new Exception("Tried to discard with max clues");
                        Clues++;
                        Card card = hands_[current_player][act.Card];
                        hands_[current_player].RemoveAt(act.Card);
                        Log("Player {0} discarded {1}", current_player, card);
                        if (deck_.Count > 0)
                        {
                            new_card = DealOneCard();
                            hands_[current_player].Add(new_card.Value);
                            Log(" Received card {0}", new_card);
                        }
                        result = new ActionResult(card, deck_.Count > 0);
                    }
                    break;
                case ActionType.Clue:
                    {
                        if (Clues == 0)
                            throw new Exception("Tried to clue with none left");
                        Clues--;
                        List<int> ret = new List<int>();
                        int real_player = ActualPlayer(act.TargetPlayer, current_player);
                        if (real_player == current_player)
                            throw new Exception("Tried to clue self");
                        Log("Player {0} gave player {1} the clue '{2}'",
                            current_player, real_player, act.Clue == ClueType.Colour ? ((Colour)act.Value).ToString() : act.Value.ToString());
                        for (int i = 0; i < hands_[real_player].Count; i++)
                        {
                            if (act.Clue == ClueType.Colour)
                            {
                                if (hands_[real_player][i].Colour == act.Value)
                                    ret.Add(i);
                            }
                            else
                            {
                                if (hands_[real_player][i].Number == act.Value)
                                    ret.Add(i);
                            }
                        }
                        Log(" Matching cards: {0}", string.Join(", ", ret));
                        result = new ActionResult(new ReadOnlyCollection<int>(ret));
                    }
                    break;
            }
            for (int i = 0; i < NumPlayers; i++)
            {
                Action send_act = act.Type != ActionType.Clue ? act :
                    new Action(AdjustApparent(act.TargetPlayer, current_player, i), act.Clue, act.Value);
                players_[i].NotifyAction(ApparentPlayer(current_player, i), send_act, result);
            }
            current_player = (current_player + 1) % 4;
        }
        return Score; // Ran out of time
    }
}