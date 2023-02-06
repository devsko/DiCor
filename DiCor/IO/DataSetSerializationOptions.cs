namespace DiCor.IO
{
    public enum CharacterDecoderFallbackMode
    {
        Replacement,
        Exception,
    }

    public class DataSetSerializationOptions
    {
        public CharacterDecoderFallbackMode CharacterDecoderFallback { get; set; }
    }
}
