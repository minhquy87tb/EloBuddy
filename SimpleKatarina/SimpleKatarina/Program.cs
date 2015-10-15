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


namespace SimpleKatarina
{
    class Program
    {
        //Skills
        public static Spell.Targeted Q;
        public static Spell.Active W;
        public static Spell.Targeted E;
        public static Spell.Active R;
        private static Spell.Targeted _ignite;
        private static float _rTick = 0;
        private static bool none;
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
            if (!_Player.ChampionName.Contains("Katarina")) return;
            Bootstrap.Init(null);
            uint level = (uint)Player.Instance.Level;
            Q = new Spell.Targeted(SpellSlot.Q, 675);
            W = new Spell.Active(SpellSlot.W, 375);
            E = new Spell.Targeted(SpellSlot.E, 700);
            R = new Spell.Active(SpellSlot.R, 550);
            if(Player.Spells.FirstOrDefault(o => o.SData.Name.Contains("summonerdot")) != null)
                _ignite = new Spell.Targeted(ObjectManager.Player.GetSpellSlotFromName("summonerdot"), 600);

            Menu = MainMenu.AddMenu("Simple Katarina", "simpleKata");
            Menu.AddGroupLabel("Simple Katarina");
            Menu.AddLabel("Version: " + "1.0.0.0 - 15.10.15 09:00 GMT+2");
            Menu.AddLabel("First public release, report bugs in the forum!");
            Menu.AddSeparator();
            Menu.AddLabel("By Pataxx");
            Menu.AddSeparator();
            Menu.AddLabel("Thanks to: Finndev, Hellsing, Fluxy");
            Menu.AddSeparator();

            ComboMenu = Menu.AddSubMenu("Combo", "SimpleCombo");
            ComboMenu.AddGroupLabel("Combo Settings");
            ComboMenu.Add("useQCombo", new CheckBox("Use Q", true));
            ComboMenu.Add("useWCombo", new CheckBox("Use W", true));
            ComboMenu.Add("useECombo", new CheckBox("Use E", true));
            ComboMenu.Add("useETCombo", new CheckBox("Towerdive with E", true));
            ComboMenu.AddGroupLabel("R Settings");
            var rUsage = ComboMenu.Add("RSettings", new Slider("Select R Usage", 1, 0, 1));
            var diff = new[] { "Burst Mode", "Smart R" };
            rUsage.DisplayName = diff[rUsage.CurrentValue];
            rUsage.OnValueChange += delegate (ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args2)
            {
                sender.DisplayName = diff[args2.NewValue];
            };

            HarassMenu = Menu.AddSubMenu("Harass", "SimpleHarass");
            HarassMenu.AddGroupLabel("Harass Settings");
            HarassMenu.Add("useQHarass", new CheckBox("Use Q", true));
            HarassMenu.Add("useWHarass", new CheckBox("Use W", true));
            HarassMenu.Add("useEHarass", new CheckBox("Use E", false));
            HarassMenu.AddGroupLabel("Auto Harass");
            HarassMenu.Add("autoQHarass", new CheckBox("Auto Q", true));
            HarassMenu.Add("autoWHarass", new CheckBox("Auto W", true));
            HarassMenu.Add("autoHToggle", new KeyBind("Auto Harass Toggle", false, KeyBind.BindTypes.PressToggle, 'Y'));


            KsMenu = Menu.AddSubMenu("Killsteal", "SimpleKS");
            KsMenu.AddGroupLabel("Killsteal Settings");
            KsMenu.Add("useQKs", new CheckBox("Use Q", true));
            KsMenu.Add("useWKs", new CheckBox("Use W", false));
            KsMenu.Add("useEKs", new CheckBox("Use E", true));
            KsMenu.Add("useIKs", new CheckBox("Use Ignite", true));
            KsMenu.Add("useUCKs", new CheckBox("Cancel Ult for KS", true));


            MiscMenu = Menu.AddSubMenu("Misc", "SimpleDraw");
            MiscMenu.AddGroupLabel("Draw Settings");
            MiscMenu.Add("drawQ", new CheckBox("Draw Q", true));
            MiscMenu.Add("drawW", new CheckBox("Draw W", true));
            MiscMenu.Add("drawE", new CheckBox("Draw E", true));
            MiscMenu.Add("drawR", new CheckBox("Draw R", false));
            MiscMenu.AddGroupLabel("Stuff");
            MiscMenu.Add("wardJump", new CheckBox("Ward Jump (use Flee mode)", true));
            MiscMenu.Add("isKillable", new CheckBox("Draw if Killable", true));
            //Activator.init();


            Game.OnTick += Game_OnTick;
            Player.OnIssueOrder += Player_OnIssueOrder;
            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
            Drawing.OnDraw += Drawing_OnDraw;
            
        }
        private static void Game_OnTick(EventArgs args)
        {
            if (_Player.IsDead) return;

            if(_rTick !=0)
                checkCancel();
            if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee) && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear) && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
                none = true;
            else
                none = false;

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                Harass();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee) && MiscMenu["wardJump"].Cast<CheckBox>().CurrentValue)
            {
                wardjump();
            }
            KillSteal();
            if(HarassMenu["autoHToggle"].Cast<KeyBind>().CurrentValue && none)
                AutoStuff();
        }

        //---------------------------
        //---------------------------
        //---------------------------

        //Stuff
        private static void AutoStuff()
        {
            if (Orbwalker.IsAutoAttacking) return;
            var tar = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            if (tar == null) return;
            if(HarassMenu["autoQHarass"].Cast<CheckBox>().CurrentValue && Q.IsReady() && tar.IsValidTarget(Q.Range) && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.None))
            {
                Q.Cast(tar);
            }
            if(HarassMenu["autoWHarass"].Cast<CheckBox>().CurrentValue && W.IsReady() && tar.IsValidTarget(W.Range) && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.None))
            {
                W.Cast();
            }

        }
        private static void KillSteal()
        {
            var useQ = KsMenu["useQKs"].Cast<CheckBox>().CurrentValue;
            var useW = KsMenu["useWKs"].Cast<CheckBox>().CurrentValue;
            var useE = KsMenu["useEKs"].Cast<CheckBox>().CurrentValue;
            var useI = KsMenu["useIKs"].Cast<CheckBox>().CurrentValue;
            var useUC = KsMenu["useUCKs"].Cast<CheckBox>().CurrentValue;
            if (!useUC && Player.HasBuff("katarinarsound"))
                return;
            foreach (var target in EntityManager.Heroes.Enemies.Where(hero => hero.IsValidTarget(E.Range) && !hero.IsDead && !hero.IsZombie && hero.HealthPercent <= 25))
            {
                if (useQ && Q.IsReady() && target.Health < Player.Instance.GetSpellDamage(target, SpellSlot.Q, DamageLibrary.SpellStages.Default))
                {
                    Q.Cast(target);
                }
                if (useW && W.IsReady() && target.Health < Player.Instance.GetSpellDamage(target, SpellSlot.W, DamageLibrary.SpellStages.Default))
                {
                    W.Cast();
                }
                if (useE && E.IsReady() && target.Health < Player.Instance.GetSpellDamage(target, SpellSlot.E, DamageLibrary.SpellStages.Default) && target.Position.CountEnemiesInRange(800) == 1)
                {
                    E.Cast(target);
                }
                if (Player.Spells.FirstOrDefault(o => o.SData.Name.Contains("summonerdot")) != null && useI && _ignite.IsReady() && target.Health < Player.Instance.GetSpellDamage(target, _ignite.Slot, DamageLibrary.SpellStages.Default))
                {
                    _ignite.Cast(target);
                }
            }
            
        }
        //---------------------------
        //---------------------------
        //---------------------------

        //States
        private static void Combo()
        {
            var target = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            if (target == null) return;
            var nearTurret = EntityManager.Turrets.Enemies.FirstOrDefault(a => !a.IsDead && a.Distance(target) <= 775 + _Player.BoundingRadius + target.BoundingRadius + 44.2); //yolo, should work
            var useQ = ComboMenu["useQCombo"].Cast<CheckBox>().CurrentValue;
            var useW = ComboMenu["useWCombo"].Cast<CheckBox>().CurrentValue;
            var useE = ComboMenu["useECombo"].Cast<CheckBox>().CurrentValue;
            var useET = ComboMenu["useETCombo"].Cast<CheckBox>().CurrentValue;

            if (Orbwalker.IsAutoAttacking) return;

            if (useQ && Q.IsReady() && target.IsValidTarget(Q.Range) && !_Player.HasBuff("katarinarsound"))
            {
                Q.Cast(target);
            }
            if (useW && W.IsReady() && target.IsValidTarget(W.Range) && !_Player.HasBuff("katarinarsound") )
            {
                W.Cast();
            }
            if (useE && E.IsReady() && target.IsValidTarget(E.Range) && !_Player.HasBuff("katarinarsound"))
            {
                if (useET)
                    E.Cast(target);
                else if (nearTurret == null)
                    E.Cast(target);
                else if (_Player.Distance(nearTurret.Position) < 775 + (nearTurret.BoundingRadius / 2))
                    E.Cast(target);
            }
            if (ComboMenu["RSettings"].Cast<Slider>().CurrentValue == 1 && target.Health < Player.Instance.GetSpellDamage(target, SpellSlot.R) && R.IsReady() && target.IsValidTarget(R.Range) && !E.IsReady())
            {
                R.Cast();
                _rTick = Environment.TickCount;
            }
            else if (ComboMenu["RSettings"].Cast<Slider>().CurrentValue == 0 && R.IsReady() && target.IsValidTarget(R.Range) && !E.IsReady())
            {
                R.Cast();
                _rTick = Environment.TickCount;
            }


        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            if (target == null)
                return;
            var nearTurret = EntityManager.Turrets.Enemies.FirstOrDefault(a => !a.IsDead && a.Distance(target) <= 775 + _Player.BoundingRadius + target.BoundingRadius + 44.2); //yolo, should work

            

            var useQ = HarassMenu["useQHarass"].Cast<CheckBox>().CurrentValue;
            var useW = HarassMenu["useWHarass"].Cast<CheckBox>().CurrentValue;
            var useE = HarassMenu["useEHarass"].Cast<CheckBox>().CurrentValue;

            if (Orbwalker.IsAutoAttacking) return;

            if (useQ && Q.IsReady() && target.IsValidTarget(Q.Range))
            {
                Q.Cast(target);
            }
            if (useW && W.IsReady() && target.IsValidTarget(W.Range))
            {
                W.Cast();
            }
            if (useE && E.IsReady() && target.IsValidTarget(E.Range) && nearTurret == null)
            {
                E.Cast(target);
            }
        }

        //---------------------------
        //---------------------------
        //---------------------------

        //Drawings
        private static void Drawing_OnDraw(EventArgs args)
        {
            if (_Player.IsDead) return;

            if (MiscMenu["isKillable"].Cast<CheckBox>().CurrentValue)
            {
                foreach (var unit in EntityManager.Heroes.Enemies.Where(u => u.IsValid && u.IsHPBarRendered))
                {
                    var hpPos = unit.HPBarPosition;
                    var damage = ComboDMG(unit);
                    if (unit.Health + unit.MagicShield< damage)
                        Drawing.DrawText(hpPos.X - 10, hpPos.Y + 40, Color.Lime, "Is Killable");
                }
            }


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
                new Circle() { Color = Color.LightGreen, BorderWidth = 1, Radius = E.Range }.Draw(_Player.Position);
            }
            if (MiscMenu["drawR"].Cast<CheckBox>().CurrentValue)
            {
                new Circle() { Color = Color.DarkOrange, BorderWidth = 1, Radius = R.Range}.Draw(_Player.Position);
            }
        }

        //---------------------------
        //---------------------------
        //---------------------------
         private static void Player_OnIssueOrder(GameObject sender, PlayerIssueOrderEventArgs args)
         {
            if (sender.IsMe && Environment.TickCount < _rTick + 300 && _rTick != 0)
             {
                 args.Process = false;
             }
         }

        private static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            args.Process = !_Player.HasBuff("katarinarsound");
        }

        private static void checkCancel()
        {
            if(Player.HasBuff("katarinarsound") && EntityManager.Heroes.Enemies.Count(e => e.Distance(_Player) < 550 && e.Health > 0 && !e.IsZombie) < 1)
            {
                var tar = TargetSelector.GetTarget(E.Range, DamageType.Magical);
                _rTick = 0;
                if(tar!=null)
                    Player.IssueOrder(GameObjectOrder.AttackTo, tar);
            }
        }

        private static void wardjump()
        {
            if(E.IsReady())
            {
                var wardIDs = new[] { ItemId.Farsight_Orb_Trinket, ItemId.Warding_Totem_Trinket, ItemId.Greater_Stealth_Totem_Trinket, ItemId.Greater_Vision_Totem_Trinket, ItemId.Stealth_Ward, ItemId.Sightstone, ItemId.Ruby_Sightstone, ItemId.Vision_Ward};
                var use = _Player.InventoryItems.FirstOrDefault(a => wardIDs.Contains(a.Id) && a.CanUseItem());
                var jumpto = ObjectManager.Get<Obj_AI_Base>().FirstOrDefault(a => a.Name.ToLower().Contains("ward") && a.Distance(_Player) <= 900);
                if (jumpto != null)
                {
                    E.Cast(jumpto);
                    return;
                }
                if (use != null)
                {
                    use.Cast(_Player.Position.Extend(Game.CursorPos, 600).To3D());
                    return;
                }
            }

        }
        private static float ComboDMG(Obj_AI_Base enemy)
        {
            float x = 0;

            if (Q.IsReady())
                x += Player.Instance.GetSpellDamage(enemy, SpellSlot.Q, DamageLibrary.SpellStages.Default);
            if (W.IsReady())
                x += Player.Instance.GetSpellDamage(enemy, SpellSlot.W, DamageLibrary.SpellStages.Default);
            if (E.IsReady())
                x += Player.Instance.GetSpellDamage(enemy, SpellSlot.E, DamageLibrary.SpellStages.Default);
            if (R.IsReady())
                x += Player.Instance.GetSpellDamage(enemy, SpellSlot.R, DamageLibrary.SpellStages.Default);
            return x;
        }
    }
}
