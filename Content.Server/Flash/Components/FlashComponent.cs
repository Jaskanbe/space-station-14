using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Flash.Components
{
    [RegisterComponent]
    public class FlashComponent : Component
    {
        public override string Name => "Flash";

        [DataField("duration")]
        [ViewVariables(VVAccess.ReadWrite)]
        public int FlashDuration { get; set; } = 5000;

        [DataField("uses")]
        [ViewVariables(VVAccess.ReadWrite)]
        public int Uses { get; set; } = 5;

        [DataField("range")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Range { get; set; } = 7f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("aoeFlashDuration")]
        public int AoeFlashDuration { get; set; } = 2000;

        [DataField("slowTo")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float SlowTo { get; set; } = 0.5f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("sound")]
        public SoundSpecifier Sound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/flash.ogg");

        public bool Flashing;

        public bool HasUses => Uses > 0;
    }
}
