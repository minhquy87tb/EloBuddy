﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using Color = System.Drawing.Color;

namespace SimpleTristana
{
    class Program
    {
        //Skills
        public static Spell.Active Q;
        public static Spell.Skillshot W;
        public static Spell.Targeted E;
        public static Spell.Targeted R;
        public static int[] levelUps = { 2, 0, 1, 2, 2, 3, 2, 0 , 2, 0, 3, 0, 0, 1, 1, 3, 1, 1};
        
        //MenuVars
        public static Menu Menu,
        ComboMenu,
        HarassMenu,
        FarmMenu,
        KsMenu,
        MiscMenu,
        InterruptorMenu,
        GapCloserMenu;

        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Game_OnStart;
        }

        public static AIHeroClient _Player
        {
            get { return ObjectManager.Player; }
        }

        private static void Game_OnStart(EventArgs args)
        {
            if (!_Player.ChampionName.Contains("Tristana")) return;
            Bootstrap.Init(null);
            uint level = (uint)Player.Instance.Level;
            Q = new Spell.Active(SpellSlot.Q, 543 + level * 7);
            W = new Spell.Skillshot(SpellSlot.W, 825, SkillShotType.Circular, (int)0.25f, Int32.MaxValue, (int)80f);
            E = new Spell.Targeted(SpellSlot.E, 543 + level * 7);
            R = new Spell.Targeted(SpellSlot.R, 543 + level * 7);

            Menu = MainMenu.AddMenu("Simple Tristana", "simpleTrist");
            Menu.AddGroupLabel("Simple Tristana");
            Menu.AddLabel("Version: " + "1.0.6.2 - 25.10.15 12:00 GMT+2");
            Menu.AddSeparator();
            Menu.AddLabel("By Pataxx");
            Menu.AddSeparator();
            Menu.AddLabel("Changes: Tweaked Activator");
            Menu.AddSeparator();
            Menu.AddSeparator();
            Menu.AddLabel("Thanks to: Finndev, Hellsing, Fluxy");
            Menu.AddSeparator();
            
            ComboMenu = Menu.AddSubMenu("Combo", "SimpleCombo");
            ComboMenu.AddGroupLabel("Combo Settings");
            ComboMenu.Add("useQCombo", new CheckBox("Use Q", true));
            ComboMenu.Add("useECombo", new CheckBox("Use E", true));
            ComboMenu.Add("useRCombo", new CheckBox("Use R", true));
            ComboMenu.AddLabel("Finisher Settings");
            ComboMenu.Add("useERFinish", new CheckBox("Allow E + R Finish", true));
            ComboMenu.Add("useWFinish", new CheckBox("Allow W Finish", false));

            HarassMenu = Menu.AddSubMenu("Harass", "SimpleHarass");
            HarassMenu.AddGroupLabel("Harass Settings");
            HarassMenu.Add("useQHarass", new CheckBox("Use Q", false));
            HarassMenu.Add("useEHarass", new CheckBox("Use E", false));
            HarassMenu.Add("manaHarass", new Slider("Manaslider", 35, 0, 100));

            FarmMenu = Menu.AddSubMenu("Laneclear", "SimpleClear");
            FarmMenu.AddGroupLabel("Laneclear Settings");
            FarmMenu.Add("useQLane", new CheckBox("Use Q", false));
            FarmMenu.Add("useELane", new CheckBox("Use E", false));
            FarmMenu.Add("useELaneT", new CheckBox("Use E on Tower", false));
            FarmMenu.Add("manaFarm", new Slider("Manaslider", 50, 0, 100));

            KsMenu = Menu.AddSubMenu("Killsteal", "SimpleKS");
            KsMenu.AddGroupLabel("Killsteal Settings");
            KsMenu.Add("useRKs", new CheckBox("Use R", true));
            KsMenu.Add("useWKs", new CheckBox("Use W", false));



            MiscMenu = Menu.AddSubMenu("Misc", "SimpleDraw");
            MiscMenu.AddGroupLabel("Finisher Tweaks");
            MiscMenu.Add("ERBuffer", new Slider("E-R Damage-Buffer", 60, 0, 500));
            MiscMenu.Add("RBuffer", new Slider("R Damage-Buffer", 60, 0, 500));
            MiscMenu.Add("WBuffer", new Slider("W Damage-Buffer", 50, 0, 500));
            MiscMenu.AddGroupLabel("Anti-Gapcloser");
            MiscMenu.Add("antiGC", new CheckBox("Basic Anti-Gapcloser", true));
            MiscMenu.Add("antiKitty", new CheckBox("Anti Rengar", true));
            MiscMenu.Add("antiBug", new CheckBox("Anti Kha'Zix", true));
            MiscMenu.AddGroupLabel("Draw Settings");
            MiscMenu.Add("drawAA", new CheckBox("Draw AA / E / R", true));
            MiscMenu.Add("drawW", new CheckBox("Draw W", true));
            MiscMenu.AddGroupLabel("Stuff");
            MiscMenu.Add("autoLv", new CheckBox("Auto Levelup", true));
            Activator.init();


            Game.OnTick += Game_OnTick;
            Game.OnUpdate += Game_OnUpdate;

            Drawing.OnDraw += Drawing_OnDraw;
            Gapcloser.OnGapcloser += Gapcloser_OnGapCloser;
            GameObject.OnCreate += GameObject_OnCreate;

        }
        private static void Game_OnTick(EventArgs args)
        {
            if (_Player.IsDead) return;
             if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
             {
                 Combo();
             }
             if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
             {
                 Harass();
             }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                LaneClear();
            }
            KillSteal();

        }

        static void Gapcloser_OnGapCloser(Obj_AI_Base sender, Gapcloser.GapcloserEventArgs args)
        {
            if(MiscMenu["antiGC"].Cast<CheckBox>().CurrentValue && args.Sender.Distance(_Player)<300)
            {
                R.Cast(args.Sender);
            }
        }

        // Not tested yet, took it from my old L# project
        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            var kitty = EntityManager.Heroes.Enemies.Find(k => k.ChampionName.Equals("Rengar"));
            var khazix = EntityManager.Heroes.Enemies.Find(k => k.ChampionName.Equals("Khazix"));

            //AntiKittyCat-Untermenschenchamp #FAKURENGO
            if (kitty != null)
            {
                if (sender.Name == ("Rengar_LeapSound.troy") && MiscMenu["antiKitty"].Cast<CheckBox>().CurrentValue && sender.Position.Distance(_Player) < R.Range)
                    R.Cast(kitty);
            }

            //ANTSPRAYYYYYYYYY
            if (khazix != null)
            {
                if (sender.Name == ("Khazix_Base_E_Tar.troy") && MiscMenu["antiBug"].Cast<CheckBox>().CurrentValue && sender.Position.Distance(_Player) <= 400)
                    R.Cast(khazix);
            }
        }

        //---------------------------
        //---------------------------
        //---------------------------

        //Stuff
        private static void KillSteal()
        {
            var useR = KsMenu["useRKs"].Cast<CheckBox>().CurrentValue;
            var useW = KsMenu["useWKs"].Cast<CheckBox>().CurrentValue;
            foreach (var target in EntityManager.Heroes.Enemies.Where(hero => hero.IsValidTarget(W.Range) && !hero.IsDead && !hero.IsZombie && hero.HealthPercent <= 25))
            {
                var nearTurret = EntityManager.Turrets.Enemies.FirstOrDefault(a => !a.IsDead && a.Distance(target) <= 775 + _Player.BoundingRadius + (target.BoundingRadius / 2) + 44.2); //yolo, should work
                if (useR && R.IsReady() && target.Health + target.AttackShield < Player.Instance.GetSpellDamage(target, SpellSlot.R, DamageLibrary.SpellStages.Default))
                {
                    R.Cast(target);
                }

                if (useW && W.IsReady() && target.Health + target.AttackShield < Player.Instance.GetSpellDamage(target, SpellSlot.W, DamageLibrary.SpellStages.Default) && target.Position.CountEnemiesInRange(800) == 1 && nearTurret == null && _Player.Mana >= 120)
                {
                    W.Cast(target);
                }

            }
        }
        //---------------------------
        //---------------------------
        //---------------------------

        //States
        private static void Combo()
        {
            var target = TargetSelector.GetTarget(900, DamageType.Physical);
            var targetE = EntityManager.Heroes.Enemies.FirstOrDefault(a => a.HasBuff("tristanaecharge") && a.Distance(_Player) < _Player.AttackRange);
            if (target == null) return;
            // if (targetE != null) Orbwalker.ForcedTarget = targetE; -- Still buggy Orbwalker
            var nearTurret = EntityManager.Turrets.Enemies.FirstOrDefault(a => !a.IsDead && a.Distance(target) <= 775 + _Player.BoundingRadius + (target.BoundingRadius/2) + 44.2); //yolo, should work

            var useQ = ComboMenu["useQCombo"].Cast<CheckBox>().CurrentValue;
            var useE = ComboMenu["useECombo"].Cast<CheckBox>().CurrentValue;
            var useR = ComboMenu["useRCombo"].Cast<CheckBox>().CurrentValue;

            var useER = ComboMenu["useERFinish"].Cast<CheckBox>().CurrentValue;
            var useWf = ComboMenu["useWFinish"].Cast<CheckBox>().CurrentValue;

            if (Orbwalker.IsAutoAttacking) return;

                if (useE && E.IsReady() && target.IsValidTarget(E.Range))
                {
                    E.Cast(target);
                    Orbwalker.ForcedTarget = target;
                }
                if (useQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                {
                    Q.Cast();
                }
                if (useR && R.IsReady() && target.IsValidTarget(R.Range) && target.Health + target.AttackShield + MiscMenu["RBuffer"].Cast<Slider>().CurrentValue < Player.Instance.GetSpellDamage(target, SpellSlot.R, DamageLibrary.SpellStages.Default))
                {
                    R.Cast(target);
                }

                if (useWf && W.IsReady() && target.IsValidTarget(W.Range) && target.Health + target.AttackShield + MiscMenu["WBuffer"].Cast<Slider>().CurrentValue < Player.Instance.GetSpellDamage(target, SpellSlot.W, DamageLibrary.SpellStages.Default) && target.Position.CountEnemiesInRange(800) == 1 && nearTurret == null)
                {
                    W.Cast(target);
                }
                if(targetE !=null)
                    if (useER && !E.IsReady() && R.IsReady() && targetE.IsValidTarget(R.Range) && (targetE.Health + targetE.AllShield + MiscMenu["ERBuffer"].Cast<Slider>().CurrentValue) - (Player.Instance.GetSpellDamage(targetE, SpellSlot.E, DamageLibrary.SpellStages.Default) + (targetE.Buffs.Find(a => a.Name == "tristanaecharge").Count * Player.Instance.GetSpellDamage(targetE, SpellSlot.E, DamageLibrary.SpellStages.Detonation))) < Player.Instance.GetSpellDamage(targetE, SpellSlot.R))
                    {
                        R.Cast(targetE);
                    }
        } 
        private static void Harass()
        {
            var target = TargetSelector.GetTarget(900, DamageType.Physical);
            if (target == null) return;
            var useQ = HarassMenu["useQHarass"].Cast<CheckBox>().CurrentValue;
            var useE = HarassMenu["useEHarass"].Cast<CheckBox>().CurrentValue;
            if (Orbwalker.IsAutoAttacking) return;

            if (useE && E.IsReady() && E.Cast(target) && target.IsValidTarget(E.Range) && _Player.ManaPercent > HarassMenu["manaHarass"].Cast<Slider>().CurrentValue)
            {
                E.Cast(target);

            }
            if (useQ && Q.IsReady() && target.IsValidTarget(Q.Range))
            {
                Q.Cast();
            }
        }
        private static void LaneClear()
        {
            
            var minion = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Instance.Position, _Player.AttackRange).FirstOrDefault(a => !a.IsDead);
            var minionE = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Instance.Position, _Player.AttackRange).FirstOrDefault(a => a.HasBuff("tristanaecharge"));
            var tower = EntityManager.Turrets.Enemies.FirstOrDefault(a => !a.IsDead && a.Distance(_Player) < _Player.AttackRange);
            if (minion == null)
                if (tower == null)
                    return;
            var useQ = FarmMenu["useQLane"].Cast<CheckBox>().CurrentValue;
            var useE = FarmMenu["useELane"].Cast<CheckBox>().CurrentValue;
            var useET = FarmMenu["useELaneT"].Cast<CheckBox>().CurrentValue;
            
            if (useET && E.IsReady() && tower.IsValidTarget(E.Range) && _Player.ManaPercent > FarmMenu["manaFarm"].Cast<Slider>().CurrentValue)
            {
                E.Cast(tower);
            }

            if (useE && E.IsReady() && minion.IsValidTarget(E.Range) && _Player.ManaPercent > FarmMenu["manaFarm"].Cast<Slider>().CurrentValue)
            {
                if (useET && !tower.IsValidTarget(E.Range))
                    E.Cast(minion);
                else if(!useET)
                    E.Cast(minion);
            }
            if (useQ && Q.IsReady())
            {
                Q.Cast();
            }

        }
        //Drawings
        private static void Drawing_OnDraw(EventArgs args)
        {
            if (MiscMenu["drawAA"].Cast<CheckBox>().CurrentValue)
            {
                new Circle() { Color = Color.Red, BorderWidth = 1, Radius = E.Range }.Draw(_Player.Position);
            }
            if (MiscMenu["drawW"].Cast<CheckBox>().CurrentValue)
            {
                new Circle() { Color = Color.Blue, BorderWidth = 1, Radius = W.Range }.Draw(_Player.Position);
            }
        }
        //---------------------------
        //---------------------------
        //---------------------------

        //Auto-Levelup
        private static void Game_OnUpdate(EventArgs args)
        {
            uint level = (uint)Player.Instance.Level;

            Q = new Spell.Active(SpellSlot.Q, 543 + level * 7);
            W = new Spell.Skillshot(SpellSlot.W, 825, SkillShotType.Circular, (int)0.25f, Int32.MaxValue, (int)80f);
            E = new Spell.Targeted(SpellSlot.E, 543 + level * 7);
            R = new Spell.Targeted(SpellSlot.R, 543 + level * 7);

            if (MiscMenu["autoLv"].Cast<CheckBox>().CurrentValue)
            { 
                if (_Player.Spellbook.GetSpell(SpellSlot.Q).Level + _Player.Spellbook.GetSpell(SpellSlot.W).Level + _Player.Spellbook.GetSpell(SpellSlot.E).Level + _Player.Spellbook.GetSpell(SpellSlot.R).Level < _Player.Level)
                {
                    int[] levels = new int[] { 0, 0, 0, 0 };
                    for (int i = 0; i < ObjectManager.Player.Level; i++)
                    {
                        levels[levelUps[i]] = levels[levelUps[i]] + 1;
                    }
                    if (_Player.Spellbook.GetSpell(SpellSlot.Q).Level < levels[0]) _Player.Spellbook.LevelSpell(SpellSlot.Q);
                    if (_Player.Spellbook.GetSpell(SpellSlot.W).Level < levels[1]) _Player.Spellbook.LevelSpell(SpellSlot.W);
                    if (_Player.Spellbook.GetSpell(SpellSlot.E).Level < levels[2]) _Player.Spellbook.LevelSpell(SpellSlot.E);
                    if (_Player.Spellbook.GetSpell(SpellSlot.R).Level < levels[3]) _Player.Spellbook.LevelSpell(SpellSlot.R);
                }
            }
        }
        //---------------------------
        //---------------------------
        //---------------------------

        
    }
}