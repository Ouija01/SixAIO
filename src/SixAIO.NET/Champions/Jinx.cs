﻿using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using System;
using System.Linq;
using System.Windows.Forms;

namespace SixAIO.Champions
{
    internal sealed class Jinx : Champion
    {
        private static bool IsQActive()
        {
            var buff = UnitManager.MyChampion.BuffManager.ActiveBuffs.FirstOrDefault(x => x.Name == "JinxQ");
            return buff != null && buff.IsActive && buff.Stacks >= 1;
        }

        public Jinx()
        {
            Oasys.Common.Logic.Orbwalker.OnOrbwalkerBeforeBasicAttack += Orbwalker_OnOrbwalkerBeforeBasicAttack;
            Oasys.SDK.InputProviders.KeyboardProvider.OnKeyPress += KeyboardProvider_OnKeyPress;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                MinimumMana = () => 20,
                IsEnabled = () => UseQ,
                ShouldCast = (mode, target, spellClass, damage) =>
                {
                    if (!UseQLaneclear && mode == Orbwalker.OrbWalkingModeType.LaneClear)
                    {
                        return false;
                    }

                    var usingRockets = IsQActive();
                    var usingMinigun = !usingRockets;
                    var extraRange = 50 + (30 * UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q).Level);
                    var minigunRange = usingRockets
                                        ? UnitManager.MyChampion.TrueAttackRange - extraRange
                                        : UnitManager.MyChampion.TrueAttackRange;
                    minigunRange = Math.Min(QMinigunMaximumRange, minigunRange);

                    var rocketRange = usingRockets
                                        ? UnitManager.MyChampion.TrueAttackRange
                                        : UnitManager.MyChampion.TrueAttackRange + extraRange;
                    rocketRange = Math.Max(QMinigunMaximumRange, rocketRange);

                    Orbwalker.SelectedTarget = Oasys.Common.Logic.Orbwalker.GetTarget((Oasys.Common.Logic.OrbwalkingMode)mode, rocketRange);

                    if (mode == Orbwalker.OrbWalkingModeType.LaneClear)
                    {
                        if (ShouldUseRocketsForAOE(usingRockets, Orbwalker.LaneClearTarget))
                        {
                            return !usingRockets;
                        }

                        return QPreferRockets
                            ? !usingRockets
                            : usingRockets;
                    }

                    if (usingMinigun && Orbwalker.TargetHero != null && Orbwalker.TargetHero.Distance > minigunRange)
                    {
                        return true;
                    }
                    if (ShouldUseRocketsForAOE(usingRockets, Orbwalker.TargetHero))
                    {
                        return !usingRockets;
                    }
                    else if (usingRockets && Orbwalker.TargetHero != null && Orbwalker.TargetHero.Distance < minigunRange)
                    {
                        return true;
                    }

                    if (!usingRockets && Orbwalker.TargetHero == null)
                    {
                        return QPreferRockets
                                ? !usingRockets
                                : UnitManager.EnemyChampions.Any(x => x.Distance < rocketRange && TargetSelector.IsAttackable(x));
                    }

                    return false;
                }
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => WHitChance,
                Range = () => 1500,
                Radius = () => 120,
                Speed = () => 3300,
                Delay = () => 0.4f,
                MinimumMana = () => 90,
                //Damage = (target, spellClass) =>
                //            target != null
                //            ? DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) *
                //            ((10 + spellClass.Level * 40) +
                //            (UnitManager.MyChampion.UnitStats.TotalAttackDamage * (1.15f + 0.15f * spellClass.Level)))
                //            : 0,
                IsEnabled = () => UseW && (!WOnlyOutsideOfAttackRange || !UnitManager.EnemyChampions.Any(TargetSelector.IsInRange)),
                TargetSelect = (mode) => SpellW.GetTargets(mode, x => x.Distance > WMinimumRange).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => EHitChance,
                Radius = () => 120,
                Speed = () => 1500,
                Range = () => 925,
                MinimumMana = () => 90,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => EOnSelf && UnitManager.EnemyChampions.Any(x => x.Distance <= EMaximumRange && TargetSelector.IsAttackable(x))
                                        ? UnitManager.MyChampion
                                        : EOnlyOnSelf
                                            ? null
                                            : SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                AllowCastOnMap = () => AllowRCastOnMinimap,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => RHitChance,
                Range = () => 30000,
                Radius = () => 280,
                Speed = () => 2000,
                Delay = () => 0.6f,
                MinimumMana = () => 100,
                //Damage = (target, spellClass) =>
                //            target != null
                //            ? DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, target) *
                //                        ((100 + spellClass.Level * 150) +
                //                        (1.5f*UnitManager.MyChampion.UnitStats.BonusAttackDamage) +
                //                         (target.Health / target.MaxHealth * 100) < 50))
                //            : 0,
                IsEnabled = () => UseR,
                IsSpellReady = (spellClass, minimumMana, minimumCharges) =>
                                spellClass.IsSpellReady &&
                                UnitManager.MyChampion.Mana >= spellClass.SpellData.ResourceCost &&
                                UnitManager.MyChampion.Mana >= minimumMana &&
                                spellClass.Charges >= minimumCharges &&
                                !ROnlyOutsideOfAttackRange || !UnitManager.EnemyChampions.Any(TargetSelector.IsInRange),
                TargetSelect = (mode) => SpellR.GetTargets(mode, x => x.HealthPercent <= RTargetMaxHPPercent && x.Distance > RMinimumRange && x.Distance <= RMaximumRange).FirstOrDefault()
            };
            SpellRSemiAuto = new Spell(CastSlot.R, SpellSlot.R)
            {
                AllowCastOnMap = () => AllowRCastOnMinimap,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => SemiAutoRHitChance,
                Range = () => 30000,
                Radius = () => 280,
                Speed = () => 2000,
                Delay = () => 0.6f,
                MinimumMana = () => 100,
                IsEnabled = () => UseSemiAutoR,
                TargetSelect = (mode) => SpellRSemiAuto.GetTargets(mode, x => x.Distance > RMinimumRange && x.Distance <= RMaximumRange).FirstOrDefault()
            };
        }

        private void Orbwalker_OnOrbwalkerBeforeBasicAttack(float gameTime, GameObjectBase target)
        {
            if (!IsQActive() && ShouldUseRocketsForAOE(false, target))
            {
                SpellCastProvider.CastSpell(CastSlot.Q);
            }
        }

        private bool ShouldUseRocketsForAOE(bool usingRockets, GameObjectBase orbTarget)
        {
            if (!UseRocketsForAOE)
            {
                return false;
            }

            if (orbTarget is null)
            {
                return usingRockets;
            }

            var championNear = UnitManager.EnemyChampions.Any(x => x.NetworkID != orbTarget.NetworkID && x.DistanceTo(orbTarget.Position) <= QAOERadius && TargetSelector.IsAttackable(x));
            if (championNear)
            {
                return true;
            }

            if (orbTarget.IsObject(ObjectTypeFlag.AIMinionClient))
            {
                var minionNear = UnitManager.EnemyMinions.Any(x => x.NetworkID != orbTarget.NetworkID && x.DistanceTo(orbTarget.Position) <= QAOERadius && TargetSelector.IsAttackable(x));
                if (minionNear)
                {
                    return true;
                }
                var monsterNear = UnitManager.EnemyJungleMobs.Any(x => x.NetworkID != orbTarget.NetworkID && x.DistanceTo(orbTarget.Position) <= QAOERadius && TargetSelector.IsAttackable(x));
                if (monsterNear)
                {
                    return true;
                }
            }

            return false;
        }

        private void KeyboardProvider_OnKeyPress(Keys keyBeingPressed, Oasys.Common.Tools.Devices.Keyboard.KeyPressState pressState)
        {
            if (keyBeingPressed == SemiAutoRKey && pressState == Oasys.Common.Tools.Devices.Keyboard.KeyPressState.Down)
            {
                SpellRSemiAuto.ExecuteCastSpell();
            }
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            if (SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
        }

        private bool UseRocketsForAOE
        {
            get => QSettings.GetItem<Switch>("Use Rockets For AOE").IsOn;
            set => QSettings.GetItem<Switch>("Use Rockets For AOE").IsOn = value;
        }

        private int QAOERadius
        {
            get => QSettings.GetItem<Counter>("Q AOE Radius").Value;
            set => QSettings.GetItem<Counter>("Q AOE Radius").Value = value;
        }

        private bool QPreferRockets
        {
            get => QSettings.GetItem<Switch>("Q prefer rockets").IsOn;
            set => QSettings.GetItem<Switch>("Q prefer rockets").IsOn = value;
        }

        private int QMinigunMaximumRange
        {
            get => QSettings.GetItem<Counter>("Q Minigun Maximum Range").Value;
            set => QSettings.GetItem<Counter>("Q Minigun Maximum Range").Value = value;
        }

        private bool WOnlyOutsideOfAttackRange
        {
            get => WSettings.GetItem<Switch>("W only outside of attack range").IsOn;
            set => WSettings.GetItem<Switch>("W only outside of attack range").IsOn = value;
        }

        private int WMinimumRange
        {
            get => WSettings.GetItem<Counter>("W minimum range").Value;
            set => WSettings.GetItem<Counter>("W minimum range").Value = value;
        }

        private bool EOnSelf
        {
            get => ESettings.GetItem<Switch>("E on self").IsOn;
            set => ESettings.GetItem<Switch>("E on self").IsOn = value;
        }

        private bool EOnlyOnSelf
        {
            get => ESettings.GetItem<Switch>("E only on self").IsOn;
            set => ESettings.GetItem<Switch>("E only on self").IsOn = value;
        }

        private int EMaximumRange
        {
            get => ESettings.GetItem<Counter>("E enemy maximum range").Value;
            set => ESettings.GetItem<Counter>("E enemy maximum range").Value = value;
        }

        private bool ROnlyOutsideOfAttackRange
        {
            get => RSettings.GetItem<Switch>("R only outside of attack range").IsOn;
            set => RSettings.GetItem<Switch>("R only outside of attack range").IsOn = value;
        }

        private int RMinimumRange
        {
            get => RSettings.GetItem<Counter>("R minimum range").Value;
            set => RSettings.GetItem<Counter>("R minimum range").Value = value;
        }

        private int RMaximumRange
        {
            get => RSettings.GetItem<Counter>("R maximum range").Value;
            set => RSettings.GetItem<Counter>("R maximum range").Value = value;
        }

        private int RTargetMaxHPPercent
        {
            get => RSettings.GetItem<Counter>("R Target Max HP Percent").Value;
            set => RSettings.GetItem<Counter>("R Target Max HP Percent").Value = value;
        }


        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Jinx)}"));

            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Rockets For AOE", IsOn = true });
            QSettings.AddItem(new Counter() { Title = "Q AOE Radius", MinValue = 25, MaxValue = 250, Value = 250, ValueFrequency = 25 });
            QSettings.AddItem(new Switch() { Title = "Q prefer rockets", IsOn = false });
            QSettings.AddItem(new Counter() { Title = "Q Minigun Maximum Range", MinValue = 0, MaxValue = 750, Value = 750, ValueFrequency = 25 });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });
            WSettings.AddItem(new Switch() { Title = "W only outside of attack range", IsOn = false });
            WSettings.AddItem(new Counter() { Title = "W minimum range", MinValue = 0, MaxValue = 1500, Value = 0, ValueFrequency = 50 });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "Immobile" });
            ESettings.AddItem(new InfoDisplay() { Title = "---E anti melee Settings---" });
            ESettings.AddItem(new Switch() { Title = "E on self", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "E only on self", IsOn = false });
            ESettings.AddItem(new Counter() { Title = "E enemy maximum range", MinValue = 0, MaxValue = 900, Value = 250, ValueFrequency = 50 });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });


            RSettings.AddItem(new Switch() { Title = "Use Semi Auto R", IsOn = true });
            RSettings.AddItem(new KeyBinding() { Title = "Semi Auto R Key", SelectedKey = Keys.T });
            RSettings.AddItem(new ModeDisplay() { Title = "Semi Auto R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });


            RSettings.AddItem(new Switch() { Title = "Allow R cast on minimap", IsOn = true });
            RSettings.AddItem(new Switch() { Title = "R only outside of attack range", IsOn = false });
            RSettings.AddItem(new Counter() { Title = "R minimum range", MinValue = 0, MaxValue = 30_000, Value = 0, ValueFrequency = 50 });
            RSettings.AddItem(new Counter() { Title = "R maximum range", MinValue = 0, MaxValue = 30_000, Value = 30_000, ValueFrequency = 50 });
            RSettings.AddItem(new Counter() { Title = "R Target Max HP Percent", MinValue = 10, MaxValue = 100, Value = 50, ValueFrequency = 5 });
        }
    }
}
