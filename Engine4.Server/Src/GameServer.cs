using Engine4.Utility.Versions;

namespace Engine4.Server;

public abstract class GameServer : GameCore {
	protected GameServer(string name, IPackableVersion version) : base(name, version) { }
}