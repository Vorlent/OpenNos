﻿/*
 * This file is part of the OpenNos Emulator Project. See AUTHORS file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using OpenNos.Core;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject.Helpers;
using OpenNos.GameObject.Networking;
using OpenNos.PathFinder;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using static OpenNos.Domain.BCardType;

namespace OpenNos.GameObject
{
    public class MapMonster : MapMonsterDTO
    {
        #region Members

        private int _movetime;
        private Random _random;

        #endregion

        #region Instantiation

        public MapMonster(MapMonster mapMonster) : base(mapMonster)
        {
            this.CurrentHp = mapMonster.CurrentHp;
            this.CurrentMp = mapMonster.CurrentMp;
            this.Death = mapMonster.Death;
            this.IsAlive = mapMonster.IsAlive;
            this.IsBonus = mapMonster.IsBonus;
            this.IsBoss = mapMonster.IsBoss;
            this.NoticeRange = mapMonster.NoticeRange;
            this.IsHostile = mapMonster.IsHostile;
            this.IsTarget = mapMonster.IsTarget;
            this.LastEffect = mapMonster.LastEffect;
            this.LastMove = mapMonster.LastMove;
            this.LastSkill = mapMonster.LastSkill;
            this.MapInstance = mapMonster.MapInstance;

            this.ShouldRespawn = mapMonster.ShouldRespawn;
            this.Started = mapMonster.Started;
            this.FirstX = mapMonster.FirstX;
            this.FirstY = mapMonster.FirstY;
            
            HitQueue = new ConcurrentQueue<HitRequest>();
            OnDeathEvents = new List<EventContainer>();
            OnNoticeEvents = new List<EventContainer>();
        }

        public MapMonster()
        {
            HitQueue = new ConcurrentQueue<HitRequest>();
            OnDeathEvents = new List<EventContainer>();
            OnNoticeEvents = new List<EventContainer>();
        }

        #endregion

        #region Properties

        public ConcurrentBag<Buff> Buff { get; internal set; }

        public ConcurrentBag<BCard> SkillBcards { get; set; }

        public int CurrentHp { get; set; }

        public int CurrentMp { get; set; }

        public IDictionary<long, long> DamageList { get; private set; }

        public DateTime Death { get; set; }

        public ConcurrentQueue<HitRequest> HitQueue { get; }

        public bool IsAlive { get; set; }

        public bool IsFactionTargettable(FactionType faction)
        {
            switch (MonsterVNum)
            {
                case 679:
                    if (faction == FactionType.Angel)
                    {
                        return false;
                    }
                    break;
                case 680:
                    if (faction == FactionType.Demon)
                    {
                        return false;
                    }
                    break;
            }
            return true;
        }

        public bool IsBonus { get; set; }

        public bool IsBoss { get; set; }

        public byte NoticeRange { get; set; }

        public bool IsHostile { get; set; }

        public bool IsTarget { get; set; }

        public DateTime LastEffect { get; set; }

        public DateTime LastMove { get; set; }

        public DateTime LastSkill { get; set; }

        public IDisposable LifeEvent { get; set; }

        public MapInstance MapInstance { get; set; }

        public NpcMonster Monster { get; private set; }

        public List<EventContainer> OnDeathEvents { get; set; }

        public List<EventContainer> OnNoticeEvents { get; set; }

        public ZoneEvent MoveEvent { get; set; }

        public List<Node> Path { get; set; }

        public bool? ShouldRespawn { get; set; }

        public ConcurrentBag<NpcMonsterSkill> Skills { get; set; } = new ConcurrentBag<NpcMonsterSkill>();

        public bool Started { get; internal set; }

        public long Target { get; set; }

        public short FirstX { get; set; }

        public short FirstY { get; set; }
        public IDisposable Life { get; private set; }

        #endregion

        #region Methods

        public EffectPacket GenerateEff(int effectid)
        {
            return new EffectPacket
            {
                EffectType = 3,
                CharacterId = MapMonsterId,
                Id = effectid
            };
        }

        public string GenerateIn()
        {
            if (IsAlive && !IsDisabled)
            {
                return $"in 3 {MonsterVNum} {MapMonsterId} {MapX} {MapY} {Position} {(int)((float)CurrentHp / (float)Monster.MaxHP * 100)} {(int)((float)CurrentMp / (float)Monster.MaxMP * 100)} 0 0 0 -1 {(Monster.NoAggresiveIcon ? (byte)InRespawnType.NoEffect : (byte)InRespawnType.TeleportationEffect)} 0 -1 - 0 -1 0 0 0 0 0 0 0 0";
            }
            return string.Empty;
        }

        public string GenerateOut()
        {
            return $"out 3 {MapMonsterId}";
        }

        public string GenerateSay(string message, int type)
        {
            return $"say 3 {MapMonsterId} {type} {message}";
        }

        public void Initialize(MapInstance currentMapInstance)
        {
            MapInstance = currentMapInstance;
            Initialize();
            StartLife();
        }

        public override void Initialize()
        {
            FirstX = MapX;
            FirstY = MapY;
            LastSkill = LastMove = LastEffect = DateTime.Now;
            Target = -1;
            Path = new List<Node>();
            IsAlive = true;
            ShouldRespawn = ShouldRespawn ?? true;
            Monster = ServerManager.Instance.GetNpc(MonsterVNum);
            IsHostile = Monster.IsHostile;
            CurrentHp = Monster.MaxHP;
            CurrentMp = Monster.MaxMP;
            Monster.Skills.ForEach(s => Skills.Add(s));
            DamageList = new Dictionary<long, long>();
            _random = new Random(MapMonsterId);
            _movetime = ServerManager.Instance.RandomNumber(400, 3200);
            Buff = new ConcurrentBag<Buff>();
            SkillBcards = new ConcurrentBag<BCard>();
        }

        /// <summary>
        /// Check if the Monster is in the given Range.
        /// </summary>
        /// <param name="mapX">The X coordinate on the Map of the object to check.</param>
        /// <param name="mapY">The Y coordinate on the Map of the object to check.</param>
        /// <param name="distance">The maximum distance of the object to check.</param>
        /// <returns>True if the Monster is in range, False if not.</returns>
        public bool IsInRange(short mapX, short mapY, byte distance)
        {
            return Map.GetDistance(new MapCell {X = mapX, Y = mapY}, new MapCell {X = MapX, Y = MapY}) <= distance + 1;
        }

        public void RunDeathEvent()
        {
            if (IsBonus)
            {
                MapInstance.InstanceBag.Combo++;
                MapInstance.InstanceBag.Point += EventHelper.Instance.CalculateComboPoint(MapInstance.InstanceBag.Combo + 1);
            }
            else
            {
                MapInstance.InstanceBag.Combo = 0;
                MapInstance.InstanceBag.Point += EventHelper.Instance.CalculateComboPoint(MapInstance.InstanceBag.Combo);
            }
            MapInstance.InstanceBag.MonstersKilled++;
            OnDeathEvents.ForEach(e =>
            {
                EventHelper.Instance.RunEvent(e, monster: this);
            });
        }

        public void StartLife()
        {
          Life =  Observable.Interval(TimeSpan.FromMilliseconds(400)).Subscribe(x =>
            {
                try
                {
                    if (!MapInstance.IsSleeping)
                    {
                        MonsterLife();
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            });
        }

        private void GetNearestOponent()
        {
            if (Target != -1)
            {
                return;
            }
            const int maxDistance = 100;
            int distance = 100;
            List<ClientSession> sess = new List<ClientSession>();
            DamageList.Keys.ToList().ForEach(s => sess.Add(MapInstance.GetSessionByCharacterId(s)));
            ClientSession session = sess.OrderBy(s => distance = Map.GetDistance(new MapCell { X = MapX, Y = MapY }, new MapCell { X = s.Character.PositionX, Y = s.Character.PositionY })).FirstOrDefault();
            if (distance >= maxDistance)
            {
                return;
            }
            if (session != null)
            {
                Target = session.Character.CharacterId;
            }
        }

        private void HostilityTarget()
        {
            if (!IsHostile || Target != -1)
            {
                return;
            }
            Character character = ServerManager.Instance.Sessions.FirstOrDefault(s =>
                    s?.Character != null && s.Character.Hp > 0 && !s.Character.InvisibleGm && !s.Character.Invisible && s.Character.MapInstance == MapInstance &&
                    IsFactionTargettable(s.Character.Faction) &&
                    Map.GetDistance(new MapCell {X = MapX, Y = MapY}, new MapCell {X = s.Character.PositionX, Y = s.Character.PositionY}) < (NoticeRange == 0 ? Monster.NoticeRange : NoticeRange))
                ?.Character;
            if (character == null)
            {
                return;
            }
            if (!OnNoticeEvents.Any() && MoveEvent == null)
            {
                Target = character.CharacterId;
                if (!Monster.NoAggresiveIcon && LastEffect.AddSeconds(5) < DateTime.Now)
                {
                    character.Session.SendPacket(GenerateEff(5000));
                }
            }
            OnNoticeEvents.ForEach(e =>
            {
                EventHelper.Instance.RunEvent(e, monster: this);
            });
            OnNoticeEvents.RemoveAll(s => s != null);
        }

        /// <summary>
        /// Remove the current Target from Monster.
        /// </summary>
        internal void RemoveTarget()
        {
            if (Target == -1)
            {
                return;
            }
            Path.Clear();
            Target = -1;
            //return to origin
            Path = BestFirstSearch.FindPath(new Node { X = MapX, Y = MapY }, new Node { X = FirstX, Y = FirstY }, MapInstance.Map.Grid);
        }

        /// <summary>
        /// Follow the Monsters target to it's position.
        /// </summary>
        /// <param name="targetSession">The TargetSession to follow</param>
        private void FollowTarget(ClientSession targetSession)
        {
            if (!IsMoving)
            {
                return;
            }
            if (!Path.Any() && targetSession != null)
            {
                short xoffset = (short)ServerManager.Instance.RandomNumber(-1, 1);
                short yoffset = (short)ServerManager.Instance.RandomNumber(-1, 1);
                try
                {
                    List<Node> list = BestFirstSearch.TracePath(new Node() { X = MapX, Y = MapY }, targetSession.Character.BrushFire, targetSession.Character.MapInstance.Map.Grid);
                    Path = list;
                }
                catch (Exception ex)
                {
                    Logger.Log.Error($"Pathfinding using Pathfinder failed. Map: {MapId} StartX: {MapX} StartY: {MapY} TargetX: {(short)(targetSession.Character.PositionX + xoffset)} TargetY: {(short)(targetSession.Character.PositionY + yoffset)}", ex);
                    RemoveTarget();
                }
            }
            if (targetSession == null || MapId != targetSession.Character.MapInstance.Map.MapId)
            {
                RemoveTarget();
            }
        }

        /// <summary>
        /// Generate the Monster -&gt; Character Damage
        /// </summary>
        /// <param name="targetCharacter"></param>
        /// <param name="skill"></param>
        /// <param name="hitmode"></param>
        /// <returns></returns>
        private int GenerateDamage(Character targetCharacter, Skill skill, ref int hitmode)
        {
            #region Definitions

            if (targetCharacter == null)
            {
                return 0;
            }

            int playerDefense = targetCharacter.GetBuff(CardType.Defence, (byte)AdditionalTypes.Defence.AllIncreased)[0]
                              - targetCharacter.GetBuff(CardType.Defence, (byte)AdditionalTypes.Defence.AllDecreased)[0];

            byte playerDefenseUpgrade = (byte)(targetCharacter.GetBuff(CardType.Defence, (byte)AdditionalTypes.Defence.DefenceLevelIncreased)[0]
                                      - targetCharacter.GetBuff(CardType.Defence, (byte)AdditionalTypes.Defence.DefenceLevelDecreased)[0]);

            int playerDodge = targetCharacter.GetBuff(CardType.DodgeAndDefencePercent, (byte)AdditionalTypes.DodgeAndDefencePercent.DodgeIncreased)[0]
                            - targetCharacter.GetBuff(CardType.DodgeAndDefencePercent, (byte)AdditionalTypes.DodgeAndDefencePercent.DodgeDecreased)[0];

            int playerMorale = targetCharacter.Level + targetCharacter.GetBuff(CardType.Morale, (byte)AdditionalTypes.Morale.MoraleIncreased)[0]
                                                     - targetCharacter.GetBuff(CardType.Morale, (byte)AdditionalTypes.Morale.MoraleDecreased)[0];

            int Morale = Monster.Level + GetBuff(CardType.Morale, (byte)AdditionalTypes.Morale.MoraleIncreased)[0]
                                       - GetBuff(CardType.Morale, (byte)AdditionalTypes.Morale.MoraleDecreased)[0];

            if (targetCharacter.Inventory.Armor != null)
            {
                playerDefenseUpgrade += targetCharacter.Inventory.Armor.Upgrade;
            }

            short mainUpgrade = Monster.AttackUpgrade;
            int mainCritChance = Monster.CriticalChance;
            int mainCritHit = Monster.CriticalRate - 30;
            int mainMinDmg = Monster.DamageMinimum;
            int mainMaxDmg = Monster.DamageMaximum;
            int mainHitRate = Monster.Concentrate; //probably missnamed, check later
            if (mainMaxDmg == 0)
            {
                mainMinDmg = Monster.Level * 8;
                mainMaxDmg = Monster.Level * 12;
                mainCritChance = 10;
                mainCritHit = 120;
                mainHitRate = Monster.Level / 2 + 1;
            }

            #endregion

            #region Get Player defense

            skill?.BCards?.ToList().ForEach(s => SkillBcards.Add(s));

            int playerBoostpercentage;

            int boost = GetBuff(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.AllAttacksIncreased)[0]
                        - GetBuff(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.AllAttacksDecreased)[0];

            int boostpercentage = GetBuff(CardType.Damage, (byte)AdditionalTypes.Damage.DamageIncreased)[0]
                                  - GetBuff(CardType.Damage, (byte)AdditionalTypes.Damage.DamageDecreased)[0];

            switch (Monster.AttackClass)
            {
                case 0:
                    playerDefense += targetCharacter.Defence;
                    playerDodge += targetCharacter.DefenceRate;
                    playerBoostpercentage = targetCharacter.GetBuff(CardType.Defence, (byte)AdditionalTypes.Defence.MeleeIncreased)[0]
                                          - targetCharacter.GetBuff(CardType.Defence, (byte)AdditionalTypes.Defence.MeleeDecreased)[0];
                    playerDefense = (int)(playerDefense * (1 + playerBoostpercentage / 100D));

                    boost += GetBuff(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.MeleeAttacksIncreased)[0]
                           - GetBuff(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.MeleeAttacksDecreased)[0];
                    boostpercentage += GetBuff(CardType.Damage, (byte)AdditionalTypes.Damage.MeleeIncreased)[0]
                                     - GetBuff(CardType.Damage, (byte)AdditionalTypes.Damage.MeleeDecreased)[0];
                    break;

                case 1:
                    playerDefense += targetCharacter.DistanceDefence;
                    playerDodge += targetCharacter.DistanceDefenceRate;
                    playerBoostpercentage = targetCharacter.GetBuff(CardType.Defence, (byte)AdditionalTypes.Defence.RangedIncreased)[0]
                                          - targetCharacter.GetBuff(CardType.Defence, (byte)AdditionalTypes.Defence.RangedDecreased)[0];
                    playerDefense = (int)(playerDefense * (1 + playerBoostpercentage / 100D));

                    boost += GetBuff(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.RangedAttacksIncreased)[0]
                           - GetBuff(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.RangedAttacksDecreased)[0];
                    boostpercentage += GetBuff(CardType.Damage, (byte)AdditionalTypes.Damage.RangedIncreased)[0]
                                     - GetBuff(CardType.Damage, (byte)AdditionalTypes.Damage.RangedDecreased)[0];
                    break;

                case 2:
                    playerDefense += targetCharacter.MagicalDefence;
                    playerBoostpercentage = targetCharacter.GetBuff(CardType.Defence, (byte)AdditionalTypes.Defence.MagicalIncreased)[0]
                                          - targetCharacter.GetBuff(CardType.Defence, (byte)AdditionalTypes.Defence.MeleeDecreased)[0];
                    playerDefense = (int)(playerDefense * (1 + playerBoostpercentage / 100D));

                    boost += GetBuff(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.MagicalAttacksIncreased)[0]
                           - GetBuff(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.MagicalAttacksDecreased)[0];
                    boostpercentage += GetBuff(CardType.Damage, (byte)AdditionalTypes.Damage.MagicalIncreased)[0]
                                     - GetBuff(CardType.Damage, (byte)AdditionalTypes.Damage.MagicalDecreased)[0];
                    break;

                default:
                    throw new Exception($"Monster.AttackClass {Monster.AttackClass} not implemented");
            }

            mainMinDmg += boost;
            mainMaxDmg += boost;
            mainMinDmg = (int)(mainMinDmg * (1 + boostpercentage / 100D));
            mainMaxDmg = (int)(mainMaxDmg * (1 + boostpercentage / 100D));

            #endregion

            #region Basic Damage Data Calculation

            mainCritChance += targetCharacter.GetBuff(CardType.Critical, (byte)AdditionalTypes.Critical.ReceivingIncreased)[0]
                           + GetBuff(CardType.Critical, (byte)AdditionalTypes.Critical.InflictingIncreased)[0]
                           - targetCharacter.GetBuff(CardType.Critical, (byte)AdditionalTypes.Critical.ReceivingDecreased)[0]
                           - GetBuff(CardType.Critical, (byte)AdditionalTypes.Critical.InflictingReduced)[0];

            mainCritHit += GetBuff(CardType.Critical, (byte)AdditionalTypes.Critical.DamageIncreased)[0]
                         - GetBuff(CardType.Critical, (byte)AdditionalTypes.Critical.DamageIncreasedInflictingReduced)[0];

            // Critical damage deacreased by x %
            mainCritHit = (int)((mainCritHit / 100D) * (100 + targetCharacter.GetBuff(CardType.Critical, (byte)AdditionalTypes.Critical.DamageFromCriticalIncreased)[0]
                                                            - targetCharacter.GetBuff(CardType.Critical, (byte)AdditionalTypes.Critical.DamageFromCriticalDecreased)[0]));

            mainUpgrade -= playerDefenseUpgrade;

            // Useless
            /*if (mainUpgrade < -10)
            {
                mainUpgrade = -10;
            }
            else if (mainUpgrade > 10)
            {
                mainUpgrade = 10;
            }*/

            #endregion

            #region Detailed Calculation

            #region Dodge

            double multiplier = playerDodge / (double)mainHitRate;
            if (multiplier > 5)
            {
                multiplier = 5;
            }
            double chance = -0.25 * Math.Pow(multiplier, 3) - 0.57 * Math.Pow(multiplier, 2) + 25.3 * multiplier - 1.41;
            if (chance <= 1)
            {
                chance = 1;
            }
            if (Monster.AttackClass == 0 || Monster.AttackClass == 1)
            {
                if (ServerManager.Instance.RandomNumber() <= chance)
                {
                    hitmode = 1;
                    return 0;
                }
            }

            #endregion

            #region Base Damage

            int baseDamage = ServerManager.Instance.RandomNumber(mainMinDmg, mainMaxDmg + 1);
            baseDamage += Morale - playerMorale;

            playerDefense += (int)(playerDefense * Character.CalculateDefenseLevelModifier(mainUpgrade));
            baseDamage += (int)(baseDamage * Character.CalculateAttackLevelModifier(mainUpgrade));

            #endregion

            #region Elementary Damage

            int elementalDamage = GetBuff(CardType.Element, (byte)AdditionalTypes.Element.AllIncreased)[0] - GetBuff(CardType.Element, (byte)AdditionalTypes.Element.AllDecreased)[0];

            int bonusrez = targetCharacter.GetBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.AllIncreased)[0] 
                         - targetCharacter.GetBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.AllDecreased)[0];

            #region Calculate Elemental Boost + Rate

            double elementalBoost = 0;
            int playerRessistance = 0;
            switch ((ElementType)Monster.Element)
            {
                case ElementType.None:
                    break;

                case ElementType.Fire:
                    bonusrez += targetCharacter.GetBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.FireIncreased)[0]
                              - targetCharacter.GetBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.FireDecreased)[0];

                    elementalDamage += GetBuff(CardType.Element, (byte)AdditionalTypes.Element.FireIncreased)[0] 
                                     - GetBuff(CardType.Element, (byte)AdditionalTypes.Element.FireDecreased)[0];

                    playerRessistance = targetCharacter.FireResistance;
                    elementalBoost = Character.CalculateFireBoost((ElementType)targetCharacter.Element, elementalBoost);
                    break;

                case ElementType.Water:
                    bonusrez += targetCharacter.GetBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.WaterIncreased)[0]
                              - targetCharacter.GetBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.WaterDecreased)[0];
                    elementalDamage += GetBuff(CardType.Element, (byte)AdditionalTypes.Element.WaterIncreased)[0]
                                     - GetBuff(CardType.Element, (byte)AdditionalTypes.Element.WaterDecreased)[0];
                    playerRessistance = targetCharacter.WaterResistance;
                    elementalBoost = Character.CalculateWaterBoost((ElementType)targetCharacter.Element, elementalBoost);
                    break;

                case ElementType.Light:
                    bonusrez += targetCharacter.GetBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.LightIncreased)[0]
                              - targetCharacter.GetBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.LightDecreased)[0];
                    elementalDamage += GetBuff(CardType.Element, (byte)AdditionalTypes.Element.LightIncreased)[0]
                                     - GetBuff(CardType.Element, (byte)AdditionalTypes.Element.LightDecreased)[0];
                    playerRessistance = targetCharacter.LightResistance;
                    elementalBoost = Character.CalculateLightBoost((ElementType)targetCharacter.Element, elementalBoost);
                    break;

                case ElementType.Darkness:
                    bonusrez += targetCharacter.GetBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.DarkIncreased)[0]
                              - targetCharacter.GetBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.DarkDecreased)[0];
                    playerRessistance = targetCharacter.DarkResistance;
                    elementalDamage += GetBuff(CardType.Element, (byte)AdditionalTypes.Element.DarkIncreased)[0] 
                                     - GetBuff(CardType.Element, (byte)AdditionalTypes.Element.DarkDecreased)[0];
                    elementalBoost = Character.CalculateDarknessBoost((ElementType)targetCharacter.Element, elementalBoost);
                    break;
            }

            #endregion;

            if (Monster.Element == 0)
            {
                if (elementalBoost == 0.5)
                {
                    elementalBoost = 0;
                }
                else if (elementalBoost == 1)
                {
                    elementalBoost = 0.05;
                }
                else if (elementalBoost == 1.3)
                {
                    elementalBoost = 0;
                }
                else if (elementalBoost == 1.5)
                {
                    elementalBoost = 0.15;
                }
                else if (elementalBoost == 2)
                {
                    elementalBoost = 0.2;
                }
                else if (elementalBoost == 3)
                {
                    elementalBoost = 0.2;
                }
            }
            elementalDamage = (int)((elementalDamage + (100 + baseDamage) * (Monster.ElementRate / 100D)) * elementalBoost);
            elementalDamage = elementalDamage / 100 * (100 - playerRessistance - bonusrez);
            if (elementalDamage < 0)
            {
                elementalDamage = 0;
            }

            #endregion

            #region Critical Damage

            if (ServerManager.Instance.RandomNumber() <= mainCritChance)
            {
                if (Monster.AttackClass == 2)
                {
                }
                else
                {
                    baseDamage += (int)(baseDamage * (mainCritHit / 100D));
                    hitmode = 3;
                }
            }

            SkillBcards.Clear();
            
            #endregion

            #region Total Damage

            int totalDamage = baseDamage + elementalDamage - (targetCharacter.HasBuff(CardType.SpecialDefence, (byte)AdditionalTypes.SpecialDefence.AllDefenceNullified) ? 0 : playerDefense);
            if (totalDamage < 5)
            {
                totalDamage = ServerManager.Instance.RandomNumber(1, 6);
            }

            #endregion

            #endregion

            #region Minimum damage

            if (Monster.Level < 45)
            {
                //no minimum damage
            }
            else if (Monster.Level < 55)
            {
                totalDamage += Monster.Level;
            }
            else if (Monster.Level < 60)
            {
                totalDamage += Monster.Level * 2;
            }
            else if (Monster.Level < 65)
            {
                totalDamage += Monster.Level * 3;
            }
            else if (Monster.Level < 70)
            {
                totalDamage += Monster.Level * 4;
            }
            else
            {
                totalDamage += Monster.Level * 5;
            }

            #endregion

            return totalDamage;
        }


        private string GenerateMv3()
        {
            return $"mv 3 {MapMonsterId} {MapX} {MapY} {Monster.Speed}";
        }
        
        /// <summary>
        /// Handle any kind of Monster interaction
        /// </summary>
        private void MonsterLife()
        {
            if (Monster == null)
            {
                return;
            }

            // handle hit queue
            while (HitQueue.TryDequeue(out HitRequest hitRequest))
            {
                if (IsAlive && hitRequest.Session.Character.Hp > 0)
                {
                    int hitmode = 0;

                    // calculate damage
                    bool onyxWings = false;
                    int damage = hitRequest.Session.Character.GenerateDamage(this, hitRequest.Skill, ref hitmode, ref onyxWings);

                    if (onyxWings && MapInstance != null)
                    {
                        short onyxX = (short)(hitRequest.Session.Character.PositionX + 2);
                        short onyxY = (short)(hitRequest.Session.Character.PositionY + 2);
                        int onyxId = MapInstance.GetNextMonsterId();
                        MapMonster onyx = new MapMonster
                        {
                            MonsterVNum = 2371,
                            MapX = onyxX,
                            MapY = onyxY,
                            MapMonsterId = onyxId,
                            IsHostile = false,
                            IsMoving = false,
                            ShouldRespawn = false
                        };
                        MapInstance.Broadcast($"guri 31 1 {hitRequest.Session.Character.CharacterId} {onyxX} {onyxY}");
                        onyx.Initialize(MapInstance);
                        MapInstance.AddMonster(onyx);
                        MapInstance.Broadcast(onyx.GenerateIn());
                        CurrentHp -= damage / 2;
                        HitRequest request = hitRequest;
                        Observable.Timer(TimeSpan.FromMilliseconds(350)).Subscribe(o =>
                        {
                            MapInstance.Broadcast($"su 3 {onyxId} 3 {MapMonsterId} -1 0 -1 {request.Skill.Effect} -1 -1 1 92 {damage / 2} 0 0");
                            MapInstance.RemoveMonster(onyx);
                            MapInstance.Broadcast(onyx.GenerateOut());
                        });
                    }
                    switch (hitRequest.TargetHitType)
                    {
                        case TargetHitType.SingleTargetHit:
                            MapInstance?.Broadcast($"su 1 {hitRequest.Session.Character.CharacterId} 3 {MapMonsterId} {hitRequest.Skill.SkillVNum} {hitRequest.Skill.Cooldown} {hitRequest.Skill.AttackAnimation} {hitRequest.SkillEffect} {hitRequest.Session.Character.PositionX} {hitRequest.Session.Character.PositionY} {(IsAlive ? 1 : 0)} {(int)((float)CurrentHp / (float)Monster.MaxHP * 100)} {damage} {hitmode} {hitRequest.Skill.SkillType - 1}");
                            break;

                        case TargetHitType.SingleTargetHitCombo:
                            MapInstance?.Broadcast($"su 1 {hitRequest.Session.Character.CharacterId} 3 {MapMonsterId} {hitRequest.Skill.SkillVNum} {hitRequest.Skill.Cooldown} {hitRequest.SkillCombo.Animation} {hitRequest.SkillCombo.Effect} {hitRequest.Session.Character.PositionX} {hitRequest.Session.Character.PositionY} {(IsAlive ? 1 : 0)} {(int)((float)CurrentHp / (float)Monster.MaxHP * 100)} {damage} {hitmode} {hitRequest.Skill.SkillType - 1}");
                            break;

                        case TargetHitType.SingleAOETargetHit:
                            switch (hitmode)
                            {
                                case 1:
                                    hitmode = 4;
                                    break;

                                case 3:
                                    hitmode = 6;
                                    break;

                                default:
                                    hitmode = 5;
                                    break;
                            }
                            if (hitRequest.ShowTargetHitAnimation)
                            {
                                MapInstance?.Broadcast($"su 1 {hitRequest.Session.Character.CharacterId} 3 {MapMonsterId} {hitRequest.Skill.SkillVNum} {hitRequest.Skill.Cooldown} {hitRequest.Skill.AttackAnimation} {hitRequest.SkillEffect} 0 0 {(IsAlive ? 1 : 0)} {(int)((float)CurrentHp / (float)Monster.MaxHP * 100)} 0 0 {hitRequest.Skill.SkillType - 1}");
                            }
                            MapInstance?.Broadcast($"su 1 {hitRequest.Session.Character.CharacterId} 3 {MapMonsterId} {hitRequest.Skill.SkillVNum} {hitRequest.Skill.Cooldown} {hitRequest.Skill.AttackAnimation} {hitRequest.SkillEffect} {hitRequest.Session.Character.PositionX} {hitRequest.Session.Character.PositionY} {(IsAlive ? 1 : 0)} {(int)((float)CurrentHp / (float)Monster.MaxHP * 100)} {damage} {hitmode} {hitRequest.Skill.SkillType - 1}");
                            break;

                        case TargetHitType.AOETargetHit:
                            switch (hitmode)
                            {
                                case 1:
                                    hitmode = 4;
                                    break;

                                case 3:
                                    hitmode = 6;
                                    break;

                                default:
                                    hitmode = 5;
                                    break;
                            }
                            MapInstance?.Broadcast($"su 1 {hitRequest.Session.Character.CharacterId} 3 {MapMonsterId} {hitRequest.Skill.SkillVNum} {hitRequest.Skill.Cooldown} {hitRequest.Skill.AttackAnimation} {hitRequest.SkillEffect} {hitRequest.Session.Character.PositionX} {hitRequest.Session.Character.PositionY} {(IsAlive ? 1 : 0)} {(int)((float)CurrentHp / (float)Monster.MaxHP * 100)} {damage} {hitmode} {hitRequest.Skill.SkillType - 1}");
                            break;

                        case TargetHitType.ZoneHit:
                            MapInstance?.Broadcast($"su 1 {hitRequest.Session.Character.CharacterId} 3 {MapMonsterId} {hitRequest.Skill.SkillVNum} {hitRequest.Skill.Cooldown} {hitRequest.Skill.AttackAnimation} {hitRequest.SkillEffect} {hitRequest.MapX} {hitRequest.MapY} {(IsAlive ? 1 : 0)} {(int)((float)CurrentHp / (float)Monster.MaxHP * 100)} {damage} 5 {hitRequest.Skill.SkillType - 1}");
                            break;

                        case TargetHitType.SpecialZoneHit:
                            MapInstance?.Broadcast($"su 1 {hitRequest.Session.Character.CharacterId} 3 {MapMonsterId} {hitRequest.Skill.SkillVNum} {hitRequest.Skill.Cooldown} {hitRequest.Skill.AttackAnimation} {hitRequest.SkillEffect} {hitRequest.Session.Character.PositionX} {hitRequest.Session.Character.PositionY} {(IsAlive ? 1 : 0)} {(int)((float)CurrentHp / (float)Monster.MaxHP * 100)} {damage} 0 {hitRequest.Skill.SkillType - 1}");
                            break;
                    }

                    // generate the kill bonus
                    hitRequest.Session.Character.GenerateKillBonus(this);
                }
                else
                {
                    // monster already has been killed, send cancel
                    hitRequest.Session.SendPacket($"cancel 2 {MapMonsterId}");
                }
                if (IsBoss)
                {
                    MapInstance?.Broadcast(GenerateBoss());
                }
                else
                {
                    hitRequest.Skill.BCards.ToList().ForEach(b => b.ApplyBCards(this, hitRequest.Session.Character));
                }
            }

            // Respawn
            if (!IsAlive && ShouldRespawn != null && ShouldRespawn.Value)
            {
                double timeDeath = (DateTime.Now - Death).TotalSeconds;
                if (timeDeath >= Monster.RespawnTime / 10d)
                {
                    Respawn();
                }
            }
            // normal movement
            Move();
            // target following
            if (Target == -1)
            {
                return;
            }
            if (MapInstance == null)
            {
                return;
            }
            GetNearestOponent();
            HostilityTarget();

            ClientSession targetSession = MapInstance.GetSessionByCharacterId(Target);

            // remove target in some situations
            if (targetSession == null || targetSession.Character.Invisible || targetSession.Character.Hp <= 0 || CurrentHp <= 0)
            {
                RemoveTarget();
                return;
            }

            lock (targetSession)
            {
                NpcMonsterSkill npcMonsterSkill = null;
                if (ServerManager.Instance.RandomNumber(0, 10) > 8 && Skills != null)
                {
                    npcMonsterSkill = Skills.Where(s => (DateTime.Now - s.LastSkillUse).TotalMilliseconds >= 100 * s.Skill.Cooldown).OrderBy(rnd => _random.Next()).FirstOrDefault();
                }

                if (npcMonsterSkill?.Skill.TargetType == 1 && npcMonsterSkill.Skill.HitType == 0)
                {
                    TargetHit(targetSession, npcMonsterSkill);
                }

                // check if target is in range
                if (!targetSession.Character.InvisibleGm && !targetSession.Character.Invisible && targetSession.Character.Hp > 0)
                {
                    if (npcMonsterSkill != null && CurrentMp >= npcMonsterSkill.Skill.MpCost &&
                        Map.GetDistance(new MapCell
                            {
                                X = MapX,
                                Y = MapY
                            },
                            new MapCell
                            {
                                X = targetSession.Character.PositionX,
                                Y = targetSession.Character.PositionY
                            }) < npcMonsterSkill.Skill.Range)
                    {
                        TargetHit(targetSession, npcMonsterSkill);
                    }
                    else if (Map.GetDistance(new MapCell
                                 {
                                     X = MapX,
                                     Y = MapY
                                 },
                                 new MapCell
                                 {
                                     X = targetSession.Character.PositionX,
                                     Y = targetSession.Character.PositionY
                                 }) <= Monster.BasicRange)
                    {
                        TargetHit(targetSession, npcMonsterSkill);
                    }
                    else
                    {
                        FollowTarget(targetSession);
                    }
                }
                else
                {
                    FollowTarget(targetSession);
                }
            }
        }
        public void ShowEffect()
        {
            if (!((DateTime.Now - LastEffect).TotalSeconds >= 5))
            {
                return;
            }
            if (IsTarget)
            {

                MapInstance.Broadcast(GenerateEff(824));
            }
            if (IsBonus)
            {
                MapInstance.Broadcast(GenerateEff(826));
            }
            LastEffect = DateTime.Now;
        }
        public string GenerateBoss()
        {
            return $"rboss 3 {MapMonsterId} {CurrentHp} {Monster.MaxHP}";
        }

        private void Move()
        {
            // Normal Move Mode
            if (Monster == null || !IsAlive || HasBuff(CardType.Move, (byte)AdditionalTypes.Move.MovementImpossible))
            {
                return;
            }

            if (IsMoving && Monster.Speed > 0)
            {
                double time = (DateTime.Now - LastMove).TotalMilliseconds;

                if (Path.Any())
                {

                    int timetowalk = 2000 / Monster.Speed;
                    if (time > timetowalk)
                    {
                        int maxindex = Path.Count > Monster.Speed / 2 ? Monster.Speed / 2 : Path.Count;
                        short mapX = Path.ElementAt(maxindex - 1).X;
                        short mapY = Path.ElementAt(maxindex - 1).Y;
                        double waitingtime = Map.GetDistance(new MapCell {X = mapX, Y = mapY}, new MapCell {X = MapX, Y = MapY}) / (double) Monster.Speed;
                        LastMove = DateTime.Now.AddSeconds(waitingtime > 1 ? 1 : waitingtime);

                        Observable.Timer(TimeSpan.FromMilliseconds(timetowalk)).Subscribe(x =>
                        {
                            MapX = mapX;
                            MapY = mapY;

                            MoveEvent?.Events.ToList().ForEach(e =>
                            {
                                EventHelper.Instance.RunEvent(e, monster: this);
                            });
                            if (MoveEvent != null && MoveEvent.InZone(MapX, MapY))
                            {
                                MoveEvent = null;
                            }
                        });
                        Path.RemoveRange(0, maxindex);
                        MapInstance.Broadcast(new BroadcastPacket(null, GenerateMv3(), ReceiverType.All, xCoordinate: mapX, yCoordinate: mapY));
                        return;
                    }
                }
                else if (time > _movetime && Target == -1)
                {
                    short mapX = FirstX, mapY = FirstY;
                    if (MapInstance.Map?.GetFreePosition(ref mapX, ref mapY, (byte)ServerManager.Instance.RandomNumber(0, 2), (byte)_random.Next(0, 2)) ?? false)
                    {
                        int distance = Map.GetDistance(new MapCell
                        {
                            X = mapX,
                            Y = mapY
                        }, new MapCell
                        {
                            X = MapX,
                            Y = MapY
                        });

                        double value = 1000d * distance / (2 * Monster.Speed);
                        Observable.Timer(TimeSpan.FromMilliseconds(value))
                    .Subscribe(
                        x =>
                        {
                            MapX = mapX;
                            MapY = mapY;
                        });

                        LastMove = DateTime.Now.AddMilliseconds(value);
                        MapInstance.Broadcast(new BroadcastPacket(null, GenerateMv3(), ReceiverType.All));
                    }
                }
            }
            HostilityTarget();
        }

        private void Respawn()
        {
            if (Monster == null)
            {
                return;
            }
            DamageList = new Dictionary<long, long>();
            IsAlive = true;
            Target = -1;
            CurrentHp = Monster.MaxHP;
            CurrentMp = Monster.MaxMP;
            MapX = FirstX;
            MapY = FirstY;
            Path = new List<Node>();
            MapInstance.Broadcast(GenerateIn());
            Monster.BCards.ForEach(s => s.ApplyBCards(this));
        }

        /// <summary>
        /// Hit the Target Character.
        /// </summary>
        /// <param name="targetSession"></param>
        /// <param name="npcMonsterSkill"></param>
        private void TargetHit(ClientSession targetSession, NpcMonsterSkill npcMonsterSkill)
        {
            if (Monster == null || (!((DateTime.Now - LastSkill).TotalMilliseconds >= 1000 + Monster.BasicCooldown * 200) && npcMonsterSkill == null) || HasBuff(CardType.SpecialAttack, (byte)AdditionalTypes.SpecialAttack.NoAttack))
            {
                return;
            }
            int hitmode = 0;

            int damage = npcMonsterSkill != null ? GenerateDamage(targetSession.Character, npcMonsterSkill.Skill, ref hitmode) : GenerateDamage(targetSession.Character, null, ref hitmode);

            if (npcMonsterSkill != null)
            {
                if (CurrentMp < npcMonsterSkill.Skill.MpCost)
                {
                    FollowTarget(targetSession);
                    return;
                }
                npcMonsterSkill.LastSkillUse = DateTime.Now;
                CurrentMp -= npcMonsterSkill.Skill.MpCost;
                MapInstance.Broadcast($"ct 3 {MapMonsterId} 1 {Target} {npcMonsterSkill.Skill.CastAnimation} {npcMonsterSkill.Skill.CastEffect} {npcMonsterSkill.Skill.SkillVNum}");
            }
            LastMove = DateTime.Now;

            // deal 0 damage to GM with GodMode
            if (targetSession.Character.HasGodMode)
            {
                damage = 0;
            }
            if (targetSession.Character.IsSitting)
            {
                targetSession.Character.IsSitting = false;
                MapInstance.Broadcast(targetSession.Character.GenerateRest());
            }
            int castTime = 0;
            if (npcMonsterSkill != null && npcMonsterSkill.Skill.CastEffect != 0)
            {
                MapInstance.Broadcast(GenerateEff(npcMonsterSkill.Skill.CastEffect), MapX, MapY);
                castTime = npcMonsterSkill.Skill.CastTime * 100;
            }
            Observable.Timer(TimeSpan.FromMilliseconds(castTime))
                .Subscribe(
                    o =>
                    {
                        if (targetSession.Character.Hp > 0)
                        {
                            TargetHit2(targetSession, npcMonsterSkill, damage, hitmode);
                        }
                    });
        }

        private void TargetHit2(ClientSession targetSession, NpcMonsterSkill npcMonsterSkill, int damage, int hitmode)
        {
            if (targetSession.Character.Hp > 0)
            {
                targetSession.Character.GetDamage(damage);

                MapInstance.Broadcast(null, ServerManager.Instance.GetUserMethod<string>(Target, "GenerateStat"), ReceiverType.OnlySomeone, "", Target);

                MapInstance.Broadcast(npcMonsterSkill != null
                    ? $"su 3 {MapMonsterId} 1 {Target} {npcMonsterSkill.SkillVNum} {npcMonsterSkill.Skill.Cooldown} {npcMonsterSkill.Skill.AttackAnimation} {npcMonsterSkill.Skill.Effect} {MapX} {MapY} {(targetSession.Character.Hp > 0 ? 1 : 0)} {(int) (targetSession.Character.Hp / targetSession.Character.HPLoad() * 100)} {damage} {hitmode} 0"
                    : $"su 3 {MapMonsterId} 1 {Target} 0 {Monster.BasicCooldown} 11 {Monster.BasicSkill} 0 0 {(targetSession.Character.Hp > 0 ? 1 : 0)} {(int) (targetSession.Character.Hp / targetSession.Character.HPLoad() * 100)} {damage} {hitmode} 0");
                npcMonsterSkill?.Skill.BCards.ToList().ForEach(s => s.ApplyBCards(targetSession.Character));
                LastSkill = DateTime.Now;
                if (targetSession.Character.Hp <= 0)
                {
                    RemoveTarget();
                    Observable.Timer(TimeSpan.FromMilliseconds(1000)).Subscribe(o => { ServerManager.Instance.AskRevive(targetSession.Character.CharacterId); });
                }
            }
            if (npcMonsterSkill == null || (npcMonsterSkill.Skill.Range <= 0 && npcMonsterSkill.Skill.TargetRange <= 0))
            {
                return;
            }
            foreach (Character characterInRange in MapInstance
                .GetCharactersInRange(npcMonsterSkill.Skill.TargetRange == 0 ? MapX : targetSession.Character.PositionX,
                    npcMonsterSkill.Skill.TargetRange == 0 ? MapY : targetSession.Character.PositionY, npcMonsterSkill.Skill.TargetRange)
                .Where(s => s.CharacterId != Target && s.Hp > 0 && !s.InvisibleGm))
            {
                if (characterInRange.IsSitting)
                {
                    characterInRange.IsSitting = false;
                    MapInstance.Broadcast(characterInRange.GenerateRest());
                }
                if (characterInRange.HasGodMode)
                {
                    damage = 0;
                    hitmode = 1;
                }
                if (characterInRange.Hp <= 0)
                {
                    continue;
                }
                characterInRange.GetDamage(damage);
                MapInstance.Broadcast(null, characterInRange.GenerateStat(), ReceiverType.OnlySomeone, "", characterInRange.CharacterId);
                MapInstance.Broadcast(
                    $"su 3 {MapMonsterId} 1 {characterInRange.CharacterId} 0 {Monster.BasicCooldown} 11 {Monster.BasicSkill} 0 0 {(characterInRange.Hp > 0 ? 1 : 0)} {(int) (characterInRange.Hp / characterInRange.HPLoad() * 100)} {damage} {hitmode} 0");
                if (characterInRange.Hp > 0)
                {
                    continue;
                }
                RemoveTarget();
                Observable.Timer(TimeSpan.FromMilliseconds(1000)).Subscribe(o => { ServerManager.Instance.AskRevive(characterInRange.CharacterId); });
            }
        }

        public void AddBuff(Buff indicator)
        {
            if (indicator?.Card == null)
            {
                return;
            }
            Buff = Buff.Where(s => !s.Card.CardId.Equals(indicator.Card.CardId));
            indicator.RemainingTime = indicator.Card.Duration;
            indicator.Start = DateTime.Now;
            Buff.Add(indicator);
            indicator.Card.BCards.ForEach(c => c.ApplyBCards(this));
            if (indicator.Card.EffectId > 0)
            {
                GenerateEff(indicator.Card.EffectId);
            }
            Observable.Timer(TimeSpan.FromMilliseconds(indicator.Card.Duration * 100)).Subscribe(o =>{ RemoveBuff(indicator.Card.CardId); });
        }

        private void RemoveBuff(int id)
        {
            Buff indicator = Buff.FirstOrDefault(s => s.Card.CardId == id);
            if (indicator == null)
            {
                return;
            }
            if (Buff.Contains(indicator))
            {
                Buff = Buff.Where(s => s.Card.CardId != id);
            }
        }

        public int[] GetBuff(CardType type, byte subtype, bool affectingOpposite = false)
        {
            int value1 = 0;
            int value2 = 0;

            foreach (BCard entry in SkillBcards.Where(s => s != null && s.Type.Equals((byte)type) && s.SubType.Equals(subtype)))
            {
                if (entry.IsLevelScaled)
                {
                    if (entry.IsLevelDivided)
                    {
                        value1 += Monster.Level / entry.FirstData;
                    }
                    else
                    {
                        value1 += entry.FirstData * Monster.Level;
                    }
                }
                else
                {
                    value1 += entry.FirstData;
                }
                value2 += entry.SecondData;
            }

            foreach (Buff buff in Buff)
            {
                foreach (BCard entry in buff.Card.BCards.Where(s =>
                    s.Type.Equals((byte)type) && s.SubType.Equals(subtype) &&
                    (s.CastType != 1 || s.CastType == 1 && buff.Start.AddMilliseconds(buff.Card.Delay * 100) < DateTime.Now)))
                {
                    if (entry.IsLevelScaled)
                    {
                        if (entry.IsLevelDivided)
                        {
                            value1 += buff.Level / entry.FirstData;
                        }
                        else
                        {
                            value1 += entry.FirstData * buff.Level;
                        }
                    }
                    else
                    {
                        value1 += entry.FirstData;
                    }
                    value2 += entry.SecondData;
                }
            }

            return new[] { value1, value2 };
        }

        // NoAttack // NoMove [...]
        public bool HasBuff(CardType type, byte subtype)
        {
            return Buff.Any(buff => buff.Card.BCards.Any(b => b.Type == (byte)type && b.SubType == subtype && (b.CastType != 1 || b.CastType == 1 && buff.Start.AddMilliseconds(buff.Card.Delay * 100) < DateTime.Now)));
        }
        #endregion
    }
}