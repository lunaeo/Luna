using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoonScript;

namespace LunaServer
{
    using EndlessOnline;
    using EndlessOnline.Communication;
    using EndlessOnline.Domain.Character;
    using EndlessOnline.Replies;
    using Events;
    using LunaServer.Addons;
    using Utilities;

    public class World
    {
        public GameServer GameServer { get; }
        public MoonScriptEngine ScriptEngine { get; }
        public ConcurrentBag<Page> GlobalScripts { get; }
        public ConcurrentBag<Page> MapScripts { get; }
        public IDisposable PingTimer { get; private set; }

        public World(GameServer server)
        {
            this.GameServer = server;
            this.ScriptEngine = new MoonScriptEngine();
            this.GlobalScripts = new ConcurrentBag<Page>();
            this.MapScripts = new ConcurrentBag<Page>();

            // setup global scripts
            foreach (var filename in Directory.GetFiles(Path.Combine("config", "scripts", "global"), "*.*", SearchOption.AllDirectories))
            {
                var script_name = Path.GetFileNameWithoutExtension(filename);

                if (script_name.All(char.IsDigit))
                {
                    this.GameServer.Console.Warning("The script at {path} has an invalid filename.");
                }
                else
                {
                    this.GlobalScripts.Add(this.LoadScript(filename, null));
                    this.GameServer.Console.Debug("MoonScript global script loaded: {name}", script_name);
                }
            }

            foreach (var category in Enum.GetValues(typeof(TriggerCategory)).Cast<TriggerCategory>().Where(t => t != TriggerCategory.Undefined))
                this.GameServer.Console.Debug("{0} {name} triggers defined.", this.GlobalScripts.First().Handlers.Count(t => t.Key.Category == category), category);

            // When everything is starting up,
            foreach (var script in this.GlobalScripts)
                script.Execute(null, null, 0);

            this.SetupPingTimer();
        }

        public Page LoadScript(string filename, CauseTriggerDiscoveryHandler causeTriggerDiscoveryHandler)
        {
            var script_name = Path.GetFileNameWithoutExtension(filename);
            var script_text = File.ReadAllText(filename);
            var debug_ignore = new List<(int category, int id)>();

            foreach (var entry in this.GameServer.ConsoleConfiguration.DebugIgnoreConsoleTriggers)
            {
                var split = entry.Split(':');

                if (!int.TryParse(split[0], out var category))
                    continue;

                if (!int.TryParse(split[1], out var id))
                    continue;

                debug_ignore.Add((category, id));
            }

            var page = this.ScriptEngine.LoadFromString(script_text,
                // trigger execution log for debugging
                (s, paragraph) =>
                {
                    foreach (var trigger in paragraph)
                    {
                        if (debug_ignore.Contains(((int)trigger.Category, trigger.Id)))
                            continue;

                        this.GameServer.Console.Debug("A trigger is being executed. Page: {page} Category: {cat} Id: {id} Desc: ({desc})",
                            script_name, trigger.Category, trigger.Id, s.Handlers.FirstOrDefault(t => t.Key.Equals(trigger)).Key.Description ?? "");
                    }
                },
                // cause trigger discovery
                (s, paragraph, trigger) =>
                {
                    causeTriggerDiscoveryHandler?.Invoke(s, paragraph, trigger);
                });

            page.Name = script_name;
            page.DefaultArea = new Area();
            page.DefaultArea.Points = new List<Point>() { new Point(-1, -1) };

            page.OnVariable += (trigger, type, key) =>
            {
                switch (type)
                {
                    case VariableType.Number:
                        get_number_variable(this.GameServer, page, key, out var number_variable);
                        return number_variable;

                    case VariableType.Message:
                        get_message_variable(this.GameServer, page, key, out var message_variable);
                        return message_variable;

                    default:
                        throw new InvalidOperationException();
                }
            };

            static IEnumerable<Character> players_around_inclusive(Character character)
                => character.Map.Characters.ToList().Where(t => t.InRange(character));

            static IEnumerable<Character> players_around_exclusive(Character character)
                => character.Map.Characters.ToList().Where(t => t.Session.PlayerId != character.Session.PlayerId && t.InRange(character));

            static bool set_message_variable(GameServer server, Page page, string key, string value)
            {
                var check_variable = get_message_variable(server, page, key, out var variable);
                page.SetMessageVariable(key, value);
                return true;
            }

            static bool get_message_variable(GameServer server, Page page, string key, out object var)
            {
                var = "";

                if (page.Variables.Where(t => t.Type == VariableType.Message).Any(t => t.Key == key))
                {
                    var = (string)page.Variables.Where(t => t.Type == VariableType.Message).First(t => t.Key == key).Value;
                    return true;
                }

                return false;
            }

            static bool set_number_variable(GameServer server, Page page, string key, object value)
            {
                if (key.EndsWith(".x"))
                {
                    var variable = page.Variables.Where(t => t.Type == VariableType.Number).First(t => t.Key == key[0..^2]);

                    if (variable == null)
                    {
                        server.Console.Warning("Script: {name} - variable {key} was accessed and was not previously defined.", page.Name, key);
                        return false;
                    }

                    if (value is double)
                        page.SetVariable(variable.Key, new IntVariable(Convert.ToInt32(value), ((IntVariable)variable.Value).Y));
                    else if (value is int)
                        page.SetVariable(variable.Key, new IntVariable((int)value, ((IntVariable)variable.Value).Y));
                    else if (value is IntVariable)
                        page.SetVariable(variable.Key, new IntVariable(((IntVariable)value).X, ((IntVariable)value).Y));
                    return true;
                }
                else if (key.EndsWith(".y"))
                {
                    var variable = page.Variables.Where(t => t.Type == VariableType.Number).First(t => t.Key == key[0..^2]);

                    if (variable == null)
                    {
                        server.Console.Warning("Script: {name} - variable {key} was accessed and was not previously defined.", page.Name, key);
                        return false;
                    }

                    if (value is double)
                        page.SetVariable(variable.Key, new IntVariable(((IntVariable)variable.Value).X, Convert.ToInt32(value)));
                    else if (value is int)
                        page.SetVariable(variable.Key, new IntVariable(((IntVariable)variable.Value).X, (int)value));
                    else if (value is IntVariable)
                        page.SetVariable(variable.Key, new IntVariable(((IntVariable)value).X, ((IntVariable)value).Y));
                    return true;
                }
                else
                {
                    var check_variable = get_number_variable(server, page, key, out var variable);

                    if (variable is IntVariable intVariable)
                    {
                        if (value is double)
                            page.SetVariable(key, new IntVariable(Convert.ToInt32(value), intVariable.Y));
                        else if (value is int)
                            page.SetVariable(key, new IntVariable((int)value, intVariable.Y));
                        else if (value is IntVariable)
                            page.SetVariable(key, new IntVariable(((IntVariable)value).X, ((IntVariable)value).Y));
                    }
                    else
                    {
                        if (value is double)
                            page.SetVariable(key, new IntVariable(Convert.ToInt32(value), 0));
                        else if (value is int)
                            page.SetVariable(key, new IntVariable((int)value, 0));
                        else if (value is IntVariable)
                            page.SetVariable(key, new IntVariable(((IntVariable)value).X, ((IntVariable)value).Y));
                    }
                    return true;
                }
            }

            static bool get_number_variable(GameServer server, Page page, string key, out object var)
            {
                var = new IntVariable(0, 0);

                if (key.Length > 2)
                {
                    if (key.EndsWith(".x"))
                    {
                        var variable = page.Variables.Where(t => t.Type == VariableType.Number).First(t => t.Key == key[0..^2]);

                        if (variable == null)
                            return false;

                        var = ((IntVariable)variable.Value).X;
                        return true;
                    }
                    else if (key.EndsWith(".y"))
                    {
                        var variable = page.Variables.Where(t => t.Type == VariableType.Number).First(t => t.Key == key[0..^2]);

                        if (variable == null)
                            return false;

                        var = ((IntVariable)variable.Value).Y;
                        return true;
                    }
                }

                if (page.Variables.Where(t => t.Type == VariableType.Number).Any(t => t.Key == key))
                {
                    var = ((IntVariable)page.Variables.Where(t => t.Type == VariableType.Number).First(t => t.Key == key).Value).X;
                    return true;
                }

                var = 0;
                return false;
            }

            // Vanilla Triggers

            #region Causes

            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 0), (trigger, ctx, args) => true, "When everything is starting up,");
            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 1), (trigger, ctx, args) => true, "Whenever someone moves,");
            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 10), (trigger, ctx, args) => true, "Whenever someone turns,");
            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 13), (trigger, ctx, args) => true, "When someone arrives in any map,");
            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 14), (trigger, ctx, args) => true, "When someone leaves any map,");
            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 16), (trigger, ctx, args) => true, "When someone attacks,");
            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 17), (trigger, ctx, args) => true, "When someone stands up,");
            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 18), (trigger, ctx, args) => true, "When someone sits down,");
            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 20), (trigger, ctx, args) => true, "When someone uses any emote,");
            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 21), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var emoteEvent = (EmoteEvent)args;
                var emote = trigger.Get<int>(0);

                return (int)emoteEvent.Emote == emote;
            }, "When someone uses emote #,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 30), (trigger, ctx, args) => true, "When someone says anything,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 31), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var talkEvent = (TalkEvent)args;
                var message = trigger.Get<string>(0);

                return string.Equals(talkEvent.Message, message, StringComparison.OrdinalIgnoreCase);
            }, "When someone says {...},");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 32), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var talkEvent = (TalkEvent)args;
                var message = trigger.Get<string>(0);

                return talkEvent.Message.ToLower().Contains(message.ToLower());
            }, "When someone says something with {...} in it,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 60), (trigger, ctx, args) => true, "When someone moves northeast,");
            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 61), (trigger, ctx, args) => true, "When someone moves southeast,");
            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 62), (trigger, ctx, args) => true, "When someone moves southwest,");
            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 63), (trigger, ctx, args) => true, "When someone moves northwest,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 100), (trigger, ctx, args) => true, "When someone tries to drop any item,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 101), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var itemDropEvent = (ItemDropEvent)args;
                var amount = trigger.Get<int>(0);
                var type = trigger.Get<int>(1);

                return (itemDropEvent.Amount == amount && itemDropEvent.Id == type);
            }, "When someone tries to drop # of item #,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 102), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var itemDropEvent = (ItemDropEvent)args;
                var start = trigger.Get<int>(0);
                var end = trigger.Get<int>(1);
                var type = trigger.Get<int>(2);

                return itemDropEvent.Amount >= start && itemDropEvent.Amount <= end && itemDropEvent.Id == type;
            }, "When someone tries to drop between # and # of item #,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 103), (trigger, ctx, args) => true, "When someone tries to pick up any item,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 104), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var itemPickUpEvent = (ItemPickupEvent)args;
                var amount = trigger.Get<int>(0);
                var type = trigger.Get<int>(1);

                return (itemPickUpEvent.Amount == amount && itemPickUpEvent.Id == type);
            }, "When someone tries to pick up # of item #,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 105), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var itemPickUpEvent = (ItemPickupEvent)args;
                var start = trigger.Get<int>(0);
                var end = trigger.Get<int>(1);
                var type = trigger.Get<int>(2);

                return itemPickUpEvent.Amount >= start && itemPickUpEvent.Amount <= end && itemPickUpEvent.Id == type;
            }, "When someone tries to pick up between # and # of item #,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 150), (trigger, ctx, args) => true, "When someone tries to open any door,");
            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 151), (trigger, ctx, args) =>
            {
                var doorOpenEvent = (DoorOpenEvent)args;
                var x = trigger.Get<int>(0);
                var y = trigger.Get<int>(1);

                return doorOpenEvent.X == x && doorOpenEvent.Y == y;
            }, "When someone tries to open a door at (#,#),");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Cause, 250), (trigger, ctx, args) => true, "When # seconds have passed, offset by #,");

            #endregion Causes

            #region Conditions

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 0), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var id = trigger.Get<int>(0);

                return context.Character.MapId == id;
            }, "and they are on map #,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 1), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;

                return context.Character.Gender == Gender.Male;
            }, "and they are male,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 2), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;

                return context.Character.Gender == Gender.Female;
            }, "and they are female,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 3), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;

                return context.Character.Gender != Gender.Male;
            }, "and they are not male,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 4), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;

                return context.Character.Gender != Gender.Female;
            }, "and they are not female,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 5), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.Get<string>(0);

                return context.Character.Name == name;
            }, "and their name is {...},");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 6), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var x = trigger.Get<int>(0);
                var y = trigger.Get<int>(1);

                return context.Character.Map.Characters.Any(t => t.X == x && t.Y == y);
            }, "and there's a player at (#,#),");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 7), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var x = trigger.Get<int>(0);
                var y = trigger.Get<int>(1);

                return !context.Character.Map.Characters.Any(t => t.X == x && t.Y == y);
            }, "and there's no player at (#,#),");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 8), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var x = trigger.Get<int>(0);
                var y = trigger.Get<int>(1);

                return context.Character.Map.MapNPCs.Any(t => t.X == x && t.Y == y);
            }, "and there's a NPC at (#,#),");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 9), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var x = trigger.Get<int>(0);
                var y = trigger.Get<int>(1);

                return !context.Character.Map.MapNPCs.Any(t => t.X == x && t.Y == y);
            }, "and there's no NPC at (#,#),");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 10), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var amount = trigger.Get<int>(0);
                var type = trigger.Get<int>(1);

                return (context.Character.HasItem((ushort)type) >= amount);
            }, "and they have at least # of # in their inventory,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 11), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var amount = trigger.Get<int>(0);
                var type = trigger.Get<int>(1);

                return (context.Character.HasItem((ushort)type) > amount);
            }, "and they have more than # of # in their inventory,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 12), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var amount = trigger.Get<int>(0);
                var type = trigger.Get<int>(1);

                return (context.Character.HasItem((ushort)type) < amount);
            }, "and they have fewer than # of # in their inventory,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 13), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var type = trigger.Get<int>(0);
                var x = trigger.Get<int>(1);
                var y = trigger.Get<int>(2);

                return context.Character.Map.MapItems.Any(n => n.ItemID == type && n.X == (byte)x && n.Y == (byte)y && n.OwningPlayerID == context.GameSession.PlayerId);
            }, "and the item # at (#,#) belongs to them,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 14), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var type = trigger.Get<int>(0);
                var x = trigger.Get<int>(1);
                var y = trigger.Get<int>(2);
                var name = trigger.Get<string>(3);

                var session = this.GameServer.PlayingSessions.FirstOrDefault(t => t.Character.Name == name);

                if (session == null)
                    return false;

                return context.Character.Map.MapItems.Any(n => n.ItemID == type && n.X == (byte)x && n.Y == (byte)y && n.OwningPlayerID == session.PlayerId);
            }, "and the item # at (#,#) belongs to the player named {...},");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 15), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var type = trigger.Get<int>(0);
                var x = trigger.Get<int>(1);
                var y = trigger.Get<int>(2);
                var mapId = trigger.Get<int>(3);

                return this.GameServer.Maps[(ushort)mapId].MapItems.Any(n => n.ItemID == type && n.X == (byte)x && n.Y == (byte)y && n.OwningPlayerID == context.GameSession.PlayerId);
            }, "and the item # at (#,#) on map # belongs to them,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 16), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var type = trigger.Get<int>(0);
                var x = trigger.Get<int>(1);
                var y = trigger.Get<int>(2);
                var mapId = trigger.Get<int>(3);
                var name = trigger.Get<string>(4);

                var session = this.GameServer.PlayingSessions.FirstOrDefault(t => t.Character.Name == name);

                if (session == null)
                    return false;

                return this.GameServer.Maps[(ushort)mapId].MapItems.Any(n => n.ItemID == type && n.X == (byte)x && n.Y == (byte)y && n.OwningPlayerID == session.PlayerId);
            }, "and the item # at (#,#) on map # belongs to the player named {...},");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 35), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;

                var x = trigger.Get<int>(0);
                var y = trigger.Get<int>(1);

                return context.Character.X == x && context.Character.Y == y;
            }, "and they move into position (#,#),");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 50), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var title = trigger.Get<string>(0);

                return context.Character.Title == title;
            }, "and their title is {...},");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 51), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var home = trigger.Get<string>(0);

                return context.Character.Home == home;
            }, "and their home is {...},");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 52), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var fiance = trigger.Get<string>(0);

                return context.Character.Fiance == fiance;
            }, "and their fiance is {...},");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 53), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var partner = trigger.Get<string>(0);

                return context.Character.Partner == partner;
            }, "and their partner is {...},");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 54), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var characterClass = trigger.Get<int>(0);

                return context.Character.Class == characterClass;
            }, "and their class is #,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 55), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var skin = trigger.Get<int>(0);

                return (int)context.Character.Skin == skin;
            }, "and their skin is #,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 56), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var hairStyle = trigger.Get<int>(0);

                return (int)context.Character.HairStyle == hairStyle;
            }, "and their hair style is #,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 57), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var haircolor = trigger.Get<int>(0);

                return (int)context.Character.HairColor == haircolor;
            }, "and their hair color is #,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 58), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var direction = trigger.Get<int>(0);

                return (int)context.Character.Direction == direction;
            }, "and they are facing direction #,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 59), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var value = trigger.Get<int>(0);

                return (int)context.Character.Level > value;
            }, "and their level is higher than #,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 60), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var value = trigger.Get<int>(0);

                return (int)context.Character.Health > value;
            }, "and their health is higher than #,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 61), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var value = trigger.Get<int>(0);

                return (int)context.Character.MaxHealth > value;
            }, "and their maximum health is higher than #,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 62), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var value = trigger.Get<int>(0);

                return (int)context.Character.Mana > value;
            }, "and their mana is higher than #,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 63), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var value = trigger.Get<int>(0);

                return (int)context.Character.MaxMana > value;
            }, "and their maximum mana is higher than #,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 64), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var value = trigger.Get<int>(0);

                return (int)context.Character.MaxStamina > value;
            }, "and their maximum stamina is higher than #,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 65), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var value = trigger.Get<int>(0);

                return (int)context.Character.Exp > value;
            }, "and their EXP is higher than #,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 66), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var value = trigger.Get<int>(0);

                return (SitState)context.Character.SitState == SitState.Floor;
            }, "and they are sitting on the ground,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 67), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var value = trigger.Get<int>(0);

                return (SitState)context.Character.SitState != SitState.Floor;
            }, "and they are not sitting on the ground,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 68), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var value = trigger.Get<int>(0);

                return (SitState)context.Character.SitState == SitState.Chair;
            }, "and they are sitting on a chair,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 69), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var value = trigger.Get<int>(0);

                return (SitState)context.Character.SitState != SitState.Chair;
            }, "and they are not sitting on a chair,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 70), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var value = trigger.Get<int>(0);

                return (SitState)context.Character.SitState == SitState.Stand;
            }, "and they are standing,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 71), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var value = trigger.Get<int>(0);

                return (SitState)context.Character.SitState != SitState.Stand;
            }, "and they are not standing,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 72), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var value = trigger.Get<int>(0);

                return (int)context.Character.StatPoints >= value;
            }, "and they have at least # stat points remaining,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 73), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var value = trigger.Get<int>(0);

                return (int)context.Character.SkillPoints >= value;
            }, "and they have at least # skill points remaining,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 80), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var x = trigger.Get<int>(0);
                var y = trigger.Get<int>(1);

                return context.Character.InRange((byte)x, (byte)y);
            }, "and they can see position (#,#),");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 200), (trigger, ctx, args) =>
            {
                var name = trigger.GetVariableName(0);
                var value = trigger.Get<int>(1);

                if (get_number_variable(this.GameServer, page, name, out var variable))
                    return (Convert.ToInt32(variable) == value);
                return false;
            }, "and variable # is equal to #,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 201), (trigger, ctx, args) =>
            {
                var name = trigger.GetVariableName(0);
                var value = trigger.Get<int>(1);

                if (get_number_variable(this.GameServer, page, name, out var variable))
                    return (Convert.ToInt32(variable) > value);
                return false;
            }, "and variable # is more than #,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 202), (trigger, ctx, args) =>
            {
                var name = trigger.GetVariableName(0);
                var value = trigger.Get<int>(1);

                if (get_number_variable(this.GameServer, page, name, out var variable))
                    return (Convert.ToInt32(variable) < value);
                return false;
            }, "and variable # is less than #,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 203), (trigger, ctx, args) =>
            {
                var name = trigger.GetVariableName(0);
                var value = trigger.Get<int>(1);

                if (get_number_variable(this.GameServer, page, name, out var variable))
                    return (Convert.ToInt32(variable) != value);
                return false;
            }, "and variable # is not equal to #,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 208), (trigger, ctx, args) =>
            {
                var first = trigger.GetVariableName(0);
                var second = trigger.GetVariableName(1);

                get_number_variable(this.GameServer, page, first + ".x", out var fx);
                get_number_variable(this.GameServer, page, second + ".x", out var sx);
                get_number_variable(this.GameServer, page, first + ".y", out var fy);
                get_number_variable(this.GameServer, page, second + ".y", out var sy);

                var first_x = Convert.ToInt32(fx);
                var first_y = Convert.ToInt32(fy);
                var second_x = Convert.ToInt32(sx);
                var second_y = Convert.ToInt32(sy);

                return first_x == second_x && first_y == second_y;
            }, "and the X,Y position in variable # is the same as the position in variable #,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Condition, 209), (trigger, ctx, args) =>
            {
                var first = trigger.GetVariableName(0);
                var second = trigger.GetVariableName(1);

                get_number_variable(this.GameServer, page, first + ".x", out var fx);
                get_number_variable(this.GameServer, page, second + ".x", out var sx);
                get_number_variable(this.GameServer, page, first + ".y", out var fy);
                get_number_variable(this.GameServer, page, second + ".y", out var sy);

                var first_x = Convert.ToInt32(fx);
                var first_y = Convert.ToInt32(fy);
                var second_x = Convert.ToInt32(sx);
                var second_y = Convert.ToInt32(sy);

                return first_x != second_x && first_y != second_y;
            }, "and the X,Y position in variable # is not the same as the position in variable #,");

            #endregion Conditions

            #region Areas

            page.SetTriggerHandler(new Trigger(TriggerCategory.Area, 1), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                trigger.Area = new Area();

                for (var x = 0; x < context.Character.Map.Width; x++)
                    for (var y = 0; y < context.Character.Map.Height; y++)
                        trigger.Area.Points.Add(new Point(x, y));

                return true;
            }, "everywhere on the whole map,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Area, 2), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var x = trigger.Get<int>(0);
                var y = trigger.Get<int>(1);

                trigger.Area = new Area();
                trigger.Area.Points.Add(new Point(x, y));

                return true;
            }, "at position (#,#) on the map,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Area, 5), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;

                trigger.Area = new Area();
                trigger.Area.Points.Add(new Point(context.Character.X, context.Character.Y));

                return true;
            }, "where the triggering player is at, not moving,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Area, 8), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;

                trigger.Area = new Area();

                for (var x = 0; x < context.Character.Map.Width; x++)
                    for (var y = 0; y < context.Character.Map.Height; y++)
                        if (context.Character.InRange((byte)x, (byte)y))
                            trigger.Area.Points.Add(new Point(x, y));

                return true;
            }, "everyplace the triggering player can see,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Area, 10), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                trigger.Area = new Area();

                var x = context.Character.X;
                var y = context.Character.Y;

                switch (context.Character.Direction)
                {
                    case Direction.Down:
                        y += 1;
                        break;

                    case Direction.Left:
                        x -= 1;
                        break;

                    case Direction.Up:
                        y -= 1;
                        break;

                    case Direction.Right:
                        x += 1;
                        break;
                }

                trigger.Area.Points.Add(new Point(x, y));
                return true;
            }, "in the space right in front of the triggering player,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Area, 11), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var steps = trigger.Get<int>(0);

                trigger.Area = new Area();

                var x = context.Character.X;
                var y = context.Character.Y;

                switch (context.Character.Direction)
                {
                    case Direction.Down:
                        y += (byte)steps;
                        break;

                    case Direction.Left:
                        x -= (byte)steps;
                        break;

                    case Direction.Up:
                        y -= (byte)steps;
                        break;

                    case Direction.Right:
                        x += (byte)steps;
                        break;
                }

                trigger.Area.Points.Add(new Point(x, y));
                return true;
            }, "# step(s) in front of the triggering player,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Area, 12), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                trigger.Area = new Area();

                var x = context.Character.X;
                var y = context.Character.Y;

                switch (context.Character.Direction)
                {
                    case Direction.Down:
                        y -= 1;
                        break;

                    case Direction.Left:
                        x += 1;
                        break;

                    case Direction.Up:
                        y += 1;
                        break;

                    case Direction.Right:
                        x -= 1;
                        break;
                }

                trigger.Area.Points.Add(new Point(x, y));
                return true;
            }, "in the space right behind the triggering player,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Area, 13), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var steps = trigger.Get<int>(0);

                trigger.Area = new Area();

                var x = context.Character.X;
                var y = context.Character.Y;

                switch (context.Character.Direction)
                {
                    case Direction.Down:
                        y -= (byte)steps;
                        break;

                    case Direction.Left:
                        x += (byte)steps;
                        break;

                    case Direction.Up:
                        y += (byte)steps;
                        break;

                    case Direction.Right:
                        x -= (byte)steps;
                        break;
                }

                trigger.Area.Points.Add(new Point(x, y));
                return true;
            }, "# step(s) behind the triggering player,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Area, 20), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                trigger.Area = new Area();

                var (x, y, item) = context.Character.LastItemDropped;

                if (item.ItemID == 0)
                    return false;

                trigger.Area.Points.Add(new Point(x, y));

                return true;
            }, "at the location where the triggering player (currently is/previously was) attempting to drop an item,");

            #endregion Areas

            #region Filters

            page.SetTriggerHandler(new Trigger(TriggerCategory.Filter, 9), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                trigger.Area.Points = trigger.Area.Points.Where(t => context.Character.Map.Walkable((byte)t.X, (byte)t.Y, false)).ToList();
                return true;
            }, "only in places where someone can walk,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Filter, 10), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                trigger.Area.Points = trigger.Area.Points.Where(t => !context.Character.Map.Walkable((byte)t.X, (byte)t.Y, false)).ToList();
                return true;
            }, "only in places that can't be walked into,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Filter, 12), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                trigger.Area.Points = trigger.Area.Points.Where(t => context.Character.InRange((byte)t.X, (byte)t.Y)).ToList();
                return true;
            }, "only in places the triggering player can see,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Filter, 13), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                trigger.Area.Points = trigger.Area.Points.Where(t => !context.Character.InRange((byte)t.X, (byte)t.Y)).ToList();
                return true;
            }, "only in places the triggering player cannot see,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Filter, 20), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                trigger.Area.Points = trigger.Area.Points.Where(t => context.Character.Map.MapNPCs.Any(x => x.Alive && x.X == t.X && x.Y == t.Y)).ToList();
                return true;
            }, "only in places where there is an NPC,");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Filter, 21), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                trigger.Area.Points = trigger.Area.Points.Where(t => !context.Character.Map.MapNPCs.Any(x => x.Alive && x.X == t.X && x.Y == t.Y)).ToList();
                return true;
            }, "only in places where there are no NPCs,");

            #endregion Filters

            #region Effects

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 14), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var x = trigger.Get<int>(0);
                var y = trigger.Get<int>(1);

                if (!context.Character.Map.Characters.Any(t => t.X == (byte)x && t.Y == (byte)y))
                    context.Character.Warp(context.Character.MapId, (byte)x, (byte)y, WarpAnimation.None);

                return true;
            }, "move the triggering player to (#,#) if there's nobody already there.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 15), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var x = trigger.Get<int>(0);
                var y = trigger.Get<int>(1);

                context.Character.Warp(context.Character.MapId, (byte)x, (byte)y, WarpAnimation.None);

                return true;
            }, "move the triggering player to (#,#).");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 16), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var x = trigger.Get<int>(0);
                var y = trigger.Get<int>(1);

                if (!context.Character.Map.Characters.Any(t => t.X == (byte)x && t.Y == (byte)y))
                    foreach (var character in players_around_inclusive(context.Character))
                        character.Warp(context.Character.MapId, (byte)x, (byte)y, WarpAnimation.None);

                return true;
            }, "move any player present to (#,#) if there's nobody already there.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 17), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var x = trigger.Get<int>(0);
                var y = trigger.Get<int>(1);

                foreach (var character in players_around_inclusive(context.Character))
                    context.Character.Warp(context.Character.MapId, (byte)x, (byte)y, WarpAnimation.None);

                return true;
            }, "move any player present to (#,#).");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 18), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var x = trigger.Get<int>(0);
                var y = trigger.Get<int>(1);
                var mapId = trigger.Get<int>(2);

                if (!context.Character.Map.Characters.Any(t => t.X == (byte)x && t.Y == (byte)y))
                    context.Character.Warp((ushort)mapId, (byte)x, (byte)y, WarpAnimation.None);

                return true;
            }, "move the triggering player to (#,#,#) if there's nobody already there.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 19), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var x = trigger.Get<int>(0);
                var y = trigger.Get<int>(1);
                var mapId = trigger.Get<int>(2);

                context.Character.Warp((ushort)mapId, (byte)x, (byte)y, WarpAnimation.None);

                return true;
            }, "move the triggering player to (#,#,#).");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 20), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var x = trigger.Get<int>(0);
                var y = trigger.Get<int>(1);
                var mapId = trigger.Get<int>(2);

                if (!context.Character.Map.Characters.Any(t => t.X == (byte)x && t.Y == (byte)y))
                    foreach (var character in players_around_inclusive(context.Character))
                        character.Warp((ushort)mapId, (byte)x, (byte)y, WarpAnimation.None);

                return true;
            }, "move any player present to (#,#,#) if there's nobody already there.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 21), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var x = trigger.Get<int>(0);
                var y = trigger.Get<int>(1);
                var mapId = trigger.Get<int>(2);

                foreach (var character in players_around_inclusive(context.Character))
                    context.Character.Warp((ushort)mapId, (byte)x, (byte)y, WarpAnimation.None);

                return true;
            }, "move any player present to (#,#,#).");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 31), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var x = trigger.Get<int>(0);
                var y = trigger.Get<int>(1);

                var player = context.Character.Map.Characters.FirstOrDefault(t => t.X == x && t.Y == y);

                if (player == null)
                    return false;

                ctx = new EndlessContext(player.Session);
                return true;
            }, "make any player standing at (#,#) become the new triggering player.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 32), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var x = trigger.Get<int>(0);
                var y = trigger.Get<int>(1);
                var mapId = trigger.Get<int>(2);

                if (!this.GameServer.Maps.ContainsKey((ushort)mapId))
                    return false;

                var player = this.GameServer.Maps[(ushort)mapId].Characters.FirstOrDefault(t => t.X == x && t.Y == y);

                if (player == null)
                    return false;

                ctx = new EndlessContext(player.Session);
                return true;
            }, "make any player standing at (#,#,#) become the new triggering player.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 48), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var x = trigger.Get<int>(0);
                var y = trigger.Get<int>(1);
                var name = trigger.Get<string>(2);

                var player = context.Character.Map.Characters.FirstOrDefault(t => t.Name == name);

                if (player == null)
                    return false;

                ctx = new EndlessContext(player.Session);
                return true;
            }, "make the player named {...} the new triggering player, if they're in the map right now.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 49), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var x = trigger.Get<int>(0);
                var y = trigger.Get<int>(1);
                var name = trigger.Get<string>(2);

                var player = this.GameServer.PlayingSessions.FirstOrDefault(t => t.Character.Name == name);

                if (player == null)
                    return false;

                ctx = new EndlessContext(player.Character.Session);
                return true;
            }, "make the player named {...} the new triggering player, if they're in the world right now.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 50), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var message = trigger.Get<string>(0);

                var (type, from_name, from_message) = ParseEmitMessage(message, out var success);
                this.SendEmitMessage(context, new GameSession[] { context.GameSession }, message);

                return true;
            }, "emit message {...} to the triggering player.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 51), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var message = trigger.Get<string>(0);

                var (type, from_name, from_message) = ParseEmitMessage(message, out var success);
                this.SendEmitMessage(context, players_around_inclusive(context.Character).Select(p => p.Session), message);

                return true;
            }, "emit message {...} to any player present.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 52), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var message = trigger.Get<string>(0);
                var x = (byte)trigger.Get<int>(1);
                var y = (byte)trigger.Get<int>(2);

                var (type, from_name, from_message) = ParseEmitMessage(message, out var success);
                this.SendEmitMessage(context, context.Character.Map.Characters.Where(t => t.InRange(x, y)).Select(t => t.Session), message);

                return true;
            }, "emit message {...} to every player who can see (#,#).");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 53), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var message = trigger.Get<string>(0);

                var (type, from_name, from_message) = ParseEmitMessage(message, out var success);
                this.SendEmitMessage(context, players_around_exclusive(context.Character).Select(t => t.Session), message);

                return true;
            }, "emit message {...} to every player who can see the triggering player.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 54), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var message = trigger.Get<string>(0);

                var (type, from_name, from_message) = ParseEmitMessage(message, out var success);
                this.SendEmitMessage(context, context.Character.Map.Characters.Select(t => t.Session), message);

                return true;
            }, "emit message {...} to everyone on the map.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 55), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var message = trigger.Get<string>(0);
                var player_name = trigger.Get<string>(1);

                var (type, from_name, from_message) = ParseEmitMessage(message, out var success);

                if (context.Character.Map.Characters.Any(t => t.Name == from_name))
                    this.SendEmitMessage(context, context.Character.Map.Characters.Where(t => t.Name == from_name).Select(t => t.Session), message);
                return true;
            }, "emit message {...} to the player named {...} if they're in the map.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 56), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var message = trigger.Get<string>(0);

                this.SendEmitMessage(context, context.GameSession.GameServer.PlayingSessions, message);
                return true;
            }, "emit message {...} to everyone in the world.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 68), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var amount = trigger.Get<int>(0);
                var type = trigger.Get<int>(1);

                foreach (var point in trigger.Area.Points)
                {
                    var item = context.Character.Map.MapItems.FirstOrDefault(n => n.ItemID == (short)type && n.Amount == amount);

                    if (item != null)
                    {
                        context.Character.Map.DeleteItem(item.UniqueID, null);
                        return true;
                    }
                }

                return true;
            }, "remove # of item # on the map.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 69), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var amount = trigger.Get<int>(0);
                var itemId = trigger.Get<int>(1);
                var x = trigger.Get<int>(2);
                var y = trigger.Get<int>(3);

                var items = context.Character.Map.MapItems.Where(n => n.ItemID == itemId && n.X == x && n.Y == y).ToList();
                var taken = 0;

                foreach (var item in items)
                {
                    if (item == null)
                        continue;

                    if (taken >= amount)
                        return true;

                    taken++;
                    context.Character.Map.DeleteItem(item.UniqueID, null);
                }

                return true;
            }, "remove # of item # at (#,#) on the map.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 70), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var amount = trigger.Get<int>(0);
                var type = trigger.Get<int>(1);

                foreach (var point in trigger.Area.Points)
                    context.Character.Map.AddItem((ushort)type, amount, (byte)point.X, (byte)point.Y, context.Character);

                return true;
            }, "place # of item # on the map.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 71), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var amount = trigger.Get<int>(0);
                var type = trigger.Get<int>(1);
                var x = trigger.Get<int>(2);
                var y = trigger.Get<int>(3);

                context.Character.Map.AddItem((ushort)type, amount, (byte)x, (byte)y, context.Character);
                return true;
            }, "place # of item # at (#,#) on the map.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 75), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var amount = trigger.Get<int>(0);
                var type = trigger.Get<int>(1);

                context.Character.GiveItem(new InventoryItem((short)type, amount));
                return true;
            }, "place # of item # in the triggering player's inventory.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 100), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;

                context.GameSession.Disconnect();
                return true;
            }, "disconnect the triggering player.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 150), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var emote = trigger.Get<int>(0);
                context.Character.Emote((Emote)emote, true);
                return true;
            }, "make the triggering player use emote #.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 151), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var emote = trigger.Get<int>(0);

                foreach (var character in players_around_inclusive(context.Character))
                    character.Emote((Emote)emote, true);

                context.Character.Emote((Emote)emote, true);
                return true;
            }, "make any player present use emote #.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 152), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var x = (byte)trigger.Get<int>(0);
                var y = (byte)trigger.Get<int>(1);
                var emote = trigger.Get<int>(2);

                foreach (var character in context.Character.Map.Characters.Where(t => t.X == x && t.Y == y))
                    character.Emote((Emote)emote, true);

                context.Character.Emote((Emote)emote, true);
                return true;
            }, "make any player standing at (#,#) use emote #.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 200), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var x = (byte)trigger.Get<int>(0);
                var y = (byte)trigger.Get<int>(1);

                context.Character.Map.OpenDoor(x, y);
                return true;
            }, "open the door at (#,#) on the map.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 201), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var x = (byte)trigger.Get<int>(0);
                var y = (byte)trigger.Get<int>(1);
                var mapId = (byte)trigger.Get<int>(2);

                this.GameServer.Maps[mapId].OpenDoor(x, y);
                return true;
            }, "open the door at (#,#,#).");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 220), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var npcId = (byte)trigger.Get<int>(0);
                var x = (byte)trigger.Get<int>(1);
                var y = (byte)trigger.Get<int>(2);

                context.Character.Map.AddNPC(npcId, x, y);
                return true;
            }, "spawn a new NPC # at (#,#) on the map.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 221), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var npcId = (byte)trigger.Get<int>(0);

                foreach (var point in trigger.Area.Points)
                    context.Character.Map.AddNPC(npcId, (byte)point.X, (byte)point.Y);

                return true;
            }, "spawn a new NPC # on the map.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 222), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var npcIndex = (byte)trigger.Get<int>(0);
                var x = (byte)trigger.Get<int>(1);
                var y = (byte)trigger.Get<int>(2);

                var target = context.Character.Map.MapNPCs.FirstOrDefault(t => t.Alive && t.Index == npcIndex && t.X == x && t.Y == y);

                if (target != null)
                    target.Damage(context.Character, target.TotalHP);

                return true;
            }, "kill the NPC # at (#,#) on the map.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 223), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var x = (byte)trigger.Get<int>(0);
                var y = (byte)trigger.Get<int>(1);

                foreach (var point in trigger.Area.Points)
                {
                    var target = context.Character.Map.MapNPCs.FirstOrDefault(t => t.Alive && t.X == point.X && t.Y == point.Y);

                    if (target != null)
                        target.Damage(context.Character, target.TotalHP);
                }

                return true;
            }, "kill any NPC located at (#,#) on the map.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 224), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var npcId = (byte)trigger.Get<int>(0);

                foreach (var point in trigger.Area.Points)
                {
                    var target = context.Character.Map.MapNPCs.FirstOrDefault(t => t.Alive && t.Id == npcId && t.X == point.X && t.Y == point.Y);

                    if (target != null)
                        target.Damage(context.Character, target.TotalHP);
                }

                return true;
            }, "kill any NPC # on the map.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 225), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;

                foreach (var point in trigger.Area.Points)
                {
                    var target = context.Character.Map.MapNPCs.FirstOrDefault(t => t.Alive && t.X == point.X && t.Y == point.Y);

                    if (target != null)
                        target.Damage(context.Character, target.TotalHP);
                }

                return true;
            }, "kill any NPC on the map.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 226), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var damage = trigger.Get<int>(0);

                foreach (var point in trigger.Area.Points)
                {
                    var target = context.Character.Map.MapNPCs.FirstOrDefault(t => t.Alive && t.X == point.X && t.Y == point.Y);

                    if (target != null)
                        target.Damage(context.Character, damage);
                }

                return true;
            }, "inflict # damage upon any NPC on the map.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 227), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var damage = trigger.Get<int>(0);
                var npcIndex = trigger.Get<int>(1);

                foreach (var point in trigger.Area.Points)
                {
                    var target = context.Character.Map.MapNPCs.FirstOrDefault(t => t.Alive && t.Index == npcIndex && t.X == point.X && t.Y == point.Y);

                    if (target != null)
                        target.Damage(context.Character, damage);
                }
                return true;
            }, "inflict # damage upon NPC # on the map.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 230), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var npcIndex = trigger.Get<int>(0);
                var steps = trigger.Get<int>(1);
                var direction = trigger.Get<int>(2);

                var target = context.Character.Map.MapNPCs.FirstOrDefault(t => t.Alive && t.Index == npcIndex);

                if (target == null)
                    return false;

                for (var i = 0; i < steps; i++)
                    target.Walk((Direction)direction);

                return true;
            }, "make NPC # walk # steps in the direction #.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 1000), (trigger, ctx, args) =>
            {
                var message = trigger.Get<string>(0);

                this.GameServer.Console.Information("EmitDebug: " + message);
                return true;
            }, "emit message {...} to the server console.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 300), (trigger, ctx, args) =>
            {
                var name = trigger.GetVariableName(0);
                var value = trigger.Get<object>(1);

                if (set_number_variable(this.GameServer, page, name, value))
                    return true;

                this.GameServer.Console.Error("Unable to set variable '{variable}' to {value}", name, value);
                return false;
            }, "set variable # to the value #.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 301), (trigger, ctx, args) =>
            {
                var first = trigger.GetVariableName(0);
                var second = trigger.GetVariableName(1);

                var first_result = get_number_variable(this.GameServer, page, first, out var first_variable);
                var second_result = get_number_variable(this.GameServer, page, second, out var second_variable);

                if (first_result && second_result)
                {
                    if (!first.Contains(".") && !second.Contains("."))
                    {
                        if (get_number_variable(this.GameServer, page, first + ".x", out var rx))
                            set_number_variable(this.GameServer, page, second + ".x", rx);
                        else return false;

                        if (get_number_variable(this.GameServer, page, first + ".y", out var ry))
                            set_number_variable(this.GameServer, page, second + ".y", ry);
                        else return false;

                        return true;
                    }
                    else
                    {
                        return set_number_variable(this.GameServer, page, second, first_variable);
                    }
                }

                this.GameServer.Console.Error("Unable to copy value of variable '{second}' into variable '{first}'", second, first);
                return false;
            }, "copy the value of variable # into variable #.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 302), (trigger, ctx, args) =>
            {
                var name = trigger.GetVariableName(0);
                var value = trigger.Get<int>(1);

                get_number_variable(this.GameServer, page, name, out var current_value);

                if (current_value is int)
                    return set_number_variable(this.GameServer, page, name, (int)current_value + value);
                else if (current_value is double)
                    return set_number_variable(this.GameServer, page, name, (double)current_value + value);

                this.GameServer.Console.Error("Unable to add {value} into variable '{variable}'", value, name);
                return false;
            }, "take variable # and add # to it.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 303), (trigger, ctx, args) =>
            {
                var first = trigger.GetVariableName(0);
                var second = trigger.GetVariableName(1);

                var first_result = get_number_variable(this.GameServer, page, first, out var first_variable);
                var second_result = get_number_variable(this.GameServer, page, second, out var second_variable);

                if (first_result && second_result)
                {
                    get_number_variable(this.GameServer, page, first, out var fx);
                    get_number_variable(this.GameServer, page, second, out var sx);

                    var start_amount = Convert.ToInt32(fx);
                    var add_amount = Convert.ToInt32(sx);

                    if (set_number_variable(this.GameServer, page, first, start_amount + add_amount))
                        return true;
                }

                this.GameServer.Console.Error("Unable to add value of variable '{second}' into variable '{first}'", second, first);
                return false;
            }, "take variable # and add variable # to it.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 304), (trigger, ctx, args) =>
            {
                var name = trigger.GetVariableName(0);
                var value = trigger.Get<int>(1);

                get_number_variable(this.GameServer, page, name, out var current_value);

                if (current_value is int)
                    return set_number_variable(this.GameServer, page, name, (int)current_value - value);
                else if (current_value is double)
                    return set_number_variable(this.GameServer, page, name, (double)current_value - value);

                this.GameServer.Console.Error("Unable to subtract {value} from variable '{variable}'", value, name);
                return false;
            }, "take variable # and subtract # from it.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 305), (trigger, ctx, args) =>
            {
                var first = trigger.GetVariableName(0);
                var second = trigger.GetVariableName(1);

                var first_result = get_number_variable(this.GameServer, page, first, out var first_variable);
                var second_result = get_number_variable(this.GameServer, page, second, out var second_variable);

                if (first_result && second_result)
                {
                    get_number_variable(this.GameServer, page, first, out var fx);
                    get_number_variable(this.GameServer, page, second, out var sx);

                    var start_amount = Convert.ToInt32(fx);
                    var subtract_amount = Convert.ToInt32(sx);

                    if (set_number_variable(this.GameServer, page, first, start_amount - subtract_amount))
                        return true;
                }

                this.GameServer.Console.Error("Unable to subtract value '{second}' from variable '{first}'", second, first);
                return false;
            }, "take variable # and subtract variable # from it.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 318), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);

                return set_number_variable(this.GameServer, page, name, context.Character.Map.Characters.Count);
            }, "set variable # to the number of players in the map.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 319), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);

                return set_number_variable(this.GameServer, page, name, context.GameSession.GameServer.PlayingSessions.Count());
            }, "set variable # to the number of players in the server.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 320), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);

                return set_number_variable(this.GameServer, page, name, new IntVariable(context.Character.X, context.Character.Y));
            }, "set variable # to the X,Y position the triggering player is standing at.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 321), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);

                return set_number_variable(this.GameServer, page, name, DateTime.UtcNow.Day);
            }, "set variable # to the current day of the month.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 322), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);

                return set_number_variable(this.GameServer, page, name, DateTime.UtcNow.Hour);
            }, "set variable # to the current hour UTC (Universal Standard Time in twenty-four hour format).");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 323), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);

                return set_number_variable(this.GameServer, page, name, DateTime.UtcNow.Minute);
            }, "set variable # to the current minute UTC (Universal Standard Time).");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 324), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);

                return set_number_variable(this.GameServer, page, name, DateTime.UtcNow.Month);
            }, "set variable # to the current month of the year.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 325), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);

                return set_number_variable(this.GameServer, page, name, DateTime.UtcNow.Second);
            }, "set variable # to the current second UTC (Universal Standard Time).");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 326), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);

                return set_number_variable(this.GameServer, page, name, DateTime.UtcNow.Year);
            }, "set variable # to the current year.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 347), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);

                return set_number_variable(this.GameServer, page, name, (int)context.Character.LastItemDropped.item.ItemID);
            }, "set variable # to the ID of the item the triggering player tried to drop.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 348), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);

                return set_number_variable(this.GameServer, page, name, context.Character.LastItemDropped.item.Amount);
            }, "set variable # to the amount of the item the triggering player tried to drop.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 349), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);

                return set_number_variable(this.GameServer, page, name, new IntVariable(context.Character.LastItemDropped.x, context.Character.LastItemDropped.y));
            }, "set variable # to the X,Y position of the item the triggering player tried to drop.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 350), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);

                return set_number_variable(this.GameServer, page, name, (int)context.Character.LastItemPickedUp.item.ItemID);
            }, "set variable # to the ID of the item the triggering player tried to pick up.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 351), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);

                return set_number_variable(this.GameServer, page, name, context.Character.LastItemPickedUp.item.Amount);
            }, "set variable # to the amount of the item the triggering player tried to pick up.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 352), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);

                return set_number_variable(this.GameServer, page, name, new IntVariable(context.Character.LastItemPickedUp.x, context.Character.LastItemPickedUp.y));
            }, "set variable # to the X,Y position of the item the triggering player tried to pick up.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 384), (trigger, ctx, args) =>
            {
                var name = trigger.GetVariableName(0);
                var x = trigger.Get<int>(1);
                var y = trigger.Get<int>(2);

                return set_number_variable(this.GameServer, page, name, new IntVariable(x, y));
            }, "set variable # to the X,Y position (#,#).");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 385), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);

                foreach (var point in trigger.Area.Points)
                {
                    if (context.Character.Map.MapNPCs.Any(t => t.Alive && t.X == point.X && t.Y == point.Y))
                    {
                        return set_number_variable(this.GameServer, page, name, new IntVariable(point.X, point.Y));
                    }
                }

                return false;
            }, "set variable # to the X,Y position of the NPC.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 386), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);

                foreach (var point in trigger.Area.Points)
                {
                    var npc = context.Character.Map.MapNPCs.FirstOrDefault(t => t.Alive && t.X == point.X && t.Y == point.Y);

                    if (npc != null)
                        return set_number_variable(this.GameServer, page, name, npc.Index);
                }

                return false;
            }, "set variable # to the UID of the NPC.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 387), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);

                foreach (var point in trigger.Area.Points)
                {
                    var npc = context.Character.Map.MapNPCs.FirstOrDefault(t => t.Alive && t.X == point.X && t.Y == point.Y);

                    if (npc != null)
                        return set_number_variable(this.GameServer, page, name, npc.Id);
                }

                return false;
            }, "set variable # to the type of the NPC.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 390), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);

                return set_number_variable(this.GameServer, page, name, context.Character.LastEmoteUsed.id);
            }, "set variable # to the last emote the triggering player used.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 391), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);

                return set_number_variable(this.GameServer, page, name, context.Character.LastNPCAttacked.Index);
            }, "set variable # to the UID of the last NPC the triggering player attacked.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 392), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);

                return set_number_variable(this.GameServer, page, name, context.Character.LastNPCAttacked.Id);
            }, "set variable # to the ID of the last NPC the triggering player attacked.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 393), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);

                return set_number_variable(this.GameServer, page, name, context.Character.LastNPCAttacked.HP);
            }, "set variable # to the current health of the last NPC the triggering player attacked.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 394), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);

                return set_number_variable(this.GameServer, page, name, context.Character.LastNPCAttacked.TotalHP);
            }, "set variable # to the total health of the last NPC the triggering player attacked.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 395), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);

                return set_number_variable(this.GameServer, page, name, new IntVariable(context.Character.LastNPCAttacked.X, context.Character.LastNPCAttacked.Y));
            }, "set variable # to the X,Y position of the last NPC the triggering player attacked.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 399), (trigger, ctx, args) =>
            {
                var name = trigger.GetVariableName(0);

                foreach (var variable in page.Variables)
                {
                    set_number_variable(this.GameServer, page, variable.Key + "x", 0);
                    set_number_variable(this.GameServer, page, variable.Key + "y", 0);
                }

                return true;
            }, "clear all variables to zero.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 404), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);
                var message = trigger.Get<string>(1);

                return set_message_variable(this.GameServer, page, name, message);
            }, "set message ~ to {...}.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 405), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);
                var message = context.Character.LastMessageSpoken;

                return set_message_variable(this.GameServer, page, name, message);
            }, "set message ~ to what the triggering player last said.");

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 406), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var name = trigger.GetVariableName(0);

                return set_message_variable(this.GameServer, page, name, context.Character.Name);
            }, "set message ~ to the triggering players's name.");

            #endregion Effects

            // Addon Triggers

            #region Effect

            page.SetTriggerHandler(new Trigger(TriggerCategory.Effect, 800), (trigger, ctx, args) =>
            {
                var context = (EndlessContext)ctx;
                var groundId = trigger.Get<int>(0);

                if (context.GameSession.AddonConnection == null)
                    return false;

                foreach (var point in trigger.Area.Points)
                    context.GameSession.AddonConnection.Send(new AddonMessage("mutate", 0, point.X, point.Y, groundId));

                return true;
            }, "set the floor to #.");

            #endregion

            return page;
        }

        private void SetupPingTimer()
        {
            if (this.GameServer.GameServerConfiguration.PingRate == 0)
                return;

            this.PingTimer = SimpleTimer.SetInterval(() =>
            {
                foreach (var session in this.GameServer.Sessions.Cast<GameSession>())
                {
                    if (session.NeedsPong)
                    {
                        session.Disconnect();
                    }
                    else
                    {
                        var builder = new Packet(PacketFamily.Connection, PacketAction.Player);
                        builder.AddShort(6);
                        builder.AddChar(6);

                        session.NeedsPong = true;
                        session.Send(builder);
                    }
                }
            }, this.GameServer.GameServerConfiguration.PingRate);
        }

        public void ExecuteTrigger(IContext ctx, object args, int id)
        {
            var context = (EndlessContext)ctx;

            foreach (var page in this.GlobalScripts)
                page.Execute(context, args, id);

            if (context.GameSession.State == ClientState.Playing)
                foreach (var page in this.MapScripts.Where(p => p.Name == context.Character.MapId.ToString()))
                    page.Execute(context, args, id);
        }

        private static (EmitMessageType type, string from_user, string from_message) ParseEmitMessage(string message, out bool success)
        {
            success = false;

            if (!message.StartsWith(":"))
                return (EmitMessageType.Server, "", message);

            if (message.Split(' ').Length <= 2)
                return (EmitMessageType.Server, "", message);

            var message_type = message.Split(' ')[0];
            var message_from = message.Split(' ')[1];
            var message_content = message.Substring(message_type.Length + message_from.Length + 2, (message.Length - (message_type.Length + message_from.Length)) - 2);

            if (!string.IsNullOrWhiteSpace(message_type) && !string.IsNullOrWhiteSpace(message_from) && !string.IsNullOrWhiteSpace(message_content))
            {
                success = true;
                if (message_type == ":announce")
                    return (EmitMessageType.Announce, message_from, message_content);
                else if (message_type == ":global")
                    return (EmitMessageType.Global, message_from, message_content);
                else if (message_type == ":admin")
                    return (EmitMessageType.Admin, message_from, message_content);
                else if (message_type == ":from")
                    return (EmitMessageType.Normal, message_from, message_content);
                else if (message_type == ":npc")
                    return (EmitMessageType.NPC, message_from, message_content);
                else success = false;
            }

            return (EmitMessageType.Server, "", "");
        }

        private enum EmitMessageType
        {
            Server,
            Announce,
            Global,
            Admin,
            Normal,
            NPC
        }

        /// <summary>
        /// Parse the message and send the appropriate emit.
        /// </summary>
        private void SendEmitMessage(EndlessContext context, IEnumerable<GameSession> sessions, string message)
        {
            var (type, from_name, from_message) = ParseEmitMessage(message, out var success);

            if (success)
            {
                switch (type)
                {
                    case EmitMessageType.Announce:
                        this.SendAnnounceMessage(from_name, from_message, sessions);
                        break;

                    case EmitMessageType.Admin:
                        this.SendAdminMessage(from_name, from_message, sessions);
                        break;

                    case EmitMessageType.Global:
                        this.SendGlobalMessage(from_name, from_message, sessions);
                        break;

                    case EmitMessageType.Normal:
                        var character = context.Character.Map.Characters.FirstOrDefault(t => t.Name == from_name);

                        if (character != null)
                            this.SendMessage(character, from_message, sessions);
                        break;

                    case EmitMessageType.NPC:
                        if (int.TryParse(from_name, out var index))
                        {
                            if (!context.Character.Map.MapNPCs.Any(t => t.Index == index))
                                return;

                            this.SendNPCMessage(index, from_message, sessions);
                        }
                        break;
                }
            }
            else this.SendServerMessage(message, sessions);
        }

        /// <summary>
        /// Send an NPC chat message.
        /// </summary>
        public void SendNPCMessage(int from_index, string message, IEnumerable<GameSession> sessions)
        {
            var builder = new Packet(PacketFamily.NPC, PacketAction.Player);
            builder.AddBreak();
            builder.AddBreak();
            builder.AddChar((byte)from_index);
            builder.AddChar((byte)message.Length);
            builder.AddString(message);

            foreach (var session in sessions)
                session.Send(builder);
        }

        /// <summary>
        /// Send a normal chat message.
        /// </summary>
        public void SendMessage(Character from_character, string message, bool echo, IEnumerable<GameSession> sessions)
        {
            var builder = new Packet(PacketFamily.Talk, PacketAction.Player);
            builder.AddBreakString(from_character.Name);
            builder.AddBreakString(message);

            foreach (var session in sessions)
            {
                if (!echo && from_character.Session.PlayerId == session.PlayerId)
                    continue;

                session.Send(builder);
            }
        }

        /// <summary>
        /// Send a normal chat message to the specified sessions.
        /// </summary>
        public void SendMessage(Character from_character, string message, IEnumerable<GameSession> sessions)
        {
            var builder = new Packet(PacketFamily.Talk, PacketAction.Player);
            builder.AddShort(from_character.Session.PlayerId);
            builder.AddString(message);

            foreach (var session in sessions)
                session.Send(builder);
        }

        /// <summary>
        /// Send a global chat message.
        /// </summary>
        public void SendGlobalMessage(Character from_character, string message, bool echo)
        {
            var builder = new Packet(PacketFamily.Talk, PacketAction.Message);
            builder.AddBreakString(from_character.Name);
            builder.AddBreakString(message);

            foreach (var session in this.GameServer.Sessions.Cast<GameSession>())
            {
                if (!echo && from_character.Session.PlayerId == session.PlayerId)
                    continue;

                session.Send(builder);
            }
        }

        /// <summary>
        /// Send a global chat message.
        /// </summary>
        public void SendGlobalMessage(string from_name, string message, IEnumerable<GameSession> sessions)
        {
            var builder = new Packet(PacketFamily.Talk, PacketAction.Message);
            builder.AddBreakString(from_name);
            builder.AddBreakString(message);

            foreach (var session in sessions)
                session.Send(builder);
        }

        /// <summary>
        /// Send a announce chat message.
        /// </summary>
        public void SendAnnounceMessage(string from_name, string message, IEnumerable<GameSession> sessions)
        {
            var builder = new Packet(PacketFamily.Talk, PacketAction.Announce);
            builder.AddBreakString(from_name);
            builder.AddBreakString(message);

            foreach (var session in sessions)
                session.Send(builder);
        }

        /// <summary>
        /// Send a admin chat message.
        /// </summary>
        public void SendAdminMessage(string from_name, string message, IEnumerable<GameSession> sessions)
        {
            var builder = new Packet(PacketFamily.Talk, PacketAction.Admin);
            builder.AddBreakString(from_name);
            builder.AddBreakString(message);

            foreach (var session in sessions)
                session.Send(builder);
        }

        /// <summary>
        /// Send a server chat message.
        /// </summary>
        public void SendServerMessage(string message, IEnumerable<GameSession> sessions)
        {
            var builder = new Packet(PacketFamily.Talk, PacketAction.Server);
            builder.AddString(message);

            foreach (var session in sessions)
                session.Send(builder);
        }
    }
}