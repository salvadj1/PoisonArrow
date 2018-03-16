using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fougerite;
using Fougerite.Events;
using UnityEngine;
using System.IO;

namespace PoisonArrow
{
    public class PoisonArrowClass : Fougerite.Module
    {
        public override string Name { get { return "PoisonArrow"; } }
        public override string Author { get { return "Salva/Juli"; } }
        public override string Description { get { return "PoisonArrow"; } }
        public override Version Version { get { return new Version("1.0"); } }

        public string red = "[color #B40404]";
        public string blue = "[color #81F7F3]";
        public string green = "[color #82FA58]";
        public string yellow = "[color #F4FA58]";
        public string orange = "[color #FF8000]";
        public string pink = "[color #FA58F4]";
        public string white = "[color #FFFFFF]";

        public IniParser Settings;
        System.Random rnd;

        public int PossibilityOfPoisoning = 50;
        public int AmountOfVenne = 4;
        public bool AvoidBypass = true;
        public int TimeToAvoidBypass = 2;

        public List<string> PoisonedIDs;

        public override void Initialize()
        {
            rnd = new System.Random();
            PoisonedIDs = new List<string>();
            Hooks.OnServerInit += OnServerInit;
            Hooks.OnCommand += OnCommand;
            Hooks.OnPlayerSpawned += OnPlayerSpawned;
            Hooks.OnPlayerHurt += OnPlayerHurt;
            Hooks.OnPlayerKilled += OnPlayerKilled;
        }
        public override void DeInitialize()
        {
            Hooks.OnServerInit -= OnServerInit;
            Hooks.OnCommand -= OnCommand;
            Hooks.OnPlayerSpawned -= OnPlayerSpawned;
            Hooks.OnPlayerHurt -= OnPlayerHurt;
            Hooks.OnPlayerKilled -= OnPlayerKilled;
        }

        public void OnServerInit()
        {
            ReloadConfig();
        }
        private void ReloadConfig()
        {
            if (!File.Exists(Path.Combine(ModuleFolder, "Settings.ini")))
            {
                File.Create(Path.Combine(ModuleFolder, "Settings.ini")).Dispose();
                Settings = new IniParser(Path.Combine(ModuleFolder, "Settings.ini"));
                Settings.AddSetting("PoisonArrow", "PossibilityOfPoisoning(0-100)", "50");
                Settings.AddSetting("PoisonArrow", "AmountOfVenne(0-10)", "3");
                Settings.AddSetting("PoisonArrow", "AvoidBypass", "true");
                Settings.AddSetting("PoisonArrow", "TimeToAvoidBypass", "2");
                Settings.Save();
                Logger.Log(Name + " Plugin: New Settings File Created!");
                ReloadConfig();
            }
            else
            {
                Settings = new IniParser(Path.Combine(ModuleFolder, "Settings.ini"));
                if (Settings.ContainsSetting("PoisonArrow", "PossibilityOfPoisoning(0-100)") &&
                    Settings.ContainsSetting("PoisonArrow", "AmountOfVenne(0-10)") &&
                    Settings.ContainsSetting("PoisonArrow", "AvoidBypass") &&
                    Settings.ContainsSetting("PoisonArrow", "TimeToAvoidBypass"))
                {

                    try
                    {
                        PossibilityOfPoisoning = int.Parse(Settings.GetSetting("PoisonArrow", "PossibilityOfPoisoning(0-100)"));
                        AmountOfVenne = int.Parse(Settings.GetSetting("PoisonArrow", "AmountOfVenne(0-10)"));
                        AvoidBypass = Settings.GetBoolSetting("PoisonArrow", "AvoidBypass");
                        TimeToAvoidBypass = int.Parse(Settings.GetSetting("PoisonArrow", "TimeToAvoidBypass"));
                        Logger.Log(Name + " Plugin: Settings file Loaded!");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(Name + " Plugin: Detected a problem in the configuration");
                        Logger.Log("ERROR -->" + ex.Message);
                        File.Delete(Path.Combine(ModuleFolder, "Settings.ini"));
                        Logger.Log(Name + " Plugin: Deleted the old configuration file");
                        ReloadConfig();
                    }
                }
                else
                {
                    Logger.LogError(Name + " Plugin: Detected a problem in the configuration (lost key)");
                    File.Delete(Path.Combine(ModuleFolder, "Settings.ini"));
                    Logger.LogError(Name + " Plugin: Deleted the old configuration file");
                    ReloadConfig();
                }
                return;
            }
        }
        public void OnCommand(Fougerite.Player player, string cmd, string[] args)
        {
            if (!player.Admin) { return; }
            if (cmd == "pa" || cmd == "poison" || cmd == "arrow")
            {
                if (args.Length == 0)
                {
                    player.MessageFrom(Name, "/pa " + orange + " List of commands");
                    player.MessageFrom(Name, "/pa reload " + orange + " Reload and apply the Settings");
                    player.MessageFrom(Name, "/pa test " + orange + " Utility to simulate an arrow impact yourself");
                    player.MessageFrom(Name, "/pa list " + orange + " List of the current players poisoned");
                }
                else
                {
                    if (args[0] == "reload")
                    {
                        ReloadConfig();
                        player.MessageFrom(Name, orange + "Settings has been Reloaded :)");
                    }
                    else if (args[0] == "test")
                    {
                        var possibility = rnd.Next(0, 100);

                        if (possibility < PossibilityOfPoisoning)
                        {

                            player.AdjustPoisonLevel(AmountOfVenne);
                            player.MessageFrom(Name, "Poisoned: " + player.IsPoisoned);
                            player.MessageFrom(Name, "AmountOfVenne: " + AmountOfVenne);
                            player.MessageFrom(Name, "Possibility: " + possibility);
                            player.MessageFrom(Name, "AvoidBypass: " + AvoidBypass.ToString());
                            player.MessageFrom(Name, "TimeToAvoidBypass: " + TimeToAvoidBypass + " mins.");
                            if (AvoidBypass)
                            {
                                PoisonedIDs.Add(player.SteamID);
                                var listpois = new Dictionary<string, object>();
                                listpois["pID"] = player.SteamID;
                                Timer1(TimeToAvoidBypass * 60000, listpois).Start();
                                player.MessageFrom(Name, "The player will be registered for" + TimeToAvoidBypass + " minutes to avoid bypass");
                            }
                        }
                        else
                        {
                            player.MessageFrom(Name, orange + "You were lucky and you were not poisoned");
                        }
                    }
                    else if (args[0] == "list")
                    {
                        foreach (var xx in Server.GetServer().Players)
                        {
                            if (PoisonedIDs.Contains(xx.SteamID))
                            {
                                player.MessageFrom(Name, "Player: " + xx.Name + " ID: " + xx.SteamID);
                            }
                        }
                        player.MessageFrom(Name, orange + "End of list");
                    }
                }
            }
        }
        public void OnPlayerSpawned(Fougerite.Player pl,SpawnEvent se)
        {
            if (PoisonedIDs.Contains(pl.SteamID))
            {
                pl.AdjustPoisonLevel(AmountOfVenne);
                pl.MessageFrom(Name, red + "WARNING " + white + " You are still poisoned.");
            }
        }
        public void OnPlayerHurt(HurtEvent he)
        {
            if (he.WeaponName == "Hunting Bow" && he.AttackerIsPlayer && !he.VictimIsSleeper)
            {
                Fougerite.Player attacker = (Fougerite.Player)he.Attacker;
                Fougerite.Player victim = (Fougerite.Player)he.Victim;
                if (victim.IsOnline && !victim.IsDisconnecting && victim.IsAlive)
                {
                    var possibility = rnd.Next(0, 100);
                    if (possibility < PossibilityOfPoisoning)
                    {
                        victim.AdjustPoisonLevel(AmountOfVenne);
                        victim.Message("You have been poisoned with an PoisonArrow");
                        attacker.Message("You poisoned your victim with a PoisonArrow");
                        if (AvoidBypass)
                        {
                            //asegurarse que el jugador no intenta desconectarse para evitar su envenenamiento
                            PoisonedIDs.Add(victim.SteamID);
                            var listpois = new Dictionary<string, object>();
                            listpois["pID"] = victim.SteamID;
                            Timer1(TimeToAvoidBypass * 60000, listpois).Start();
                        }
                    }
                }
            }
        }
        public void OnPlayerKilled(HurtEvent he)
        {
            if (he.VictimIsPlayer || !he.VictimIsSleeper)
            {
                Fougerite.Player victim = (Fougerite.Player)he.Victim;
                if (PoisonedIDs.Contains(victim.SteamID))
                {
                    PoisonedIDs.RemoveAll(t => PoisonedIDs.Contains(victim.SteamID));// se asegura de quitar todas
                }
            }
        }

        public TimedEvent Timer1(int timeoutDelay, Dictionary<string, object> args)
        {
            TimedEvent timedEvent = new TimedEvent(timeoutDelay);
            timedEvent.Args = args;
            timedEvent.OnFire += RemovePlayerFromPosisonedList;
            return timedEvent;
        }
        public void RemovePlayerFromPosisonedList(TimedEvent e)
        {
            var listpois = e.Args;
            e.Kill();
            var ID = listpois["pID"];
            //PoisonedIDs.Remove(ID.ToString()); metodo antiguo
            PoisonedIDs.RemoveAll(t => PoisonedIDs.Contains(ID.ToString()));// se asegura de quitar todas
        }
    }
}
