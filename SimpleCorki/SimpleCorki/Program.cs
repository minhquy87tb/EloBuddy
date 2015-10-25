using System;
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

namespace SimpleCorki
{
    class Program
    {
        //Skills
        public static Spell.Skillshot Q;
        public static Spell.Skillshot W;
        public static Spell.Active E;
        public static Spell.Skillshot R;
        public static int[] levelUps = { 0, 1, 2, 0, 0, 3, 0, 2, 0, 2, 3, 2, 2, 1, 1, 3, 1, 1 };

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
            if (!_Player.ChampionName.Contains("Corki")) return;
            Bootstrap.Init(null);
            uint level = (uint)Player.Instance.Level;
            Q = new Spell.Skillshot(SpellSlot.Q, 825, SkillShotType.Circular, 300 , 1000 ,250);
            W = new Spell.Skillshot(SpellSlot.W, 800, SkillShotType.Linear);
            E = new Spell.Active(SpellSlot.E, 600);
            R = new Spell.Skillshot(SpellSlot.R, 1300, SkillShotType.Linear, 200, 1950, 40);

            Menu = MainMenu.AddMenu("Simple Corki", "simpleCorki");
            Menu.AddGroupLabel("Simple Corki");
            Menu.AddLabel("Version: " + "1.0.0.0 - 25.10.15 02:30 GMT+2");
            Menu.AddSeparator();
            Menu.AddLabel("By Pataxx");
            Menu.AddSeparator();
            Menu.AddLabel("Changes: First release, don't expect it to be perfect");
            Menu.AddLabel("Changes: Report bugs in the Thread!");
            Menu.AddSeparator();
            Menu.AddSeparator();
            Menu.AddLabel("Thanks to: Finndev, Hellsing, Fluxy");
            Menu.AddSeparator();

            ComboMenu = Menu.AddSubMenu("Combo", "SimpleCombo");
            ComboMenu.AddGroupLabel("Combo Settings");
            ComboMenu.Add("useQCombo", new CheckBox("Use Q", true));
            ComboMenu.Add("useECombo", new CheckBox("Use E", true));
            ComboMenu.Add("useRCombo", new CheckBox("Use R", true));

            ComboMenu.AddGroupLabel("Harass Settings");
            ComboMenu.Add("useQHarass", new CheckBox("Use Q", false));
            ComboMenu.Add("useRHarass", new CheckBox("Use R", true));
            ComboMenu.Add("manaHarass", new Slider("Manaslider", 50, 0, 100));
            ComboMenu.Add("saveHarass", new Slider("Save Rockets", 3, 0, 6));

            ComboMenu.AddGroupLabel("Laneclear Settings");
            ComboMenu.Add("useQLane", new CheckBox("Use Q", false));
            ComboMenu.Add("useRLane", new CheckBox("Use R", false));
            ComboMenu.Add("manaLane", new Slider("Manaslider", 65, 0, 100));
            ComboMenu.Add("saveLane", new Slider("Save Rockets", 3, 0, 6));

            KsMenu = Menu.AddSubMenu("Killsteal", "SimpleKS");
            KsMenu.AddGroupLabel("Killsteal Settings");
            KsMenu.Add("useRKs", new CheckBox("Use R", true));
            KsMenu.Add("useQKs", new CheckBox("Use Q", false));

            MiscMenu = Menu.AddSubMenu("Misc", "SimpleDraw");
            MiscMenu.AddGroupLabel("Draw Settings");
            MiscMenu.Add("drawQ", new CheckBox("Draw Q", true));
            MiscMenu.Add("drawW", new CheckBox("Draw W", false));
            MiscMenu.Add("drawE", new CheckBox("Draw E", false));
            MiscMenu.Add("drawR", new CheckBox("Draw R", true));
            MiscMenu.AddGroupLabel("Stuff");
            MiscMenu.Add("autoLv", new CheckBox("Auto Levelup", false));
            Activator.init();


            Game.OnTick += Game_OnTick;
            Game.OnUpdate += Game_OnUpdate;

            Drawing.OnDraw += Drawing_OnDraw;

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


        //---------------------------
        //---------------------------
        //---------------------------

        //Stuff
        private static void KillSteal()
        {
            var useR = KsMenu["useRKs"].Cast<CheckBox>().CurrentValue;
            var useQ = KsMenu["useQKs"].Cast<CheckBox>().CurrentValue;
            foreach (var target in EntityManager.Heroes.Enemies.Where(hero => hero.IsValidTarget(W.Range) && !hero.IsDead && !hero.IsZombie && hero.HealthPercent <= 25))
            {
                if (useR && R.IsReady() && target.Health + target.AttackShield < Player.Instance.GetSpellDamage(target, SpellSlot.R, DamageLibrary.SpellStages.Default))
                {
                    R.Cast(target);
                }

                if (useQ && Q.IsReady() && target.Health + target.AttackShield < Player.Instance.GetSpellDamage(target, SpellSlot.Q, DamageLibrary.SpellStages.Default) && target.Position.CountEnemiesInRange(800) == 1)
                {
                    Q.Cast(target);
                }
            }
        }
        //---------------------------
        //---------------------------
        //---------------------------

        //States
        private static void Combo()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            var targetR = TargetSelector.GetTarget(R.Range, DamageType.Magical);
            if (target == null) return;

            var useQ = ComboMenu["useQCombo"].Cast<CheckBox>().CurrentValue;
            var useE = ComboMenu["useECombo"].Cast<CheckBox>().CurrentValue;
            var useR = ComboMenu["useRCombo"].Cast<CheckBox>().CurrentValue;

            if (Orbwalker.IsAutoAttacking) return;

            if (useE && E.IsReady() && target.IsValidTarget(E.Range) && _Player.IsFacing(target))
            {
                E.Cast();
            }
            if (useQ && Q.IsReady() && target.IsValidTarget(Q.Range))
            {
                Q.Cast(target);
            }
            if (targetR != null && useR && R.IsReady() && targetR.IsValidTarget(R.Range) && R.Handle.Ammo > 0)
            {
                R.Cast(targetR);
            }
        }

        private static void Harass()
        {
            if (_Player.ManaPercent < ComboMenu["manaHarass"].Cast<Slider>().CurrentValue)
                return;
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            var targetR = TargetSelector.GetTarget(R.Range, DamageType.Magical);
            if (target == null) return;
            var useQ = ComboMenu["useQHarass"].Cast<CheckBox>().CurrentValue;
            var useR = ComboMenu["useRHarass"].Cast<CheckBox>().CurrentValue;
            if (Orbwalker.IsAutoAttacking) return;

            if (useQ && Q.IsReady() && target.IsValidTarget(Q.Range))
            {
                Q.Cast(target);
            }
            if (targetR != null && useR && R.IsReady() && targetR.IsValidTarget(R.Range) && R.Handle.Ammo > ComboMenu["saveHarass"].Cast<Slider>().CurrentValue)
            {
                R.Cast(targetR);
            }
        }

        private static void LaneClear()
        {
            if (_Player.ManaPercent < ComboMenu["manaLane"].Cast<Slider>().CurrentValue)
                return;

            var minionQ = EntityManager.MinionsAndMonsters.GetLaneMinions().Where(m => m.IsValidTarget(R.Range))
                .FirstOrDefault(unit => EntityManager.MinionsAndMonsters.EnemyMinions.Count(m => m.Distance(unit) < Q.Radius) > 2);

            var useQ = ComboMenu["useQLane"].Cast<CheckBox>().CurrentValue;
            var useR = ComboMenu["useRLane"].Cast<CheckBox>().CurrentValue;

            if (useR && minionQ != null && R.Handle.Ammo > ComboMenu["saveLane"].Cast<Slider>().CurrentValue && R.IsReady())
                R.Cast(minionQ.ServerPosition);
            if (useQ && minionQ != null && Q.IsReady() && minionQ.IsValidTarget(Q.Range))
                Q.Cast(minionQ);
        }
        //Drawings
        private static void Drawing_OnDraw(EventArgs args)
        {
            if (MiscMenu["drawQ"].Cast<CheckBox>().CurrentValue)
            {
                new Circle() { Color = Color.Red, BorderWidth = 1, Radius = Q.Range }.Draw(_Player.Position);
            }
            if (MiscMenu["drawW"].Cast<CheckBox>().CurrentValue)
            {
                new Circle() { Color = Color.Blue, BorderWidth = 1, Radius = W.Range }.Draw(_Player.Position);
            }
            if (MiscMenu["drawE"].Cast<CheckBox>().CurrentValue)
            {
                new Circle() { Color = Color.Blue, BorderWidth = 1, Radius = E.Range }.Draw(_Player.Position);
            }
            if (MiscMenu["drawR"].Cast<CheckBox>().CurrentValue)
            {
                new Circle() { Color = Color.Blue, BorderWidth = 1, Radius = R.Range }.Draw(_Player.Position);
            }
        }
        //---------------------------
        //---------------------------
        //---------------------------

        //Auto-Levelup
        private static void Game_OnUpdate(EventArgs args)
        {
            uint level = (uint)Player.Instance.Level;

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