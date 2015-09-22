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


namespace SimpleTristana
{
    class Activator
    {
        public static Menu Menu;
        public static AIHeroClient _Player
        {
            get { return ObjectManager.Player; }
        }

        public static void init()
        {
            Loading.OnLoadingComplete += Game_OnStart;
        }
        public static float BotrkDamage(Obj_AI_Base target)
        {
            return _Player.CalculateDamageOnUnit(target, DamageType.Physical, target.MaxHealth * (float)0.1);
        }
        private static void Game_OnStart(EventArgs args)
        {
            Menu = Program.Menu.AddSubMenu("Activator", "simpleActivator");
            Menu.AddGroupLabel("Potion Settings");
            Menu.Add("useHP", new CheckBox("Use Health Potion", true));
            Menu.Add("hpSlider", new Slider("At % HP", 25, 0, 100));
            Menu.Add("useMP", new CheckBox("Use Mana Potion", true));
            Menu.Add("mpSlider", new Slider("At % HP", 25, 0, 100));
            Menu.Add("useFlask", new CheckBox("Use Elixir in Combo", true));

            Menu.AddGroupLabel("Item Settings");
            Menu.Add("useBOTRK", new CheckBox("Use BOTRK", true));
            Menu.Add("useSmartBOTRK", new CheckBox("Smart BOTRK", true));
            Menu.Add("useBOTRKks", new CheckBox("BOTRK KS", true));
            Menu.Add("useGhostB", new CheckBox("Use Ghostblade", true));

            Game.OnTick += Game_OnTick;
        }

        private static void Game_OnTick(EventArgs args)
        {
            Orbwalker.ForcedTarget = null;
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }
            if(Menu["useBOTRKks"].Cast<CheckBox>().CurrentValue)
                Killsteal();
            Survival();
        }

        private static void Combo()
        {
            var target = TargetSelector2.GetTarget(900, DamageType.Physical);
            if (target == null) return;
            if (Orbwalker.IsAutoAttacking) return;

            if (Menu["useBOTRK"].Cast<CheckBox>().CurrentValue)
            {
                var botrkCheck = _Player.InventoryItems.FirstOrDefault(a => a.Id == (ItemId)3153);
                var bilgeCheck = _Player.InventoryItems.FirstOrDefault(a => a.Id == (ItemId)3144);
                var ghostCheck = _Player.InventoryItems.FirstOrDefault(a => a.Id == (ItemId)3142);

                var ironCheck = _Player.InventoryItems.FirstOrDefault(a => a.Id == ItemId.Elixir_of_Iron);
                var ruinCheck = _Player.InventoryItems.FirstOrDefault(a => a.Id == ItemId.Elixir_of_Ruin);
                var sorceryCheck = _Player.InventoryItems.FirstOrDefault(a => a.Id == ItemId.Elixir_of_Sorcery);
                var wrathCheck = _Player.InventoryItems.FirstOrDefault(a => a.Id == ItemId.Elixir_of_Wrath);


                if (bilgeCheck != null)
                {
                    var itSlot = bilgeCheck.SpellSlot;
                    Player.CastSpell(itSlot, target);
                }

                if (botrkCheck != null && !Menu["useSmartBOTRK"].Cast<CheckBox>().CurrentValue)
                {
                    var itSlot = botrkCheck.SpellSlot;
                    Player.CastSpell(itSlot, target);
                }

                if (botrkCheck != null && Menu["useSmartBOTRK"].Cast<CheckBox>().CurrentValue)
                {
                    var itSlot = botrkCheck.SpellSlot;
                    if (_Player.Health + BotrkDamage(target) <= _Player.MaxHealth)
                    {
                        Player.CastSpell(itSlot, target);
                    }
                }

                if(ghostCheck != null && Menu["useGhostB"].Cast<CheckBox>().CurrentValue && target.Distance(_Player) <= Program.Q.Range)
                {
                    var itSlot = ghostCheck.SpellSlot;
                    Player.CastSpell(itSlot, target);
                }

                if (Menu["useFlask"].Cast<CheckBox>().CurrentValue && !_Player.HasBuff("ElixirOfRuin") && !_Player.HasBuff("ElixirOfIron") && !_Player.HasBuff("ElixirOfSorcery") && !_Player.HasBuff("ElixirOfWrath"))
                {
                    if(ironCheck !=null)
                    {
                        var itSlot = ironCheck.SpellSlot;
                        Player.CastSpell(itSlot);
                    }
                    else if(ruinCheck != null)
                    {
                        var itSlot = ruinCheck.SpellSlot;
                        Player.CastSpell(itSlot);
                    }
                    else if(sorceryCheck !=null)
                    {
                        var itSlot = sorceryCheck.SpellSlot;
                        Player.CastSpell(itSlot);
                    }
                    else if(wrathCheck !=null)
                    {
                        var itSlot = wrathCheck.SpellSlot;
                        Player.CastSpell(itSlot);
                    }
                }

            }


        }
        private static void Killsteal()
        {
            var botrkCheck = _Player.InventoryItems.FirstOrDefault(a => a.Id == (ItemId)3153);
            var bilgeCheck = _Player.InventoryItems.FirstOrDefault(a => a.Id == (ItemId)3144);
            
            if (bilgeCheck != null)
            {
                var itSlot = bilgeCheck.SpellSlot;
                foreach (var target in HeroManager.Enemies.Where(hero => hero.IsValidTarget(550) && !hero.IsDead && !hero.IsZombie && hero.Health <= BotrkDamage(hero)))
                {
                    Player.CastSpell(itSlot, target);
                }
            }

            if (botrkCheck != null)
            {
                var itSlot = botrkCheck.SpellSlot;
                foreach (var target in HeroManager.Enemies.Where(hero => hero.IsValidTarget(550) && !hero.IsDead && !hero.IsZombie && hero.Health <= BotrkDamage(hero)))
                {
                    Player.CastSpell(itSlot, target);
                }
            }
        }
        private static void Survival()
        {
            if (!Menu["useHP"].Cast<CheckBox>().CurrentValue && !Menu["useMP"].Cast<CheckBox>().CurrentValue && !Menu["useFlask"].Cast<CheckBox>().CurrentValue)
                return;
            var hpotionCheck = _Player.InventoryItems.FirstOrDefault(a => a.Id == ItemId.Health_Potion);
            var mpotionCheck = _Player.InventoryItems.FirstOrDefault(a => a.Id == ItemId.Mana_Potion);

            if (hpotionCheck !=null && Menu["useHP"].Cast<CheckBox>().CurrentValue && _Player.HealthPercent <= Menu["hpSlider"].Cast<Slider>().CurrentValue && !_Player.HasBuff("RegenerationPotion"))
            {
                var itSlot = hpotionCheck.SpellSlot;
                Player.CastSpell(itSlot);
            }
            if (mpotionCheck != null && Menu["useMP"].Cast<CheckBox>().CurrentValue && _Player.ManaPercent <= Menu["mpSlider"].Cast<Slider>().CurrentValue && !_Player.HasBuff("FlaskOfCrystalWater"))
            {
                var itSlot = mpotionCheck.SpellSlot;
                Player.CastSpell(itSlot);
            }
        }
    }
}
