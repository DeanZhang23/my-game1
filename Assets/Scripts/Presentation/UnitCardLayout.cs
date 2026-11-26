namespace Echoes.Presentation
{
    public struct UnitLayerPlacement
    {
        public float CardX;
        public float CardY;
        public float SpriteX;
        public float SpriteY;
        public bool LeftSlot;
    }

    // Board-local pixels. The nameplate stays on the formation slot.
    // PortraitRest is the combat card's existing portrait point, so a body
    // that has not left its slot still sits in that card.
    // SpriteX/Y is that portrait in the same board space: the body plus PortraitRest.
    public static class UnitCardLayout
    {
        public const float PortraitRestX = -26f;
        public const float PortraitRestY = 12f;

        public static UnitLayerPlacement Place(float slotX, float slotY, float bodyX, float bodyY)
        {
            UnitLayerPlacement place;
            place.CardX = slotX;
            place.CardY = slotY;
            place.LeftSlot = bodyX != slotX || bodyY != slotY;
            place.SpriteX = bodyX + PortraitRestX;
            place.SpriteY = bodyY + PortraitRestY;
            return place;
        }
    }
}
