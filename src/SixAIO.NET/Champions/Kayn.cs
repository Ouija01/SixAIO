﻿using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Kayn : Champion
    {
        public Kayn()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => QHitChance,
                Range = () => QMaximumRange,
                Radius = () => 350,
                Delay = () => 0f,
                Speed = () => UnitManager.MyChampion.UnitStats.MoveSpeed * 3,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) =>
                {
                    if (mode == Orbwalker.OrbWalkingModeType.LaneClear)
                    {
                        var heroTarget = SpellW.GetTargets(mode).FirstOrDefault();
                        if (heroTarget is null)
                        {
                            var targets = UnitManager.GetEnemies(ObjectTypeFlag.AIHeroClient, ObjectTypeFlag.AIMinionClient);
                            return targets
                                    .Where(x => x.Distance <= 1000 && (x.IsJungle || x.IsObject(ObjectTypeFlag.AIHeroClient)))
                                    .OrderByDescending(x => x.Health)
                                    .FirstOrDefault(TargetSelector.IsAttackable);
                        }
                    }

                    return SpellW.GetTargets(mode).FirstOrDefault();
                }
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => WHitChance,
                Range = () => WMaximumRange,
                Speed = () => 1500,
                Radius = () => 160,
                IsEnabled = () => UseW,
                TargetSelect = (mode) =>
                {
                    if (mode == Orbwalker.OrbWalkingModeType.LaneClear)
                    {
                        var heroTarget = SpellW.GetTargets(mode).FirstOrDefault();
                        if (heroTarget is null)
                        {
                            var targets = UnitManager.GetEnemies(ObjectTypeFlag.AIHeroClient, ObjectTypeFlag.AIMinionClient);
                            return targets
                                    .Where(x => x.Distance <= 1000 && (x.IsJungle || x.IsObject(ObjectTypeFlag.AIHeroClient)))
                                    .OrderByDescending(x => x.Health)
                                    .FirstOrDefault(TargetSelector.IsAttackable);
                        }
                    }

                    return SpellW.GetTargets(mode).FirstOrDefault();
                }
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            if ((UseWLaneclear && SpellW.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear)) ||
                (UseQLaneclear && SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear)))
            {
                return;
            }
        }


        private int QMaximumRange
        {
            get => QSettings.GetItem<Counter>("Q maximum range").Value;
            set => QSettings.GetItem<Counter>("Q maximum range").Value = value;
        }

        private int WMaximumRange
        {
            get => WSettings.GetItem<Counter>("W maximum range").Value;
            set => WSettings.GetItem<Counter>("W maximum range").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Kayn)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });
            QSettings.AddItem(new Counter() { Title = "Q maximum range", MinValue = 0, MaxValue = 700, Value = 600, ValueFrequency = 25 });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Switch() { Title = "Use W Laneclear", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });
            WSettings.AddItem(new Counter() { Title = "W maximum range", MinValue = 0, MaxValue = 900, Value = 600, ValueFrequency = 25 });

        }
    }
}
