using Sandbox;

namespace HC2;

public interface ISaveData
{
    string Save();
    void Load( string data );
}