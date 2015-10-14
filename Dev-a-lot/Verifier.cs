﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace TestAddon
{
    public static class Verifier
    {
        private static readonly Dictionary<GameObjectType, Type> TypeDictionary = new Dictionary<GameObjectType, Type>
        {
            { GameObjectType.AIHeroClient, typeof (AIHeroClient) },
            { GameObjectType.DrawFX, typeof (DrawFX) },
            { GameObjectType.FollowerObject, typeof (FollowerObject) },
            { GameObjectType.FollowerObjectWithLerpMovement, typeof (FollowerObjectWithLerpMovement) },
            { GameObjectType.GameObject, typeof (GameObject) },
            { GameObjectType.GrassObject, typeof (GrassObject) },
            { GameObjectType.LevelPropAI, typeof (LevelPropAI) },
            { GameObjectType.LevelPropGameObject, typeof (LevelPropGameObject) },
            { GameObjectType.LevelPropSpawnerPoint, typeof (LevelPropSpawnerPoint) },
            { GameObjectType.MissileClient, typeof (MissileClient) },
            { GameObjectType.NeutralMinionCamp, typeof (NeutralMinionCamp) },
            { GameObjectType.UnrevealedTarget, typeof (UnrevealedTarget) },
            { GameObjectType.obj_AI_Base, typeof (Obj_AI_Base) },
            { GameObjectType.obj_AI_Marker, typeof (Obj_AI_Marker) },
            { GameObjectType.obj_AI_Minion, typeof (Obj_AI_Minion) },
            { GameObjectType.obj_AI_Turret, typeof (Obj_AI_Turret) },
            { GameObjectType.obj_Barracks, typeof (Obj_Barracks) },
            { GameObjectType.obj_BarracksDampener, typeof (Obj_BarracksDampener) },
            { GameObjectType.obj_Building, typeof (Obj_Building) },
            { GameObjectType.obj_GeneralParticleEmitter, typeof (Obj_GeneralParticleEmitter) },
            { GameObjectType.obj_HQ, typeof (Obj_HQ) },
            { GameObjectType.obj_InfoPoint, typeof (Obj_InfoPoint) },
            { GameObjectType.obj_Lake, typeof (Obj_Lake) },
            { GameObjectType.obj_LampBulb, typeof (Obj_LampBulb) },
            { GameObjectType.obj_Levelsizer, typeof (Obj_Levelsizer) },
            { GameObjectType.obj_NavPoint, typeof (Obj_NavPoint) },
            { GameObjectType.obj_Shop, typeof (Obj_Shop) },
            { GameObjectType.obj_SpawnPoint, typeof (Obj_SpawnPoint) },
            { GameObjectType.obj_Turret, typeof (Obj_Turret) },
            { GameObjectType.obj_Ward, typeof (Obj_Ward) }
        };

        public static Menu Menu { get; set; }

        public static bool UseOnUpdate
        {
            get { return Menu["onUpdate"].Cast<CheckBox>().CurrentValue; }
        }
        public static bool UseOnTick
        {
            get { return Menu["onTick"].Cast<CheckBox>().CurrentValue; }
        }

        public static readonly Dictionary<Type, Func<bool>> CurrentlyEnabled = new Dictionary<Type, Func<bool>>
        {
            { typeof (Obj_AI_Base), () => Menu["Obj_AI_Base"].Cast<CheckBox>().CurrentValue },
            { typeof (Obj_AI_Minion), () => Menu["Obj_AI_Minion"].Cast<CheckBox>().CurrentValue },
            { typeof (AIHeroClient), () => Menu["AIHeroClient"].Cast<CheckBox>().CurrentValue },
            { typeof (AttackableUnit), () => Menu["AttackableUnit"].Cast<CheckBox>().CurrentValue },
            { typeof (GameObject), () => Menu["GameObject"].Cast<CheckBox>().CurrentValue }
        };

        static Verifier()
        {
            // Create the menu
            Menu = Program.Menu.AddSubMenu("Crash tests");

            Menu.AddGroupLabel("Core fail tests");
            Menu.Add("correctTypes", new CheckBox("Check if all types match", false)).CurrentValue = false;
            Menu["correctTypes"].Cast<CheckBox>().OnValueChange += delegate(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
            {
                if (args.NewValue)
                {
                    sender.CurrentValue = false;
                    Console.WriteLine("[TypeMatch] Checking...");
                    foreach (var obj in ObjectManager.Get<GameObject>().Where(obj => TypeDictionary.ContainsKey(obj.Type)).Where(obj => TypeDictionary[obj.Type] != obj.GetType()))
                    {
                        Console.WriteLine("[TypeMatch] Got: {0} ({1}) | Expected: {2} ({3})", obj.Type, obj.GetType().Name, obj.Type, TypeDictionary[obj.Type].Name);
                    }
                    Console.WriteLine("[TypeMatch] Completed!");
                    Chat.Print("Check console for TypeMatch results!");
                }
            };

            Menu.AddGroupLabel("Property Diagnosis");
            Menu.Add("onUpdate", new CheckBox("Use Game.OnUpdate")).OnValueChange += delegate(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
            {
                if (args.NewValue)
                {
                    Menu["onTick"].Cast<CheckBox>().CurrentValue = false;
                }
            };
            Menu.Add("onTick", new CheckBox("Use Game.OnTick", false)).OnValueChange += delegate(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
            {
                if (args.NewValue)
                {
                    Menu["onUpdate"].Cast<CheckBox>().CurrentValue = false;
                }
            };
            Menu.AddLabel("Note: This might cause your game to crash! Only use this if you know what you are doing!");
            Menu.Add("AIHeroClient", new CheckBox("AIHeroClient", false)).CurrentValue = false;
            Menu.Add("Obj_AI_Minion", new CheckBox("Obj_AI_Minion", false)).CurrentValue = false;
            Menu.Add("Obj_AI_Base", new CheckBox("Obj_AI_Base", false)).CurrentValue = false;
            Menu.Add("AttackableUnit", new CheckBox("AttackableUnit", false)).CurrentValue = false;
            Menu.Add("GameObject", new CheckBox("GameObject (very laggy)", false)).CurrentValue = false;

            Menu.AddSeparator();
            Menu.AddLabel(string.Format("Note: Some of those tests will create a folder on your Desktop called '{0}'!", Path.GetFileName(Program.ResultPath)));

            // Listen to required events
            Game.OnUpdate += delegate
            {
                if (UseOnUpdate)
                {
                    VerifyObjects();
                }
            };
            Game.OnTick += delegate
            {
                if (UseOnTick)
                {
                    VerifyObjects();
                }
            };
        }

        private static void VerifyObjects()
        {
            var objects = (from obj in ObjectManager.Get<GameObject>() from entry in CurrentlyEnabled.Where(entry => entry.Key.IsInstanceOfType(obj) && entry.Value()) select obj).ToList();
            if (objects.Count > 0)
            {
                if (!Directory.Exists(Program.ResultPath))
                {
                    Directory.CreateDirectory(Program.ResultPath);
                }

                using (var writer = File.CreateText(Path.Combine(Program.ResultPath, "PropertyDiagnosis.txt")))
                {
                    foreach (var obj in objects)
                    {
                        using (var analyzer = new GameObjectDiagnosis(obj, writer))
                        {
                            analyzer.Analyze();
                        }
                    }
                }
            }
        }

        public static void Initialize()
        {
        }

        private static void BlameFinn()
        {
            foreach (var obj in ObjectManager.Get<GameObject>())
            {
                if (!TypeDictionary.ContainsKey(obj.Type))
                {
                    Console.WriteLine("'{0}' was not found in dictionary!", obj.Type);
                    continue;
                }
                if (TypeDictionary[obj.Type] != obj.GetType())
                {
                    Console.WriteLine("Blame finn0x, '{0}' is not '{1}' as expected with 'GameObjectType.{2}'!", obj.GetType().Name, TypeDictionary[obj.Type].Name, obj.Type);
                }
            }
        }
    }
}
