namespace NetZero.Extensions
{
    public static class FtoQualityEx
    {
        public static int ToOpcQuality(this UAManagedCore.Quality quality)
        {
            return quality switch
            {
                UAManagedCore.Quality.Good => Cca.Cgp.Core.Base.Ia.Quality.Good.Value,
                UAManagedCore.Quality.Uncertain => Cca.Cgp.Core.Base.Ia.Quality.Uncertain.Value,
                _ => Cca.Cgp.Core.Base.Ia.Quality.Bad.Value
            };
        }
    }
}
