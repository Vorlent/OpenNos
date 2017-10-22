/*
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

namespace OpenNos.Data
{
    public class MapNpcDTO : MappingBaseDTO
    {
        public MapNpcDTO()
        {
        }

            public MapNpcDTO(MapNpcDTO mapNpcDTO)
        {
            this.Dialog = mapNpcDTO.Dialog;
            this.Effect = mapNpcDTO.Effect;
            this.EffectDelay = mapNpcDTO.EffectDelay;
            this.IsDisabled = mapNpcDTO.IsDisabled;
            this.IsMoving = mapNpcDTO.IsMoving;
            this.IsSitting = mapNpcDTO.IsSitting;
            this.MapId = mapNpcDTO.MapId;
            this.MapNpcId = mapNpcDTO.MapNpcId;
            this.MapX = mapNpcDTO.MapX;
            this.MapY = mapNpcDTO.MapY;
            this.NpcVNum = mapNpcDTO.NpcVNum;
            this.Position = mapNpcDTO.Position;
        }

        #region Properties

        public short Dialog { get; set; }

        public short Effect { get; set; }

        public short EffectDelay { get; set; }

        public bool IsDisabled { get; set; }

        public bool IsMoving { get; set; }

        public bool IsSitting { get; set; }

        public short MapId { get; set; }

        public int MapNpcId { get; set; }

        public short MapX { get; set; }

        public short MapY { get; set; }

        public short NpcVNum { get; set; }

        public byte Position { get; set; }

        #endregion
    }
}