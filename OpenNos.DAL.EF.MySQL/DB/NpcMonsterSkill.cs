//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace OpenNos.DAL.EF.MySQL.DB
{
    using System;
    using System.Collections.Generic;
    
    public partial class NpcMonsterSkill
    {
        public long CharacterSkillId { get; set; }
        public short Rate { get; set; }
        public short SkillVNum { get; set; }
        public short NpcMonsterVNum { get; set; }
    
        public virtual Skill skill { get; set; }
        public virtual NpcMonster npcmonster { get; set; }
    }
}
