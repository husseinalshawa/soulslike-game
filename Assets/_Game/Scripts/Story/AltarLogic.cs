namespace SoulsGame.Story
{
    // منطق مذبح الكنيسة الغارقة: وضع رماد الحارس يكمل الترتيلة الأخيرة —
    // مرة واحدة للأبد. الغلاف (GameLoop) يترجم النتيجة لراية/أرواح/تحول لون.
    public static class AltarLogic
    {
        public enum Result { Silent, Placed, AlreadyPlaced }

        public static Result TryPlace(StoryState state, KeyItemInventory items)
        {
            if (state.IsSet(StoryFlag.AshPlaced))
                return Result.AlreadyPlaced;
            if (!items.Has(StoryLibrary.WardensAshes))
                return Result.Silent;

            items.Remove(StoryLibrary.WardensAshes);
            state.Set(StoryFlag.AshPlaced);
            return Result.Placed;
        }
    }
}
