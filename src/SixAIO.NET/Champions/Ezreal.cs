﻿using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Helpers;
using SixAIO.Models;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Ezreal : Champion
    {
        public Ezreal()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                Range = () => 1100,
                Width = () => 120,
                Speed = () => 2000,
                //Damage = (target, spellClass) =>
                //            target != null
                //            ? DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) *
                //            ((-5 + spellClass.Level * 25) +
                //            (UnitManager.MyChampion.UnitStats.TotalAttackDamage * 1.3f) +
                //            (UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.15f))
                //            : 0,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 40 &&
                            UnitManager.MyChampion.Mana > QMinMana &&
                            target != null,
                TargetSelect = (mode) =>
                            UnitManager.EnemyChampions
                            .Where(x => x.IsAlive && x.Distance <= SpellQ.Range() && TargetSelector.IsAttackable(x))
                            .OrderBy(x => x.Health)
                            .ThenBy(x => !Collision.MinionCollision(x.Position, 120))
                            .FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                Range = () => 1000,
                Width = () => 160,
                Speed = () => 1700,
                ShouldCast = (target, spellClass, damage) =>
                            UseW &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 50 &&
                            UnitManager.MyChampion.Mana > WMinMana &&
                            target != null,
                TargetSelect = (mode) =>
                {
                    if (WTargetshouldbecced || WTargetshouldbeslowed)
                    {
                        if (WTargetshouldbecced)
                        {
                            var target = UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= SpellW.Range() && x.IsAlive && TargetSelector.IsAttackable(x) &&
                                                                        x.BuffManager.GetBuffList().Any(BuffChecker.IsCrowdControlled));
                            if (target != null)
                            {
                                return target;
                            }
                        }
                        if (WTargetshouldbeslowed)
                        {
                            var target = UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= SpellW.Range() && x.IsAlive && TargetSelector.IsAttackable(x) &&
                                                                        x.BuffManager.GetBuffList().Any(BuffChecker.IsCrowdControlledOrSlowed));
                            if (target != null)
                            {
                                return target;
                            }
                        }
                    }
                    else
                    {
                        return UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= SpellW.Range() && x.IsAlive && TargetSelector.IsAttackable(x));
                    }

                    return null;
                }
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                CastTime = () => 1f,
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 90 &&
                            UnitManager.MyChampion.Mana > EMinMana &&
                            target != null,
                TargetSelect = (mode) =>
                            UnitManager.EnemyChampions
                            .Where(x => TargetSelector.IsAttackable(x))
                            .Where(x => x.Distance <= 800 && x.IsAlive)
                            .FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                Range = () => 30000,
                Width = () => 320,
                Speed = () => 2000,
                CastTime = () => 1f,
                Damage = (target, spellClass) =>
                            target != null
                            ? DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, target) *
                                        ((200 + spellClass.Level * 150) +
                                        (UnitManager.MyChampion.UnitStats.BonusAttackDamage) +
                                        (UnitManager.MyChampion.UnitStats.TotalAbilityPower))
                            : 0,
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 100 &&
                            UnitManager.MyChampion.Mana > RMinMana &&
                            target != null &&
                            target.Health < damage,
                TargetSelect = (mode) =>
                {
                    if (RTargetshouldbecced || RTargetshouldbeslowed)
                    {
                        if (RTargetshouldbecced)
                        {
                            var target = UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= SpellR.Range() && x.IsAlive && TargetSelector.IsAttackable(x) &&
                                                                        (x.Health / x.MaxHealth * 100) < RTargetMaxHPPercent &&
                                                                        x.BuffManager.GetBuffList().Any(BuffChecker.IsCrowdControlled));
                            if (target != null)
                            {
                                return target;
                            }
                        }
                        if (RTargetshouldbeslowed)
                        {
                            var target = UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= SpellR.Range() && x.IsAlive && TargetSelector.IsAttackable(x) &&
                                                                        (x.Health / x.MaxHealth * 100) < RTargetMaxHPPercent &&
                                                                        x.BuffManager.GetBuffList().Any(BuffChecker.IsCrowdControlledOrSlowed));
                            if (target != null)
                            {
                                return target;
                            }
                        }
                    }
                    else
                    {
                        return UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= SpellR.Range() && x.IsAlive && TargetSelector.IsAttackable(x) &&
                                                                        (x.Health / x.MaxHealth * 100) < RTargetMaxHPPercent);
                    }

                    return null;
                }
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellW.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        private int QMinMana
        {
            get => MenuTab.GetItem<Counter>("Q Min Mana").Value;
            set => MenuTab.GetItem<Counter>("Q Min Mana").Value = value;
        }

        private int WMinMana
        {
            get => MenuTab.GetItem<Counter>("W Min Mana").Value;
            set => MenuTab.GetItem<Counter>("W Min Mana").Value = value;
        }

        private bool WTargetshouldbeslowed
        {
            get => MenuTab.GetItem<Switch>("W Target should be slowed").IsOn;
            set => MenuTab.GetItem<Switch>("W Target should be slowed").IsOn = value;
        }

        private bool WTargetshouldbecced
        {
            get => MenuTab.GetItem<Switch>("W Target should be cc'ed").IsOn;
            set => MenuTab.GetItem<Switch>("W Target should be cc'ed").IsOn = value;
        }

        private int EMinMana
        {
            get => MenuTab.GetItem<Counter>("E Min Mana").Value;
            set => MenuTab.GetItem<Counter>("E Min Mana").Value = value;
        }

        private int RMinMana
        {
            get => MenuTab.GetItem<Counter>("R Min Mana").Value;
            set => MenuTab.GetItem<Counter>("R Min Mana").Value = value;
        }

        private int RTargetMaxHPPercent
        {
            get => MenuTab.GetItem<Counter>("R Target Max HP Percent").Value;
            set => MenuTab.GetItem<Counter>("R Target Max HP Percent").Value = value;
        }

        private bool RTargetshouldbeslowed
        {
            get => MenuTab.GetItem<Switch>("R Target should be slowed").IsOn;
            set => MenuTab.GetItem<Switch>("R Target should be slowed").IsOn = value;
        }

        private bool RTargetshouldbecced
        {
            get => MenuTab.GetItem<Switch>("R Target should be cc'ed").IsOn;
            set => MenuTab.GetItem<Switch>("R Target should be cc'ed").IsOn = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Ezreal)}"));
            MenuTab.AddItem(new InfoDisplay() { Title = "---Q Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "Q Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });

            MenuTab.AddItem(new InfoDisplay() { Title = "---W Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "W Min Mana", MinValue = 0, MaxValue = 500, Value = 0, ValueFrequency = 10 });
            MenuTab.AddItem(new Switch() { Title = "W Target should be slowed", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "W Target should be cc'ed", IsOn = true });

            //MenuTab.AddItem(new InfoDisplay() { Title = "---E Settings---" });
            //MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = false });
            //MenuTab.AddItem(new Counter() { Title = "E Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });

            MenuTab.AddItem(new InfoDisplay() { Title = "---R Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "R Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            MenuTab.AddItem(new Counter() { Title = "R Target Max HP Percent", MinValue = 10, MaxValue = 100, Value = 50, ValueFrequency = 5 });
            MenuTab.AddItem(new Switch() { Title = "R Target should be slowed", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "R Target should be cc'ed", IsOn = true });
        }
    }
}
