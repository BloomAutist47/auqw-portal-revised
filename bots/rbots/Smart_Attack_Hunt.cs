using System;
using RBot;
using System.Linq;
using RBot.Items;

public class SmartAttackHunt {
    
    /// This bot has two modes: 
    /// - If you target a monster in-game before activating the bot, it will hunt that monster all across the map (private room is advised).
    /// - If you dont target a monster in-game before hand, it will attack any monster on screen and will stay on said screen.
    /// Note that the bot will automatically turn in any quests that are completed while the bot is active.
    /// ConsiderBossHPTreshhold is the amount of HP an enemy needs to have in order to be considered a boss, this is used for hunting delay
    /// Its recommended to use attack mode if you are farming a boss for a quest
    
    //-----------EDIT BELOW-------------//
    public int TurnInAttempts = 10;
    public readonly int[] SkillOrder = { 2, 4, 3, 1 };

    public bool AutoQuestComplete = true;
    public bool ConsumeBoost = false;
    public int SelectedBoost = (int)BoostIDs.DailyXP60; // Check line 144 to see what to place here, only replace the stuff after (int)BoostIDs
    public bool DropGrabber = false;
    public string[] GrabTheseDrops = {
        "Item Name Here",
        "Item Name Here",
        "Item Name Here",
        "Item Name Here",
        "Item Name Here"
        //If you need more items than 5, just add more items to this list
     };
    //-----------EDIT ABOVE-------------//

    public ScriptInterface bot => ScriptInterface.Instance;
    public string Target;
    public void ScriptMain(ScriptInterface bot){
        bot.Options.SafeTimings = true;
        bot.Options.RestPackets = true;
        bot.Options.InfiniteRange = true;
        bot.Options.ExitCombatBeforeQuest = true;
        
        

        if (DropGrabber)
            GetDropList(GrabTheseDrops);
        if (ConsumeBoost)
        {   
            if (SelectedBoost == 19189 || SelectedBoost == 22448 || SelectedBoost == 27552)
                UseBoost(SelectedBoost, RBot.Items.BoostType.Experience, true);
            else if (SelectedBoost == 19761 || SelectedBoost == 22447 || SelectedBoost == 27555)
                UseBoost(SelectedBoost, RBot.Items.BoostType.Class, true);
            else if (SelectedBoost == 19762 || SelectedBoost == 22449 || SelectedBoost == 27553)
                UseBoost(SelectedBoost, RBot.Items.BoostType.Reputation, true);
            else if (SelectedBoost == 19763 || SelectedBoost == 22450 || SelectedBoost == 27554)
                UseBoost(SelectedBoost, RBot.Items.BoostType.Gold, true);
        }
            

        SkillList(SkillOrder);
        DeathHandler();
        
        FormatLog(Text: "Script Started", Title: true);
        if (bot.Player.HasTarget)
            Target = bot.Player.Target.Name;
        
    // Hunting
        if (Target != null)
        {
            FormatLog("Hunting", $"[{Target}]");
            while(!bot.ShouldExit())
            {
                bot.Player.Hunt(Target);
            // Auto Quest Complete
                if (AutoQuestComplete)
                {
                    if (bot.Quests.ActiveQuests.Count >= 1)
                    {
                        foreach (var Quest in bot.Quests.ActiveQuests)
                        {
                            int QuestID = Quest.ID;
                            if (bot.Quests.CanComplete(QuestID))
                            {
                                ModSafeQuestComplete(QuestID);
                            }
                        }
                    }
                }
            }
        }
        
    // Attacking
        else
        {
            FormatLog("Attacking", "[Everything]", Tabs: 1);
            while(!bot.ShouldExit())
            {
                bot.Player.SetSpawnPoint();
                bot.Player.Attack("*");
            // Auto Quest Complete
                if (AutoQuestComplete)
                {
                    if (bot.Quests.ActiveQuests.Count >= 1)
                    {
                        foreach (var Quest in bot.Quests.ActiveQuests)
                        {
                            int QuestID = Quest.ID;
                            if (bot.Quests.CanComplete(QuestID))
                            {
                                ModSafeQuestComplete(QuestID);
                            }
                        }
                    }
                }
            }
        }	
    }
    
    /*------------------------------------------------------------------------------------------------------------
                                                     Required Functions
    ------------------------------------------------------------------------------------------------------------*/
    //These functions are required for this bot to function.

    /// <summary>
    /// Uses a boost with the given ID.
    /// </summary>
    /// <param name="boostID">ID of the Boost</param>
    /// <param name="type">Type of the Boost</param>
    /// <param name="useMultiple">Whether use more than one boost</param>
    public void UseBoost(int boostID, BoostType type, bool useMultiple = true)
    {
        if (useMultiple)
        {
            bot.RegisterHandler(5, b =>
            {
                if (!b.Player.IsBoostActive(type))
                    b.Player.UseBoost(boostID);
            }); 
        }
        else
        {
            bot.Player.UseBoost(boostID);
        }
    }

    public enum BoostIDs
    {
        DailyXP60 = 19189,      // Daily XP Boost! (1 hr)
        XP20 = 22448,           // XP Boost! (20 min)
        XP60 = 27552,           // XP Boost! (60 min)
        DoomClass60 = 19761,    // Doom CLASS Boost! (1 hr)
        Class20 = 22447,        // Daily Login Class Boost
        Class60 = 27555,        // Class Boost! (60 Min)
        DoomREP60 = 19762,      // Doom REP Boost! (1 hr)
        REP20 = 22449,          // REP Boost! (20 min)
        REP60 = 27553,          // REP Boost! (60 min)
        DoomGold60 = 19763,     // Doom GOLD Boost! (1 hr)
        Gold20 = 22450,         // GOLD Boost! (20 min)
        Gold60 = 27554          // GOLD Boost! (60 min)
    }

    /// <summary>
    /// Spams Skills when in combat. You can get in combat by going to a cell with monsters in it with bot.Options.AggroMonsters enabled or using an attack command against one.
    /// </summary>
    public void SkillList(params int[] Skillset)
    {
        if(bot.Handlers.Any(h => h.Name == "Skill Handler"))
            bot.Handlers.RemoveAll(h => h.Name == "Skill Handler");
        bot.RegisterHandler(1, b => {
            if (bot.Player.InCombat)
            {
                foreach (var Skill in Skillset)
                {
                    if (bot.Player.CanUseSkill(Skill))
                        bot.Player.UseSkill(Skill);
                }
            }
        }, "Skill Handler");
    }
    
    /// <summary>
    /// Checks if items in an array have dropped every second and picks them up if so. GetDropList is recommended.
    /// </summary>
    public void GetDropList(params string[] GetDropList)
    {
        if(bot.Handlers.Any(h => h.Name == "Drop Handler"))
            bot.Handlers.RemoveAll(h => h.Name == "Drop Handler");
        bot.RegisterHandler(4, b => {
            foreach (string Item in GetDropList)
            {
                if (bot.Player.DropExists(Item)) bot.Player.Pickup(Item);
            }
            bot.Player.RejectExcept(GetDropList);
        }, "Drop Handler");
    }
    
    /// <summary>
    /// Attempts to complete the quest with the set amount of {TurnInAttempts}. If it fails to complete, logs out. If it successfully completes, re-accepts the quest and checks if it can be completed again.
    /// </summary>
    public void ModSafeQuestComplete(int QuestID)
    {
        //Must have the following functions in your script:
        //ExitCombat

        string Cell = bot.Player.Cell;
        string Pad = bot.Player.Pad;
        int i = 0;
        while (bot.Quests.CanComplete(QuestID))
        {
            if (bot.Player.Cell != "Wait" || bot.Player.InCombat)
                ExitCombat();
            bot.Quests.EnsureComplete(QuestID);
            i++;
            if (i > TurnInAttempts)
            {
                FormatLog("Quest", $"Turning in Quest {QuestID} failed. Logging out");
                bot.Player.Logout();
            }
        }
        FormatLog("Quest", $"Turning in Quest {QuestID} successful.");
        while (!bot.Quests.IsInProgress(QuestID)) bot.Quests.EnsureAccept(QuestID);
        bot.Player.Jump(Cell, Pad);
    }

    /// <summary>
    /// Logs following a specific format. No more than 3 tabs allowed.
    /// </summary>
    public void FormatLog(string Topic = "FormatLog", string Text = "Missing Input", int Tabs = 2, bool Title = false, bool Followup = false)
    {
        if (Title)
            bot.Log($"[{DateTime.Now:HH:mm:ss}] -----{Text}-----");
        else 
        {
            Tabs = Tabs > 3 ? 3 : Tabs;
            string TabPlace = "";
            for (int i = 0; i < Tabs; i++) 
                TabPlace += "\t";
            if (Followup) 
                bot.Log($"[{DateTime.Now:HH:mm:ss}] ↑ {TabPlace}{Text}");
            else 
                bot.Log($"[{DateTime.Now:HH:mm:ss}] {Topic} {TabPlace}{Text}");
        }
    }
    
    /// <summary>
    /// Exits Combat by jumping cells.
    /// </summary>
    public void ExitCombat()
    {
        bot.Options.AggroMonsters = false;
        bot.Player.Jump("Wait", "Spawn");
        bot.Wait.ForCellChange("Wait");
        bot.Wait.ForCombatExit();
    }

    public void DeathHandler()
    {
        bot.RegisterHandler(2, b =>
        {
            if (bot.Player.State==0)
            {
                bot.Player.SetSpawnPoint();
                ExitCombat();
                bot.Sleep(12000);
            }
        });
    }
}