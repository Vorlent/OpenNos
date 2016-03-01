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
    
    public partial class InventoryItem
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public InventoryItem()
        {
            this.inventory = new HashSet<Inventory>();
        }
    
        public long InventoryItemId { get; set; }
        public short DamageMinimum { get; set; }
        public short DamageMaximum { get; set; }
        public short Concentrate { get; set; }
        public short HitRate { get; set; }
        public byte CriticalLuckRate { get; set; }
        public short CriticalRate { get; set; }
        public short CloseDefence { get; set; }
        public short DistanceDefence { get; set; }
        public short MagicDefence { get; set; }
        public short DistanceDefenceDodge { get; set; }
        public short DefenceDodge { get; set; }
        public short ElementRate { get; set; }
        public byte Upgrade { get; set; }
        public byte Rare { get; set; }
        public short Color { get; set; }
        public byte Amount { get; set; }
        public byte SpLevel { get; set; }
        public short SpXp { get; set; }
        public short SlElement { get; set; }
        public short SlHit { get; set; }
        public short HP { get; set; }
        public short MP { get; set; }
        public short SlDefence { get; set; }
        public short SlHP { get; set; }
        public byte DarkElement { get; set; }
        public byte LightElement { get; set; }
        public byte WaterElement { get; set; }
        public byte FireElement { get; set; }
        public short ItemVNum { get; set; }
        public byte Ammo { get; set; }
        public bool IsFixed { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Inventory> inventory { get; set; }
        public virtual Item item { get; set; }
    }
}
