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
    public class MapMonsterDTO : MappingBaseDTO
    {
        public MapMonsterDTO()
        {
        }

        public MapMonsterDTO(MapMonsterDTO mapMonsterDTO)
        {
            this.IsDisabled = mapMonsterDTO.IsDisabled;
            this.IsMoving = mapMonsterDTO.IsMoving;
            this.MapId = mapMonsterDTO.MapId;
            this.MapMonsterId = mapMonsterDTO.MapMonsterId;
            this.MapX = mapMonsterDTO.MapX;
            this.MapY = mapMonsterDTO.MapY;
            this.MonsterVNum = mapMonsterDTO.MonsterVNum;
            this.Position = mapMonsterDTO.Position;
        }

        #region Properties

        public bool IsDisabled { get; set; }

        public bool IsMoving { get; set; }

        public short MapId { get; set; }

        public int MapMonsterId { get; set; }

        public short MapX { get; set; }

        public short MapY { get; set; }

        public short MonsterVNum { get; set; }

        public byte Position { get; set; }

        #endregion
    }
}