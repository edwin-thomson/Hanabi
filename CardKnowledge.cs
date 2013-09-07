using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

class PossibleCard
{
    // [colour][number-1]
    bool[][] possible_;

    /*
    int[][] num_cards_seen_;

    // XXX do these
    int[] num_colours_seen_;
    int[] num_values_seen_;
    */
    static readonly int[] value_counts_ = { 3, 2, 2, 2, 1 };

    Card? known_card_;
    int? known_colour_;
    int? known_number_;

    bool stale_knowledge_;

    public bool IsKnownCard(out Card c)
    {
        RefreshKnowledge();
        c = default(Card);
        if (known_card_.HasValue)
            c = known_card_.Value;
        return known_card_.HasValue;
    }
    public bool IsKnownColour(out int colour)
    {
        RefreshKnowledge();
        colour = 0;
        if (known_colour_.HasValue)
            colour = known_colour_.Value;
        return known_colour_.HasValue;
    }
    public bool IsKnownNumber(out int number)
    {
        RefreshKnowledge();
        number = 0;
        if (known_number_.HasValue)
            number = known_number_.Value;
        return known_number_.HasValue;
    }


    public Card? KnownCard
    {
        get
        {
            RefreshKnowledge();
            return known_card_;
        }
    }
    public int? KnownColour
    {
        get
        {
            RefreshKnowledge();
            return known_colour_;
        }
    }
    public int? KnownNumber
    {
        get
        {
            RefreshKnowledge();
            return known_number_;
        }
    }

    void RefreshKnowledge()
    {
        if (!stale_knowledge_) return;

        int colour = -1;
        int number = -1;
        bool multiple_colour = false;
        bool multiple_number = false;

        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                if (possible_[i][j])
                {
                    if (colour == -1)
                        colour = i;
                    else if (colour != i)
                        multiple_colour = true;
                    if (number == -1)
                        number = j;
                    else if (number != j)
                        multiple_number = true;
                }
            }
        }
        Debug.Assert(colour != -1 && number != -1); // We shouldn't have elminated everything!
        if (!multiple_colour)
            known_colour_ = colour;
        if (!multiple_number)
            known_number_ = number + 1;
        if (!multiple_number && !multiple_colour)
        {
            known_card_ = new Card(colour, number + 1);
        }
        stale_knowledge_ = false;
    }


    public PossibleCard()
    {
        possible_ = new bool[5][];
      //  num_cards_seen_ = new int[5][];
      //  num_colours_seen_ = new int[5];
      //  num_values_seen_ = new int[5];
        for (int i = 0; i < 5; i++)
        {
      //      num_cards_seen_[i] = new int[5];
            possible_[i] = new bool[5];
            for (int j = 0; j < 5; j++)
            {
                possible_[i][j] = true;
            }
        }
    }
    public bool SetColour(int colour)
    {
        bool new_info = false;
        for (int i = 0; i < 5; i++)
        {
            if (i == colour)
                continue;
            for (int j = 0; j < 5; j++)
            {
                if (possible_[i][j])
                {
                    new_info = true;
                    possible_[i][j] = false;
                }
            }
        }
        if (new_info)
            stale_knowledge_ = true;
        return new_info;
    }
    public bool SetNumber(int number)
    {
        number--;
        bool new_info = false;
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                if (j == number) 
                    continue;
                if (possible_[i][j])
                {
                    new_info = true;
                    possible_[i][j] = false;
                }
            }
        }
        if (new_info)
            stale_knowledge_ = true;
        return new_info;
    }
    public bool SetCard(Card card)
    {
        bool new_info = false;
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                if (i == card.Colour && j == card.Number - 1)
                    continue;
                if (possible_[i][j])
                {
                    new_info = true;
                    possible_[i][j] = false;
                }
            }
        }
        if (new_info)
            stale_knowledge_ = true;
        return new_info;
    }
    public bool SetIsOneOf(IEnumerable<Card> cards)
    {
        bool new_info = false;
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                if (cards.Contains(new Card(i, j + 1)))
                    continue;
                if (possible_[i][j])
                {
                    new_info = true;
                    possible_[i][j] = false;
                }
            }
        }
        return new_info;
    }
    public bool SetIsNotOneOf(IEnumerable<Card> cards)
    {
        bool new_info = false;
        foreach (Card c in cards)
        {
            if (possible_[c.Colour][c.Number - 1])
            {
                new_info = true;
                possible_[c.Colour][c.Number - 1] = false;
            }
        }
        return new_info;
    }
    public bool EliminateColour(int colour)
    {
        bool new_info = false;
        for (int j = 0; j < 5; j++)
        {
            if (possible_[colour][j])
            {
                new_info = true;
                possible_[colour][j] = false;
            }
        }
        if (new_info)
            stale_knowledge_ = true;
        return new_info;
    }
    public bool EliminateNumber(int number)
    {
        number--;
        bool new_info = false;
        for (int j = 0; j < 5; j++)
        {
            if (possible_[j][number])
            {
                new_info = true;
                possible_[j][number] = false;
            }
        }
        if (new_info)
            stale_knowledge_ = true;
        return new_info;
    }
    public bool EliminateCard(Card card)
    {
        bool new_info = false;
        if (possible_[card.Colour][card.Number - 1])
        {
            new_info = true;
            possible_[card.Colour][card.Number - 1] = false;
        }
        if (new_info)
            stale_knowledge_ = true;
        return new_info;
    }
    public bool CouldBe(Card card)
    {
        return possible_[card.Colour][card.Number - 1];
    }
    public bool CouldBeIn(IEnumerable<Card> cards)
    {
        foreach (Card card in cards)
        {
            if (CouldBe(card))
                return true;
        }
        return false;
    }
    public bool MustBeIn(IEnumerable<Card> cards)
    {
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                if (possible_[i][j])
                {
                    if (!cards.Contains(new Card(i, j + 1)))
                        return false;
                }
            }
        }
        return true;
    }
    /*
    public bool SeenCard(Card card)
    {
        bool new_info = false;
        num_cards_seen_[card.Colour][card.Number - 1]++;
        Debug.Assert(num_cards_seen_[card.Colour][card.Number - 1] <= value_counts_[card.Number - 1]);
        if (num_cards_seen_[card.Colour][card.Number - 1] == value_counts_[card.Number - 1])
        {
            new_info = EliminateCard(card);
        }

        return new_info;
    }
    */

    public bool DeduceFromOtherCards(IEnumerable<Card> cards, IEnumerable<PossibleCard> possibles)
    {
        RefreshKnowledge();
        // If we aleady know what the card is, don't bother with any of this
        if (known_card_.HasValue) return false;

        int[][] num_cards_seen = new int[5][];
        for (int i = 0; i < 5; i++)
            num_cards_seen[i] = new int[5];

        int[] num_colours_seen = new int[5];
        int[] num_values_seen = new int[5];
        foreach (Card card in cards)
        {
            num_cards_seen[card.Colour][card.Number - 1]++;
            num_colours_seen[card.Colour]++;
            num_values_seen[card.Number - 1]++;
        }
        foreach (PossibleCard pc in possibles)
        {
            if (this == pc) continue;
            Card card;
            int number;
            int colour;
            if (pc.IsKnownCard(out card))
                num_cards_seen[card.Colour][card.Number - 1]++;
            if (pc.IsKnownColour(out colour))
                num_colours_seen[colour]++;
            if (pc.IsKnownNumber(out number))
                num_values_seen[number - 1]++;
        }
        bool new_info = false;
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                Debug.Assert(num_cards_seen[i][j] <= value_counts_[j]);
                if (num_cards_seen[i][j] == value_counts_[j])
                    new_info = EliminateCard(new Card(i,j+1)) || new_info;
            }
        }
        for (int i = 0; i < 5; i++)
        {
            Debug.Assert(num_colours_seen[i] <= 10);
            if (num_colours_seen[i] == 10)
                new_info = EliminateColour(i) || new_info;

            Debug.Assert(num_values_seen[i] <= 5 * value_counts_[i]);
            if (num_values_seen[i] == 5 * value_counts_[i])
                new_info = EliminateNumber(i + 1) || new_info;
        }
        return new_info;
    }

    public static bool MakeDeductionsForSet(IEnumerable<PossibleCard> possibles_for, IEnumerable<Card> cards, IEnumerable<PossibleCard> other_possibles)
    {
        IEnumerable<PossibleCard> all_possibles = possibles_for.Concat(other_possibles);
        bool any_new_info = false;
        bool new_info;
        do
        {
            new_info = false;
            foreach (var poss in possibles_for)
                new_info = poss.DeduceFromOtherCards(cards, all_possibles) || new_info;
            any_new_info = any_new_info || new_info;
        } while (new_info);
        return any_new_info;
    }


    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 5;i++)
        {
            sb.Append("\n|");
            for (int j = 0; j < 5; j++)
                if (possible_[i][j])
                    sb.Append('0');
                else
                    sb.Append('.');
            sb.Append("|");
        }
        sb.Append("\n");
        return sb.ToString();
    }

}