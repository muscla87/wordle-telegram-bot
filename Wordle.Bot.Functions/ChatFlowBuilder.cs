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
                    .Subtree(NewGameBehaviour())
                    .Subtree(ShowGameBoardBehaviour())
                    .Subtree(HelpBehaviour())
                    .Subtree(StatisticsBehaviour())
                    .Subtree(ChangeDictionaryBehaviour())
                    .Subtree(GameBehaviour())
                .End()
                .Build();
    }

    private IBehaviour<GameContext> StartBehaviour()
    {
        return FluentBuilder.Create<GameContext>()
            .Sequence("Start")
                .Condition("Is Start Command?", (context) => context.IsCommand("start"))
                .Do("SendWelcomeMessage", (context) => context.SendWelcomeMessage())
                .Do("SaveInitialPlayerInformation", (context) => context.SaveInitialPlayerInformation())
            .End()
            .Build();
    }

    private IBehaviour<GameContext> NewGameBehaviour()
    {
        return FluentBuilder.Create<GameContext>()
            .Sequence("NewGame")
                .Condition("Is NewGame Command?", (context) => context.IsCommand("newgame"))
                .AlwaysSucceed("Start New Game")
                    .Selector("StartOrResume")
                        .Sequence("Start New Game")
                            .Do("Load Game", (context) => context.LoadGame())
                            .Invert("Not IsGameInProgress")
                                .Condition("IsGameInProgress?", (context) => context.IsGameInProgress())
                            .End()
                            .Do("SendInstructions", (context) => context.SendInstructions())
                            .Do("StartNewGame", (context) => context.StartNewGame())
                        .End()
                        .Sequence("NotifyStarted")
                            .Do("SendGameStatus", (context) => context.SendGameSummary())
                            .Do("SendAlreadyStartedMessage", (context) => context.SendGameAlreadyStartedMessage())
                        .End()
                    .End()
                .End()
            .End()
            .Build();
    }

    private IBehaviour<GameContext> ShowGameBoardBehaviour()
    {
        return FluentBuilder.Create<GameContext>()
            .Sequence("ShowGameBoard")
                .Condition("Is ShowGameBoard Command?", (context) => context.IsCommand("showboard"))
                .Do("Load Game", (context) => context.LoadGame())
                .Do("SendGameStatus", (context) => context.SendGameSummary())
            .End()
            .Build();
    }

    private IBehaviour<GameContext> HelpBehaviour()
    {
        return FluentBuilder.Create<GameContext>()
            .Sequence("Help")
                .Condition("Is Help Command?", (context) => context.IsCommand("help"))
                .Do("SendInstructions", (context) => context.SendInstructions())
            .End()
            .Build();
    }

    private IBehaviour<GameContext> StatisticsBehaviour()
    {
        return FluentBuilder.Create<GameContext>()
            .Sequence("Statistics")
                .Condition("Is Statistics Command?", (context) => context.IsCommand("stats"))
                .Do("SendStatistics", (context) =>  context.SendStatistics())
            .End()
            .Build();
    }

    private IBehaviour<GameContext> GameBehaviour()
    {
        return FluentBuilder.Create<GameContext>()
            .Sequence("Game")
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

    private IBehaviour<GameContext> ChangeDictionaryBehaviour()
    {
        return FluentBuilder.Create<GameContext>()
            .Sequence("ChangeDictionary")
                .Condition("Is Change Dictionary Command?", (context) => context.IsCommand("changedictionary"))
                .Selector("Apply New Dictionary or Fail")
                    .Do("SetDictionary", (context) => context.SetDictionary())
                    .Do("Display wrong command", (context) => context.ResetGame())
                .End()
            .End()
            .Build();
    }

}
