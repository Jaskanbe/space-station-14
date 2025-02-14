using System;
using Content.Shared.Damage;
using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Weapon.Melee.Components
{
    [RegisterComponent]
    public class MeleeWeaponComponent : Component
    {
        public override string Name => "MeleeWeapon";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("hitSound")]
        public SoundSpecifier HitSound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/genhit1.ogg");

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("missSound")]
        public SoundSpecifier MissSound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/punchmiss.ogg");

        [ViewVariables]
        [DataField("arcCooldownTime")]
        public float ArcCooldownTime { get; } = 1f;

        [ViewVariables]
        [DataField("cooldownTime")]
        public float CooldownTime { get; } = 1f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("clickArc")]
        public string ClickArc { get; set; } = "punch";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("arc")]
        public string Arc { get; set; } = "default";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("arcwidth")]
        public float ArcWidth { get; set; } = 90;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("range")]
        public float Range { get; set; } = 1;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("damage")]
        public int Damage { get; set; } = 5;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("damageType")]
        public DamageType DamageType { get; set; } = DamageType.Blunt;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("clickAttackEffect")]
        public bool ClickAttackEffect { get; set; } = true;

        public TimeSpan LastAttackTime;
        public TimeSpan CooldownEnd;
    }
}
