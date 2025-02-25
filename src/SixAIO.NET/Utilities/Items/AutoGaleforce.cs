﻿using Oasys.Common.Enums.GameEnums;
using Oasys.Common.EventsProvider;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.SpellCasting;
using SixAIO.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SixAIO.Utilities
{
    internal sealed class AutoGaleforce
    {
        private static Tab Tab => MenuManagerProvider.GetTab("SIXAIO - Items");
        private static Group AutoGaleforceGroup => Tab.GetGroup("Auto Galeforce");

        private static bool UseGaleforce
        {
            get => AutoGaleforceGroup.GetItem<Switch>("Use Galeforce").IsOn;
            set => AutoGaleforceGroup.GetItem<Switch>("Use Galeforce").IsOn = value;
        }

        private static DashMode DashModeSelected
        {
            get => (DashMode)Enum.Parse(typeof(DashMode), AutoGaleforceGroup.GetItem<ModeDisplay>("Dash Mode").SelectedModeName);
            set => AutoGaleforceGroup.GetItem<ModeDisplay>("Dash Mode").SelectedModeName = value.ToString();
        }

        internal static Task GameEvents_OnGameLoadComplete()
        {
            Tab.AddGroup(new Group("Auto Galeforce"));
            AutoGaleforceGroup.AddItem(new Switch() { Title = "Use Galeforce", IsOn = false });
            AutoGaleforceGroup.AddItem(new ModeDisplay() { Title = "Dash Mode", ModeNames = DashHelper.ConstructDashModeTable(), SelectedModeName = "ToMouse" });

            CoreEvents.OnCoreMainInputAsync += InputHandler;
            return Task.CompletedTask;
        }

        private static Task InputHandler()
        {
            try
            {
                if (UseGaleforce &&
                    UnitManager.MyChampion.IsAlive &&
                    TargetSelector.IsAttackable(UnitManager.MyChampion, false) &&
                    DashModeSelected == DashMode.ToMouse &&
                    UnitManager.EnemyChampions.Any(x => x.IsAlive && x.Distance <= 750 && x.Health <= GetGaleforceDamage(x)))
                {
                    if (UnitManager.MyChampion.Inventory.HasItem(ItemID.Galeforce) &&
                        UnitManager.MyChampion.Inventory.GetItemByID(ItemID.Galeforce)?.IsReady == true)
                    {
                        ItemCastProvider.CastItem(ItemID.Galeforce);
                    }
                }
            }
            catch (Exception ex)
            {
            }

            return Task.CompletedTask;
        }

        private static float GetGaleforceDamage(Hero enemy)
        {
            var magicDamage = (float)(UnitManager.MyChampion.Level <= 9 ? 60 : 60 + (5 * (UnitManager.MyChampion.Level - 9)));
            magicDamage += UnitManager.MyChampion.UnitStats.BonusAttackDamage * 0.15f;
            magicDamage *= 3;

            var missingHealthPercent = 100f - enemy.HealthPercent;
            var dmgMod = missingHealthPercent / 7 * 5 / 100;
            dmgMod = Math.Min(dmgMod, 0.5f);

            magicDamage *= 1f + dmgMod;

            var magicResMod = DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, enemy);

            var result = (float)(magicDamage * magicResMod - enemy.NeutralShield - enemy.MagicalShield);
            return result;
        }
    }
}
