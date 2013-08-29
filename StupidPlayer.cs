using System;
using System.Collections.Generic;


class StupidPlayer : IPlayer
{
    Game.Viewer view_;
    public void Init(Game.Viewer view)
    {
        view_ = view;
    }
    public Action RequestAction()
    {
        if (view_.Lives > 1 || view_.Score == 0)
            return new Action(ActionType.Play, 0);
        if (view_.Clues > 0)
            return new Action(1, ClueType.Colour, 1);
        else
            return new Action(ActionType.Discard, 0);
    }
    public void NotifyAction(int player, Action action, ActionResult result)
    {
    }

}

