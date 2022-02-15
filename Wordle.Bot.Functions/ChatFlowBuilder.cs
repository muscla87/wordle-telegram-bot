using System;
using BehaviourTree;
using BehaviourTree.FluentBuilder;

namespace Wordle.Bot;

public class ChatFlowBuilder
{
    public IBehaviour<GameContext> Build()
    {
         return FluentBuilder.Create<GameContext>()
                .PrioritySelector("Root")
                    .Subtree(StartBehaviour())
                    .Subtree(HelpBehaviour())
                    .Subtree(StatisticsBehaviour())
                    .Subtree(GameBehaviour())
                    .Subtree(ResetBehaviour())
                .End()
                .Build();
    }

    private IBehaviour<GameContext> StartBehaviour()
    {
        return FluentBuilder.Create<GameContext>()
            .Sequence("Start")
                .Condition("Is Start Command?", (context) => context.IsCommand("start"))
                .Do("SetCurrentCommandContext(Start)", (context) => context.SetCurrentCommandContext("start"))
                .Do("SendWelcomeMessage", (context) => context.SendWelcomeMessage())
                .Do("SendInstructions", (context) => context.SendInstructions())
                .AlwaysSucceed("Start New Game")
                    .Sequence("Start New Game")
                        .Do("Load Game", (context) => context.LoadGame())
                        .Invert("Not IsGameInProgress")
                            .Condition("IsGameInProgress?", (context) => context.IsGameInProgress())
                        .End()
                        .Do("StartNewGame", (context) => context.StartNewGame())
                    .End()
                .End()
                .Do("SetCurrentCommandContext(Null)", (context) => context.SetCurrentCommandContext(null))
            .End()
            .Build();
    }

    private IBehaviour<GameContext> HelpBehaviour()
    {
        return FluentBuilder.Create<GameContext>()
            .Sequence("Help")
                .Condition("Is Help Command?", (context) => context.IsCommand("help"))
                .Do("SetCurrentCommandContext(Help)", (context) => context.SetCurrentCommandContext("help"))
                .Do("SendInstructions", (context) => context.SendInstructions())
                .Do("SetCurrentCommandContext(Null)", (context) => context.SetCurrentCommandContext(null))
            .End()
            .Build();
    }

    private IBehaviour<GameContext> StatisticsBehaviour()
    {
        return FluentBuilder.Create<GameContext>()
            .Sequence("Statistics")
                .Condition("Is Statistics Command?", (context) => context.IsCommand("stats"))
                .Do("SetCurrentCommandContext(Statistics)", (context) => context.SetCurrentCommandContext("stats"))
                .Do("SendStatistics", (context) =>  context.SendStatistics())
                .Do("SetCurrentCommandContext(Null)", (context) => context.SetCurrentCommandContext(null))
            .End()
            .Build();
    }

    private IBehaviour<GameContext> GameBehaviour()
    {
        return FluentBuilder.Create<GameContext>()
            .Sequence("Game")
                .Condition("Is Command Context = NULL?", (context) => context.CurrentCommand == null)
                .Do("Load Game", (context) => context.LoadGame())
                .AlwaysSucceed("GameProgress")
                    .Sequence("GameProgress")
                        .Condition("IsGameInProgress?", (context) => context.IsGameInProgress())
                        .Do("ProcessWord", (context) => context.EvaluateWordInMessage())
                        .Do("Save Game", (context) => context.SaveGame())
                        .Condition("IsGameInProgress?", (context) => context.IsGameInProgress())
                        .Do("SendGameStatus", (context) => context.SendGameSummary())
                    .End()
                .End()
                .Sequence("GameEnd")
                    .Invert("Not IsGameInProgress")
                        .Condition("IsGameInProgress?", (context) => context.IsGameInProgress())
                    .End()
                    .Do("SendGameStatus", (context) => context.SendGameSummary())
                    .Do("EndOfGameMessage", (context) => context.SendEndOfGameMessage())
                .End()
            .End()
            .Build();
    }

    private IBehaviour<GameContext> ResetBehaviour()
    {
        return FluentBuilder.Create<GameContext>()
            .Sequence("Reset")
                .Selector("CheckCommandOrContext")
                    .Sequence("Ask For Confirmation")
                        .Condition("Is Reset Command?", (context) => context.IsCommand("reset"))
                        .Do("SendResetConfirmation", (context) => context.SendResetConfirmationMessage())
                    .End()
                    .Condition("Is Command Context = Reset?", (context) => context.CurrentCommand == "reset")
                .Do("SetCurrentCommandContext(Reset)", (context) => context.SetCurrentCommandContext("reset"))
                .Condition("Wait for Confirmation", (context) => context.IsMessage("yes"))
                .Do("ResetData", (context) => context.ResetGame())
            .End()
            .Build();
    }

}
