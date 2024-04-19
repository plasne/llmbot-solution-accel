public interface IConfig
{
    string LLM_URI { get; }

    int CHARACTERS_PER_UPDATE { get; }

    void Validate();
}